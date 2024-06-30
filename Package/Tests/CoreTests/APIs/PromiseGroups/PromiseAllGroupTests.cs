#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ProtoPromiseTests.APIs.PromiseGroups
{
    public class PromiseAllGroupTests
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
        public void PromiseAllGroup_UsingInvalidatedGroupThrows(
            [Values] CancelationType cancelationType,
            [Values] bool provideList)
        {
            var intPromise = Promise.Resolved(42);
            Assert.Catch<System.InvalidOperationException>(() => default(PromiseAllGroup<int>).Add(intPromise));
            Assert.Catch<System.InvalidOperationException>(() => default(PromiseAllGroup<int>).WaitAsync());

            using (var cancelationSource = CancelationSource.New())
            {
                var list = provideList ? new List<int>() : null;
                var allGroup1 = cancelationType == CancelationType.None ? PromiseAllGroup<int>.New(out _, list)
                    : cancelationType == CancelationType.Deferred ? PromiseAllGroup<int>.New(cancelationSource.Token, out _, list)
                    : PromiseAllGroup<int>.New(CancelationToken.Canceled(), out _, list);

                var allGroup2 = allGroup1.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => allGroup1.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => allGroup1.WaitAsync());

                var allGroup3 = allGroup2.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => allGroup2.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => allGroup2.WaitAsync());

                allGroup3.WaitAsync().Forget();
                Assert.Catch<System.InvalidOperationException>(() => allGroup3.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => allGroup3.WaitAsync());

                intPromise.Forget();
            }
        }

        [Test]
        public void PromiseAllGroupIsResolvedWhenNoPromisesAreAdded(
            [Values] CancelationType cancelationType,
            [Values] bool provideList)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var list = provideList ? new List<int>() : null;
                var allGroup = cancelationType == CancelationType.None ? PromiseAllGroup<int>.New(out _, list)
                    : cancelationType == CancelationType.Deferred ? PromiseAllGroup<int>.New(cancelationSource.Token, out _, list)
                    : PromiseAllGroup<int>.New(CancelationToken.Canceled(), out _, list);

                bool resolved = false;

                allGroup
                    .WaitAsync()
                    .Then(values =>
                    {
                        resolved = true;
                        Assert.Zero(values.Count);
                        if (provideList)
                        {
                            Assert.AreSame(list, values);
                        }
                    })
                    .Forget();

                Assert.True(resolved);
            }
        }

        [Test]
        public void PromiseAllGroupIsCompletedWhenAllPromisesAreCompleted_1(
            [Values] CancelationType cancelationType,
            [Values] bool provideList,
            [Values] CompleteType completeType,
            [Values] bool alreadyComplete)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var list = provideList ? new List<int>() : null;
                var allGroup = cancelationType == CancelationType.None ? PromiseAllGroup<int>.New(out _, list)
                    : cancelationType == CancelationType.Deferred ? PromiseAllGroup<int>.New(cancelationSource.Token, out _, list)
                    : PromiseAllGroup<int>.New(CancelationToken.Canceled(), out _, list);

                Exception expectedException = new Exception("Bang!");

                int value1 = 1;
                bool completed = false;

                allGroup
                    .Add(TestHelper.BuildPromise(completeType, alreadyComplete, value1, expectedException, out var tryCompleter))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;

                        Assert.AreEqual(completeType, (CompleteType) result.State);
                        if (completeType == CompleteType.Reject)
                        {
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
                        else if (completeType == CompleteType.Resolve)
                        {
                            CollectionAssert.AreEqual(new[] { value1 }, result.Value);
                            if (provideList)
                            {
                                Assert.AreSame(list, result.Value);
                            }
                        }
                    })
                    .Forget();

                Assert.AreEqual(alreadyComplete, completed);

                tryCompleter();
                Assert.IsTrue(completed);
            }
        }

        [Test]
        public void PromiseAllGroupIsCompletedWhenAllPromisesAreCompleted_2(
            [Values] CancelationType cancelationType,
            [Values] bool provideList,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var list = provideList ? new List<int>() : null;
                var allGroup = cancelationType == CancelationType.None ? PromiseAllGroup<int>.New(out _, list)
                    : cancelationType == CancelationType.Deferred ? PromiseAllGroup<int>.New(cancelationSource.Token, out _, list)
                    : PromiseAllGroup<int>.New(CancelationToken.Canceled(), out _, list);

                Exception expectedException = new Exception("Bang!");

                int value1 = 1;
                int value2 = 2;
                bool completed = false;

                allGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, value2, expectedException, out var tryCompleter2))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;

                        if (completeType1 == CompleteType.Reject || completeType2 == CompleteType.Reject)
                        {
                            Assert.AreEqual(Promise.State.Rejected, result.State);
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
                        else if (completeType1 == CompleteType.Cancel || completeType2 == CompleteType.Cancel)
                        {
                            Assert.AreEqual(Promise.State.Canceled, result.State);
                        }
                        else
                        {
                            Assert.AreEqual(Promise.State.Resolved, result.State);
                            CollectionAssert.AreEqual(new[] { value1, value2 }, result.Value);
                            if (provideList)
                            {
                                Assert.AreSame(list, result.Value);
                            }
                        }
                    })
                    .Forget();

                Assert.AreEqual(alreadyComplete1 && alreadyComplete2, completed);

                tryCompleter1();
                Assert.AreEqual(alreadyComplete2, completed);

                tryCompleter2();
                Assert.IsTrue(completed);
            }
        }

        private static Promise.State GetExpectedState(CancelationType cancelationType, (CompleteType completeType, bool alreadyComplete)[] completeValues)
        {
            if (cancelationType == CancelationType.Immediate)
            {
                return Promise.State.Canceled;
            }

            bool allAlreadyComplete = true;
            foreach (var cv in completeValues)
            {
                if (cv.alreadyComplete)
                {
                    if (cv.completeType != CompleteType.Resolve)
                    {
                        return (Promise.State) cv.completeType;
                    }
                }
                else
                {
                    allAlreadyComplete = false;
                }
            }
            if (allAlreadyComplete)
            {
                return Promise.State.Resolved;
            }

            if (cancelationType == CancelationType.None)
            {
                foreach (var cv in completeValues)
                {
                    if (cv.completeType != CompleteType.Resolve)
                    {
                        return (Promise.State) cv.completeType;
                    }
                }
                return Promise.State.Resolved;
            }

            // CancelationType.Deferred - cancelation source is canceled after the first promise is completed.
            if (completeValues[0].completeType != CompleteType.Resolve)
            {
                return (Promise.State) completeValues[0].completeType;
            }
            if (completeValues.Skip(1).All(cv => cv.alreadyComplete && cv.completeType == CompleteType.Resolve))
            {
                return Promise.State.Resolved;
            }
            return Promise.State.Canceled;
        }

        private static bool GetShouldBeComplete(int indexOfCompletion, (CompleteType completeType, bool alreadyComplete)[] completeValues)
        {
            // indexOfCompletion = 0 means none explicitly completed yet.
            for (int i = 0; i < indexOfCompletion; ++i)
            {
                if (completeValues[i].completeType != CompleteType.Resolve)
                {
                    return true;
                }
            }
            bool allAlreadyComplete = true;
            for (int i = indexOfCompletion; i < completeValues.Length; ++i)
            {
                if (!completeValues[i].alreadyComplete)
                {
                    allAlreadyComplete = false;
                    continue;
                }
                if (completeValues[i].completeType != CompleteType.Resolve)
                {
                    return true;
                }
            }
            return allAlreadyComplete;
        }

        [Test]
        public void PromiseAllGroupIsCompletedWhenAllPromisesAreCompleted_WithCancelation_2(
            [Values] CancelationType cancelationType,
            [Values] bool provideList,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var list = provideList ? new List<int>() : null;
                var allGroup = cancelationType == CancelationType.None ? PromiseAllGroup<int>.New(out var groupCancelationToken, list)
                    : cancelationType == CancelationType.Deferred ? PromiseAllGroup<int>.New(cancelationSource.Token, out groupCancelationToken, list)
                    : PromiseAllGroup<int>.New(CancelationToken.Canceled(), out groupCancelationToken, list);

                Exception expectedException = new Exception("Bang!");

                int value1 = 1;
                int value2 = 2;
                bool completed = false;

                var completeValues = new[]
                {
                    (completeType1, alreadyComplete1),
                    (completeType2, alreadyComplete2)
                };

                allGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, groupCancelationToken, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, value2, expectedException, groupCancelationToken, out var tryCompleter2))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;

                        var expectedState = GetExpectedState(cancelationType, completeValues);
                        Assert.AreEqual(expectedState, result.State);
                        if (expectedState == Promise.State.Rejected)
                        {
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
                        else if (expectedState == Promise.State.Resolved)
                        {
                            CollectionAssert.AreEqual(new[] { value1, value2 }, result.Value);
                            if (provideList)
                            {
                                Assert.AreSame(list, result.Value);
                            }
                        }
                    })
                    .Forget();

                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || GetShouldBeComplete(0, completeValues),
                    completed);

                tryCompleter1();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || GetShouldBeComplete(1, completeValues),
                    completed);

                cancelationSource.Cancel();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(1, completeValues),
                    completed);

                tryCompleter2();
                Assert.IsTrue(completed);
            }
        }

        [Test]
        public void PromiseAllGroup_CancelationCallbackExceptionsArePropagated_2(
            [Values(CancelationType.None, CancelationType.Deferred)] CancelationType cancelationType,
            [Values] bool provideList,
            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var list = provideList ? new List<int>() : null;
                var allGroup = cancelationType == CancelationType.None ? PromiseAllGroup<int>.New(out var groupCancelationToken, list)
                    : PromiseAllGroup<int>.New(cancelationSource.Token, out groupCancelationToken, list);

                Exception expectedException = new Exception("Error in cancelation!");
                groupCancelationToken.Register(() => { throw expectedException; });

                bool completed = false;

                allGroup
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
                        Assert.IsInstanceOf<AggregateException>(e.InnerExceptions[0]);
                        if (expectedExceptionCount > 1)
                        {
                            Assert.IsInstanceOf<System.InvalidOperationException>(e.InnerExceptions[1]);
                        }
                        Assert.AreEqual(1, e.InnerExceptions[0].UnsafeAs<AggregateException>().InnerExceptions.Count);
                        Assert.AreEqual(expectedException, e.InnerExceptions[0].UnsafeAs<AggregateException>().InnerExceptions[0]);
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