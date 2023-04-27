using Cornifer.Structures;
using Cornifer.UI.Elements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Modals
{
    public class InstallationSelection : Modal<InstallationSelection, RainWorldInstallation?>
    {
        RainWorldInstallation Installation = null!;

        UIInput InstallationName;
        UILabel FeaturesLabel;

        public InstallationSelection()
        {
            Height = 160;
            Width = 300;
            Padding = 4;

            Elements = new(this)
            {
                new UILabel
                {
                    Height = 0,
                    TextAlign = new(.5f),
                    Text = "Configure Rain World installation"
                },
                new UILabel
                {
                    Top = 30,
                    Height = 20,
                    TextAlign = new(.5f),
                    Text = "Installation name:"
                },
                new UIInput
                {
                    Top = 50,
                    Height = 20,
                    Left = new(0, .5f, -.5f),
                    Width = new(0, .6f)
                }.Assign(out InstallationName),
                new UILabel
                {
                    Top = 80,
                    Height = 0,
                    TextAlign = new(.5f),
                    Text = "Installation features:"
                },
                new UILabel
                {
                    Top = 97,
                    Height = 0,
                    TextAlign = new(.5f),
                    Text = "features"
                }.Assign(out FeaturesLabel),

                new UIButton
                {
                    Top = new(0, 1, -1),
                    Height = 20,
                    Width = 80,
                    Left = new(-2, .5f, -1),
                    TextAlign = new(.5f),
                    Text = "Apply",
                }.OnEvent(ClickEvent, (_, _) => 
                {
                    Installation.Name = InstallationName.Text;
                    ReturnResult(Installation);
                }),

                new UIButton
                {
                    Top = new(0, 1, -1),
                    Height = 20,
                    Width = 80,
                    Left = new(2, .5f),
                    TextAlign = new(.5f),
                    Text = "Cancel",
                }.OnEvent(ClickEvent, (_, _) =>
                {
                    ReturnResult(null);
                }),
            };
        }

        protected override void Shown()
        {
            InstallationName.Text = Installation.Name;
            FeaturesLabel.Text = Installation.GetFeaturesString();
        }

        public new static async Task<RainWorldInstallation?> ShowDialog()
        {
            string? rainWorld = await Platform.OpenFileDialog("Select Rain World executable", "Windows Executable|*.exe");
            if (rainWorld is null)
                return null;
            
            Instance ??= new();
            Instance.Installation = RainWorldInstallation.CreateFromPath(Path.GetDirectoryName(rainWorld)!);
            Instance.Installation.Name = "Custom";

            return await Modal<InstallationSelection, RainWorldInstallation?>.ShowDialog();
        }
    }
}
