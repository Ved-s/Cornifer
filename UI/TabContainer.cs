using Cornifer.UI.Elements;
using Cornifer.UI.Helpers;
using Cornifer.UI.Structures;

namespace Cornifer.UI
{
    public class TabContainer : UIContainer, ITabController<TabContainer.Tab>, IElementListController, ILayoutContainer
    {
        private readonly TabSelector TabSelector;
        private bool ModifyingElements = false;
        private UIElement? CurrentTabElement;

        public bool CanDeselectTabs { get => TabSelector.CanDeselectTabs; set => TabSelector.CanDeselectTabs = value; }

        public TabCollection<Tab> Tabs { get; }

        public TabContainer()
        {
            Tabs = new(this);

            TabSelector = new()
            {
                Height = 0,
                CanDeselectTabs = false,
                EventOnTabAdd = true
            };
            TabSelector.OnEvent(TabSelector.TabSelectedEvent, (_, tab) => TabSelected(tab as Tab));

            ModifyingElements = true;
            Elements.Add(TabSelector);
            ModifyingElements = false;
        }

        void TabSelected(Tab? tab)
        {
            ModifyingElements = true;
            if (CurrentTabElement is not null)
                Elements.Remove(CurrentTabElement);

            CurrentTabElement = tab?.Element;

            if (CurrentTabElement is not null)
                Elements.Add(CurrentTabElement);

            ModifyingElements = false;
        }

        void ITabController<Tab>.AddTab(Tab tab)
        {
            TabSelector.Tabs.Add(tab);
        }

        void ITabController<Tab>.RemoveTab(Tab tab)
        {
            TabSelector.Tabs.Remove(tab);
        }

        void ITabController<Tab>.ClearTabs()
        {
            TabSelector.Tabs.Clear();
        }

        bool IElementListController.CanModifyElements() => ModifyingElements;

        void ILayoutContainer.LayoutChild(UIElement child, ref Rect screenRect)
        {
            if (child != CurrentTabElement)
                return;

            float pad = TabSelector.ScreenRect.Height + 5;
            screenRect.Y += pad;
            if (screenRect.Height > ScreenRect.Height - pad)
                screenRect.Height = ScreenRect.Height - pad;
        }

        public class Tab : TabSelector.Tab
        {
            public UIElement? Element;
        }
    }
}
