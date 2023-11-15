#if !UNITY_WEBGL

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using System;
using System.Collections.Generic;
using System.Threading;

#pragma warning disable CS0219 // The variable is assigned but its value is never used.

namespace ProtoPromiseTests.Concurrency
{
    public class MiscellaneousConcurrencyTests
    {
        [SetUp]
        public void Setup()
        {
            TestHelper.Setup();
        }

        [TearDown]
        public void Teardown()
        {
            TestHelper.Cleanup();
        }

        public enum ContinuationType
        {
            ContinueWith,
            Await
        }

        [Test]
        public void ResetRuntimeContext_SuppressesUnhandledExceptions_Concurrent()
        {
            var threadHelper = new ThreadHelper();
            threadHelper.ExecuteParallelActionsWithOffsets(true,
                //setup:
                () => { },
                //teardown:
                () => Promise.Manager.ResetRuntimeContext(),
                // actions:
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
                () =>
                {
                    var asyncLock = new AsyncLock();
                    var key = asyncLock.Lock();
                    var keyPromise = asyncLock.LockAsync();
                    Promise.Manager.ResetRuntimeContext();
                },
#endif
                () =>
                {
                    var cancelationSource = CancelationSource.New();
                    Promise.Manager.ResetRuntimeContext();
                },
                () => CancelationSource.New().Dispose(),
                () =>
                {
                    var deferred = Promise.NewDeferred();
                    deferred.Resolve();
                    var promise = deferred.Promise;
                    promise.Then(() => { }).Forget();
                    try
                    {
                        promise.Then(() => { });
                    }
                    catch (System.InvalidOperationException)
                    {
                    }
                },
                () =>
                {
                    var deferred = Promise.NewDeferred();
                    var promise = deferred.Promise.Then(() => { });
                    Promise.Manager.ResetRuntimeContext();
                },
                () => Promise.Manager.ResetRuntimeContext()
            );
        }
    }
}

#endif // !UNITY_WEBGL