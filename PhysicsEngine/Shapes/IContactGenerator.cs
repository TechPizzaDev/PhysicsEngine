namespace PhysicsEngine.Shapes;

public interface IContactGenerator<T1, T2>
{
    bool Generate(ref T1 a, ref T2 b, out Contact2D contact);
}
