#if UNITY_5_5 || NET_2_0 || NET_2_0_SUBSET
#define NET_LEGACY
#endif

#if NET_LEGACY

namespace System.Runtime.ExceptionServices
{
    // This is just used so that we can reduce #if NET_LEGACY checks around the code base.
    // It doesn't actually behave the same as the ExceptionDispatchInfo type does in .Net 4.5+, as it requires runtime support.
    // This behaves the same as a simple throw.
    internal struct ExceptionDispatchInfo
    {
        private Exception _source;

        public static ExceptionDispatchInfo Capture(Exception source)
        {
            return new ExceptionDispatchInfo() { _source = source };
        }

        public void Throw()
        {
            throw _source;
        }
    }
}

#endif