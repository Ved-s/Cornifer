using Microsoft.Xna.Framework.Input;

namespace Cornifer.Input
{
    public class MouseInput : KeybindInput
    {
        public MouseKeys Key { get; set; }

        public override bool CurrentState => GetMouseInput(InputHandler.MouseState);
        public override bool OldState => GetMouseInput(InputHandler.OldMouseState);
        public override string KeyName => Key.ToString();

        public MouseInput(MouseKeys key)
        {
            Key = key;
        }

        bool GetMouseInput(MouseState state)
        {
            if (InputHandler.DisableInputs)
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
