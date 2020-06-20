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
        private readonly int _id;
        private readonly int _order;

        internal CancelationRegistration(object cancelationRef, object cancelDelegate)
        {
            _ref = (Internal.CancelationRef) cancelationRef;
            _id = _ref.SourceId;
            _order = _ref.Register((Internal.ICancelDelegate) cancelDelegate);
        }

        /// <summary>
        /// Get whether the callback is registered and the associated <see cref="CancelationToken"/> has not been canceled.
        /// </summary>
        public bool IsRegistered
        {
            get
            {
                return _ref != null && _ref.IsRegistered(_order);
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
                hash = hash * 31 + _id.GetHashCode();
                hash = hash * 31 + _order.GetHashCode();
                hash = hash * 31 + _ref.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(CancelationRegistration c1, CancelationRegistration c2)
        {
            return c1._ref == c2._ref & c1._id == c2._id & c1._order == c2._order;
        }

        public static bool operator !=(CancelationRegistration c1, CancelationRegistration c2)
        {
            return !(c1 == c2);
        }
    }
}