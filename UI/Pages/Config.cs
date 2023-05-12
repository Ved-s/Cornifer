using Cornifer.MapObjects;
using Cornifer.UI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Cornifer.UI.Pages
{
    public class Config : Page
    {
        public static UILabel NoConfigObjectLabel = null!;
        public static UILabel NoConfigLabel = null!;
        public static Config ConfigPanel = null!;

        static UIElement? ConfigElement;
        static MapObject? configurableObject;

        public static MapObject? ConfigurableObject
        {
            get => configurableObject;
            set
            {
                if (ConfigElement is null)
                {
                    NoConfigObjectLabel.Visible = Main.SelectedObjects.Count != 1;
                    NoConfigLabel.Visible = Main.SelectedObjects.Count == 1;
                }
                else
                {
                    NoConfigObjectLabel.Visible = false;
                    NoConfigLabel.Visible = false;
                }

                if (ReferenceEquals(configurableObject, value))
                    return;

                configurableObject = value;

                if (ConfigElement is not null)
                    ConfigPanel.Elements.Remove(ConfigElement);

                ConfigElement = value?.Config;

                if (ConfigElement is not null)
                    ConfigPanel.Elements.Add(ConfigElement);
            }
        }

        public override int Order => 4;

        public Config()
        {
            ConfigPanel = this;

            Elements = new(this)
            {
                new UILabel
                {
                    Height = 20,
                    Text = "Select one object on the map to configure",
                    TextAlign = new(.5f),
                    Visible = ConfigurableObject is null && Main.SelectedObjects.Count != 1,
                }.Assign(out NoConfigObjectLabel),
                new UILabel
                {
                    Height = 20,
                    Text = "Selected object is not configurable",
                    TextAlign = new(.5f),
                    Visible = ConfigurableObject is null && Main.SelectedObjects.Count == 1
                }.Assign(out NoConfigLabel)
            };

            if (ConfigElement is not null)
                Elements.Add(ConfigElement);
        }

        internal static void Init()
        {
            if (ConfigurableObject is not null)
            {
                ConfigurableObject.ConfigCache = null;
            }

            if (ConfigElement is not null)
                ConfigPanel.Elements.Remove(ConfigElement);

            ConfigElement = configurableObject?.Config;
        }

        protected override void UpdateSelf()
        {
            base.UpdateSelf();

            if (Main.SelectedObjects.Count != 1)
                ConfigurableObject = null;
            else
                ConfigurableObject = Main.SelectedObjects.First();
        }
    }
}
