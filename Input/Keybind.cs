using System.Collections.Generic;

namespace Cornifer.Input
{
    public class Keybind
    {
        public string Name { get; }

        // KeyA & KeyB, KeyA & KeyC
        public List<List<KeybindInput>> Inputs = new();

        public KeybindState State
        {
            get
            {
                KeybindState state = KeybindState.Released;

                foreach (List<KeybindInput> combo in Inputs)
                {
                    KeybindState comboState = GetComboState(combo);
                    if (comboState == KeybindState.Pressed)
                        return KeybindState.Pressed;

                    if (comboState == KeybindState.JustReleased && state < KeybindState.JustReleased)
                        state = KeybindState.JustReleased;

                    if (comboState == KeybindState.JustPressed && state < KeybindState.JustPressed)
                        state = KeybindState.JustPressed;
                }

                return state;
            }
        }

        public bool Released => State == KeybindState.Released;
        public bool JustReleased => State == KeybindState.JustReleased;
        public bool JustPressed => State == KeybindState.JustPressed;
        public bool Pressed => State == KeybindState.Pressed;

        public bool AnyKeyPressed
        {
            get
            {
                foreach (List<KeybindInput> combo in Inputs)
                    foreach (KeybindInput input in combo)
                        if (input.CurrentState)
                            return true;
                return false;
            }
        }
        public bool AnyOldKeyPressed
        {
            get
            {
                foreach (List<KeybindInput> combo in Inputs)
                    foreach (KeybindInput input in combo)
                        if (input.OldState)
                            return true;
                return false;
            }
        }

        KeybindState GetComboState(List<KeybindInput> inputs)
        {
            KeybindState state = KeybindState.Pressed;

            foreach (KeybindInput input in inputs)
            {
                KeybindState keyState = input.State;
                if (keyState == KeybindState.Released)
                    return KeybindState.Released;

                if (keyState == KeybindState.JustReleased && state >= KeybindState.JustPressed)
                    state = KeybindState.JustReleased;

                if (keyState == KeybindState.JustPressed && state > KeybindState.JustPressed)
                    state = KeybindState.JustPressed;
            }

            return state;
        }

        public Keybind(string name, IEnumerable<KeybindInput> defaults)
        {
            Name = name;
            Inputs.Add(new(defaults));
        }

        public Keybind(string name, IEnumerable<IEnumerable<KeybindInput>> defaults)
        {
            Name = name;

            foreach (var keyCombo in defaults)
                Inputs.Add(new(keyCombo));
        }

        public Keybind(string name, params KeybindInput[][] @default) : this(name, (IEnumerable<KeybindInput[]>)@default) { }

        public Keybind(string name, params KeybindInput[] @default) : this(name, (IEnumerable<KeybindInput>)@default) { }
    }
}
