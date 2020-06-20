using System;

namespace Proto.Promises
{
    /// <summary>
    /// Cancelation source used to cancel promises.
    /// </summary>
    [System.Diagnostics.DebuggerNonUserCode]
    public struct CancelationSource : ICancelableAny, IDisposable, IEquatable<CancelationSource>
    {
        private readonly Internal.CancelationRef _ref;
        private readonly int _id;

        /// <summary>
        /// Get a new <see cref="CancelationSource"/>.
        /// </summary>
        public static CancelationSource New()
        {
            return new CancelationSource(Internal.CancelationRef.GetOrCreate());
        }

        private CancelationSource(Internal.CancelationRef cancelationRef)
        {
            _ref = cancelationRef;
            _id = _ref.SourceId;
        }

        /// <summary>
        /// Get the <see cref="CancelationToken"/> associated with this <see cref="CancelationSource"/>.
        /// </summary>
        public CancelationToken Token
        {
            get
            {
                if (!IsValid)
                {
                    throw new InvalidOperationException("CancelationSource.Token: source is not valid.", Internal.GetFormattedStacktrace(1));
                }
                return new CancelationToken(_ref);
            }
        }

        /// <summary>
        /// Get whether or not this <see cref="CancelationSource"/> is valid.
        /// <para/>A <see cref="CancelationSource"/> is valid if it was created from <see cref="New"/> and was not disposed.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return _ref != null && _ref.SourceId == _id;
            }
        }

        /// <summary>
        /// Communicates a request for cancelation without providing a reason, and invokes all callbacks that are registered to any associated <see cref="CancelationToken"/>.
        /// </summary>
        public void Cancel()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("CancelationSource.Cancel: source is not valid.", Internal.GetFormattedStacktrace(1));
            }
            if (_ref.IsCanceled)
            {
                throw new InvalidOperationException("CancelationSource.Cancel: source was already canceled.", Internal.GetFormattedStacktrace(1));
            }
            _ref.SetCanceled();
        }

        /// <summary>
        /// Communicates a request for cancelation with the provided reason, and invokes all callbacks that are registered to any associated <see cref="CancelationToken"/>.
        /// </summary>
        public void Cancel<TCancel>(TCancel reason)
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("CancelationSource.Cancel: source is not valid.", Internal.GetFormattedStacktrace(1));
            }
            if (_ref.IsCanceled)
            {
                throw new InvalidOperationException("CancelationSource.Cancel: source was already canceled.", Internal.GetFormattedStacktrace(1));
            }
            _ref.SetCanceled(ref reason);
        }

        /// <summary>
        /// Release all resources used by this <see cref="CancelationSource"/>. This instance will no longer be valid.
        /// </summary>
        public void Dispose()
        {
            if (!IsValid)
            {
                throw new InvalidOperationException("CancelationSource.Dispose: source is not valid.", Internal.GetFormattedStacktrace(1));
            }
            _ref.Dispose();
        }

        public bool Equals(CancelationSource other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            if (obj is CancelationSource)
            {
                return Equals((CancelationSource) obj);
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
                hash = hash * 31 + _ref.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(CancelationSource c1, CancelationSource c2)
        {
            return c1._ref == c2._ref & c1._id == c2._id;
        }

        public static bool operator !=(CancelationSource c1, CancelationSource c2)
        {
            return !(c1 == c2);
        }
    }
}