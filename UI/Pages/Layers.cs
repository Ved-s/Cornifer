using Cornifer.MapObjects;
using Cornifer.UI.Elements;
using Cornifer.UI.Modals;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Cornifer.Capture.PSD.PSDFile;

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
                new UIButton 
                {
                    Text = "Add layer",
                    TextAlign = new(.5f),
                    Height = 25,
                }.OnClick(_ => AddLayer()),
                new UIList
                {
                    Top = 30,
                    Height = new(-30, 1),
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
                LayerUI ui = CreateLayerUI(layer);
                UIs.Add(ui);

                LayerList.Elements.Add(ui.Panel);
            }
            UpdateUpDowns();
        }

        static async void AddLayer()
        {
            var res = await StringEdit.ShowDialog("New layer name", "");
            if (res.Cancel)
                return;

            string id = $"u_{Random.Shared.Next():x}";
            while (Main.Layers.Any(l => l.Id == id))
                id = $"u_{Random.Shared.Next():x}";

            Layer layer = new(id, res.String, false);
            LayerUI ui = CreateLayerUI(layer);

            Main.Layers.Add(layer);
            UIs.Insert(0, ui);

            int firstPanel = LayerList.Elements.Count;
            for (int i = 0; i < LayerList.Elements.Count; i++)
                if (LayerList.Elements[i] is UIPanel)
                {
                    firstPanel = i;
                    break;
                }

            LayerList.Elements.Insert(firstPanel, ui.Panel);
            UpdateUpDowns();
        }
        static async void RemoveLayer(Layer layer, UIPanel panel) 
        {
            HashSet<MapObject> layerObjects = new(Main.EnumerateAllObjects().Where(o => o.RenderLayer.Value == layer));

            if (layerObjects.Count > 0)
            {
                HashSet<MapObject> diff = new(Main.WorldObjects);
                diff.IntersectWith(layerObjects);

                if (diff.Count == layerObjects.Count)
                {
                    if (await MessageBox.Show("Layer contains objects. Delete them?", MessageBox.ButtonsOkCancel) == 0)
                        return;

                    Main.WorldObjects.RemoveAll(o => diff.Contains(o));
                }
                else 
                {
                    await MessageBox.Show("Layer contains objects that cannot be deleted.\nDelete them or move them into other layers then retry.", MessageBox.ButtonsOk);
                    return;
                }
            }

            Main.Layers.Remove(layer);
            UIs.RemoveAll(ui => ui.Layer == layer);
            LayerList.Elements.Remove(panel);

            UpdateUpDowns();
        }
        static async void RenameLayer(Layer layer, UILabel label) 
        {
            var res = await StringEdit.ShowDialog("Layer name", layer.Name);
            if (res.Cancel)
                return;

            layer.Name = res.String;
            label.Text = res.String;
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
                System.Diagnostics.Debug.Assert(ui.Panel == LayerList.Elements.Where(e => e is not UIScrollBar).ElementAt(i));
            }

#endif

            UpdateUpDowns();
        }

        static LayerUI CreateLayerUI(Layer layer)
        {
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
                    }.Assign(out UIButton up),
                    new UIButton
                    {
                        Text = "˅",
                        Top = new(0, 1, -1),
                        Left = new(0, 1, -1),
                        Width = 19,
                        Height = 19,
                        Padding = new(7, 6, 0, 0),
                    }.Assign(out UIButton down),
                    new UIPanel
                    {
                        Width = new(-20, 1),
                        BorderColor = new(100, 100, 100),

                        Elements =
                        {
                            new UILabel
                            {
                                Text = layer.Name,
                                Height = 20,
                                AutoSize = false,
                                Margin = new(2, 4, 0, 0),
                                TextAlign = new(0, .5f),

                                Width = new(-20, 1)
                            }.Assign(out UILabel name),

                            new UIButton
                            {
                                Text = "V",
                                Selectable = true,
                                Selected = layer.Visible,

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                                Top = new(-2, 1, -1),
                                Left = 2,
                                Width = 17,
                                Height = 17,
                                TextAlign = new(.5f),
                                HoverText = "Layer visibility"
                            }.OnClick(b => layer.Visible = b.Selected),
                        }
                    }.Assign(out UIPanel inner)
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
                    HoverText = "Delete layer"
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
                    HoverText = "Rename layer"
                }.OnClick(_ =>
                {
                    RenameLayer(layer, name);
                }));
            }

            LayerUI ui = new(layer, panel, up, down);

            up.OnClick(_ => MoveLayer(layer, panel, true));
            down.OnClick(_ => MoveLayer(layer, panel, false));

            return ui;
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
