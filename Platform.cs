using Microsoft.Win32;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cornifer
{
    public static class Platform
    {
        private static IWin32Window? GameWindow
        {
            get
            {
                if (DetachedWindow)
                    return null;

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
        private static bool DetachedWindow = false;

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

        public static async Task<string> GetClipboardText()
        {
            return await Sheduler.Shedule(() =>
            {
                return Clipboard.GetText();
            });
        }

        public static void SetClipboardText(string value)
        {
            Sheduler.Shedule(() =>
            {
                Clipboard.SetText(value);
            });
        }

        public static async Task<Image<Rgba32>?> GetClipboardImage() 
        {
            return await Sheduler.Shedule(() =>
            {
                IDataObject data = Clipboard.GetDataObject();

                if (data.GetDataPresent("PNG") && data.GetData("PNG") is Stream pngStream)
                {
                    try
                    {
                        return Image<Rgba32>.Load<Rgba32>(pngStream);
                    }
                    catch { }
                }

                // TODO: Bitmap and DIB

                return null;
            });
        }

        // https://stackoverflow.com/a/46424800
        // Stores clipboard image in PNG, Bitmap and DIB formats
        public static void SetClipboardImage(Image<Rgba32> image)
        {
            MemoryStream pngStream = new();
            image.SaveAsPng(pngStream);

            Bitmap bitmap = new(image.Width, image.Height);

            var bits = bitmap.LockBits(new(0, 0, image.Width, image.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            static void WriteDIB(Stream stream, Span<byte> argbData, int width, int height)
            {
                using BinaryWriter writer = new(stream, Encoding.Default, true);

                // BITMAPINFOHEADER struct for DIB.
                int hdrSize = 0x28;
                //Byte[] fullImage = new Byte[hdrSize + 12 + bm32bData.Length];

                //Int32 biSize;
                writer.Write(hdrSize);
                //Int32 biWidth;
                writer.Write(width);
                //Int32 biHeight;
                writer.Write(height);
                //Int16 biPlanes;
                writer.Write((ushort)1);
                //Int16 biBitCount;
                writer.Write((ushort)32);
                //BITMAPCOMPRESSION biCompression = BITMAPCOMPRESSION.BITFIELDS;
                writer.Write(3);
                //Int32 biSizeImage;
                writer.Write(argbData.Length);
                //Int32 biXPelsPerMeter = 0;
                writer.Write(0);
                //Int32 biYPelsPerMeter = 0;
                writer.Write(0);
                //Int32 biClrUsed = 0;
                writer.Write(0);
                //Int32 biClrImportant = 0;
                writer.Write(0);

                // The aforementioned "BITFIELDS": colour masks applied to the Int32 pixel value to get the R, G and B values.
                writer.Write(0x00FF0000);
                writer.Write(0x0000FF00);
                writer.Write(0x000000FF);

                writer.Write(argbData);
            }

            MemoryStream dibStream = new();
            unsafe
            {
                for (int j = 0; j < image.Height; j++)
                {
                    Span<Rgba32> src = image.DangerousGetPixelRowMemory(j).Span;
                    Span<uint> dst = new((bits.Scan0 + j * image.Width * 4).ToPointer(), image.Width);

                    for (int i = 0; i < image.Width; i++)
                    {
                        Rgba32 c = src[i];
                        dst[i] = (uint)c.A << 24 | (uint)c.R << 16 | (uint)c.G << 8 | (uint)c.B;
                    }
                }
                WriteDIB(dibStream, new(bits.Scan0.ToPointer(), image.Width * image.Height * 4), image.Width, image.Height);
            }
            bitmap.UnlockBits(bits);

            Sheduler.Shedule(() =>
            {
                DataObject data = new();

                data.SetData(DataFormats.Bitmap, bitmap);
                data.SetData("PNG", true, pngStream);
                data.SetData(DataFormats.Dib, false, dibStream);

                Clipboard.SetDataObject(data, true);
                pngStream.Dispose();
                dibStream.Dispose();
            });
        }

        public static void DetachWindow()
        {
            gameWindow = null;
            DetachedWindow = true;
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
