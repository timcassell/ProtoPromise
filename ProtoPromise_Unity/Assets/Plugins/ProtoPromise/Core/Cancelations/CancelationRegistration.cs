#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Diagnostics;

namespace Proto.Promises
{
    /// <summary>
    /// Represents a callback delegate that has been registered with a <see cref="CancelationToken"/>.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    public
#if CSHARP_7_3_OR_NEWER
        readonly
#endif
        struct CancelationRegistration : IEquatable<CancelationRegistration>
    {
        private readonly Internal.CancelationRef _ref;
        private readonly Internal.CancelationCallbackNode _node;
        private readonly int _nodeId;
        private readonly int _tokenId;

        /// <summary>
        /// FOR INTERNAL USE ONLY!
        /// </summary>
        internal CancelationRegistration(Internal.CancelationRef cancelationRef, Internal.CancelationCallbackNode node, int nodeId, int tokenId)
        {
            _ref = cancelationRef;
            _node = node;
            _nodeId = nodeId;
            _tokenId = tokenId;
        }

        /// <summary>
        /// Get the <see cref="CancelationToken"/> associated with this <see cref="CancelationRegistration"/>.
        /// </summary>
        public CancelationToken Token
        {
            get
            {
                return new CancelationToken(_ref, _tokenId);
            }
        }

        /// <summary>
        /// Get whether the callback is registered and the associated <see cref="CancelationToken"/> has not been canceled and the associated <see cref="CancelationSource"/> has not been disposed.
        /// </summary>
        public bool IsRegistered
        {
            get
            {
                bool isRegistered, _;
                GetIsRegisteredAndIsCancelationRequested(out isRegistered, out _);
                return isRegistered;
            }
        }

        /// <summary>
        /// Get whether this is registered and whether the associated <see cref="CancelationToken"/> is requesting cancelation as an atomic operation.
        /// </summary>
        /// <param name="isRegistered">true if this is registered, false otherwise</param>
        /// <param name="isTokenCancelationRequested">true if the associated <see cref="CancelationToken"/> is requesting cancelation, false otherwise</param>
        public void GetIsRegisteredAndIsCancelationRequested(out bool isRegistered, out bool isTokenCancelationRequested)
        {
            isRegistered = Internal.CancelationCallbackNode.GetIsRegisteredAndIsCanceled(_ref, _node, _nodeId, _tokenId, out isTokenCancelationRequested);
        }

        /// <summary>
        /// Unregister the callback from the associated <see cref="CancelationToken"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public void Unregister()
        {
            if (!TryUnregister())
            {
                throw new InvalidOperationException("CancelationRegistration is not registered.", Internal.GetFormattedStacktrace(1));
            }
        }

        /// <summary>
        /// Try to unregister the callback from the associated <see cref="CancelationToken"/>. Returns true if the callback was successfully unregistered, false otherwise.
        /// </summary>
        /// <returns>true if the callback was previously registered and the associated <see cref="CancelationToken"/> not yet canceled and the associated <see cref="CancelationSource"/> not yet disposed, false otherwise</returns>
        public bool TryUnregister()
        {
            bool _;
            return TryUnregister(out _);
        }

        /// <summary>
        /// Try to unregister the callback from the associated <see cref="CancelationToken"/>. Returns true if the callback was successfully unregistered, false otherwise.
        /// <paramref name="isTokenCancelationRequested"/> will be true if the associated <see cref="CancelationToken"/> is requesting cancelation, false otherwise.
        /// </summary>
        /// <param name="isTokenCancelationRequested">true if the associated <see cref="CancelationToken"/> is requesting cancelation, false otherwise</param>
        /// <returns>true if the callback was previously registered and the associated <see cref="CancelationSource"/> not yet canceled or disposed, false otherwise</returns>
        public bool TryUnregister(out bool isTokenCancelationRequested)
        {
            return Internal.CancelationCallbackNode.TryUnregister(_ref, _node, _nodeId, _tokenId, out isTokenCancelationRequested);
        }

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="CancelationRegistration"/>.</summary>
        public bool Equals(CancelationRegistration other)
        {
            return this == other;
        }

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
        public override bool Equals(object obj)
        {
#if CSHARP_7_3_OR_NEWER
            return obj is CancelationRegistration registration && Equals(registration);
#else
            return obj is CancelationRegistration && Equals((CancelationRegistration) obj);
#endif
        }

        /// <summary>Returns the hash code for this instance.</summary>
        public override int GetHashCode()
        {
            return Internal.BuildHashCode(_node, _nodeId.GetHashCode(), _tokenId.GetHashCode());
        }

        /// <summary>Returns a value indicating whether two <see cref="CancelationRegistration"/> values are equal.</summary>
        public static bool operator ==(CancelationRegistration lhs, CancelationRegistration rhs)
        {
            return lhs._node == rhs._node & lhs._nodeId == rhs._nodeId & lhs._tokenId == rhs._tokenId;
        }

        /// <summary>Returns a value indicating whether two <see cref="CancelationRegistration"/> values are not equal.</summary>
        public static bool operator !=(CancelationRegistration lhs, CancelationRegistration rhs)
        {
            return !(lhs == rhs);
        }
    }
}