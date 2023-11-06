#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ProtoPromiseTests.APIs
{
#if !UNITY_WEBGL
    public sealed class ParallelForTests
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
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => { Promise.ParallelForEach((IEnumerable<int>) null, (item, cancelationToken) => Promise.Resolved()); });
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => { Promise.ParallelForEach(Enumerable.Range(1, 10), (item, cancelationToken) => Promise.Resolved(), maxDegreeOfParallelism: -2); });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => { Promise.ParallelForEach(Enumerable.Range(1, 10), null); });
        }
#endif

        private static IEnumerable<int> MarkStart(StrongBox<bool> box)
        {
            Assert.False(box.Value);
            box.Value = true;
            yield return 0;
        }

        [Test]
        public void PreCanceled_CancelsSynchronously()
        {
            var box = new StrongBox<bool>(false);
            var cts = CancelationSource.New();
            cts.Cancel();

            bool canceled = false;
            Promise.ParallelForEach(MarkStart(box), (item, cancelationToken) =>
            {
                Assert.Fail("Should not have been invoked");
                return Promise.Resolved();
            }, cts.Token)
                .CatchCancelation(() => canceled = true)
                .Forget();
            Assert.True(canceled);

            Assert.False(box.Value);
            cts.Dispose();
        }

        private static IEnumerable<int> IterateUntilSet(StrongBox<bool> box)
        {
            int counter = 0;
            while (!box.Value)
            {
                yield return counter++;
            }
        }

        [Test]
        public void Dop_WorkersCreatedRespectingLimit_Sync(
            [Values(-1, 1, 2, 4, 128)] int dop)
        {
            var box = new StrongBox<bool>(false);

            int activeWorkers = 0;
            var block = Promise.NewDeferred();
            var blockPromise = block.Promise.Preserve();

            Promise t = Promise.ParallelForEach(IterateUntilSet(box), (item, cancelationToken) =>
            {
                Interlocked.Increment(ref activeWorkers);
                return blockPromise;
            }, maxDegreeOfParallelism: dop);

            Thread.Sleep(20); // give the loop some time to run

            box.Value = true;
            block.Resolve();
            int maxWorkers = dop == -1 ? Environment.ProcessorCount : dop;
            t.WaitWithTimeout(TimeSpan.FromSeconds(maxWorkers));

            blockPromise.Forget();
            Assert.LessOrEqual(activeWorkers, maxWorkers);
        }

        private static IEnumerable<int> InfiniteZero()
        {
            while (true) yield return 0;
        }

        [Test]
        public void RunsAsynchronously_EvenForEntirelySynchronousWork_Sync()
        {
            var cts = CancelationSource.New();

            Promise.State state = Promise.State.Pending;
            Promise t = Promise.ParallelForEach(InfiniteZero(), (item, cancelationToken) => Promise.Resolved(), cts.Token)
                .ContinueWith(resultContainer => state = resultContainer.State);
            Assert.AreEqual(Promise.State.Pending, state);

            cts.Cancel();

            t.WaitWithTimeout(TimeSpan.FromSeconds(Environment.ProcessorCount));
            Assert.AreEqual(Promise.State.Canceled, state);

            cts.Dispose();
        }

        [Test]
        public void EmptySource_Sync()
        {
            int counter = 0;
            Promise.ParallelForEach(Enumerable.Range(0, 0), (item, cancelationToken) =>
            {
                Interlocked.Increment(ref counter);
                return Promise.Resolved();
            })
                .WaitWithTimeout(TimeSpan.FromSeconds(Environment.ProcessorCount));

            Assert.AreEqual(0, counter);
        }

        [Test]
        public void AllItemsEnumeratedOnce_Sync(
            [Values] bool yield)
        {
            const int Start = 10, Count = 100;

            var set = new HashSet<int>();

            Promise.ParallelForEach(Enumerable.Range(Start, Count), (item, cancelationToken) =>
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
                .WaitWithTimeout(TimeSpan.FromSeconds(Environment.ProcessorCount));

            for (int i = Start; i < Start + Count; i++)
            {
                Assert.True(set.Contains(i));
            }
        }

        [Test]
        public void AllItemsEnumeratedOnce_WithCaptureValue_Sync(
            [Values] bool yield)
        {
            const int Start = 10, Count = 100;

            string expectedCaptureValue = "Expected";

            var set = new HashSet<int>();

            Promise.ParallelForEach(Enumerable.Range(Start, Count), expectedCaptureValue, (item, cv, cancelationToken) =>
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
                .WaitWithTimeout(TimeSpan.FromSeconds(Environment.ProcessorCount));

            for (int i = Start; i < Start + Count; i++)
            {
                Assert.True(set.Contains(i));
            }
        }

        private IEnumerable<int> IterateAndAssertContext(SynchronizationType expectedContext, Thread mainThread)
        {
            TestHelper.AssertCallbackContext(expectedContext, expectedContext, mainThread);
            for (int i = 1; i <= 100; i++)
            {
                yield return i;
                TestHelper.AssertCallbackContext(expectedContext, expectedContext, mainThread);
            }
        }

        [Test]
        public void SynchronizationContext_AllCodeExecutedOnCorrectContext_Sync(
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

            Promise.ParallelForEach(IterateAndAssertContext(syncContext, mainThread), (item, cancelationToken) =>
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
            }, synchronizationContext: context)
                .Finally(() => isComplete = true)
                .Forget();

            if (!SpinWait.SpinUntil(() =>
            {
                TestHelper.ExecuteForegroundCallbacks();
                return isComplete;
            }, TimeSpan.FromSeconds(1)))
            {
                throw new TimeoutException();
            }

            CollectionAssert.AreEqual(Enumerable.Range(1, 100), cq.OrderBy(i => i));
        }

        private static IEnumerable<int> Infinite()
        {
            int i = 0;
            while (true)
            {
                yield return i++;
            }
        }

        [Test]
        public void Cancelation_CancelsIterationAndReturnsCanceledPromise_Sync()
        {
            var cts = CancelationSource.New();
            Promise.State state = Promise.State.Pending;
            Promise t = Promise.ParallelForEach(Infinite(), (item, cancelationToken) =>
            {
                return Promise.SwitchToContext(TestHelper._backgroundContext, forceAsync: true);
            }, cts.Token)
                .ContinueWith(resultContainer => state = resultContainer.State);

            Thread.Sleep(20); // give the loop some time to run
            Assert.AreEqual(Promise.State.Pending, state);
            cts.Cancel();

            t.WaitWithTimeout(TimeSpan.FromSeconds(Environment.ProcessorCount));
            Assert.AreEqual(Promise.State.Canceled, state);
            cts.Dispose();
        }

        [Test]
        public void Cancelation_SameTokenPassedToEveryInvocation_Sync()
        {
            var cq = new Queue<CancelationToken>();

            Promise.ParallelForEach(Enumerable.Range(1, 100), (item, cancelationToken) =>
            {
                lock (cq)
                {
                    cq.Enqueue(cancelationToken);
                }
                return Promise.Resolved();
            })
                .WaitWithTimeout(TimeSpan.FromSeconds(Environment.ProcessorCount));

            Assert.AreEqual(100, cq.Count);
            Assert.AreEqual(1, cq.Distinct().Count());
        }

        [Test]
        public void Exceptions_HavePriorityOverCancelation_Sync()
        {
            var deferred = Promise.NewDeferred();
            var promisePreserved = deferred.Promise.Preserve();
            var cts = CancelationSource.New();

            Exception expected = new Exception();
            Exception actual = null;

            Promise.ParallelForEach(Infinite(), (item, cancelationToken) =>
            {
                if (item == 0)
                {
                    return promisePreserved
                        .Then(() =>
                        {
                            cts.Cancel();
                            throw expected;
                        });
                }
                else
                {
                    deferred.TryResolve();
                    return Promise.Resolved();
                }
            }, cts.Token, maxDegreeOfParallelism: 2)
                .Catch((Exception e) => actual = e)
                .WaitWithTimeout(TimeSpan.FromSeconds(2));

            promisePreserved.Forget();
            cts.Dispose();

            Assert.IsInstanceOf<Proto.Promises.AggregateException>(actual);
            var aggregate = (Proto.Promises.AggregateException) actual;
            Assert.AreEqual(1, aggregate.InnerExceptions.Count);
            Assert.AreEqual(expected, aggregate.InnerException);
        }

        [Test]
        public void OperationCanceledException_CancelsParallelPromise()
        {
            var cts = CancelationSource.New();
            Promise.State state = Promise.State.Pending;

            Promise.ParallelForEach(Infinite(), (item, cancelationToken) =>
            {
                throw new OperationCanceledException();
            }, cts.Token)
                .ContinueWith(resultContainer => { state = resultContainer.State; })
                .WaitWithTimeout(TimeSpan.FromSeconds(Environment.ProcessorCount));

            cts.Dispose();
            Assert.AreEqual(Promise.State.Canceled, state);
        }

        private static IEnumerable<int> Iterate4ThenThrow(Exception e)
        {
            for (int i = 0; i < 10; i++)
            {
                if (i == 4)
                {
                    throw e;
                }
                yield return i;
            }
        }

        [Test]
        public void Exception_FromMoveNext_Sync()
        {
            Exception expected = new Exception();
            Exception actual = null;

            Promise.ParallelForEach(Iterate4ThenThrow(expected), (item, cancelationToken) => Promise.Resolved())
                .Catch((Exception e) => actual = e)
                .WaitWithTimeout(TimeSpan.FromSeconds(Environment.ProcessorCount));

            Assert.IsInstanceOf<Proto.Promises.AggregateException>(actual);
            var aggregate = (Proto.Promises.AggregateException) actual;
            Assert.AreEqual(1, aggregate.InnerExceptions.Count);
            Assert.AreEqual(expected, aggregate.InnerException);
        }

        private static IEnumerable<int> Iterate2()
        {
            yield return 1;
            yield return 2;
        }

        [Test]
        public void Exception_FromLoopBody_Sync()
        {
            Exception actual = null;

            var barrier = new Barrier(2);
            Promise.ParallelForEach(Iterate2(), (item, cancelationToken) =>
            {
                barrier.SignalAndWait();
                switch (item)
                {
                    case 1: throw new FormatException();
                    case 2: throw new InvalidTimeZoneException();
                    default: throw new Exception();
                }
            }, maxDegreeOfParallelism: barrier.ParticipantCount)
                .Catch((Exception e) => actual = e)
                .WaitWithTimeout(TimeSpan.FromSeconds(barrier.ParticipantCount));

            Assert.IsInstanceOf<Proto.Promises.AggregateException>(actual);
            var aggregate = (Proto.Promises.AggregateException) actual;

            Assert.AreEqual(2, aggregate.InnerExceptions.Count);
            Assert.IsTrue(aggregate.InnerExceptions.Any(e => e is FormatException));
            Assert.IsTrue(aggregate.InnerExceptions.Any(e => e is InvalidTimeZoneException));
        }

        [Test]
        public void Exception_ImplicitlyCancelsOtherWorkers_Sync()
        {
            Proto.Promises.AggregateException aggregateException = null;

            Promise.ParallelForEach(Infinite(), (item, cancelationToken) =>
            {
                if (item == 1000)
                {
                    throw new Exception();
                }
                return Promise.Resolved();
            })
                .Catch((Proto.Promises.AggregateException e) => aggregateException = e)
                .WaitWithTimeout(TimeSpan.FromSeconds(Environment.ProcessorCount));

            Assert.IsNotNull(aggregateException);
            aggregateException = null;

            Promise.ParallelForEach(Infinite(), (item, cancelationToken) =>
            {
                if (item == 0)
                {
                    throw new FormatException();
                }
                Assert.AreEqual(1, item);
                var deferred = Promise.NewDeferred();
                cancelationToken.Register(() => deferred.Resolve());
                return deferred.Promise;
            }, maxDegreeOfParallelism: 2)
                .Catch((Proto.Promises.AggregateException e) => aggregateException = e)
                .WaitWithTimeout(TimeSpan.FromSeconds(2));

            Assert.IsNotNull(aggregateException);
            aggregateException = null;
        }

        [Test]
        public void ParallelFor_AllIndicesEnumeratedOnce_Sync(
            [Values] bool yield)
        {
            const int Start = 10, Count = 100;

            var set = new HashSet<int>();

            Promise.ParallelFor(Start, Start + Count, (item, cancelationToken) =>
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
                .WaitWithTimeout(TimeSpan.FromSeconds(Environment.ProcessorCount));

            for (int i = Start; i < Start + Count; i++)
            {
                Assert.True(set.Contains(i));
            }
        }

        [Test]
        public void ParallelFor_AllIndicesEnumeratedOnce_WithCaptureValue_Sync(
            [Values] bool yield)
        {
            const int Start = 10, Count = 100;

            string expectedCaptureValue = "Expected";

            var set = new HashSet<int>();

            Promise.ParallelFor(Start, Start + Count, expectedCaptureValue, (item, cv, cancelationToken) =>
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
                .WaitWithTimeout(TimeSpan.FromSeconds(Environment.ProcessorCount));

            for (int i = Start; i < Start + Count; i++)
            {
                Assert.True(set.Contains(i));
            }
        }

#if CSHARP_7_3_OR_NEWER
        [Test]
        public void ParallelFor_ExecutionContextFlowsToWorkerBodies(
            [Values] bool foregroundContext)
        {
            Promise.Config.AsyncFlowExecutionContextEnabled = true;
            var context = foregroundContext
                ? (SynchronizationContext) TestHelper._foregroundContext
                : TestHelper._backgroundContext;

            var al = new AsyncLocal<int>();
            al.Value = 42;
            Promise.ParallelFor(0, 100, async (item, cancellationToken) =>
            {
                await Promise.SwitchToForegroundAwait(forceAsync: true);
                Assert.AreEqual(42, al.Value);
            })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(Environment.ProcessorCount));
        }

        private static IEnumerable<int> Iterate100()
        {
            for (int i = 0; i < 100; i++)
            {
                yield return i;
            }
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
            Promise.ParallelForEach(Iterate100(), async (item, cancellationToken) =>
            {
                await Promise.SwitchToForegroundAwait(forceAsync: true);
                Assert.AreEqual(42, al.Value);
            })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(Environment.ProcessorCount));
        }
#endif // CSHARP_7_3_OR_NEWER
    }
#endif // !UNITY_WEBGL
}