using Cornifer.Input;
using Cornifer.UI.Elements;
using Cornifer.UI.Modals;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Pages
{
    public class Keybinds : Page
    {
        public static UIList KeybindsList = null!;

        public override int Order => 5;

        public Keybinds()
        {
            Elements = new(this)
            {
                new UIList
                {
                    ElementSpacing = 2,
                }.Assign(out KeybindsList)
                .Execute(list =>
                {
                    foreach (Keybind keybind in InputHandler.Keybinds.Values)
                    {
                        if (keybind.Name.Length == 0)
                            continue;

                        list.Elements.Add(new UILabel
                        {
                            Text = keybind.Name,
                            AutoSize = true,
                            Height = 0,
                            TextAlign = new(.5f)
                        });

                        UIList combos = new()
                        {
                            ElementSpacing = 4,
                            AutoSize = true,
                            Height = 0,
                        };

                        foreach (List<KeybindInput> inputs in keybind.Inputs)
                            AddKeyComboPanel(list, keybind, inputs);

                        list.Elements.Add(combos);
                        list.Elements.Add(new UIButton
                        {
                            Text = "Add keybind",
                            TextAlign = new(.5f),
                            Height = 20
                        }.OnEvent(UIElement.ClickEvent, async (_, _) =>
                        {
                            await KeybindSelector.Show(keybind);
                            List<KeybindInput>? inputs = await KeybindSelector.Task;
                            if (inputs is null)
                                return;

                            keybind.Inputs.Add(inputs);
                            AddKeyComboPanel(combos, keybind, inputs);
                            InputHandler.SaveKeybinds();
                            KeybindsList.Recalculate();
                        }));
                        list.Elements.Add(new UIElement { Height = 10 });
                    }
                })
            };
        }

        static void AddKeyComboPanel(UIList list, Keybind keybind, List<KeybindInput> inputs)
        {
            UIPanel panel = new()
            {
                Height = 18,

                BackColor = Color.Transparent,
                BorderColor = Color.Transparent,

                Elements =
                {
                    new UIPanel
                    {
                        Height = 18,
                        Width = new(-20, 1),
                        BackColor = new(48, 48, 48),
                        BorderColor = new(100, 100, 100),

                        Elements =
                        {
                            new UILabel
                            {
                                Left = 3,
                                Top = 1,
                                Width = new(-3, 1),
                                Text = string.Join(" + ", inputs.Select(i => i.KeyName)),
                                TextAlign = new(.5f),
                                AutoSize = false,
                            },
                        }
                    },
                }
            };

            panel.Elements.Add(new UIButton
            {
                Text = "D",
                Height = 18,
                Width = 18,
                Left = new(0, 1, -1),
            }.OnEvent(UIElement.ClickEvent, (_, _) =>
            {
                list.Elements.Remove(panel);
                keybind.Inputs.Remove(inputs);
                list.Recalculate();
                InputHandler.SaveKeybinds();
            }));

            list.Elements.Add(panel);
        }
    }
}
