using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using PhysicsEngine.Collision;

namespace PhysicsEngine.Collections
{
    public class Storage<T> : IConsumer<T>
    {
        private T[] _values;
        private int _count;

        public int Count => _count;

        public int Capacity => _values.Length;

        public Storage(int capacity)
        {
            if (capacity == 0)
                _values = Array.Empty<T>();
            else
                _values = new T[BitOperations.RoundUpToPowerOf2(Math.Max(4, (uint) capacity))];
        }

        public Storage() : this(0)
        {
        }

        public ref T this[int i] => ref Get(i);

        public Span<T> AsSpan() => _values.AsSpan(0, _count);

        void IConsumer<T>.Accept(T value) => Add() = value;

        public void Add(T value) => Add() = value;

        public ref T Add()
        {
            int count = _count;
            T[] values = _values;
            if ((uint) count < (uint) values.Length)
            {
                _count = count + 1;
                return ref values[count];
            }
            return ref AddWithResize();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ref T AddWithResize()
        {
            int count = _count;

            T[] newArray = new T[BitOperations.RoundUpToPowerOf2((uint) count + 1)];
            _values.CopyTo(new Span<T>(newArray));
            _values = newArray;

            _count = count + 1;
            return ref newArray[count];
        }

        public ref T Get(int index)
        {
            int count = _count;
            T[] values = _values;
            if (index < count && (uint) index < (uint) values.Length)
            {
                return ref values[index];
            }
            ThrowOutOfBounds();
            return ref Unsafe.NullRef<T>();
        }

        public void RemoveAt(int index)
        {
            int last = _count - 1;
            T[] values = _values;
            if ((uint) last < (uint) values.Length && (uint) index < (uint) values.Length)
            {
                T lastValue = values[last];
                values[index] = lastValue;
                _count = last;
            }
            else
            {
                ThrowOutOfBounds();
            }
        }

        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                AsSpan().Clear();
            }
            _count = 0;
        }

        [DoesNotReturn]
        private static void ThrowOutOfBounds()
        {
            throw new IndexOutOfRangeException();
        }
    }
}