using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace CgLogViewer
{
    public class CgLogHandler : IDisposable
    {
        const int InitialTailReadBytes = 32 * 1024;
        const int MaxTailReadBytes = 2 * 1024 * 1024;
        static readonly Encoding bestFitEncoding = Encoding.GetEncoding(950, EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback);
        readonly object locker = new object();
        readonly Encoding logEncoding = bestFitEncoding;
        readonly Dictionary<string, long> readOffsets = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, string> pendingTextBuffers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, byte[]> pendingByteBuffers = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Decoder> decoders = new Dictionary<string, Decoder>(StringComparer.OrdinalIgnoreCase);
        readonly string logPath;
        readonly FileSystemWatcher fsw;
        static readonly Lazy<Dictionary<char, char>> bestFitPrivateUseMap = new Lazy<Dictionary<char, char>>(BuildBestFitPrivateUseMap, true);

        public event EventHandler<LogLineEventArgs> OnNewLog;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int WideCharToMultiByte(
            uint codePage,
            uint flags,
            string wideCharStr,
            int cchWideChar,
            byte[] multiByteStr,
            int cbMultiByte,
            IntPtr defaultChar,
            IntPtr usedDefaultChar);

        public CgLogHandler(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path) || !ValidationPath(path))
            {
                throw new ArgumentException(nameof(path));
            }

            logPath = Path.Combine(path, "Log");
            InitializeReadOffsets();

            fsw = new FileSystemWatcher(logPath)
            {
                Filter = "*.txt",
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            fsw.Changed += Fsw_Changed;
            fsw.Created += Fsw_Created;
            fsw.Renamed += Fsw_Renamed;
        }

        public static bool ValidationPath(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    return false;
                }

                return Directory.Exists(Path.Combine(path, "Log"));
            }
            catch
            {
                return false;
            }
        }

        public IReadOnlyList<LogLine> ReadRecentLogs(int maxCount)
        {
            int safeCount = Math.Max(0, maxCount);
            if (safeCount == 0)
            {
                return Array.Empty<LogLine>();
            }

            var logFiles = Directory.GetFiles(logPath, "*.txt")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .Take(5)
                .OrderBy(path => path)
                .ToArray();

            var results = new List<LogLine>(safeCount);
            foreach (var filePath in logFiles)
            {
                try
                {
                    var tailContent = ReadTailContent(filePath, Math.Max(safeCount, 120));
                    foreach (var rawLine in SplitLines(tailContent))
                    {
                        var parsed = ParseLine(rawLine, filePath);
                        if (parsed != null)
                        {
                            results.Add(parsed);
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }

            return results
                .OrderBy(line => line.EffectiveTimestamp)
                .Skip(Math.Max(0, results.Count - safeCount))
                .ToList();
        }

        public static IReadOnlyList<LogLine> ReadLogFile(string filePath, int maxCount = int.MaxValue)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return Array.Empty<LogLine>();
            }

            int safeCount = maxCount <= 0 ? int.MaxValue : maxCount;
            var results = new List<LogLine>();

            using (var reader = new StreamReader(filePath, bestFitEncoding, true))
            {
                string rawLine;
                while ((rawLine = reader.ReadLine()) != null)
                {
                    var parsed = ParseStandaloneLine(NormalizeChunk(rawLine), filePath);
                    if (parsed != null)
                    {
                        results.Add(parsed);
                    }
                }
            }

            if (safeCount == int.MaxValue || results.Count <= safeCount)
            {
                return results;
            }

            return results
                .OrderBy(line => line.EffectiveTimestamp)
                .Skip(results.Count - safeCount)
                .ToList();
        }

        void InitializeReadOffsets()
        {
            foreach (var filePath in Directory.GetFiles(logPath, "*.txt"))
            {
                InitializeFileState(filePath, setOffsetToEnd: true);
            }
        }

        void Fsw_Changed(object sender, FileSystemEventArgs e)
        {
            ReadAppendedLogs(e.FullPath);
        }

        void Fsw_Created(object sender, FileSystemEventArgs e)
        {
            InitializeFileState(e.FullPath, setOffsetToEnd: false);
            ReadAppendedLogs(e.FullPath);
        }

        void Fsw_Renamed(object sender, RenamedEventArgs e)
        {
            InitializeFileState(e.FullPath, setOffsetToEnd: false);
            ReadAppendedLogs(e.FullPath);
        }

        void InitializeFileState(string filePath, bool setOffsetToEnd)
        {
            try
            {
                long offset = 0;
                if (setOffsetToEnd && File.Exists(filePath))
                {
                    offset = new FileInfo(filePath).Length;
                }

                readOffsets[filePath] = offset;
                pendingTextBuffers[filePath] = string.Empty;
                pendingByteBuffers[filePath] = Array.Empty<byte>();
                decoders[filePath] = logEncoding.GetDecoder();
            }
            catch
            {
                // ignored
            }
        }

        void ResetDecoderState(string filePath)
        {
            pendingTextBuffers[filePath] = string.Empty;
            pendingByteBuffers[filePath] = Array.Empty<byte>();
            decoders[filePath] = logEncoding.GetDecoder();
        }

        void ReadAppendedLogs(string filePath)
        {
            try
            {
                lock (locker)
                {
                    if (!File.Exists(filePath))
                    {
                        return;
                    }

                    if (!readOffsets.ContainsKey(filePath))
                    {
                        InitializeFileState(filePath, setOffsetToEnd: false);
                    }

                    long offset = readOffsets.TryGetValue(filePath, out long storedOffset) ? storedOffset : 0;

                    using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        if (stream.Length < offset)
                        {
                            offset = 0;
                            ResetDecoderState(filePath);
                        }

                        if (stream.Length == offset)
                        {
                            return;
                        }

                        stream.Seek(offset, SeekOrigin.Begin);
                        var unreadLength = checked((int)(stream.Length - offset));
                        var buffer = new byte[unreadLength];
                        var bytesRead = stream.Read(buffer, 0, buffer.Length);
                        readOffsets[filePath] = stream.Length;

                        if (bytesRead <= 0)
                        {
                            return;
                        }

                        var chunk = DecodeIncrementalChunk(filePath, buffer, bytesRead);
                        var combined = (pendingTextBuffers.TryGetValue(filePath, out string pendingText) ? pendingText : string.Empty) + chunk;
                        var segments = SplitLines(combined).ToList();
                        bool endsWithLineBreak = EndsWithLineBreak(combined);

                        if (!endsWithLineBreak && segments.Count > 0)
                        {
                            pendingTextBuffers[filePath] = segments[segments.Count - 1];
                            segments.RemoveAt(segments.Count - 1);
                        }
                        else
                        {
                            pendingTextBuffers[filePath] = string.Empty;
                        }

                        foreach (var segment in segments)
                        {
                            var parsed = ParseLine(segment, filePath, DateTime.Now);
                            if (parsed != null)
                            {
                                OnNewLog?.Invoke(this, new LogLineEventArgs(parsed));
                            }
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }
        }

        string ReadTailContent(string filePath, int minimumLineCount)
        {
            long length = new FileInfo(filePath).Length;
            if (length == 0)
            {
                return string.Empty;
            }

            int windowSize = InitialTailReadBytes;
            while (true)
            {
                int bytesToRead = (int)Math.Min(length, windowSize);
                var buffer = new byte[bytesToRead];

                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    stream.Seek(length - bytesToRead, SeekOrigin.Begin);
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead != buffer.Length)
                    {
                        Array.Resize(ref buffer, bytesRead);
                    }
                }

                var content = NormalizeChunk(logEncoding.GetString(buffer));
                if (bytesToRead < length)
                {
                    content = DropPartialLeadingLine(content);
                }

                int parsedLineCount = SplitLines(content).Count(rawLine => ParseLine(rawLine, filePath) != null);
                if (bytesToRead >= length || parsedLineCount >= minimumLineCount || windowSize >= MaxTailReadBytes)
                {
                    return content;
                }

                windowSize *= 2;
            }
        }

        string DecodeIncrementalChunk(string filePath, byte[] buffer, int bytesRead)
        {
            var prefix = pendingByteBuffers.TryGetValue(filePath, out byte[] pendingBytes) ? pendingBytes : Array.Empty<byte>();
            var combinedBytes = new byte[prefix.Length + bytesRead];

            if (prefix.Length > 0)
            {
                Buffer.BlockCopy(prefix, 0, combinedBytes, 0, prefix.Length);
            }

            Buffer.BlockCopy(buffer, 0, combinedBytes, prefix.Length, bytesRead);

            if (!decoders.TryGetValue(filePath, out Decoder decoder))
            {
                decoder = logEncoding.GetDecoder();
                decoders[filePath] = decoder;
            }

            var charBuffer = new char[logEncoding.GetMaxCharCount(combinedBytes.Length)];
            decoder.Convert(combinedBytes, 0, combinedBytes.Length, charBuffer, 0, charBuffer.Length, false, out int bytesUsed, out int charsUsed, out bool _);

            int pendingCount = combinedBytes.Length - bytesUsed;
            if (pendingCount > 0)
            {
                var leftover = new byte[pendingCount];
                Buffer.BlockCopy(combinedBytes, bytesUsed, leftover, 0, pendingCount);
                pendingByteBuffers[filePath] = leftover;
            }
            else
            {
                pendingByteBuffers[filePath] = Array.Empty<byte>();
            }

            return NormalizeChunk(new string(charBuffer, 0, charsUsed));
        }

        static IEnumerable<string> SplitLines(string content)
        {
            return content.Split(new[] { '\n' }, StringSplitOptions.None);
        }

        static bool EndsWithLineBreak(string content)
        {
            return !string.IsNullOrEmpty(content) &&
                (content[content.Length - 1] == '\n' || content[content.Length - 1] == '\r');
        }

        static string DropPartialLeadingLine(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return string.Empty;
            }

            int lineBreakIndex = content.IndexOf('\n');
            if (lineBreakIndex < 0)
            {
                return string.Empty;
            }

            return content.Substring(lineBreakIndex + 1);
        }

        static string NormalizeChunk(string chunk)
        {
            return ConvertPrivateUseJapanese(chunk
                .Replace("\r\n", "\n")
                .Replace('\r', '\n')
                .Replace("\ueeb8", " ")
                .Trim('\u0000'));
        }

        static string ConvertPrivateUseJapanese(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var map = bestFitPrivateUseMap.Value;
            var builder = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (map.TryGetValue(c, out char mapped))
                {
                    builder.Append(mapped);
                    continue;
                }

                builder.Append(c);
            }

            return builder.ToString();
        }

        static Dictionary<char, char> BuildBestFitPrivateUseMap()
        {
            var map = new Dictionary<char, char>();

            AddPreferredMappings(map, 0x3000, 0x30FF);
            AddPreferredMappings(map, 0x31F0, 0x31FF);
            AddPreferredMappings(map, 0x3400, 0x4DBF);
            AddPreferredMappings(map, 0x4E00, 0x9FFF);
            AddPreferredMappings(map, 0xF900, 0xFAFF);
            AddPreferredMappings(map, 0xFF00, 0xFFEF);
            AddPreferredMappings(map, 0x0001, 0xFFFF, skipPreferredRanges: true);

            return map;
        }

        static void AddPreferredMappings(Dictionary<char, char> map, int start, int end, bool skipPreferredRanges = false)
        {
            for (int codePoint = start; codePoint <= end; codePoint++)
            {
                if (skipPreferredRanges && IsPreferredRange(codePoint))
                {
                    continue;
                }

                if (codePoint >= 0xD800 && codePoint <= 0xDFFF)
                {
                    continue;
                }

                TryAddBestFitMapping(map, (char)codePoint);
            }
        }

        static bool IsPreferredRange(int codePoint)
        {
            return (codePoint >= 0x3000 && codePoint <= 0x30FF) ||
                (codePoint >= 0x31F0 && codePoint <= 0x31FF) ||
                (codePoint >= 0x3400 && codePoint <= 0x4DBF) ||
                (codePoint >= 0x4E00 && codePoint <= 0x9FFF) ||
                (codePoint >= 0xF900 && codePoint <= 0xFAFF) ||
                (codePoint >= 0xFF00 && codePoint <= 0xFFEF);
        }

        static void TryAddBestFitMapping(Dictionary<char, char> map, char sourceChar)
        {
            var buffer = new byte[8];
            int byteCount = WideCharToMultiByte(950, 0, sourceChar.ToString(), 1, buffer, buffer.Length, IntPtr.Zero, IntPtr.Zero);
            if (byteCount <= 0)
            {
                return;
            }

            var decoded = bestFitEncoding.GetString(buffer, 0, byteCount);

            if (decoded.Length != 1)
            {
                return;
            }

            char privateUseChar = decoded[0];
            if (privateUseChar == sourceChar || !IsPrivateUse(privateUseChar) || map.ContainsKey(privateUseChar))
            {
                return;
            }

            map.Add(privateUseChar, sourceChar);
        }

        static bool IsPrivateUse(char c)
        {
            return (c >= '\uE000' && c <= '\uF8FF');
        }

        LogLine ParseLine(string line, string filePath, DateTime? observedAt = null)
        {
            return ParseStandaloneLine(line, filePath, observedAt);
        }

        static LogLine ParseStandaloneLine(string line, string filePath, DateTime? observedAt = null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            var trimmed = line.TrimStart();
            if (trimmed.Length < 8)
            {
                return null;
            }

            if (!DateTime.TryParseExact(trimmed.Substring(0, 8), "HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timePart))
            {
                return null;
            }

            var message = trimmed.Substring(8).TrimStart(' ', '\t', '\u3000', '\ueeb8');
            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            var logDate = ExtractLogDate(filePath) ?? DateTime.Today;
            var parsedTimestamp = logDate.Date.Add(timePart.TimeOfDay);
            var effectiveTimestamp = observedAt.HasValue
                ? ResolveObservedTimestamp(observedAt.Value, timePart.TimeOfDay)
                : parsedTimestamp;
            var normalizedMessage = message.Trim();
            return new LogLine(
                parsedTimestamp,
                effectiveTimestamp,
                normalizedMessage,
                Path.GetFileName(filePath),
                LogLine.ClassifyMessage(normalizedMessage));
        }

        static DateTime ResolveObservedTimestamp(DateTime observedAt, TimeSpan timeOfDay)
        {
            var sameDay = observedAt.Date.Add(timeOfDay);
            var previousDay = sameDay.AddDays(-1);
            var nextDay = sameDay.AddDays(1);

            var best = sameDay;
            var bestDistance = Math.Abs((observedAt - sameDay).Ticks);

            var previousDistance = Math.Abs((observedAt - previousDay).Ticks);
            if (previousDistance < bestDistance)
            {
                best = previousDay;
                bestDistance = previousDistance;
            }

            var nextDistance = Math.Abs((observedAt - nextDay).Ticks);
            if (nextDistance < bestDistance)
            {
                best = nextDay;
            }

            return best;
        }

        static DateTime? ExtractLogDate(string filePath)
        {
            var match = Regex.Match(Path.GetFileNameWithoutExtension(filePath), @"_(\d{6})$");
            if (!match.Success)
            {
                return null;
            }

            if (DateTime.TryParseExact(match.Groups[1].Value, "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                return parsedDate;
            }

            return null;
        }

        public void Dispose()
        {
            fsw.Dispose();
        }
    }
}
