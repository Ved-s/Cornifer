using Cornifer.UI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Cornifer.UI.Pages
{
    public abstract class Page : UIPanel
    {
        public virtual string PageName => GetType().Name;
        public virtual int Order { get; } = 0;
        public virtual bool IsDebugOnly { get; } = false;

        static List<Type>? PageTypes;

        public Page() 
        {
            BackColor = new(30, 30, 30);
            BorderColor = new(100, 100, 100);
            Padding = new(5);
        }

        public static void CreatePages(TabContainer tabs)
        {
            if (PageTypes is null)
            {
                PageTypes = new();

                foreach (Type type in Assembly.GetExecutingAssembly().GetExportedTypes())
                {
                    if (type.IsAbstract || !type.IsAssignableTo(typeof(Page)))
                        continue;

                    PageTypes.Add(type);
                }
            }

            IEnumerable<TabContainer.Tab> newtabs = PageTypes
                .Select(ty => ty.CreateInstance<Page>())
                .Where(p => !p.IsDebugOnly || Main.DebugMode)
                .OrderBy(p => p.Order)
                .Select(p => new TabContainer.Tab() { Name = p.PageName, Element = p });

            foreach (TabContainer.Tab tab in newtabs)
                tabs.Tabs.Add(tab);
        }
    }
}
