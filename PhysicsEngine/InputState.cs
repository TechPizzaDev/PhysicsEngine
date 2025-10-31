using MonoGame.Framework;
using MonoGame.Framework.Input;

namespace PhysicsEngine;

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

    public KeyModifiers Modifiers => newKeyState.Modifiers;

    public Point MousePosition => newMouseState.Position;

    public bool IsKeyPressed(Keys key) => newKeyState.IsKeyDown(key) && oldKeyState.IsKeyUp(key);

    public bool IsKeyDown(Keys key) => newKeyState.IsKeyDown(key);

    public bool IsLeftMouseButtonPressed()
    {
        return (newMouseState.LeftButton == ButtonState.Pressed)
            && (oldMouseState.LeftButton == ButtonState.Released);
    }
}