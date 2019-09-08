namespace Proto.Promises
{
    partial class Promise
    {
        public static class Manager
        {
            public static void HandleCompletes()
            {
                HandleComplete();
                ThrowUnhandledRejections();
            }

            public static void HandleCompletesAndProgress()
            {
                HandleComplete();
                Promise.HandleProgress();
                ThrowUnhandledRejections();
            }

#pragma warning disable RECS0146 // Member hides static member from outer class
            public static void HandleProgress()
#pragma warning restore RECS0146 // Member hides static member from outer class
            {
                Promise.HandleProgress();
                ThrowUnhandledRejections();
            }

            /// <summary>
            /// Clears all currently pooled objects. Does not affect pending or retained promises.
            /// </summary>
            public static void ClearObjectPool()
            {
                ClearPooledProgress();
                Internal.OnClearPool.Invoke();
            }
        }
    }
}