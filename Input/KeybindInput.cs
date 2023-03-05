using Microsoft.Xna.Framework.Input;

namespace Cornifer.Input
{
    public abstract class KeybindInput
    {
        public abstract bool CurrentState { get; }
        public abstract bool OldState { get; }

        public abstract string KeyName { get; }

        public KeybindState State => (KeybindState)((CurrentState ? 1 : 0) << 1 | (OldState ? 1 : 0));

        public static implicit operator KeybindInput(Keys key) => new KeyboardInput(key);
        public static implicit operator KeybindInput(ModifierKeys key) => new ModifierInput(key);
        public static implicit operator KeybindInput(MouseKeys key) => new MouseInput(key);
    }
}
