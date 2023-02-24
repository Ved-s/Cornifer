using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;

namespace Cornifer
{
    public class InputHandler
    {

        public InputHandler()
        {
            string path = "keybinds.txt";
            if (!File.Exists(path)) CreateDefaultFile(path);

            keyBindings = LoadKeyBindingsFromFile(path);
        }

        public void Update()
        {
            OldMouseState = MouseState;
            MouseState = Mouse.GetState();

            OldKeyboardState = KeyboardState;
            KeyboardState = Keyboard.GetState();
        }

        public struct InputState
        {
            public InputType type;
            public KeyboardState keyboardState;
            public MouseState mouseState;

            public InputState(InputType type, KeyboardState keyboardState, MouseState mouseState)
            {
                this.type = type;
                this.keyboardState = keyboardState;
                this.mouseState = mouseState;
            }
        }


        public static List<InputState> LoadKeyBindingsFromFile(string filepath)
        {
            List<InputState> keyBindings = new List<InputState>();
            try
            {
                string[] lines = File.ReadAllLines(filepath);
                foreach (string line in lines)
                {
                    // Split the line into InputType and key list parts
                    string[] parts = line.Split('=');
                    if (parts.Length != 2)
                    {
                        // Ignore the line if it doesn't contain exactly one equals sign
                        continue;
                    }
                    string inputTypeString = parts[0].Trim();
                    string[] keyStrings = parts[1].Split('&');

                    // Convert the input type string to an enum value
                    InputType inputType;
                    if (!Enum.TryParse(inputTypeString, out inputType))
                    {
                        // Ignore the line if the input type string is not a valid enum value
                        continue;
                    }

                    // Convert the key strings to Keys values and add them to the dictionary
                    List<MouseType> mouseTypes = new List<MouseType>();
                    
                    List<Keys> keys = new List<Keys>();
                    foreach (string keyString in keyStrings)
                    {
                        if (Enum.TryParse(keyString.Trim(), out Keys key))
                        {
                            keys.Add(key);
                        }

                        if (Enum.TryParse(keyString.Trim(), out MouseType mouseType))
                        {
                            mouseTypes.Add(mouseType);
                        }
                    }
                    if (keys.Count > 0 || mouseTypes.Count > 0)
                    {
                        MouseState mouseState = new MouseState(
                            0,0,0,
                            mouseTypes.Contains(MouseType.LeftButton)? ButtonState.Pressed : ButtonState.Released,
                            mouseTypes.Contains(MouseType.MiddleButton)? ButtonState.Pressed : ButtonState.Released,
                            mouseTypes.Contains(MouseType.RightButton) ? ButtonState.Pressed : ButtonState.Released,
                            mouseTypes.Contains(MouseType.XButton1) ? ButtonState.Pressed : ButtonState.Released,
                            mouseTypes.Contains(MouseType.XButton2) ? ButtonState.Pressed : ButtonState.Released);
                        KeyboardState keyboardState = new KeyboardState(keys.ToArray());

                        keyBindings.Add(new InputState(inputType, keyboardState, mouseState));
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any file I/O errors by logging an error message and returning an empty list
                Debug.WriteLine($"Error loading key bindings from file: {ex.Message}");
                keyBindings = new List<InputState>();
            }

            return keyBindings;
        }

        public static void CreateDefaultFile(string filepath)
        {
            File.WriteAllText(filepath, 
                "Init=F12\r\n" +
                "ClearErrors=Esc\r\n" +
                "MoveMultiplier=LeftShift\r\n" +
                "UndoDebug=F8\r\n" +
                "MoveUp=W\r\n" +
                "MoveDown=S\r\n" +
                "MoveLeft=A\r\n" +
                "MoveRight=D\r\n" +
                "DeleteObject=Delete\r\n" +
                "DeleteConnection=Delete\r\n" +
                "DeleteConnection=Back\r\n" +
                "NewConnectionPoint=LeftButton\r\n" +
                "StopDragging=LeftControl&Y\r\n" +
                "StopDragging=LeftControl&Z\r\n" +
                "StopDragging=RightControl&Y\r\n" +
                "StopDragging=RightControl&Z\r\n" +
                "Drag=LeftButton\r\n" +
                "AddToSelection=LeftControl\r\n" +
                "SubFromSelection=LeftShift\r\n" +
                "Pan=RightButton");
        }

        /// <summary>
        /// Returns whether or not the given InputType is pressed.
        /// This is the preferred way of accessing InputHandler.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="keybindState"></param>
        /// <param name="exclusive"></param>
        /// <returns></returns>
        public bool CheckAction(InputType type, KeybindState keybindState = InputHandler.KeybindState.Pressed, bool exclusive = false)
        {

            bool newPress = exclusive ? CheckActionExclusive(type, KeyboardState, MouseState) : CheckActionInclusive(type, KeyboardState, MouseState);
            bool oldPress = exclusive ? CheckActionExclusive(type, OldKeyboardState, OldMouseState) : CheckActionInclusive(type, OldKeyboardState, OldMouseState);
            //this should use the logic of UIRoot.GetKeyState but this is what I'm doing for now
            switch (keybindState)
            {
                case KeybindState.Pressed:
                    return newPress;

                case KeybindState.JustPressed:
                    return newPress && !oldPress;

                case KeybindState.Released:
                    return !newPress;

                case KeybindState.JustReleased:
                    return !newPress && oldPress;

                default:
                    return false;
            }
        }

        private bool CheckActionExclusive(InputType type, KeyboardState keyboardState, MouseState mouseState)
        {
            MouseState mouseState2 = new MouseState(0, 0, 0,
                mouseState.LeftButton,
                mouseState.MiddleButton,
                mouseState.RightButton,
                mouseState.XButton1,
                mouseState.XButton2);

            foreach (InputState inputState in keyBindings)
            {
                if (inputState.Equals(new InputState(type, keyboardState, mouseState2))) { return true; }
            }

            return false;
        }

        private bool CheckActionInclusive(InputType type, KeyboardState keyboardState, MouseState mouseState)
        {

            foreach (InputState inputState in keyBindings)
            {
                if (inputState.type != type) continue;

                bool match = true;

                foreach (Keys key in inputState.keyboardState.GetPressedKeys())
                { if (!keyboardState.IsKeyDown(key)) match = false; }

                if (match == false) continue;

                if (inputState.mouseState.LeftButton == ButtonState.Pressed && mouseState.LeftButton != ButtonState.Pressed)
                { continue; }

                if (inputState.mouseState.RightButton == ButtonState.Pressed && mouseState.RightButton != ButtonState.Pressed)
                { continue; }

                if (inputState.mouseState.MiddleButton == ButtonState.Pressed && mouseState.MiddleButton != ButtonState.Pressed)
                { continue; }

                if (inputState.mouseState.XButton1 == ButtonState.Pressed && mouseState.XButton1 != ButtonState.Pressed)
                { continue; }

                if (inputState.mouseState.XButton2 == ButtonState.Pressed && mouseState.XButton2 != ButtonState.Pressed)
                { continue; }

                return true;
            }

            return false;
        }


        public bool CheckActionInclusive(InputType type, bool old = false) => CheckActionInclusive(type, old ? OldKeyboardState : KeyboardState, old ? OldMouseState : MouseState);

        public bool CheckActionExclusive(InputType type, bool old = false) => CheckActionExclusive(type, old ? OldKeyboardState : KeyboardState, old ? OldMouseState : MouseState);



        public KeyboardState KeyboardState;
        public KeyboardState OldKeyboardState;

        public MouseState MouseState;
        public MouseState OldMouseState;

        // Define a dictionary to hold key bindings
        List<InputState> keyBindings = new List<InputState>();

        public enum InputType
        {
            None,
            Init,
            ClearErrors,
            MoveMultiplier,
            UndoDebug,
            MoveUp,
            MoveDown,
            MoveLeft,
            MoveRight,
            DeleteObject,
            DeleteConnection,
            NewConnectionPoint,
            StopDragging,
            Drag,
            AddToSelection,
            SubFromSelection,
            Pan
        }

        public enum MouseType
        {
            None,
            LeftButton,
            RightButton,
            MiddleButton,
            XButton1,
            XButton2
        }

        public enum KeybindState
        {
            Released = 0,
            JustReleased = 1,
            Pressed = 3,
            JustPressed = 2,
        }
    }
}
