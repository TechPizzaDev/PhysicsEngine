using MonoGame.Framework;

namespace PhysicsEngine.Shapes;

public interface IColor
{
    Color Color { get; set; }

    ColorPalette GetColorPalette() => new(Color);
}
