using PhysicsEngine.Shapes;

namespace PhysicsEngine.Levels;

public class Exercise1 : ExerciseWorld
{
    public Exercise1()
    {
        Add(new CircleBody()
        {
            Radius = 1,
            Density = 250
        }).CalculateMass();
    }
}
