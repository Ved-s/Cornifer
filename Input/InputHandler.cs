using Cornifer.UI.Elements;
using Cornifer.UI.Modals;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace Cornifer.Input
{
    public static class InputHandler
    {
        public static KeyboardState KeyboardState;
        public static KeyboardState OldKeyboardState;

        public static MouseState MouseState;
        public static MouseState OldMouseState;

        public static bool DisableInputs => KeybindSelector.ModalVisible;

        public static Keys[] AllKeys = Enum.GetValues<Keys>();
        public static MouseKeys[] AllMouseKeys = Enum.GetValues<MouseKeys>();

        public static Dictionary<string, Keybind> Keybinds = new();
        public static string OldKeybindsFile => Path.Combine(Main.MainDir, "keybinds.txt");

        public static Keybind ReinitUI = new("", Keys.F12);
        public static Keybind TimingsDebug = new("", Keys.F10);
        public static Keybind UndoDebug = new("", Keys.F8);
        public static Keybind ModsDebug = new("", Keys.F7);
        public static Keybind ClearErrors = new("Clear errors", Keys.Escape);

        public static Keybind MoveMultiplier = new("Move multiplier", ModifierKeys.Shift);
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

        public static Keybind SelectAll = new("Select all", ModifierKeys.Control, Keys.A);
        public static Keybind Cut = new("Cut", ModifierKeys.Control, Keys.X);
        public static Keybind Copy = new("Copy", ModifierKeys.Control, Keys.C);
        public static Keybind Paste = new("Paste", ModifierKeys.Control, Keys.V);

        public static Keybind CopyImage = new("Copy as image", ModifierKeys.Control, ModifierKeys.Shift, Keys.C);

        public static void Init()
        {
            foreach (FieldInfo field in typeof(InputHandler).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!field.FieldType.IsAssignableTo(typeof(Keybind)))
                    continue;

                Keybinds[field.Name] = (Keybind)field.GetValue(null)!;
            }

            string oldKeybindsFile = OldKeybindsFile;
            if (File.Exists(oldKeybindsFile))
            {
                LoadOldKeybinds();
                SaveKeybinds();
                File.Delete(oldKeybindsFile);
            }
            else
            {
                if (Profile.Current.Keybinds is null)
                    SaveKeybinds();
                else
                    LoadKeybinds();
            }
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
            Profile.Current.Keybinds ??= new();
            Profile.Current.Keybinds.Clear();

            foreach (var (name, keybind) in Keybinds)
            {
                if (keybind.Inputs.Count == 0)
                {
                    Profile.Keybind keybindInfo = new()
                    {
                        Name = name,
                        Keys = Array.Empty<string>()
                    };
                    Profile.Current.Keybinds.Add(keybindInfo);
                    continue;
                }

                foreach (List<KeybindInput> keyCombo in keybind.Inputs)
                {
                    Profile.Keybind keybindInfo = new()
                    {
                        Name = name,
                        Keys = keyCombo.Select(k => k.KeyName).ToArray()
                    };
                    Profile.Current.Keybinds.Add(keybindInfo);
                }
            }

            Profile.Save();
        }

        public static void LoadKeybinds()
        {
            if (Profile.Current.Keybinds is null)
                return;

            HashSet<string> keybindsReset = new();

            foreach (var keybindInfo in Profile.Current.Keybinds)
            {
                if (!Keybinds.TryGetValue(keybindInfo.Name, out Keybind? keybind))
                    continue;

                if (!keybindsReset.Contains(keybindInfo.Name))
                {
                    keybind.Inputs.Clear();
                    keybindsReset.Add(keybindInfo.Name);
                }
                if (keybindInfo.Keys.Length == 0)
                    continue;

                List<KeybindInput> inputs = new();
                // Convert the key strings to inputs and add them to the dictionary
                foreach (string keyString in keybindInfo.Keys)
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

        public static void LoadOldKeybinds()
        {
            HashSet<string> keybindsReset = new();

            try
            {
                string[] lines = File.ReadAllLines(OldKeybindsFile);
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
    }
}
