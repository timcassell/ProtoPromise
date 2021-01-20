#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;

namespace Proto.Promises
{
    /// <summary>
    /// Represents a callback delegate that has been registered with a <see cref="CancelationToken"/>.
    /// </summary>
#if !PROTO_PROMISE_DEVELOPER_MODE
    [System.Diagnostics.DebuggerNonUserCode]
#endif
    public struct CancelationRegistration : IEquatable<CancelationRegistration>
    {
        private readonly Internal.CancelationRef _ref;
        private readonly uint _order;
        private readonly ushort _id;

        /// <summary>
        /// FOR INTERNAL USE ONLY!
        /// </summary>
        internal CancelationRegistration(Internal.CancelationRef cancelationRef, Internal.ICancelDelegate cancelDelegate)
        {
            _ref = cancelationRef;
            _id = _ref.TokenId;
            _order = _ref.Register(cancelDelegate);
        }

        /// <summary>
        /// Get whether the callback is registered and the associated <see cref="CancelationToken"/> has not been canceled.
        /// </summary>
        public bool IsRegistered
        {
            get
            {
                return _ref != null && _ref.IsRegistered(_id, _order);
            }
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
        /// <returns>true if the callback was previously registered and the associated <see cref="CancelationToken"/> not yet canceled, false otherwise</returns>
        public bool TryUnregister()
        {
            if (_ref == null)
            {
                return false;
            }
            return _ref.TryUnregister(_id, _order);
        }

        public bool Equals(CancelationRegistration other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
#if CSHARP_7_OR_LATER
            return obj is CancelationRegistration registration && Equals(registration);
#else
            return obj is CancelationRegistration && Equals((CancelationRegistration) obj);
#endif
        }

        public override int GetHashCode()
        {
            if (_ref == null)
            {
                return 0;
            }
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + _ref.GetHashCode();
                hash = hash * 31 + _order.GetHashCode();
                hash = hash * 31 + _id.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(CancelationRegistration lhs, CancelationRegistration rhs)
        {
            return lhs._ref == rhs._ref & lhs._order == rhs._order & lhs._id == rhs._id;
        }

        public static bool operator !=(CancelationRegistration lhs, CancelationRegistration rhs)
        {
            return !(lhs == rhs);
        }
    }
}