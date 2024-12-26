#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable IDE0090 // Use 'new(...)'

namespace Proto.Promises
{
    /// <summary>
    /// Propagates notification that operations should be canceled.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public readonly partial struct CancelationToken : IEquatable<CancelationToken>
    {
        internal readonly Internal.CancelationRef _ref;
        internal readonly int _id;

        /// <summary>
        /// Returns an empty <see cref="CancelationToken"/>.
        /// </summary>
        public static CancelationToken None
        {
            [MethodImpl(Internal.InlineOption)]
            get => default;
        }

        internal CancelationToken(Internal.CancelationRef cancelationRef, int tokenId)
        {
            _ref = cancelationRef;
            _id = tokenId;
        }

        /// <summary>
        /// Get a token that is already in the canceled state.
        /// </summary>
        public static CancelationToken Canceled()
            => new CancelationToken(Internal.CancelationRef.s_canceledSentinel, Internal.CancelationRef.s_canceledSentinel.TokenId);

        /// <summary>
        /// Gets whether this token is capable of being in the canceled state.
        /// </summary>
        /// <remarks>
        /// A <see cref="CancelationToken"/> is capable of being in the canceled state when the <see cref="CancelationSource"/> it is attached to has not been disposed,
        /// or if the token is already canceled and it has been retained and not yet released.
        /// </remarks>
        public bool CanBeCanceled
            => Internal.CancelationRef.CanTokenBeCanceled(_ref, _id);

        /// <summary>
        /// Gets whether cancelation has been requested for this token.
        /// </summary>
        public bool IsCancelationRequested
            => Internal.CancelationRef.IsTokenCanceled(_ref, _id);

        /// <summary>
        /// If cancelation was requested on this token, throws a <see cref="CanceledException"/>.
        /// </summary>
        /// <exception cref="CanceledException"/>
        public void ThrowIfCancelationRequested()
        {
            if (IsCancelationRequested)
            {
                throw Promise.CancelException();
            }
        }

        /// <summary>
        /// Try to register a delegate that will be invoked when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, the callback will be invoked immediately and this will return true.
        /// </summary>
        /// <param name="callback">The delegate to be executed when the <see cref="CancelationToken"/> is canceled.</param>
        /// <param name="cancelationRegistration">The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</param>
        /// <returns>true if <paramref name="callback"/> was registered successfully, false otherwise.</returns>
        public bool TryRegister(Action callback, out CancelationRegistration cancelationRegistration)
        {
            ValidateArgument(callback, nameof(callback), 1);
            return Internal.CancelationRef.TryRegister(_ref, _id, new Internal.CancelDelegateTokenVoid(callback), out cancelationRegistration);
        }

        /// <summary>
        /// Try to capture a value and register a delegate that will be invoked with the captured value when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, the callback will be invoked immediately and this will return true.
        /// </summary>
        /// <param name="captureValue">The value to pass into <paramref name="callback"/>.</param>
        /// <param name="callback">The delegate to be executed when the <see cref="CancelationToken"/> is canceled.</param>
        /// <param name="cancelationRegistration">The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</param>
        /// <returns>true if <paramref name="callback"/> was registered successfully, false otherwise.</returns>
        public bool TryRegister<TCapture>(TCapture captureValue, Action<TCapture> callback, out CancelationRegistration cancelationRegistration)
        {
            ValidateArgument(callback, nameof(callback), 1);
            return Internal.CancelationRef.TryRegister(_ref, _id, new Internal.CancelDelegateToken<TCapture>(captureValue, callback), out cancelationRegistration);
        }

        /// <summary>
        /// Try to register a cancelable that will be canceled when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, it will be canceled immediately and this will return true.
        /// </summary>
        /// <param name="cancelable">The cancelable to be canceled when the <see cref="CancelationToken"/> is canceled.</param>
        /// <param name="cancelationRegistration">The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</param>
        /// <returns>true if <paramref name="cancelable"/> was registered successfully, false otherwise.</returns>
        public bool TryRegister<TCancelable>(TCancelable cancelable, out CancelationRegistration cancelationRegistration) where TCancelable : ICancelable
        {
            ValidateArgument(cancelable, nameof(cancelable), 1);
            return Internal.CancelationRef.TryRegister(_ref, _id, cancelable, out cancelationRegistration);
        }

        /// <summary>
        /// Try to register a cancelable that will be canceled when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, the <paramref name="cancelable"/> will not be invoked and <paramref name="alreadyCanceled"/> will be set to <see langword="true"/>.
        /// </summary>
        /// <param name="cancelable">The cancelable to be canceled when the <see cref="CancelationToken"/> is canceled.</param>
        /// <param name="cancelationRegistration">The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</param>
        /// <param name="alreadyCanceled">If true, this was already canceled and the <paramref name="cancelable"/> will not be invoked.</param>
        /// <returns>true if <paramref name="cancelable"/> was registered successfully or this was already canceled, false otherwise.</returns>
        public bool TryRegisterWithoutImmediateInvoke<TCancelable>(TCancelable cancelable, out CancelationRegistration cancelationRegistration, out bool alreadyCanceled) where TCancelable : ICancelable
        {
            ValidateArgument(cancelable, nameof(cancelable), 1);
            return Internal.CancelationRef.TryRegister(_ref, _id, cancelable, out cancelationRegistration, out alreadyCanceled);
        }

        /// <summary>
        /// Register a delegate that will be invoked when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, the callback will be invoked immediately.
        /// </summary>
        /// <param name="callback">The delegate to be executed when the <see cref="CancelationToken"/> is canceled.</param>
        /// <returns>The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</returns>
        public CancelationRegistration Register(Action callback)
        {
            TryRegister(callback, out var registration);
            return registration;
        }

        /// <summary>
        /// Capture a value and register a delegate that will be invoked with the captured value when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, the callback will be invoked immediately.
        /// </summary>
        /// <param name="captureValue">The value to pass into <paramref name="callback"/>.</param>
        /// <param name="callback">The delegate to be executed when the <see cref="CancelationToken"/> is canceled.</param>
        /// <returns>The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</returns>
        public CancelationRegistration Register<TCapture>(TCapture captureValue, Action<TCapture> callback)
        {
            TryRegister(captureValue, callback, out var registration);
            return registration;
        }

        /// <summary>
        /// Register a cancelable that will be canceled when this <see cref="CancelationToken"/> is canceled.
        /// If this is already canceled, it will be canceled immediately.
        /// </summary>
        /// <param name="cancelable">The cancelable to be canceled when the <see cref="CancelationToken"/> is canceled.</param>
        /// <returns>The <see cref="CancelationRegistration"/> instance that can be used to unregister the callback.</returns>
        public CancelationRegistration Register<TCancelable>(TCancelable cancelable) where TCancelable : ICancelable
        {
            TryRegister(cancelable, out var registration);
            return registration;
        }

        /// <summary>
        /// Try to retain this instance. Returns true if successful, false otherwise.
        /// <para/>If successful, allows continued use of this instance, even after the associated <see cref="CancelationSource"/> has been disposed, until this is released.
        /// If successful, this should be paired with a call to <see cref="Release"/>.
        /// </summary>
        public bool TryRetain()
            => Internal.CancelationRef.TryRetainUser(_ref, _id);

        /// <summary>
        /// Release this instance. Allows resources to be released when the associated <see cref="CancelationSource"/> is disposed (if <see cref="Release"/> has been called for all <see cref="TryRetain"/> calls).
        /// <para/>This should always be paired with a call to <see cref="TryRetain"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public void Release()
        {
            if (!Internal.CancelationRef.TryReleaseUser(_ref, _id))
            {
                throw new InvalidOperationException("CancelationToken.Release: you must call Retain before you call Release.", Internal.GetFormattedStacktrace(1));
            }
        }

        /// <summary>
        /// Gets a retainer that facilitates retaining and releasing this instance. This is intended to be used with a using block `using (token.GetRetainer()) { ... }`.
        /// </summary>
        [MethodImpl(Internal.InlineOption)]
        public Retainer GetRetainer()
            => new Retainer(this);

        /// <summary>
        /// Convert this to a <see cref="System.Threading.CancellationToken"/>.
        /// </summary>
        /// <returns>A <see cref="System.Threading.CancellationToken"/> that will be canceled when this is canceled.</returns>
        public System.Threading.CancellationToken ToCancellationToken()
            => Internal.CancelationRef.GetCancellationToken(_ref, _id);

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="CancelationToken"/>.</summary>
        public bool Equals(CancelationToken other)
            => this == other;

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
        public override bool Equals(object obj)
            => obj is CancelationToken token && Equals(token);

        /// <summary>Returns the hash code for this instance.</summary>
        public override int GetHashCode()
            => HashCode.Combine(_ref, _id);

        /// <summary>Returns a value indicating whether two <see cref="CancelationToken"/> values are equal.</summary>
        public static bool operator ==(CancelationToken lhs, CancelationToken rhs)
            => lhs._ref == rhs._ref
            & lhs._id == rhs._id;

        /// <summary>Returns a value indicating whether two <see cref="CancelationToken"/> values are not equal.</summary>
        public static bool operator !=(CancelationToken lhs, CancelationToken rhs)
            => !(lhs == rhs);

        // Calls to this get compiled away in RELEASE mode
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames);
#if PROMISE_DEBUG
        static partial void ValidateArgument<TArg>(TArg arg, string argName, int skipFrames)
            => Internal.ValidateArgument(arg, argName, skipFrames + 1);
#endif

        /// <summary>
        /// A helper type that facilitates retaining and releasing <see cref="CancelationToken"/>s with a using statement.
        /// This is intended to be used instead of <see cref="TryRetain"/> and <see cref="Release"/> to reduce boilerplate code.
        /// </summary>
        public readonly struct Retainer : IDisposable
        {
            /// <summary>
            /// The retained token.
            /// </summary>
            public readonly CancelationToken token;
            /// <summary>
            /// Is the <see cref="token"/> retained.
            /// </summary>
            public readonly bool isRetained;

            [MethodImpl(Internal.InlineOption)]
            internal Retainer(CancelationToken cancelationToken)
            {
                token = cancelationToken;
                isRetained = cancelationToken.TryRetain();
            }

            /// <summary>
            /// Releases the token if it was retained. This instance is no longer valid after it has been disposed, and should not continue to be used.
            /// </summary>
            [MethodImpl(Internal.InlineOption)]
            public void Dispose()
            {
                if (isRetained)
                {
                    token.Release();
                }
            }
        }
    }

    /// <summary>
    /// Helpful extension class to convert <see cref="System.Threading.CancellationToken"/> to <see cref="CancelationToken"/>.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public static class CancellationTokenExtensions
    {
        /// <summary>
        /// Convert <paramref name="token"/> to a <see cref="CancelationToken"/>.
        /// </summary>
        /// <param name="token">The cancellation token to convert</param>
        /// <returns>A <see cref="CancelationToken"/> that will be canceled when <paramref name="token"/> is canceled.</returns>
        public static CancelationToken ToCancelationToken(this System.Threading.CancellationToken token)
            => Internal.CancelationRef.CancelationConverter.Convert(token);
    }
}