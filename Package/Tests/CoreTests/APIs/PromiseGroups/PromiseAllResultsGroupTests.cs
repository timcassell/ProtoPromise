//#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
//#define PROMISE_DEBUG
//#else
//#undef PROMISE_DEBUG
//#endif

//using NUnit.Framework;
//using Proto.Promises;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace ProtoPromiseTests.APIs.PromiseGroups
//{
//    public class PromiseAllResultsGroupTests
//    {
//        [SetUp]
//        public void Setup()
//        {
//            TestHelper.Setup();
//        }

//        [TearDown]
//        public void Teardown()
//        {
//            TestHelper.Cleanup();
//        }

//        private static void AssertResult<TReject>(CompleteType completeType, Promise.ResultContainer resultContainer, TReject expectedReason)
//        {
//            Assert.AreEqual((Promise.State) completeType, resultContainer.State);
//            if (completeType == CompleteType.Reject)
//            {
//                Assert.AreEqual(expectedReason, resultContainer.Reason);
//            }
//        }

//        private static void AssertResult<TResult, TReject>(CompleteType completeType, Promise<TResult>.ResultContainer resultContainer, TResult expectedResult, TReject expectedReason)
//        {
//            Assert.AreEqual((Promise.State) completeType, resultContainer.State);
//            if (completeType == CompleteType.Resolve)
//            {
//                Assert.AreEqual(expectedResult, resultContainer.Value);
//            }
//            else if (completeType == CompleteType.Reject)
//            {
//                Assert.AreEqual(expectedReason, resultContainer.Reason);
//            }
//        }

//        [Test]
//        public void PromiseAllResultsGroup_UsingInvalidatedGroupThrows_void(
//            [Values] CancelationType cancelationType)
//        {
//            var voidPromise = Promise.Resolved();
//            Assert.Catch<System.InvalidOperationException>(() => default(PromiseAllResultsGroup).Add(voidPromise));

//            using (var cancelationSource = CancelationSource.New())
//            {
//                var mergeGroup1 = cancelationType == CancelationType.None ? PromiseAllResultsGroup.New(out _)
//                    : cancelationType == CancelationType.Deferred ? PromiseAllResultsGroup.New(cancelationSource.Token, out _)
//                    : PromiseAllResultsGroup.New(CancelationToken.Canceled(), out _);

//                var mergeGroup2 = mergeGroup1.Add(Promise.Resolved());
//                Assert.Catch<System.InvalidOperationException>(() => mergeGroup1.Add(voidPromise));
//                Assert.Catch<System.InvalidOperationException>(() => mergeGroup1.WaitAsync());

//                var mergeGroup3 = mergeGroup2.Add(Promise.Resolved());
//                Assert.Catch<System.InvalidOperationException>(() => mergeGroup2.Add(voidPromise));
//                Assert.Catch<System.InvalidOperationException>(() => mergeGroup2.WaitAsync());

//                mergeGroup3.WaitAsync().Forget();
//                Assert.Catch<System.InvalidOperationException>(() => mergeGroup3.Add(voidPromise));
//                Assert.Catch<System.InvalidOperationException>(() => mergeGroup3.WaitAsync());

//                voidPromise.Forget();
//            }
//        }

//        [Test]
//        public void PromiseAllResultsGroupIsResolvedWhenNoPromisesAreAdded_void(
//            [Values] CancelationType cancelationType,
//            [Values] bool provideList)
//        {
//            using (var cancelationSource = CancelationSource.New())
//            {
//                var list = provideList ? new List<Promise.ResultContainer>() : null;
//                var allGroup = cancelationType == CancelationType.None ? PromiseAllResultsGroup.New(out _, list)
//                    : cancelationType == CancelationType.Deferred ? PromiseAllResultsGroup.New(cancelationSource.Token, out _, list)
//                    : PromiseAllResultsGroup.New(CancelationToken.Canceled(), out _, list);

//                bool resolved = false;

//                allGroup
//                    .WaitAsync()
//                    .Then(values =>
//                    {
//                        resolved = true;
//                        Assert.Zero(values.Count);
//                        if (provideList)
//                        {
//                            Assert.AreSame(list, values);
//                        }
//                    })
//                    .Forget();

//                Assert.True(resolved);
//            }
//        }

//        [Test]
//        public void PromiseAllResultsGroupIsResolvedWhenAllPromisesAreCompleted_1_void(
//            [Values] CancelationType cancelationType,
//            [Values] bool provideList,
//            [Values] CompleteType completeType,
//            [Values] bool alreadyComplete)
//        {
//            using (var cancelationSource = CancelationSource.New())
//            {
//                var list = provideList ? new List<Promise.ResultContainer>() : null;
//                var allGroup = cancelationType == CancelationType.None ? PromiseAllResultsGroup.New(out _, list)
//                    : cancelationType == CancelationType.Deferred ? PromiseAllResultsGroup.New(cancelationSource.Token, out _, list)
//                    : PromiseAllResultsGroup.New(CancelationToken.Canceled(), out _, list);

//                Exception expectedException = new Exception("Bang!");

//                bool completed = false;

//                allGroup
//                    .Add(TestHelper.BuildPromise(completeType, alreadyComplete, expectedException, out var tryCompleter))
//                    .WaitAsync()
//                    .ContinueWith(result =>
//                    {
//                        completed = true;
//                        Assert.AreEqual(Promise.State.Resolved, result.State);
//                        Assert.AreEqual(1, result.Value.Count);
//                        AssertResult(completeType, result.Value[0], expectedException);
//                        if (provideList)
//                        {
//                            Assert.AreSame(list, result.Value);
//                        }
//                    })
//                    .Forget();

//                Assert.AreEqual(alreadyComplete, completed);

//                tryCompleter();
//                Assert.IsTrue(completed);
//            }
//        }

//        [Test]
//        public void PromiseAllResultsGroupIsResolvedWhenAllPromisesAreCompleted_2_void(
//            [Values] CancelationType cancelationType,
//            [Values] bool provideList,
//            [Values] CompleteType completeType1,
//            [Values] bool alreadyComplete1,
//            [Values] CompleteType completeType2,
//            [Values] bool alreadyComplete2)
//        {
//            using (var cancelationSource = CancelationSource.New())
//            {
//                var list = provideList ? new List<Promise.ResultContainer>() : null;
//                var allGroup = cancelationType == CancelationType.None ? PromiseAllResultsGroup.New(out _, list)
//                    : cancelationType == CancelationType.Deferred ? PromiseAllResultsGroup.New(cancelationSource.Token, out _, list)
//                    : PromiseAllResultsGroup.New(CancelationToken.Canceled(), out _, list);

//                Exception expectedException = new Exception("Bang!");

//                bool completed = false;

//                allGroup
//                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, expectedException, out var tryCompleter1))
//                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, expectedException, out var tryCompleter2))
//                    .WaitAsync()
//                    .ContinueWith(result =>
//                    {
//                        completed = true;
//                        Assert.AreEqual(Promise.State.Resolved, result.State);
//                        Assert.AreEqual(2, result.Value.Count);
//                        AssertResult(completeType1, result.Value[0], expectedException);
//                        AssertResult(completeType2, result.Value[1], expectedException);
//                        if (provideList)
//                        {
//                            Assert.AreSame(list, result.Value);
//                        }
//                    })
//                    .Forget();

//                Assert.AreEqual(alreadyComplete1 && alreadyComplete2, completed);

//                tryCompleter1();
//                Assert.AreEqual(alreadyComplete2, completed);

//                tryCompleter2();
//                Assert.IsTrue(completed);
//            }
//        }

//        private static bool GetShouldBeComplete(int indexOfCompletion, (CompleteType completeType, bool alreadyComplete)[] completeValues)
//        {
//            // indexOfCompletion = 0 means none explicitly completed yet.
//            for (int i = 0; i < indexOfCompletion; ++i)
//            {
//                if (completeValues[i].completeType != CompleteType.Resolve)
//                {
//                    return true;
//                }
//            }
//            bool allAlreadyComplete = true;
//            for (int i = indexOfCompletion; i < completeValues.Length; ++i)
//            {
//                if (!completeValues[i].alreadyComplete)
//                {
//                    allAlreadyComplete = false;
//                    continue;
//                }
//                if (completeValues[i].completeType != CompleteType.Resolve)
//                {
//                    return true;
//                }
//            }
//            return allAlreadyComplete;
//        }

//        private static CompleteType GetExpectedState(int index, CancelationType cancelationType, (CompleteType completeType, bool alreadyComplete)[] completeValues)
//        {
//            if (cancelationType == CancelationType.Immediate)
//            {
//                return CompleteType.Cancel;
//            }

//            for (int i = 0; i < index; ++i)
//            {
//                var cv = completeValues[i];
//                if (cv.alreadyComplete && cv.completeType != CompleteType.Resolve)
//                {
//                    return CompleteType.Cancel;
//                }
//            }

//            if (completeValues[index].alreadyComplete)
//            {
//                return completeValues[index].completeType;
//            }

//            for (int i = index + 1; i < completeValues.Length; ++i)
//            {
//                var cv = completeValues[i];
//                if (cv.alreadyComplete && cv.completeType != CompleteType.Resolve)
//                {
//                    return CompleteType.Cancel;
//                }
//            }

//            if (cancelationType == CancelationType.Deferred)
//            {
//                // Cancelation source is canceled after the first promise is completed.
//                if (index == 0)
//                {
//                    return completeValues[0].completeType;
//                }

//                return CompleteType.Cancel;
//            }

//            // CancelationType.None

//            for (int i = 0; i < index; ++i)
//            {
//                if (completeValues[i].completeType != CompleteType.Resolve)
//                {
//                    return CompleteType.Cancel;
//                }
//            }

//            return completeValues[index].completeType;
//        }

//        [Test]
//        public void PromiseAllResultsGroupIsResolvedWhenAllPromisesAreCompleted_WithCancelation_2_void(
//            [Values] CancelationType cancelationType,
//            [Values] bool provideList,
//            [Values] CompleteType completeType1,
//            [Values] bool alreadyComplete1,
//            [Values] CompleteType completeType2,
//            [Values] bool alreadyComplete2)
//        {
//            using (var cancelationSource = CancelationSource.New())
//            {
//                var list = provideList ? new List<Promise.ResultContainer>() : null;
//                var allGroup = cancelationType == CancelationType.None ? PromiseAllResultsGroup.New(out var groupCancelationToken, list)
//                    : cancelationType == CancelationType.Deferred ? PromiseAllResultsGroup.New(cancelationSource.Token, out groupCancelationToken, list)
//                    : PromiseAllResultsGroup.New(CancelationToken.Canceled(), out groupCancelationToken, list);

//                Exception expectedException = new Exception("Bang!");

//                bool completed = false;

//                var completeValues = new[]
//                {
//                    (completeType1, alreadyComplete1),
//                    (completeType2, alreadyComplete2)
//                };

//                allGroup
//                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, expectedException, groupCancelationToken, out var tryCompleter1))
//                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, expectedException, groupCancelationToken, out var tryCompleter2))
//                    .WaitAsync()
//                    .ContinueWith(result =>
//                    {
//                        completed = true;
//                        Assert.AreEqual(Promise.State.Resolved, result.State);
//                        Assert.AreEqual(2, result.Value.Count);
//                        AssertResult(GetExpectedState(0, cancelationType, completeValues), result.Value[0], expectedException);
//                        AssertResult(GetExpectedState(1, cancelationType, completeValues), result.Value[1], expectedException);
//                        if (provideList)
//                        {
//                            Assert.AreSame(list, result.Value);
//                        }
//                    })
//                    .Forget();

//                Assert.AreEqual(cancelationType == CancelationType.Immediate
//                    || GetShouldBeComplete(0, completeValues),
//                    completed);

//                tryCompleter1();
//                Assert.AreEqual(cancelationType == CancelationType.Immediate
//                    || GetShouldBeComplete(1, completeValues),
//                    completed);

//                cancelationSource.Cancel();
//                Assert.AreEqual(cancelationType == CancelationType.Immediate
//                    || cancelationType == CancelationType.Deferred
//                    || GetShouldBeComplete(1, completeValues),
//                    completed);

//                tryCompleter2();
//                Assert.IsTrue(completed);
//            }
//        }

//        [Test]
//        public void PromiseAllResultsGroup_CancelationCallbackExceptionsArePropagated_2_void(
//            [Values(CancelationType.None, CancelationType.Deferred)] CancelationType cancelationType,
//            [Values] bool provideList,
//            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
//            [Values] bool alreadyComplete1,
//            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType2,
//            [Values] bool alreadyComplete2)
//        {
//            using (var cancelationSource = CancelationSource.New())
//            {
//                var list = provideList ? new List<Promise.ResultContainer>() : null;
//                var allGroup = cancelationType == CancelationType.None ? PromiseAllResultsGroup.New(out var groupCancelationToken, list)
//                    : cancelationType == CancelationType.Deferred ? PromiseAllResultsGroup.New(cancelationSource.Token, out groupCancelationToken, list)
//                    : PromiseAllResultsGroup.New(CancelationToken.Canceled(), out groupCancelationToken, list);

//                Exception expectedException = new Exception("Error in cancelation!");
//                groupCancelationToken.Register(() => { throw expectedException; });

//                bool completed = false;

//                allGroup
//                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, new System.InvalidOperationException("Bang!"), out var tryCompleter1))
//                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, new System.InvalidOperationException("Bang!"), out var tryCompleter2))
//                    .WaitAsync()
//                    .ContinueWith(result =>
//                    {
//                        completed = true;
//                        Assert.AreEqual(Promise.State.Rejected, result.State);
//                        Assert.IsAssignableFrom<AggregateException>(result.Reason);

//                        var e = result.Reason.UnsafeAs<AggregateException>();
//                        Assert.AreEqual(1, e.InnerExceptions.Count);
//                        Assert.IsInstanceOf<AggregateException>(e.InnerExceptions[0]);
//                        Assert.AreEqual(1, e.InnerExceptions[0].UnsafeAs<AggregateException>().InnerExceptions.Count);
//                        Assert.AreEqual(expectedException, e.InnerExceptions[0].UnsafeAs<AggregateException>().InnerExceptions[0]);
//                    })
//                    .Forget();

//                Assert.AreEqual(alreadyComplete1 && alreadyComplete2, completed);

//                tryCompleter1();
//                Assert.AreEqual(alreadyComplete2, completed);

//                tryCompleter2();
//                Assert.IsTrue(completed);
//            }
//        }

//        [Test]
//        public void PromiseAllResultsGroup_UsingInvalidatedGroupThrows_T(
//            [Values] CancelationType cancelationType)
//        {
//            var intPromise = Promise.Resolved(42);
//            Assert.Catch<System.InvalidOperationException>(() => default(PromiseAllResultsGroup<int>).Add(intPromise));

//            using (var cancelationSource = CancelationSource.New())
//            {
//                var mergeGroup1 = cancelationType == CancelationType.None ? PromiseAllResultsGroup<int>.New(out _)
//                    : cancelationType == CancelationType.Deferred ? PromiseAllResultsGroup<int>.New(cancelationSource.Token, out _)
//                    : PromiseAllResultsGroup<int>.New(CancelationToken.Canceled(), out _);

//                var mergeGroup2 = mergeGroup1.Add(Promise.Resolved(2));
//                Assert.Catch<System.InvalidOperationException>(() => mergeGroup1.Add(intPromise));
//                Assert.Catch<System.InvalidOperationException>(() => mergeGroup1.WaitAsync());

//                var mergeGroup3 = mergeGroup2.Add(Promise.Resolved(2));
//                Assert.Catch<System.InvalidOperationException>(() => mergeGroup2.Add(intPromise));
//                Assert.Catch<System.InvalidOperationException>(() => mergeGroup2.WaitAsync());

//                mergeGroup3.WaitAsync().Forget();
//                Assert.Catch<System.InvalidOperationException>(() => mergeGroup3.Add(intPromise));
//                Assert.Catch<System.InvalidOperationException>(() => mergeGroup3.WaitAsync());

//                intPromise.Forget();
//            }
//        }

//        [Test]
//        public void PromiseAllResultsGroupIsResolvedWhenNoPromisesAreAdded_T(
//            [Values] CancelationType cancelationType,
//            [Values] bool provideList)
//        {
//            using (var cancelationSource = CancelationSource.New())
//            {
//                var list = provideList ? new List<Promise<int>.ResultContainer>() : null;
//                var allGroup = cancelationType == CancelationType.None ? PromiseAllResultsGroup<int>.New(out _, list)
//                    : cancelationType == CancelationType.Deferred ? PromiseAllResultsGroup<int>.New(cancelationSource.Token, out _, list)
//                    : PromiseAllResultsGroup<int>.New(CancelationToken.Canceled(), out _, list);

//                bool resolved = false;

//                allGroup
//                    .WaitAsync()
//                    .Then(values =>
//                    {
//                        resolved = true;
//                        Assert.Zero(values.Count);
//                        if (provideList)
//                        {
//                            Assert.AreSame(list, values);
//                        }
//                    })
//                    .Forget();

//                Assert.True(resolved);
//            }
//        }

//        [Test]
//        public void PromiseAllResultsGroupIsResolvedWhenAllPromisesAreCompleted_1_T(
//            [Values] CancelationType cancelationType,
//            [Values] bool provideList,
//            [Values] CompleteType completeType,
//            [Values] bool alreadyComplete)
//        {
//            using (var cancelationSource = CancelationSource.New())
//            {
//                var list = provideList ? new List<Promise<int>.ResultContainer>() : null;
//                var allGroup = cancelationType == CancelationType.None ? PromiseAllResultsGroup<int>.New(out _, list)
//                    : cancelationType == CancelationType.Deferred ? PromiseAllResultsGroup<int>.New(cancelationSource.Token, out _, list)
//                    : PromiseAllResultsGroup<int>.New(CancelationToken.Canceled(), out _, list);

//                Exception expectedException = new Exception("Bang!");

//                int value1 = 1;
//                bool completed = false;

//                allGroup
//                    .Add(TestHelper.BuildPromise(completeType, alreadyComplete, value1, expectedException, out var tryCompleter))
//                    .WaitAsync()
//                    .ContinueWith(result =>
//                    {
//                        completed = true;
//                        Assert.AreEqual(Promise.State.Resolved, result.State);
//                        Assert.AreEqual(1, result.Value.Count);
//                        AssertResult(completeType, result.Value[0], value1, expectedException);
//                        if (provideList)
//                        {
//                            Assert.AreSame(list, result.Value);
//                        }
//                    })
//                    .Forget();

//                Assert.AreEqual(alreadyComplete, completed);

//                tryCompleter();
//                Assert.IsTrue(completed);
//            }
//        }

//        [Test]
//        public void PromiseAllResultsGroupIsResolvedWhenAllPromisesAreCompleted_2_T(
//            [Values] CancelationType cancelationType,
//            [Values] bool provideList,
//            [Values] CompleteType completeType1,
//            [Values] bool alreadyComplete1,
//            [Values] CompleteType completeType2,
//            [Values] bool alreadyComplete2)
//        {
//            using (var cancelationSource = CancelationSource.New())
//            {
//                var list = provideList ? new List<Promise<int>.ResultContainer>() : null;
//                var allGroup = cancelationType == CancelationType.None ? PromiseAllResultsGroup<int>.New(out _, list)
//                    : cancelationType == CancelationType.Deferred ? PromiseAllResultsGroup<int>.New(cancelationSource.Token, out _, list)
//                    : PromiseAllResultsGroup<int>.New(CancelationToken.Canceled(), out _, list);

//                Exception expectedException = new Exception("Bang!");

//                int value1 = 1;
//                int value2 = 2;
//                bool completed = false;

//                allGroup
//                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, out var tryCompleter1))
//                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, value2, expectedException, out var tryCompleter2))
//                    .WaitAsync()
//                    .ContinueWith(result =>
//                    {
//                        completed = true;
//                        Assert.AreEqual(Promise.State.Resolved, result.State);
//                        Assert.AreEqual(2, result.Value.Count);
//                        AssertResult(completeType1, result.Value[0], value1, expectedException);
//                        AssertResult(completeType2, result.Value[1], value2, expectedException);
//                        if (provideList)
//                        {
//                            Assert.AreSame(list, result.Value);
//                        }
//                    })
//                    .Forget();

//                Assert.AreEqual(alreadyComplete1 && alreadyComplete2, completed);

//                tryCompleter1();
//                Assert.AreEqual(alreadyComplete2, completed);

//                tryCompleter2();
//                Assert.IsTrue(completed);
//            }
//        }

//        [Test]
//        public void PromiseAllResultsGroupIsResolvedWhenAllPromisesAreCompleted_WithCancelation_2_T(
//            [Values] CancelationType cancelationType,
//            [Values] bool provideList,
//            [Values] CompleteType completeType1,
//            [Values] bool alreadyComplete1,
//            [Values] CompleteType completeType2,
//            [Values] bool alreadyComplete2)
//        {
//            using (var cancelationSource = CancelationSource.New())
//            {
//                var list = provideList ? new List<Promise<int>.ResultContainer>() : null;
//                var allGroup = cancelationType == CancelationType.None ? PromiseAllResultsGroup<int>.New(out var groupCancelationToken, list)
//                    : cancelationType == CancelationType.Deferred ? PromiseAllResultsGroup<int>.New(cancelationSource.Token, out groupCancelationToken, list)
//                    : PromiseAllResultsGroup<int>.New(CancelationToken.Canceled(), out groupCancelationToken, list);

//                Exception expectedException = new Exception("Bang!");

//                int value1 = 1;
//                int value2 = 2;
//                bool completed = false;

//                var completeValues = new[]
//                {
//                    (completeType1, alreadyComplete1),
//                    (completeType2, alreadyComplete2)
//                };

//                allGroup
//                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, groupCancelationToken, out var tryCompleter1))
//                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, value2, expectedException, groupCancelationToken, out var tryCompleter2))
//                    .WaitAsync()
//                    .ContinueWith(result =>
//                    {
//                        completed = true;
//                        Assert.AreEqual(Promise.State.Resolved, result.State);
//                        Assert.AreEqual(2, result.Value.Count);
//                        AssertResult(GetExpectedState(0, cancelationType, completeValues), result.Value[0], value1, expectedException);
//                        AssertResult(GetExpectedState(1, cancelationType, completeValues), result.Value[1], value2, expectedException);
//                        if (provideList)
//                        {
//                            Assert.AreSame(list, result.Value);
//                        }
//                    })
//                    .Forget();

//                Assert.AreEqual(cancelationType == CancelationType.Immediate
//                    || GetShouldBeComplete(0, completeValues),
//                    completed);

//                tryCompleter1();
//                Assert.AreEqual(cancelationType == CancelationType.Immediate
//                    || GetShouldBeComplete(1, completeValues),
//                    completed);

//                cancelationSource.Cancel();
//                Assert.AreEqual(cancelationType == CancelationType.Immediate
//                    || cancelationType == CancelationType.Deferred
//                    || GetShouldBeComplete(1, completeValues),
//                    completed);

//                tryCompleter2();
//                Assert.IsTrue(completed);
//            }
//        }

//        [Test]
//        public void PromiseAllResultsGroup_CancelationCallbackExceptionsArePropagated_2_T(
//            [Values(CancelationType.None, CancelationType.Deferred)] CancelationType cancelationType,
//            [Values] bool provideList,
//            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
//            [Values] bool alreadyComplete1,
//            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType2,
//            [Values] bool alreadyComplete2)
//        {
//            using (var cancelationSource = CancelationSource.New())
//            {
//                var list = provideList ? new List<Promise<int>.ResultContainer>() : null;
//                var allGroup = cancelationType == CancelationType.None ? PromiseAllResultsGroup<int>.New(out var groupCancelationToken, list)
//                    : cancelationType == CancelationType.Deferred ? PromiseAllResultsGroup<int>.New(cancelationSource.Token, out groupCancelationToken, list)
//                    : PromiseAllResultsGroup<int>.New(CancelationToken.Canceled(), out groupCancelationToken, list);

//                Exception expectedException = new Exception("Error in cancelation!");
//                groupCancelationToken.Register(() => { throw expectedException; });

//                bool completed = false;

//                allGroup
//                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, 1, new System.InvalidOperationException("Bang!"), out var tryCompleter1))
//                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, 2, new System.InvalidOperationException("Bang!"), out var tryCompleter2))
//                    .WaitAsync()
//                    .ContinueWith(result =>
//                    {
//                        completed = true;
//                        Assert.AreEqual(Promise.State.Rejected, result.State);
//                        Assert.IsAssignableFrom<AggregateException>(result.Reason);

//                        var e = result.Reason.UnsafeAs<AggregateException>();
//                        Assert.AreEqual(1, e.InnerExceptions.Count);
//                        Assert.IsInstanceOf<AggregateException>(e.InnerExceptions[0]);
//                        Assert.AreEqual(1, e.InnerExceptions[0].UnsafeAs<AggregateException>().InnerExceptions.Count);
//                        Assert.AreEqual(expectedException, e.InnerExceptions[0].UnsafeAs<AggregateException>().InnerExceptions[0]);
//                    })
//                    .Forget();

//                Assert.AreEqual(alreadyComplete1 && alreadyComplete2, completed);

//                tryCompleter1();
//                Assert.AreEqual(alreadyComplete2, completed);

//                tryCompleter2();
//                Assert.IsTrue(completed);
//            }
//        }
//    }
//}