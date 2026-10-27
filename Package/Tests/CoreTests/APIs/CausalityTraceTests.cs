#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Diagnostics;
using Proto.Promises;
using ProtoPromise.Tests.Concurrency;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ProtoPromise.Tests.APIs
{
    public class CausalityTraceTests
    {
        [SetUp]
        public void Setup()
        {
            TestHelper.Setup();
        }

        [TearDown]
        public void Teardown()
        {
            // Some tests continue on the thread pool, which may still be releasing objects after the test completes.
            TestHelper.Cleanup(spinForThreadPool: true);

            TestHelper.s_expectedUncaughtRejectValue = null;
        }

        private static int GetCausalityTraceDepth(CausalityTrace trace)
        {
            int depth = 0;
            for (; trace != null; trace = trace.Previous)
            {
                ++depth;
            }
            return depth;
        }

        [Test]
        public void CausalityTraceCapture_ReturnsNullWhenDisabled()
        {
            CausalityTrace.IsEnabled = false;
            Assert.IsNull(CausalityTrace.Capture());
            Assert.IsNull(CausalityTrace.Capture(0));
        }

        [Test]
        public void CausalityTraceCapture_ReturnsTraceStartingAtCallerWhenEnabled()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var trace = CausalityTrace.Capture();
                Assert.IsNotNull(trace);
#if !PROTO_PROMISE_DEVELOPER_MODE // Developer mode doesn't skip frames.
                Assert.AreEqual(nameof(CausalityTraceCapture_ReturnsTraceStartingAtCallerWhenEnabled), trace.StackTrace.GetFrame(0).GetMethod().Name);
#endif
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureSkippingFramesForTraceTest(int skipFrames)
        {
            var trace = CausalityTrace.Capture(skipFrames);
            Volatile.Write(ref s_frameKeeper, 28);
            return trace;
        }

        [Test]
        public void CausalityTraceCapture_WithSkipFrames_ReturnsTraceStartingThatManyFramesUpTheStack()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var traceAtHelper = CaptureSkippingFramesForTraceTest(0);
                var traceAtCaller = CaptureSkippingFramesForTraceTest(1);
                Assert.IsNotNull(traceAtHelper);
                Assert.IsNotNull(traceAtCaller);
#if !PROTO_PROMISE_DEVELOPER_MODE // Developer mode doesn't skip frames.
                Assert.AreEqual(nameof(CaptureSkippingFramesForTraceTest), traceAtHelper.StackTrace.GetFrame(0).GetMethod().Name);
                Assert.AreEqual(nameof(CausalityTraceCapture_WithSkipFrames_ReturnsTraceStartingThatManyFramesUpTheStack), traceAtCaller.StackTrace.GetFrame(0).GetMethod().Name);
#endif
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_WithTheLargestSkipFrames_ReturnsTraceWithoutFrames(
            [Values(int.MaxValue - 1, int.MaxValue)] int skipFrames)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var trace = CausalityTrace.Capture(skipFrames);
                Assert.IsNotNull(trace);
#if !PROTO_PROMISE_DEVELOPER_MODE // Developer mode doesn't skip frames.
                Assert.AreEqual(0, trace.StackTrace.FrameCount);
#endif
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if !PROMISE_DEBUG // Object pooling is always disabled in DEBUG mode.
        [Test]
        public void ObjectPooling_IsDisabledWhileCausalityTracesAreEnabled()
        {
            bool wasObjectPoolingEnabled = Promise.Config.ObjectPoolingEnabled;
            try
            {
                Promise.Config.ObjectPoolingEnabled = true;
                CausalityTrace.IsEnabled = true;
                Assert.IsFalse(Internal.IsObjectPoolingEnabled);

                // Enabling object pooling while causality traces are enabled doesn't enable it.
                Promise.Config.ObjectPoolingEnabled = false;
                Promise.Config.ObjectPoolingEnabled = true;
                Assert.IsFalse(Internal.IsObjectPoolingEnabled);

                CausalityTrace.IsEnabled = false;
                Assert.IsTrue(Internal.IsObjectPoolingEnabled);

                // Disabling causality traces doesn't enable object pooling if it was disabled.
                Promise.Config.ObjectPoolingEnabled = false;
                CausalityTrace.IsEnabled = true;
                CausalityTrace.IsEnabled = false;
                Assert.IsFalse(Internal.IsObjectPoolingEnabled);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.ObjectPoolingEnabled = wasObjectPoolingEnabled;
            }
        }
#endif

        [Test]
        public void CausalityTraceToString_PrintsTheChainOfPreviousTraces()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // The traces are captured in different methods, because optimized code may report the same location for calls in the same method,
                // which would make them repeats of each other (see CausalityTrace.Repeats).
                var traceA = CaptureAtOther();
                var scopeA = CausalityTrace.EnterScope(traceA);
                var traceB = CaptureAtStep();
                // B's callback runs synchronously inside A's callback.
                var scopeB = CausalityTrace.EnterScope(traceB);
                var traceX = CaptureAtLoop();
                scopeB.Dispose();
                scopeA.Dispose();

                // The previous trace is the innermost scope. A's callback is already on the stack.
                Assert.AreSame(traceB, traceX.Previous);
                Assert.AreSame(traceA, traceB.Previous);
                Assert.IsNull(traceA.Previous);
                Assert.IsNull(OuterTrace(traceX));
                CollectionAssert.IsEmpty(traceX.Repeats);

                var text = traceX.ToString();
                var lines = text.Split(new[] { System.Environment.NewLine }, System.StringSplitOptions.None);
                // The chain is X -> B -> A.
                Assert.AreEqual(2, lines.Count(l => l == "--- End of stack trace from the previous location ---"), text);
                Assert.IsFalse(lines.Any(l => l.StartsWith("    ")), text);
                AssertCausalityTraceIsPrintedCorrectly(traceX, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_IsMemoized()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var trace = CausalityTrace.Capture();
                Assert.AreSame(trace.ToString(), trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE // Developer mode doesn't hide internal frames.
        [Test]
        public void CausalityTraceGetFrames_ExcludesInternalFrames()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                CausalityTrace trace = null;
                var deferred = Promise.NewDeferred();
                deferred.Promise
                    .Then(() => trace = CausalityTrace.Capture())
                    .Forget();
                deferred.Resolve();

                var rawFrames = trace.StackTrace.GetFrames();
                var visibleFrames = trace.GetFrames();
                System.Func<System.Diagnostics.StackFrame, bool> isInternal = f => f.GetMethod()?.DeclaringType?.Namespace?.StartsWith("Proto.") == true;
                Assert.IsTrue(rawFrames.Any(isInternal), "The raw stack trace was expected to contain internal frames.");
                Assert.IsFalse(visibleFrames.Any(isInternal), string.Join(System.Environment.NewLine, visibleFrames.Where(isInternal).Select(f => f.GetMethod().DeclaringType + "." + f.GetMethod().Name)));
                Assert.Less(visibleFrames.Length, rawFrames.Length);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }
#endif

#if NET6_0_OR_GREATER // Building a stack trace from frames requires .Net 5+, and the runtime's StackTraceHiddenAttribute requires .Net 6+.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static System.Diagnostics.StackFrame GetVisibleFrameForTraceTest() => new System.Diagnostics.StackFrame(0, true);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining), System.Diagnostics.StackTraceHidden]
        private static System.Diagnostics.StackFrame GetHiddenFrameForTraceTest() => new System.Diagnostics.StackFrame(0, true);

        [Test]
        public void CausalityTraceFormatStackTrace_HidesTheOutermostFrameIfItIsAnnotated()
        {
            // Unlike the runtime's StackTrace.ToString(), the outermost frame is hidden like the other frames.
            var stackTrace = new System.Diagnostics.StackTrace(new[] { GetVisibleFrameForTraceTest(), GetHiddenFrameForTraceTest() });
            Assert.AreEqual(1, CausalityTrace.GetVisibleFrames(stackTrace).Length);
            var text = CausalityTrace.FormatStackTrace(stackTrace);
            StringAssert.Contains(nameof(GetVisibleFrameForTraceTest), text);
            StringAssert.DoesNotContain(nameof(GetHiddenFrameForTraceTest), text);
        }
#endif

        // Not inlined, so that its frame is in the stack trace, but marked for aggressive inlining, which the runtime's StackTrace.ToString() hides.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining | System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static CausalityTrace CaptureInAMethodMarkedForAggressiveInliningForTraceTest() => CausalityTrace.Capture();

        [Test]
        public void CausalityTraceGetFrames_ExcludesFramesOfMethodsMarkedForAggressiveInlining()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var trace = CaptureInAMethodMarkedForAggressiveInliningForTraceTest();

                const string methodName = nameof(CaptureInAMethodMarkedForAggressiveInliningForTraceTest);
#if !ENABLE_IL2CPP // IL2CPP's C++ compiler may inline it anyway.
                Assert.IsTrue(trace.StackTrace.GetFrames().Any(f => f.GetMethod()?.Name == methodName), trace.StackTrace.ToString());
#endif
                Assert.IsFalse(ContainsMethod(trace, methodName), trace.ToString());
                StringAssert.DoesNotContain(methodName, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceDebuggerDisplay_IsTheThreadAndTheInnermostFrame()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var trace = CaptureAtLeaf();
#if PROTO_PROMISE_DEVELOPER_MODE
                // Developer mode doesn't hide internal frames.
                string innermostMethod = $"{typeof(CausalityTrace).FullName}.{nameof(CausalityTrace.Capture)}";
#else
                string innermostMethod = $"{typeof(CausalityTraceTests).FullName}.{nameof(CaptureAtLeaf)}";
#endif

                StringAssert.StartsWith($"Thread {System.Threading.Thread.CurrentThread.ManagedThreadId} ", trace.DebuggerDisplay);
                StringAssert.EndsWith($" at {innermostMethod}", trace.DebuggerDisplay);
                // Unlike ToString(), it doesn't print the stack trace.
                StringAssert.DoesNotContain(System.Environment.NewLine, trace.DebuggerDisplay);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void RejectForReportTraceTest(Promise.Deferred deferred, System.Exception exception)
        {
            deferred.Reject(exception);
            Volatile.Write(ref s_frameKeeper, 1);
        }


        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void DisposeForReportTraceTest(Promise.Retainer retainer)
        {
            retainer.Dispose();
            Volatile.Write(ref s_frameKeeper, 2);
        }

        private static UnhandledException CaptureUnhandledException(System.Exception exception, System.Action reportUnhandledRejection)
        {
            UnhandledException unhandledException = null;
            var originalHandler = Promise.Config.UncaughtRejectionHandler;
            Promise.Config.UncaughtRejectionHandler = e => unhandledException = e;
            try
            {
                reportUnhandledRejection();
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = originalHandler;
            }
            Assert.IsNotNull(unhandledException);
#if PROTO_PROMISE_DEVELOPER_MODE
            // Developer mode wraps the reported exception in another one with the location it was reported.
            unhandledException = (UnhandledException) unhandledException.InnerException;
#endif
            Assert.AreSame(exception, unhandledException.Value);
            return unhandledException;
        }

        private static UnhandledException RejectAndReportLater(System.Exception exception)
            => CaptureUnhandledException(exception, () =>
            {
                // The rejection is reported when the retainer is disposed, which is far removed from where it was rejected.
                var deferred = Promise.NewDeferred();
                var retainer = deferred.Promise.GetRetainer();
                RejectForReportTraceTest(deferred, exception);
                DisposeForReportTraceTest(retainer);
            });

        // The deferred is created in its own method, so that its trace is not dropped like the frames of the test method that is still running
        // (see KeepsOuterTracesFromTheSameMethod), so that the tests fail if its scope is entered when it's completed.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise.Deferred NewVoidDeferredForTraceTest()
        {
            var deferred = Promise.NewDeferred();
            Volatile.Write(ref s_frameKeeper, 25);
            return deferred;
        }

        [Test]
        public void DeferredReject_RejectionTraceIsWhereItWasRejected()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var exception = new System.InvalidOperationException("Bang!");
                var trace = CaptureUnhandledException(exception, () =>
                {
                    var deferred = NewVoidDeferredForTraceTest();
                    deferred.Promise.Forget();
                    RejectForReportTraceTest(deferred, exception);
                }).CausalityTrace;

                // Completing a deferred doesn't enter its scope, so where it was created is not included (the code that rejected it holds it).
                var text = trace.ToString();
                StringAssert.Contains(nameof(RejectForReportTraceTest), trace.StackTrace.ToString(), text);
                Assert.IsFalse(HistoryContainsMethod(trace, nameof(NewVoidDeferredForTraceTest)), text);
                Assert.IsNull(trace.Previous, text);
                CollectionAssert.IsEmpty(trace.OuterTraces, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledExceptionMessage_Traced_SaysTheStackTraceIsTheCausalityTrace()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var exception = new System.InvalidOperationException("Bang!");
                var unhandledException = CaptureUnhandledException(exception, () => Promise.Rejected(exception).Forget());

                Assert.IsNotNull(unhandledException.CausalityTrace);
                Assert.AreEqual(unhandledException.CausalityTrace.ToString(), unhandledException.StackTrace);
                StringAssert.EndsWith(" -- This exception's StackTrace contains the causality trace of the rejection (the async operations that led to it).", unhandledException.Message);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledExceptionMessage_TracingDisabled_SaysHowToEnableIt()
        {
            var exception = new System.InvalidOperationException("Bang!");
            var unhandledException = CaptureUnhandledException(exception, () => Promise.Rejected(exception).Forget());

            Assert.IsNull(unhandledException.CausalityTrace);
            Assert.IsNull(unhandledException.StackTrace);
            StringAssert.EndsWith(" -- Set Proto.Diagnostics.CausalityTrace.IsEnabled = true to get a causality trace.", unhandledException.Message);
        }

        [Test]
        public void UnhandledExceptionMessage_TracingEnabledAfterTheRejection_SaysNoTraceWasFound()
        {
            var exception = new System.InvalidOperationException("Bang!");
            var promise = Promise.Rejected(exception);
            CausalityTrace.IsEnabled = true;
            try
            {
                var unhandledException = CaptureUnhandledException(exception, () => promise.Forget());

                Assert.IsNull(unhandledException.CausalityTrace);
#if PROMISE_DEBUG
                StringAssert.EndsWith(" -- No causality trace was captured. CausalityTrace.IsEnabled may have been set after the operation was started.", unhandledException.Message);
#else
                StringAssert.EndsWith(" -- No causality trace was found. Causality traces are not always available in RELEASE mode (for example, objects reported from finalizers on some runtimes). Enable DEBUG mode for reliable causality traces.", unhandledException.Message);
#endif
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void DeferredResolve_ContinuationsInvokedSynchronously_DoNotRunInsideTheDeferredScope()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = NewVoidDeferredForTraceTest();
                CausalityTrace callbackTrace = null;
                CausalityTrace callbackScope = null;
                var thenPromise = deferred.Promise.Then(() =>
                {
                    callbackTrace = CausalityTrace.Capture();
                    callbackScope = GetCurrentTraceForTraceTest();
                });
                var thenTrace = thenPromise._ref.Trace;
                thenPromise.Forget();

                deferred.Resolve();

                Assert.IsNotNull(thenTrace);
                // Completing a deferred doesn't enter its scope. What completed it is on the stack.
                Assert.AreSame(thenTrace, callbackScope);
                AssertPreviousTrace(thenTrace, callbackTrace);
                Assert.IsFalse(HistoryContainsMethod(callbackTrace, nameof(NewVoidDeferredForTraceTest)), callbackTrace.ToString());
                CollectionAssert.IsEmpty(callbackTrace.OuterTraces, callbackTrace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void DeferredResolve_CreatedWhileTracingIsDisabled_ContinuationsRunInsideTheirOwnScopes()
        {
            var deferred = Promise.NewDeferred();
            CausalityTrace.IsEnabled = true;
            try
            {
                CausalityTrace callbackTrace = null;
                CausalityTrace callbackScope = null;
                var thenPromise = deferred.Promise.Then(() =>
                {
                    callbackTrace = CausalityTrace.Capture();
                    callbackScope = GetCurrentTraceForTraceTest();
                });
                var thenTrace = thenPromise._ref.Trace;
                thenPromise.Forget();

                deferred.Resolve();

                Assert.IsNotNull(thenTrace);
                Assert.AreSame(thenTrace, callbackScope);
                AssertPreviousTrace(thenTrace, callbackTrace);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void DeferredResolveAndReject_CreatedWhileTracingIsEnabled_CompleteNormallyAfterTracingIsDisabled(
            [Values] bool reject)
        {
            CausalityTrace.IsEnabled = true;
            var deferred = Promise.NewDeferred();
            CausalityTrace.IsEnabled = false;

            bool completed = false;
            deferred.Promise
                .ContinueWith(result =>
                {
                    Assert.AreEqual(reject ? Promise.State.Rejected : Promise.State.Resolved, result.State);
                    completed = true;
                })
                .Forget();

            if (reject)
            {
                deferred.Reject("Rejected");
            }
            else
            {
                deferred.Resolve();
            }
            Assert.IsTrue(completed);
        }

        [Test]
        public void Finally_CallbackRunsInsideItsScope(
            [Values] bool reject,
            [Values] bool callbackReturnsPromise)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = Promise.NewDeferred();
                CausalityTrace callbackTrace = null;
                CausalityTrace callbackScope = null;
                var finallyPromise = callbackReturnsPromise
                    ? deferred.Promise.Finally(() =>
                    {
                        callbackTrace = CausalityTrace.Capture();
                        callbackScope = GetCurrentTraceForTraceTest();
                        return Promise.Resolved();
                    })
                    : deferred.Promise.Finally(() =>
                    {
                        callbackTrace = CausalityTrace.Capture();
                        callbackScope = GetCurrentTraceForTraceTest();
                    });
                var finallyTrace = finallyPromise._ref.Trace;
                finallyPromise
                    .Catch(() => { })
                    .Forget();

                if (reject)
                {
                    deferred.Reject("Rejected");
                }
                else
                {
                    deferred.Resolve();
                }

                Assert.IsNotNull(finallyTrace);
                Assert.AreSame(finallyTrace, callbackScope);
                AssertPreviousTrace(finallyTrace, callbackTrace);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void Catch_CallbackRunsInsideItsScope(
            [Values] bool callbackReturnsPromise)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = Promise.NewDeferred();
                CausalityTrace callbackTrace = null;
                CausalityTrace callbackScope = null;
                var catchPromise = callbackReturnsPromise
                    ? deferred.Promise.Catch(() =>
                    {
                        callbackTrace = CausalityTrace.Capture();
                        callbackScope = GetCurrentTraceForTraceTest();
                        return Promise.Resolved();
                    })
                    : deferred.Promise.Catch(() =>
                    {
                        callbackTrace = CausalityTrace.Capture();
                        callbackScope = GetCurrentTraceForTraceTest();
                    });
                var catchTrace = catchPromise._ref.Trace;
                catchPromise.Forget();

                deferred.Reject("Rejected");

                Assert.IsNotNull(catchTrace);
                Assert.AreSame(catchTrace, callbackScope);
                AssertPreviousTrace(catchTrace, callbackTrace);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ThrowRejectExceptionForTraceTest(System.Exception exception)
        {
            throw Promise.RejectException(exception);
        }

        [Test]
        public void RejectException_RejectionTraceIsWhereItWasThrown()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var exception = new System.InvalidOperationException("Bang!");
                CausalityTrace thenTrace = null;
                CausalityTrace thenScope = null;
                var trace = CaptureUnhandledException(exception, () =>
                {
                    var deferred = Promise.NewDeferred();
                    var thenPromise = deferred.Promise.Then(() =>
                    {
                        thenScope = GetCurrentTraceForTraceTest();
                        ThrowRejectExceptionForTraceTest(exception);
                    });
                    thenTrace = thenPromise._ref.Trace;
                    thenPromise.Forget();
                    deferred.Resolve();
                }).CausalityTrace;

                Assert.IsNotNull(thenTrace);
                Assert.AreSame(thenTrace, thenScope);
                StringAssert.Contains(nameof(ThrowRejectExceptionForTraceTest), trace.StackTrace.ToString());
                AssertPreviousTrace(thenTrace, trace);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        public enum TracedPromiseKind
        {
            Deferred,
            Async,
            Then,
            CatchPassThrough,
            CancelableContinueWith
        }

        private static async Promise TracedAsyncForSentinelTest(Promise promise, bool cancel)
        {
            await promise;
            if (cancel)
            {
                throw Promise.CancelException();
            }
        }

        // Creates a traced promise of the kind that stores a sentinel in the field that's shared with the RejectContainer,
        // and returns an action that completes it, resolved or canceled.
        private static Promise CreateTracedPromiseForSentinelTest(TracedPromiseKind kind, bool cancel, out System.Action complete)
        {
            var deferred = Promise.NewDeferred();
            switch (kind)
            {
                case TracedPromiseKind.Deferred:
                    complete = cancel ? (System.Action) deferred.Cancel : deferred.Resolve;
                    return deferred.Promise;
                case TracedPromiseKind.Async:
                    complete = deferred.Resolve;
                    return TracedAsyncForSentinelTest(deferred.Promise, cancel);
                case TracedPromiseKind.Then:
                    complete = deferred.Resolve;
                    return deferred.Promise.Then(() =>
                    {
                        if (cancel)
                        {
                            throw Promise.CancelException();
                        }
                    });
                case TracedPromiseKind.CatchPassThrough:
                    complete = cancel ? (System.Action) deferred.Cancel : deferred.Resolve;
                    return deferred.Promise.Catch(() => { });
                default:
                    var cancelationSource = CancelationSource.New();
                    var promise = deferred.Promise.ContinueWith(_ => { }, cancelationSource.Token);
                    complete = () =>
                    {
                        if (cancel)
                        {
                            cancelationSource.Cancel();
                        }
                        deferred.Resolve();
                        cancelationSource.Dispose();
                    };
                    return promise;
            }
        }

        [Test]
        public void PromiseEachGroup_DisposedWithTracedResultsRemaining_DoesNotTreatThemAsRejected(
            [Values] TracedPromiseKind kind,
            [Values] bool cancel)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var enumerator = PromiseEachGroup.New(out _)
                    .Add(CreateTracedPromiseForSentinelTest(kind, cancel, out var complete1))
                    .Add(CreateTracedPromiseForSentinelTest(kind, cancel, out var complete2))
                    .GetAsyncEnumerable()
                    .GetAsyncEnumerator();
                complete1();
                complete2();

                // Disposing with results remaining checks them for rejections.
                bool completed = false;
                enumerator.DisposeAsync()
                    .ContinueWith(result =>
                    {
                        Assert.AreEqual(Promise.State.Resolved, result.State);
                        completed = true;
                    })
                    .Forget();
                Assert.IsTrue(completed);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void PromiseNew_RejectInsideTheCallback_ContinuationRunsInsideTheCallbackScope()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var exception = new System.InvalidOperationException("Bang!");
                int scopeCount = 0;
                var trace = CaptureUnhandledException(exception, () =>
                {
                    Promise.New(deferred =>
                    {
                        // The continuation runs while the deferred is rejected, inside the callback's scope.
                        deferred.Promise
                            .Catch(() =>
                            {
                                scopeCount = CausalityTrace.ts_traces.Count;
                                throw Promise.Rethrow;
                            })
                            .Forget();
                        RejectForReportTraceTest(deferred, exception);
                    });
                }).CausalityTrace;

                // The scope of Promise.New's callback, which is still running, and the scope of the continuation.
                Assert.AreEqual(2, scopeCount);

#if PROTO_PROMISE_DEVELOPER_MODE // Developer mode doesn't hide internal frames, so the promise's trace is not contained by the trace.
                // The promise's trace is the cause, and it has no cause of its own.
                Assert.IsNotNull(trace.Previous);
                Assert.IsNull(trace.Previous.Previous);
#else
                // The promise's trace is skipped, because the call to Promise.New is on the stack, and it has no cause of its own.
                Assert.IsNull(trace.Previous);
#endif
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void AsyncPromise_ContinuationsRunInsideTheAsyncPromiseScope(
            [Values] bool configureFirstAwaitToForeground,
            [Values] bool flowExecutionContext,
            [Values] bool awaitTraces)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            // Enabled covers the ExecutionContext and ConfiguredAwaitDualContext paths.
            bool wasFlowExecutionContextEnabled = Promise.Config.AsyncFlowExecutionContextEnabled;
            Promise.Config.SetAsyncFlowExecutionContextEnabled(flowExecutionContext);
            Promise.Config.AwaitTracesEnabled = awaitTraces;
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred1 = NewVoidDeferredForTraceTest();
                var deferred2 = NewVoidDeferredForTraceTest();
                CausalityTrace traceAfterFirstAwait = null;
                CausalityTrace traceAfterSecondAwait = null;
                CausalityTrace firstAwaitTrace = null;
                Promise promise = default;

                async Promise Func()
                {
                    if (configureFirstAwaitToForeground)
                    {
                        await deferred1.Promise.ConfigureAwait(ContinuationOptions.Foreground);
                    }
                    else
                    {
                        await deferred1.Promise;
                    }
                    traceAfterFirstAwait = CausalityTrace.Capture();
                    firstAwaitTrace = promise._ref.AwaitTrace;
                    // If the first await resumed on a context, this checks that the async promise is still traced afterwards.
                    await deferred2.Promise;
                    traceAfterSecondAwait = CausalityTrace.Capture();
                }

                promise = Func();
                var asyncTrace = promise._ref.Trace;
                promise.Forget();

                deferred1.Resolve();
                TestHelper.ExecuteForegroundCallbacks();
                deferred2.Resolve();

                Assert.IsNotNull(asyncTrace);
                Assert.AreEqual(awaitTraces, firstAwaitTrace != null);
#if PROTO_PROMISE_DEVELOPER_MODE // Developer mode doesn't hide internal frames, so where it first awaited doesn't contain the async promise's trace.
                Assert.AreSame(asyncTrace, traceAfterFirstAwait.Previous);
#else
                if (!awaitTraces)
                {
                    Assert.AreSame(asyncTrace, traceAfterFirstAwait.Previous);
                }
                else
                {
                    // Where it first awaited contains where it was called, so the async promise's trace is skipped, and its cause is kept.
                    // Where it first awaited is printed in its place, unless it's dropped like the frames of this method (see KeepsOuterTracesFromTheSameMethod).
                    Assert.AreSame(asyncTrace.Previous, traceAfterFirstAwait.Previous, traceAfterFirstAwait.ToString());
                    if (KeepsOuterTracesFromTheSameMethod)
                    {
                        CollectionAssert.Contains(traceAfterFirstAwait.OuterTraces, firstAwaitTrace, traceAfterFirstAwait.ToString());
                    }
                }
#endif
                Assert.AreSame(asyncTrace, traceAfterSecondAwait.Previous);
                // Only the continuation that was scheduled on the context has where it was scheduled.
                // The context runs it from this method (see KeepsOuterTracesFromTheSameMethod).
                // A continuation that the deferred invoked synchronously has what completed the deferred on its stack.
                if (!configureFirstAwaitToForeground & !awaitTraces)
                {
                    CollectionAssert.IsEmpty(traceAfterFirstAwait.OuterTraces, traceAfterFirstAwait.ToString());
                }
                else if (KeepsOuterTracesFromTheSameMethod)
                {
                    // In developer mode, the optimized async mode's first await trace is the async promise's trace (see CausalityTrace.GetWithAllFrames), which is already the previous trace.
                    bool hasFirstAwaitTrace = firstAwaitTrace != null && firstAwaitTrace != asyncTrace;
                    Assert.AreEqual((configureFirstAwaitToForeground ? 1 : 0) + (hasFirstAwaitTrace ? 1 : 0), traceAfterFirstAwait.OuterTraces.Count, traceAfterFirstAwait.ToString());
                }
                // Otherwise it's dropped, unless IL2CPP's C++ linker folded Deferred.Resolve with an identical function,
                // whose name is printed in its place, so its frame is visible and it's not printed like the outer frames of the trace.
                // With await traces, the second await continues inside the scope of where it awaited, which has what continued the first await on its stack (in this method).
                if (!awaitTraces)
                {
                    CollectionAssert.IsEmpty(traceAfterSecondAwait.OuterTraces, traceAfterSecondAwait.ToString());
                }
                else if (KeepsOuterTracesFromTheSameMethod)
                {
                    Assert.IsNotNull(OuterTrace(traceAfterSecondAwait), traceAfterSecondAwait.ToString());
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.AwaitTracesEnabled = false;
                Promise.Config.SetAsyncFlowExecutionContextEnabled(wasFlowExecutionContextEnabled);
            }
        }

        [Test]
        public void AsyncPromise_AwaitedWhileTheExecutionContextFlowIsSuppressed_ContinuationRunsInsideTheAsyncPromiseScope()
        {
            // The async promise can't capture the ExecutionContext while its flow is suppressed, so it continues without one, still inside its scope.
            bool wasFlowExecutionContextEnabled = Promise.Config.AsyncFlowExecutionContextEnabled;
            Promise.Config.SetAsyncFlowExecutionContextEnabled(true);
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = NewVoidDeferredForTraceTest();
                CausalityTrace traceAfterAwait = null;

                async Promise Func()
                {
                    await deferred.Promise;
                    traceAfterAwait = CausalityTrace.Capture();
                }

                Promise promise;
                using (ExecutionContext.SuppressFlow())
                {
                    promise = Func();
                }
                var asyncTrace = promise._ref.Trace;
                promise.Forget();

                deferred.Resolve();

                Assert.IsNotNull(asyncTrace);
                Assert.IsNotNull(traceAfterAwait);
                Assert.AreSame(asyncTrace, traceAfterAwait.Previous, traceAfterAwait.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.SetAsyncFlowExecutionContextEnabled(wasFlowExecutionContextEnabled);
            }
        }

        [Test]
        public void AsyncIterator_ResumedByADeferred_RunsInsideItsScope_AndTheConsumerRunsInsideItsOwnScope()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = NewVoidDeferredForTraceTest();
                CausalityTrace iteratorScope = null;
                CausalityTrace consumerScope = null;
                var items = new List<int>();

                var enumerable = Proto.Promises.Linq.AsyncEnumerable<int>.Create(async (writer, cancelationToken) =>
                {
                    await writer.YieldAsync(1);
                    await deferred.Promise;
                    iteratorScope = GetCurrentTraceForTraceTest();
                    await writer.YieldAsync(2);
                });

                // The enumerator is used directly instead of await foreach, which requires C# 8.
                async Promise Consume()
                {
                    var enumerator = enumerable.GetAsyncEnumerator();
                    try
                    {
                        while (await enumerator.MoveNextAsync())
                        {
                            items.Add(enumerator.Current);
                        }
                        consumerScope = GetCurrentTraceForTraceTest();
                    }
                    finally
                    {
                        await enumerator.DisposeAsync();
                    }
                }

                var consumer = Consume();
                var consumerTrace = consumer._ref.Trace;
                consumer.Forget();
                CollectionAssert.AreEqual(new[] { 1 }, items);

                deferred.Resolve();

                CollectionAssert.AreEqual(new[] { 1, 2 }, items);
                Assert.IsNotNull(consumerTrace);
                Assert.AreSame(consumerTrace, consumerScope);
                // The iterator resumes in the scope of its own async function, which was started by the consumer.
                Assert.IsNotNull(iteratorScope);
                Assert.AreNotSame(consumerTrace, iteratorScope);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if PROMISE_DEBUG
        [Test]
        public void AsyncPromise_ResumedOnAContextWhileTracingIsDisabled_ContinuationsRunInsideTheAsyncPromiseScopeAfterItIsEnabledAgain()
        {
            // In DEBUG mode, the async promise keeps its trace while causality traces are disabled, so it has its trace again after they're enabled again.
            // Resuming on a context clears the field that marks it as traced, so this checks that the mark is restored while they're disabled.
            // (When the ExecutionContext flows, the field is the ExecutionContext instead, which is captured again at every await.)
            bool wasFlowExecutionContextEnabled = Promise.Config.AsyncFlowExecutionContextEnabled;
            Promise.Config.SetAsyncFlowExecutionContextEnabled(false);
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred1 = Promise.NewDeferred();
                var deferred2 = Promise.NewDeferred();
                CausalityTrace traceAfterSecondAwait = null;

                async Promise Func()
                {
                    await deferred1.Promise.ConfigureAwait(ContinuationOptions.Foreground);
                    await deferred2.Promise;
                    traceAfterSecondAwait = CausalityTrace.Capture();
                }

                var promise = Func();
                var asyncTrace = promise._ref.Trace;
                promise.Forget();

                CausalityTrace.IsEnabled = false;
                deferred1.Resolve();
                TestHelper.ExecuteForegroundCallbacks();
                CausalityTrace.IsEnabled = true;
                deferred2.Resolve();

                Assert.IsNotNull(asyncTrace);
                Assert.AreSame(asyncTrace, traceAfterSecondAwait.Previous);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.SetAsyncFlowExecutionContextEnabled(wasFlowExecutionContextEnabled);
            }
        }
#endif

        [Test]
        public void AsyncPromise_CreatedWhileTracingIsEnabled_CompletesNormallyAfterTracingIsDisabled(
            [Values] bool flowExecutionContext)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            // Disabled covers the path where the async promise resumes with its traced mark (and its trace is ignored) after tracing is disabled.
            bool wasFlowExecutionContextEnabled = Promise.Config.AsyncFlowExecutionContextEnabled;
            Promise.Config.SetAsyncFlowExecutionContextEnabled(flowExecutionContext);
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred1 = Promise.NewDeferred();
                var deferred2 = Promise.NewDeferred();
                bool resumed = false;

                async Promise Func()
                {
                    await deferred1.Promise;
                    await deferred2.Promise;
                    resumed = true;
                }

                var promise = Func();
                Assert.IsNotNull(promise._ref.Trace);
                bool completed = false;
                promise
                    .Then(() => completed = true)
                    .Forget();

                CausalityTrace.IsEnabled = false;
                deferred1.Resolve();
                deferred2.Resolve();

                Assert.IsTrue(resumed);
                Assert.IsTrue(completed);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.SetAsyncFlowExecutionContextEnabled(wasFlowExecutionContextEnabled);
            }
        }

        [Test]
        public void AsyncPromise_CreatedWhileTracingIsDisabled_ContinuationsRunWithoutAScope()
        {
            var deferred = Promise.NewDeferred();
            CausalityTrace traceAfterAwait = null;

            async Promise Func()
            {
                await deferred.Promise;
                traceAfterAwait = CausalityTrace.Capture();
            }

            var promise = Func();
            promise.Forget();

            CausalityTrace.IsEnabled = true;
            try
            {
                deferred.Resolve();

                Assert.IsNotNull(traceAfterAwait);
                // Neither the async promise nor the deferred was traced, so there are no scopes.
                Assert.IsNull(traceAfterAwait.Previous);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void RejectChannelForTraceTest(Proto.Promises.Channels.Channel<int> channel, System.Exception exception)
        {
            channel.Writer.TryReject(exception);
            Volatile.Write(ref s_frameKeeper, 3);
        }

        // The channel is created in its own method, so that its trace is not dropped like the frames of the test method that is still running
        // (see KeepsOuterTracesFromTheSameMethod), so that the test fails if its scope is entered when it's completed.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Proto.Promises.Channels.Channel<int> NewChannelForRejectTraceTest()
        {
            var channel = Proto.Promises.Channels.Channel<int>.NewUnbounded();
            Volatile.Write(ref s_frameKeeper, 26);
            return channel;
        }

        [Test]
        public void ChannelReject_RejectionTraceIsWhereItWasRejected()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var exception = new System.InvalidOperationException("Bang!");
                var channel = NewChannelForRejectTraceTest();
                try
                {
                    RejectChannelForTraceTest(channel, exception);
                    // Reading from a rejected channel resolves with the reason rather than rejecting, so report the channel's rejection directly.
                    var trace = CaptureUnhandledException(exception, () => ((Internal.IRejectContainer) channel._ref._closedReason).ReportUnhandled())
                        .CausalityTrace;

                    // Completing a channel doesn't enter its scope, so where it was created is not included (the code that rejected it holds it).
                    var text = trace.ToString();
                    StringAssert.Contains(nameof(RejectChannelForTraceTest), trace.StackTrace.ToString(), text);
                    Assert.IsFalse(HistoryContainsMethod(trace, nameof(NewChannelForRejectTraceTest)), text);
                    Assert.IsNull(trace.Previous, text);
                    CollectionAssert.IsEmpty(trace.OuterTraces, text);
                }
                finally
                {
                    channel.Dispose();
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        // Asserts that the callback ran inside the scope of the operation that was started by the method (one of the ...ForTraceTest methods).
        private static void AssertRanInsideTheScopeStartedBy(CausalityTrace callbackTrace, string methodName)
        {
            Assert.IsNotNull(callbackTrace);
            Assert.IsNotNull(callbackTrace.Previous);
            StringAssert.Contains(methodName, callbackTrace.Previous.StackTrace.ToString());
        }

        // Like AssertRanInsideTheScopeStartedBy, for a callback that the method invoked synchronously, where scopeTrace is the current trace when the callback ran.
        // The operation's trace is skipped as the callback trace's previous trace, because the method is on the callback's stack,
        // except in developer mode, which doesn't hide internal frames.
        private static void AssertRanSynchronouslyInsideTheScopeStartedBy(CausalityTrace callbackTrace, CausalityTrace scopeTrace, string methodName)
        {
            Assert.IsNotNull(callbackTrace);
            Assert.IsNotNull(scopeTrace);
            StringAssert.Contains(methodName, scopeTrace.StackTrace.ToString());
#if PROTO_PROMISE_DEVELOPER_MODE
            Assert.AreSame(scopeTrace, callbackTrace.Previous);
#else
            // The scope's trace is skipped, because it's printed like the outer frames of the callback's trace,
            // unless one of its hidden frames is printed with another function's name (see MayPrintHiddenFramesWithOtherNames).
            if (!MayPrintHiddenFramesWithOtherNames || callbackTrace.Previous != scopeTrace)
            {
                Assert.AreSame(scopeTrace.Previous, callbackTrace.Previous);
            }
            StringAssert.Contains(methodName, callbackTrace.StackTrace.ToString());
#endif
        }

        private static CausalityTrace GetCurrentTraceForTraceTest()
        {
            var traces = CausalityTrace.ts_traces;
            return traces == null || traces.Count == 0 ? null : traces.Peek();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void RegisterForTraceTest(CancelationToken cancelationToken, CausalityTrace[] callbackTrace)
        {
            cancelationToken.Register(() => callbackTrace[0] = CausalityTrace.Capture());
            Volatile.Write(ref s_frameKeeper, 4);
        }

        [Test]
        public void CancelationCallback_RunsInsideTheScopeOfWhereItWasRegistered()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var cancelationSource = CancelationSource.New();
                var callbackTrace = new CausalityTrace[1];
                RegisterForTraceTest(cancelationSource.Token, callbackTrace);

                cancelationSource.Cancel();
                cancelationSource.Dispose();

                AssertRanInsideTheScopeStartedBy(callbackTrace[0], nameof(RegisterForTraceTest));
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        // This is how other async method builders (e.g. async Task) await a promise.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void OnCompletedForTraceTest(Promise promise, bool configured, CausalityTrace[] continuationTrace)
        {
            if (configured)
            {
                var awaiter = promise.ConfigureAwait(ContinuationOptions.Foreground).GetAwaiter();
                awaiter.OnCompleted(() =>
                {
                    continuationTrace[0] = CausalityTrace.Capture();
                    awaiter.GetResult();
                });
            }
            else
            {
                var awaiter = promise.GetAwaiter();
                awaiter.OnCompleted(() =>
                {
                    continuationTrace[0] = CausalityTrace.Capture();
                    awaiter.GetResult();
                });
            }
            Volatile.Write(ref s_frameKeeper, 5);
        }

        [Test]
        public void AwaiterContinuation_DoesNotRunInsideTheScopeOfWhereItWasAwaited(
            [Values] bool configured)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = NewVoidDeferredForTraceTest();
                var continuationTrace = new CausalityTrace[1];
                OnCompletedForTraceTest(deferred.Promise, configured, continuationTrace);

                ResolveForTraceTest(deferred);
                TestHelper.ExecuteForegroundCallbacks();

                // The awaiting function isn't traced, so where it awaited isn't captured, the same as an async Promise function without await traces.
                var text = continuationTrace[0].ToString();
                Assert.IsFalse(HistoryContainsMethod(continuationTrace[0], nameof(OnCompletedForTraceTest)), text);
                CollectionAssert.IsEmpty(continuationTrace[0].OuterTraces, text);
                // Only the continuation that was scheduled on the context has where it was scheduled from, which is what completed the deferred.
                // The continuation that the deferred invoked synchronously has what completed the deferred on its stack.
                if (configured)
                {
                    Assert.IsTrue(HistoryContainsMethod(continuationTrace[0], nameof(ResolveForTraceTest)), text);
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        // NoInlining, so that where it was called is not printed like the frames of the test method that is still running when it completes
        // (see KeepsOuterTracesFromTheSameMethod), so that the tests fail if its scope is not exited.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Promise AwaitForTraceTest(Promise promise)
        {
            await promise;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE // Developer mode doesn't hide internal frames.
        [Test]
        public void AsyncPromiseTrace_StartsAtTheAsyncFunctionWhereItWasCalled(
            [Values] bool flowExecutionContext,
            [Values] bool awaitTraces)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            // Enabled runs the async function with the runtime's async method builder, whose frames are between the async function and where it was called.
            bool wasFlowExecutionContextEnabled = Promise.Config.AsyncFlowExecutionContextEnabled;
            Promise.Config.SetAsyncFlowExecutionContextEnabled(flowExecutionContext);
            Promise.Config.AwaitTracesEnabled = awaitTraces;
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = Promise.NewDeferred();
                var promise = AwaitForTraceTest(deferred.Promise);
                var asyncTrace = promise._ref.Trace;
                var awaitTrace = promise._ref.AwaitTrace;
                promise.Forget();
                deferred.Resolve();

                // Whether it's captured before the async function's body runs (non-optimized mode) or where it first awaits (optimized mode),
                // it starts at the method that the compiler generated for the async function, followed by where it was called.
                var frames = asyncTrace.GetFrames();
                Assert.AreEqual(nameof(AwaitForTraceTest), frames[0].GetMethod().Name, asyncTrace.ToString());
                Assert.AreEqual(nameof(AsyncPromiseTrace_StartsAtTheAsyncFunctionWhereItWasCalled), frames[1].GetMethod().Name, asyncTrace.ToString());
                if (awaitTraces)
                {
                    // Where it first awaited starts at the async function's state machine, where it awaited.
                    var awaitFrames = awaitTrace.GetFrames();
                    Assert.AreEqual("MoveNext", awaitFrames[0].GetMethod().Name, awaitTrace.ToString());
                    StringAssert.Contains(nameof(AwaitForTraceTest), awaitFrames[0].GetMethod().DeclaringType.Name, awaitTrace.ToString());
                }
                else
                {
                    Assert.IsNull(awaitTrace);
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.AwaitTracesEnabled = false;
                Promise.Config.SetAsyncFlowExecutionContextEnabled(wasFlowExecutionContextEnabled);
            }
        }

        [Test]
        public void CausalityTraceCapture_AfterContinuingOnTheExecutionContext_HidesExecutionContextRunOnlyIfProtoPromiseCalledIt()
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            bool wasFlowExecutionContextEnabled = Promise.Config.AsyncFlowExecutionContextEnabled;
            Promise.Config.SetAsyncFlowExecutionContextEnabled(true);
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = Promise.NewDeferred();
                CausalityTrace asyncTrace = null;
                CausalityTrace userTrace = null;
                async Promise Func()
                {
                    await deferred.Promise;
                    asyncTrace = CausalityTrace.Capture();
                }
                Func().Forget();
                deferred.Resolve();
                System.Threading.ExecutionContext.Run(System.Threading.ExecutionContext.Capture(), _ => userTrace = CausalityTrace.Capture(), null);

                // Only the frames above this method are checked, because the thread that runs it may have started it with ExecutionContext.Run too.
                bool HasExecutionContextFrame(System.Diagnostics.StackFrame[] frames) => frames
                    .TakeWhile(frame => frame.GetMethod().Name != nameof(CausalityTraceCapture_AfterContinuingOnTheExecutionContext_HidesExecutionContextRunOnlyIfProtoPromiseCalledIt))
                    .Any(frame => frame.GetMethod().DeclaringType == typeof(System.Threading.ExecutionContext));
                // The async function continued on its ExecutionContext with ExecutionContext.Run, which is hidden like the code that called it.
                Assert.IsTrue(HasExecutionContextFrame(asyncTrace.StackTrace.GetFrames()), asyncTrace.StackTrace.ToString());
                Assert.IsFalse(HasExecutionContextFrame(asyncTrace.GetFrames()), asyncTrace.ToString());
                // It's not hidden when other code calls it.
                Assert.IsTrue(HasExecutionContextFrame(userTrace.GetFrames()), userTrace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.SetAsyncFlowExecutionContextEnabled(wasFlowExecutionContextEnabled);
            }
        }
#endif // !PROTO_PROMISE_DEVELOPER_MODE

        // Whether the pending async promise is in the optimized async mode, which creates it where its function first awaits (see AsyncInternal.cs).
        // It depends on how the library was built (e.g. its .Net Standard 2.0 build has the IL2CPP workarounds), so it's checked at runtime.
        private static bool IsInOptimizedAsyncMode(Promise promise)
            => promise._ref.GetType().Name.Contains("Machine");

        public enum CallbackKindForTraceTest
        {
            Then,
            ThenReturningPromise,
            ThenReturningPromiseT,
            ThenOnRejected,
            ThenOnRejectedReturningPromise,
            Catch,
            CatchReturningPromise,
            ContinueWith,
            ContinueWithReturningPromise,
            Finally,
            FinallyReturningPromise
        }

        [Test]
        public void Callback_AsyncFunctionCompletesSynchronously_RunsOutsideTheAsyncFunctionScope(
            [Values] CallbackKindForTraceTest kind,
            [Values] bool flowExecutionContext)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            bool wasFlowExecutionContextEnabled = Promise.Config.AsyncFlowExecutionContextEnabled;
            Promise.Config.SetAsyncFlowExecutionContextEnabled(flowExecutionContext);
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = Promise.NewDeferred();
                var asyncPromise = AwaitForTraceTest(deferred.Promise);
                CausalityTrace callbackTrace = null;
                CausalityTrace waiterTrace = null;
                Promise next;
                switch (kind)
                {
                    case CallbackKindForTraceTest.Then:
                        next = asyncPromise.Then(() => { callbackTrace = CausalityTrace.Capture(); });
                        break;
                    case CallbackKindForTraceTest.ThenReturningPromise:
                        next = asyncPromise.Then(() => { callbackTrace = CausalityTrace.Capture(); return Promise.Resolved(); });
                        break;
                    case CallbackKindForTraceTest.ThenReturningPromiseT:
                        next = asyncPromise.Then(() => { callbackTrace = CausalityTrace.Capture(); return Promise.Resolved(1); });
                        break;
                    case CallbackKindForTraceTest.ThenOnRejected:
                        next = asyncPromise.Then(() => { }, () => { callbackTrace = CausalityTrace.Capture(); });
                        break;
                    case CallbackKindForTraceTest.ThenOnRejectedReturningPromise:
                        next = asyncPromise.Then(() => Promise.Resolved(), () => { callbackTrace = CausalityTrace.Capture(); return Promise.Resolved(); });
                        break;
                    case CallbackKindForTraceTest.Catch:
                        next = asyncPromise.Catch(() => { callbackTrace = CausalityTrace.Capture(); });
                        break;
                    case CallbackKindForTraceTest.CatchReturningPromise:
                        next = asyncPromise.Catch(() => { callbackTrace = CausalityTrace.Capture(); return Promise.Resolved(); });
                        break;
                    case CallbackKindForTraceTest.ContinueWith:
                        next = asyncPromise.ContinueWith(_ => { callbackTrace = CausalityTrace.Capture(); });
                        break;
                    case CallbackKindForTraceTest.ContinueWithReturningPromise:
                        next = asyncPromise.ContinueWith(_ => { callbackTrace = CausalityTrace.Capture(); return Promise.Resolved(); });
                        break;
                    case CallbackKindForTraceTest.Finally:
                        next = asyncPromise.Finally(() => { callbackTrace = CausalityTrace.Capture(); });
                        break;
                    default:
                        next = asyncPromise.Finally(() => { callbackTrace = CausalityTrace.Capture(); return Promise.Resolved(); });
                        break;
                }
                next.Then(() => { waiterTrace = CausalityTrace.Capture(); }).Forget();

                if (kind == CallbackKindForTraceTest.ThenOnRejected || kind == CallbackKindForTraceTest.ThenOnRejectedReturningPromise
                    || kind == CallbackKindForTraceTest.Catch || kind == CallbackKindForTraceTest.CatchReturningPromise)
                {
                    deferred.Reject(new System.InvalidOperationException("Bang!"));
                }
                else
                {
                    deferred.Resolve();
                }

                // The async function completed inside its own scope, but its scope was exited while the callback's promise completed,
                // so neither the callback nor its waiters run inside it.
                Assert.IsNotNull(callbackTrace);
                Assert.IsNotNull(waiterTrace);
                CollectionAssert.IsEmpty(callbackTrace.OuterTraces, callbackTrace.ToString());
                CollectionAssert.IsEmpty(waiterTrace.OuterTraces, waiterTrace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.SetAsyncFlowExecutionContextEnabled(wasFlowExecutionContextEnabled);
            }
        }

        [Test]
        public void Callback_ReturnsAPendingPromise_ItsScopeIsExitedWhenItReturns(
            [Values(CallbackKindForTraceTest.ThenReturningPromise, CallbackKindForTraceTest.ThenReturningPromiseT, CallbackKindForTraceTest.ThenOnRejectedReturningPromise,
                CallbackKindForTraceTest.CatchReturningPromise, CallbackKindForTraceTest.ContinueWithReturningPromise, CallbackKindForTraceTest.FinallyReturningPromise)]
            CallbackKindForTraceTest kind,
            // A promise with a result runs other callback implementations (e.g. Catch returns a Promise<T>).
            [Values] bool previousHasResult)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Only one of them is used.
                var deferred = default(Promise.Deferred);
                var deferredT = default(Promise<int>.Deferred);
                var returnedDeferred = Promise.NewDeferred<int>();
                bool isInvoked = false;
                System.Func<Promise<int>> callback = () =>
                {
                    isInvoked = true;
                    return returnedDeferred.Promise;
                };
                Promise next;
                if (!previousHasResult)
                {
                    deferred = Promise.NewDeferred();
                    switch (kind)
                    {
                        case CallbackKindForTraceTest.ThenReturningPromise:
                            next = deferred.Promise.Then(() => (Promise) callback());
                            break;
                        case CallbackKindForTraceTest.ThenReturningPromiseT:
                            next = deferred.Promise.Then(callback);
                            break;
                        case CallbackKindForTraceTest.ThenOnRejectedReturningPromise:
                            next = deferred.Promise.Then(() => Promise.Resolved(), () => (Promise) callback());
                            break;
                        case CallbackKindForTraceTest.CatchReturningPromise:
                            next = deferred.Promise.Catch(() => (Promise) callback());
                            break;
                        case CallbackKindForTraceTest.ContinueWithReturningPromise:
                            next = deferred.Promise.ContinueWith(_ => (Promise) callback());
                            break;
                        default:
                            next = deferred.Promise.Finally(() => (Promise) callback());
                            break;
                    }
                }
                else
                {
                    deferredT = Promise.NewDeferred<int>();
                    switch (kind)
                    {
                        case CallbackKindForTraceTest.ThenReturningPromise:
                            next = deferredT.Promise.Then(_ => (Promise) callback());
                            break;
                        case CallbackKindForTraceTest.ThenReturningPromiseT:
                            next = deferredT.Promise.Then(_ => callback());
                            break;
                        case CallbackKindForTraceTest.ThenOnRejectedReturningPromise:
                            next = deferredT.Promise.Then(_ => callback(), () => callback());
                            break;
                        case CallbackKindForTraceTest.CatchReturningPromise:
                            next = deferredT.Promise.Catch(() => callback());
                            break;
                        case CallbackKindForTraceTest.ContinueWithReturningPromise:
                            next = deferredT.Promise.ContinueWith(_ => (Promise) callback());
                            break;
                        default:
                            next = deferredT.Promise.Finally(() => (Promise) callback());
                            break;
                    }
                }
                bool isComplete = false;
                next.Finally(() => { isComplete = true; }).Forget();

                if (kind == CallbackKindForTraceTest.ThenOnRejectedReturningPromise || kind == CallbackKindForTraceTest.CatchReturningPromise)
                {
                    var exception = new System.InvalidOperationException("Bang!");
                    if (previousHasResult)
                    {
                        deferredT.Reject(exception);
                    }
                    else
                    {
                        deferred.Reject(exception);
                    }
                }
                else if (previousHasResult)
                {
                    deferredT.Resolve(1);
                }
                else
                {
                    deferred.Resolve();
                }

                // The callback's scope was exited when it returned, although the promise that it returned is still pending.
                Assert.IsTrue(isInvoked);
                Assert.IsFalse(isComplete);
                Assert.IsNull(CausalityTrace.Capture().Previous);

                returnedDeferred.Resolve(1);
                Assert.IsTrue(isComplete);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static System.Threading.CancellationToken ToCancellationTokenForTraceTest(CancelationToken token)
        {
            var cancellationToken = token.ToCancellationToken();
            Volatile.Write(ref s_frameKeeper, 34);
            return cancellationToken;
        }

        [Test]
        public void ConvertedCancellationTokenCallback_RunsInsideTheScopeOfWhatCanceledTheSource()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var source = CancelationSource.New();
                var cancellationToken = ToCancellationTokenForTraceTest(source.Token);
                CausalityTrace callbackTrace = null;
                cancellationToken.Register(() => callbackTrace = CausalityTrace.Capture());

                var cancelTrace = CaptureAtOther();
                var scope = CausalityTrace.EnterScope(cancelTrace);
                source.Cancel();
                scope.Dispose();
                source.Dispose();

                // Where the token was converted is not what registered the callback.
                Assert.IsNotNull(callbackTrace);
                Assert.AreSame(cancelTrace, callbackTrace.Previous, callbackTrace.ToString());
                Assert.IsFalse(HistoryContainsMethod(callbackTrace, nameof(ToCancellationTokenForTraceTest)), callbackTrace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void AwaiterContinuation_AwaitedAsyncFunctionCompletesSynchronously_RunsOutsideTheAsyncFunctionScope(
            [Values] bool flowExecutionContext)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            bool wasFlowExecutionContextEnabled = Promise.Config.AsyncFlowExecutionContextEnabled;
            Promise.Config.SetAsyncFlowExecutionContextEnabled(flowExecutionContext);
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = Promise.NewDeferred();
                var promise = AwaitForTraceTest(deferred.Promise);
                var continuationTrace = new CausalityTrace[1];
                OnCompletedForTraceTest(promise, false, continuationTrace);

                deferred.Resolve();

                // The async function completed inside its own scope, but its scope was exited while the continuation ran,
                // and the awaiting function isn't traced, so the continuation ran outside of any scope.
                // Where the async function was called is usually already on the stack of the code that awaits it.
                Assert.IsNull(continuationTrace[0].Previous, continuationTrace[0].ToString());
                CollectionAssert.IsEmpty(continuationTrace[0].OuterTraces, continuationTrace[0].ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.SetAsyncFlowExecutionContextEnabled(wasFlowExecutionContextEnabled);
            }
        }

#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ReleaseForTraceTest(Proto.Promises.Threading.AsyncLock.Key key)
        {
            key.Dispose();
            Volatile.Write(ref s_frameKeeper, 6);
        }

        private static async Promise LockOnTheCapturedContextForTraceTest(Proto.Promises.Threading.AsyncLock asyncLock, CausalityTrace[] trace)
        {
            using (await asyncLock.LockAsync(true))
            {
                trace[0] = CaptureAtLeaf();
            }
        }

        [Test]
        public void AsyncLockContinuationOnTheCapturedContext_IncludesWhereTheLockWasReleasedFrom()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var asyncLock = new Proto.Promises.Threading.AsyncLock();
                var trace = new CausalityTrace[1];
                var key = asyncLock.Lock();
                // The captured context is the foreground context of the test thread (see Promise.Manager.ThreadStaticSynchronizationContext).
                LockOnTheCapturedContextForTraceTest(asyncLock, trace).Forget();
                ReleaseForTraceTest(key);
                Assert.IsNull(trace[0]);
                TestHelper.ExecuteForegroundCallbacks();

                // The waiter continued on the context, so where the lock was released from is not on its stack.
                Assert.IsNotNull(trace[0]);
                var text = trace[0].ToString();
                Assert.IsNotNull(OuterTrace(trace[0]), text);
                Assert.IsTrue(ContainsMethod(OuterTrace(trace[0]), nameof(ReleaseForTraceTest)), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Progress NewProgressForTraceTest(CausalityTrace[] handlerTrace)
        {
            var progress = Progress.New(_ => handlerTrace[0] = CausalityTrace.Capture(), SynchronizationOption.Synchronous);
            Volatile.Write(ref s_frameKeeper, 7);
            return progress;
        }

        [Test]
        public void ProgressHandler_RunsInsideTheScopeOfWhereTheProgressWasCreated()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var handlerTrace = new CausalityTrace[1];
                var progress = NewProgressForTraceTest(handlerTrace);

                progress.Token.Report(0.5d);
                progress.DisposeAsync()
                    .WaitWithTimeoutWhileExecutingForegroundContext(System.TimeSpan.FromSeconds(1));

                AssertRanInsideTheScopeStartedBy(handlerTrace[0], nameof(NewProgressForTraceTest));
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Progress NewThrowingProgressForTraceTest(System.Exception exception, SynchronizationOption invokeOption)
        {
            var progress = Progress.New(_ => throw exception, invokeOption);
            Volatile.Write(ref s_frameKeeper, 35);
            return progress;
        }

        [Test]
        public void UnhandledException_ThrownInProgressHandler_IsReportedInsideTheScopeOfWhereTheProgressWasCreated(
            [Values(SynchronizationOption.Synchronous, SynchronizationOption.Foreground)] SynchronizationOption invokeOption)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException("Thrown in progress handler");
                    var progress = NewThrowingProgressForTraceTest(exception, invokeOption);

                    progress.Token.Report(0.5d);
                    progress.DisposeAsync()
                        .WaitWithTimeoutWhileExecutingForegroundContext(System.TimeSpan.FromSeconds(1));

                    // The report's trace is where the exception was caught, inside the scope of where the progress was created.
                    AssertRanInsideTheScopeStartedBy(reports.GetTrace(exception), nameof(NewThrowingProgressForTraceTest));
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static PromiseAllGroup<int> NewAllGroupForTraceTest(CausalityTrace[] cleanupTrace)
        {
            var group = PromiseAllGroup<int>.New(out _, value => { cleanupTrace[0] = CausalityTrace.Capture(); });
            Volatile.Write(ref s_frameKeeper, 29);
            return group;
        }

        [Test]
        public void PromiseAllGroupCleanup_RunsInsideTheScopeOfWhereItWasRegistered()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var cleanupTrace = new CausalityTrace[1];
                var resolved = Promise<int>.NewDeferred();
                var rejected = Promise<int>.NewDeferred();
                NewAllGroupForTraceTest(cleanupTrace)
                    .Add(resolved.Promise)
                    .Add(rejected.Promise)
                    .WaitAsync()
                    .ContinueWith(_ => { })
                    .Forget();

                resolved.Resolve(1);
                // The resolved value is cleaned up when the group is rejected, inside the call that rejected it.
                rejected.Reject(new System.InvalidOperationException("Bang!"));

                AssertRanInsideTheScopeStartedBy(cleanupTrace[0], nameof(NewAllGroupForTraceTest));
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static PromiseMergeGroup<int> AddWithCleanupForTraceTest(PromiseMergeGroup group, Promise<int> promise, CausalityTrace[] cleanupTrace)
        {
            var result = group.Add(promise, value => { cleanupTrace[0] = CausalityTrace.Capture(); });
            Volatile.Write(ref s_frameKeeper, 30);
            return result;
        }

        [Test]
        public void PromiseMergeGroupCleanup_RunsInsideTheScopeOfWhereItWasRegistered()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var cleanupTrace = new CausalityTrace[1];
                var resolved = Promise<int>.NewDeferred();
                var rejected = Promise<int>.NewDeferred();
                AddWithCleanupForTraceTest(PromiseMergeGroup.New(out _), resolved.Promise, cleanupTrace)
                    .Add(rejected.Promise)
                    .WaitAsync()
                    .ContinueWith(_ => { })
                    .Forget();

                resolved.Resolve(1);
                // The resolved value is cleaned up when the group is rejected, inside the call that rejected it.
                rejected.Reject(new System.InvalidOperationException("Bang!"));

                AssertRanInsideTheScopeStartedBy(cleanupTrace[0], nameof(AddWithCleanupForTraceTest));
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static PromiseRaceGroup<int> NewRaceGroupForTraceTest(CausalityTrace[] cleanupTrace)
        {
            var group = PromiseRaceGroup<int>.New(out _, value => { cleanupTrace[0] = CausalityTrace.Capture(); });
            Volatile.Write(ref s_frameKeeper, 31);
            return group;
        }

        [Test]
        public void PromiseRaceGroupCleanup_RunsInsideTheScopeOfWhereItWasRegistered()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var cleanupTrace = new CausalityTrace[1];
                var winner = Promise<int>.NewDeferred();
                var loser = Promise<int>.NewDeferred();
                NewRaceGroupForTraceTest(cleanupTrace)
                    .Add(winner.Promise)
                    .Add(loser.Promise)
                    .WaitAsync()
                    .Forget();

                winner.Resolve(1);
                // The value that lost the race is cleaned up inside the call that resolved it.
                loser.Resolve(2);

                AssertRanInsideTheScopeStartedBy(cleanupTrace[0], nameof(NewRaceGroupForTraceTest));
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if !UNITY_WEBGL
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Proto.Timers.Timer CreateTimerForTraceTest(Proto.Timers.TimerFactory timerFactory, CausalityTrace[] callbackTrace, System.Threading.ManualResetEventSlim invoked)
        {
            var timer = timerFactory.CreateTimer(_ =>
            {
                callbackTrace[0] = CausalityTrace.Capture();
                invoked.Set();
            }, null, System.TimeSpan.FromMilliseconds(1), System.Threading.Timeout.InfiniteTimeSpan);
            Volatile.Write(ref s_frameKeeper, 8);
            return timer;
        }

        [Test]
        public void TimerCallback_RunsInsideTheScopeOfWhereTheTimerWasCreated(
            [Values] bool customTimeProvider)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var timerFactory = customTimeProvider
                    ? Proto.Timers.TimerFactory.FromTimeProvider(new CustomTimeProviderForTraceTest())
                    : Proto.Timers.TimerFactory.System;
                var callbackTrace = new CausalityTrace[1];
                // Not disposed, because a late callback may set it after the wait timed out.
                var invoked = new System.Threading.ManualResetEventSlim();
                var timer = CreateTimerForTraceTest(timerFactory, callbackTrace, invoked);
                try
                {
                    Assert.IsTrue(invoked.Wait(System.TimeSpan.FromSeconds(5)));
                }
                finally
                {
                    // Waits for the callback to return.
                    timer.DisposeAsync().WaitWithTimeout(System.TimeSpan.FromSeconds(5));
                }

                AssertRanInsideTheScopeStartedBy(callbackTrace[0], nameof(CreateTimerForTraceTest));
                Assert.IsNull(OuterTrace(callbackTrace[0]), callbackTrace[0].ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void TimerCallback_RethrowsARejectedException_IsReportedWithTheTraceOfWhereItWasRejected_AndTheTimerIsDisposed(
            [Values] bool customTimeProvider,
            [Values] bool isTraced)
        {
            CausalityTrace.IsEnabled = isTraced;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException("Rethrown");
                    Promise.Rejected(exception).Forget();
                    var originalTrace = reports.GetTrace(exception);

                    var timerFactory = customTimeProvider
                        ? Proto.Timers.TimerFactory.FromTimeProvider(new CustomTimeProviderForTraceTest())
                        : Proto.Timers.TimerFactory.System;
                    var timer = timerFactory.CreateTimer(_ => throw exception, null, System.TimeSpan.FromMilliseconds(1), System.Threading.Timeout.InfiniteTimeSpan);
                    TestHelper.SpinUntil(() => reports.GetTraces(exception).Length == 2, System.TimeSpan.FromSeconds(5));
                    // The callback that threw is complete, so it doesn't keep the timer from being disposed.
                    timer.DisposeAsync().WaitWithTimeout(System.TimeSpan.FromSeconds(5));

                    var traces = reports.GetTraces(exception);
                    Assert.AreEqual(2, traces.Length);
                    Assert.AreSame(originalTrace, traces[1]);
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Proto.Timers.Timer CreateThrowingTimerForTraceTest(Proto.Timers.TimerFactory timerFactory, System.Exception exception)
        {
            var timer = timerFactory.CreateTimer(_ => throw exception, null, System.TimeSpan.FromMilliseconds(1), System.Threading.Timeout.InfiniteTimeSpan);
            Volatile.Write(ref s_frameKeeper, 33);
            return timer;
        }

        [Test]
        public void UnhandledException_ThrownInTimerCallback_IsReportedInsideTheScopeOfWhereTheTimerWasCreated(
            [Values] bool customTimeProvider)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException("Thrown");
                    var timerFactory = customTimeProvider
                        ? Proto.Timers.TimerFactory.FromTimeProvider(new CustomTimeProviderForTraceTest())
                        : Proto.Timers.TimerFactory.System;
                    var timer = CreateThrowingTimerForTraceTest(timerFactory, exception);
                    TestHelper.SpinUntil(() => reports.GetTraces(exception).Length == 1, System.TimeSpan.FromSeconds(5));
                    timer.DisposeAsync().WaitWithTimeout(System.TimeSpan.FromSeconds(5));

                    // The report's trace is where the exception was caught, inside the scope of where the timer was created.
                    AssertRanInsideTheScopeStartedBy(reports.GetTrace(exception), nameof(CreateThrowingTimerForTraceTest));
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private sealed class CustomTimeProviderForTraceTest : System.TimeProvider { }
#endif // !UNITY_WEBGL

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise<int> GetLazyPromiseForTraceTest(AsyncLazy<int> lazy)
        {
            var promise = lazy.Promise;
            Volatile.Write(ref s_frameKeeper, 9);
            return promise;
        }

        [Test]
        public void AsyncLazyFactory_RunsInsideTheScopeOfWhereTheResultWasFirstRequested(
            [Values] bool withProgress)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                CausalityTrace factoryTrace = null, scopeTrace = null;
                var lazy = withProgress
                    ? new AsyncLazy<int>(_ =>
                    {
                        scopeTrace = GetCurrentTraceForTraceTest();
                        factoryTrace = CausalityTrace.Capture();
                        return Promise.Resolved(1);
                    })
                    : new AsyncLazy<int>(() =>
                    {
                        scopeTrace = GetCurrentTraceForTraceTest();
                        factoryTrace = CausalityTrace.Capture();
                        return Promise.Resolved(1);
                    });

                Assert.AreEqual(1, GetLazyPromiseForTraceTest(lazy)
                    .WaitWithTimeoutWhileExecutingForegroundContext(System.TimeSpan.FromSeconds(1)));

                AssertRanSynchronouslyInsideTheScopeStartedBy(factoryTrace, scopeTrace, nameof(GetLazyPromiseForTraceTest));
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledRejection_AsyncLazyFactoryThrows_TraceIsTheLazysTrace(
            [Values] bool withProgress)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException("Thrown in the lazy's factory");
                    CausalityTrace lazyTrace = null;
                    System.Func<Promise<int>> factory = () =>
                    {
                        lazyTrace = GetCurrentTraceForTraceTest();
                        throw exception;
                    };
                    var lazy = withProgress
                        ? new AsyncLazy<int>(_ => factory())
                        : new AsyncLazy<int>(factory);
                    GetLazyPromiseForTraceTest(lazy).Forget();

                    Assert.IsNotNull(lazyTrace);
                    StringAssert.Contains(nameof(GetLazyPromiseForTraceTest), lazyTrace.StackTrace.ToString());
                    // The factory ran when the lazy promise was first requested, so the lazy's trace already shows what invoked it,
                    // and the rejection uses it instead of capturing the same frames again.
                    var trace = reports.GetTrace(exception);
                    Assert.AreSame(lazyTrace, trace, trace.ToString());
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if !UNITY_WEBGL
        public enum LazyFactoryOutcomeForTraceTest
        {
            Value,
            Throw,
            Cancel,
            CompletedPromise
        }

        [Test]
        public void AsyncLazyContinuation_AddedByAnotherCallerWhileTheFactoryRuns_TheFactoryScopeIsNotPrinted(
            [Values] bool withProgress,
            [Values] LazyFactoryOutcomeForTraceTest outcome)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var testThread = Thread.CurrentThread;
                Thread continuationThread = null;
                CausalityTrace continuationTrace = null;
                AsyncLazy<int> lazy = null;
                System.Func<Promise<int>> factory = () =>
                {
                    new ThreadHelper().ExecuteSingleAction(() =>
                        lazy.Promise
                            .ContinueWith(_ =>
                            {
                                continuationThread = Thread.CurrentThread;
                                continuationTrace = CausalityTrace.Capture();
                            })
                            .Forget()
                    );
                    switch (outcome)
                    {
                        case LazyFactoryOutcomeForTraceTest.Throw:
                            throw new System.InvalidOperationException("Bang!");
                        case LazyFactoryOutcomeForTraceTest.Cancel:
                            throw Promise.CancelException();
                        case LazyFactoryOutcomeForTraceTest.CompletedPromise:
                            var deferred = Promise.NewDeferred<int>();
                            deferred.Resolve(1);
                            return deferred.Promise;
                        default:
                            return Promise.Resolved(1);
                    }
                };
                lazy = withProgress
                    ? new AsyncLazy<int>(_ => factory())
                    : new AsyncLazy<int>(factory);

                GetLazyPromiseForTraceTest(lazy)
                    .ContinueWith(_ => { })
                    .WaitWithTimeoutWhileExecutingForegroundContext(System.TimeSpan.FromSeconds(1));

                // The continuation ran synchronously when the factory's result completed the lazy promise, while the factory's scope was still entered.
                // The scope's trace was captured on the continuation's stack, so it's not printed as an outer trace (see CausalityTrace.OuterTraces).
                Assert.AreSame(testThread, continuationThread);
                Assert.IsNotNull(continuationTrace);
#if PROTO_PROMISE_DEVELOPER_MODE
                // Developer mode doesn't hide the library's frames, so the scope's trace has frames that aren't on the continuation's stack, and it's printed.
                Assert.AreEqual(1, continuationTrace.OuterTraces.Count, continuationTrace.ToString());
                StringAssert.Contains(nameof(GetLazyPromiseForTraceTest), continuationTrace.OuterTraces[0].StackTrace.ToString());
#else
                if (!MayPrintHiddenFramesWithOtherNames || continuationTrace.OuterTraces.Count == 0)
                {
                    CollectionAssert.IsEmpty(continuationTrace.OuterTraces, continuationTrace.ToString());
                }
                else
                {
                    // It's printed if one of its hidden frames is printed with another function's name (see MayPrintHiddenFramesWithOtherNames).
                    Assert.AreEqual(1, continuationTrace.OuterTraces.Count, continuationTrace.ToString());
                    StringAssert.Contains(nameof(GetLazyPromiseForTraceTest), continuationTrace.OuterTraces[0].StackTrace.ToString());
                }
#endif
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }
#endif // !UNITY_WEBGL

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise ForEachForTraceTest(bool asyncEnumerable, List<CausalityTrace> bodyTraces)
        {
            // The body runs on the foreground context, so that it runs on the test thread.
            var options = new Proto.Promises.Threading.ParallelAsyncOptions() { SynchronizationContext = Promise.Config.ForegroundContext };
            System.Func<int, CancelationToken, Promise> body = (_, __) =>
            {
                bodyTraces.Add(CausalityTrace.Capture());
                return Promise.Resolved();
            };
            var promise = asyncEnumerable
                ? Proto.Promises.Threading.ParallelAsync.ForEach(Proto.Promises.Linq.AsyncEnumerable.Range(0, 3), options, body)
                : Proto.Promises.Threading.ParallelAsync.ForEach(Enumerable.Range(0, 3), options, body);
            Volatile.Write(ref s_frameKeeper, 10);
            return promise;
        }

        [Test]
        public void ParallelAsyncBody_RunsInsideTheScopeOfWhereTheLoopWasStarted(
            [Values] bool asyncEnumerable)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var bodyTraces = new List<CausalityTrace>();
                ForEachForTraceTest(asyncEnumerable, bodyTraces)
                    .WaitWithTimeoutWhileExecutingForegroundContext(System.TimeSpan.FromSeconds(1));

                Assert.AreEqual(3, bodyTraces.Count);
                foreach (var bodyTrace in bodyTraces)
                {
                    AssertRanInsideTheScopeStartedBy(bodyTrace, nameof(ForEachForTraceTest));
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void ParallelAsyncBody_AfterThePreviousBodyCompleted_DoesNotIncludeWhatCompletedIt(
            [Values] bool asyncEnumerable)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = NewVoidDeferredForTraceTest();
                var bodyTraces = new List<CausalityTrace>();
                var options = new Proto.Promises.Threading.ParallelAsyncOptions()
                {
                    SynchronizationContext = Promise.Config.ForegroundContext,
                    MaxDegreeOfParallelism = 1
                };
                // The first body completes when the deferred is resolved, then the loop schedules the worker again for the second body.
                System.Func<int, CancelationToken, Promise> body = (index, _) =>
                {
                    bodyTraces.Add(CausalityTrace.Capture());
                    return index == 0 ? deferred.Promise : Promise.Resolved();
                };
                var promise = asyncEnumerable
                    ? Proto.Promises.Threading.ParallelAsync.ForEach(Proto.Promises.Linq.AsyncEnumerable.Range(0, 2), options, body)
                    : Proto.Promises.Threading.ParallelAsync.ForEach(Enumerable.Range(0, 2), options, body);
                TestHelper.ExecuteForegroundCallbacks();
                ResolveForTraceTest(deferred);
                promise.WaitWithTimeoutWhileExecutingForegroundContext(System.TimeSpan.FromSeconds(1));

                Assert.AreEqual(2, bodyTraces.Count);
                // What completed the previous body doesn't concern the next one, so it's not captured.
                Assert.IsFalse(HistoryContainsMethod(bodyTraces[1], nameof(ResolveForTraceTest)), bodyTraces[1].ToString());
                AssertRanInsideTheScopeStartedBy(bodyTraces[1], nameof(ParallelAsyncBody_AfterThePreviousBodyCompleted_DoesNotIncludeWhatCompletedIt));
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        public enum ParallelBodyOutcomeForTraceTest
        {
            Throw,
            Cancel,
            ThrowAfterPending,
            CancelAfterPending
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise ForEachWithOutcomeForTraceTest(bool asyncEnumerable, ParallelBodyOutcomeForTraceTest outcome, System.Exception exception,
            Promise pendingPromise, List<CausalityTrace> bodyTraces)
        {
            // One worker, so that the body that throws runs in the worker that resumed after the pending promise completed.
            var options = new Proto.Promises.Threading.ParallelAsyncOptions()
            {
                SynchronizationContext = Promise.Config.ForegroundContext,
                MaxDegreeOfParallelism = 1
            };
            bool afterPending = outcome == ParallelBodyOutcomeForTraceTest.ThrowAfterPending || outcome == ParallelBodyOutcomeForTraceTest.CancelAfterPending;
            System.Func<int, CancelationToken, Promise> body = (i, _) =>
            {
                if (afterPending && i == 0)
                {
                    return pendingPromise;
                }
                bodyTraces.Add(CausalityTrace.Capture());
                if (outcome == ParallelBodyOutcomeForTraceTest.Throw || outcome == ParallelBodyOutcomeForTraceTest.ThrowAfterPending)
                {
                    throw exception;
                }
                throw Promise.CancelException();
            };
            var promise = asyncEnumerable
                ? Proto.Promises.Threading.ParallelAsync.ForEach(Proto.Promises.Linq.AsyncEnumerable.Range(0, 3), options, body)
                : Proto.Promises.Threading.ParallelAsync.ForEach(Enumerable.Range(0, 3), options, body);
            Volatile.Write(ref s_frameKeeper, 37);
            return promise;
        }

        [Test]
        public void ParallelAsyncBody_ThatThrows_TheLoopIsRejectedOrCanceled_AndTheBodyRanInsideTheScopeOfWhereTheLoopWasStarted(
            [Values] bool asyncEnumerable,
            [Values] ParallelBodyOutcomeForTraceTest outcome)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var exception = new System.InvalidOperationException("Bang!");
                bool afterPending = outcome == ParallelBodyOutcomeForTraceTest.ThrowAfterPending || outcome == ParallelBodyOutcomeForTraceTest.CancelAfterPending;
                var deferred = afterPending ? Promise.NewDeferred() : default;
                var bodyTraces = new List<CausalityTrace>();
                Promise.State state = Promise.State.Pending;
                System.Exception rejection = null;
                var loopPromise = ForEachWithOutcomeForTraceTest(asyncEnumerable, outcome, exception, afterPending ? deferred.Promise : default, bodyTraces)
                    .ContinueWith(result =>
                    {
                        state = result.State;
                        if (state == Promise.State.Rejected)
                        {
                            rejection = (System.Exception) result.Reason;
                        }
                    });
                if (afterPending)
                {
                    TestHelper.ExecuteForegroundCallbacks();
                    // The worker waits for the pending promise, and runs the next body on the context when it resolves.
                    Assert.AreEqual(0, bodyTraces.Count);
                    deferred.Resolve();
                }
                loopPromise.WaitWithTimeoutWhileExecutingForegroundContext(System.TimeSpan.FromSeconds(1));

                if (outcome == ParallelBodyOutcomeForTraceTest.Throw || outcome == ParallelBodyOutcomeForTraceTest.ThrowAfterPending)
                {
                    Assert.AreEqual(Promise.State.Rejected, state);
                    Assert.IsInstanceOf<System.AggregateException>(rejection);
                    CollectionAssert.AreEqual(new[] { exception }, ((System.AggregateException) rejection).InnerExceptions);
                }
                else
                {
                    Assert.AreEqual(Promise.State.Canceled, state);
                }
                // The loop stops after the first body that throws.
                Assert.AreEqual(1, bodyTraces.Count);
                AssertRanInsideTheScopeStartedBy(bodyTraces[0], nameof(ForEachWithOutcomeForTraceTest));
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledRejection_CausalityTraceIsTheRejectionTrace_EvenWhenReportedElsewhere()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var trace = RejectAndReportLater(new System.InvalidOperationException("Bang!")).CausalityTrace;

                Assert.IsNotNull(trace);
                StringAssert.Contains(nameof(RejectForReportTraceTest), trace.StackTrace.ToString());
                StringAssert.DoesNotContain(nameof(DisposeForReportTraceTest), trace.StackTrace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledRejection_CausalityTraceIsNullWhenDisabled()
        {
            CausalityTrace.IsEnabled = false;
            Assert.IsNull(RejectAndReportLater(new System.InvalidOperationException("Bang!")).CausalityTrace);
        }

        [Test]
        public void UnhandledRejection_SameExceptionAfterTracingWasDisabled_DoesNotHaveTheOldTrace()
        {
            var exception = new System.InvalidOperationException("Bang!");
            CausalityTrace.IsEnabled = true;
            try
            {
                var firstTrace = RejectAndReportLater(exception).CausalityTrace;
                Assert.IsNotNull(firstTrace);

                CausalityTrace.IsEnabled = false;
                Assert.IsNull(RejectAndReportLater(exception).CausalityTrace);

                CausalityTrace.IsEnabled = true;
                var secondTrace = RejectAndReportLater(exception).CausalityTrace;
                Assert.IsNotNull(secondTrace);
                Assert.AreNotSame(firstTrace, secondTrace);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if PROTO_PROMISE_TEST_GC_ENABLED && !PROMISE_DEBUG // In DEBUG mode, traces are stored in their objects' fields.
        [Test]
        public void CausalityTraceIsEnabled_Disabled_ReleasesTheTracesOfOperationsThatAreStillPending()
        {
            CausalityTrace.IsEnabled = true;
            Promise.Deferred deferred;
            System.WeakReference traceReference;
            try
            {
                deferred = NewDeferredAndGetItsTraceForTraceTest(out traceReference);
                Assert.IsTrue(traceReference.IsAlive);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }

            // The table of traces may only be released when its finalizer runs, so this collects twice.
            TestHelper.GcCollectAndWaitForFinalizers();
            TestHelper.GcCollectAndWaitForFinalizers();
            Assert.IsFalse(traceReference.IsAlive);
            // The promise is still pending, so it's not collected with its trace.
            deferred.Resolve();
        }

        // NoInlining so that the trace isn't kept alive by a local of the test.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise.Deferred NewDeferredAndGetItsTraceForTraceTest(out System.WeakReference traceReference)
        {
            var deferred = Promise.NewDeferred();
            var trace = deferred.Promise._ref.Trace;
            Assert.IsNotNull(trace);
            traceReference = new System.WeakReference(trace);
            deferred.Promise.Forget();
            return deferred;
        }
#endif

        [Test]
        public void CausalityTraceMaxDepth_MustBeGreaterThanZero()
        {
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => CausalityTrace.MaxDepth = 0);
        }

        [Test]
        public void CausalityTraceMaxOuterTraces_MustNotBeNegative()
        {
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => CausalityTrace.MaxOuterTraces = -1);
        }

        [Test]
        public void CausalityTraceCapture_MoreOuterScopesThanMaxOuterTraces_KeepsTheInnermostOnes(
            [Values(0, 2, 3)] int maxOuterTraces)
        {
            CausalityTrace.MaxOuterTraces = maxOuterTraces;
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                // From the innermost.
                var outers = new[] { CaptureAtStep(), CaptureAtLoop(), CaptureAtRoot() };
                var trace = CaptureInScopesForTraceTest(other, outers);

                var text = trace.ToString();
                Assert.AreSame(other, trace.Previous, text);
                CollectionAssert.AreEqual(outers.Take(maxOuterTraces), trace.OuterTraces, text);
                Assert.AreEqual(maxOuterTraces < outers.Length, trace.AreOuterTracesTruncated, text);
                if (trace.AreOuterTracesTruncated)
                {
                    // Printed after the outer traces, indented one more level.
                    StringAssert.Contains(System.Environment.NewLine + new string(' ', 3 * (maxOuterTraces + 2))
                        + "--- Outer traces truncated, increase CausalityTrace.MaxOuterTraces to see more ---" + System.Environment.NewLine, text);
                }
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                CausalityTrace.MaxOuterTraces = 8;
            }
        }

        [Test]
        public void CausalityTraceCapture_RepeatOfThePreviousTrace_TheOuterTracesAreTruncatedIfItsOuterTracesDontFitOrWereTruncated(
            [Values] bool sameOuterTrace,
            [Values] bool repeatOuterTracesWereTruncated)
        {
            CausalityTrace.MaxOuterTraces = 1;
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                // The repeat's outer traces were truncated, or they fit.
                var previousOuters = repeatOuterTracesWereTruncated
                    ? new[] { CaptureAtLoop(), CaptureAtRoot() }
                    : new[] { CaptureAtLoop() };
                // If the trace's outer trace is the repeat's, the repeat's outer traces fit, but the ones that it dropped are still missing.
                // Otherwise, the repeat's outer trace doesn't fit after the trace's own.
                // A null scope doesn't change the current trace, so that the second trace is captured in as many nested calls as the first.
                var outers = new CausalityTrace[previousOuters.Length];
                outers[0] = sameOuterTrace ? previousOuters[0] : CaptureAtStep();
                // Captured from the same line, so that the second trace is a repeat of the first.
                var trace = other;
                for (int i = 0; i < 2; ++i)
                {
                    trace = CaptureInScopesForTraceTest(trace, i == 0 ? previousOuters : outers);
                }

                var text = trace.ToString();
                Assert.AreEqual(1, trace.Repeats.Count, text);
                Assert.AreSame(other, trace.Previous, text);
                CollectionAssert.AreEqual(new[] { outers[0] }, trace.OuterTraces, text);
                Assert.AreEqual(repeatOuterTracesWereTruncated || !sameOuterTrace, trace.AreOuterTracesTruncated, text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                CausalityTrace.MaxOuterTraces = 8;
            }
        }

        [Test]
        public void CausalityTraceCapture_OuterScopesWhoseTracesWereCapturedAtTheSameLocation_KeepsOnlyTheInnermost()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                // Separate captures at the same location (e.g. two callbacks that were scheduled from the same place, running inside each other).
                var outers = new CausalityTrace[2];
                for (int i = 0; i < outers.Length; ++i)
                {
                    outers[i] = CaptureInScopeForTraceTest(null);
                }
                Assert.IsTrue(HasSameFrames(outers[0], outers[1]));
                var trace = CaptureInScopesForTraceTest(other, outers);

                var text = trace.ToString();
                Assert.AreSame(other, trace.Previous, text);
                CollectionAssert.AreEqual(new[] { outers[0] }, trace.OuterTraces, text);
                Assert.IsFalse(trace.AreOuterTracesTruncated, text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_TruncatedCopyOfATraceWhoseOuterTracesWereTruncated_TheyAreStillTruncated()
        {
            CausalityTrace.MaxOuterTraces = 0;
            CausalityTrace.MaxDepth = 4;
            CausalityTrace.IsEnabled = true;
            try
            {
                var trace = CaptureAtRoot();
                for (int i = 0; i < 2; ++i)
                {
                    var scope = CausalityTrace.EnterScope(trace);
                    trace = i == 0 ? CaptureAtLoop() : CaptureAtLeaf();
                    scope.Dispose();
                }
                var withTruncatedOuterTraces = CaptureInScopesForTraceTest(trace, new[] { CaptureAtStep() });
                Assert.IsTrue(withTruncatedOuterTraces.AreOuterTracesTruncated, withTruncatedOuterTraces.ToString());
                // The trace is at the max depth, so the next trace has a truncated copy of it.
                var deepScope = CausalityTrace.EnterScope(withTruncatedOuterTraces);
                var last = CaptureAtOther();
                deepScope.Dispose();

                var text = last.ToString();
                Assert.AreEqual(2, GetCausalityTraceDepth(last), text);
                Assert.AreNotSame(withTruncatedOuterTraces, last.Previous, text);
                Assert.IsTrue(last.Previous.AreOuterTracesTruncated, text);
                AssertCausalityTraceIsPrintedCorrectly(last, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                CausalityTrace.MaxDepth = 128;
                CausalityTrace.MaxOuterTraces = 8;
            }
        }

        // Captures inside the scopes of the outer traces (from the innermost) and the previous trace.
        private static CausalityTrace CaptureInScopesForTraceTest(CausalityTrace previous, CausalityTrace[] outers)
            => CaptureInScopesForTraceTest(previous, outers, outers.Length - 1);

        private static CausalityTrace CaptureInScopesForTraceTest(CausalityTrace previous, CausalityTrace[] outers, int index)
        {
            if (index < 0)
            {
                return CaptureInScopeForTraceTest(previous);
            }
            var scope = CausalityTrace.EnterScope(outers[index]);
            try
            {
                return CaptureInScopesForTraceTest(previous, outers, index - 1);
            }
            finally
            {
                scope.Dispose();
            }
        }

        [Test]
        public void CausalityTraceCapture_DeepChainOfAsyncFunctionsThatCompleteSynchronously_EachRunsOutsideTheScopeOfTheOneThatCompleted(
            [Values] bool flowExecutionContext,
            [Values] bool awaitBefore,
            [Values] bool awaitTraces)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            bool wasFlowExecutionContextEnabled = Promise.Config.AsyncFlowExecutionContextEnabled;
            Promise.Config.SetAsyncFlowExecutionContextEnabled(flowExecutionContext);
            Promise.Config.AwaitTracesEnabled = awaitTraces;
            CausalityTrace.IsEnabled = true;
            try
            {
                // Each async function continues the one that awaited it while it's still running inside its own scope,
                // so without exiting that scope, their scopes would be nested as deep as the chain.
                // With await traces, each continues inside the scope of where it awaited too, which is exited with its own scope.
                var deferred = Promise.NewDeferred();
                var traces = new List<CausalityTrace>();
                var pending = new Queue<Promise.Deferred>();
                var promise = AwaitRecursivelyForTraceTest(deferred.Promise, 16, traces, awaitBefore ? pending : null);
#if PROTO_PROMISE_DEVELOPER_MODE
                // In developer mode, the optimized async mode's first await trace is the async promise's trace (see CausalityTrace.GetWithAllFrames),
                // so without an await before, where it awaited isn't an outer trace.
                bool hasAwaitTrace = awaitTraces & (awaitBefore | !IsInOptimizedAsyncMode(promise));
#else
                bool hasAwaitTrace = awaitTraces;
#endif
                while (pending.Count > 0)
                {
                    pending.Dequeue().Resolve();
                }
                deferred.Resolve();
                promise.WaitWithTimeout(System.TimeSpan.FromSeconds(5));

                Assert.AreEqual(17, traces.Count);
                foreach (var trace in traces)
                {
                    if (!hasAwaitTrace)
                    {
                        CollectionAssert.IsEmpty(trace.OuterTraces, trace.ToString());
                    }
                    // The trace of where its own async function awaited, unless it's dropped like the frames of this method (see KeepsOuterTracesFromTheSameMethod).
                    else if (KeepsOuterTracesFromTheSameMethod)
                    {
                        Assert.AreEqual(1, trace.OuterTraces.Count, trace.ToString());
                    }
                    else
                    {
                        Assert.LessOrEqual(trace.OuterTraces.Count, 1, trace.ToString());
                    }
                    Assert.IsFalse(trace.AreOuterTracesTruncated, trace.ToString());
                }
                AssertCausalityTraceIsPrintedCorrectly(traces[traces.Count - 1], traces[traces.Count - 1].ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.AwaitTracesEnabled = false;
                Promise.Config.SetAsyncFlowExecutionContextEnabled(wasFlowExecutionContextEnabled);
            }
        }

        // The promise is created in its own method, so that its trace is not dropped like the frames of the test method that is still running
        // (see KeepsOuterTracesFromTheSameMethod).
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise NewPromiseResolvedByAScheduledCallbackForTraceTest()
        {
            // The callback is scheduled, so that it completes the promise after it's awaited.
            var promise = Promise.New(deferred => deferred.Resolve(), SynchronizationOption.Foreground, forceAsync: true);
            Volatile.Write(ref s_frameKeeper, 27);
            return promise;
        }

        [Test]
        public void AsyncPromise_AwaitedPromiseNewCallbackCompletesItSynchronously_RunsInsideTheCallbackScope(
            [Values] bool flowExecutionContext)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            bool wasFlowExecutionContextEnabled = Promise.Config.AsyncFlowExecutionContextEnabled;
            Promise.Config.SetAsyncFlowExecutionContextEnabled(flowExecutionContext);
            CausalityTrace.IsEnabled = true;
            try
            {
                CausalityTrace trace = null;
                async Promise Func(Promise promise)
                {
                    await promise;
                    trace = CausalityTrace.Capture();
                }

                var newPromise = NewPromiseResolvedByAScheduledCallbackForTraceTest();
                var newTrace = newPromise._ref.Trace;
                Func(newPromise).Forget();
                TestHelper.ExecuteForegroundCallbacks();

                // The callback is still running when it completes the promise, so the async function runs inside the scope of Promise.New's trace,
                // which is where the scheduled callback came from.
                Assert.IsNotNull(newTrace);
                Assert.IsNotNull(trace);
                Assert.IsTrue(trace.OuterTraces.Any(outer => outer.StackTrace == newTrace.StackTrace), trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.SetAsyncFlowExecutionContextEnabled(wasFlowExecutionContextEnabled);
            }
        }

        [Test]
        public void AsyncPromise_AwaitedAsyncFunctionCompletesSynchronously_ContextRunsTheContinuationSynchronously_RunsOutsideTheAsyncFunctionScope(
            [Values] bool flowExecutionContext)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            bool wasFlowExecutionContextEnabled = Promise.Config.AsyncFlowExecutionContextEnabled;
            Promise.Config.SetAsyncFlowExecutionContextEnabled(flowExecutionContext);
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = Promise.NewDeferred();
                var trace = new CausalityTrace[1];
                var asyncPromise = AwaitForTraceTest(deferred.Promise);
                var asyncTrace = asyncPromise._ref.Trace;
                CaptureAfterAwaitingOnAnInlineContextForTraceTest(asyncPromise, trace).Forget();
                deferred.Resolve();

                // The async function completed inside its own scope, and the context that the continuation was scheduled on ran it synchronously,
                // but its scope was exited while the continuation ran.
                Assert.IsNotNull(asyncTrace);
                Assert.IsNotNull(trace[0]);
                Assert.IsFalse(trace[0].OuterTraces.Any(outer => outer.StackTrace == asyncTrace.StackTrace), trace[0].ToString());
#if !PROTO_PROMISE_DEVELOPER_MODE
                // Where it was scheduled from is on the stack. Developer mode doesn't hide internal frames, so it's never printed like the outer frames of the trace.
                CollectionAssert.IsEmpty(trace[0].OuterTraces, trace[0].ToString());
#endif
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.SetAsyncFlowExecutionContextEnabled(wasFlowExecutionContextEnabled);
            }
        }

        public enum PassThroughKindForTraceTest
        {
            CatchNotInvoked,
            ThenNotInvoked,
            Retainer,
            All,
            AdoptedByACallback
        }

        [Test]
        public void AsyncPromise_AwaitedAsyncFunctionCompletesSynchronouslyThroughAnotherPromise_ItsScopeIsExited(
            [Values] PassThroughKindForTraceTest kind,
            [Values] bool flowExecutionContext)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            bool wasFlowExecutionContextEnabled = Promise.Config.AsyncFlowExecutionContextEnabled;
            Promise.Config.SetAsyncFlowExecutionContextEnabled(flowExecutionContext);
            CausalityTrace.IsEnabled = true;
            try
            {
                CausalityTrace trace = null;
                async Promise Func(Promise promise)
                {
                    try
                    {
                        await promise;
                    }
                    catch (System.InvalidOperationException)
                    {
                    }
                    trace = CausalityTrace.Capture();
                }

                var deferred = Promise.NewDeferred();
                var asyncPromise = AwaitForTraceTest(deferred.Promise);
                var retainer = default(Promise.Retainer);
                Promise awaited;
                switch (kind)
                {
                    case PassThroughKindForTraceTest.CatchNotInvoked:
                        // It's resolved, so the callback is not invoked.
                        awaited = asyncPromise.Catch(() => { });
                        break;
                    case PassThroughKindForTraceTest.ThenNotInvoked:
                        // It's rejected, so the callback is not invoked.
                        awaited = asyncPromise.Then(() => { });
                        break;
                    case PassThroughKindForTraceTest.Retainer:
                        retainer = asyncPromise.GetRetainer();
                        awaited = retainer.WaitAsync();
                        break;
                    case PassThroughKindForTraceTest.All:
                        awaited = Promise.All(asyncPromise, Promise.Resolved());
                        break;
                    default:
                        // The callback's promise adopts the async function's promise.
                        var callbackDeferred = Promise.NewDeferred();
                        awaited = callbackDeferred.Promise.Then(() => asyncPromise);
                        callbackDeferred.Resolve();
                        break;
                }
                Func(awaited).Forget();
                if (kind == PassThroughKindForTraceTest.ThenNotInvoked)
                {
                    deferred.Reject(new System.InvalidOperationException("Bang!"));
                }
                else
                {
                    deferred.Resolve();
                }
                if (kind == PassThroughKindForTraceTest.Retainer)
                {
                    retainer.Dispose();
                }

                // The async function completed inside its own scope, but its scope was exited while the promise that waited for it completed.
                Assert.IsNotNull(trace);
                CollectionAssert.IsEmpty(trace.OuterTraces, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.SetAsyncFlowExecutionContextEnabled(wasFlowExecutionContextEnabled);
            }
        }

        private static async Promise CaptureAfterAwaitingOnAnInlineContextForTraceTest(Promise promise, CausalityTrace[] trace)
        {
            await promise.ConfigureAwait(new ContinuationOptions(new InlineContext()));
            trace[0] = CausalityTrace.Capture();
        }

        // NoInlining for the same reason as AwaitForTraceTest.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Promise AwaitRecursivelyForTraceTest(Promise promise, int depth, List<CausalityTrace> traces, Queue<Promise.Deferred> pendingBefore = null)
        {
            if (pendingBefore != null)
            {
                await GetPendingPromise(pendingBefore);
            }
            if (depth == 0)
            {
                await promise;
            }
            else
            {
                await AwaitRecursivelyForTraceTest(promise, depth - 1, traces, pendingBefore);
            }
            traces.Add(CausalityTrace.Capture());
        }

        [Test]
        public void CausalityTrace_SequentialScopes_DepthIsTruncatedToHalfWhenItExceedsMax()
        {
            const int maxDepth = 10;
            int originalMaxDepth = CausalityTrace.MaxDepth;
            CausalityTrace.MaxDepth = maxDepth;
            CausalityTrace.IsEnabled = true;
            try
            {
                CausalityTrace trace = null;
                int expectedDepth = 0;
                bool expectTruncated = false;
                for (int i = 0; i < 100; ++i)
                {
                    var scope = CausalityTrace.EnterScope(trace);
                    // Captured at two locations in turn, so that each trace is not a repeat of its previous trace (see CausalityTrace.Repeats).
                    trace = i % 2 == 0 ? CaptureAtStep() : CaptureAtLoop();
                    scope.Dispose();

                    ++expectedDepth;
                    if (expectedDepth > maxDepth)
                    {
                        expectedDepth = maxDepth / 2;
                        expectTruncated = true;
                    }
                    Assert.AreEqual(expectedDepth, GetCausalityTraceDepth(trace));
                    Assert.AreEqual(expectTruncated, trace.ToString().Contains("Older traces truncated"));
                }
            }
            finally
            {
                CausalityTrace.MaxDepth = originalMaxDepth;
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTrace_NestedScopes_DepthDoesNotExceedMax()
        {
            const int maxDepth = 10;
            int originalMaxDepth = CausalityTrace.MaxDepth;
            CausalityTrace.MaxDepth = maxDepth;
            CausalityTrace.IsEnabled = true;
            try
            {
                // Each trace is captured while all previous traces are still in scope, so traces share their previous traces.
                CaptureNestedCausalityTraces(null, 30, maxDepth);
            }
            finally
            {
                CausalityTrace.MaxDepth = originalMaxDepth;
                CausalityTrace.IsEnabled = false;
            }
        }

        private static void CaptureNestedCausalityTraces(CausalityTrace trace, int remaining, int maxDepth)
        {
            var scope = CausalityTrace.EnterScope(trace);
            try
            {
                // Captured in another method, so that the previous trace is not skipped without line numbers.
                var nextTrace = CaptureAtLeaf();
                int depth = GetCausalityTraceDepth(nextTrace);
                Assert.LessOrEqual(depth, maxDepth);
                if (remaining > 0)
                {
                    CaptureNestedCausalityTraces(nextTrace, remaining - 1, maxDepth);
                }
                else
                {
                    Assert.Greater(depth, 1);
                }
            }
            finally
            {
                scope.Dispose();
            }
        }

        [Test]
        public void CausalityTrace_RecursivelyScheduledContinuations_AreFoldedIntoOneTrace()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Each continuation schedules the next continuation, like a polling loop.
                Promise.Deferred deferred = default;
                CausalityTrace callbackTrace = null;
                System.Action schedule = null;
                schedule = () =>
                {
                    deferred = Promise.NewDeferred();
                    deferred.Promise
                        .Then(() =>
                        {
                            // Not the last call, so that it's not a tail call, which would remove this callback's frame from the next traces.
                            schedule();
                            callbackTrace = CausalityTrace.Capture();
                        })
                        .Forget();
                };
                schedule();

                for (int i = 0; i < 100; ++i)
                {
                    deferred.Resolve();
                    // The callback, the continuation that was scheduled from the previous callback, and the first continuation.
                    Assert.LessOrEqual(GetCausalityTraceDepth(callbackTrace), 3);
                }
                // Each continuation was scheduled from the same location as the previous one, so it replaced it, and the others are its repeats.
                var continuationTrace = callbackTrace.Previous;
                Assert.AreEqual(1, continuationTrace.Repeats.Count);
                Assert.AreEqual(98, continuationTrace.Repeats[0].Count);
                Assert.AreEqual(continuationTrace.ThreadId, continuationTrace.Repeats[0].ThreadId);
                Assert.That(continuationTrace.Repeats[0].FirstTimestamp, Is.LessThanOrEqualTo(continuationTrace.Repeats[0].LastTimestamp));
                Assert.That(continuationTrace.Repeats[0].LastTimestamp, Is.LessThanOrEqualTo(continuationTrace.Timestamp));
                Assert.That(continuationTrace.Repeats[0].FirstTimestamp, Is.GreaterThanOrEqualTo(continuationTrace.Previous.Timestamp));
                var repeats = continuationTrace.Repeats;
                Assert.AreSame(repeats, continuationTrace.Repeats);
                Assert.Throws<System.NotSupportedException>(() => ((IList<CausalityTrace.RepeatGroup>) repeats)[0] = default);
                StringAssert.Contains("Repeated 98 times: ", callbackTrace.ToString());

                // Complete the last scheduled continuation without scheduling another.
                schedule = () => { };
                deferred.Resolve();
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_RecursivelyScheduledContinuations_DoNotGrowWithTheIterations()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // The traces are captured from the same line, so that their stack traces are the same.
                var traces = new[] { 200, 400 }.Select(iterations => GetTraceFromRecursivelyScheduledContinuations(iterations)).ToArray();
                var text200 = traces[0].ToString();
                var trace400 = traces[1];
                var text400 = trace400.ToString();
                StringAssert.IsMatch(@"--- Repeated \d+ times: Thread \d+ ", text400);
                // Only the number of repeats is different.
                AssertPrintedTheSame(text200.Replace("Repeated 199 times", "Repeated 399 times"), text400);
                AssertCausalityTraceIsPrintedCorrectly(trace400, text400);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static CausalityTrace GetTraceFromRecursivelyScheduledContinuations(int iterations)
        {
            // Each continuation schedules the next continuation, like a polling loop.
            Promise.Deferred deferred = default;
            CausalityTrace callbackTrace = null;
            System.Action schedule = null;
            schedule = () =>
            {
                deferred = Promise.NewDeferred();
                deferred.Promise
                    .Then(() =>
                    {
                        // Not the last call, so that it's not a tail call, which would remove this callback's frame from the next traces.
                        schedule();
                        callbackTrace = CausalityTrace.Capture();
                    })
                    .Forget();
            };
            schedule();
            try
            {
                for (int i = 0; i < iterations; ++i)
                {
                    deferred.Resolve();
                }
            }
            finally
            {
                // Complete the last scheduled continuation without scheduling another.
                schedule = () => { };
                deferred.Resolve();
            }
            return callbackTrace;
        }

        [Test]
        public void CausalityTraceToString_AsyncLoop_DoesNotGrowWithTheIterations()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Each awaited operation is started inside the async function's scope, so its cause is the async function, not the previous iteration.
                // The traces are captured from the same line, so that their stack traces are the same.
                var traces = new[] { 16, 64 }.Select(iterations => GetTraceFromAsyncLoop(iterations)).ToArray();
                var text16 = traces[0].ToString();
                var trace64 = traces[1];
                var text64 = trace64.ToString();
                AssertPrintedTheSame(text16, text64);
                AssertCausalityTraceIsPrintedCorrectly(trace64, text64);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_NegativeSkipFrames_Throws()
        {
            Assert.Throws<Proto.Promises.ArgumentOutOfRangeException>(() => CausalityTrace.Capture(-1));
        }

        [Test]
        public void CausalityTrace_CapturesInTheSameTooDeepScopeAfterTheMaxDepthChanged_AreTruncatedToTheNewDepth()
        {
            int originalMaxDepth = CausalityTrace.MaxDepth;
            CausalityTrace.MaxDepth = 6;
            CausalityTrace.IsEnabled = true;
            try
            {
                // Build a scope whose trace is at the max depth, so that every capture inside it is truncated (see the test below).
                var trace = CaptureAtRoot();
                for (int i = 1; i < 6; ++i)
                {
                    var scope = CausalityTrace.EnterScope(trace);
                    trace = i % 2 == 0 ? CaptureAtStep() : CaptureAtLoop();
                    scope.Dispose();
                }
                var deepScope = CausalityTrace.EnterScope(trace);
                var trace1 = CaptureAtLeaf();
                CausalityTrace.MaxDepth = 4;
                var trace2 = CaptureAtLeaf();
                deepScope.Dispose();

                // Half of the max depth is kept.
                Assert.AreEqual(3, GetCausalityTraceDepth(trace1));
                Assert.AreEqual(2, GetCausalityTraceDepth(trace2));
            }
            finally
            {
                CausalityTrace.MaxDepth = originalMaxDepth;
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTrace_CapturesInTheSameTooDeepScope_ShareTheTruncatedTraces()
        {
            const int maxDepth = 4;
            int originalMaxDepth = CausalityTrace.MaxDepth;
            CausalityTrace.MaxDepth = maxDepth;
            CausalityTrace.IsEnabled = true;
            try
            {
                // Build a scope whose trace is at the max depth, so that every capture inside it is truncated.
                // The traces are captured at two locations in turn, so that each trace is not a repeat of its previous trace (see CausalityTrace.Repeats),
                // and each location is a call, so that each trace doesn't contain its previous trace (see CausalityTrace.Previous).
                var trace = CaptureAtRoot();
                for (int i = 1; i < maxDepth; ++i)
                {
                    var scope = CausalityTrace.EnterScope(trace);
                    trace = i % 2 == 0 ? CaptureAtStep() : CaptureAtLoop();
                    scope.Dispose();
                }
                var deepScope = CausalityTrace.EnterScope(trace);
                var trace1 = CausalityTrace.Capture();
                var trace2 = CausalityTrace.Capture();
                deepScope.Dispose();

                Assert.AreNotSame(trace, trace1.Previous, "The previous trace was expected to be truncated.");
                Assert.AreSame(trace1.Previous, trace2.Previous);
            }
            finally
            {
                CausalityTrace.MaxDepth = originalMaxDepth;
                CausalityTrace.IsEnabled = false;
            }
        }

        // Collects the exceptions that are reported to the UncaughtRejectionHandler, such as scopes that were not disposed correctly.
        private sealed class ReportedExceptions : System.IDisposable
        {
            private readonly System.Action<UnhandledException> _previousHandler = Promise.Config.UncaughtRejectionHandler;
            private readonly List<System.Exception> _exceptions = new List<System.Exception>();
            private readonly List<CausalityTrace> _traces = new List<CausalityTrace>();

            internal ReportedExceptions()
            {
                Promise.Config.UncaughtRejectionHandler = ex =>
                {
#if PROTO_PROMISE_DEVELOPER_MODE
                    // Developer mode wraps the report in an exception that is traced where it was reported.
                    ex = (UnhandledException) ex.InnerException;
#endif
                    lock (_exceptions)
                    {
                        _exceptions.Add((System.Exception) ex.Value);
                        _traces.Add(ex.CausalityTrace);
                    }
                };
            }

            // The causality traces of every report of the exception.
            internal CausalityTrace[] GetTraces(System.Exception exception)
            {
                lock (_exceptions)
                {
                    return Enumerable.Range(0, _exceptions.Count).Where(i => _exceptions[i] == exception).Select(i => _traces[i]).ToArray();
                }
            }

            // The causality trace of the report of the exception.
            internal CausalityTrace GetTrace(System.Exception exception)
            {
                lock (_exceptions)
                {
                    return _traces[_exceptions.IndexOf(exception)];
                }
            }

            internal System.Exception[] Exceptions
            {
                get
                {
                    lock (_exceptions)
                    {
                        return _exceptions.ToArray();
                    }
                }
            }

            // Asserts that each reported exception is a misused scope with the message, in order.
            internal void AssertScopeMisuses(params string[] messages)
            {
                var exceptions = Exceptions;
                Assert.AreEqual(messages.Length, exceptions.Length, string.Join("\n", exceptions.Select(e => e.ToString())));
                for (int i = 0; i < messages.Length; i++)
                {
                    Assert.IsInstanceOf<System.InvalidOperationException>(exceptions[i]);
                    StringAssert.Contains("CausalityTrace.Scope", exceptions[i].Message);
                    StringAssert.Contains(messages[i], exceptions[i].Message);
                }
            }

            public void Dispose()
                => Promise.Config.UncaughtRejectionHandler = _previousHandler;
        }

        // Records what the callbacks that are posted to it throw, by running them immediately.
        private sealed class ThrowRecordingContext : System.Threading.SynchronizationContext, System.IDisposable
        {
            private readonly System.Threading.SynchronizationContext _previousContext = Promise.Config.ForegroundContext;
            private readonly List<System.Exception> _thrown = new List<System.Exception>();

            internal ThrowRecordingContext()
                => Promise.Config.ForegroundContext = this;

            public override void Post(System.Threading.SendOrPostCallback d, object state)
            {
                System.Exception thrown = null;
                try
                {
                    d(state);
                }
                catch (System.Exception e)
                {
                    thrown = e;
                }
                lock (_thrown)
                {
                    _thrown.Add(thrown);
                }
            }

            // Asserts that each posted callback threw the exception.
            internal void AssertThrown(System.Exception expected, int count)
            {
                System.Exception[] thrown;
                lock (_thrown)
                {
                    thrown = _thrown.ToArray();
                }
                Assert.AreEqual(count, thrown.Length);
                foreach (var e in thrown)
                {
                    Assert.IsNotNull(e);
                    TestHelper.AssertRejection(expected, e);
                }
            }

            public void Dispose()
                => Promise.Config.ForegroundContext = _previousContext;
        }

        [Test]
        public void UncaughtRejectionHandler_Throws_ItsExceptionIsThrownInTheContext()
        {
            var previousHandler = Promise.Config.UncaughtRejectionHandler;
            try
            {
                using (var context = new ThrowRecordingContext())
                {
                    var handlerException = new System.InvalidOperationException("Handler");
                    Promise.Config.UncaughtRejectionHandler = _ => throw handlerException;

                    // The handler's exception doesn't escape into the code that reported the rejection.
                    Promise.Rejected(new System.Exception("Rejected")).Forget();
                    context.AssertThrown(handlerException, 1);
                }
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = previousHandler;
            }
        }

        // Returns the exception that disposing the scope throws, or null. A ref struct can't be used in a lambda, so we don't use Assert.Throws.
        // Copies of a scope are disposed the same way as the scope.
        private static System.Exception GetDisposeException(CausalityTrace.Scope scope)
        {
            try
            {
                scope.Dispose();
                return null;
            }
            catch (System.Exception e)
            {
                return e;
            }
        }

        private static System.Exception GetNestedScopesNotDisposedReport(ReportedExceptions reports)
            => reports.Exceptions.Single(e => e is System.InvalidOperationException && e.Message.Contains("before its nested"));

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace.Scope EnterScopeForTraceTest(CausalityTrace trace)
        {
            var scope = CausalityTrace.EnterScope(trace);
            Volatile.Write(ref s_frameKeeper, 32);
            return scope;
        }

        // Disposing a scope that was already exited (by disposing it or its outer scope) is improper, so it throws.
        private static void AssertDisposeThrows(CausalityTrace.Scope scope, bool isAlreadyDisposed)
        {
            var exception = GetDisposeException(scope);
            Assert.IsInstanceOf<System.InvalidOperationException>(exception);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
            // DEBUG mode knows whether the scope was already disposed.
            Assert.AreEqual(isAlreadyDisposed, exception is System.ObjectDisposedException, exception.ToString());
#endif
        }

        [Test]
        public void CausalityTraceScope_DisposedTwice_ThrowsAndDoesNotExitOtherScopes()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var outerTrace = CaptureAtOther();
                    var innerTrace = CaptureAtStep();
                    var outerScope = CausalityTrace.EnterScope(outerTrace);
                    var innerScope = CausalityTrace.EnterScope(innerTrace);
                    innerScope.Dispose();
                    AssertDisposeThrows(innerScope, true);

                    // A scope that was entered in the same place on the stack is not exited either.
                    var laterTrace = CaptureAtLoop();
                    var laterScope = CausalityTrace.EnterScope(laterTrace);
                    AssertDisposeThrows(innerScope, true);
                    Assert.AreSame(laterTrace, CausalityTrace.Capture().Previous);
                    laterScope.Dispose();

                    // The outer scope is still the current scope.
                    var trace = CausalityTrace.Capture();
                    outerScope.Dispose();
                    Assert.AreSame(outerTrace, trace.Previous);
                    reports.AssertScopeMisuses();
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceScope_OuterDisposedBeforeInner_IsReportedAndExitsTheInnerScope_InnerDisposeThrows()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var outerTrace = CaptureAtOther();
                    var innerTrace = CaptureAtStep();
                    var outerScope = EnterScopeForTraceTest(outerTrace);
                    var innerScope = CausalityTrace.EnterScope(innerTrace);
                    outerScope.Dispose();
                    reports.AssertScopeMisuses("before its nested");
                    Assert.IsNull(CausalityTrace.Capture().Previous);
                    var reportTrace = reports.GetTrace(GetNestedScopesNotDisposedReport(reports));
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    // DEBUG mode captures where the scope was entered, which is more useful than the trace that was passed to it.
                    Assert.AreNotSame(outerTrace, reportTrace);
                    StringAssert.Contains(nameof(EnterScopeForTraceTest), reportTrace.ToString());
#else
                    Assert.AreSame(outerTrace, reportTrace);
#endif

                    // Disposing the inner scope later throws, and doesn't exit a scope that was entered after the outer scope was disposed.
                    var laterTrace = CaptureAtLoop();
                    var laterScope = CausalityTrace.EnterScope(laterTrace);
                    AssertDisposeThrows(innerScope, false);
                    var trace = CausalityTrace.Capture();
                    laterScope.Dispose();
                    Assert.AreSame(laterTrace, trace.Previous);
                    reports.AssertScopeMisuses("before its nested");
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if !(PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE) || PROTO_PROMISE_TEST_GC_ENABLED
        // In DEBUG mode, the scope that is not disposed is also reported when it's garbage collected, so this requires the GC.
        [Test]
        public void CausalityTraceScope_NotDisposedInCallback_IsReportedAndTheCallbackCompletes()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var deferred = Promise.NewDeferred();
                    bool isContinued = false;
                    ThenWithScopeNotDisposedForTraceTest(deferred.Promise)
                        .Then(() => isContinued = true)
                        .Forget();
                    deferred.Resolve();

                    Assert.IsTrue(isContinued);
                    // The callback's scope also exited the scope that was not disposed.
                    Assert.IsNull(CausalityTrace.Capture().Previous);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    // The scope that was not disposed is also reported when it's collected, which may happen at any time.
                    TestHelper.GcCollectAndWaitForFinalizers();
                    var exceptions = reports.Exceptions;
                    Assert.AreEqual(2, exceptions.Length, string.Join("\n", exceptions.Select(e => e.ToString())));
                    Assert.AreEqual(1, exceptions.OfType<UnreleasedObjectException>().Count());
                    Assert.AreEqual(1, exceptions.OfType<System.InvalidOperationException>().Count(e => e.Message.Contains("before its nested")));
#else
                    reports.AssertScopeMisuses("before its nested");
#endif
                    // The report's causality trace is where the callback that didn't dispose its scope was registered.
                    var reportTrace = reports.GetTrace(GetNestedScopesNotDisposedReport(reports));
                    Assert.IsNotNull(reportTrace);
                    StringAssert.Contains(nameof(ThenWithScopeNotDisposedForTraceTest), reportTrace.StackTrace.ToString());
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise ThenWithScopeNotDisposedForTraceTest(Promise promise)
        {
            var thenPromise = promise.Then(() => { CausalityTrace.EnterScope(CausalityTrace.Capture()); });
            Volatile.Write(ref s_frameKeeper, 11);
            return thenPromise;
        }

        [Test]
        public void CausalityTraceScope_NotDisposedInCallback_HandlerThrows_TheCallbackCompletes()
        {
            var previousHandler = Promise.Config.UncaughtRejectionHandler;
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var context = new ThrowRecordingContext())
                {
                    var handlerException = new System.InvalidOperationException("Handler");
                    Promise.Config.UncaughtRejectionHandler = _ => throw handlerException;

                    var deferred = Promise.NewDeferred();
                    bool isContinued = false;
                    deferred.Promise
                        .Then(() => { CausalityTrace.EnterScope(CausalityTrace.Capture()); })
                        .Then(() => isContinued = true)
                        .Forget();
                    deferred.Resolve();

                    Assert.IsTrue(isContinued);
                    Assert.IsNull(CausalityTrace.Capture().Previous);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    // The scope that was not disposed is also reported when it's collected, which may happen at any time.
                    TestHelper.GcCollectAndWaitForFinalizers();
                    context.AssertThrown(handlerException, 2);
#else
                    context.AssertThrown(handlerException, 1);
#endif
                }
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = previousHandler;
                CausalityTrace.IsEnabled = false;
            }
        }
#endif

#if PROTO_PROMISE_TEST_GC_ENABLED && (PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE)
        [Test]
        public void CausalityTraceScope_NotDisposed_IsReportedWithWhereItWasEntered([Values] bool isTraceNull)
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            UnhandledException unhandledException = null;
            Promise.Config.UncaughtRejectionHandler = ex =>
            {
#if PROTO_PROMISE_DEVELOPER_MODE
                // Developer mode wraps the report in an exception that is traced where it was reported.
                ex = (UnhandledException) ex.InnerException;
#endif
                unhandledException = ex;
            };
            CausalityTrace.IsEnabled = true;
            try
            {
                // The scope is entered on another thread, so that it's not left on this thread's scopes.
                var thread = new Thread(() => EnterAndAbandonScope(isTraceNull));
                thread.Start();
                thread.Join();

                TestHelper.GcCollectAndWaitForFinalizers();
                Assert.IsNotNull(unhandledException);
                Assert.IsInstanceOf<UnreleasedObjectException>(unhandledException.Value);
                StringAssert.Contains("CausalityTrace.Scope", ((UnreleasedObjectException) unhandledException.Value).Message);
                StringAssert.Contains(nameof(EnterAndAbandonScope), unhandledException.StackTrace);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void EnterAndAbandonScope(bool isTraceNull)
        {
            CausalityTrace.EnterScope(isTraceNull ? null : CausalityTrace.Capture());
            Volatile.Write(ref s_frameKeeper, 12);
        }
#endif

#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
        [Test]
        public void CausalityTraceScope_NullTrace_IsCheckedForBeingDisposedCorrectly()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var outerScope = CausalityTrace.EnterScope(CausalityTrace.Capture());
                    var nullScope = CausalityTrace.EnterScope(null);
                    outerScope.Dispose();
                    Assert.IsNull(CausalityTrace.Capture().Previous);
                    AssertDisposeThrows(nullScope, false);
                    AssertDisposeThrows(nullScope, true);
                    reports.AssertScopeMisuses("before its nested");
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceScope_NullTraceDisposedBeforeInner_IsReportedAndExitsTheInnerScope_InnerDisposeThrows()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var nullScope = CausalityTrace.EnterScope(null);
                    var innerScope = CausalityTrace.EnterScope(CausalityTrace.Capture());
                    nullScope.Dispose();
                    reports.AssertScopeMisuses("before its nested");
                    Assert.IsNull(CausalityTrace.Capture().Previous);
                    AssertDisposeThrows(innerScope, false);
                    reports.AssertScopeMisuses("before its nested");
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }
#endif

        [Test]
        public void CausalityTraceScope_EnteredWhileDisabled_OuterDisposedBeforeInner_IsReportedAndExitsTheInnerScope_InnerDisposeThrows()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var outerTrace = CausalityTrace.Capture();
                    var innerTrace = CausalityTrace.Capture();
                    var outerScope = CausalityTrace.EnterScope(outerTrace);
                    // Scopes that are entered while causality traces are disabled must also be disposed in order.
                    CausalityTrace.IsEnabled = false;
                    var innerScope = CausalityTrace.EnterScope(innerTrace);
                    CausalityTrace.IsEnabled = true;
                    outerScope.Dispose();
                    Assert.IsNull(CausalityTrace.Capture().Previous);
                    AssertDisposeThrows(innerScope, false);
                    reports.AssertScopeMisuses("before its nested");
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceScope_OuterDisposedBeforeInner_ExitsTheInnerScopeBeforeItIsReported()
        {
            var previousHandler = Promise.Config.UncaughtRejectionHandler;
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var context = new ThrowRecordingContext())
                {
                    // The handler runs with the restored traces, and if it throws, its exception is thrown in the context instead of from Dispose.
                    var handlerException = new System.InvalidOperationException("Handler");
                    CausalityTrace handlerTrace = null;
                    Promise.Config.UncaughtRejectionHandler = _ =>
                    {
                        handlerTrace = CausalityTrace.Capture();
                        throw handlerException;
                    };
                    var outerScope = CausalityTrace.EnterScope(CausalityTrace.Capture());
                    var innerScope = CausalityTrace.EnterScope(CausalityTrace.Capture());

                    Assert.IsNull(GetDisposeException(outerScope));
                    Assert.IsNull(handlerTrace.Previous);
                    Assert.IsNull(CausalityTrace.Capture().Previous);
                    context.AssertThrown(handlerException, 1);
                    AssertDisposeThrows(innerScope, false);
                }
            }
            finally
            {
                Promise.Config.UncaughtRejectionHandler = previousHandler;
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceEnterScope_NullTrace_DoesNothing()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Captured in different methods, so that the trace is not a repeat of the outer trace (see CausalityTraceToString_PrintsTheChainOfPreviousTraces).
                var outerTrace = CaptureAtOther();
                var outerScope = CausalityTrace.EnterScope(outerTrace);
                var nullScope = CausalityTrace.EnterScope(null);
                var trace = CaptureAtStep();
                nullScope.Dispose();
                outerScope.Dispose();
                Assert.AreSame(outerTrace, trace.Previous);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE // Developer mode doesn't skip frames.
        [Test]
        public void CausalityTraceToString_TracesWithoutVisibleFrames_ArePrintedAsHidden()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Traces without frames in the chain and as the root. They're not repeats of each other, because they have no frames to compare.
                var root = CopyTrace(CaptureWithoutFrames(), false, CopyTrace(CaptureWithoutFrames(), false, CaptureAtLeaf()));

                var text = root.ToString();
                Assert.AreEqual(2, CountOccurrences(text, "--- All frames are hidden ---"), text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        // Skips every frame, like a trace whose frames are all hidden.
        private static CausalityTrace CaptureWithoutFrames() => new CausalityTrace(1000);
#endif

        [Test]
        public void CausalityTrace_HasNoStaticConstructor()
        {
            // A static constructor would make IL2CPP check that the class is initialized every time IsEnabled is read on hot paths.
            Assert.IsNull(typeof(CausalityTrace).TypeInitializer);
        }

        [Test]
        public void PromiseConfig_HasNoStaticConstructor()
        {
            // A static constructor would make IL2CPP check that the class is initialized every time a setting is read on hot paths.
            Assert.IsNull(typeof(Promise.Config).TypeInitializer);
        }

        [Test]
        public void PromiseConfigDefaultTimerFactory_CanBeSetAndRestored()
        {
            var original = Promise.Config.DefaultTimerFactory;
#if !UNITY_WEBGL // The WebGL player replaces the default with a Unity timer factory.
            Assert.AreSame(Proto.Timers.TimerFactory.System, original);
#endif
            var timerFactory = new FakeTimerFactoryForConfigTest();
            Promise.Config.DefaultTimerFactory = timerFactory;
            try
            {
                Assert.AreSame(timerFactory, Promise.Config.DefaultTimerFactory);
                Assert.Throws<Proto.Promises.ArgumentNullException>(() => Promise.Config.DefaultTimerFactory = null);
                Assert.AreSame(timerFactory, Promise.Config.DefaultTimerFactory);
            }
            finally
            {
                Promise.Config.DefaultTimerFactory = original;
            }
            Assert.AreSame(original, Promise.Config.DefaultTimerFactory);
        }

        private sealed class FakeTimerFactoryForConfigTest : Proto.Timers.TimerFactory
        {
            public override Proto.Timers.Timer CreateTimer(System.Threading.TimerCallback callback, object state, System.TimeSpan dueTime, System.TimeSpan period)
                => throw new System.NotImplementedException();
        }

        [Test]
        public void CausalityTraceMaxDepth_CanBeSetAndRestored()
        {
            int defaultMaxDepth = CausalityTrace.MaxDepth;
            // The default, unless another test leaked a change.
            Assert.AreEqual(128, defaultMaxDepth);
            try
            {
                CausalityTrace.MaxDepth = 1;
                Assert.AreEqual(1, CausalityTrace.MaxDepth);
                CausalityTrace.MaxDepth = int.MaxValue;
                Assert.AreEqual(int.MaxValue, CausalityTrace.MaxDepth);
            }
            finally
            {
                CausalityTrace.MaxDepth = defaultMaxDepth;
            }
            Assert.AreEqual(defaultMaxDepth, CausalityTrace.MaxDepth);
        }

        [Test]
        public void CausalityTraceCapture_RecordsTheThreadAndTime()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var thread = System.Threading.Thread.CurrentThread;
                long before = System.TimeProvider.System.GetTimestamp();
                var trace = CausalityTrace.Capture();
                long after = System.TimeProvider.System.GetTimestamp();

                Assert.AreEqual(thread.ManagedThreadId, trace.ThreadId);
                Assert.AreEqual(thread.Name, trace.ThreadName);
                Assert.AreEqual(thread.IsThreadPoolThread, trace.IsThreadPoolThread);
                Assert.AreEqual(thread.IsBackground, trace.IsBackgroundThread);
                Assert.That(trace.Timestamp, Is.InRange(before, after));
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if !UNITY_WEBGL
        [Test]
        public void CausalityTraceToString_PrintsTheThreadAndTimeOfEachTrace()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Traces from the same frames are not next to each other, so that they're not folded as repeats.
                var foreground = CaptureOnNewThread(false, null);
                var named = CaptureOnThreadHelper(foreground);
                var background = CaptureOnNewThread(true, named);
                var pooled = CaptureOnThreadPool(background);
                var root = CaptureInScopeForTraceTest(pooled);
                var text = root.ToString();

                Assert.IsNull(foreground.ThreadName);
                Assert.IsFalse(foreground.IsBackgroundThread);
                Assert.IsNotNull(named.ThreadName);
                Assert.IsNull(background.ThreadName);
                Assert.IsTrue(background.IsBackgroundThread);
                Assert.IsTrue(pooled.IsThreadPoolThread);
                Assert.That(named.Timestamp, Is.InRange(foreground.Timestamp, background.Timestamp));
                Assert.That(pooled.Timestamp, Is.InRange(background.Timestamp, root.Timestamp));

                // The printed trace has no time, and the earlier traces show how long before it they were captured.
                var firstLine = text.Split('\n')[0].TrimEnd('\r');
                StringAssert.StartsWith($"   --- Thread {root.ThreadId} ", firstLine);
                StringAssert.DoesNotContain("earlier", firstLine);
                const string time = @", \d+\.\d{3} ms earlier ---";
                StringAssert.IsMatch($@"--- Thread {foreground.ThreadId} \(foreground\){time}", text);
                StringAssert.IsMatch($@"--- {ThreadPattern(named)}{time}", text);
                StringAssert.IsMatch($@"--- Thread {background.ThreadId} \(background\){time}", text);
                // Newer runtimes name thread pool threads.
                var poolThread = pooled.ThreadName == null ? $@"Thread {pooled.ThreadId} \(thread pool\)" : ThreadPattern(pooled);
                StringAssert.IsMatch($@"--- {poolThread}{time}", text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_ThreadNameWithLineBreaks_IsPrintedOnOneLine()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                CausalityTrace trace = null;
                // A new thread, because the name of a thread can only be set once.
                var thread = new System.Threading.Thread(() => trace = CaptureAtLeaf()) { Name = "First\r\nSecond\nThird" };
                thread.Start();
                thread.Join();

                var text = trace.ToString();
                StringAssert.StartsWith($"   --- Thread {trace.ThreadId} \"First  Second Third\" ---", text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_RepeatsOnOtherThreads_AreFoldedWithTheirThreadsAndTimes()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // The same code ran on thread A twice, then on thread B, then on thread A twice more, each time inside the scope of the previous time,
                // like a loop that moved between threads. Each second time on thread A replaced the first one, which is its repeat.
                var last = TurnCapturer.Capture(CaptureAtOther(), 2, 0, 0, 1, 0, 0);
                var root = CaptureInScopeForTraceTest(last);
                Assert.AreEqual(4, GetCausalityTraceDepth(last));
                Assert.AreEqual(1, last.Repeats.Count);
                var text = root.ToString();

                // The last time is printed, and the others are folded into it, grouped by thread from the latest to the earliest.
                var threadA = ThreadPattern(last);
                var threadB = ThreadPattern(last.Previous);
                StringAssert.IsMatch($@"--- {threadA}, \d+\.\d{{3}} ms earlier ---", text);
                const string time = @"(\d+\.\d{3})";
                var match = System.Text.RegularExpressions.Regex.Match(text, $@"--- Repeated 4 times: {threadA} 3 times at {time} to {time} ms earlier; "
                    + $@"{threadB} 1 time at {time} ms earlier ---");
                Assert.IsTrue(match.Success, text);
                var times = Enumerable.Range(1, 3).Select(i => double.Parse(match.Groups[i].Value, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                Assert.That(times[0], Is.LessThan(times[1]), text);
                Assert.That(times[2], Is.InRange(times[0], times[1]), text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_RepeatsOnThreadPoolThreads_AreFoldedAsOneThread()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Like a loop that continues on the thread pool, which may be a different thread each time.
                var other = CaptureAtOther();
                var traces = CaptureOnDifferentThreadPoolThreads(other, 3);
                Assert.AreEqual(3, traces.Select(t => t.ThreadId).Distinct().Count());

                var trace = traces[2];
                Assert.AreSame(other, trace.Previous);
                Assert.AreEqual(1, trace.Repeats.Count);
                Assert.IsTrue(trace.Repeats[0].IsThreadPoolThread);
                Assert.AreEqual(2, trace.Repeats[0].Count);
                Assert.AreEqual(traces[1].ThreadId, trace.Repeats[0].ThreadId);
                var text = trace.ToString();
                StringAssert.IsMatch(@"--- Repeated 2 times: thread pool 2 times at \d+\.\d{3} to \d+\.\d{3} ms earlier ---", text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static string ThreadPattern(CausalityTrace namedThreadTrace)
            => $@"Thread {namedThreadTrace.ThreadId} ""{System.Text.RegularExpressions.Regex.Escape(namedThreadTrace.ThreadName)}""";
#endif // !UNITY_WEBGL

        // Captures a trace inside the scope of the previous trace, from the same frames every time.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureInScopeForTraceTest(CausalityTrace previous)
        {
            var scope = CausalityTrace.EnterScope(previous);
            try
            {
                return CausalityTrace.Capture();
            }
            finally
            {
                scope.Dispose();
            }
        }

        [Test]
        public void CausalityTraceCapture_RepeatOfThePreviousTrace_IsFoldedIntoIt()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Each trace is captured at the same location inside the scope of the previous trace, like a callback that registers itself again.
                var other = CaptureAtOther();
                var trace = other;
                var traces = new CausalityTrace[5];
                for (int i = 0; i < 5; ++i)
                {
                    trace = CaptureInScopeForTraceTest(trace);
                    traces[i] = trace;
                }

                // The first trace in the loop was not a repeat, because it was captured inside the scope of a trace from another location.
                Assert.AreSame(other, trace.Previous);
                Assert.AreEqual(1, trace.Repeats.Count);
                var repeat = trace.Repeats[0];
                Assert.AreEqual(4, repeat.Count);
                Assert.AreEqual(trace.ThreadId, repeat.ThreadId);
                Assert.AreEqual(trace.ThreadName, repeat.ThreadName);
                Assert.AreEqual(trace.IsThreadPoolThread, repeat.IsThreadPoolThread);
                Assert.AreEqual(trace.IsBackgroundThread, repeat.IsBackgroundThread);
                Assert.AreEqual(traces[0].Timestamp, repeat.FirstTimestamp);
                Assert.AreEqual(traces[3].Timestamp, repeat.LastTimestamp);

                var text = trace.ToString();
                StringAssert.IsMatch(@"--- Repeated 4 times: Thread \d+ .* 4 times at \d+\.\d{3} to \d+\.\d{3} ms earlier ---", text);
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureInScopeForTraceTest)), text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if !PROTO_PROMISE_DEVELOPER_MODE // Developer mode doesn't hide internal frames, so the traces of the library's calls are not contained by other traces.
        [Test]
        public void CausalityTraceCapture_InsideTheCallThatCapturedThePreviousTrace_SkipsIt()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                CausalityTrace newTrace, callbackTrace = null;
                Promise.Deferred deferred = default;
                var scope = CausalityTrace.EnterScope(other);
                try
                {
                    // Promise.New captures the promise's trace, and invokes the callback inside its scope.
                    var promise = Promise.New(d =>
                    {
                        deferred = d;
                        callbackTrace = CaptureAtLeaf();
                    });
                    newTrace = promise._ref.Trace;
                    promise.Forget();
                }
                finally
                {
                    scope.Dispose();
                }
                deferred.Resolve();

                // The promise's trace is skipped, because its frames are the outer frames of the callback's trace. Its cause is kept.
                // It's kept instead if one of its hidden frames is printed with another function's name (see MayPrintHiddenFramesWithOtherNames).
                Assert.IsNotNull(newTrace);
                Assert.AreSame(other, newTrace.Previous);
                if (!MayPrintHiddenFramesWithOtherNames || callbackTrace.Previous != newTrace)
                {
                    Assert.AreSame(other, callbackTrace.Previous);
                }
                AssertCausalityTraceIsPrintedCorrectly(callbackTrace, callbackTrace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_AfterSwitchingToAContextAtTheFirstAwait_ItsCauseIsWhereTheAsyncFunctionWasCalled(
            [Values] bool awaitTraces)
        {
            Promise.Config.AwaitTracesEnabled = awaitTraces;
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var trace = new CausalityTrace[1];
                CaptureAfterSwitchForTraceTest(context, trace).Forget();
                context.Execute();

                var text = trace[0].ToString();
                // Nothing ran before the first await except where the async function was called, which is its own trace,
                // so where the switch was scheduled (where it awaited) isn't captured.
                if (!awaitTraces)
                {
                    CollectionAssert.IsEmpty(trace[0].OuterTraces, text);
                    AssertPreviousTraceIsFrom(trace[0], nameof(CausalityTraceCapture_AfterSwitchingToAContextAtTheFirstAwait_ItsCauseIsWhereTheAsyncFunctionWasCalled));
                }
                // With await traces, where it awaited contains where it was called, so its own trace is skipped, and where it awaited is printed in its place,
                // unless it's dropped like the frames of this method (see KeepsOuterTracesFromTheSameMethod).
                else if (KeepsOuterTracesFromTheSameMethod)
                {
                    Assert.IsNotNull(OuterTrace(trace[0]), text);
                    Assert.IsTrue(ContainsMethod(OuterTrace(trace[0]), nameof(CausalityTraceCapture_AfterSwitchingToAContextAtTheFirstAwait_ItsCauseIsWhereTheAsyncFunctionWasCalled)), text);
                    Assert.IsNull(trace[0].Previous, text);
                }
                Assert.LessOrEqual(GetCausalityTraceDepth(trace[0]), 2, text);
                AssertCausalityTraceIsPrintedCorrectly(trace[0], text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.AwaitTracesEnabled = false;
            }
        }

        private static async Promise CaptureAfterSwitchForTraceTest(QueuedContext context, CausalityTrace[] trace)
        {
            await Promise.SwitchToContextAwait(context, forceAsync: true);
            trace[0] = CausalityTrace.Capture();
        }

        // The tests below capture with CaptureFromTheSameLineForTraceTest in a loop, so that a trace that is captured deeper contains the trace before it,
        // like a trace captured in Promise.New's callback contains the trace of Promise.New. They enter the scope of an outer trace around it directly,
        // like a callback that was scheduled from that trace.

        // Captures a trace inside the scope of the previous trace, here or one call deeper, from the same line.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureFromTheSameLineForTraceTest(CausalityTrace previous, bool deeper)
        {
            var scope = CausalityTrace.EnterScope(previous);
            try
            {
                return deeper ? CaptureAtLeaf() : CausalityTrace.Capture();
            }
            finally
            {
                scope.Dispose();
            }
        }

        // Captures a trace and then a deeper trace inside its scope, each inside the scope of an outer trace (if it's not null), as if their callbacks were scheduled from it.
        private static CausalityTrace CaptureContainedForTraceTest(CausalityTrace previous, CausalityTrace outer, CausalityTrace deeperOuter, out CausalityTrace contained)
        {
            contained = null;
            var trace = previous;
            for (int i = 0; i < 2; ++i)
            {
                var scope = CausalityTrace.EnterScope(i == 0 ? outer : deeperOuter);
                try
                {
                    trace = CaptureFromTheSameLineForTraceTest(trace, i == 1);
                }
                finally
                {
                    scope.Dispose();
                }
                if (i == 0)
                {
                    contained = trace;
                }
            }
            return trace;
        }

        [Test]
        public void CausalityTraceCapture_SkippedTrace_ItsOuterTracesAreKept(
            [Values] bool deeperHasAnOuterTrace)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                var outer = CaptureAtStep();
                var deeperOuter = deeperHasAnOuterTrace ? CaptureAtLoop() : null;
                var trace = CaptureContainedForTraceTest(other, outer, deeperOuter, out var contained);

                CollectionAssert.AreEqual(new[] { outer }, contained.OuterTraces);
                // The contained trace is skipped, and its outer traces are kept after the trace's own.
                var text = trace.ToString();
                Assert.AreSame(other, trace.Previous, text);
                CollectionAssert.AreEqual(deeperHasAnOuterTrace ? new[] { deeperOuter, outer } : new[] { outer }, trace.OuterTraces, text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_SkippedTraceRepeatedByAnOuterScope_TheRepeatIsNotAnOuterTrace()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                CausalityTrace contained = null;
                CausalityTrace trace = null;
                for (int i = 0; i < 2; ++i)
                {
                    // The second time, the deeper trace is captured inside the scope of the first contained trace, which has the same frames as the second
                    // contained trace, as if a callback that was scheduled from the same location ran it. Its frames are already printed in the deeper trace.
                    trace = CaptureContainedForTraceTest(other, null, contained, out contained);
                }

                var text = trace.ToString();
                Assert.AreSame(other, contained.Previous, text);
                // The second contained trace is skipped, and the first one adds nothing.
                Assert.AreSame(other, trace.Previous, text);
                CollectionAssert.IsEmpty(trace.OuterTraces, text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_TraceContainedByWhereTheCallbackWasScheduledFrom_IsSkippedAndItsOuterTracesAreKept(
            [Values] bool containedHasAnOuterTrace)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // The trace of where the callback was scheduled from contains the previous trace, like an async function that switched to another context.
                var other = CaptureAtOther();
                var containedOuter = containedHasAnOuterTrace ? CaptureAtStep() : null;
                var scheduledFrom = CaptureContainedForTraceTest(other, containedOuter, null, out var contained);
                CausalityTrace trace;
                var scope = CausalityTrace.EnterScope(scheduledFrom);
                try
                {
                    trace = CaptureInScopeForTraceTest(contained);
                }
                finally
                {
                    scope.Dispose();
                }

                // The contained trace is skipped, and its outer traces are kept after the trace's own.
                var text = trace.ToString();
                Assert.AreSame(other, trace.Previous, text);
                Assert.AreEqual(containedHasAnOuterTrace ? 2 : 1, trace.OuterTraces.Count, text);
                Assert.AreSame(scheduledFrom.StackTrace, trace.OuterTraces[0].StackTrace, text);
                if (containedHasAnOuterTrace)
                {
                    Assert.AreSame(containedOuter, trace.OuterTraces[1], text);
                }
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_SkippedTraceWithRepeats_IsKept()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // The second capture is a repeat of the first, and the third contains it.
                var other = CaptureAtOther();
                var trace = other;
                CausalityTrace repeated = null;
                for (int i = 0; i < 3; ++i)
                {
                    repeated = trace;
                    trace = CaptureFromTheSameLineForTraceTest(trace, i == 2);
                }

                // The repeats would be lost, so it's kept.
                Assert.AreEqual(1, repeated.Repeats.Count);
                Assert.AreSame(repeated, trace.Previous, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_SkippedTruncatedTrace_TheTraceIsTruncated()
        {
            int maxDepth = CausalityTrace.MaxDepth;
            CausalityTrace.MaxDepth = 2;
            CausalityTrace.IsEnabled = true;
            try
            {
                var step = CopyTrace(CaptureAtStep(), false, CaptureAtOther());
                var trace = CaptureContainedForTraceTest(step, null, null, out var contained);

                // The contained trace was the oldest trace that was kept, so the trace is now the oldest.
                Assert.IsTrue(contained.IsTruncated);
                Assert.IsNull(trace.Previous);
                Assert.IsTrue(trace.IsTruncated);
                var text = trace.ToString();
                StringAssert.EndsWith(System.Environment.NewLine
                    + "--- End of stack trace from the previous location ---" + System.Environment.NewLine
                    + "   --- Older traces truncated, increase CausalityTrace.MaxDepth to see more ---" + System.Environment.NewLine, text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.MaxDepth = maxDepth;
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_LoopThroughACallThatInvokesItsCallback_DoesNotGrowTheChain()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Like a loop whose iterations call Promise.New, and capture in its callback.
                var other = CaptureAtOther();
                var trace = other;
                for (int i = 0; i < 10; ++i)
                {
                    trace = CaptureFromTheSameLineForTraceTest(trace, i % 2 == 1);
                }

                // Each contained trace is skipped, which exposes the previous iteration's trace, which is a repeat.
                var text = trace.ToString();
                Assert.AreSame(other, trace.Previous, text);
                Assert.AreEqual(1, trace.Repeats.Count, text);
                Assert.AreEqual(4, trace.Repeats[0].Count, text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_SkippedTraceInsideTheScopeOfWhereTheCallbackWasScheduledFrom_DoesNotIncludeItTwice()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Like a scheduled callback without its own trace (whose scope is where it was scheduled from) that calls Promise.New.
                var scheduledFrom = CaptureAtOther();
                CausalityTrace trace, contained;
                var scope = CausalityTrace.EnterScope(scheduledFrom);
                try
                {
                    trace = CaptureContainedForTraceTest(scheduledFrom, scheduledFrom, scheduledFrom, out contained);
                }
                finally
                {
                    scope.Dispose();
                }

                var text = trace.ToString();
                Assert.AreSame(scheduledFrom, trace.Previous, text);
                Assert.IsNull(OuterTrace(trace), text);
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureAtOther)), text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_TraceContainedByATraceScheduledFromTheSameTrace_IsSkipped(
            [Values] bool isCopy)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                var scheduledFrom = CaptureAtStep();
                // A copy of the same trace, like copies made on different threads.
                var deeperScheduledFrom = isCopy ? CopyTrace(scheduledFrom, false) : scheduledFrom;
                var trace = CaptureContainedForTraceTest(other, scheduledFrom, deeperScheduledFrom, out var contained);

                // Both traces were scheduled from the same trace, so nothing is lost.
                var text = trace.ToString();
                Assert.AreSame(scheduledFrom, OuterTrace(contained), text);
                Assert.AreSame(other, trace.Previous, text);
                Assert.AreSame(deeperScheduledFrom, OuterTrace(trace), text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_PromiseNewInAContinuationOnAContext_SkipsTheTraceOfPromiseNew()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var deferred = Promise.NewDeferred();
                var traces = new CausalityTrace[2];
                PromiseNewAfterSwitchForTraceTest(deferred.Promise, context, traces).Forget();
                ResolveForTraceTest(deferred);
                context.Execute();

                // Both traces were captured in the continuation, so they were scheduled from the same trace.
                var promiseTrace = traces[0];
                var callbackTrace = traces[1];
                var text = callbackTrace.ToString();
                Assert.IsNotNull(OuterTrace(promiseTrace), promiseTrace.ToString());
                Assert.AreSame(promiseTrace.Previous, callbackTrace.Previous, text);
                Assert.AreSame(OuterTrace(promiseTrace), OuterTrace(callbackTrace), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static async Promise PromiseNewAfterSwitchForTraceTest(Promise promise, QueuedContext context, CausalityTrace[] traces)
        {
            await promise.ConfigureAwait(new ContinuationOptions(context));
            var newPromise = Promise.New(deferred =>
            {
                traces[1] = CausalityTrace.Capture();
                deferred.Resolve();
            });
            traces[0] = newPromise._ref.Trace;
            await newPromise;
        }

        [Test]
        public void CausalityTraceCapture_SkippedTraceWithoutOuterTraces_OuterTraceAtTheSameLocationAsTheNewPreviousTraceIsDropped()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var steps = new CausalityTrace[2];
                for (int i = 0; i < 2; ++i)
                {
                    steps[i] = CaptureAtStep();
                }
                // The deeper trace is scheduled from a repeat of the contained trace's previous trace, which is its previous trace after the skip.
                var trace = CaptureContainedForTraceTest(steps[0], null, steps[1], out _);

                var text = trace.ToString();
                Assert.AreSame(steps[0], trace.Previous, text);
                Assert.IsNull(OuterTrace(trace), text);
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureAtStep)), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_SkippedTraceScheduledFromTheSameLocationAsTheTrace_ItsOuterTraceIsDropped()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                // Like a callback that schedules itself again from Promise.New's callback: the second time, both traces are scheduled from the first deeper trace,
                // which was captured at the same location as the second deeper trace.
                var traces = new CausalityTrace[2];
                CausalityTrace contained = null;
                for (int i = 0; i < 2; ++i)
                {
                    var scheduledFrom = i == 0 ? null : traces[0];
                    traces[i] = CaptureContainedForTraceTest(other, scheduledFrom, scheduledFrom, out contained);
                }

                var trace = traces[1];
                var text = trace.ToString();
                Assert.AreSame(traces[0].StackTrace, OuterTrace(contained).StackTrace, text);
                Assert.AreSame(other, trace.Previous, text);
                Assert.IsNull(OuterTrace(trace), text);
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureAtLeaf)), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_CallbackThatSchedulesItselfAgainInPromiseNew_IsNotScheduledFromItself()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var scheduledFrom = new CausalityTrace[1];
                ScheduleInPromiseNewForTraceTest(context, scheduledFrom, 3);
                context.Execute();

                // The last continuation was scheduled from the same location as the one before it, so that isn't one of its outer traces.
                var text = scheduledFrom[0].ToString();
                Assert.IsFalse(scheduledFrom[0].OuterTraces.Any(trace => HasSameFrames(trace, scheduledFrom[0])), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static void ScheduleInPromiseNewForTraceTest(QueuedContext context, CausalityTrace[] scheduledFrom, int count)
        {
            // The promise is awaited from the callback.
            _ = Promise.New(deferred =>
            {
                ContinueOnContextForTraceTest(deferred.Promise, context, scheduledFrom, count).Forget();
                deferred.Resolve();
            });
        }

        private static async Promise ContinueOnContextForTraceTest(Promise promise, QueuedContext context, CausalityTrace[] scheduledFrom, int count)
        {
            await promise.ConfigureAwait(new ContinuationOptions(context, CompletedContinuationBehavior.Asynchronous));
            // The continuation runs in the scope of where it was scheduled from, which is the outermost scope, because the test runs the context outside of any scope.
            var traces = CausalityTrace.ts_traces.ToArray();
            scheduledFrom[0] = traces[traces.Length - 1];
            if (count > 1)
            {
                ScheduleInPromiseNewForTraceTest(context, scheduledFrom, count - 1);
            }
        }

        [Test]
        public void CausalityTraceCapture_OuterTraceThatIsThePreviousTraceWithoutVisibleFrames_IsDropped()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Like a callback without its own trace, whose scope is where it was scheduled from, which entered the scope again.
                var hidden = CaptureWithoutFrames();
                CausalityTrace trace;
                var scope = CausalityTrace.EnterScope(hidden);
                try
                {
                    trace = CaptureInScopeForTraceTest(hidden);
                }
                finally
                {
                    scope.Dispose();
                }

                var text = trace.ToString();
                Assert.AreSame(hidden, trace.Previous, text);
                Assert.IsNull(OuterTrace(trace), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_SkippedTraceInsideTheScopeOfWhereTheCallbackWasScheduledFromWithoutVisibleFrames_DoesNotIncludeItTwice()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Like a scheduled callback without its own trace (whose scope is where it was scheduled from) that calls Promise.New.
                var hidden = CaptureWithoutFrames();
                var trace = CaptureContainedForTraceTest(hidden, hidden, hidden, out _);

                var text = trace.ToString();
                Assert.AreSame(hidden, trace.Previous, text);
                Assert.IsNull(OuterTrace(trace), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_InsideTheScopeOfATraceWithoutVisibleFrames_KeepsIt()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // A trace without visible frames has nothing to compare, so it's not contained by the trace.
                var hidden = CaptureWithoutFrames();
                var trace = CaptureInScopeForTraceTest(hidden);

                Assert.AreSame(hidden, trace.Previous, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_WithoutVisibleFramesInsideTheScopeOfATraceWithoutVisibleFrames_IsNotARepeat()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Traces without visible frames have nothing to compare, so they're not repeats of each other.
                var hidden = CaptureWithoutFrames();
                CausalityTrace trace;
                var scope = CausalityTrace.EnterScope(hidden);
                try
                {
                    trace = CaptureWithoutFrames();
                }
                finally
                {
                    scope.Dispose();
                }

                Assert.AreSame(hidden, trace.Previous, trace.ToString());
                Assert.AreEqual(0, trace.Repeats.Count, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_InACallbackThatAContextRanSynchronously_DoesNotIncludeWhereItWasScheduled()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new InlineContext();
                var deferred = Promise.NewDeferred();
                CausalityTrace trace = null;
                CaptureAfterAwaitOnContextForTraceTest(deferred.Promise, context, t => trace = t).Forget();
                ResolveForTraceTest(deferred);

                // What resolved the deferred is already in the trace, so where it was scheduled is not one of its outer traces.
                var text = trace.ToString();
                Assert.IsTrue(ContainsMethod(trace, nameof(ResolveForTraceTest)), text);
                Assert.IsFalse(trace.OuterTraces.Any(outer => ContainsMethod(outer, nameof(ResolveForTraceTest))), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }
#endif // !PROTO_PROMISE_DEVELOPER_MODE

        [Test]
        public void CausalityTraceCapture_RepeatOfThePreviousTrace_ItsOuterTracesAreKept()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                var scheduledFrom = CaptureAtStep();
                // Captured from the same line, so that the second trace is a repeat of the first.
                var trace = other;
                for (int i = 0; i < 2; ++i)
                {
                    var scope = CausalityTrace.EnterScope(i == 0 ? scheduledFrom : null);
                    try
                    {
                        trace = CaptureInScopeForTraceTest(trace);
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                }

                var text = trace.ToString();
                Assert.AreEqual(1, trace.Repeats.Count, text);
                Assert.AreSame(other, trace.Previous, text);
                Assert.AreSame(scheduledFrom, OuterTrace(trace), text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_RepeatOfThePreviousTrace_ItsOwnOuterTracesAreFirst(
            [Values] bool previousHasAnOuterTrace)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                var scheduledFrom = CaptureAtStep();
                var previousScheduledFrom = previousHasAnOuterTrace ? CaptureAtLoop() : null;
                // Captured from the same line, so that the second trace is a repeat of the first.
                var trace = other;
                for (int i = 0; i < 2; ++i)
                {
                    var scope = CausalityTrace.EnterScope(i == 0 ? previousScheduledFrom : scheduledFrom);
                    try
                    {
                        trace = CaptureInScopeForTraceTest(trace);
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                }

                // The repeat's outer traces are kept after the trace's own.
                var text = trace.ToString();
                Assert.AreEqual(1, trace.Repeats.Count, text);
                CollectionAssert.AreEqual(previousHasAnOuterTrace ? new[] { scheduledFrom, previousScheduledFrom } : new[] { scheduledFrom }, trace.OuterTraces, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_RepeatOfATruncatedTrace_TheTraceIsTruncated()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // The second trace is captured inside the scope of a truncated copy of the first, from the same line.
                var trace = CaptureAtOther();
                for (int i = 0; i < 2; ++i)
                {
                    trace = CaptureInScopeForTraceTest(i == 0 ? trace : CopyTrace(trace, true));
                }

                var text = trace.ToString();
                Assert.AreEqual(1, trace.Repeats.Count, text);
                Assert.IsNull(trace.Previous, text);
                Assert.IsTrue(trace.IsTruncated, text);
                StringAssert.Contains("Older traces truncated", text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_OuterTraceAtTheSameLocationAsTheTrace_IsDropped()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                // Like an operation whose trace was captured where it was scheduled: both traces are captured from the same line.
                CausalityTrace trace = null;
                for (int i = 0; i < 2; ++i)
                {
                    var scope = CausalityTrace.EnterScope(trace);
                    try
                    {
                        trace = CaptureInScopeForTraceTest(other);
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                }

                var text = trace.ToString();
                Assert.AreSame(other, trace.Previous, text);
                Assert.IsNull(OuterTrace(trace), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_OuterTraceAtTheSameLocationAsThePreviousTrace_IsDropped()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var steps = new CausalityTrace[2];
                for (int i = 0; i < 2; ++i)
                {
                    steps[i] = CaptureAtStep();
                }
                CausalityTrace trace;
                var scope = CausalityTrace.EnterScope(steps[1]);
                try
                {
                    trace = CaptureInScopeForTraceTest(steps[0]);
                }
                finally
                {
                    scope.Dispose();
                }

                var text = trace.ToString();
                Assert.AreSame(steps[0], trace.Previous, text);
                Assert.IsNull(OuterTrace(trace), text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_InAListenerOfAQueuedEvent_IncludesWhereTheEventWasRaised()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Like an event that is raised, then dispatched later: the dispatcher enters the scope of where the event was raised,
                // then each listener is invoked in the scope of where it was added.
                var listenerTrace = CaptureAtStep();
                var raiseTrace = CaptureAtLoop();
                CausalityTrace trace;
                var scope = CausalityTrace.EnterScope(raiseTrace);
                try
                {
                    trace = CaptureInScopeForTraceTest(listenerTrace);
                }
                finally
                {
                    scope.Dispose();
                }

                // The listener's trace is the cause, and where the event was raised is an outer trace.
                var text = trace.ToString();
                Assert.AreSame(listenerTrace, trace.Previous, text);
                CollectionAssert.AreEqual(new[] { raiseTrace }, trace.OuterTraces, text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_OuterTraces_EachIsIndentedOneMoreLevel()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                var outers = new[] { CaptureAtStep(), CaptureAtLoop() };
                CausalityTrace trace;
                // The outermost scope is entered first.
                var outerScope = CausalityTrace.EnterScope(outers[1]);
                try
                {
                    var innerScope = CausalityTrace.EnterScope(outers[0]);
                    try
                    {
                        trace = CaptureInScopeForTraceTest(other);
                    }
                    finally
                    {
                        innerScope.Dispose();
                    }
                }
                finally
                {
                    outerScope.Dispose();
                }

                var text = trace.ToString();
                CollectionAssert.AreEqual(outers, trace.OuterTraces, text);
                // The outer traces are printed after the trace, from the innermost, each indented one more level than the one before it.
                StringAssert.IsMatch(@"(?m)^      --- Thread \d+ .*, \d+\.\d{3} ms earlier ---\r?$", text);
                StringAssert.IsMatch(@"(?m)^         --- Thread \d+ .*, \d+\.\d{3} ms earlier ---\r?$", text);
                Assert.Less(text.IndexOf(nameof(CaptureAtStep)), text.IndexOf(nameof(CaptureAtLoop)), text);
                Assert.Less(text.IndexOf(nameof(CaptureAtLoop)), text.IndexOf(nameof(CaptureAtOther)), text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_InsideAScopeWithANullTrace_KeepsTheTracesOfTheScopesOutsideOfIt(
            [Values] bool hasInnerScope)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // A scope with a null trace doesn't change the current trace, so the traces outside of it are kept.
                var outer = CaptureAtStep();
                var inner = CaptureAtLoop();
                CausalityTrace trace;
                var outerScope = CausalityTrace.EnterScope(outer);
                try
                {
                    var nullScope = CausalityTrace.EnterScope(null);
                    try
                    {
                        trace = hasInnerScope ? CaptureInScopeForTraceTest(inner) : CaptureAtLeaf();
                    }
                    finally
                    {
                        nullScope.Dispose();
                    }
                }
                finally
                {
                    outerScope.Dispose();
                }

                var text = trace.ToString();
                Assert.AreSame(hasInnerScope ? inner : outer, trace.Previous, text);
                CollectionAssert.AreEqual(hasInnerScope ? new[] { outer } : new CausalityTrace[0], trace.OuterTraces, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceOuterTraces_IsCachedAndReadOnly()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var outer = CaptureAtStep();
                var cause = CaptureAtLoop();
                CausalityTrace trace;
                var scope = CausalityTrace.EnterScope(outer);
                try
                {
                    trace = CaptureInScopeForTraceTest(cause);
                }
                finally
                {
                    scope.Dispose();
                }

                var outerTraces = trace.OuterTraces;
                Assert.AreSame(cause, trace.Previous, trace.ToString());
                CollectionAssert.AreEqual(new[] { outer }, outerTraces, trace.ToString());
                Assert.AreSame(outerTraces, trace.OuterTraces);
                Assert.Throws<System.NotSupportedException>(() => ((IList<CausalityTrace>) outerTraces)[0] = null);
                // A trace that was captured outside of any scope has no previous or outer traces.
                Assert.IsNull(outer.Previous);
                CollectionAssert.IsEmpty(outer.OuterTraces);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_InsideTheScopeOfThePreviousIteration_DoesNotKeepTheEarlierIterations()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Each iteration's trace is captured inside the scope of the previous iteration's trace, so it's an outer trace of it.
                // The outer traces are copies without their histories, so the iterations don't keep each other.
                var traces = new[] { 4, 16 }.Select(iterations => GetTraceFromNestedIterations(iterations)).ToArray();
                foreach (var trace in traces)
                {
                    Assert.AreEqual(1, trace.OuterTraces.Count, trace.ToString());
                }
                Assert.AreEqual(CountReachableTraces(traces[0]), CountReachableTraces(traces[1]));
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        // Each iteration captures its trace inside the scope of the previous iteration's trace, and inside the scope of another trace, so that the previous iteration's
        // trace is an outer trace instead of the previous trace. They're captured at alternating locations, so that they're not repeats of each other.
        private static CausalityTrace GetTraceFromNestedIterations(int iterations)
        {
            var innerTrace = CausalityTrace.Capture();
            var trace = CaptureAtStep();
            for (int i = 0; i < iterations; ++i)
            {
                var outerScope = CausalityTrace.EnterScope(trace);
                try
                {
                    var innerScope = CausalityTrace.EnterScope(innerTrace);
                    try
                    {
                        trace = i % 2 == 0 ? CaptureAtLeaf() : CaptureAtStep();
                    }
                    finally
                    {
                        innerScope.Dispose();
                    }
                }
                finally
                {
                    outerScope.Dispose();
                }
            }
            return trace;
        }

        // The number of distinct traces that the trace keeps, through its previous traces.
        private static int CountReachableTraces(CausalityTrace trace)
        {
            var reached = new HashSet<CausalityTrace>();
            var pending = new Stack<CausalityTrace>();
            pending.Push(trace);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (reached.Add(current))
                {
                    if (current.Previous != null)
                    {
                        pending.Push(current.Previous);
                    }
                    foreach (var outer in current.OuterTraces)
                    {
                        pending.Push(outer);
                    }
                }
            }
            return reached.Count;
        }

        [Test]
        public void CausalityTraceToString_RepeatedPartOfTheChainScheduledFromAnotherLocation_IsNotFolded()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                var scheduledFroms = new[] { CaptureAtStep(), CaptureAtLoop() };
                var traces = new CausalityTrace[2];
                for (int i = 0; i < 2; ++i)
                {
                    var scope = CausalityTrace.EnterScope(scheduledFroms[i]);
                    try
                    {
                        traces[i] = CaptureInScopeForTraceTest(other);
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                }
                CollectionAssert.AreEqual(new[] { scheduledFroms[0] }, traces[0].OuterTraces);
                CollectionAssert.AreEqual(new[] { scheduledFroms[1] }, traces[1].OuterTraces);

                // The traces have the same frames, but folding them would lose where one of them was scheduled from.
                var trace = CopyTrace(traces[1], false, CopyTrace(traces[0], false, other));
                var text = trace.ToString();
                StringAssert.DoesNotContain("--- Repeated", text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_RepeatedPartOfTheChainWithOtherOuterTraces_IsNotFolded(
            [Values] bool newerHasMore,
            [Values] bool onlyTheTruncationDiffers)
        {
            if (onlyTheTruncationDiffers)
            {
                CausalityTrace.MaxOuterTraces = 1;
            }
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                var scheduledFrom = CaptureAtStep();
                // Both copies are captured in as many scopes, so that they have the same frames (a null scope doesn't change the current trace).
                // The one with more has one more outer trace, or the same outer trace and one that was truncated.
                var fewer = onlyTheTruncationDiffers ? new[] { scheduledFrom, null } : new CausalityTrace[] { null, null };
                var more = new[] { scheduledFrom, onlyTheTruncationDiffers ? CaptureAtLoop() : null };
                var traces = new CausalityTrace[2];
                for (int i = 0; i < 2; ++i)
                {
                    traces[i] = CaptureInScopesForTraceTest(other, (i == 1) == newerHasMore ? more : fewer);
                }
                var withMore = traces[newerHasMore ? 1 : 0];
                var withFewer = traces[newerHasMore ? 0 : 1];
                CollectionAssert.AreEqual(new[] { scheduledFrom }, withMore.OuterTraces);
                Assert.AreEqual(onlyTheTruncationDiffers, withMore.AreOuterTracesTruncated);
                Assert.AreEqual(onlyTheTruncationDiffers ? 1 : 0, withFewer.OuterTraces.Count);
                Assert.IsFalse(withFewer.AreOuterTracesTruncated);

                // The traces have the same frames, but folding them would lose one of their outer traces, or that it was truncated.
                var trace = CopyTrace(traces[1], false, CopyTrace(traces[0], false, other));
                var text = trace.ToString();
                StringAssert.DoesNotContain("--- Repeated", text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                CausalityTrace.MaxOuterTraces = 8;
            }
        }

#if PROMISE_DEBUG // In optimized code, two calls in one method may report the same line.
        [Test]
        public void CausalityTraceCapture_InsideTheScopeOfATraceCapturedAtAnotherLineOfTheSameCall_KeepsIt()
        {
            if (!s_hasLineNumbers)
            {
                Assert.Ignore("Without line numbers, the traces are printed the same, so the previous trace is skipped.");
            }

            CausalityTrace.IsEnabled = true;
            try
            {
                var traces = CaptureTwiceForTraceTest();

                // The method is on the stack, but the previous trace was captured at another line, which is not in the trace.
                Assert.AreSame(traces[0], traces[1].Previous);
                AssertCausalityTraceIsPrintedCorrectly(traces[1], traces[1].ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace[] CaptureTwiceForTraceTest()
        {
            var trace = CausalityTrace.Capture();
            return new[] { trace, CaptureInScopeForTraceTest(trace) };
        }

#if !ENABLE_IL2CPP // IL2CPP doesn't provide accurate IL offsets.
        [Test]
        public void CausalityTraceCapture_InsideTheScopeOfATraceWithTheSameFramesFromAnotherCallOnTheSameLine_KeepsIt()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var traces = CaptureTwiceFromOneLineForTraceTest();

                // The traces are printed the same, but they were captured by different calls, so the previous trace is not a repeat,
                // and the trace has no more frames than the previous trace, so it doesn't contain it.
                Assert.AreSame(traces[0], traces[1].Previous);
                Assert.AreEqual(0, traces[1].Repeats.Count);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace[] CaptureTwiceFromOneLineForTraceTest()
        {
            // Both traces are captured on the same line, so that they have the same method and line, at different IL offsets.
            var traces = new CausalityTrace[2]; traces[0] = CausalityTrace.Capture(); var scope = CausalityTrace.EnterScope(traces[0]); try { traces[1] = CausalityTrace.Capture(); } finally { scope.Dispose(); }
            return traces;
        }

        [Test]
        public void CausalityTraceToString_TracesFromDifferentCallsOnTheSameLine_AreNotFolded()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Copies that are next to each other in the chain, so that the printer compares them.
                var traces = CaptureTwiceFromOneLineForTraceTest();
                var root = CopyTrace(CaptureAtRoot(), false, CopyTrace(traces[1], false, CopyTrace(traces[0], false, CaptureAtOther())));

                // They're printed the same, but they were captured by different calls, so they're not repeats.
                var text = root.ToString();
                Assert.AreEqual(2, CountOccurrences(text, nameof(CaptureTwiceFromOneLineForTraceTest)), text);
                StringAssert.DoesNotContain("Repeated", text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_ScheduledFromAnotherCallOnTheSameLineOfAMethodThatIsStillRunning_KeepsIt()
        {
            // The second switch captures where it awaits, which is where it switches.
            Promise.Config.AwaitTracesEnabled = true;
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var trace = new CausalityTrace[1];
                SwitchAndCaptureOnOneLineForTraceTest(context, trace).Forget();
                context.Execute();

                // The context ran the continuation from the method that scheduled it, so where it was scheduled from is printed like the outer frames of the trace,
                // but the async function had moved on to another call on the same line, so the context didn't run it synchronously.
                Assert.IsNotNull(OuterTrace(trace[0]), trace[0].ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.AwaitTracesEnabled = false;
            }
        }

        private static async Promise SwitchAndCaptureOnOneLineForTraceTest(QueuedContext context, CausalityTrace[] trace)
        {
            await Promise.SwitchToContextAwait(context, forceAsync: true);
            // Scheduled from inside the context on the same line as the capture, so that they have the same method and line, at different IL offsets.
            await Promise.SwitchToContextAwait(context, forceAsync: true); trace[0] = CaptureAtLeaf();
        }
#endif // !ENABLE_IL2CPP
#endif // PROMISE_DEBUG

        [Test]
        public void CausalityTraceToString_RepeatedPartOfTheChain_IsPrintedOnce()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // A part of the chain with two traces repeats, like two callbacks that schedule each other.
                var step = CaptureAtStep();
                var loop = CaptureAtLoop();
                var trace = CaptureAtOther();
                for (int i = 0; i < 3; ++i)
                {
                    trace = CopyTrace(step, false, CopyTrace(loop, false, trace));
                }
                var root = CopyTrace(CaptureAtRoot(), false, trace);

                var text = root.ToString();
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureAtStep)), text);
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureAtLoop)), text);
                StringAssert.IsMatch(@"--- #1 repeated 2 times: Thread \d+ .* 2 times at \d+\.\d{3} to \d+\.\d{3} ms earlier ---", text);
                StringAssert.Contains(nameof(CaptureAtOther), text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_RepeatedPartOfTheChainWithThreeTraces_IsPrintedOnce()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Like three callbacks that schedule each other.
                var step = CaptureAtStep();
                var loop = CaptureAtLoop();
                var leaf = CaptureAtLeaf();
                var trace = CaptureAtOther();
                for (int i = 0; i < 3; ++i)
                {
                    trace = CopyTrace(step, false, CopyTrace(loop, false, CopyTrace(leaf, false, trace)));
                }
                var root = CopyTrace(CaptureAtRoot(), false, trace);

                var text = root.ToString();
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureAtStep)), text);
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureAtLoop)), text);
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureAtLeaf)), text);
                StringAssert.Contains("--- #1 repeated 2 times: ", text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if !UNITY_WEBGL
        [Test]
        public void CausalityTraceToString_RepeatedPartOfTheChainOnOtherThreads_IncludesTheThreadOfEachCopy()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // Each copy of the part was captured on another thread, from the same frames.
                var steps = new CausalityTrace[3];
                var loops = new CausalityTrace[3];
                RunOnDifferentNewThreads(3, i =>
                {
                    steps[i] = CaptureAtStep();
                    loops[i] = CaptureAtLoop();
                });
                var trace = CaptureAtOther();
                for (int i = 0; i < 3; ++i)
                {
                    trace = CopyTrace(steps[i], false, CopyTrace(loops[i], false, trace));
                }
                var root = CopyTrace(CaptureAtRoot(), false, trace);

                // The latest copy is printed, and the threads and times of the earlier copies are listed from the latest to the earliest.
                var text = root.ToString();
                StringAssert.IsMatch($@"--- Thread {steps[2].ThreadId} \(background\), \d+\.\d{{3}} ms earlier ---", text);
                StringAssert.IsMatch($@"--- #1 repeated 2 times: Thread {steps[1].ThreadId} \(background\) 1 time at \d+\.\d{{3}} ms earlier; "
                    + $@"Thread {steps[0].ThreadId} \(background\) 1 time at \d+\.\d{{3}} ms earlier ---", text);
                AssertCausalityTraceIsPrintedCorrectly(root, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        // Runs the action on new threads, one after another. Each thread is blocked until every action has run, so that the threads have different ids.
        private static void RunOnDifferentNewThreads(int count, System.Action<int> action)
        {
            var threads = new System.Threading.Thread[count];
            System.Runtime.ExceptionServices.ExceptionDispatchInfo exception = null;
            // The events are not disposed, because a thread may still use them after a wait timed out.
            var release = new System.Threading.ManualResetEventSlim();
            try
            {
                for (int i = 0; i < count; ++i)
                {
                    int index = i;
                    var ran = new System.Threading.ManualResetEventSlim();
                    threads[i] = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            action(index);
                        }
                        catch (System.Exception e)
                        {
                            exception = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e);
                        }
                        finally
                        {
                            ran.Set();
                        }
                        release.Wait();
                    }) { IsBackground = true };
                    threads[i].Start();
                    Assert.IsTrue(ran.Wait(System.TimeSpan.FromSeconds(10)), "The thread did not run the action in time.");
                }
            }
            finally
            {
                release.Set();
                foreach (var thread in threads)
                {
                    if (thread != null)
                    {
                        Assert.IsTrue(thread.Join(System.TimeSpan.FromSeconds(10)), "The thread did not exit in time.");
                    }
                }
            }
            exception?.Throw();
        }
#endif // !UNITY_WEBGL

        [Test]
        public void CausalityTraceToString_RepeatedPartOfTheChain_IncludesTheRepeatsOfEachCopy()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                // The traces with repeats are captured from the same line, so that they have the same stack trace.
                var withRepeats = new[] { 3, 5 }.Select(count => GetTraceWithRepeatsForTraceTest(count)).ToArray();
                var step = CaptureAtStep();
                var trace = CaptureAtOther();
                foreach (var repeated in withRepeats)
                {
                    trace = CopyTrace(step, false, CopyTrace(repeated, false, trace));
                }

                var text = trace.ToString();
                StringAssert.Contains("--- #1 repeated 1 time: ", text);
                StringAssert.Contains("--- Repeated 8 times: ", text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_OneRepeat_SaysOneTime()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var trace = GetTraceWithRepeatsForTraceTest(1);

                var text = trace.ToString();
                StringAssert.Contains("--- Repeated 1 time: ", text);
                StringAssert.DoesNotContain("1 times", text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceToString_UnnamedThreads_AreDescribedByTheirKind()
        {
            // Checked directly, because newer runtimes name their thread pool threads.
            var sb = new System.Text.StringBuilder();
            CausalityTrace.Printer.AppendThreadDescription(sb, 5, null, true, true);
            Assert.AreEqual("Thread 5 (thread pool)", sb.ToString());

            sb.Clear();
            CausalityTrace.Printer.AppendThreadDescription(sb, 6, null, false, true);
            Assert.AreEqual("Thread 6 (background)", sb.ToString());

            sb.Clear();
            CausalityTrace.Printer.AppendThreadDescription(sb, 7, null, false, false);
            Assert.AreEqual("Thread 7 (foreground)", sb.ToString());

            sb.Clear();
            CausalityTrace.Printer.AppendThreadDescription(sb, 8, "Worker", true, true);
            Assert.AreEqual("Thread 8 \"Worker\"", sb.ToString());
        }

        // Gets a trace with the count of repeats.
        private static CausalityTrace GetTraceWithRepeatsForTraceTest(int count)
        {
            var trace = CaptureAtRoot();
            for (int i = 0; i <= count; ++i)
            {
                trace = CaptureInScopeForTraceTest(trace);
            }
            return CopyTrace(trace, false);
        }

        [Test]
        public void CausalityTraceCapture_AfterSequentialAwaits_DoesNotIncludeTheEarlierAwaits()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var pending = new Queue<Promise.Deferred>();
                // The trace, and the scope where it was captured.
                var trace = new CausalityTrace[2];
                var promise = InitializeForTraceTest(pending, trace);
                var asyncTrace = promise._ref.Trace;
                promise.Forget();
                while (pending.Count > 0)
                {
                    pending.Dequeue().Resolve();
                }

                // The operations that the async function awaited were started inside its scope, so their causes are the async function, not each other.
                // The trace's cause is where the async function was called, and the code that completed the last operation is on the stack.
                if (s_hasLineNumbers)
                {
                    Assert.AreEqual(2, GetCausalityTraceDepth(trace[0]), trace[0].ToString());
                }
                else
                {
                    // Without line numbers, where the async function was called may be printed like the outer frames of the trace, so it's skipped.
                    Assert.LessOrEqual(GetCausalityTraceDepth(trace[0]), 2, trace[0].ToString());
                }
                Assert.IsFalse(HistoryContainsMethod(trace[0], nameof(LoadConfigForTraceTest)), trace[0].ToString());
                Assert.IsFalse(HistoryContainsMethod(trace[0], nameof(ConnectForTraceTest)), trace[0].ToString());
                Assert.IsNotNull(asyncTrace);
                Assert.AreSame(asyncTrace, trace[1]);
                AssertPreviousTraceIsFrom(trace[0], nameof(CausalityTraceCapture_AfterSequentialAwaits_DoesNotIncludeTheEarlierAwaits));
                AssertCausalityTraceIsPrintedCorrectly(trace[0], trace[0].ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static async Promise InitializeForTraceTest(Queue<Promise.Deferred> pending, CausalityTrace[] trace)
        {
            await LoadConfigForTraceTest(pending);
            await ConnectForTraceTest(pending);
            await GetPendingPromise(pending);
            trace[0] = CausalityTrace.Capture();
            trace[1] = GetCurrentTraceForTraceTest();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise LoadConfigForTraceTest(Queue<Promise.Deferred> pending)
        {
            var promise = GetPendingPromise(pending);
            Volatile.Write(ref s_frameKeeper, 13);
            return promise;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise ConnectForTraceTest(Queue<Promise.Deferred> pending)
        {
            var promise = GetPendingPromise(pending);
            Volatile.Write(ref s_frameKeeper, 14);
            return promise;
        }

        [Test]
        public void CausalityTraceToString_LoopOnAContext_DoesNotGrowWithTheIterations()
        {
            // Each iteration captures where it awaits, which is where it switches.
            Promise.Config.AwaitTracesEnabled = true;
            CausalityTrace.IsEnabled = true;
            try
            {
                // Like a loop that runs once per frame on a game's main thread.
                // The traces are captured from the same line, so that their stack traces are the same.
                var traces = new[] { 4, 16 }.Select(iterations => GetTraceFromLoopOnContext(iterations)).ToArray();
                var text4 = traces[0].ToString();
                var trace16 = traces[1];
                var text16 = trace16.ToString();
                AssertPrintedTheSame(text4, text16);
                // The continuation was scheduled by the previous iteration.
                if (s_hasLineNumbers)
                {
                    Assert.AreEqual(2, GetCausalityTraceDepth(trace16), text16);
                }
                else
                {
                    // Without line numbers, the previous trace may be printed like the outer frames of the trace, so it's skipped.
                    Assert.LessOrEqual(GetCausalityTraceDepth(trace16), 2, text16);
                }
                // The async function's frame is its state machine's MoveNext, which is printed with the function's name.
                // The context runs it from this method (see KeepsOuterTracesFromTheSameMethod).
                if (KeepsOuterTracesFromTheSameMethod)
                {
                    StringAssert.Contains(nameof(SwitchInLoopForTraceTest), OuterTrace(trace16).ToString());
                    // Outer traces are indented one more level.
                    StringAssert.Contains("      --- Thread ", text16);
                }
                else
                {
                    Assert.IsNull(OuterTrace(trace16), text16);
                }
                AssertCausalityTraceIsPrintedCorrectly(trace16, text16);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.AwaitTracesEnabled = false;
            }
        }

        private static CausalityTrace GetTraceFromLoopOnContext(int iterations)
        {
            var context = new QueuedContext();
            var trace = new CausalityTrace[1];
            SwitchInLoopForTraceTest(context, iterations, trace).Forget();
            context.Execute();
            return trace[0];
        }

        private static async Promise SwitchInLoopForTraceTest(QueuedContext context, int iterations, CausalityTrace[] trace)
        {
            for (int i = 0; i < iterations; ++i)
            {
                await Promise.SwitchToContextAwait(context, forceAsync: true);
            }
            trace[0] = CaptureAtLeaf();
        }

#if !UNITY_WEBGL
        [Test]
        public void CausalityTraceCapture_AfterContinuingOnTheThreadPool_IncludesWhereItWasScheduledFrom()
        {
            var backgroundContext = Promise.Config.BackgroundContext;
            // Without a background context, background continuations run on the thread pool.
            Promise.Config.BackgroundContext = null;
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = Promise.NewDeferred();
                CausalityTrace trace = null;
                var promise = CaptureAfterAwaitOnBackgroundForTraceTest(deferred.Promise, t => trace = t);
                ResolveForTraceTest(deferred);
                Assert.IsTrue(promise.TryWait(System.TimeSpan.FromSeconds(10)));

                var text = trace.ToString();
                Assert.IsTrue(trace.IsThreadPoolThread, text);
                Assert.IsNotNull(OuterTrace(trace), text);
                Assert.IsTrue(ContainsMethod(OuterTrace(trace), nameof(ResolveForTraceTest)), text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.BackgroundContext = backgroundContext;
            }
        }

        private static async Promise CaptureAfterAwaitOnBackgroundForTraceTest(Promise promise, System.Action<CausalityTrace> onCaptured)
        {
            await promise.ConfigureAwait(ContinuationOptions.Background);
            onCaptured(CaptureAtLeaf());
        }

        // Captures a trace on a new unnamed thread (the threads of ThreadHelper have names).
        private static CausalityTrace CaptureOnNewThread(bool isBackground, CausalityTrace previous)
            => CaptureOnNewThreadWith(isBackground, () => CaptureInScopeForTraceTest(previous), out _);

        private static CausalityTrace CaptureOnNewThreadWith(bool isBackground, System.Func<CausalityTrace> capture, out System.Threading.Thread thread)
        {
            CausalityTrace trace = null;
            System.Runtime.ExceptionServices.ExceptionDispatchInfo exception = null;
            thread = new System.Threading.Thread(() =>
            {
                try
                {
                    trace = capture();
                }
                catch (System.Exception e)
                {
                    exception = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e);
                }
            });
            thread.IsBackground = isBackground;
            thread.Start();
            thread.Join();
            exception?.Throw();
            return trace;
        }

#if !PROTO_PROMISE_DEVELOPER_MODE // Developer mode doesn't skip frames, so the innermost frame of every trace is the same, and no trace contains another.
        [Test]
        public void CausalityTraceCapture_InsideTheScopeOfATraceWithTheOuterFramesOnAnotherThread_KeepsIt()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var previous = CaptureFromTheSameLineOnNewThread(null, false, out var previousThread);
                var trace = CaptureFromTheSameLineOnNewThread(previous, true, out _);
                // A thread's id may be reused after its Thread is collected.
                System.GC.KeepAlive(previousThread);

                // The previous trace's frames are the outer frames of the trace, but they were on another thread's stack, so they're not the calls that are running.
                var frames = trace.GetFrames();
                var previousFrames = previous.GetFrames();
                Assert.Greater(frames.Length, previousFrames.Length);
                for (int i = 1; i <= previousFrames.Length; ++i)
                {
                    Assert.AreEqual(previousFrames[previousFrames.Length - i].GetMethod(), frames[frames.Length - i].GetMethod());
                    Assert.AreEqual(previousFrames[previousFrames.Length - i].GetFileLineNumber(), frames[frames.Length - i].GetFileLineNumber());
                }
                Assert.AreNotEqual(previous.ThreadId, trace.ThreadId);
                Assert.AreSame(previous, trace.Previous);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static CausalityTrace CaptureFromTheSameLineOnNewThread(CausalityTrace previous, bool deeper, out System.Threading.Thread thread)
            => CaptureOnNewThreadWith(false, () => CaptureFromTheSameLineForTraceTest(previous, deeper), out thread);
#endif // !PROTO_PROMISE_DEVELOPER_MODE

        private static CausalityTrace CaptureOnThreadHelper(CausalityTrace previous)
        {
            CausalityTrace trace = null;
            new ThreadHelper().ExecuteSingleAction(() => trace = CaptureInScopeForTraceTest(previous));
            return trace;
        }

        private static CausalityTrace CaptureOnThreadPool(CausalityTrace previous)
        {
            CausalityTrace trace = null;
            System.Runtime.ExceptionServices.ExceptionDispatchInfo exception = null;
            // Not disposed, because the thread pool may still run the callback after the wait timed out.
            var captured = new System.Threading.ManualResetEventSlim();
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    trace = CaptureInScopeForTraceTest(previous);
                }
                catch (System.Exception e)
                {
                    exception = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e);
                }
                finally
                {
                    captured.Set();
                }
            });
            Assert.IsTrue(captured.Wait(System.TimeSpan.FromSeconds(10)), "The thread pool did not run the callback in time.");
            exception?.Throw();
            return trace;
        }

        // Captures traces on thread pool threads, each inside the scope of the previous one. Each thread is blocked until every trace is captured,
        // so that each trace is captured on a different thread.
        private static CausalityTrace[] CaptureOnDifferentThreadPoolThreads(CausalityTrace previous, int count)
        {
            var traces = new CausalityTrace[count];
            // The events are not disposed, because the threads may still be waiting on them after they're set, or use them after a wait timed out.
            var release = new System.Threading.ManualResetEventSlim();
            try
            {
                for (int i = 0; i < count; ++i)
                {
                    int index = i;
                    var scopeTrace = i == 0 ? previous : traces[i - 1];
                    var captured = new System.Threading.ManualResetEventSlim();
                    System.Threading.ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            traces[index] = CaptureInScopeForTraceTest(scopeTrace);
                        }
                        finally
                        {
                            captured.Set();
                        }
                        release.Wait();
                    });
                    Assert.IsTrue(captured.Wait(System.TimeSpan.FromSeconds(10)), "The thread pool did not run the callback in time.");
                    Assert.IsNotNull(traces[i]);
                }
            }
            finally
            {
                release.Set();
            }
            return traces;
        }

        // Captures traces on the threads of ThreadHelper in turns, each inside the scope of the previous one, with the same frames on every thread.
        private sealed class TurnCapturer
        {
            private readonly System.Threading.SemaphoreSlim[] _turns;
            private readonly System.Threading.SemaphoreSlim _captured = new System.Threading.SemaphoreSlim(0);
            private CausalityTrace _previous;
            private volatile bool _isDone;

            private TurnCapturer(int threadCount)
            {
                _turns = new System.Threading.SemaphoreSlim[threadCount];
                for (int i = 0; i < threadCount; ++i)
                {
                    _turns[i] = new System.Threading.SemaphoreSlim(0);
                }
            }

            // Captures a trace on the thread of each turn (an index of the threads), and returns the last trace.
            internal static CausalityTrace Capture(CausalityTrace previous, int threadCount, params int[] turns)
            {
                var capturer = new TurnCapturer(threadCount) { _previous = previous };
                var threadHelper = new ThreadHelper();
                for (int i = 0; i < threadCount; ++i)
                {
                    int index = i;
                    threadHelper.AddParallelAction(() => capturer.Run(index));
                }
                // The turns are given from another thread, because this thread waits for all of the actions.
                threadHelper.AddParallelAction(() => capturer.GiveTurns(turns));
                threadHelper.ExecutePendingParallelActions();
                return capturer._previous;
            }

            private void GiveTurns(int[] turns)
            {
                try
                {
                    foreach (int turn in turns)
                    {
                        _turns[turn].Release();
                        _captured.Wait();
                        if (_isDone)
                        {
                            // A capture failed.
                            break;
                        }
                    }
                }
                finally
                {
                    _isDone = true;
                    foreach (var turn in _turns)
                    {
                        turn.Release();
                    }
                }
            }

            private void Run(int threadIndex)
            {
                var turn = _turns[threadIndex];
                while (true)
                {
                    turn.Wait();
                    if (_isDone)
                    {
                        return;
                    }
                    try
                    {
                        _previous = CaptureInScopeForTraceTest(_previous);
                    }
                    catch
                    {
                        _isDone = true;
                        throw;
                    }
                    finally
                    {
                        _captured.Release();
                    }
                }
            }
        }
#endif // !UNITY_WEBGL

        [Test]
        public void CausalityTraceCapture_InContinuationOnAnotherContext_IncludesWhereTheContinuationWasScheduled()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var deferred = Promise.NewDeferred();
                CausalityTrace trace = null;
                CausalityTrace scope = null;
                var promise = CaptureAfterAwaitOnContextForTraceTest(deferred.Promise, context, captured =>
                {
                    trace = captured;
                    scope = GetCurrentTraceForTraceTest();
                });
                var asyncTrace = promise._ref.Trace;
                promise.Forget();
                ResolveForTraceTest(deferred);
                Assert.IsNull(trace);
                context.Execute();

                Assert.IsNotNull(asyncTrace);
                Assert.AreSame(asyncTrace, scope);
                // The code that resolved the deferred isn't on the stack when the continuation runs on the context, but the trace includes it.
                Assert.IsFalse(ContainsMethod(trace, nameof(ResolveForTraceTest)), trace.ToString());
                Assert.IsNotNull(OuterTrace(trace), trace.ToString());
                Assert.IsTrue(ContainsMethod(OuterTrace(trace), nameof(ResolveForTraceTest)), trace.ToString());
                // It only has its own stack trace.
                Assert.IsNull(OuterTrace(trace).Previous);
                Assert.IsNull(OuterTrace(OuterTrace(trace)));
                CollectionAssert.IsEmpty(OuterTrace(trace).Repeats);
                // The async function is still the previous trace.
                AssertPreviousTraceIsFrom(trace, nameof(CausalityTraceCapture_InContinuationOnAnotherContext_IncludesWhereTheContinuationWasScheduled));
                AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_AfterSwitchingToAnotherContext_IncludesWhatContinuedTheAsyncFunctionBeforeItSwitched_OnlyWithAwaitTraces(
            [Values] bool awaitTraces)
        {
            Promise.Config.AwaitTracesEnabled = awaitTraces;
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var deferred = Promise.NewDeferred();
                CausalityTrace trace = null;
                CaptureAfterSwitchToContextForTraceTest(deferred.Promise, context, captured => trace = captured).Forget();
                // The async function continues in ResolveForTraceTest, then switches to the context.
                ResolveForTraceTest(deferred);
                Assert.IsNull(trace);
                context.Execute();

                Assert.IsFalse(ContainsMethod(trace, nameof(ResolveForTraceTest)), trace.ToString());
                // The switch is scheduled where the async function awaits, so it's only captured as where it awaited.
                if (awaitTraces)
                {
                    Assert.IsNotNull(OuterTrace(trace), trace.ToString());
                    Assert.IsTrue(ContainsMethod(OuterTrace(trace), nameof(ResolveForTraceTest)), trace.ToString());
                }
                else
                {
                    Assert.IsFalse(HistoryContainsMethod(trace, nameof(ResolveForTraceTest)), trace.ToString());
                }
                AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.AwaitTracesEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_AfterSwitchingToAnotherContextInAnAsyncTaskFunction_DoesNotIncludeWhatContinuedItBeforeItSwitched(
            [Values] bool configureAwait)
        {
#if PROMISE_DEBUG
            if (configureAwait)
            {
                // In DEBUG mode, Promise.Resolved() is backed by an object, so the await takes the path of a pending promise, whose continuation is traced.
                Assert.Ignore();
            }
#endif
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var deferred = Promise.NewDeferred();
                CausalityTrace trace = null;
                var task = CaptureAfterSwitchToContextInTaskForTraceTest(deferred.Promise, context, configureAwait, captured => trace = captured);
                // The async function continues in ResolveForTraceTest, then switches to the context.
                ResolveForTraceTest(deferred);
                Assert.IsNull(trace);
                context.Execute();

                Assert.IsTrue(task.IsCompleted);
                task.Wait();
                // Only async Promise functions capture where they await, which is where the switch is scheduled.
                CollectionAssert.IsEmpty(trace.OuterTraces, trace.ToString());
                Assert.IsFalse(HistoryContainsMethod(trace, nameof(ResolveForTraceTest)), trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        // Switches with SwitchToContextAwait, or with ConfigureAwait on a promise that's already complete.
        private static async System.Threading.Tasks.Task CaptureAfterSwitchToContextInTaskForTraceTest(Promise promise, System.Threading.SynchronizationContext context, bool configureAwait,
            System.Action<CausalityTrace> onCaptured)
        {
            await promise;
            if (configureAwait)
            {
                await Promise.Resolved().ConfigureAwait(new ContinuationOptions(context, CompletedContinuationBehavior.Asynchronous));
            }
            else
            {
                await Promise.SwitchToContextAwait(context, forceAsync: true);
            }
            onCaptured(CausalityTrace.Capture());
        }

        [Test]
        public void UnhandledRejection_ExceptionThrownInCallback_TraceIncludesWhatInvokedTheCallback()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException("Thrown in callback");
                    var deferred = Promise.NewDeferred();
                    CausalityTrace thenScope = null;
                    var thenPromise = deferred.Promise.Then(() =>
                    {
                        thenScope = GetCurrentTraceForTraceTest();
                        throw exception;
                    });
                    var thenTrace = thenPromise._ref.Trace;
                    thenPromise.Forget();
                    ResolveForTraceTest(deferred);

                    Assert.IsNotNull(thenTrace);
                    Assert.AreSame(thenTrace, thenScope);
                    var trace = reports.GetTrace(exception);
                    // Where the exception was caught, which is inside the call that resolved the deferred.
                    Assert.IsTrue(trace.GetFrames().Any(frame => frame.GetMethod().Name == nameof(ResolveForTraceTest)), trace.ToString());
                    // The callback's operation is still the previous trace.
                    AssertPreviousTraceIsFrom(trace, nameof(UnhandledRejection_ExceptionThrownInCallback_TraceIncludesWhatInvokedTheCallback));
                    AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledRejection_ExceptionThrownInAsyncFunction_TraceIncludesWhatContinuedTheFunction()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException("Thrown in async function");
                    var deferred = Promise.NewDeferred();
                    var scope = new CausalityTrace[1];
                    var promise = ThrowAfterAwaitForTraceTest(deferred.Promise, exception, scope);
                    var asyncTrace = promise._ref.Trace;
                    promise.Forget();
                    ResolveForTraceTest(deferred);

                    Assert.IsNotNull(asyncTrace);
                    Assert.AreSame(asyncTrace, scope[0]);
                    var trace = reports.GetTrace(exception);
                    Assert.IsTrue(trace.GetFrames().Any(frame => frame.GetMethod().Name == nameof(ResolveForTraceTest)), trace.ToString());
                    // Where the async function was called is still the previous trace.
                    AssertPreviousTraceIsFrom(trace, nameof(UnhandledRejection_ExceptionThrownInAsyncFunction_TraceIncludesWhatContinuedTheFunction));
                    AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledRejection_ExceptionThrownInAsyncFunctionAfterItAwaitedTwice_TraceIncludesWhatContinuedTheFunctionBeforeItAwaited(
            [Values] bool continueOnAnotherThread)
        {
            Promise.Config.AwaitTracesEnabled = true;
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException("Thrown in async function");
                    var deferred1 = NewVoidDeferredForTraceTest();
                    var deferred2 = NewVoidDeferredForTraceTest();
                    ThrowAfterAwaitingTwiceForTraceTest(deferred1.Promise, deferred2.Promise, exception).Forget();
                    var threadHelper = new ThreadHelper();
                    threadHelper.ExecuteSynchronousOrOnThread(() => ResolveFirstForTraceTest(deferred1), !continueOnAnotherThread);
                    threadHelper.ExecuteSynchronousOrOnThread(() => ResolveSecondForTraceTest(deferred2), !continueOnAnotherThread);

                    var trace = reports.GetTrace(exception);
                    var text = trace.ToString();
                    // What continued the function is on the stack of where it threw, and what continued it before it awaited is in the trace of where it awaited.
                    Assert.IsTrue(ContainsMethod(trace, nameof(ResolveSecondForTraceTest)), text);
                    Assert.IsTrue(trace.OuterTraces.Any(outer => ContainsMethod(outer, nameof(ResolveFirstForTraceTest))), text);
                    AssertPreviousTraceIsFrom(trace, nameof(UnhandledRejection_ExceptionThrownInAsyncFunctionAfterItAwaitedTwice_TraceIncludesWhatContinuedTheFunctionBeforeItAwaited));
                    AssertCausalityTraceIsPrintedCorrectly(trace, text);
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.AwaitTracesEnabled = false;
            }
        }

        private static async Promise ThrowAfterAwaitingTwiceForTraceTest(Promise first, Promise second, System.Exception exception)
        {
            await first;
            await second;
            throw exception;
        }

        [Test]
        public void CausalityTraceCapture_AfterAwaits_IncludesWhatContinuedTheAsyncFunctionBeforeItAwaited_ButNotBeforeThat(
            [Values] bool continueOnAnotherThread,
            [Values] bool lastAwaitIsForeign)
        {
            Promise.Config.AwaitTracesEnabled = true;
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred1 = NewVoidDeferredForTraceTest();
                var deferred2 = NewVoidDeferredForTraceTest();
                // The last await is either a promise or an awaitable that isn't ProtoPromise's. The deferred is only created if it's awaited.
                var deferred3 = lastAwaitIsForeign ? default : NewVoidDeferredForTraceTest();
                var foreignAwaitable = lastAwaitIsForeign ? new ForeignAwaitableForTraceTest() : null;
                var traces = new CausalityTrace[2];
                AwaitThreeTimesForTraceTest(deferred1.Promise, deferred2.Promise, deferred3, foreignAwaitable, traces).Forget();
                var threadHelper = new ThreadHelper();
                threadHelper.ExecuteSynchronousOrOnThread(() => ResolveFirstForTraceTest(deferred1), !continueOnAnotherThread);
                threadHelper.ExecuteSynchronousOrOnThread(() => ResolveSecondForTraceTest(deferred2), !continueOnAnotherThread);
                threadHelper.ExecuteSynchronousOrOnThread(() =>
                {
                    if (lastAwaitIsForeign)
                    {
                        ContinueForeignAwaitForTraceTest(foreignAwaitable);
                    }
                    else
                    {
                        ResolveThirdForTraceTest(deferred3);
                    }
                }, !continueOnAnotherThread);

                // What continued the function is on its stack, and what continued it before it awaited is in the trace of where it awaited,
                // whose scope is entered outside of the async function's own scope.
                var text0 = traces[0].ToString();
                Assert.IsTrue(ContainsMethod(traces[0], nameof(ResolveSecondForTraceTest)), text0);
                Assert.IsNotNull(OuterTrace(traces[0]), text0);
                Assert.IsTrue(ContainsMethod(OuterTrace(traces[0]), nameof(ResolveFirstForTraceTest)), text0);
                AssertPreviousTraceIsFrom(traces[0], nameof(CausalityTraceCapture_AfterAwaits_IncludesWhatContinuedTheAsyncFunctionBeforeItAwaited_ButNotBeforeThat));

                // What continued it before that isn't kept, so that its traces don't grow with the number of times it awaited.
                var text1 = traces[1].ToString();
                Assert.IsTrue(ContainsMethod(traces[1], lastAwaitIsForeign ? nameof(ContinueForeignAwaitForTraceTest) : nameof(ResolveThirdForTraceTest)), text1);
                Assert.IsNotNull(OuterTrace(traces[1]), text1);
                Assert.IsTrue(ContainsMethod(OuterTrace(traces[1]), nameof(ResolveSecondForTraceTest)), text1);
                Assert.IsFalse(HistoryContainsMethod(traces[1], nameof(ResolveFirstForTraceTest)), text1);
                AssertPreviousTraceIsFrom(traces[1], nameof(CausalityTraceCapture_AfterAwaits_IncludesWhatContinuedTheAsyncFunctionBeforeItAwaited_ButNotBeforeThat));
                AssertCausalityTraceIsPrintedCorrectly(traces[1], text1);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.AwaitTracesEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_AfterAwaitsWithoutAwaitTraces_OnlyIncludesWhatContinuedTheAsyncFunctionLast(
            [Values] bool continueOnAnotherThread,
            [Values] bool lastAwaitIsForeign,
            [Values] bool flowExecutionContext)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            // Enabled covers the async functions that capture the ExecutionContext where they await, without capturing a trace.
            bool wasFlowExecutionContextEnabled = Promise.Config.AsyncFlowExecutionContextEnabled;
            Promise.Config.SetAsyncFlowExecutionContextEnabled(flowExecutionContext);
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred1 = NewVoidDeferredForTraceTest();
                var deferred2 = NewVoidDeferredForTraceTest();
                var deferred3 = lastAwaitIsForeign ? default : NewVoidDeferredForTraceTest();
                var foreignAwaitable = lastAwaitIsForeign ? new ForeignAwaitableForTraceTest() : null;
                var traces = new CausalityTrace[2];
                AwaitThreeTimesForTraceTest(deferred1.Promise, deferred2.Promise, deferred3, foreignAwaitable, traces).Forget();
                var threadHelper = new ThreadHelper();
                threadHelper.ExecuteSynchronousOrOnThread(() => ResolveFirstForTraceTest(deferred1), !continueOnAnotherThread);
                threadHelper.ExecuteSynchronousOrOnThread(() => ResolveSecondForTraceTest(deferred2), !continueOnAnotherThread);
                threadHelper.ExecuteSynchronousOrOnThread(() =>
                {
                    if (lastAwaitIsForeign)
                    {
                        ContinueForeignAwaitForTraceTest(foreignAwaitable);
                    }
                    else
                    {
                        ResolveThirdForTraceTest(deferred3);
                    }
                }, !continueOnAnotherThread);

                // What continued the function is on its stack, but where it awaited isn't captured, so what continued it before that isn't kept.
                var text0 = traces[0].ToString();
                Assert.IsTrue(ContainsMethod(traces[0], nameof(ResolveSecondForTraceTest)), text0);
                Assert.IsFalse(HistoryContainsMethod(traces[0], nameof(ResolveFirstForTraceTest)), text0);
                AssertPreviousTraceIsFrom(traces[0], nameof(CausalityTraceCapture_AfterAwaitsWithoutAwaitTraces_OnlyIncludesWhatContinuedTheAsyncFunctionLast));

                var text1 = traces[1].ToString();
                Assert.IsTrue(ContainsMethod(traces[1], lastAwaitIsForeign ? nameof(ContinueForeignAwaitForTraceTest) : nameof(ResolveThirdForTraceTest)), text1);
                Assert.IsFalse(HistoryContainsMethod(traces[1], nameof(ResolveSecondForTraceTest)), text1);
                AssertPreviousTraceIsFrom(traces[1], nameof(CausalityTraceCapture_AfterAwaitsWithoutAwaitTraces_OnlyIncludesWhatContinuedTheAsyncFunctionLast));
                AssertCausalityTraceIsPrintedCorrectly(traces[1], text1);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.SetAsyncFlowExecutionContextEnabled(wasFlowExecutionContextEnabled);
            }
        }

        [Test]
        public void Config_AwaitCaptureIsEnabledWhileTheExecutionContextFlowsOrAwaitsAreTraced(
            [Values] bool flowExecutionContext,
            [Values] bool causalityTraces,
            [Values] bool awaitTraces)
        {
            // Async functions check the combined setting when they await, so it must follow each setting, whichever is set last.
            bool wasFlowExecutionContextEnabled = Promise.Config.AsyncFlowExecutionContextEnabled;
            try
            {
                Promise.Config.SetAsyncFlowExecutionContextEnabled(flowExecutionContext);
                Promise.Config.AwaitTracesEnabled = awaitTraces;
                CausalityTrace.IsEnabled = causalityTraces;
                Assert.AreEqual(flowExecutionContext | (causalityTraces & awaitTraces), Promise.Config.s_isAwaitCaptureEnabled);

                CausalityTrace.IsEnabled = !causalityTraces;
                Promise.Config.AwaitTracesEnabled = !awaitTraces;
                Assert.AreEqual(flowExecutionContext | (!causalityTraces & !awaitTraces), Promise.Config.s_isAwaitCaptureEnabled);

                Promise.Config.SetAsyncFlowExecutionContextEnabled(!flowExecutionContext);
                Assert.AreEqual(!flowExecutionContext | (!causalityTraces & !awaitTraces), Promise.Config.s_isAwaitCaptureEnabled);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.AwaitTracesEnabled = false;
                Promise.Config.SetAsyncFlowExecutionContextEnabled(wasFlowExecutionContextEnabled);
            }
        }

        // The last await is the foreign awaitable if it's not null, otherwise the third deferred's promise.
        private static async Promise AwaitThreeTimesForTraceTest(Promise first, Promise second, Promise.Deferred third, ForeignAwaitableForTraceTest foreignAwaitable, CausalityTrace[] traces)
        {
            await first;
            await second;
            traces[0] = CausalityTrace.Capture();
            if (foreignAwaitable != null)
            {
                await foreignAwaitable;
            }
            else
            {
                await third.Promise;
            }
            traces[1] = CausalityTrace.Capture();
        }

        // An awaitable that isn't ProtoPromise's (like a Task or a Unity Awaitable), which continues the await when it's continued.
        private sealed class ForeignAwaitableForTraceTest : System.Runtime.CompilerServices.INotifyCompletion
        {
            private System.Action _continuation;

            public ForeignAwaitableForTraceTest GetAwaiter() => this;

            public bool IsCompleted => false;

            public void GetResult() { }

            public void OnCompleted(System.Action continuation) => _continuation = continuation;

            internal void Continue() => _continuation.Invoke();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ContinueForeignAwaitForTraceTest(ForeignAwaitableForTraceTest foreignAwaitable)
        {
            foreignAwaitable.Continue();
            Volatile.Write(ref s_frameKeeper, 38);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ResolveFirstForTraceTest(Promise.Deferred deferred)
        {
            deferred.Resolve();
            Volatile.Write(ref s_frameKeeper, 39);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ResolveSecondForTraceTest(Promise.Deferred deferred)
        {
            deferred.Resolve();
            Volatile.Write(ref s_frameKeeper, 40);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ResolveThirdForTraceTest(Promise.Deferred deferred)
        {
            deferred.Resolve();
            Volatile.Write(ref s_frameKeeper, 41);
        }

        [Test]
        public void UnhandledRejection_ExceptionThrownInCatchContinueWithOrFinally_TraceIncludesWhatInvokedTheCallback(
            [Values("Catch", "ContinueWith", "Finally")] string callbackType)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException("Thrown in " + callbackType);
                    var deferred = Promise.NewDeferred();
                    CausalityTrace callbackScope = null;
                    System.Action throwInScope = () =>
                    {
                        callbackScope = GetCurrentTraceForTraceTest();
                        throw exception;
                    };
                    Promise promise = callbackType == "Catch" ? deferred.Promise.Then(() => throw new System.Exception("Caught")).Catch(throwInScope)
                        : callbackType == "ContinueWith" ? deferred.Promise.ContinueWith(_ => throwInScope())
                        : deferred.Promise.Finally(throwInScope);
                    var callbackTrace = promise._ref.Trace;
                    promise.Forget();
                    ResolveForTraceTest(deferred);

                    Assert.IsNotNull(callbackTrace);
                    Assert.AreSame(callbackTrace, callbackScope);
                    var trace = reports.GetTrace(exception);
                    Assert.IsTrue(trace.GetFrames().Any(frame => frame.GetMethod().Name == nameof(ResolveForTraceTest)), trace.ToString());
                    AssertPreviousTraceIsFrom(trace, nameof(UnhandledRejection_ExceptionThrownInCatchContinueWithOrFinally_TraceIncludesWhatInvokedTheCallback));
                    AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledRejection_ExceptionThrownInPromiseNewOnAContext_TraceIncludesWhatRanTheCallback()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var context = new QueuedContext();
                    var exception = new System.InvalidOperationException("Thrown in Promise.New");
                    CausalityTrace newScope = null;
                    var newPromise = Promise.New(_ =>
                    {
                        newScope = GetCurrentTraceForTraceTest();
                        throw exception;
                    }, context, forceAsync: true);
                    var newTrace = newPromise._ref.Trace;
                    newPromise.Forget();
                    ExecuteForTraceTest(context);

                    Assert.IsNotNull(newTrace);
                    Assert.AreSame(newTrace, newScope);
                    var trace = reports.GetTrace(exception);
                    Assert.IsTrue(trace.GetFrames().Any(frame => frame.GetMethod().Name == nameof(ExecuteForTraceTest)), trace.ToString());
                    AssertPreviousTraceIsFrom(trace, nameof(UnhandledRejection_ExceptionThrownInPromiseNewOnAContext_TraceIncludesWhatRanTheCallback));
                    AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledRejection_ExceptionThrownInPromiseNewImmediately_TraceIsThePromisesTrace()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException("Thrown in Promise.New");
                    var promise = Promise.New(_ => throw exception);
                    var promiseTrace = promise._ref.Trace;
                    promise.Forget();

                    // The callback ran inside Promise.New, so a trace captured where the exception was caught would print the same stack again.
                    var trace = reports.GetTrace(exception);
                    Assert.AreSame(promiseTrace, trace, trace.ToString());
                    Assert.IsNull(trace.Previous, trace.ToString());
                    AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledException_ThrownInProgressHandlerOnAContext_TraceIncludesWhatReportedTheProgress()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var context = new QueuedContext();
                    var exception = new System.InvalidOperationException("Thrown in progress handler");
                    var progress = Progress.New(_ => throw exception, context);
                    ReportProgressForTraceTest(progress.Token);
                    context.Execute();
                    progress.DisposeAsync().Forget();
                    context.Execute();

                    var trace = reports.GetTrace(exception);
                    Assert.IsTrue(HistoryContainsMethod(trace, nameof(ReportProgressForTraceTest)), trace.ToString());
                    AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledRejection_PromiseRejected_TraceIsWhereItWasCreated(
            [Values("Rejected", "ThenOnResolved")] string rejectionType)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException(rejectionType);
                    var promise = rejectionType == "Rejected"
                        ? Promise.Rejected(exception)
                        // The callback runs synchronously inside Then, which rejects the promise it returns with Promise.Rejected.
                        : Promise.Resolved().Then(() => throw exception);
                    var promiseTrace = promise._ref.Trace;
                    promise.Forget();

                    // The trace isn't captured again where the promise was rejected, which would print the same stack twice.
                    var trace = reports.GetTrace(exception);
                    Assert.AreSame(promiseTrace, trace, trace.ToString());
                    Assert.IsNull(trace.Previous, trace.ToString());
                    Assert.IsTrue(trace.GetFrames().Any(frame => frame.GetMethod().Name == nameof(UnhandledRejection_PromiseRejected_TraceIsWhereItWasCreated)), trace.ToString());
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledException_RethrownInProgressHandler_KeepsItsOriginalTrace()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException("Rethrown");
                    Promise.Rejected(exception).Forget();
                    var originalTrace = reports.GetTrace(exception);

                    var context = new QueuedContext();
                    var progress = Progress.New(_ => throw exception, context);
                    ReportProgressForTraceTest(progress.Token);
                    context.Execute();
                    progress.DisposeAsync().Forget();
                    context.Execute();

                    var traces = reports.GetTraces(exception);
                    Assert.AreEqual(2, traces.Length);
                    Assert.AreSame(originalTrace, traces[1]);
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void UnhandledException_RethrownByAwait_HasTheTraceOfWhereItWasRejected()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException("Rethrown");
                    var deferred = Promise.NewDeferred();
                    // The async function rethrows the exception when it awaits the rejected promise.
                    AwaitForTraceTest(deferred.Promise).Forget();
                    deferred.Reject(exception);
                    // Rejecting it again also uses the trace of where it was first rejected.
                    Promise.Rejected(exception).Forget();

                    var traces = reports.GetTraces(exception);
                    Assert.AreEqual(2, traces.Length);
                    Assert.IsNotNull(traces[0]);
                    Assert.AreSame(traces[1], traces[0]);
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static async Promise ThrowBeforeAwaitForTraceTest(System.Exception exception, bool doThrow)
        {
            if (doThrow)
            {
                throw exception;
            }
            await Promise.Resolved();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise CallThrowBeforeAwaitForTraceTest(System.Exception exception)
        {
            var promise = ThrowBeforeAwaitForTraceTest(exception, true);
            Volatile.Write(ref s_frameKeeper, 36);
            return promise;
        }

        [Test]
        public void UnhandledException_ThrownByAsyncFunctionBeforeItAwaits_HasTheTraceOfWhereTheFunctionWasCalled()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var exception = new System.InvalidOperationException("Thrown before await");
                    var promise = CallThrowBeforeAwaitForTraceTest(exception);
                    var asyncTrace = promise._ref.Trace;
                    promise.Forget();

                    var trace = reports.GetTrace(exception);
                    Assert.IsNotNull(trace);
                    StringAssert.Contains(nameof(CallThrowBeforeAwaitForTraceTest), trace.StackTrace.ToString(), trace.ToString());
                    // The function ran synchronously in its caller, so nothing else caused it.
                    Assert.IsNull(trace.Previous, trace.ToString());
                    // The library doesn't use the optimized async mode in DEBUG mode or for old IL2CPP, which includes its netstandard2.0 build that net48 uses.
#if !PROMISE_DEBUG && !(ENABLE_IL2CPP && !UNITY_2021_2_OR_NEWER) && !NETFRAMEWORK
                    // The optimized async mode captures the trace where the function threw, and the rejection uses it instead of capturing the same frames again.
                    Assert.AreSame(asyncTrace, trace, trace.ToString());
#endif
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_InCallbackThatAdoptedACompletedConfiguredPromise_IncludesWhatRanTheCallback()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var deferred = Promise.NewDeferred();
                CausalityTrace trace = null;
                deferred.Promise
                    // The callback returns a configured promise that is already complete, so the callback promise adopts it while the callback runs.
                    .Then(() => Promise.Resolved().ConfigureContinuation(new ContinuationOptions(context, CompletedContinuationBehavior.Asynchronous)))
                    .Then(() => { trace = CausalityTrace.Capture(); })
                    .Forget();
                ResolveForTraceTest(deferred);
                context.Execute();

                Assert.IsTrue(HistoryContainsMethod(trace, nameof(ResolveForTraceTest)), trace.ToString());
                AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

#if UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER
        [Test]
        public void CausalityTraceCapture_AfterAwaitingACompletedConfiguredPromiseAsValueTask_IncludesWhatRanTheAwait()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var deferred = Promise.NewDeferred();
                CausalityTrace trace = null;
                // The value task's source has no trace of its own, so the await relies on the trace captured where it was scheduled.
                deferred.Promise
                    .Then(() => { _ = CaptureAfterAwaitingValueTaskForTraceTest(Promise.Resolved().ConfigureContinuation(new ContinuationOptions(context, CompletedContinuationBehavior.Asynchronous)), captured => trace = captured); })
                    .Forget();
                ResolveForTraceTest(deferred);
                context.Execute();

                Assert.IsTrue(HistoryContainsMethod(trace, nameof(ResolveForTraceTest)), trace.ToString());
                // The trace of where it was scheduled is the previous trace, so it's not also the trace it was scheduled from.
                Assert.IsNull(OuterTrace(trace), trace.ToString());
                AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static async System.Threading.Tasks.Task CaptureAfterAwaitingValueTaskForTraceTest(Promise promise, System.Action<CausalityTrace> onCaptured)
        {
            // ConfigureAwait(false), so that the continuation runs where the value task is completed instead of on the test's context.
            await promise.AsValueTask().ConfigureAwait(false);
            onCaptured(CausalityTrace.Capture());
        }

#if !UNITY_WEBGL
        [Test]
        public void CausalityTraceCapture_AfterAwaitingACompletedPromiseConfiguredToTheThreadPoolAsValueTask_IncludesWhatRanTheAwait()
        {
            var backgroundContext = Promise.Config.BackgroundContext;
            // Without a background context, background continuations run on the thread pool.
            Promise.Config.BackgroundContext = null;
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = Promise.NewDeferred();
                CausalityTrace trace = null;
                System.Threading.Tasks.Task task = null;
                // The value task's source has no trace of its own, so the await relies on the trace captured where it was scheduled.
                deferred.Promise
                    .Then(() => { task = CaptureAfterAwaitingValueTaskForTraceTest(Promise.Resolved().ConfigureContinuation(new ContinuationOptions(SynchronizationOption.Background, CompletedContinuationBehavior.Asynchronous)), captured => trace = captured); })
                    .Forget();
                ResolveForTraceTest(deferred);
                Assert.IsTrue(task.Wait(System.TimeSpan.FromSeconds(10)));

                Assert.IsTrue(trace.IsThreadPoolThread, trace.ToString());
                Assert.IsTrue(HistoryContainsMethod(trace, nameof(ResolveForTraceTest)), trace.ToString());
                // The trace of where it was scheduled is the previous trace, so it's not also the trace it was scheduled from.
                Assert.IsNull(OuterTrace(trace), trace.ToString());
                AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.BackgroundContext = backgroundContext;
            }
        }
#endif

        // Awaits the promise as a value task, whose source has no trace of its own, so the trace captured after the await only has the traces of the scopes that the continuation ran in.
        private static async System.Threading.Tasks.Task CaptureAfterAwaitingAsValueTaskForTraceTest<T>(Promise<T> promise, System.Action<CausalityTrace> onCaptured)
        {
            try
            {
                // ConfigureAwait(false), so that the continuation runs synchronously where the promise is completed.
                await promise.AsValueTask().ConfigureAwait(false);
            }
            catch (System.Exception)
            {
                // The promise may be canceled or rejected.
            }
            onCaptured(CausalityTrace.Capture());
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Promise<int>.Deferred NewDeferredForTraceTest()
        {
            var deferred = Promise<int>.NewDeferred();
            Volatile.Write(ref s_frameKeeper, 23);
            return deferred;
        }

        [Test]
        public void DeferredCompletion_ValueTaskContinuationsInvokedSynchronously_DoNotRunInsideTheDeferredScope(
            [Values] bool cancel)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = NewDeferredForTraceTest();
                CausalityTrace trace = null;
                _ = CaptureAfterAwaitingAsValueTaskForTraceTest(deferred.Promise, captured => trace = captured);
                if (cancel)
                {
                    deferred.Cancel();
                }
                else
                {
                    deferred.Resolve(1);
                }

                // Completing a deferred doesn't enter its scope, and the ValueTask's continuation has no trace of its own,
                // so it only has what completed the deferred on its stack.
                Assert.IsNotNull(trace);
                Assert.IsFalse(HistoryContainsMethod(trace, nameof(NewDeferredForTraceTest)), trace.ToString());
                Assert.IsNull(trace.Previous, trace.ToString());
                CollectionAssert.IsEmpty(trace.OuterTraces, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        // NoInlining for the same reason as AwaitForTraceTest.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static async Promise<int> AwaitAndReturnForTraceTest(Promise<int> promise)
        {
            return await promise;
        }

        [Test]
        public void AsyncPromise_AwaitedAsValueTaskCompletesSynchronously_ContinuationRunsOutsideTheAsyncFunctionScope(
            [Values] bool flowExecutionContext)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            bool wasFlowExecutionContextEnabled = Promise.Config.AsyncFlowExecutionContextEnabled;
            Promise.Config.SetAsyncFlowExecutionContextEnabled(flowExecutionContext);
            CausalityTrace.IsEnabled = true;
            try
            {
                var deferred = NewDeferredForTraceTest();
                CausalityTrace trace = null;
                _ = CaptureAfterAwaitingAsValueTaskForTraceTest(AwaitAndReturnForTraceTest(deferred.Promise), captured => trace = captured);
                deferred.Resolve(1);

                // The value task's continuation has no scope of its own, so if it ran inside the scope of the async function, which completed inside its own scope,
                // the async function's trace would be its cause.
                Assert.IsNotNull(trace);
                Assert.IsNull(trace.Previous, trace.ToString());
                CollectionAssert.IsEmpty(trace.OuterTraces, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.SetAsyncFlowExecutionContextEnabled(wasFlowExecutionContextEnabled);
            }
        }

        public enum ChannelCompletionForTraceTest
        {
            Cancel,
            Close,
            Dispose
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static Proto.Promises.Channels.Channel<int> NewChannelForTraceTest()
        {
            var channel = Proto.Promises.Channels.Channel<int>.NewUnbounded();
            Volatile.Write(ref s_frameKeeper, 24);
            return channel;
        }

        [Test]
        public void ChannelCompletion_ValueTaskContinuationsInvokedSynchronously_DoNotRunInsideTheChannelScope(
            [Values] ChannelCompletionForTraceTest completion)
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var channel = NewChannelForTraceTest();
                try
                {
                    CausalityTrace trace = null;
                    _ = CaptureAfterAwaitingAsValueTaskForTraceTest(channel.Reader.ReadAsync(false), captured => trace = captured);
                    switch (completion)
                    {
                        case ChannelCompletionForTraceTest.Cancel:
                            channel.Writer.TryCancel();
                            break;
                        case ChannelCompletionForTraceTest.Close:
                            channel.Writer.TryClose();
                            break;
                        default:
                            channel.Dispose();
                            break;
                    }

                    // Completing a channel doesn't enter its scope, and the ValueTask's continuation has no trace of its own,
                    // so it only has what completed the channel on its stack.
                    Assert.IsNotNull(trace);
                    Assert.IsFalse(HistoryContainsMethod(trace, nameof(NewChannelForTraceTest)), trace.ToString());
                    Assert.IsNull(trace.Previous, trace.ToString());
                    CollectionAssert.IsEmpty(trace.OuterTraces, trace.ToString());
                }
                finally
                {
                    if (completion != ChannelCompletionForTraceTest.Dispose)
                    {
                        channel.Dispose();
                    }
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }
#if !(PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE) || PROTO_PROMISE_TEST_GC_ENABLED
        // In DEBUG mode, the scope that is not disposed is also reported when it's garbage collected, so this requires the GC.
        [Test]
        public void CausalityTraceScope_NotDisposedByAContinuationWhileTheAsyncFunctionScopeIsExited_IsReportedAndTheScopesAreRestored(
            [Values] bool flowExecutionContext)
        {
            // Normally AsyncFlowExecutionContextEnabled cannot be disabled after it is enabled, but we need to test both configurations.
            bool wasFlowExecutionContextEnabled = Promise.Config.AsyncFlowExecutionContextEnabled;
            Promise.Config.SetAsyncFlowExecutionContextEnabled(flowExecutionContext);
            CausalityTrace.IsEnabled = true;
            try
            {
                using (var reports = new ReportedExceptions())
                {
                    var deferred = Promise.NewDeferred();
                    // The value task's continuation has no scope of its own, and it runs after the callback's scope was disposed,
                    // while the async function's scope is still exited.
                    var awaiter = AwaitForTraceTest(deferred.Promise).Then(() => { }).AsValueTask().ConfigureAwait(false).GetAwaiter();
                    awaiter.UnsafeOnCompleted(() =>
                    {
                        awaiter.GetResult();
                        CausalityTrace.EnterScope(CausalityTrace.Capture());
                    });

                    Assert.DoesNotThrow(() => deferred.Resolve());

                    // The scope that was not disposed was exited when the async function's scope was restored.
                    Assert.AreEqual(0, CausalityTrace.ts_traces.Count);
#if PROMISE_DEBUG || PROTO_PROMISE_DEVELOPER_MODE
                    // The scope that was not disposed is also reported when it's collected, which may happen at any time.
                    TestHelper.GcCollectAndWaitForFinalizers();
                    var exceptions = reports.Exceptions;
                    Assert.AreEqual(2, exceptions.Length, string.Join("\n", exceptions.Select(e => e.ToString())));
                    Assert.AreEqual(1, exceptions.OfType<UnreleasedObjectException>().Count());
                    Assert.AreEqual(1, exceptions.OfType<System.InvalidOperationException>().Count(e => e.Message.Contains("before its nested")));
#else
                    reports.AssertScopeMisuses("before its nested");
#endif
                }
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
                Promise.Config.SetAsyncFlowExecutionContextEnabled(wasFlowExecutionContextEnabled);
            }
        }
#endif
#endif // UNITY_2021_2_OR_NEWER || !UNITY_2018_3_OR_NEWER

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ExecuteForTraceTest(QueuedContext context)
        {
            context.Execute();
            Volatile.Write(ref s_frameKeeper, 15);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ReportProgressForTraceTest(ProgressToken token)
        {
            token.Report(0.5);
            Volatile.Write(ref s_frameKeeper, 16);
        }

        private static async Promise ThrowAfterAwaitForTraceTest(Promise promise, System.Exception exception, CausalityTrace[] scope)
        {
            await promise;
            scope[0] = GetCurrentTraceForTraceTest();
            throw exception;
        }

        [Test]
        public void CausalityTraceCapture_InPromiseRun_DoesNotIncludeWhereItWasScheduledTwice()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                CausalityTrace trace = null;
                CausalityTrace scope = null;
                var runPromise = Promise.Run(() =>
                {
                    trace = CausalityTrace.Capture();
                    scope = GetCurrentTraceForTraceTest();
                }, context);
                var runTrace = runPromise._ref.Trace;
                runPromise.Forget();
                context.Execute();

                // The run promise's trace is the only cause; scheduling the callback when the promise was created doesn't add another.
                Assert.IsNull(runTrace.Previous, runTrace.ToString());
                Assert.AreSame(runTrace, scope);
                AssertPreviousTrace(runTrace, trace);
                Assert.IsNull(OuterTrace(trace), trace.ToString());
                AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_InCallbackOfCompletedConfiguredPromise_DoesNotIncludeWhereItWasScheduledTwice()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var deferred = Promise.NewDeferred();
                var configured = deferred.Promise.ConfigureContinuation(new ContinuationOptions(context, CompletedContinuationBehavior.Asynchronous));
                deferred.Resolve();
                CausalityTrace trace = null;
                CausalityTrace scope = null;
                // The configured promise is already complete, so the callback is scheduled on the context when Then is called.
                var thenPromise = configured.Then(() =>
                {
                    trace = CausalityTrace.Capture();
                    scope = GetCurrentTraceForTraceTest();
                });
                var thenTrace = thenPromise._ref.Trace;
                thenPromise.Forget();
                context.Execute();

                // The callback promise's trace is the only cause; scheduling the callback when it was hooked up doesn't add another.
                Assert.IsNull(thenTrace.Previous, thenTrace.ToString());
                Assert.AreSame(thenTrace, scope);
                AssertPreviousTrace(thenTrace, trace);
                Assert.IsNull(OuterTrace(trace), trace.ToString());
                AssertCausalityTraceIsPrintedCorrectly(trace, trace.ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static async Promise CaptureAfterSwitchToContextForTraceTest(Promise promise, System.Threading.SynchronizationContext context, System.Action<CausalityTrace> onCaptured)
        {
            await promise;
            await Promise.SwitchToContextAwait(context, forceAsync: true);
            onCaptured(CausalityTrace.Capture());
        }

        private static async Promise CaptureAfterAwaitOnContextForTraceTest(Promise promise, System.Threading.SynchronizationContext context, System.Action<CausalityTrace> onCaptured)
        {
            await promise.ConfigureAwait(new ContinuationOptions(context));
            onCaptured(CausalityTrace.Capture());
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ResolveForTraceTest(Promise.Deferred deferred)
        {
            deferred.Resolve();
            Volatile.Write(ref s_frameKeeper, 17);
        }

        // Whether the method is in the frames of the trace or its previous traces, or their outer traces.
        private static bool HistoryContainsMethod(CausalityTrace trace, string methodName)
        {
            for (var current = trace; current != null; current = current.Previous)
            {
                if (ContainsMethod(current, methodName) || current.OuterTraces.Any(outer => ContainsMethod(outer, methodName)))
                {
                    return true;
                }
            }
            return false;
        }

        // The trace's only outer trace (in these tests, where the callback that captured it was scheduled from), or null if it has none.
        private static CausalityTrace OuterTrace(CausalityTrace trace)
        {
            Assert.LessOrEqual(trace.OuterTraces.Count, 1, trace.ToString());
            return trace.OuterTraces.Count == 0 ? null : trace.OuterTraces[0];
        }

        // Whether the traces have the same visible frames, by their methods and lines.
        private static bool HasSameFrames(CausalityTrace trace, CausalityTrace other)
            => trace.GetFrames().Select(frame => (frame.GetMethod(), frame.GetFileLineNumber()))
                .SequenceEqual(other.GetFrames().Select(frame => (frame.GetMethod(), frame.GetFileLineNumber())));

        private static bool ContainsMethod(CausalityTrace trace, string methodName)
            => trace.GetFrames().Any(frame => frame.GetMethod().Name == methodName);

        // The NoInlining helpers whose frames the tests look for write a different value to this after their calls, so that IL2CPP's C++ compiler
        // doesn't turn the calls into tail calls or fold helpers with the same code together, which would remove their frames from the traces.
        // It's a volatile write, so that it can't be moved before the call.
        private static int s_frameKeeper;

        // Without line numbers (e.g. IL2CPP only prints method names by default), a trace that was captured earlier in a method that is still running
        // is printed like the outer frames of a trace captured in that method, so it's skipped (see CausalityTrace.Previous).
        private static readonly bool s_hasLineNumbers = new System.Diagnostics.StackFrame(0, true).GetFileLineNumber() != 0;

        // Whether an outer trace that was captured earlier in a method that is still running is kept (e.g. where a callback was scheduled from,
        // when a context runs the callback from that method).
        // Without line numbers and accurate IL offsets (IL2CPP's default), it's printed like the outer frames of the trace, so it's dropped (see CausalityTrace.OuterTraces).
        // Developer mode doesn't hide internal frames, so it's never printed like the outer frames of the trace.
#if ENABLE_IL2CPP && !PROTO_PROMISE_DEVELOPER_MODE
        private static bool KeepsOuterTracesFromTheSameMethod => s_hasLineNumbers;
#else
        private static bool KeepsOuterTracesFromTheSameMethod => true;
#endif

        // Whether a hidden frame may be printed with the name of another function, so that a trace that's printed like the outer frames of another trace is not skipped.
        // IL2CPP's C++ linker in Unity versions before 2021.2 folds some library functions with identical functions, whose names are printed in their place
        // (e.g. UnityEngine.ImageConversion.LoadImage in Unity 2019.4 on Windows).
#if ENABLE_IL2CPP && !UNITY_2021_2_OR_NEWER && !PROTO_PROMISE_DEVELOPER_MODE
        private static bool MayPrintHiddenFramesWithOtherNames => true;
#else
        private static bool MayPrintHiddenFramesWithOtherNames => false;
#endif

        // Asserts that the previous trace of the trace is the expected trace, or, without line numbers, that it was skipped.
        // Without line numbers, this can't tell a skipped trace from a scope that wasn't entered, so the callers also check the scope where the trace was captured (see GetCurrentTraceForTraceTest).
        private static void AssertPreviousTrace(CausalityTrace expected, CausalityTrace trace)
        {
            if (s_hasLineNumbers || trace.Previous == expected)
            {
                Assert.AreSame(expected, trace.Previous, trace.ToString());
            }
            else
            {
                Assert.AreSame(expected.Previous, trace.Previous, trace.ToString());
            }
        }

        // Asserts that the previous trace of the trace was captured in the method, or, without line numbers, that it was skipped.
        // The callers capture the previous trace without an outer scope, so the trace has no previous trace if it was skipped.
        // Without line numbers, this can't tell a skipped trace from a scope that wasn't entered, so the callers also check the scope where the trace was captured (see GetCurrentTraceForTraceTest).
        private static void AssertPreviousTraceIsFrom(CausalityTrace trace, string methodName)
        {
            if (s_hasLineNumbers || (trace.Previous != null && ContainsMethod(trace.Previous, methodName)))
            {
                Assert.IsTrue(ContainsMethod(trace.Previous, methodName), trace.ToString());
            }
            else
            {
                Assert.IsTrue(ContainsMethod(trace, methodName), trace.ToString());
                Assert.IsNull(trace.Previous, trace.ToString());
            }
        }

        [Test]
        public void CausalityTraceCapture_InACallbackOfAPromiseCreatedInTheSameScheduledCallback_DoesNotIncludeWhereItWasScheduledTwice()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var deferred = Promise.NewDeferred();
                var trace = new CausalityTrace[1];
                CaptureInCallbackAfterAwaitOnContextForTraceTest(deferred.Promise, context, trace).Forget();
                ResolveForTraceTest(deferred);
                context.Execute();

                // The callback's trace and its promise's trace were both captured after the async function continued on the context,
                // so they were scheduled from the same trace, which is printed once.
                var text = trace[0].ToString();
                Assert.AreEqual(1, CountOccurrences(text, nameof(ResolveForTraceTest)), text);
                AssertCausalityTraceIsPrintedCorrectly(trace[0], text);
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static async Promise CaptureInCallbackAfterAwaitOnContextForTraceTest(Promise promise, System.Threading.SynchronizationContext context, CausalityTrace[] trace)
        {
            await promise.ConfigureAwait(new ContinuationOptions(context));
            var deferred = Promise.NewDeferred();
            var thenPromise = deferred.Promise.Then(() => { trace[0] = CausalityTrace.Capture(); });
            deferred.Resolve();
            await thenPromise;
        }

        [Test]
        public void CausalityTraceCapture_PreviousTraceScheduledFromTheSameTrace_OuterTraceIsOnlyKeptIfThePreviousTraceIsTruncated(
            [Values] bool isTruncated)
        {
            int originalMaxDepth = CausalityTrace.MaxDepth;
            // A max depth of 2 keeps only the trace when the chain is truncated.
            CausalityTrace.MaxDepth = isTruncated ? 2 : originalMaxDepth;
            CausalityTrace.IsEnabled = true;
            try
            {
                var other = CaptureAtOther();
                var scheduledFrom = CaptureAtStep();
                CausalityTrace previous, trace;
                // Both traces are captured in the same callback.
                var outerScope = CausalityTrace.EnterScope(scheduledFrom);
                try
                {
                    previous = CaptureInScopeForTraceTest(other);
                    var scope = CausalityTrace.EnterScope(previous);
                    try
                    {
                        trace = CaptureAtLeaf();
                    }
                    finally
                    {
                        scope.Dispose();
                    }
                }
                finally
                {
                    outerScope.Dispose();
                }

                var text = trace.ToString();
                Assert.AreSame(scheduledFrom, OuterTrace(previous), text);
                if (isTruncated)
                {
                    Assert.IsNull(trace.Previous, text);
                    Assert.AreSame(scheduledFrom, OuterTrace(trace), text);
                }
                else
                {
                    Assert.AreSame(previous, trace.Previous, text);
                    Assert.IsNull(OuterTrace(trace), text);
                }
                Assert.AreEqual(1, CountOccurrences(text, nameof(CaptureAtStep)), text);
                AssertCausalityTraceIsPrintedCorrectly(trace, text);
            }
            finally
            {
                CausalityTrace.MaxDepth = originalMaxDepth;
                CausalityTrace.IsEnabled = false;
            }
        }

        [Test]
        public void CausalityTraceCapture_AfterAContextRanACallbackSynchronously_IncludesWhereTheOuterCallbackWasScheduledFrom()
        {
            CausalityTrace.IsEnabled = true;
            try
            {
                var context = new QueuedContext();
                var deferred = Promise.NewDeferred();
                var traces = new CausalityTrace[2];
                CaptureAroundAContinuationOnAnInlineContextForTraceTest(deferred.Promise, context, traces).Forget();
                ResolveForTraceTest(deferred);
                context.Execute();

                // Where the outer callback was scheduled from is restored after the callback that the other context ran synchronously.
                Assert.IsNotNull(OuterTrace(traces[0]), traces[0].ToString());
                Assert.AreSame(OuterTrace(traces[0]), OuterTrace(traces[1]), traces[1].ToString());
            }
            finally
            {
                CausalityTrace.IsEnabled = false;
            }
        }

        private static async Promise CaptureAroundAContinuationOnAnInlineContextForTraceTest(Promise promise, QueuedContext context, CausalityTrace[] traces)
        {
            await promise.ConfigureAwait(new ContinuationOptions(context));
            traces[0] = CaptureAtLeaf();
            var deferred = Promise.NewDeferred();
            AwaitOnAnInlineContextForTraceTest(deferred.Promise).Forget();
            deferred.Resolve();
            traces[1] = CaptureAtStep();
        }

        private static async Promise AwaitOnAnInlineContextForTraceTest(Promise promise)
        {
            await promise.ConfigureAwait(new ContinuationOptions(new InlineContext(), forceAsync: true));
        }

        // Runs the posted callbacks synchronously.
        private sealed class InlineContext : System.Threading.SynchronizationContext
        {
            public override void Post(System.Threading.SendOrPostCallback d, object state)
                => d.Invoke(state);
        }

        // Runs the posted callbacks when Execute is called, like a UI thread's message loop.
        private sealed class QueuedContext : System.Threading.SynchronizationContext
        {
            private readonly Queue<(System.Threading.SendOrPostCallback callback, object state)> _queue = new Queue<(System.Threading.SendOrPostCallback, object)>();

            public override void Post(System.Threading.SendOrPostCallback d, object state)
            {
                lock (_queue)
                {
                    _queue.Enqueue((d, state));
                }
            }

            internal void Execute()
            {
                while (true)
                {
                    (System.Threading.SendOrPostCallback callback, object state) item;
                    lock (_queue)
                    {
                        if (_queue.Count == 0)
                        {
                            return;
                        }
                        item = _queue.Dequeue();
                    }
                    item.callback.Invoke(item.state);
                }
            }
        }

        // Copies the trace with another previous trace, so that the copy has the same stack trace.
        private static CausalityTrace CopyTrace(CausalityTrace trace, bool isTruncated, CausalityTrace previous = null)
            => new CausalityTrace(trace, previous, isTruncated);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureAtLeaf()
        {
            var trace = CausalityTrace.Capture();
            Volatile.Write(ref s_frameKeeper, 18);
            return trace;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureAtLoop()
        {
            var trace = CausalityTrace.Capture();
            Volatile.Write(ref s_frameKeeper, 19);
            return trace;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureAtStep()
        {
            var trace = CausalityTrace.Capture();
            Volatile.Write(ref s_frameKeeper, 20);
            return trace;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureAtRoot()
        {
            var trace = CausalityTrace.Capture();
            Volatile.Write(ref s_frameKeeper, 21);
            return trace;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static CausalityTrace CaptureAtOther()
        {
            var trace = CausalityTrace.Capture();
            Volatile.Write(ref s_frameKeeper, 22);
            return trace;
        }

        private static Promise GetPendingPromise(Queue<Promise.Deferred> pending)
        {
            var deferred = Promise.NewDeferred();
            pending.Enqueue(deferred);
            return deferred.Promise;
        }

        private static async Promise CaptureAfterPending(Queue<Promise.Deferred> pending, CausalityTrace[] trace)
        {
            await GetPendingPromise(pending);
            trace[0] = CausalityTrace.Capture();
        }

        private static async Promise LoopCallingAsyncMethod(Queue<Promise.Deferred> pending, CausalityTrace[] trace, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                await CaptureAfterPending(pending, trace);
            }
        }

        private static CausalityTrace GetTraceFromAsyncLoop(int iterations)
        {
            var pending = new Queue<Promise.Deferred>();
            var trace = new CausalityTrace[1];
            LoopCallingAsyncMethod(pending, trace, iterations).Forget();
            while (pending.Count > 0)
            {
                pending.Dequeue().Resolve();
            }
            return trace[0];
        }

        // Repeated blocks must be labeled once, and printed before they are repeated.
        // Every stack trace in the chain, and every one of their outer traces (indented one more level than the one before it), must be printed (as a whole),
        // and the truncation must be printed if the chain is truncated.
        private static void AssertCausalityTraceIsPrintedCorrectly(CausalityTrace trace, string text)
        {
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(text, @"--- #(\d+) repeated (\d+) times?(?:: .*?)? ---"))
            {
                var number = match.Groups[1].Value;
                int beginIndex = text.IndexOf("--- #" + number + " Begin ---");
                int endIndex = text.IndexOf("--- #" + number + " End ---");
                Assert.GreaterOrEqual(beginIndex, 0, "Missing begin label for " + match.Value + ": " + text);
                Assert.Greater(endIndex, beginIndex, "Missing end label for " + match.Value + ": " + text);
                Assert.Less(endIndex, match.Index, match.Value + " must be after its block: " + text);
                Assert.AreEqual(1, CountOccurrences(text, "--- #" + number + " Begin ---"), "Duplicate begin label for " + match.Value + ": " + text);
                Assert.AreNotEqual("0", match.Groups[2].Value, text);
            }
            var openLabels = new Stack<string>();
            foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(text, @"--- #(\d+) (Begin|End) ---"))
            {
                if (match.Groups[2].Value == "Begin")
                {
                    openLabels.Push(match.Groups[1].Value);
                }
                else
                {
                    Assert.AreEqual(openLabels.Count > 0 ? openLabels.Pop() : null, match.Groups[1].Value, "Misnested " + match.Value + ": " + text);
                }
            }
            Assert.AreEqual(0, openLabels.Count, "Unclosed labels: " + text);

            // The stack traces are printed in the format of the visible frames (see CausalityTrace.FormatStackTrace).
            var textLines = text.Split(new[] { System.Environment.NewLine }, System.StringSplitOptions.None);
            var chain = new List<CausalityTrace>();
            for (var current = trace; current != null; current = current.Previous)
            {
                chain.Add(current);
                AssertStackTraceIsPrinted(current, textLines, text, "");
                string indent = "";
                foreach (var outer in current.OuterTraces)
                {
                    indent += "   ";
                    AssertStackTraceIsPrinted(outer, textLines, text, indent);
                    // Outer traces don't have their own histories, so that a chain of causes is only printed once.
                    Assert.IsNull(outer.Previous, text);
                    CollectionAssert.IsEmpty(outer.OuterTraces, text);
                    CollectionAssert.IsEmpty(outer.Repeats, text);
                }
            }
            Assert.AreEqual(chain[chain.Count - 1].IsTruncated, text.Contains("Older traces truncated"), text);
            Assert.AreEqual(chain.Any(current => current.AreOuterTracesTruncated), text.Contains("Outer traces truncated"), text);

            // Each trace's thread and time is printed at most once with its stack trace and once with each of its outer traces,
            // and each group of repeats has at most two times (including the traces that are folded as repeats),
            // so the output can't grow faster than the number of traces, their outer traces and their repeats.
            int timeCount = System.Text.RegularExpressions.Regex.Matches(text, @"\d+\.\d{3}(?= ms earlier| to )").Count;
            Assert.LessOrEqual(timeCount, chain.Sum(current => 4 + current.OuterTraces.Count + current.Repeats.Count * 2), text);
        }

        private static void AssertStackTraceIsPrinted(CausalityTrace trace, string[] textLines, string text, string indent)
        {
            var stackTraceText = string.Join(System.Environment.NewLine,
                trace.GetFrames().Select(frame => indent + new System.Diagnostics.StackTrace(frame).ToString().TrimEnd('\r', '\n')));
            var stackTraceLines = stackTraceText.Length == 0
                ? new[] { indent + "   --- All frames are hidden ---" }
                : stackTraceText.Split(new[] { System.Environment.NewLine }, System.StringSplitOptions.None);
            Assert.IsTrue(ContainsBlock(textLines, stackTraceLines), "Missing stack trace:" + System.Environment.NewLine + stackTraceText + System.Environment.NewLine + "in:" + System.Environment.NewLine + text);
        }

        // Whether the lines are printed as a whole block, between markers (or blank lines, or the start or end of the text),
        // so that a stack trace that is only printed as part of a longer stack trace is not found.
        private static bool ContainsBlock(string[] textLines, string[] blockLines)
        {
            for (int start = 0; start + blockLines.Length <= textLines.Length; ++start)
            {
                int end = start + blockLines.Length;
                if ((start == 0 || IsMarkerLine(textLines[start - 1]))
                    && (end == textLines.Length || IsMarkerLine(textLines[end]))
                    && Enumerable.Range(0, blockLines.Length).All(i => textLines[start + i] == blockLines[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsMarkerLine(string line)
            => line.Length == 0 || line.TrimStart().StartsWith("---");

        private static int CountOccurrences(string text, string value)
            => System.Text.RegularExpressions.Regex.Matches(text, System.Text.RegularExpressions.Regex.Escape(value)).Count;

        // Asserts that the traces are printed the same, other than the threads and times.
        private static void AssertPrintedTheSame(string expected, string actual)
            => Assert.AreEqual(WithoutThreadsAndTimes(expected), WithoutThreadsAndTimes(actual), expected + System.Environment.NewLine + "----" + System.Environment.NewLine + actual);

        // Removes the threads and times of repeats, and replaces the times of the stack traces.
        private static string WithoutThreadsAndTimes(string text)
        {
            text = System.Text.RegularExpressions.Regex.Replace(text, @"[:;] (?:Thread|thread pool) .*?(?= ---\r?$)", "", System.Text.RegularExpressions.RegexOptions.Multiline);
            return System.Text.RegularExpressions.Regex.Replace(text, @"\d+\.\d{3} ms earlier", "0.000 ms earlier");
        }
    }
}