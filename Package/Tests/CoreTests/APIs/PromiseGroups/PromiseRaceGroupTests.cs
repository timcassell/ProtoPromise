#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Linq;

namespace ProtoPromiseTests.APIs.PromiseGroups
{
    public class PromiseRaceGroupTests
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

        [Test]
        public void PromiseRaceGroup_UsingInvalidatedGroupThrows_void(
            [Values] CancelationType cancelationType,
            [Values] bool cancelOnNonResolved)
        {
            var voidPromise = Promise.Resolved();
            Assert.Catch<System.InvalidOperationException>(() => default(PromiseRaceGroup).Add(voidPromise));
            Assert.Catch<System.InvalidOperationException>(() => default(PromiseRaceGroup).WaitAsync());

            using (var cancelationSource = CancelationSource.New())
            {
                var raceGroup1 = cancelationType == CancelationType.None ? PromiseRaceGroup.New(out _, cancelOnNonResolved)
                    : cancelationType == CancelationType.Deferred ? PromiseRaceGroup.New(cancelationSource.Token, out _, cancelOnNonResolved)
                    : PromiseRaceGroup.New(CancelationToken.Canceled(), out _, cancelOnNonResolved);

                var raceGroup2 = raceGroup1.Add(Promise.Resolved());
                Assert.Catch<System.InvalidOperationException>(() => raceGroup1.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => raceGroup1.WaitAsync());

                var raceGroup3 = raceGroup2.Add(Promise.Resolved());
                Assert.Catch<System.InvalidOperationException>(() => raceGroup2.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => raceGroup2.WaitAsync());

                raceGroup3.WaitAsync().Forget();
                Assert.Catch<System.InvalidOperationException>(() => raceGroup3.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => raceGroup3.WaitAsync());

                voidPromise.Forget();
            }
        }

        [Test]
        public void PromiseRaceGroupThrowsWhenNoPromisesAreAdded_void(
            [Values] CancelationType cancelationType,
            [Values] bool cancelOnNonResolved)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var raceGroup = cancelationType == CancelationType.None ? PromiseRaceGroup.New(out _, cancelOnNonResolved)
                    : cancelationType == CancelationType.Deferred ? PromiseRaceGroup.New(cancelationSource.Token, out _, cancelOnNonResolved)
                    : PromiseRaceGroup.New(CancelationToken.Canceled(), out _, cancelOnNonResolved);

                Assert.Catch<System.InvalidOperationException>(() => raceGroup.WaitAsync());

                raceGroup.Add(Promise.Resolved()).WaitAsync().Forget();
            }
        }

        private static Promise.State GetExpectedState(CompleteType completeType1, CompleteType completeType2)
        {
            if (completeType1 == CompleteType.Resolve || completeType2 == CompleteType.Resolve)
            {
                return Promise.State.Resolved;
            }
            if (completeType1 == CompleteType.Reject || completeType2 == CompleteType.Reject)
            {
                return Promise.State.Rejected;
            }
            return Promise.State.Canceled;
        }

        [Test]
        public void PromiseRaceGroupAdoptsTheStateOfTheFirstCompletedPromise_1_void(
            [Values] CancelationType cancelationType,
            [Values] bool cancelOnNonResolved,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType,
            [Values] bool alreadyComplete)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var raceGroup = cancelationType == CancelationType.None ? PromiseRaceGroup.New(out _, cancelOnNonResolved)
                    : cancelationType == CancelationType.Deferred ? PromiseRaceGroup.New(cancelationSource.Token, out _, cancelOnNonResolved)
                    : PromiseRaceGroup.New(CancelationToken.Canceled(), out _, cancelOnNonResolved);

                Exception expectedException = new Exception("Bang!");

                bool completed = false;

                raceGroup
                    .Add(TestHelper.BuildPromise(completeType, alreadyComplete, expectedException, out var tryCompleter))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;

                        Assert.AreEqual((Promise.State) completeType, result.State);
                        if (completeType == CompleteType.Reject)
                        {
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
                    })
                    .Forget();

                Assert.AreEqual(alreadyComplete, completed);

                tryCompleter();
                Assert.IsTrue(completed);
            }
        }

        [Test]
        public void PromiseRaceGroupAdoptsTheStateOfTheFirstCompletedPromise_2_void(
            [Values] CancelationType cancelationType,
            [Values] bool cancelOnNonResolved,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType2,
            [Values] bool alreadyComplete2,
            [Values] bool completeFirstPromiseFirst)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var raceGroup = cancelationType == CancelationType.None ? PromiseRaceGroup.New(out _, cancelOnNonResolved)
                    : cancelationType == CancelationType.Deferred ? PromiseRaceGroup.New(cancelationSource.Token, out _, cancelOnNonResolved)
                    : PromiseRaceGroup.New(CancelationToken.Canceled(), out _, cancelOnNonResolved);

                Exception expectedException = new Exception("Bang!");

                bool completed = false;

                raceGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, expectedException, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, expectedException, out var tryCompleter2))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;

                        var expectedState = GetExpectedState(completeType1, completeType2);
                        Assert.AreEqual(expectedState, result.State);
                        if (expectedState == Promise.State.Rejected)
                        {
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
                    })
                    .Forget();

                if (!completeFirstPromiseFirst)
                {
                    (tryCompleter1, tryCompleter2) = (tryCompleter2, tryCompleter1);
                    (alreadyComplete1, alreadyComplete2) = (alreadyComplete2, alreadyComplete1);
                }

                Assert.AreEqual(alreadyComplete1 && alreadyComplete2, completed);

                tryCompleter1();
                Assert.AreEqual(alreadyComplete2, completed);

                tryCompleter2();
                Assert.IsTrue(completed);
            }
        }

        private static Promise.State GetExpectedState(CancelationType cancelationType, bool cancelOnNonResolved,
            CompleteType completeType1, bool alreadyComplete1, CompleteType completeType2, bool alreadyComplete2,
            bool completeFirstPromiseFirst)
        {
            if (cancelationType == CancelationType.Immediate)
            {
                return Promise.State.Canceled;
            }

            if (!alreadyComplete1 && (alreadyComplete2 || !completeFirstPromiseFirst))
            {
                // If the second promise should complete first, swap them.
                (completeType1, completeType2) = (completeType2, completeType1);
                (alreadyComplete1, alreadyComplete2) = (alreadyComplete2, alreadyComplete1);
            }

            if (alreadyComplete1)
            {
                if (completeType1 == CompleteType.Resolve)
                {
                    return Promise.State.Resolved;
                }

                if (cancelOnNonResolved)
                {
                    return (Promise.State) completeType1;
                }

                if (alreadyComplete2)
                {
                    if (completeType2 == CompleteType.Resolve)
                    {
                        return Promise.State.Resolved;
                    }
                    if (completeType2 == CompleteType.Cancel)
                    {
                        return (Promise.State) completeType1;
                    }
                    return Promise.State.Rejected;
                }

                if (cancelationType == CancelationType.Deferred)
                {
                    return (Promise.State) completeType1;
                }
                if (completeType2 == CompleteType.Cancel)
                {
                    return (Promise.State) completeType1;
                }
                return (Promise.State) completeType2;
            }

            if (alreadyComplete2)
            {
                if (completeType2 == CompleteType.Resolve)
                {
                    return Promise.State.Resolved;
                }

                if (cancelOnNonResolved)
                {
                    return (Promise.State) completeType2;
                }

                if (completeType1 == CompleteType.Cancel)
                {
                    return (Promise.State) completeType2;
                }
                return (Promise.State) completeType1;
            }

            if (cancelationType == CancelationType.Deferred)
            {
                return (Promise.State) completeType1;
            }

            // CancelationType.None
            if (cancelOnNonResolved)
            {
                return (Promise.State) completeType1;
            }

            return GetExpectedState(completeType1, completeType2);
        }

        [Test]
        public void PromiseRaceGroupAdoptsTheStateOfTheFirstCompletedPromise_WithCancelation_2_void(
            [Values] CancelationType cancelationType,
            [Values] bool cancelOnNonResolved,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType2,
            [Values] bool alreadyComplete2,
            [Values] bool completeFirstPromiseFirst)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var raceGroup = cancelationType == CancelationType.None ? PromiseRaceGroup.New(out var groupCancelationToken, cancelOnNonResolved)
                    : cancelationType == CancelationType.Deferred ? PromiseRaceGroup.New(cancelationSource.Token, out groupCancelationToken, cancelOnNonResolved)
                    : PromiseRaceGroup.New(CancelationToken.Canceled(), out groupCancelationToken, cancelOnNonResolved);

                Exception expectedException = new Exception("Bang!");
                var expectedState = GetExpectedState(cancelationType, cancelOnNonResolved, completeType1, alreadyComplete1, completeType2, alreadyComplete2, completeFirstPromiseFirst);

                bool completed = false;

                raceGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, expectedException, groupCancelationToken, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, expectedException, groupCancelationToken, out var tryCompleter2))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;

                        Assert.AreEqual(expectedState, result.State);
                        if (expectedState == Promise.State.Rejected)
                        {
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
                    })
                    .Forget();

                if (!alreadyComplete1 && (alreadyComplete2 || !completeFirstPromiseFirst))
                {
                    // If the second promise should complete first, swap them.
                    (tryCompleter1, tryCompleter2) = (tryCompleter2, tryCompleter1);
                    (completeType1, completeType2) = (completeType2, completeType1);
                    (alreadyComplete1, alreadyComplete2) = (alreadyComplete2, alreadyComplete1);
                }

                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || (alreadyComplete1 && alreadyComplete2)
                    || (cancelOnNonResolved && (alreadyComplete1 || alreadyComplete2))
                    || (alreadyComplete1 && completeType1 == CompleteType.Resolve),
                    completed);

                tryCompleter1();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || alreadyComplete2
                    || cancelOnNonResolved
                    || completeType1 == CompleteType.Resolve,
                    completed);

                cancelationSource.Cancel();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || alreadyComplete2
                    || cancelOnNonResolved
                    || completeType1 == CompleteType.Resolve,
                    completed);

                tryCompleter2();
                Assert.IsTrue(completed);
            }
        }

        [Test]
        public void PromiseRaceGroup_NonResolved_CancelationCallbackExceptionsArePropagated_2_void(
            [Values(CancelationType.None, CancelationType.Deferred)] CancelationType cancelationType,
            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            Test_PromiseRaceGroup_CancelationCallbackExceptionsArePropagated_2_void(cancelationType, true, completeType1, alreadyComplete1, completeType2, alreadyComplete2);
        }

        [Test]
        public void PromiseRaceGroup_CancelationCallbackExceptionsArePropagated_2_void(
            [Values(CancelationType.None, CancelationType.Deferred)] CancelationType cancelationType,
            [Values] bool cancelOnNonResolved,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            Test_PromiseRaceGroup_CancelationCallbackExceptionsArePropagated_2_void(cancelationType, cancelOnNonResolved, completeType1, alreadyComplete1, completeType2, alreadyComplete2);
        }

        private static void Test_PromiseRaceGroup_CancelationCallbackExceptionsArePropagated_2_void(
            CancelationType cancelationType,
            bool cancelOnNonResolved,
            CompleteType completeType1,
            bool alreadyComplete1,
            CompleteType completeType2,
            bool alreadyComplete2)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var raceGroup = cancelationType == CancelationType.None ? PromiseRaceGroup.New(out var groupCancelationToken, cancelOnNonResolved)
                    : cancelationType == CancelationType.Deferred ? PromiseRaceGroup.New(cancelationSource.Token, out groupCancelationToken, cancelOnNonResolved)
                    : PromiseRaceGroup.New(CancelationToken.Canceled(), out groupCancelationToken, cancelOnNonResolved);

                Exception expectedException = new Exception("Error in cancelation!");
                groupCancelationToken.Register(() => { throw expectedException; });

                bool completed = false;

                raceGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, new System.InvalidOperationException("Bang!"), out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, new System.InvalidOperationException("Bang!"), out var tryCompleter2))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;
                        Assert.AreEqual(Promise.State.Rejected, result.State);
                        Assert.IsAssignableFrom<AggregateException>(result.Reason);

                        int expectedExceptionCount = 1;
                        if (completeType1 == CompleteType.Reject)
                        {
                            ++expectedExceptionCount;
                        }
                        if (completeType2 == CompleteType.Reject)
                        {
                            ++expectedExceptionCount;
                        }

                        var e = result.Reason.UnsafeAs<AggregateException>();
                        Assert.AreEqual(expectedExceptionCount, e.InnerExceptions.Count);
                        var cancelationAggregateException = e.InnerExceptions.OfType<AggregateException>().Single();
                        Assert.AreEqual(expectedExceptionCount - 1, e.InnerExceptions.OfType<System.InvalidOperationException>().Count());
                        Assert.AreEqual(1, cancelationAggregateException.InnerExceptions.Count);
                        Assert.AreEqual(expectedException, cancelationAggregateException.InnerExceptions[0]);
                    })
                    .Forget();

                Assert.AreEqual(alreadyComplete1 && alreadyComplete2, completed);

                tryCompleter1();
                Assert.AreEqual(alreadyComplete2, completed);

                tryCompleter2();
                Assert.IsTrue(completed);
            }
        }

        [Test]
        public void PromiseRaceGroup_UsingInvalidatedGroupThrows_T(
            [Values] CancelationType cancelationType,
            [Values] bool cancelOnNonResolved)
        {
            var intPromise = Promise.Resolved(42);
            Assert.Catch<System.InvalidOperationException>(() => default(PromiseRaceGroup<int>).Add(intPromise));
            Assert.Catch<System.InvalidOperationException>(() => default(PromiseRaceGroup<int>).WaitAsync());

            using (var cancelationSource = CancelationSource.New())
            {
                var raceGroup1 = cancelationType == CancelationType.None ? PromiseRaceGroup<int>.New(out _, cancelOnNonResolved)
                    : cancelationType == CancelationType.Deferred ? PromiseRaceGroup<int>.New(cancelationSource.Token, out _, cancelOnNonResolved)
                    : PromiseRaceGroup<int>.New(CancelationToken.Canceled(), out _, cancelOnNonResolved);

                var raceGroup2 = raceGroup1.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => raceGroup1.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => raceGroup1.WaitAsync());

                var raceGroup3 = raceGroup2.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => raceGroup2.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => raceGroup2.WaitAsync());

                raceGroup3.WaitAsync().Forget();
                Assert.Catch<System.InvalidOperationException>(() => raceGroup3.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => raceGroup3.WaitAsync());

                intPromise.Forget();
            }
        }

        [Test]
        public void PromiseRaceGroupThrowsWhenNoPromisesAreAdded_T(
            [Values] CancelationType cancelationType,
            [Values] bool cancelOnNonResolved)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var raceGroup = cancelationType == CancelationType.None ? PromiseRaceGroup<int>.New(out _, cancelOnNonResolved)
                    : cancelationType == CancelationType.Deferred ? PromiseRaceGroup<int>.New(cancelationSource.Token, out _, cancelOnNonResolved)
                    : PromiseRaceGroup<int>.New(CancelationToken.Canceled(), out _, cancelOnNonResolved);

                Assert.Catch<System.InvalidOperationException>(() => raceGroup.WaitAsync());

                raceGroup.Add(Promise.Resolved(2)).WaitAsync().Forget();
            }
        }

        [Test]
        public void PromiseRaceGroupAdoptsTheStateOfTheFirstCompletedPromise_1_T(
            [Values] CancelationType cancelationType,
            [Values] bool cancelOnNonResolved,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType,
            [Values] bool alreadyComplete)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var raceGroup = cancelationType == CancelationType.None ? PromiseRaceGroup<int>.New(out _, cancelOnNonResolved)
                    : cancelationType == CancelationType.Deferred ? PromiseRaceGroup<int>.New(cancelationSource.Token, out _, cancelOnNonResolved)
                    : PromiseRaceGroup<int>.New(CancelationToken.Canceled(), out _, cancelOnNonResolved);

                Exception expectedException = new Exception("Bang!");

                int value1 = 1;
                bool completed = false;

                raceGroup
                    .Add(TestHelper.BuildPromise(completeType, alreadyComplete, value1, expectedException, out var tryCompleter))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;

                        Assert.AreEqual((Promise.State) completeType, result.State);
                        if (completeType == CompleteType.Reject)
                        {
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
                        else if (completeType == CompleteType.Resolve)
                        {
                            Assert.AreEqual(value1, result.Value);
                        }
                    })
                    .Forget();

                Assert.AreEqual(alreadyComplete, completed);

                tryCompleter();
                Assert.IsTrue(completed);
            }
        }

        private static T GetExpectedValue<T>(T value1, T value2,
            CompleteType completeType1, bool alreadyComplete1, CompleteType completeType2, bool alreadyComplete2,
            bool completeFirstPromiseFirst)
        {
            if (!alreadyComplete1 && (alreadyComplete2 || !completeFirstPromiseFirst))
            {
                // If the second promise should complete first, swap them.
                (completeType1, completeType2) = (completeType2, completeType1);
                (alreadyComplete1, alreadyComplete2) = (alreadyComplete2, alreadyComplete1);
                (value1, value2) = (value2, value1);
            }

            if (alreadyComplete1)
            {
                return completeType1 == CompleteType.Resolve
                    ? value1
                    : value2;
            }

            if (alreadyComplete2)
            {
                return completeType2 == CompleteType.Resolve
                    ? value2
                    : value1;
            }

            return completeType1 == CompleteType.Resolve
                ? value1
                : value2;
        }

        [Test]
        public void PromiseRaceGroupAdoptsTheStateOfTheFirstCompletedPromise_2_T(
            [Values] CancelationType cancelationType,
            [Values] bool cancelOnNonResolved,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType2,
            [Values] bool alreadyComplete2,
            [Values] bool completeFirstPromiseFirst)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var raceGroup = cancelationType == CancelationType.None ? PromiseRaceGroup<int>.New(out _, cancelOnNonResolved)
                    : cancelationType == CancelationType.Deferred ? PromiseRaceGroup<int>.New(cancelationSource.Token, out _, cancelOnNonResolved)
                    : PromiseRaceGroup<int>.New(CancelationToken.Canceled(), out _, cancelOnNonResolved);

                int value1 = 1;
                int value2 = 2;
                Exception expectedException = new Exception("Bang!");
                int expectedValue = GetExpectedValue(value1, value2, completeType1, alreadyComplete1, completeType2, alreadyComplete2, completeFirstPromiseFirst);

                bool completed = false;

                raceGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, value2, expectedException, out var tryCompleter2))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;

                        var expectedState = GetExpectedState(completeType1, completeType2);
                        Assert.AreEqual(expectedState, result.State);
                        if (expectedState == Promise.State.Rejected)
                        {
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
                        else if (expectedState == Promise.State.Resolved)
                        {
                            Assert.AreEqual(expectedValue, result.Value);
                        }
                    })
                    .Forget();

                if (!completeFirstPromiseFirst)
                {
                    (tryCompleter1, tryCompleter2) = (tryCompleter2, tryCompleter1);
                    (alreadyComplete1, alreadyComplete2) = (alreadyComplete2, alreadyComplete1);
                }

                Assert.AreEqual(alreadyComplete1 && alreadyComplete2, completed);

                tryCompleter1();
                Assert.AreEqual(alreadyComplete2, completed);

                tryCompleter2();
                Assert.IsTrue(completed);
            }
        }

        [Test]
        public void PromiseRaceGroupAdoptsTheStateOfTheFirstCompletedPromise_WithCancelation_2_T(
            [Values] CancelationType cancelationType,
            [Values] bool cancelOnNonResolved,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType2,
            [Values] bool alreadyComplete2,
            [Values] bool completeFirstPromiseFirst)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var raceGroup = cancelationType == CancelationType.None ? PromiseRaceGroup<int>.New(out var groupCancelationToken, cancelOnNonResolved)
                    : cancelationType == CancelationType.Deferred ? PromiseRaceGroup<int>.New(cancelationSource.Token, out groupCancelationToken, cancelOnNonResolved)
                    : PromiseRaceGroup<int>.New(CancelationToken.Canceled(), out groupCancelationToken, cancelOnNonResolved);

                int value1 = 1;
                int value2 = 2;
                Exception expectedException = new Exception("Bang!");
                int expectedValue = GetExpectedValue(value1, value2, completeType1, alreadyComplete1, completeType2, alreadyComplete2, completeFirstPromiseFirst);
                var expectedState = GetExpectedState(cancelationType, cancelOnNonResolved, completeType1, alreadyComplete1, completeType2, alreadyComplete2, completeFirstPromiseFirst);

                bool completed = false;

                raceGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, groupCancelationToken, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, value2, expectedException, groupCancelationToken, out var tryCompleter2))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;

                        Assert.AreEqual(expectedState, result.State);
                        if (expectedState == Promise.State.Rejected)
                        {
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
                        else if (expectedState == Promise.State.Resolved)
                        {
                            Assert.AreEqual(expectedValue, result.Value);
                        }
                    })
                    .Forget();

                if (!alreadyComplete1 && (alreadyComplete2 || !completeFirstPromiseFirst))
                {
                    // If the second promise should complete first, swap them.
                    (tryCompleter1, tryCompleter2) = (tryCompleter2, tryCompleter1);
                    (completeType1, completeType2) = (completeType2, completeType1);
                    (alreadyComplete1, alreadyComplete2) = (alreadyComplete2, alreadyComplete1);
                }

                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || (alreadyComplete1 && alreadyComplete2)
                    || (cancelOnNonResolved && (alreadyComplete1 || alreadyComplete2))
                    || (alreadyComplete1 && completeType1 == CompleteType.Resolve),
                    completed);

                tryCompleter1();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || alreadyComplete2
                    || cancelOnNonResolved
                    || completeType1 == CompleteType.Resolve,
                    completed);

                cancelationSource.Cancel();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || alreadyComplete2
                    || cancelOnNonResolved
                    || completeType1 == CompleteType.Resolve,
                    completed);

                tryCompleter2();
                Assert.IsTrue(completed);
            }
        }

        [Test]
        public void PromiseRaceGroup_NonResolved_CancelationCallbackExceptionsArePropagated_2_T(
            [Values(CancelationType.None, CancelationType.Deferred)] CancelationType cancelationType,
            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            Test_PromiseRaceGroup_CancelationCallbackExceptionsArePropagated_2_T(cancelationType, true, completeType1, alreadyComplete1, completeType2, alreadyComplete2);
        }

        [Test]
        public void PromiseRaceGroup_CancelationCallbackExceptionsArePropagated_2_T(
            [Values(CancelationType.None, CancelationType.Deferred)] CancelationType cancelationType,
            [Values] bool cancelOnNonResolved,
            [Values(CompleteType.Resolve)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            Test_PromiseRaceGroup_CancelationCallbackExceptionsArePropagated_2_T(cancelationType, cancelOnNonResolved, completeType1, alreadyComplete1, completeType2, alreadyComplete2);
        }

        private static void Test_PromiseRaceGroup_CancelationCallbackExceptionsArePropagated_2_T(
            CancelationType cancelationType,
            bool cancelOnNonResolved,
            CompleteType completeType1,
            bool alreadyComplete1,
            CompleteType completeType2,
            bool alreadyComplete2)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var raceGroup = cancelationType == CancelationType.None ? PromiseRaceGroup<int>.New(out var groupCancelationToken, cancelOnNonResolved)
                    : cancelationType == CancelationType.Deferred ? PromiseRaceGroup<int>.New(cancelationSource.Token, out groupCancelationToken, cancelOnNonResolved)
                    : PromiseRaceGroup<int>.New(CancelationToken.Canceled(), out groupCancelationToken, cancelOnNonResolved);

                Exception expectedException = new Exception("Error in cancelation!");
                groupCancelationToken.Register(() => { throw expectedException; });

                bool completed = false;

                raceGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, 1, new System.InvalidOperationException("Bang!"), out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, 2, new System.InvalidOperationException("Bang!"), out var tryCompleter2))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;
                        Assert.AreEqual(Promise.State.Rejected, result.State);
                        Assert.IsAssignableFrom<AggregateException>(result.Reason);

                        int expectedExceptionCount = 1;
                        if (completeType1 == CompleteType.Reject)
                        {
                            ++expectedExceptionCount;
                        }
                        if (completeType2 == CompleteType.Reject)
                        {
                            ++expectedExceptionCount;
                        }

                        var e = result.Reason.UnsafeAs<AggregateException>();
                        Assert.AreEqual(expectedExceptionCount, e.InnerExceptions.Count);
                        var cancelationAggregateException = e.InnerExceptions.OfType<AggregateException>().Single();
                        Assert.AreEqual(expectedExceptionCount - 1, e.InnerExceptions.OfType<System.InvalidOperationException>().Count());
                        Assert.AreEqual(1, cancelationAggregateException.InnerExceptions.Count);
                        Assert.AreEqual(expectedException, cancelationAggregateException.InnerExceptions[0]);
                    })
                    .Forget();

                Assert.AreEqual(alreadyComplete1 && alreadyComplete2, completed);

                tryCompleter1();
                Assert.AreEqual(alreadyComplete2, completed);

                tryCompleter2();
                Assert.IsTrue(completed);
            }
        }
    }
}