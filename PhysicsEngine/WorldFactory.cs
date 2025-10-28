using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace PhysicsEngine;

public record struct WorldFactory(string Name, Func<Random, World> Factory)
{
    public static WorldFactory New<T>()
        where T : World
    {
        var param = Expression.Parameter(typeof(Random));

        NewExpression call = (typeof(T).GetConstructor([typeof(Random)]) is { } new1)
            ? Expression.New(new1, param)
            : Expression.New(typeof(T).GetConstructor([]) ?? throw new ArgumentException());

        var factory = Expression.Lambda<Func<Random, World>>(call, param).Compile();

        var zeroedWorld = (World) RuntimeHelpers.GetUninitializedObject(typeof(T));
        return new WorldFactory(zeroedWorld.Name, factory);
    }
}