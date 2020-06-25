using System;

namespace Proto.Promises
{
    /// <summary>
    /// Represents a callback delegate that has been registered with a <see cref="CancelationToken"/>.
    /// </summary>
    [System.Diagnostics.DebuggerNonUserCode]
    public struct CancelationRegistration : IEquatable<CancelationRegistration>
    {
        private readonly Internal.CancelationRef _ref;
        private readonly uint _order;

        /// <summary>
        /// FOR INTERNAL USE ONLY!
        /// </summary>
        internal CancelationRegistration(Internal.CancelationRef cancelationRef, Internal.ICancelDelegate cancelDelegate)
        {
            _ref = cancelationRef;
            _order = _ref.Register(cancelDelegate);
        }

        /// <summary>
        /// Get whether the callback is registered and the associated <see cref="CancelationToken"/> has not been canceled.
        /// </summary>
        public bool IsRegistered
        {
            get
            {
                return _ref != null && _ref.IndexOf(_order) >= 0;
            }
        }

        /// <summary>
        /// Unregister the callback from the associated <see cref="CancelationToken"/>.
        /// </summary>
        public void Unregister()
        {
            if (!IsRegistered)
            {
                throw new InvalidOperationException("CancelationRegistration is not registered.", Internal.GetFormattedStacktrace(1));
            }
            _ref.Unregister(_order);
        }

        /// <summary>
        /// Try to unregister the callback from the associated <see cref="CancelationToken"/>. Returns true if the callback was successfully unregistered, false otherwise.
        /// </summary>
        /// <returns>true if the callback was previously registered, false otherwise</returns>
        public bool TryUnregister()
        {
            if (_ref == null)
            {
                return false;
            }
            return _ref.TryUnregister(_order);
        }

        public bool Equals(CancelationRegistration other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            if (obj is CancelationRegistration)
            {
                return Equals((CancelationRegistration) obj);
            }
            return false;
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
                hash = hash * 31 + _order.GetHashCode();
                hash = hash * 31 + _ref.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(CancelationRegistration c1, CancelationRegistration c2)
        {
            return c1._ref == c2._ref & c1._order == c2._order;
        }

        public static bool operator !=(CancelationRegistration c1, CancelationRegistration c2)
        {
            return !(c1 == c2);
        }
    }
}