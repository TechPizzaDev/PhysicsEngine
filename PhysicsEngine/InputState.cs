using MonoGame.Framework.Input;

namespace PhysicsEngine
{
    public readonly struct InputState(
        MouseState oldMouseState,
        MouseState newMouseState,
        KeyboardState oldKeyState,
        KeyboardState newKeyState)
    {
        public MouseState OldMouseState => oldMouseState;
        public MouseState NewMouseState => newMouseState;

        public KeyboardState OldKeyState => oldKeyState;
        public KeyboardState NewKeyState => newKeyState;

        public bool IsKeyPressed(Keys key)
        {
            return newKeyState.IsKeyDown(key) && oldKeyState.IsKeyUp(key);
        }

        public bool IsLeftMouseButtonPressed()
        {
            return (newMouseState.LeftButton == ButtonState.Pressed) 
                && (oldMouseState.LeftButton == ButtonState.Released);
        }
    }
}