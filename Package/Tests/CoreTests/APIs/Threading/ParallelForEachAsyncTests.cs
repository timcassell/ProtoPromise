#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Linq;
using Proto.Promises.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProtoPromiseTests.APIs.Threading
{
#if !UNITY_WEBGL
    public sealed class ParallelForEachTests
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

        private class StrongBox<T>
        {
            public T Value;

            public StrongBox(T value)
            {
                Value = value;
            }
        }

#if PROMISE_DEBUG
        [Test]
        public void InvalidArguments_ThrowsException()
        {
            Assert.Catch<System.ArgumentNullException>(() => { ParallelAsync.ForEach(default(AsyncEnumerable<int>), null); });
            Assert.Catch<System.ArgumentNullException>(() => { ParallelAsync.ForEach(default(AsyncEnumerable<int>), default, null); });
            Assert.Catch<System.ArgumentNullException>(() => { ParallelAsync.ForEach(default(AsyncEnumerable<int>), 1, null); });
            Assert.Catch<System.ArgumentNullException>(() => { ParallelAsync.ForEach(default(AsyncEnumerable<int>), default, 1, null); });
        }
#endif

        [Test]
        public void PreCanceled_CancelsSynchronously()
        {
            var box = new StrongBox<bool>(false);
            var cts = CancelationSource.New();
            cts.Cancel();

            bool canceled = false;
            ParallelAsync.ForEach(MarkStartAsync(box), new ParallelAsyncOptions() { CancelationToken = cts.Token },
                (item, cancelationToken) =>
                {
                    Assert.Fail("Should not have been invoked");
                    return Promise.Resolved();
                })
                .CatchCancelation(() => canceled = true)
                .Forget();
            Assert.True(canceled);

            Assert.False(box.Value);
            cts.Dispose();

            AsyncEnumerable<int> MarkStartAsync(StrongBox<bool> b)
            {
                return AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
                {
                    Assert.False(b.Value);
                    b.Value = true;
                    await writer.YieldAsync(0);
                });
            }
        }

        [Test]
        public void RunsAsynchronously_EvenForEntirelySynchronousWork_Async()
        {
            var cts = CancelationSource.New();

            var asyncEnumerable = AsyncEnumerable.Create<int>(async (writer, cancelationToken) =>
            {
                while (true) await writer.YieldAsync(0);
            });

            bool completed = false;
            bool canceled = false;
            var promise = ParallelAsync.ForEach(asyncEnumerable, new ParallelAsyncOptions() { CancelationToken = cts.Token },
                (item, cancelationToken) => Promise.Resolved())
                .Finally(() => completed = true)
                .CatchCancelation(() => canceled = true);
            Assert.False(completed);
            Assert.False(canceled);

            cts.Cancel();

            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(Environment.ProcessorCount));
            Assert.True(canceled);
            Assert.True(completed);
            cts.Dispose();
        }

        [Test]
        public void Dop_WorkersCreatedRespectingLimit_Async(
            [Values(0, 1, 2, 4, 128)] int dop)
        {
            var box = new StrongBox<bool>(false);
            var iterateUntilSetAsync = AsyncEnumerable<int>.Create(box, async (sBox, writer, cancelationToken) =>
            {
                int counter = 0;
                while (!sBox.Value)
                {
                    await writer.YieldAsync(counter++);
                }
            });

            int activeWorkers = 0;
            var block = Promise.NewDeferred();
            using (var blockPromiseRetainer = block.Promise.GetRetainer())
            {
                Promise t = ParallelAsync.ForEach(iterateUntilSetAsync, new ParallelAsyncOptions() { MaxDegreeOfParallelism = dop },
                    (item, cancelationToken) =>
                    {
                        Interlocked.Increment(ref activeWorkers);
                        return blockPromiseRetainer.WaitAsync();
                    });

                Thread.Sleep(20); // give the loop some time to run

                box.Value = true;
                block.Resolve();
                int maxWorkers = dop == 0 ? Environment.ProcessorCount : dop;
                t.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(maxWorkers));

                Assert.LessOrEqual(activeWorkers, maxWorkers);
            }
        }

        [Test]
        public void EmptySource_Async()
        {
            int counter = 0;
            ParallelAsync.ForEach(EnumerableRangeAsync(0, 0), (item, cancelationToken) =>
            {
                Interlocked.Increment(ref counter);
                return Promise.Resolved();
            })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(Environment.ProcessorCount));

            Assert.AreEqual(0, counter);
        }

        [Test]
        public void AllItemsEnumeratedOnce_Async(
            [Values] bool yield)
        {
            const int Start = 10, Count = 100;

            var set = new HashSet<int>();

            ParallelAsync.ForEach(EnumerableRangeAsync(Start, Count), (item, cancelationToken) =>
            {
                lock (set)
                {
                    Assert.True(set.Add(item));
                }

                if (yield)
                {
                    return Promise.SwitchToContext(TestHelper._backgroundContext, forceAsync: true);
                }
                return Promise.Resolved();
            })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(Environment.ProcessorCount));

            for (int i = Start; i < Start + Count; i++)
            {
                Assert.True(set.Contains(i));
            }
        }

        [Test]
        public void AllItemsEnumeratedOnce_WithCaptureValue_Async(
            [Values] bool yield)
        {
            const int Start = 10, Count = 100;

            string expectedCaptureValue = "Expected";

            var set = new HashSet<int>();

            ParallelAsync.ForEach(EnumerableRangeAsync(Start, Count), expectedCaptureValue, (cv, item, cancelationToken) =>
            {
                Assert.AreEqual(expectedCaptureValue, cv);
                lock (set)
                {
                    Assert.True(set.Add(item));
                }

                if (yield)
                {
                    return Promise.SwitchToContext(TestHelper._backgroundContext, forceAsync: true);
                }
                return Promise.Resolved();
            })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(Environment.ProcessorCount));

            for (int i = Start; i < Start + Count; i++)
            {
                Assert.True(set.Contains(i));
            }
        }

        private AsyncEnumerable<int> IterateAndAssertContext(SynchronizationType expectedContext, Thread mainThread, SynchronizationContext otherContext)
        {
            return AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
            {
                TestHelper.AssertCallbackContext(expectedContext, expectedContext, mainThread);
                for (int i = 1; i <= 100; i++)
                {
                    await Promise.SwitchToContextAwait(otherContext, forceAsync: true);
                    await writer.YieldAsync(i);
                    TestHelper.AssertCallbackContext(expectedContext, expectedContext, mainThread);
                }
            });
        }

        [Test]
        public void SynchronizationContext_AllCodeExecutedOnCorrectContext_Async(
            [Values(SynchronizationType.Foreground, SynchronizationType.Background)] SynchronizationType syncContext)
        {
            var mainThread = Thread.CurrentThread;
            SynchronizationContext context = syncContext == SynchronizationType.Foreground
                ? TestHelper._foregroundContext
                : (SynchronizationContext) TestHelper._backgroundContext;

            var otherContext = syncContext == SynchronizationType.Foreground
                ? (SynchronizationContext) TestHelper._backgroundContext
                : TestHelper._foregroundContext;

            var cq = new Queue<int>();
            bool isComplete = false;

            ParallelAsync.ForEach(IterateAndAssertContext(syncContext, mainThread, otherContext), new ParallelAsyncOptions() { SynchronizationContext = context },
                (item, cancelationToken) =>
                {
                    TestHelper.AssertCallbackContext(syncContext, syncContext, mainThread);
                    return Promise.SwitchToContext(context)
                        .Then(() =>
                        {
                            lock (cq)
                            {
                                cq.Enqueue(item);
                            }
                            if (item % 10 == 0)
                            {
                                return Promise.SwitchToContext(otherContext, forceAsync: true);
                            }
                            return Promise.Resolved();
                        });
                })
                .Finally(() => isComplete = true)
                .Forget();

            TestHelper.SpinUntilWhileExecutingForegroundContext(() => isComplete, TimeSpan.FromSeconds(10));

            CollectionAssert.AreEqual(Enumerable.Range(1, 100), cq.OrderBy(i => i));
        }

        private static AsyncEnumerable<int> Infinite()
        {
            return AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
            {
                int i = 0;
                while (true)
                {
                    await writer.YieldAsync(i++);
                }
            });
        }

        [Test]
        public void Cancelation_CancelsIterationAndReturnsCanceledPromise_Async()
        {
            var cts = CancelationSource.New();
            Promise.State state = Promise.State.Pending;
            Promise t = ParallelAsync.ForEach(Infinite(), new ParallelAsyncOptions() { CancelationToken = cts.Token },
                (item, cancelationToken) =>
                {
                    return Promise.SwitchToContext(TestHelper._backgroundContext, forceAsync: true);
                })
                .ContinueWith(resultContainer => state = resultContainer.State);

            Thread.Sleep(20); // give the loop some time to run
            Assert.AreEqual(Promise.State.Pending, state);
            cts.Cancel();

            t.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(Environment.ProcessorCount));
            Assert.AreEqual(Promise.State.Canceled, state);
            cts.Dispose();
        }

        [Test]
        public void Cancelation_CorrectTokenPassedToAsyncEnumerator()
        {
            var yieldTokenAsync = AsyncEnumerable<CancelationToken>.Create(async (writer, cancelationToken) =>
            {
                await Task.Yield();
                await writer.YieldAsync(cancelationToken);
            });

            ParallelAsync.ForEach(yieldTokenAsync, (item, cancelationToken) =>
            {
                Assert.AreEqual(cancelationToken, item);
                return Promise.Resolved();
            })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(Environment.ProcessorCount));
        }

        [Test]
        public void Cancelation_SameTokenPassedToEveryInvocation_Async()
        {
            var cq = new Queue<CancelationToken>();

            ParallelAsync.ForEach(EnumerableRangeAsync(1, 100), (item, cancelationToken) =>
            {
                lock (cq)
                {
                    cq.Enqueue(cancelationToken);
                }
                return Promise.Resolved();
            })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(Environment.ProcessorCount));

            Assert.AreEqual(100, cq.Count);
            Assert.AreEqual(1, cq.Distinct().Count());
        }

        [Test]
        public void Exceptions_HavePriorityOverCancelation_Async()
        {
            var deferred = Promise.NewDeferred();
            using (var promiseRetainer = deferred.Promise.GetRetainer())
            {
                var cts = CancelationSource.New();

                Exception expected = new Exception();
                Exception actual = null;

                ParallelAsync.ForEach(Infinite(), new ParallelAsyncOptions() { CancelationToken = cts.Token, MaxDegreeOfParallelism = 2 }, 
                    (item, cancelationToken) =>
                    {
                        if (item == 0)
                        {
                            return promiseRetainer.WaitAsync()
                                .Then(() =>
                                {
                                    cts.Cancel();
                                    throw expected;
                                });
                        }
                        else if (item == 10)
                        {
                            deferred.Resolve();
                        }
                        return Promise.Resolved();
                    })
                    .Catch((Exception e) => actual = e)
                    .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(2));
                cts.Dispose();

                Assert.IsInstanceOf<AggregateException>(actual);
                var aggregate = (AggregateException) actual;
                Assert.AreEqual(1, aggregate.InnerExceptions.Count);
                Assert.AreEqual(expected, aggregate.InnerException);
            }
        }

        [Test]
        public void OperationCanceledException_CancelsParallelPromise()
        {
            var cts = CancelationSource.New();
            Promise.State state = Promise.State.Pending;

            ParallelAsync.ForEach(Infinite(), new ParallelAsyncOptions() { CancelationToken = cts.Token }, 
                (item, cancelationToken) =>
                {
                    throw new OperationCanceledException();
                })
                .ContinueWith(resultContainer => { state = resultContainer.State; })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(Environment.ProcessorCount));

            cts.Dispose();
            Assert.AreEqual(Promise.State.Canceled, state);
        }

        private static AsyncEnumerable<int> Iterate4ThenThrow(Exception e)
        {
            return AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
            {
                await Task.Yield();
                for (int i = 0; i < 10; i++)
                {
                    if (i == 4)
                    {
                        throw e;
                    }
                    await writer.YieldAsync(i);
                }
            });
        }

        [Test]
        public void Exception_FromMoveNext_Async()
        {
            Exception expected = new Exception();
            Exception actual = null;

            ParallelAsync.ForEach(Iterate4ThenThrow(expected), (item, cancelationToken) => Promise.Resolved())
                .Catch((Exception e) => actual = e)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(Environment.ProcessorCount));

            Assert.IsInstanceOf<AggregateException>(actual);
            var aggregate = (AggregateException) actual;
            Assert.AreEqual(1, aggregate.InnerExceptions.Count);
            Assert.AreEqual(expected, aggregate.InnerException);
        }

        private static AsyncEnumerable<int> Iterate2()
        {
            return AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
            {
                await Task.Yield();
                await writer.YieldAsync(1);
                await writer.YieldAsync(2);
            });
        }

        [Test]
        public void Exception_FromLoopBody_Async()
        {
            Exception actual = null;

            var barrier = new Barrier(2);
            ParallelAsync.ForEach(Iterate2(), new ParallelAsyncOptions() { MaxDegreeOfParallelism = barrier.ParticipantCount },
                (item, cancelationToken) =>
                {
                    barrier.SignalAndWait();
                    switch (item)
                    {
                        case 1: throw new FormatException();
                        case 2: throw new InvalidTimeZoneException();
                        default: throw new Exception();
                    }
                })
                .Catch((Exception e) => actual = e)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(barrier.ParticipantCount));

            Assert.IsInstanceOf<AggregateException>(actual);
            var aggregate = (AggregateException) actual;

            Assert.AreEqual(2, aggregate.InnerExceptions.Count);
            Assert.IsTrue(aggregate.InnerExceptions.Any(e => e is FormatException));
            Assert.IsTrue(aggregate.InnerExceptions.Any(e => e is InvalidTimeZoneException));
        }

        [Test]
        public void Exception_ImplicitlyCancelsOtherWorkers_Async()
        {
            AggregateException aggregateException = null;

            ParallelAsync.ForEach(Infinite(), (item, cancelationToken) =>
            {
                if (item == 1000)
                {
                    throw new Exception();
                }
                return Promise.Resolved();
            })
                .Catch((AggregateException e) => aggregateException = e)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(Environment.ProcessorCount));

            Assert.IsNotNull(aggregateException);
            aggregateException = null;

            ParallelAsync.ForEach(Infinite(), new ParallelAsyncOptions() { MaxDegreeOfParallelism = 2 },
                (item, cancelationToken) =>
                {
                    if (item == 0)
                    {
                        throw new FormatException();
                    }
                    Assert.AreEqual(1, item);
                    var deferred = Promise.NewDeferred();
                    cancelationToken.Register(() => deferred.Resolve());
                    return deferred.Promise;
                })
                .Catch((AggregateException e) => aggregateException = e)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(2));

            Assert.IsNotNull(aggregateException);
            aggregateException = null;
        }

        [Test]
        public void ParallelForEach_ExecutionContextFlowsToWorkerBodies(
            [Values] bool foregroundContext)
        {
            Promise.Config.AsyncFlowExecutionContextEnabled = true;
            var context = foregroundContext
                ? (SynchronizationContext) TestHelper._foregroundContext
                : TestHelper._backgroundContext;

            var al = new AsyncLocal<int>();
            al.Value = 42;

            var asyncEnumerable = AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
            {
                for (int i = 0; i < 100; i++)
                {
                    await Task.Yield();
                    await writer.YieldAsync(i);
                }
            });

            ParallelAsync.ForEach(asyncEnumerable, new ParallelAsyncOptions() { SynchronizationContext = context }, 
                async (item, cancelationToken) =>
                {
                    await Promise.SwitchToForegroundAwait(forceAsync: true);
                    Assert.AreEqual(42, al.Value);
                    al.Value = 43;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(Environment.ProcessorCount));
            Assert.AreEqual(42, al.Value);
        }

        [Test]
        public void ParallelForEach_NotCanceledTooEarly(
            [Values] bool foregroundContext,
            [Values] bool yieldEnumerable)
        {
            var context = foregroundContext
                ? (SynchronizationContext) TestHelper._foregroundContext
                : TestHelper._backgroundContext;

            ParallelAsync.ForEach(EnumerableRangeAsync(0, Environment.ProcessorCount, yieldEnumerable), new ParallelAsyncOptions() { SynchronizationContext = context },
                async (index, cancelationToken) =>
                {
                    if (index % 2 == 0)
                    {
                        await System.Threading.Tasks.Task.Delay(100);
                    }
                    Assert.False(cancelationToken.IsCancelationRequested);
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(Environment.ProcessorCount));
        }

        [Test]
        public void ParallelForEach_CancelationCallbackExceptionsArePropagated(
            [Values] bool foregroundContext)
        {
            var context = foregroundContext
                ? (SynchronizationContext) TestHelper._foregroundContext
                : TestHelper._backgroundContext;

            var deferred = Promise.NewDeferred();
            using (var blockPromiseRetainer = deferred.Promise.GetRetainer())
            {
                int readyCount = 0;

                var parallelPromise = ParallelAsync.ForEach(AsyncEnumerable.Range(0, 3), new ParallelAsyncOptions() { SynchronizationContext = context },
                    (index, cancelationToken) =>
                    {
                        cancelationToken.Register(() => throw new Exception("Error in cancelation!"));
                        Interlocked.Increment(ref readyCount);
                        if (index == 2)
                        {
                            // Wait until all iterations are ready, otherwise the token could be canceled before a worker registered, causing it to throw synchronously.
                            TestHelper.SpinUntil(() => readyCount == 3, TimeSpan.FromSeconds(2));
                            throw new System.InvalidOperationException("Error in loop body!");
                        }
                        return blockPromiseRetainer.WaitAsync();
                    });

                TestHelper.SpinUntilWhileExecutingForegroundContext(() => readyCount == 3, TimeSpan.FromSeconds(3));

                bool didThrow = false;
                try
                {
                    deferred.Resolve();
                    parallelPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(Environment.ProcessorCount));
                }
                catch (AggregateException e)
                {
                    didThrow = true;
                    Assert.AreEqual(2, e.InnerExceptions.Count);
                    Assert.IsInstanceOf<System.InvalidOperationException>(e.InnerExceptions[0]);
                    Assert.IsInstanceOf<AggregateException>(e.InnerExceptions[1]);
                    Assert.AreEqual(3, ((AggregateException) e.InnerExceptions[1]).InnerExceptions.Count);
                }

                Assert.True(didThrow);
            }
        }

        private static AsyncEnumerable<int> EnumerableRangeAsync(int start, int count, bool yield = true)
        {
            return AsyncEnumerable<int>.Create((start, count, yield), async (cv, writer, cancelationToken) =>
            {
                for (int i = cv.start; i < cv.start + cv.count; i++)
                {
                    if (cv.yield)
                    {
                        await Promise.SwitchToBackgroundAwait(forceAsync: true);
                    }

                    await writer.YieldAsync(i);
                }
            });
        }
    }
#endif // !UNITY_WEBGL
}