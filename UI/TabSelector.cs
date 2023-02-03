using Cornifer.UI.Elements;
using Cornifer.UI.Helpers;
using Microsoft.Xna.Framework;
using System.Linq;

namespace Cornifer.UI
{
    public class TabSelector : UIFlow, IElementListController, ITabController<TabSelector.Tab>
    {
        public static readonly ElementEvent<Tab?, TabSelector> TabSelectedEvent = new();

        private bool ModifyingElements = false;
        private RadioButtonGroup RadioGroup = new();
        private bool canDeselectTabs = true;
        private bool eventOnTabAdd;

        public TabCollection<Tab> Tabs { get; }
        public bool EventOnTabAdd
        {
            get => eventOnTabAdd;
            set { eventOnTabAdd = value; RadioGroup.TriggerOnButtonAdd = value; }
        }

        public bool CanDeselectTabs
        {
            get => canDeselectTabs;
            set
            {
                canDeselectTabs = value;
                foreach (UIElement e in Elements)
                    if (e is UIButton btn && btn.RadioGroup is not null)
                        btn.CanDeselect = value;
            }
        }

        public TabSelector()
        {
            Height = 0;
            ElementSpacing = 2;

            Tabs = new(this);

            RadioGroup.ButtonClicked += GroupButtonClicked;
        }

        private void GroupButtonClicked(UIButton? button, object? tag)
        {
            Events.Call(TabSelectedEvent, tag as Tab);
        }

        void ITabController<Tab>.AddTab(Tab tab)
        {
            UIButton btn = new()
            {
                Width = 0,
                Height = 0,
                Text = tab.Name,
                RadioTag = tab,
                RadioGroup = RadioGroup,

                CanDeselect = CanDeselectTabs,
                SelectedTextColor = new(.1f, .1f, .1f),
                SelectedBackColor = Color.White,
            };
            btn.Selected = tab.Selected || !CanDeselectTabs && !Elements.Any(e => e is UIButton);
            ModifyingElements = true;
            Elements.Add(btn);
            ModifyingElements = false;
            tab.Button = btn;
        }

        void ITabController<Tab>.RemoveTab(Tab tab)
        {
            if (tab.Button is null)
                return;

            ModifyingElements = true;

            tab.Button.RadioGroup = null;
            Elements.Remove(tab.Button);

            ModifyingElements = false;
        }

        void ITabController<Tab>.ClearTabs()
        {
            ModifyingElements = true;
            Elements.Clear();
            RadioGroup.ButtonClicked -= GroupButtonClicked;
            RadioGroup = new();
            RadioGroup.TriggerOnButtonAdd = EventOnTabAdd;
            RadioGroup.ButtonClicked += GroupButtonClicked;
            ModifyingElements = false;
        }

        bool IElementListController.CanModifyElements() => ModifyingElements;

        public class Tab : UI.Tab
        {
            public override bool Selected
            {
                get => selected;
                set
                {
                    selected = value;
                    if (Button is not null)
                        Button.Selected = value;
                }
            }

            internal UIButton? Button;
            private bool selected;

            public Tab() { }
            public Tab(string name, bool selected, object? tag)
            {
                Name = name;
                Selected = selected;
                Tag = tag;
            }
        }

    }
}
