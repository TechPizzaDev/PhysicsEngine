using System.Diagnostics.CodeAnalysis;

namespace PhysicsEngine.Collections;

public static class ArrayExtensions
{
    [return: MaybeNull]
    public static T Get<T>(this T[] array, int index, [AllowNull] T defaultValue = default)
    {
        if ((uint) index < (uint) array.Length)
        {
            return array[index];
        }
        return defaultValue;
    }
}
