using System;

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
            _values = new T[capacity];
        }

        public Span<T> AsSpan()
        {
            return _values.AsSpan(0, _count);
        }

        public ref T Add()
        {
            if (_count >= _values.Length)
            {
                Array.Resize(ref _values, (_count + 1) * 2);
            }

            ref T value = ref _values[_count];
            _count++;
            return ref value;
        }

        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_count)
                throw new ArgumentOutOfRangeException(nameof(index));

            T lastValue = _values[_count - 1];
            _values[index] = lastValue;
            _count--;
        }
    }
}