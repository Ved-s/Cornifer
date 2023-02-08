using Cornifer.UI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.Interfaces
{
    public interface IConfigurable
    {
        public UIElement? ConfigCache { get; set; }
        public UIElement Config
        {
            get => ConfigCache ??= BuildConfig();
        }

        UIElement BuildConfig();
    }
}
