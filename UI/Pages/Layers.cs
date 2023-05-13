using Cornifer.UI.Elements;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Pages
{
    public class Layers : Page
    {
        public override int Order => 7;

        static UIList LayerList = null!;
        static List<LayerUI> UIs = new();

        public Layers() 
        {
            Elements = new(this)
            {
                new UIList()
                {
                    ElementSpacing = 4,
                }.Assign(out LayerList)
            };
            UpdateLayerList();
        }

        public static void UpdateLayerList()
        {
            if (LayerList is null)
                return;

            LayerList.Elements.Clear();
            UIs.Clear();

            for (int i = Main.Layers.Count - 1; i >= 0; i--)
            {
                Layer layer = Main.Layers[i];
                UIButton up, down;
                UIPanel inner;
                UILabel name;

                UIPanel panel = new()
                {
                    BackColor = Color.Transparent,
                    BorderColor = Color.Transparent,

                    Height = 39,

                    Elements =
                    {
                        new UIButton
                        {
                            Text = "˄",
                            Top = 0,
                            Left = new(0, 1, -1),
                            Width = 19,
                            Height = 19,
                            Padding = new(7, 5, 0, 0),
                            Enabled = i < Main.Layers.Count - 1,

                        }.Assign(out up),
                        new UIButton
                        {
                            Text = "˅",
                            Top = new(0, 1, -1),
                            Left = new(0, 1, -1),
                            Width = 19,
                            Height = 19,
                            Padding = new(7, 6, 0, 0),
                            Enabled = i > 0,

                        }.Assign(out down),
                        new UIPanel 
                        {
                            Width = new(-20, 1),
                            BorderColor = new(100, 100, 100),

                            Elements = 
                            {
                                new UILabel 
                                {
                                    Text = layer.Name,
                                    AutoSize = false,
                                    Margin = 4,
                                    TextAlign = new(0, .5f),

                                    Width = new(-20, 1)
                                }.Assign(out name)
                            }
                        }.Assign(out inner)
                    }
                };

                if (!layer.Special)
                {
                    inner.Elements.Add(new UIButton
                    {
                        Text = "D",
                        Top = new(-2, 1, -1),
                        Left = new(-2, 1, -1),
                        Width = 17,
                        Height = 17,
                        TextAlign = new(.5f),
                        AutoSize = false,
                    }.OnClick(_ => 
                    {
                        RemoveLayer(layer, panel);
                    }));
                    inner.Elements.Add(new UIButton
                    {
                        Text = "R",
                        Top = 2,
                        Left = new(-2, 1, -1),
                        Width = 17,
                        Height = 17,
                        TextAlign = new(.5f),
                        AutoSize = false,
                    }.OnClick(_ =>
                    {
                        RenameLayer(layer, name);
                    }));
                }

                LayerUI ui = new(layer, panel, up, down);
                UIs.Add(ui);

                up.OnClick(_ => MoveLayer(layer, panel, true));
                down.OnClick(_ => MoveLayer(layer, panel, false));

                LayerList.Elements.Add(panel);
                LayerList.Recalculate();
            }
        }

        static void RemoveLayer(Layer layer, UIPanel panel) 
        {
            // TODO: implement when adding layers is supported
        }
        static void RenameLayer(Layer layer, UILabel label) 
        {
            // TODO: name editing modal
        }
        static void MoveLayer(Layer layer, UIPanel panel, bool dirUp) 
        {
            int index = Main.Layers.IndexOf(layer);
            if (index < 0)
            {
                RemoveLayer(layer, panel);
                return;
            }

            if (dirUp && index >= Main.Layers.Count - 1 || !dirUp && index <= 0)
                return;

            int dir = dirUp? 1 : -1;

            Main.Layers.RemoveAt(index);
            Main.Layers.Insert(index + dir, layer);

            int uii = UIs.Count - 1 - index;
            LayerUI ui = UIs[uii];
            UIs.RemoveAt(uii);
            UIs.Insert(uii - dir, ui);

            int li = LayerList.Elements.IndexOf(panel);
            LayerList.Elements.RemoveAt(li);
            LayerList.Elements.Insert(li - dir, panel);

#if DEBUG

            for (int i = 0; i < UIs.Count; i++)
            {
                ui = UIs[i];
                System.Diagnostics.Debug.Assert(ui.Layer == Main.Layers[Main.Layers.Count - 1 - i]);
                System.Diagnostics.Debug.Assert(ui.Panel == LayerList.Elements[(LayerList.Elements.FirstOrDefault() is UIScrollBar ? i + 1 : i)]);
            }

#endif

            UpdateUpDowns();
        }

        static void UpdateUpDowns() 
        {
            for (int i = 0; i < UIs.Count; i++)
            {
                LayerUI ui = UIs[i];
                ui.Up.Enabled = i > 0;
                ui.Down.Enabled = i < UIs.Count - 1;
            }
            LayerList.Recalculate();
        }

        record LayerUI(Layer Layer, UIPanel Panel, UIButton Up, UIButton Down);
    }
}
