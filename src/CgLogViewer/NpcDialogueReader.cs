using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CgLogViewer
{
    public sealed class NpcDialogueProcessInfo
    {
        public NpcDialogueProcessInfo(int processId, string processName, string mainWindowTitle, string executablePath)
        {
            ProcessId = processId;
            ProcessName = processName ?? string.Empty;
            MainWindowTitle = mainWindowTitle ?? string.Empty;
            ExecutablePath = executablePath ?? string.Empty;
        }

        public int ProcessId { get; private set; }
        public string ProcessName { get; private set; }
        public string MainWindowTitle { get; private set; }
        public string ExecutablePath { get; private set; }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(MainWindowTitle)
                ? string.Format("{0} ({1})", ProcessName, ProcessId)
                : string.Format("{0} ({1}) - {2}", ProcessName, ProcessId, MainWindowTitle);
        }
    }

    public sealed class NpcDialogueSnapshot
    {
        public NpcDialogueSnapshot(IReadOnlyList<string> lines, DateTime capturedAt)
        {
            Lines = lines ?? Array.Empty<string>();
            CapturedAt = capturedAt;
            Text = string.Join(Environment.NewLine, Lines);
        }

        public IReadOnlyList<string> Lines { get; private set; }
        public DateTime CapturedAt { get; private set; }
        public string Text { get; private set; }
    }

    public static class NpcDialogueReader
    {
        public const long DefaultBaseAddress = 0xC32AB8;
        public const int DefaultLineStride = 0x51;
        public const int DefaultLineCount = 10;
        public const int MaxLineCount = 20;

        const uint ProcessVmRead = 0x0010;
        const uint ProcessQueryInformation = 0x0400;
        const uint ProcessQueryLimitedInformation = 0x1000;

        static readonly Encoding dialogueEncoding = Encoding.GetEncoding(950, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr processHandle, IntPtr baseAddress, [Out] byte[] buffer, int size, out IntPtr numberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);

        public static IReadOnlyList<NpcDialogueProcessInfo> FindCandidateProcesses(string gameDirectory)
        {
            if (string.IsNullOrWhiteSpace(gameDirectory) || !Directory.Exists(gameDirectory))
            {
                return Array.Empty<NpcDialogueProcessInfo>();
            }

            string normalizedRoot = Path.GetFullPath(gameDirectory)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string normalizedRootPrefix = normalizedRoot + Path.DirectorySeparatorChar;

            var candidates = new List<NpcDialogueProcessInfo>();
            foreach (var process in Process.GetProcessesByName("bluecg"))
            {
                try
                {
                    using (process)
                    {
                        string executablePath = process.MainModule.FileName;
                        if (string.IsNullOrWhiteSpace(executablePath))
                        {
                            continue;
                        }

                        string normalizedExecutable = Path.GetFullPath(executablePath);
                        if (!string.Equals(Path.GetFileName(normalizedExecutable), "bluecg.exe", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (!string.Equals(normalizedExecutable, Path.Combine(normalizedRoot, "bluecg.exe"), StringComparison.OrdinalIgnoreCase) &&
                            !normalizedExecutable.StartsWith(normalizedRootPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        candidates.Add(new NpcDialogueProcessInfo(
                            process.Id,
                            process.ProcessName,
                            process.MainWindowTitle,
                            normalizedExecutable));
                    }
                }
                catch
                {
                    // ignored
                }
            }

            return candidates
                .OrderBy(item => item.ProcessId)
                .ToList();
        }

        public static bool TryReadDialogue(int processId, int lineCount, out NpcDialogueSnapshot snapshot, out string errorMessage)
        {
            snapshot = null;
            errorMessage = null;

            int safeLineCount = Math.Max(1, Math.Min(MaxLineCount, lineCount));
            IntPtr processHandle = OpenProcess(ProcessQueryInformation | ProcessQueryLimitedInformation | ProcessVmRead, false, processId);
            if (processHandle == IntPtr.Zero)
            {
                errorMessage = "ゲームプロセスを開けませんでした。";
                return false;
            }

            try
            {
                int totalBytes = safeLineCount * DefaultLineStride;
                var buffer = new byte[totalBytes];
                if (!ReadProcessMemory(processHandle, new IntPtr(DefaultBaseAddress), buffer, buffer.Length, out IntPtr readPtr))
                {
                    errorMessage = "NPC会話バッファを読み取れませんでした。";
                    return false;
                }

                int bytesRead = readPtr.ToInt32();
                if (bytesRead <= 0)
                {
                    snapshot = new NpcDialogueSnapshot(Array.Empty<string>(), DateTime.Now);
                    return true;
                }

                int availableLineCount = Math.Min(safeLineCount, Math.Max(1, bytesRead / DefaultLineStride));
                var lines = new List<string>(availableLineCount);
                for (int i = 0; i < availableLineCount; i++)
                {
                    int slotOffset = i * DefaultLineStride;
                    if (slotOffset >= bytesRead)
                    {
                        break;
                    }

                    if (buffer[slotOffset] == 0)
                    {
                        continue;
                    }

                    string decodedLine = DecodeLine(buffer, slotOffset, Math.Min(DefaultLineStride, bytesRead - slotOffset));
                    if (!string.IsNullOrWhiteSpace(decodedLine))
                    {
                        lines.Add(decodedLine);
                    }
                }

                snapshot = new NpcDialogueSnapshot(lines, DateTime.Now);
                return true;
            }
            finally
            {
                CloseHandle(processHandle);
            }
        }

        static string DecodeLine(byte[] buffer, int offset, int length)
        {
            int actualLength = 0;
            while (actualLength < length && buffer[offset + actualLength] != 0)
            {
                actualLength++;
            }

            if (actualLength <= 0)
            {
                return string.Empty;
            }

            string decoded = dialogueEncoding.GetString(buffer, offset, actualLength);
            return Sanitize(decoded);
        }

        static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (c == '\r' || c == '\n')
                {
                    continue;
                }

                builder.Append(char.IsControl(c) ? ' ' : c);
            }

            return builder.ToString().Trim();
        }
    }
}
