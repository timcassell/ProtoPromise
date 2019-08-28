namespace ProtoPromise
{
    partial class Promise
    {
        public static class Manager
        {
            // TODO: Call these
            public static void HandleCompletes()
            {
                HandleComplete();
                ThrowUnhandledRejections();
            }

            public static void HandleCompletesAndProgress()
            {
                HandleComplete();
                HandleProgress();
                ThrowUnhandledRejections();
            }

            /// <summary>
            /// Clears all currently pooled objects. Does not affect pending or retained promises.
            /// </summary>
            public static void ClearObjectPool()
            {
                ValueLinkedStackZeroGC<Internal.IProgressListener>.ClearPooledNodes();
                Internal.OnClearPool.Invoke();
            }
        }
    }
}