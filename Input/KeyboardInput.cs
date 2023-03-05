using Cornifer;
using Microsoft.Xna.Framework.Input;

namespace Cornifer.Input
{
    public class KeyboardInput : KeybindInput
    {
        public Keys Key { get; set; }

        public override bool CurrentState => !InputHandler.DisableInputs && InputHandler.KeyboardState.IsKeyDown(Key);
        public override bool OldState => !InputHandler.DisableInputs && InputHandler.OldKeyboardState.IsKeyDown(Key);
        public override string KeyName => Key.ToString();

        public KeyboardInput(Keys key)
        {
            Key = key;
        }
    }
}
