using System;

namespace PhysicsEngine.Collections;

public class EnumMap<K, V>
    where K : struct, Enum, IConvertible
{
    private readonly V[] _values;

    public EnumMap()
    {
        _values = new V[Enum.GetValues<K>().Length];
    }

    public ref V this[K key] => ref _values[ToInt64(key)];

    public Span<V> AsSpan() => new(_values);

    public void Fill(V value)
    {
        _values.AsSpan().Fill(value);
    }

    public V Get(K key, Func<K, V> factory)
    {
        ref V value = ref this[key];
        value ??= factory(key);
        return value;
    }

    private static long ToInt64(K key)
    {
        return Type.GetTypeCode(typeof(K)) switch
        {
            TypeCode.Int32 => (int) (object) key,
            _ => key.ToInt64(null),
        };
    }
}
