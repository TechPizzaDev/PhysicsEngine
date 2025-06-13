namespace PhysicsEngine.Shapes;

public interface ICollisionHandler<T1, T2>
{
    void OnCollision(ref T1 a, ref T2 b);
}
