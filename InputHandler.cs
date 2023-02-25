using Cornifer.UI.Elements;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Cornifer
{
    public static class InputHandler
    {
        public static KeyboardState KeyboardState;
        public static KeyboardState OldKeyboardState;

        public static MouseState MouseState;
        public static MouseState OldMouseState;

        public static bool DisableInputs => Interface.KeybindSelectorVisible;

        public static Keys[] AllKeys = Enum.GetValues<Keys>();
        public static MouseKeys[] AllMouseKeys = Enum.GetValues<MouseKeys>();

        public static Dictionary<string, Keybind> Keybinds = new();
        const string KeybindsFile = "keybinds.txt";

        public static Keybind ReinitUI = new("", Keys.F12);
        public static Keybind ClearErrors = new("Clear errors", Keys.Escape);
        public static Keybind MoveMultiplier = new("Move multiplier", ModifierKeys.Shift);
        public static Keybind UndoDebug = new("", Keys.F8);
        public static Keybind MoveUp = new("Move up", Keys.Up);
        public static Keybind MoveDown = new("Move down", Keys.Down);
        public static Keybind MoveLeft = new("Move left", Keys.Left);
        public static Keybind MoveRight = new("Move right", Keys.Right);
        public static Keybind DeleteObject = new("Delete object", new KeybindInput[] { Keys.Delete }, new KeybindInput[] { Keys.Back });
        public static Keybind DeleteConnection = new("Delete connection point", new KeybindInput[] { Keys.Delete }, new KeybindInput[] { Keys.Back });
        public static Keybind NewConnectionPoint = new("Create connection point", MouseKeys.LeftButton);

        public static Keybind Pan = new("Pan", MouseKeys.RightButton);
        public static Keybind Drag = new("Drag", MouseKeys.LeftButton);
        public static Keybind Select = new("Select", MouseKeys.LeftButton);

        public static Keybind AddToSelection = new("Add to selection", ModifierKeys.Shift);
        public static Keybind SubFromSelection = new("Sub from selection", ModifierKeys.Control);

        public static Keybind Undo = new("Undo", ModifierKeys.Control, Keys.Z);
        public static Keybind Redo = new("Redo", ModifierKeys.Control, Keys.Y);

        public static void Init()
        {
            foreach (FieldInfo field in typeof(InputHandler).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!field.FieldType.IsAssignableTo(typeof(Keybind)))
                    continue;

                Keybinds[field.Name] = (Keybind)field.GetValue(null)!;
            }

            if (!File.Exists(KeybindsFile))
                SaveKeybinds();
            else
                LoadKeybinds();
        }

        public static void Update()
        {
            OldMouseState = MouseState;
            MouseState = Mouse.GetState();

            OldKeyboardState = KeyboardState;
            KeyboardState = Keyboard.GetState();
        }

        public static void SaveKeybinds()
        {
            using FileStream fs = File.Create(KeybindsFile);
            using StreamWriter writer = new(fs);

            writer.WriteLine("// Cornifer keybindings file");

            writer.WriteLine("//");
            writer.WriteLine($"// Keyboard key names: \n//    {string.Join(", ", Enum.GetNames<Keys>().Skip(1))}");

            writer.WriteLine("//");
            writer.WriteLine($"// Mouse key names: \n//    {string.Join(", ", Enum.GetNames<MouseKeys>())}");

            writer.WriteLine("//");
            writer.WriteLine($"// Modifier key names: \n//    {string.Join(", ", Enum.GetNames<ModifierKeys>())}");

            writer.WriteLine();

            foreach (var (name, keybind) in Keybinds)
            {
                if (keybind.Inputs.Count == 0)
                {
                    writer.Write(name);
                    writer.Write('=');
                    writer.WriteLine();
                    continue;
                }

                foreach (List<KeybindInput> keyCombo in keybind.Inputs)
                {
                    writer.Write(name);
                    writer.Write('=');
                    for (int i = 0; i < keyCombo.Count; i++)
                    {
                        if (i > 0)
                            writer.Write("&");
                        writer.Write(keyCombo[i].KeyName);
                    }
                    writer.WriteLine();
                }
            }
        }
        public static void LoadKeybinds()
        {
            HashSet<string> keybindsReset = new();

            try
            {
                string[] lines = File.ReadAllLines(KeybindsFile);
                foreach (string line in lines)
                {
                    if (line.StartsWith("//"))
                        continue;

                    // Split the line into keybind name and key list parts
                    string[] parts = line.Split('=');
                    if (parts.Length != 2)
                    {
                        // Ignore the line if it doesn't contain exactly one equals sign
                        continue;
                    }
                    string keybindNameString = parts[0].Trim();
                    string[] keyStrings = parts[1].Split('&');

                    // Convert the input type string to an enum value
                    if (!Keybinds.TryGetValue(keybindNameString, out Keybind? keybind))
                    {
                        // Ignore the line if the keybind name string is not a valid name
                        continue;
                    }

                    if (!keybindsReset.Contains(keybindNameString))
                    {
                        keybind.Inputs.Clear();
                        keybindsReset.Add(keybindNameString);
                    }
                    if (parts[1].Length == 0)
                        continue;

                    List<KeybindInput> inputs = new();
                    // Convert the key strings to inputs and add them to the dictionary
                    foreach (string keyString in keyStrings)
                    {
                        string trimmedKey = keyString.Trim();

                        if (Enum.TryParse(trimmedKey, out ModifierKeys modifierKey))
                        {
                            inputs.Add(modifierKey);
                        }

                        if (Enum.TryParse(trimmedKey, out MouseKeys mouseKey))
                        {
                            inputs.Add(mouseKey);
                        }

                        else if (Enum.TryParse(trimmedKey, out Keys key))
                        {
                            inputs.Add(key);
                        }
                    }
                    keybind.Inputs.Add(inputs);
                }
            }
            catch (Exception ex)
            {
                // Handle any file I/O errors by logging an error message and returning an empty list
                Debug.WriteLine($"Error loading key bindings from file: {ex.Message}");
            }
        }

        public static ModifierKeys? GetKeyModifiers(Keys key)
        {
            return key switch
            {
                Keys.LeftShift => ModifierKeys.Shift,
                Keys.RightShift => ModifierKeys.Shift,
                Keys.LeftControl => ModifierKeys.Control,
                Keys.RightControl => ModifierKeys.Control,
                Keys.LeftAlt => ModifierKeys.Alt,
                Keys.RightAlt => ModifierKeys.Alt,
                Keys.LeftWindows => ModifierKeys.Windows,
                Keys.RightWindows => ModifierKeys.Windows,
                _ => null
            };
        }

        public enum MouseKeys
        {
            LeftButton,
            RightButton,
            MiddleButton,
            XButton1,
            XButton2
        }

        public enum ModifierKeys
        {
            Shift,
            Control,
            Alt,
            Windows
        }

        public enum KeybindState
        {
            Released = 0,
            JustReleased = 1,
            Pressed = 3,
            JustPressed = 2,
        }

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

        public class KeyboardInput : KeybindInput
        {
            public Keys Key { get; set; }

            public override bool CurrentState => !DisableInputs && KeyboardState.IsKeyDown(Key);
            public override bool OldState => !DisableInputs && OldKeyboardState.IsKeyDown(Key);
            public override string KeyName => Key.ToString();

            public KeyboardInput(Keys key)
            {
                Key = key;
            }
        }
        public class ModifierInput : KeybindInput
        {
            public ModifierKeys Key { get; set; }

            public override bool CurrentState => GetModifierState(KeyboardState);
            public override bool OldState => GetModifierState(OldKeyboardState);
            public override string KeyName => Key.ToString();

            public ModifierInput(ModifierKeys key)
            {
                Key = key;
            }

            bool GetModifierState(KeyboardState state)
            {
                if (DisableInputs)
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
        public class MouseInput : KeybindInput
        {
            public MouseKeys Key { get; set; }

            public override bool CurrentState => GetMouseInput(MouseState);
            public override bool OldState => GetMouseInput(OldMouseState);
            public override string KeyName => Key.ToString();

            public MouseInput(MouseKeys key)
            {
                Key = key;
            }

            bool GetMouseInput(MouseState state)
            {
                if (DisableInputs)
                    return false;

                return Key switch
                {
                    MouseKeys.LeftButton => state.LeftButton == ButtonState.Pressed,
                    MouseKeys.RightButton => state.RightButton == ButtonState.Pressed,
                    MouseKeys.MiddleButton => state.MiddleButton == ButtonState.Pressed,
                    MouseKeys.XButton1 => state.XButton1 == ButtonState.Pressed,
                    MouseKeys.XButton2 => state.XButton2 == ButtonState.Pressed,
                    _ => false,
                };
            }
        }
    }
}
