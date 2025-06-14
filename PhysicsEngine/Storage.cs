using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace PhysicsEngine
{
    public class Storage<T>
    {
        private T[] _values;
        private int _count;

        public int Count => _count;

        public int Capacity => _values.Length;

        public Storage(int capacity)
        {
            _values = new T[BitOperations.RoundUpToPowerOf2((uint) capacity)];
        }

        public Storage() : this(4)
        {
        }

        public Span<T> AsSpan()
        {
            return _values.AsSpan(0, _count);
        }

        public ref T Add()
        {
            int count = _count;
            if (count >= _values.Length)
            {
                Array.Resize(ref _values, (int) BitOperations.RoundUpToPowerOf2((uint) count + 1));
            }

            ref T value = ref _values[count];
            _count = count + 1;
            return ref value;
        }

        public void RemoveAt(int index)
        {
            if ((uint) index >= (uint) _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            T lastValue = _values[_count - 1];
            _values[index] = lastValue;
            _count--;
        }

        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                AsSpan().Clear();
            }
            _count = 0;
        }
    }
}