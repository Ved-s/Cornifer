using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cornifer
{
    public static class Platform
    {
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

        static IWin32Window? GameWindow
        {
            get
            {
                nint handle = Process.GetCurrentProcess().MainWindowHandle;
                if (handle == 0)
                    return null;

                return gameWindow ??= new WindowHandle(handle);
            }
        }

        static WindowsInteractionTaskSheduler Sheduler = new();
        private static IWin32Window? gameWindow;

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

        class WindowsInteractionTaskSheduler : TaskScheduler
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
        }
        class WindowHandle : IWin32Window
        {
            public WindowHandle(nint handle)
            {
                Handle = handle;
            }

            public IntPtr Handle { get; }
        }
    }
}
