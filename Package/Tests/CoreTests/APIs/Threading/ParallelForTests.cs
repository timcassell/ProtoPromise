﻿#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ProtoPromise.Tests.APIs.Threading
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
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => { ParallelAsync.For(0, 1, null); });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => { ParallelAsync.For(0, 1, default, null); });

            Assert.Throws<Proto.Promises.ArgumentNullException>(() => { ParallelAsync.ForEach((IEnumerable<int>) null, (item, cancelationToken) => Promise.Resolved()); });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => { ParallelAsync.ForEach((IEnumerable<int>) null, default, (item, cancelationToken) => Promise.Resolved()); });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => { ParallelAsync.ForEach(Enumerable.Range(1, 10), null); });
            Assert.Throws<Proto.Promises.ArgumentNullException>(() => { ParallelAsync.ForEach(Enumerable.Range(1, 10), default, null); });
        }
#endif

        [Test]
        public void ParallelAsyncOptions_Invalid()
        {
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => { new ParallelAsyncOptions() { MaxDegreeOfParallelism = -2 }; });
        }

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
            ParallelAsync.ForEach(MarkStart(box), new ParallelAsyncOptions() { CancelationToken = cts.Token },
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
            [Values(0, 1, 2, 4, 128)] int dop)
        {
            var box = new StrongBox<bool>(false);

            int activeWorkers = 0;
            var block = Promise.NewDeferred();
            using (var blockPromiseRetainer = block.Promise.GetRetainer())
            {
                Promise t = ParallelAsync.ForEach(IterateUntilSet(box), new ParallelAsyncOptions() { MaxDegreeOfParallelism = dop },
                    (item, cancelationToken) =>
                    {
                        Interlocked.Increment(ref activeWorkers);
                        return blockPromiseRetainer.WaitAsync();
                    });

                Thread.Sleep(20); // give the loop some time to run

                box.Value = true;
                block.Resolve();
                int maxWorkers = dop == 0 ? Environment.ProcessorCount : dop;
                t.WaitWithTimeout(TimeSpan.FromSeconds(maxWorkers));

                Assert.LessOrEqual(activeWorkers, maxWorkers);
            }
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
            Promise t = ParallelAsync.ForEach(InfiniteZero(), new ParallelAsyncOptions() { CancelationToken = cts.Token },
                (item, cancelationToken) => Promise.Resolved())
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
            ParallelAsync.ForEach(Enumerable.Range(0, 0), (item, cancelationToken) =>
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

            ParallelAsync.ForEach(Enumerable.Range(Start, Count), (item, cancelationToken) =>
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

            ParallelAsync.ForEach(Enumerable.Range(Start, Count), expectedCaptureValue, (cv, item, cancelationToken) =>
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

            ParallelAsync.ForEach(IterateAndAssertContext(syncContext, mainThread), new ParallelAsyncOptions() { SynchronizationContext = context },
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

            TestHelper.SpinUntilWhileExecutingForegroundContext(() => isComplete, TimeSpan.FromSeconds(1));

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
            Promise t = ParallelAsync.ForEach(Infinite(), new ParallelAsyncOptions() { CancelationToken = cts.Token },
                (item, cancelationToken) =>
                {
                    return Promise.SwitchToContext(TestHelper._backgroundContext, forceAsync: true);
                })
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

            ParallelAsync.ForEach(Enumerable.Range(1, 100), (item, cancelationToken) =>
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
                    .WaitWithTimeout(TimeSpan.FromSeconds(2));

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

            ParallelAsync.ForEach(Iterate4ThenThrow(expected), (item, cancelationToken) => Promise.Resolved())
                .Catch((Exception e) => actual = e)
                .WaitWithTimeout(TimeSpan.FromSeconds(Environment.ProcessorCount));

            Assert.IsInstanceOf<AggregateException>(actual);
            var aggregate = (AggregateException) actual;
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
                .WaitWithTimeout(TimeSpan.FromSeconds(barrier.ParticipantCount));

            Assert.IsInstanceOf<AggregateException>(actual);
            var aggregate = (AggregateException) actual;

            Assert.AreEqual(2, aggregate.InnerExceptions.Count);
            Assert.IsTrue(aggregate.InnerExceptions.Any(e => e is FormatException));
            Assert.IsTrue(aggregate.InnerExceptions.Any(e => e is InvalidTimeZoneException));
        }

        [Test]
        public void Exception_ImplicitlyCancelsOtherWorkers_Sync()
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
                .WaitWithTimeout(TimeSpan.FromSeconds(Environment.ProcessorCount));

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

            ParallelAsync.For(Start, Start + Count, (item, cancelationToken) =>
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

            ParallelAsync.For(Start, Start + Count, expectedCaptureValue, (cv, item, cancelationToken) =>
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
            ParallelAsync.For(0, 100, new ParallelAsyncOptions() { SynchronizationContext = context },
                async (item, cancelationToken) =>
                {
                    await Promise.SwitchToForegroundAwait(forceAsync: true);
                    Assert.AreEqual(42, al.Value);
                    al.Value = 43;
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(Environment.ProcessorCount));
            Assert.AreEqual(42, al.Value);
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
            ParallelAsync.ForEach(Iterate100(), new ParallelAsyncOptions() { SynchronizationContext = context },
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
        public void ParallelFor_NotCanceledTooEarly(
            [Values] bool foregroundContext)
        {
            var context = foregroundContext
                ? (SynchronizationContext) TestHelper._foregroundContext
                : TestHelper._backgroundContext;

            ParallelAsync.For(0, Environment.ProcessorCount, new ParallelAsyncOptions() { SynchronizationContext = context },
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
        public void ParallelForEach_NotCanceledTooEarly(
            [Values] bool foregroundContext)
        {
            var context = foregroundContext
                ? (SynchronizationContext) TestHelper._foregroundContext
                : TestHelper._backgroundContext;

            ParallelAsync.ForEach(Enumerable.Range(0, Environment.ProcessorCount), new ParallelAsyncOptions() { SynchronizationContext = context },
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
        public void ParallelForEach_EnumeratorIsDisposedWhenComplete(
            [Values] bool foregroundContext)
        {
            var context = foregroundContext
                ? (SynchronizationContext) TestHelper._foregroundContext
                : TestHelper._backgroundContext;

            var enumerator = new EnumeratorDisposeChecker();
            ParallelAsync<int>.ForEach(enumerator, new ParallelAsyncOptions() { SynchronizationContext = context },
                (index, cancelationToken) => Promise.Resolved())
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(Environment.ProcessorCount));

            Assert.True(enumerator.disposed);
        }

        private class EnumeratorDisposeChecker : IEnumerator<int>
        {
            public bool disposed;

            public int Current => 0;
            object IEnumerator.Current => null;
            public bool MoveNext() => false;
            public void Dispose() => disposed = true;
            public void Reset() { }
        }

        [Test]
        public void ParallelForEach_EnumeratorIsNotMovedNextAfterItReturnsFalse(
            [Values] bool foregroundContext)
        {
            var context = foregroundContext
                ? (SynchronizationContext) TestHelper._foregroundContext
                : TestHelper._backgroundContext;

            ParallelAsync<int>.ForEach(new NoMoveNextEnumerator(), new ParallelAsyncOptions() { SynchronizationContext = context },
                async (index, cancelationToken) =>
                {
                    await System.Threading.Tasks.Task.Delay(10);
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(Environment.ProcessorCount));
        }

        private class NoMoveNextEnumerator : IEnumerator<int>
        {
            private int _current;
            private int _max = Environment.ProcessorCount;
            private bool _isComplete;

            public int Current => _current;
            object IEnumerator.Current => Current;
            public bool MoveNext()
            {
                Assert.False(_isComplete);
                if (_current < _max)
                {
                    ++_current;
                    return true;
                }
                _isComplete = true;
                return false;
            }
            public void Dispose() { }
            public void Reset() { }
        }

        [Test]
        public void ParallelFor_CancelationCallbackExceptionsArePropagated(
            [Values] bool foregroundContext)
        {
            var context = foregroundContext
                ? (SynchronizationContext) TestHelper._foregroundContext
                : TestHelper._backgroundContext;

            var deferred = Promise.NewDeferred();
            using (var blockPromiseRetainer = deferred.Promise.GetRetainer())
            {
                int readyCount = 0;

                var parallelPromise = ParallelAsync.For(0, 3, new ParallelAsyncOptions() { SynchronizationContext = context },
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

                var parallelPromise = ParallelAsync.ForEach(Enumerable.Range(0, 3), new ParallelAsyncOptions() { SynchronizationContext = context },
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
    }
#endif // !UNITY_WEBGL
}