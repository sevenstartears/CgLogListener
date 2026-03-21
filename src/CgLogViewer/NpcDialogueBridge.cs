using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace CgLogViewer
{
    public sealed class NpcDialogueBridgeServer : IDisposable
    {
        const string PipeName = "CgLogViewer.NpcDialogue";

        readonly Action<string> onMessage;
        readonly CancellationTokenSource cts = new CancellationTokenSource();
        Task listenTask;

        public NpcDialogueBridgeServer(Action<string> onMessage)
        {
            this.onMessage = onMessage ?? throw new ArgumentNullException(nameof(onMessage));
        }

        public void Start()
        {
            if (listenTask != null)
            {
                return;
            }

            listenTask = Task.Run(ListenLoopAsync);
        }

        async Task ListenLoopAsync()
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    using (var server = new NamedPipeServerStream(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                    {
                        await server.WaitForConnectionAsync(cts.Token).ConfigureAwait(false);
                        using (var reader = new BinaryReader(server))
                        {
                            string message = reader.ReadString();
                            if (!string.IsNullOrWhiteSpace(message))
                            {
                                onMessage(message);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    await Task.Delay(200, cts.Token).ConfigureAwait(false);
                }
            }
        }

        public void Dispose()
        {
            cts.Cancel();
            try
            {
                listenTask?.Wait(500);
            }
            catch
            {
                // ignored
            }
            cts.Dispose();
        }
    }

    public static class NpcDialogueBridgeClient
    {
        const string PipeName = "CgLogViewer.NpcDialogue";

        public static bool TrySend(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            try
            {
                using (var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    client.Connect(400);
                    using (var writer = new BinaryWriter(client))
                    {
                        writer.Write(message);
                        writer.Flush();
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
