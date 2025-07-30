// ReadOnlySpan is part of netstandard2.1, and we use the Span nuget package in netstandard2.0,
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
    internal readonly ref struct ReadOnlySpan<T>
    {
        private readonly Span<T> _span;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan(Span<T> span)
        {
            _span = span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan(T[] array)
        {
            _span = new Span<T>(array);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan(T[] array, int start, int length)
        {
            _span = new Span<T>(array, start, length);
        }

        internal T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span[index];
        }

        internal int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span.Length;
        }

        internal bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _span.IsEmpty;
        }

        [Obsolete("Equals() on ReadOnlySpan will always throw an exception. Use the equality operator instead.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new NotSupportedException();

        [Obsolete("GetHashCode() on ReadOnlySpan will always throw an exception.")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new NotSupportedException();

        public static implicit operator ReadOnlySpan<T>(T[] array) => new ReadOnlySpan<T>(array);

        public static implicit operator ReadOnlySpan<T>(Span<T> span) => new ReadOnlySpan<T>(span);

        internal static ReadOnlySpan<T> Empty => default;

        public Enumerator GetEnumerator() => new Enumerator(this);
        
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        public ref struct Enumerator
        {
            private readonly ReadOnlySpan<T> _span;
            private int _index;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(ReadOnlySpan<T> span)
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

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _span[_index];
            }
        }

        public static bool operator ==(ReadOnlySpan<T> left, ReadOnlySpan<T> right)
            => left._span == right._span;

        public static bool operator !=(ReadOnlySpan<T> left, ReadOnlySpan<T> right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<T> Slice(int start) => _span.Slice(start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpan<T> Slice(int start, int length) => _span.Slice(start, length);

        internal void CopyTo(Span<T> destination) => _span.CopyTo(destination);
    }
}

#endif // UNITY_2018_3_OR_NEWER && !UNITY_2021_2_OR_NEWER