using Cornifer.Input;
using Cornifer.UI.Elements;
using Microsoft.Xna.Framework.Input;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;

namespace Cornifer.UI.Modals
{
    public class KeybindSelector : Modal<KeybindSelector, List<KeybindInput>?>
    {
        List<KeybindInput> Inputs = new();
        UILabel CurrentKeybindInputs = null!;
        UILabel CurrentKeybindName = null!;
        bool UpdateSkipped = false;

        public KeybindSelector()
        {
            Width = 450;
            Height = 300;

            Margin = 5;
            Padding = 5;

            Elements = new(this)
            {
                new UILabel
                {
                    Text = "Adding keybind for",
                    Height = 18,
                    TextAlign = new(.5f),
                },
                new UILabel
                {
                    Height = 18,
                    Top = 18,
                    TextAlign = new(.5f),
                }.Assign(out CurrentKeybindName),
                new UILabel
                {
                    Height = 0,
                    Top = new(0, .5f, -.5f),
                    TextAlign = new(.5f),

                }.Assign(out CurrentKeybindInputs),
                new UILabel
                {
                    Text = "Press any key to add or remove it.\nPress modifier key twice to select any of its modifier keys\n(LeftControl -> Control).",
                    Height = 50,
                    Top = new(-20, 1, -1),
                    TextAlign = new(.5f, 1),
                },
                new UIButton
                {
                    Width = 80,
                    Height = 20,
                    Text = "Apply",
                    TextAlign = new(.5f),
                    Left = new(-2, .5f, -1),
                    Top = new(0, 1, -1),
                }.OnEvent(ClickEvent, (_, _) =>
                {
                    if (Inputs.Count > 0)
                    {
                        ReturnResult(Inputs);
                        Inputs = new();
                    }
                }),
                new UIButton
                {
                    Width = 80,
                    Height = 20,
                    Text = "Cancel",
                    TextAlign = new(.5f),
                    Left = new(2, .5f),
                    Top = new(0, 1, -1),
                }.OnEvent(ClickEvent, (_, _) =>
                {
                    ReturnResult(null);
                })
            };
        }

        public static void Show(Keybind keybind)
        {
            Instance ??= new();
            Instance.CurrentKeybindName.Text = keybind.Name;
            Show();
        }

        protected override void Shown()
        {
            CurrentKeybindInputs.Text = "None";
            Inputs.Clear();
            UpdateSkipped = false;
        }

        protected override void Hidden()
        {
            Inputs.Clear();
            UpdateSkipped = false;
        }

        protected override void UpdateSelf()
        {
            base.UpdateSelf();

            if (Main.Instance.IsActive && UpdateSkipped)
            {
                bool keysChanged = false;

                foreach (Keys key in InputHandler.AllKeys)
                {
                    if (!InputHandler.KeyboardState.IsKeyDown(key) || !InputHandler.OldKeyboardState.IsKeyUp(key))
                        continue;

                    ModifierKeys? modifier = InputHandler.GetKeyModifiers(key);
                    bool hadKey = Inputs.RemoveAll(ki => ki is KeyboardInput keyboardInput && keyboardInput.Key == key) > 0;
                    bool hadMod = modifier.HasValue && Inputs.RemoveAll(ki => ki is ModifierInput modifierInput && modifierInput.Key == modifier) > 0;

                    // None => Key => Possible modifier => None

                    if (!hadKey && !hadMod)
                    {
                        Inputs.Add(new KeyboardInput(key));
                    }
                    else if (hadKey && modifier.HasValue)
                    {
                        Inputs.Add(new ModifierInput(modifier.Value));
                    }

                    keysChanged = true;
                }

                foreach (MouseKeys key in InputHandler.AllMouseKeys)
                {
                    if (!InputHandler.MouseState.IsKeyDown(key) || !InputHandler.OldMouseState.IsKeyUp(key))
                        continue;

                    bool hadKey = Inputs.RemoveAll(ki => ki is MouseInput mouseInput && mouseInput.Key == key) > 0;
                    if (!hadKey)
                        Inputs.Add(new MouseInput(key));

                    keysChanged = true;
                }

                if (keysChanged)
                {
                    CurrentKeybindInputs.Text = Inputs.Count == 0 ? "None" : string.Join(" + ", Inputs.Select(ki => ki.KeyName));
                }
            }
            UpdateSkipped = true;
        }
    }
}
