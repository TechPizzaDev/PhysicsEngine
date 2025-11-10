namespace PhysicsEngine.Collision;

public interface IContactGenerator<T1, T2>
{
    void Generate<C>(ref T1 a, ref T2 b, C consumer)
        where C : IConsumer<Contact2D>;
}
