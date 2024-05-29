using MonoGame.Framework.Input;

namespace PhysicsEngine
{
    public readonly struct InputState
    {
        public MouseState OldMouseState { get; }
        public MouseState NewMouseState { get; }

        public KeyboardState OldKeyState { get; }
        public KeyboardState NewKeyState { get; }

        public InputState(
            MouseState oldMouseState,
            MouseState newMouseState,
            KeyboardState oldKeyState,
            KeyboardState newKeyState)
        {
            OldMouseState = oldMouseState;
            NewMouseState = newMouseState;
            OldKeyState = oldKeyState;
            NewKeyState = newKeyState;
        }

        public bool IsKeyPressed(Keys key)
        {
            return NewKeyState.IsKeyDown(key) && OldKeyState.IsKeyUp(key);
        }

        public bool IsLeftMouseButtonPressed()
        {
            return (NewMouseState.LeftButton == ButtonState.Pressed) 
                && (OldMouseState.LeftButton == ButtonState.Released);
        }
    }
}