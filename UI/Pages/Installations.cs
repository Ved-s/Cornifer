using Cornifer.Structures;
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
    public class Installations : Page
    {
        public static UIList InstallsList = null!;
        public static List<(RainWorldInstallation, UIHoverPanel)> InstallsPanels = new();

        public override int Order => 6;

        public Installations()
        {
            Elements = new(this)
            {
                new UIList
                {
                    Height = 0,
                    AutoSize = true,
                    ElementSpacing = 4,

                    Elements =
                    {
                        new UILabel
                            {
                                Text = "Rain World installations",
                                TextAlign = new(.5f),
                                Height = 0,
                            },
                        new UIList
                        {
                            Height = 0,
                            AutoSize = true,
                            ElementSpacing = 4,
                        }.Assign(out InstallsList)
                        .Execute(_ => PopulateInstallations()),
                        new UIButton
                        {
                            Height = 25,
                            TextAlign = new(.5f),
                            Text = "Add installation"
                        }.OnEvent(UIElement.ClickEvent, async (_, _) =>
                        {
                            RainWorldInstallation? install = await InstallationSelection.ShowDialog();
                            if (install is null)
                                return;

                            RWAssets.AddInstallation(install);
                        })
                    }
                }
            };
        }

        public static void PopulateInstallations()
        {
            if (InstallsList is null)
                return;

            InstallsPanels.Clear();
            InstallsList.Elements.Clear();

            foreach (RainWorldInstallation install in RWAssets.Installations)
            {
                RainWorldInstallation inst = install;
                UIHoverPanel panel = new()
                {
                    Padding = 4,
                    Height = 60,

                    Elements =
                    {
                        new UIButton()
                        {
                            Visible = inst.CanSave,
                            Width = 18,
                            Height = 18,
                            Top = new(0, 1, -1),
                            Left = new(0, 1, -1),
                            AutoSize = false,
                            Text = "D",
                        }.OnEvent(UIElement.ClickEvent, (_, _) => RWAssets.RemoveInstallation(inst)),
                        new UILabel
                        {
                            Top = 0,
                            Height = 20,
                            Text = install.Name,
                            AutoSize = false,
                            WordWrap = false,
                        },
                        new UILabel
                        {
                            Top = 20,
                            Height = 20,
                            Text = install.Path,
                            TextAlign = new(1, 0),
                            MaxWidth = new(0, 1),
                            Width = 0,
                            AutoSize = true,
                            WordWrap = false,
                        },
                        new UILabel
                        {
                            Top = 40,
                            Height = 20,
                            Text = install.GetFeaturesString(),
                            AutoSize = false,
                            WordWrap = false,
                        }
                    }
                };

                panel.OnEvent(UIElement.ClickEvent, (panel, _) =>
                {
                    if (inst == RWAssets.CurrentInstallation || panel.Root?.Hover is IHoverable or null)
                        return;

                    RWAssets.SetActiveInstallation(inst);
                });

                InstallsList.Elements.Add(panel);
                InstallsPanels.Add((install, panel));
            }

            ActiveInstallChanged();

            InstallsList.Recalculate();
        }

        public static void ActiveInstallChanged()
        {
            foreach (var (install, panel) in InstallsPanels)
            {
                if (install == RWAssets.CurrentInstallation)
                {
                    panel.BorderColor = Color.Lime;
                    panel.HoverBackColor = panel.BackColor;
                    panel.HoverBorderColor = Color.Lime;
                }
                else
                {
                    panel.BorderColor = new(100, 100, 100);
                    panel.HoverBackColor = new(.3f, .3f, .3f);
                    panel.HoverBorderColor = Color.Green;
                }
            }
        }
    }
}
