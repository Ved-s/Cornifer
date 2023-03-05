using Microsoft.Xna.Framework.Input;

namespace Cornifer.Input
{
    public class ModifierInput : KeybindInput
    {
        public ModifierKeys Key { get; set; }

        public override bool CurrentState => GetModifierState(InputHandler.KeyboardState);
        public override bool OldState => GetModifierState(InputHandler.OldKeyboardState);
        public override string KeyName => Key.ToString();

        public ModifierInput(ModifierKeys key)
        {
            Key = key;
        }

        bool GetModifierState(KeyboardState state)
        {
            if (InputHandler.DisableInputs)
                return false;

            return Key switch
            {
                ModifierKeys.Shift => state.IsKeyDown(Keys.LeftShift) || state.IsKeyDown(Keys.RightShift),
                ModifierKeys.Control => state.IsKeyDown(Keys.LeftControl) || state.IsKeyDown(Keys.RightControl),
                ModifierKeys.Alt => state.IsKeyDown(Keys.LeftAlt) || state.IsKeyDown(Keys.RightAlt),
                ModifierKeys.Windows => state.IsKeyDown(Keys.LeftWindows) || state.IsKeyDown(Keys.RightWindows),
                _ => false
            };
        }
    }
}
