using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PhysicsEngine.Collections;

public class Ring<T>
{
    private readonly T[] _buffer;

    private int _head;
    private int _tail;
    private int _size;

    public int Capacity => _buffer.Length;

    public bool IsEmpty => _size == 0;

    public bool IsFull => _size == Capacity;

    public int Head => _head;
    public int Tail => _tail;

    public Ring(int length)
    {
        _buffer = new T[length];
    }

    public Span<T> GetSpan()
    {
        return new Span<T>(_buffer);
    }

    public void PushBack(T item)
    {
        GetSlot(_tail) = item;
        MoveNext(ref _tail);

        if (IsFull)
        {
            _head = _tail;
        }
        else
        {
            _size++;
        }
    }

    public void PushFront(T item)
    {
        MovePrev(ref _head);
        GetSlot(_head) = item;

        if (IsFull)
        {
            _tail = _head;
        }
        else
        {
            _size++;
        }
    }

    public T PopBack()
    {
        ThrowIfEmpty();

        MovePrev(ref _tail);
        ref T slot = ref GetSlot(_tail);
        T value = slot;
        slot = default!;

        _size--;
        return value;
    }

    public T PopFront()
    {
        ThrowIfEmpty();

        ref T slot = ref GetSlot(_head);
        T value = slot;
        slot = default!;
        MoveNext(ref _head);

        _size--;
        return value;
    }

    /// <summary>
    /// Avoids bound checks for internal indices that are known to always be in range.
    /// </summary>
    private ref T GetSlot(int index)
    {
        Debug.Assert((uint) index < (uint) _buffer.Length);
        return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_buffer), index);
    }

    private void MoveNext(ref int index)
    {
        int tmp = index + 1;
        if (tmp == Capacity)
        {
            tmp = 0;
        }
        index = tmp;
    }

    private void MovePrev(ref int index)
    {
        int tmp = index;
        if (tmp == 0)
        {
            tmp = Capacity;
        }
        index = tmp - 1;
    }

    private void ThrowIfEmpty()
    {
        if (IsEmpty)
        {
            ThrowEmpty();
        }
    }

    [DoesNotReturn]
    private static void ThrowEmpty()
    {
        throw new InvalidOperationException("Cannot take elements from an empty buffer.");
    }
}
