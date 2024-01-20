#if CSHARP_7_3_OR_NEWER

#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Async.CompilerServices;
using Proto.Promises.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ProtoPromiseTests.APIs.Linq
{
    public class ContainsAsyncTests
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

#if PROMISE_DEBUG
        [Test]
        public void ContainsAsync_Null()
        {
            var enumerable = AsyncEnumerable.Range(0, 10);

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ContainsAsync(42, default(IEqualityComparer<int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).ContainsAsync(42, default(IEqualityComparer<int>)));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif

        [Test]
        public void ContainsAsync_Simple_True(
            [Values] bool configured)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3, 4, 5 }.ToAsyncEnumerable();
                if (configured)
                    Assert.True(await xs.ContainsAsync(3));
                else
                    Assert.True(await xs.ConfigureAwait(SynchronizationOption.Foreground).ContainsAsync(3));
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ContainsAsync_Simple_False(
            [Values] bool configured)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3, 4, 5 }.ToAsyncEnumerable();
                if (configured)
                    Assert.False(await xs.ContainsAsync(6));
                else
                    Assert.False(await xs.ConfigureAwait(SynchronizationOption.Foreground).ContainsAsync(6));
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ContainsAsync_Simple_Comparer_True(
            [Values] bool configured)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3, 4, 5 }.ToAsyncEnumerable();
                if (configured)
                    Assert.True(await xs.ContainsAsync(-3, new Eq()));
                else
                    Assert.True(await xs.ConfigureAwait(SynchronizationOption.Foreground).ContainsAsync(-3, new Eq()));
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ContainsAsync_Simple_Comparer_False(
            [Values] bool configured)
        {
            Promise.Run(async () =>
            {
                var xs = new[] { 1, 2, 3, 4, 5 }.ToAsyncEnumerable();
                if (configured)
                    Assert.False(await xs.ContainsAsync(-6, new Eq()));
                else
                    Assert.False(await xs.ConfigureAwait(SynchronizationOption.Foreground).ContainsAsync(-6, new Eq()));
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        private sealed class Eq : IEqualityComparer<int>
        {
            public bool Equals(int x, int y)
            {
                return EqualityComparer<int>.Default.Equals(Math.Abs(x), Math.Abs(y));
            }

            public int GetHashCode(int obj)
            {
                throw new NotImplementedException();
            }
        }
    }
}

#endif