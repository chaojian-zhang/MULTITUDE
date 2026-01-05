using MULTITUDE.Canvas;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace MULTITUDE.Class
{
    class StartUp
    {
        private const string AppId = "MULTITUDE"; // make this unique per app
        private static readonly string MutexName = $@"Global\{AppId}_SingleInstance";
        private static readonly string PipeName = $@"{AppId}_ArgsPipe";

        [STAThread]
        public static void Main(string[] args)
        {
            using Mutex mutex = new(initiallyOwned: true, name: MutexName, createdNew: out bool isFirstInstance);

            if (!isFirstInstance)
            {
                // Forward args to the first instance then exit.
                TrySendArgsToFirstInstance(args);
                return;
            }

            // Primary instance: start IPC server.
            using CancellationTokenSource cts = new();
            _ = Task.Run(() => PipeServerLoopAsync(cts.Token));

            // Start WPF app normally.
            App app = new();
            app.InitializeComponent();
            app.Startup += (_, __) =>
            {
                // Optional: handle initial args on startup
                if (args.Length > 0)
                    app.NewStartRequest(args);
            };

            app.Exit += (_, __) => cts.Cancel();
            app.Run();
        }

        private static void TrySendArgsToFirstInstance(string[] args)
        {
            try
            {
                using NamedPipeClientStream client = new(
                    serverName: ".",
                    pipeName: PipeName,
                    direction: PipeDirection.Out);

                // quick connect; adjust timeout if desired
                client.Connect(timeout: 500);

                string payload = JsonSerializer.Serialize(args);
                byte[] bytes = Encoding.UTF8.GetBytes(payload);
                client.Write(bytes, 0, bytes.Length);
                client.Flush();
            }
            catch
            {
                // If you want, log. Usually just exit quietly.
            }
        }

        private static async Task PipeServerLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using NamedPipeServerStream server = new(
                        PipeName,
                        PipeDirection.In,
                        maxNumberOfServerInstances: 1,
                        transmissionMode: PipeTransmissionMode.Byte,
                        options: PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(token).ConfigureAwait(false);

                    using MemoryStream ms = new();
                    byte[] buffer = new byte[4096];
                    int read;
                    while ((read = await server.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
                        ms.Write(buffer, 0, read);

                    string json = Encoding.UTF8.GetString(ms.ToArray());
                    string[] args = JsonSerializer.Deserialize<string[]>(json) ?? Array.Empty<string>();

                    // Marshal to UI thread and call your handler.
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        if (Application.Current is App app)
                            app.NewStartRequest(args);
                    });
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch
                {
                    // swallow/log and keep server alive
                }
            }
        }
    }
}