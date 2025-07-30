// Span is part of netstandard2.1, and we use the Span nuget package in netstandard2.0,
// but we don't use nuget packages in Unity, so we have to implement it ourselves.
#if UNITY_2018_3_OR_NEWER && !UNITY_2021_2_OR_NEWER

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member

namespace System
{
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    internal readonly ref struct Span<T>
    {
        // We only use it as a view over an array, not using pointers.
        private readonly T[] _array;
        private readonly int _start;
        private readonly int _length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Span(T[] array)
        {
            if (array == null)
            {
                this = default;
                return; // returns default
            }
            _array = array;
            _start = 0;
            _length = array.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Span(T[] array, int start, int length)
        {
            if (array == null)
            {
                this = default;
                return; // returns default
            }
            _array = array;
            _start = start;
            _length = length;
        }

        internal ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _array[_start + index];
        }

        internal int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        internal bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length == 0;
        }

        [Obsolete("Equals() on Span will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new NotSupportedException();

        [Obsolete("GetHashCode() on Span will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new NotSupportedException();

        public static implicit operator Span<T>(T[] array) => new Span<T>(array);

        internal static Span<T> Empty => default;

        public Enumerator GetEnumerator() => new Enumerator(this);
        
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public ref struct Enumerator
        {
            private readonly Span<T> _span;
            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(Span<T> span)
            {
                _span = span;
                _index = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                int index = _index + 1;
                if (index < _span.Length)
                {
                    _index = index;
                    return true;
                }

                return false;
            }

            public ref T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => ref _span[_index];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Clear()
        {
            Array.Clear(_array, _start, _length);
        }

        public static bool operator ==(Span<T> left, Span<T> right)
            => left._array == right._array
            && left._start == right._start
            && left._length == right._length;

        public static bool operator !=(Span<T> left, Span<T> right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Span<T> Slice(int start) => new Span<T>(_array, _start + start, _length - start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Span<T> Slice(int start, int length) => new Span<T>(_array, _start + start, length);

        internal void CopyTo(Span<T> destination)
        {
            for (int i = 0; i < _length; ++i)
            {
                destination[i] = this[i];
            }
        }
    }
}

#endif // UNITY_2018_3_OR_NEWER && !UNITY_2021_2_OR_NEWER