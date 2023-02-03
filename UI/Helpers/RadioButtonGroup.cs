using Cornifer.UI.Elements;
using System.Collections.Generic;
using System.Linq;

namespace Cornifer.UI.Helpers
{
    public class RadioButtonGroup
    {
        public delegate void ButtonClickedDelegate(UIButton? button, object? buttonRadioTag);
        public ButtonClickedDelegate? ButtonClicked;

        public bool TriggerOnButtonAdd = false;

        internal List<UIButton> Buttons = new();

        internal bool AnySelectedButtons => Buttons.Count > 0 && Buttons.Any(b => b.Selected);

        internal void SelectButton(UIButton? button)
        {
            foreach (UIButton btn in Buttons)
            {
                btn.SelectedInternal = false;
            }
            if (button is not null)
                button.SelectedInternal = true;

            ButtonClicked?.Invoke(button, button?.RadioTag);
        }

        internal void AddButton(UIButton button)
        {
            Buttons.Add(button);
            if (button.Selected && !AnySelectedButtons && TriggerOnButtonAdd)
                ButtonClicked?.Invoke(button, button.RadioTag);
        }

        internal void RemoveButton(UIButton button)
        {
            Buttons.Remove(button);
            if (button.Selected && !AnySelectedButtons && TriggerOnButtonAdd)
                ButtonClicked?.Invoke(null, null);
        }
    }
}
