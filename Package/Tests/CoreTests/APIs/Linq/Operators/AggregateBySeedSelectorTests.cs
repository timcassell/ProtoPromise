#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.CompilerServices;
using Proto.Promises.Linq;
using ProtoPromiseTests.APIs.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CS0162 // Unreachable code detected
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace ProtoPromiseTests.APIs.Linq
{
    public class AggregateBySeedSelectorTests
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
        public void AggregateBy_NullArgumentThrows()
        {
            var enumerable = AsyncEnumerable.Return(42);
            var captureValue = "captureValue";
            var nullComparer = default(IEqualityComparer<int>);

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, int>), x => 0, (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, int>), x => 0, (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, x => 0, (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, int>), x => 0, (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, int>), x => 0, (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, x => 0, (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, Promise<int>>), async x => 0, async (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, Promise<int>>), async x => 0, async (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => Promise.Resolved(0), async x => 0, async (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, Promise<int>>), async x => 0, async (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, Promise<int>>), async x => 0, async (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => Promise.Resolved(0), async x => 0, async (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, x => 0, default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, x => 0, default(Func<int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, x => 0, (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, x => 0, default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, x => 0, default(Func<int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, x => 0, (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, async x => 0, default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, async x => 0, default(Func<int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, async x => 0, async (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, async x => 0, default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, async x => 0, default(Func<int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, async x => 0, async (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, int>), x => 0, captureValue, (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, int>), x => 0, captureValue, (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, x => 0, captureValue, (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, int>), x => 0, captureValue, (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, int>), x => 0, captureValue, (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, x => 0, captureValue, (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, Promise<int>>), async x => 0, captureValue, async (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, Promise<int>>), async x => 0, captureValue, async (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => Promise.Resolved(0), async x => 0, captureValue, async (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, Promise<int>>), async x => 0, captureValue, async (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, Promise<int>>), async x => 0, captureValue, async (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => Promise.Resolved(0), async x => 0, captureValue, async (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, x => 0, captureValue, default(Func<string, int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, x => 0, captureValue, default(Func<string, int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, x => 0, captureValue, (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, x => 0, captureValue, default(Func<string, int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, x => 0, captureValue, default(Func<string, int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, x => 0, captureValue, (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, async x => 0, captureValue, default(Func<string, int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, async x => 0, captureValue, default(Func<string, int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, async x => 0, captureValue, async (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, async x => 0, captureValue, default(Func<string, int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, async x => 0, captureValue, default(Func<string, int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, async x => 0, captureValue, async (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, int>), captureValue, (cv, x) => 0, (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, int>), captureValue, (cv, x) => 0, (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, captureValue, (cv, x) => 0, (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, Promise<int>>), captureValue, async (cv, x) => 0, async (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, Promise<int>>), captureValue, async (cv, x) => 0, async (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => Promise.Resolved(0), captureValue, async (cv, x) => 0, async (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, Promise<int>>), captureValue, async (cv, x) => 0, async (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, Promise<int>>), captureValue, async (cv, x) => 0, async (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => Promise.Resolved(0), captureValue, async (cv, x) => 0, async (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, captureValue, (cv, x) => 0, default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, captureValue, (cv, x) => 0, default(Func<int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, captureValue, (cv, x) => 0, (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, default(Func<int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, captureValue, async (cv, x) => 0, default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, captureValue, async (cv, x) => 0, default(Func<int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, captureValue, async (cv, x) => 0, async (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, default(Func<int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, async (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, int>), captureValue, (cv, x) => 0, captureValue, (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, int>), captureValue, (cv, x) => 0, captureValue, (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, captureValue, (cv, x) => 0, captureValue, (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, captureValue, (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, captureValue, (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, captureValue, (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, Promise<int>>), captureValue, async (cv, x) => 0, captureValue, async (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(default(Func<int, Promise<int>>), captureValue, async (cv, x) => 0, captureValue, async (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => Promise.Resolved(0), captureValue, async (cv, x) => 0, captureValue, async (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, Promise<int>>), captureValue, async (cv, x) => 0, captureValue, async (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, default(Func<string, int, Promise<int>>), captureValue, async (cv, x) => 0, captureValue, async (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => Promise.Resolved(0), captureValue, async (cv, x) => 0, captureValue, async (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, captureValue, (cv, x) => 0, captureValue, (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, captureValue, (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, captureValue, async (cv, x) => 0, captureValue, async (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, captureValue, async (cv, acc, x) => acc, nullComparer));

            // Passing in null seedSelector makes it ambiguous, so we can't actually test it without capture value.
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, captureValue, default(Func<string, int, int>), (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, captureValue, default(Func<string, int, int>), (acc, x) => acc, EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, captureValue, default(Func<string, int, Promise<int>>), async (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, captureValue, default(Func<string, int, Promise<int>>), async (acc, x) => acc, EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, captureValue, (cv, x) => 0, default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, captureValue, (cv, x) => 0, default(Func<int, int, int>), EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, captureValue, async (cv, x) => 0, default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, captureValue, async (cv, x) => 0, default(Func<int, int, Promise<int>>), EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, captureValue, default(Func<string, int, int>), captureValue, (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, captureValue, default(Func<string, int, int>), captureValue, (cv, acc, x) => acc, EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, captureValue, default(Func<string, int, Promise<int>>), captureValue, async (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, captureValue, default(Func<string, int, Promise<int>>), captureValue, async (cv, acc, x) => acc, EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(x => 0, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int, int>), EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(async x => 0, captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, int, Promise<int>>), EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), (acc, x) => acc, EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, Promise<int>>), async (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, Promise<int>>), async (acc, x) => acc, EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, default(Func<int, int, int>), EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, default(Func<int, int, Promise<int>>), EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), captureValue, (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), captureValue, (cv, acc, x) => acc, EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, Promise<int>>), captureValue, async (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, Promise<int>>), captureValue, async (cv, acc, x) => acc, EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int, int>), EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, int, Promise<int>>), EqualityComparer<int>.Default));



            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, int>), x => 0, (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, int>), x => 0, (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, x => 0, (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, int>), x => 0, (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, int>), x => 0, (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, x => 0, (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, Promise<int>>), async x => 0, async (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, Promise<int>>), async x => 0, async (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => Promise.Resolved(0), async x => 0, async (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, Promise<int>>), async x => 0, async (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, Promise<int>>), async x => 0, async (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => Promise.Resolved(0), async x => 0, async (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, x => 0, default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, x => 0, default(Func<int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, x => 0, (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, x => 0, default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, x => 0, default(Func<int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, x => 0, (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, async x => 0, default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, async x => 0, default(Func<int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, async x => 0, async (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, async x => 0, default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, async x => 0, default(Func<int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, async x => 0, async (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, int>), x => 0, captureValue, (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, int>), x => 0, captureValue, (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, x => 0, captureValue, (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, int>), x => 0, captureValue, (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, int>), x => 0, captureValue, (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, x => 0, captureValue, (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, Promise<int>>), async x => 0, captureValue, async (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, Promise<int>>), async x => 0, captureValue, async (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => Promise.Resolved(0), async x => 0, captureValue, async (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, Promise<int>>), async x => 0, captureValue, async (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, Promise<int>>), async x => 0, captureValue, async (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => Promise.Resolved(0), async x => 0, captureValue, async (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, x => 0, captureValue, default(Func<string, int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, x => 0, captureValue, default(Func<string, int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, x => 0, captureValue, (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, x => 0, captureValue, default(Func<string, int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, x => 0, captureValue, default(Func<string, int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, x => 0, captureValue, (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, async x => 0, captureValue, default(Func<string, int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, async x => 0, captureValue, default(Func<string, int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, async x => 0, captureValue, async (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, async x => 0, captureValue, default(Func<string, int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, async x => 0, captureValue, default(Func<string, int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, async x => 0, captureValue, async (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, int>), captureValue, (cv, x) => 0, (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, int>), captureValue, (cv, x) => 0, (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, captureValue, (cv, x) => 0, (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, Promise<int>>), captureValue, async (cv, x) => 0, async (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, Promise<int>>), captureValue, async (cv, x) => 0, async (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => Promise.Resolved(0), captureValue, async (cv, x) => 0, async (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, Promise<int>>), captureValue, async (cv, x) => 0, async (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, Promise<int>>), captureValue, async (cv, x) => 0, async (acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => Promise.Resolved(0), captureValue, async (cv, x) => 0, async (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, captureValue, (cv, x) => 0, default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, captureValue, (cv, x) => 0, default(Func<int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, captureValue, (cv, x) => 0, (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, default(Func<int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, captureValue, async (cv, x) => 0, default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, captureValue, async (cv, x) => 0, default(Func<int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, captureValue, async (cv, x) => 0, async (acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, default(Func<int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, async (acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, int>), captureValue, (cv, x) => 0, captureValue, (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, int>), captureValue, (cv, x) => 0, captureValue, (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, captureValue, (cv, x) => 0, captureValue, (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, captureValue, (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, int>), captureValue, (cv, x) => 0, captureValue, (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, captureValue, (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, Promise<int>>), captureValue, async (cv, x) => 0, captureValue, async (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(default(Func<int, Promise<int>>), captureValue, async (cv, x) => 0, captureValue, async (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => Promise.Resolved(0), captureValue, async (cv, x) => 0, captureValue, async (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, Promise<int>>), captureValue, async (cv, x) => 0, captureValue, async (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, default(Func<string, int, Promise<int>>), captureValue, async (cv, x) => 0, captureValue, async (cv, acc, x) => acc, EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => Promise.Resolved(0), captureValue, async (cv, x) => 0, captureValue, async (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, captureValue, (cv, x) => 0, captureValue, (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int, int>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, captureValue, (cv, acc, x) => acc, nullComparer));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, captureValue, async (cv, x) => 0, captureValue, async (cv, acc, x) => acc, nullComparer));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, int, Promise<int>>), EqualityComparer<int>.Default));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, captureValue, async (cv, acc, x) => acc, nullComparer));

            // Passing in null seedSelector makes it ambiguous, so we can't actually test it without capture value.
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, captureValue, default(Func<string, int, int>), (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, captureValue, default(Func<string, int, int>), (acc, x) => acc, EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, captureValue, default(Func<string, int, Promise<int>>), async (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, captureValue, default(Func<string, int, Promise<int>>), async (acc, x) => acc, EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, captureValue, (cv, x) => 0, default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, captureValue, (cv, x) => 0, default(Func<int, int, int>), EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, captureValue, async (cv, x) => 0, default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, captureValue, async (cv, x) => 0, default(Func<int, int, Promise<int>>), EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, captureValue, default(Func<string, int, int>), captureValue, (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, captureValue, default(Func<string, int, int>), captureValue, (cv, acc, x) => acc, EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, captureValue, default(Func<string, int, Promise<int>>), captureValue, async (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, captureValue, default(Func<string, int, Promise<int>>), captureValue, async (cv, acc, x) => acc, EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(x => 0, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int, int>), EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(async x => 0, captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, int, Promise<int>>), EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), (acc, x) => acc, EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, Promise<int>>), async (acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, Promise<int>>), async (acc, x) => acc, EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, default(Func<int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, default(Func<int, int, int>), EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, default(Func<int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, default(Func<int, int, Promise<int>>), EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), captureValue, (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int>), captureValue, (cv, acc, x) => acc, EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, Promise<int>>), captureValue, async (cv, acc, x) => acc));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, Promise<int>>), captureValue, async (cv, acc, x) => acc, EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int, int>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, (cv, x) => 0, captureValue, (cv, x) => 0, captureValue, default(Func<string, int, int, int>), EqualityComparer<int>.Default));

            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, int, Promise<int>>)));
            Assert.Catch<System.ArgumentNullException>(() => enumerable.ConfigureAwait(SynchronizationOption.Synchronous).AggregateBy(captureValue, async (cv, x) => 0, captureValue, async (cv, x) => 0, captureValue, default(Func<string, int, int, Promise<int>>), EqualityComparer<int>.Default));

            enumerable.GetAsyncEnumerator().DisposeAsync().Forget();
        }
#endif //PROMISE_DEBUG

        // We test all the different overloads.
        private static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(AsyncEnumerable<TSource> asyncEnumerable,
            bool configured,
            bool async,
            bool captureKey,
            Func<TSource, TKey> keySelector,
            bool captureSeed,
            Func<TKey, TAccumulate> seedSelector,
            bool captureAccumulate,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            IEqualityComparer<TKey> equalityComparer = null,
            CancelationToken configuredCancelationToken = default)
        {
            if (configured)
            {
                return AggregateBy(asyncEnumerable.ConfigureAwait(SynchronizationOption.Foreground)
                    .WithCancelation(configuredCancelationToken), async, captureKey, keySelector, captureSeed, seedSelector, captureAccumulate, accumulator, equalityComparer);
            }

            const string keyCapture = "keyCapture";
            const string seedCapture = "seedCapture";
            const string accumulateCapture = "accumulateCapture";

            if (!captureKey)
            {
                if (!captureAccumulate)
                {
                    if (!captureSeed)
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.AggregateBy(async x => keySelector(x), async k => seedSelector(k), async (acc, x) => accumulator(acc, x), equalityComparer)
                                : asyncEnumerable.AggregateBy(async x => keySelector(x), async k => seedSelector(k), async (acc, x) => accumulator(acc, x))
                            : equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keySelector, seedSelector, accumulator, equalityComparer)
                                : asyncEnumerable.AggregateBy(keySelector, seedSelector, accumulator);
                    }
                    else
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.AggregateBy(async x => keySelector(x), seedCapture, async (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, async (acc, x) => accumulator(acc, x), equalityComparer)
                                : asyncEnumerable.AggregateBy(async x => keySelector(x), seedCapture, async (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, async (acc, x) => accumulator(acc, x))
                            : equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keySelector, seedCapture, (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulator, equalityComparer)
                                : asyncEnumerable.AggregateBy(keySelector, seedCapture, (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulator);
                    }
                }
                else
                {
                    if (!captureSeed)
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.AggregateBy(async x => keySelector(x), async k => seedSelector(k), accumulateCapture, async (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                }, equalityComparer)
                                : asyncEnumerable.AggregateBy(async x => keySelector(x), async k => seedSelector(k), accumulateCapture, async (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                })
                            : equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keySelector, seedSelector, accumulateCapture, (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                }, equalityComparer)
                                : asyncEnumerable.AggregateBy(keySelector, seedSelector, accumulateCapture, (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                });
                    }
                    else
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.AggregateBy(async x => keySelector(x), seedCapture, async (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulateCapture, async (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                }, equalityComparer)
                                : asyncEnumerable.AggregateBy(async x => keySelector(x), seedCapture, async (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulateCapture, async (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                })
                            : equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keySelector, seedCapture, (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulateCapture, (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                }, equalityComparer)
                                : asyncEnumerable.AggregateBy(keySelector, seedCapture, (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulateCapture, (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                });
                    }
                }
            }
            else
            {
                if (!captureAccumulate)
                {
                    if (!captureSeed)
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, async k => seedSelector(k), async (acc, x) => accumulator(acc, x), equalityComparer)
                                : asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, async k => seedSelector(k), async (acc, x) => accumulator(acc, x))
                            : equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedSelector, accumulator, equalityComparer)
                                : asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedSelector, accumulator);
                    }
                    else
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedCapture, async (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, async (acc, x) => accumulator(acc, x), equalityComparer)
                                : asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedCapture, async (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, async (acc, x) => accumulator(acc, x))
                            : equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedCapture, (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulator, equalityComparer)
                                : asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedCapture, (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulator);
                    }
                }
                else
                {
                    if (!captureSeed)
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, async k => seedSelector(k), accumulateCapture, async (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                }, equalityComparer)
                                : asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, async k => seedSelector(k), accumulateCapture, async (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                })
                            : equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedSelector, accumulateCapture, (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                }, equalityComparer)
                                : asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedSelector, accumulateCapture, (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                });
                    }
                    else
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedCapture, async (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulateCapture, async (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                }, equalityComparer)
                                : asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedCapture, async (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulateCapture, async (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                })
                            : equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedCapture, (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulateCapture, (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                }, equalityComparer)
                                : asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedCapture, (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulateCapture, (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                });
                    }
                }
            }
        }

        private static AsyncEnumerable<KeyValuePair<TKey, TAccumulate>> AggregateBy<TSource, TKey, TAccumulate>(ConfiguredAsyncEnumerable<TSource> asyncEnumerable,
            bool async,
            bool captureKey,
            Func<TSource, TKey> keySelector,
            bool captureSeed,
            Func<TKey, TAccumulate> seedSelector,
            bool captureAccumulate,
            Func<TAccumulate, TSource, TAccumulate> accumulator,
            IEqualityComparer<TKey> equalityComparer = null)
        {
            const string keyCapture = "keyCapture";
            const string seedCapture = "seedCapture";
            const string accumulateCapture = "accumulateCapture";

            if (!captureKey)
            {
                if (!captureAccumulate)
                {
                    if (!captureSeed)
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.AggregateBy(async x => keySelector(x), async k => seedSelector(k), async (acc, x) => accumulator(acc, x), equalityComparer)
                                : asyncEnumerable.AggregateBy(async x => keySelector(x), async k => seedSelector(k), async (acc, x) => accumulator(acc, x))
                            : equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keySelector, seedSelector, accumulator, equalityComparer)
                                : asyncEnumerable.AggregateBy(keySelector, seedSelector, accumulator);
                    }
                    else
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.AggregateBy(async x => keySelector(x), seedCapture, async (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, async (acc, x) => accumulator(acc, x), equalityComparer)
                                : asyncEnumerable.AggregateBy(async x => keySelector(x), seedCapture, async (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, async (acc, x) => accumulator(acc, x))
                            : equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keySelector, seedCapture, (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulator, equalityComparer)
                                : asyncEnumerable.AggregateBy(keySelector, seedCapture, (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulator);
                    }
                }
                else
                {
                    if (!captureSeed)
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.AggregateBy(async x => keySelector(x), async k => seedSelector(k), accumulateCapture, async (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                }, equalityComparer)
                                : asyncEnumerable.AggregateBy(async x => keySelector(x), async k => seedSelector(k), accumulateCapture, async (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                })
                            : equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keySelector, seedSelector, accumulateCapture, (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                }, equalityComparer)
                                : asyncEnumerable.AggregateBy(keySelector, seedSelector, accumulateCapture, (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                });
                    }
                    else
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.AggregateBy(async x => keySelector(x), seedCapture, async (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulateCapture, async (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                }, equalityComparer)
                                : asyncEnumerable.AggregateBy(async x => keySelector(x), seedCapture, async (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulateCapture, async (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                })
                            : equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keySelector, seedCapture, (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulateCapture, (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                }, equalityComparer)
                                : asyncEnumerable.AggregateBy(keySelector, seedCapture, (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulateCapture, (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                });
                    }
                }
            }
            else
            {
                if (!captureAccumulate)
                {
                    if (!captureSeed)
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, async k => seedSelector(k), async (acc, x) => accumulator(acc, x), equalityComparer)
                                : asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, async k => seedSelector(k), async (acc, x) => accumulator(acc, x))
                            : equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedSelector, accumulator, equalityComparer)
                                : asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedSelector, accumulator);
                    }
                    else
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedCapture, async (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, async (acc, x) => accumulator(acc, x), equalityComparer)
                                : asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedCapture, async (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, async (acc, x) => accumulator(acc, x))
                            : equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedCapture, (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulator, equalityComparer)
                                : asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedCapture, (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulator);
                    }
                }
                else
                {
                    if (!captureSeed)
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, async k => seedSelector(k), accumulateCapture, async (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                }, equalityComparer)
                                : asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, async k => seedSelector(k), accumulateCapture, async (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                })
                            : equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedSelector, accumulateCapture, (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                }, equalityComparer)
                                : asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedSelector, accumulateCapture, (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                });
                    }
                    else
                    {
                        return async
                            ? equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedCapture, async (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulateCapture, async (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                }, equalityComparer)
                                : asyncEnumerable.AggregateBy(keyCapture, async (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedCapture, async (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulateCapture, async (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                })
                            : equalityComparer != null
                                ? asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedCapture, (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulateCapture, (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                }, equalityComparer)
                                : asyncEnumerable.AggregateBy(keyCapture, (cv, x) =>
                                {
                                    Assert.AreEqual(keyCapture, cv);
                                    return keySelector(x);
                                }, seedCapture, (cv, k) =>
                                {
                                    Assert.AreEqual(seedCapture, cv);
                                    return seedSelector(k);
                                }, accumulateCapture, (cv, acc, x) =>
                                {
                                    Assert.AreEqual(accumulateCapture, cv);
                                    return accumulator(acc, x);
                                });
                    }
                }
            }
        }

        private static IEqualityComparer<T> GetDefaultOrNullComparer<T>(bool defaultComparer)
        {
            return defaultComparer ? EqualityComparer<T>.Default : null;
        }

        [Test]
        public void AggregateBy_Empty(
            [Values] bool configured,
            [Values] bool async,
            // Reduce number of tests.
            [Values(false)] bool captureKey,
            [Values(false)] bool captureseed,
            [Values(false)] bool captureAccumulate,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var e = AggregateBy(AsyncEnumerable.Empty<int>(), configured, async,
                    captureKey, x => x,
                    captureseed, x => 0,
                    captureAccumulate, (acc, x) => acc,
                    equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateBy_Expected1(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureseed,
            [Values] bool captureAccumulate,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] {
                    new { Name = "Bart", Age = 27 },
                    new { Name = "John", Age = 62 },
                    new { Name = "Eric", Age = 27 },
                    new { Name = "Lisa", Age = 14 },
                    new { Name = "Brad", Age = 27 },
                    new { Name = "Lisa", Age = 23 },
                    new { Name = "Eric", Age = 42 },
                };

                int seed = 0;
                var e = AggregateBy(xs.ToAsyncEnumerable(), configured, async,
                    captureKey, x => x.Name,
                    captureseed, x => ++seed,
                    captureAccumulate, (acc, x) => acc + x.Age,
                    equalityComparer: GetDefaultOrNullComparer<string>(withComparer)).GetAsyncEnumerator();

                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<string, int>("Bart", 1 + 27), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<string, int>("John", 2 + 62), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<string, int>("Eric", 3 + 27 + 42), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<string, int>("Lisa", 4 + 14 + 23), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<string, int>("Brad", 5 + 27), e.Current);
                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateBy_Expected2(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureseed,
            [Values] bool captureAccumulate,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var xs = new[] {
                    new { Name = "Bart", Age = 27 },
                    new { Name = "John", Age = 62 },
                    new { Name = "Eric", Age = 27 },
                    new { Name = "Lisa", Age = 14 },
                    new { Name = "Brad", Age = 27 },
                    new { Name = "Lisa", Age = 23 },
                    new { Name = "Eric", Age = 42 },
                };

                var e = AggregateBy(xs.ToAsyncEnumerable(), configured, async,
                    captureKey, x => x.Age,
                    captureseed, x => x.ToString(),
                    captureAccumulate, (acc, x) => acc + x.Name,
                    equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();

                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, string>(27, "27" + "Bart" + "Eric" + "Brad"), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, string>(62, "62" + "John"), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, string>(14, "14" + "Lisa"), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, string>(23, "23" + "Lisa"), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, string>(42, "42" + "Eric"), e.Current);
                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateBy_Throws_Source(
            [Values] bool configured,
            [Values] bool async,
            // Reduce number of tests.
            [Values(false)] bool captureKey,
            [Values(false)] bool captureseed,
            [Values(false)] bool captureAccumulate,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var e = AggregateBy(AsyncEnumerable<int>.Rejected(ex), configured, async,
                    captureKey, x => x,
                    captureseed, x => 0,
                    captureAccumulate, (acc, x) => acc + x,
                    equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => e.MoveNextAsync(), ex);
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateBy_KeySelectorThrows(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            // Reduce number of tests.
            [Values(false)] bool captureseed,
            [Values(false)] bool captureAccumulate,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var e = AggregateBy(AsyncEnumerable.Return(42), configured, async,
                    captureKey, x => { throw ex; return x; },
                    captureseed, x => 0,
                    captureAccumulate, (acc, x) => acc + x,
                    equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => e.MoveNextAsync(), ex);
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateBy_AccumulatorThrows(
            [Values] bool configured,
            [Values] bool async,
            // Reduce number of tests.
            [Values(false)] bool captureKey,
            [Values(false)] bool captureseed,
            [Values] bool captureAccumulate,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var e = AggregateBy(AsyncEnumerable.Return(42), configured, async,
                    captureKey, x => x,
                    captureseed, x => 0,
                    captureAccumulate, (acc, x) => { throw ex; return acc + x; },
                    equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => e.MoveNextAsync(), ex);
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateBy_SeedSelectorThrows(
            [Values] bool configured,
            [Values] bool async,
            // Reduce number of tests.
            [Values(false)] bool captureKey,
            [Values] bool captureseed,
            [Values(false)] bool captureAccumulate,
            [Values] bool withComparer)
        {
            Promise.Run(async () =>
            {
                var ex = new Exception("Bang!");
                var e = AggregateBy(AsyncEnumerable.Return(42), configured, async,
                    captureKey, x => x,
                    captureseed, x => { throw ex; return 0; },
                    captureAccumulate, (acc, x) => acc + x,
                    equalityComparer: GetDefaultOrNullComparer<int>(withComparer)).GetAsyncEnumerator();
                await TestHelper.AssertThrowsAsync(() => e.MoveNextAsync(), ex);
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AggregateBy_Comparer_Simple(
            [Values] bool configured,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureseed,
            [Values] bool captureAccumulate)
        {
            Promise.Run(async () =>
            {
                int seed = 0;
                var e = AggregateBy(AsyncEnumerable.Range(0, 10), configured, async,
                    captureKey, x => x,
                    captureseed, x => seed += 10,
                    captureAccumulate, (acc, x) => acc + x,
                    equalityComparer: new EqMod(3)).GetAsyncEnumerator();
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, int>(0, 10 + 0 + 3 + 6 + 9), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, int>(1, 20 + 1 + 4 + 7), e.Current);
                Assert.True(await e.MoveNextAsync());
                Assert.AreEqual(new KeyValuePair<int, int>(2, 30 + 2 + 5 + 8), e.Current);
                Assert.False(await e.MoveNextAsync());
                await e.DisposeAsync();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }

        private sealed class EqMod : IEqualityComparer<int>
        {
            private readonly int _d;

            public EqMod(int d)
            {
                _d = d;
            }

            public bool Equals(int x, int y)
            {
                return EqualityComparer<int>.Default.Equals(x % _d, y % _d);
            }

            public int GetHashCode(int obj)
            {
                return EqualityComparer<int>.Default.GetHashCode(obj % _d);
            }
        }

        public enum ConfiguredType
        {
            NotConfigured,
            Configured,
            ConfiguredWithCancelation
        }

        [Test]
        public void AggregateBy_Cancel(
            [Values] ConfiguredType configuredType,
            [Values] bool async,
            [Values] bool captureKey,
            [Values] bool captureseed,
            [Values] bool captureAccumulate,
            [Values] bool withComparer,
            [Values] bool enumeratorToken)
        {
            Promise.Run(async () =>
            {
                var xs = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(1);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(2);
                    cancelationToken.ThrowIfCancelationRequested();
                    await writer.YieldAsync(3);
                });
                using (var configuredCancelationSource = CancelationSource.New())
                {
                    using (var enumeratorCancelationSource = CancelationSource.New())
                    {
                        var asyncEnumerator = AggregateBy(xs, configuredType != ConfiguredType.NotConfigured, async,
                            captureKey, x =>
                            {
                                if (x == 2)
                                {
                                    configuredCancelationSource.Cancel();
                                    enumeratorCancelationSource.Cancel();
                                }
                                return x;
                            },
                            captureseed, x => 0,
                            captureAccumulate, (acc, x) => acc + x,
                            equalityComparer: GetDefaultOrNullComparer<int>(withComparer),
                            configuredCancelationToken: configuredType == ConfiguredType.ConfiguredWithCancelation ? configuredCancelationSource.Token : CancelationToken.None)
                            .GetAsyncEnumerator(enumeratorToken ? enumeratorCancelationSource.Token : CancelationToken.None);
                        if (configuredType == ConfiguredType.ConfiguredWithCancelation || enumeratorToken)
                        {
                            await TestHelper.AssertCanceledAsync(() => asyncEnumerator.MoveNextAsync());
                        }
                        await asyncEnumerator.DisposeAsync();
                    }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
        }
    }
}