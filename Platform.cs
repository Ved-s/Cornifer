using Microsoft.Win32;
using SixLabors.ImageSharp.ColorSpaces.Conversion;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace Cornifer
{
    public static class Platform
    {
        private static IWin32Window? GameWindow
        {
            get
            {
                nint handle = Process.GetCurrentProcess().MainWindowHandle;
                if (handle == 0)
                    return null;

                return gameWindow ??= new WindowHandle(handle);
            }
        }

        private static WindowsInteractionTaskSheduler Sheduler = new();
        private static IWin32Window? gameWindow;

        private static Task<Stream>? StartupStateStream;
        private static string? StartupStatePath;

        const int RegistryDataVersion = 1;
        const string OpenWebMapProtocol = "cornifer://openweb/";

        public static void Start(string[] args)
        {
            if (args.Length >= 1 && args[0].StartsWith(OpenWebMapProtocol))
            {
                Main.TryCatchReleaseException(() =>
                {
                    string url = $"https://{args[0].Substring(OpenWebMapProtocol.Length)}";

                    StartupStateStream = Task.Run(async () =>
                    {
                        HttpResponseMessage response = await new HttpClient().GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                        long? max = response.Content.Headers.ContentLength;

                        MemoryStream bufferStream = new((int?)max ?? 1024 * 1024);
                        TaskProgress progress = new("Downloading file", max is null ? null : 1);

                        byte[] buffer = ArrayPool<byte>.Shared.Rent(65536);

                        Stream stream = response.Content.ReadAsStream();

                        long current = 0;
                        while (true)
                        {
                            int read = await stream.ReadAsync(buffer);
                            if (read <= 0)
                                break;

                            await bufferStream.WriteAsync(buffer, 0, read);
                            current += read;

                            if (max.HasValue)
                                progress.Progress = (float)((double)current / max.Value);
                        }

                        ArrayPool<byte>.Shared.Return(buffer);
                        progress.Dispose();
                        bufferStream.Position = 0;
                        return bufferStream as Stream;
                    });

                }, "Exception has been thrown while opening web map");
            }
            if (args.Length >= 1 && File.Exists(args[0]))
            {
                StartupStateStream = Task.FromResult<Stream>(File.OpenRead(args[0]));
                StartupStatePath = args[0];
            }

            object? registryVersionObject = Registry.ClassesRoot.OpenSubKey("cornimapFile")?.GetValue("RegistryDataVersion", null);
            if (registryVersionObject is not int registryVersion || registryVersion != RegistryDataVersion)
            {
                string? corniferExe = Process.GetCurrentProcess().MainModule?.FileName;
                if (corniferExe is not null)
                {
                    try
                    {
                        RegistryKey cornimapExtension = Registry.ClassesRoot.CreateSubKey(".cornimap");
                        cornimapExtension.SetValue(null, "cornimapFile");

                        RegistryKey cornimapFile = Registry.ClassesRoot.CreateSubKey("cornimapFile");
                        cornimapFile.SetValue(null, "Cornifer map");
                        cornimapFile.SetValue("RegistryDataVersion", RegistryDataVersion);

                        RegistryKey open = cornimapFile.CreateSubKey("shell").CreateSubKey("open");
                        open.SetValue(null, "Open map");
                        open.CreateSubKey("command").SetValue(null, @$"{corniferExe} ""%1""");

                        RegistryKey corniferProtocol = Registry.ClassesRoot.CreateSubKey("cornifer");
                        corniferProtocol.SetValue(null, "Cornifer");
                        corniferProtocol.SetValue("URL Protocol", "");
                        open = corniferProtocol.CreateSubKey("shell").CreateSubKey("open");
                        open.SetValue(null, "Open map");
                        open.CreateSubKey("command").SetValue(null, @$"{corniferExe} ""%1""");
                    }
                    catch (UnauthorizedAccessException)
                    {
                        if (!File.Exists(Path.Combine(Main.MainDir, "noAdminWarning.txt")))
                        {
                            Sheduler.Shedule(() =>
                            {
                                System.Windows.Forms.MessageBox.Show(GameWindow,
                                    "Cannot update registry values.\n" +
                                    "Please restart Cornifer with admin privileges to register file extension\n" +
                                    "Or create file named \"noAdminWarning.txt\" in Cornifer folder.", "Admin access required");
                            });
                        }
                    }
                }
            }
        }

        public static async Task<(Stream? stream, string? saveFileName)> GetStartupStateFileStream()
        {
            if (StartupStateStream is null)
                return (null, null);

            return (await StartupStateStream, StartupStatePath);
        }

        public static async Task<MessageBoxResult> MessageBox(string text, string caption, MessageBoxButtons buttons = MessageBoxButtons.Ok)
        {
            return await Sheduler.Shedule(() =>
            {
                System.Windows.Forms.MessageBoxButtons winformsButtons = buttons switch
                {
                    MessageBoxButtons.OkCancel => System.Windows.Forms.MessageBoxButtons.OKCancel,
                    _ => System.Windows.Forms.MessageBoxButtons.OK,
                };

                DialogResult result = System.Windows.Forms.MessageBox.Show(GameWindow, text, caption, winformsButtons);

                return result switch
                {
                    DialogResult.OK => MessageBoxResult.Ok,
                    DialogResult.Cancel => MessageBoxResult.Cancel,
                    _ => MessageBoxResult.None,
                };
            });
        }

        public static async Task<string?> OpenFileDialog(string title, string filter, string? filename = null, string? startDir = null)
        {
            return await Sheduler.Shedule(() =>
            {
                OpenFileDialog dialog = new()
                {
                    FileName = filename,
                    InitialDirectory = startDir,
                    Title = title,
                    Filter = filter
                };

                if (dialog.ShowDialog(GameWindow) == DialogResult.OK)
                    return dialog.FileName;
                return null;
            });
        }

        public static async Task<string?> SaveFileDialog(string title, string filter, string? filename = null, string? startDir = null)
        {
            return await Sheduler.Shedule(() =>
            {
                SaveFileDialog dialog = new()
                {
                    FileName = filename,
                    InitialDirectory = startDir,
                    Title = title,
                    Filter = filter
                };

                if (dialog.ShowDialog(GameWindow) == DialogResult.OK)
                    return dialog.FileName;
                return null;
            });
        }

        public static async Task<string?> FolderBrowserDialog(string title, string? selectedPath = null, string? startDir = null)
        {
            return await Sheduler.Shedule(() =>
            {
                FolderBrowserDialog dialog = new()
                {
                    InitialDirectory = startDir,
                    SelectedPath = selectedPath,
                    UseDescriptionForTitle = true,
                    Description = title
                };

                if (dialog.ShowDialog(GameWindow) == DialogResult.OK)
                    return dialog.SelectedPath;
                return null;
            });
        }

        public static async Task<string> GetClipboard()
        {
            return await Sheduler.Shedule(() =>
            {
                return Clipboard.GetText();
            });
        }

        public static void SetClipboard(string value)
        {
            Sheduler.Shedule(() =>
            {
                Clipboard.SetText(value);
            });
        }

        public static void Stop()
        {
            Sheduler.Dispose();
        }

        class WindowsInteractionTaskSheduler : TaskScheduler, IDisposable
        {
            Queue<Task> TaskQueue = new();
            Thread Thread;
            AutoResetEvent Signal = new(true);

            public WindowsInteractionTaskSheduler()
            {
                Thread = new(StartInteractionThread)
                {
                    Name = "Windows interaction thread",
                };
                Thread.SetApartmentState(ApartmentState.STA);
                Thread.Start();
            }

            void StartInteractionThread()
            {
                while (true)
                {
                    try
                    {
                        while (TaskQueue.TryDequeue(out Task? task))
                        {
                            TryExecuteTask(task);
                        }
                        Signal.WaitOne();
                    }
                    catch (ThreadInterruptedException)
                    {
                        break;
                    }
                }
            }

            public Task<T> Shedule<T>(Func<T> handler)
            {
                return Task.Factory.StartNew(handler, CancellationToken.None, TaskCreationOptions.None, this);
            }

            public Task Shedule(Action handler)
            {
                return Task.Factory.StartNew(handler, CancellationToken.None, TaskCreationOptions.None, this);
            }

            protected override IEnumerable<Task>? GetScheduledTasks() => TaskQueue;

            protected override void QueueTask(Task task)
            {
                TaskQueue.Enqueue(task);
                Signal.Set();
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return false;
            }

            public void Dispose()
            {
                Thread?.Interrupt();
                Signal.Set();
            }
        }
        class WindowHandle : IWin32Window
        {
            public WindowHandle(nint handle)
            {
                Handle = handle;
            }

            public IntPtr Handle { get; }
        }

        public enum MessageBoxButtons
        {
            Ok,
            OkCancel
        }

        public enum MessageBoxResult
        {
            Ok,
            Cancel,
            None
        }
    }
}
