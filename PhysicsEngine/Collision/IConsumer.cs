namespace PhysicsEngine.Collision;

public interface IConsumer<T>
{
    void Accept(T value);
}