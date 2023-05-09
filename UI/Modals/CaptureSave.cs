using Cornifer.UI.Elements;
using Cornifer.UI.Structures;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.IO;

namespace Cornifer.UI.Modals
{
    public class CaptureSave : Modal<CaptureSave, Empty>
    {
        public CaptureSave()
        {
            Width = 250;
            Height = 150;

            Elements = new(this)
            {
                new UILabel
                {
                    Top = 5,
                    Height = 20,
                    AutoSize = false,
                    Text = "Select capture save format",
                    TextAlign = new(.5f)
                },

                new UIList
                {
                    Top = new(0, .5f, -.5f),
                    Height = 0,
                    Padding = new(0, 20),

                    AutoSize = true,

                    ElementSpacing = 4,
                    Elements =
                    {
                        new UIButton
                        {
                            Height = 20,
                            Text = "Single image",
                            TextAlign = new(.5f)
                        }.OnEvent(ClickEvent, async (_, _) =>
                        {
                            string? renderFile = await Platform.SaveFileDialog("Select render save file", "PNG Image|*.png");
                            if (renderFile is null)
                                return;

                            ReturnResult(new());

                            Main.MainThreadQueue.Enqueue(() =>
                            {
                                var capResult = Capture.CaptureMap();
                                IImageEncoder encoder = new PngEncoder();
                                using FileStream fs = File.Create(renderFile);
                                capResult.Save(fs, encoder);
                                capResult.Dispose();
                                GC.Collect();
                            });
                        }),
                        new UIButton
                        {
                            Height = 20,
                            Text = "One image per layer",
                            TextAlign = new(.5f)
                        }.OnEvent(ClickEvent, async (_, _) => 
                        {
                            if (Main.Region is null)
                                return;

                            string? renderDir = await Platform.FolderBrowserDialog("Select render save folder");
                            if (renderDir is null)
                                return;

                            ReturnResult(new());

                            Main.MainThreadQueue.Enqueue(() =>
                            {
                                Capture.CaptureMapToLayerImages(renderDir);
                                GC.Collect();
                            });
                        }),
                        new UIButton
                        {
                            Height = 20,
                            Text = "Layered PSD",
                            TextAlign = new(.5f)
                        }.OnEvent(ClickEvent, async (_, _) =>
                        {
                            string? renderFile = await Platform.SaveFileDialog("Select render save file", "Photoshop Document|*.psd");
                            if (renderFile is null)
                                return;

                            ReturnResult(new());

                            Main.MainThreadQueue.Enqueue(() =>
                            {
                                Capture.CaptureMapToPSD(renderFile);
                            });
                        }),
                    }
                },

                new UIButton
                {
                    Height = 20,
                    Top = new(-5, 1, -1),
                    Left = new(0, .5f, -.5f),
                    Text = "Cancel",
                    Width = 100,
                    TextAlign = new(.5f),
                }.OnEvent(ClickEvent, (_, _) => ReturnResult(new()))
            };
        }
    }
}
