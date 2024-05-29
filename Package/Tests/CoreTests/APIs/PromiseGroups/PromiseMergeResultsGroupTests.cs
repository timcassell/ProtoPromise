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
    public class PromiseMergeResultsGroupTests
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
        public void PromiseMergeResultsGroup_UsingInvalidatedGroupThrows(
            [Values] CancelationType cancelationType)
        {
            var voidPromise = Promise.Resolved();
            var intPromise = Promise.Resolved(42);
            Assert.Catch<System.InvalidOperationException>(() => default(PromiseMergeResultsGroup).Add(voidPromise));
            Assert.Catch<System.InvalidOperationException>(() => default(PromiseMergeResultsGroup).Add(intPromise));

            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup1 = cancelationType == CancelationType.None ? PromiseMergeResultsGroup.New(out _)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeResultsGroup.New(cancelationSource.Token, out _)
                    : PromiseMergeResultsGroup.New(CancelationToken.Canceled(), out _);

                var mergeGroup2 = mergeGroup1.Add(Promise.Resolved());
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup1.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup1.Add(intPromise));

                var mergeGroup3 = mergeGroup2.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup2.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup2.Add(intPromise));

                var mergeGroup4 = mergeGroup3.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup3.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup3.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup3.WaitAsync());

                var mergeGroup5 = mergeGroup4.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup4.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup4.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup4.WaitAsync());

                var mergeGroup6 = mergeGroup5.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup5.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup5.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup5.WaitAsync());

                var mergeGroup7 = mergeGroup6.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup6.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup6.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup6.WaitAsync());

                var mergeGroup8 = mergeGroup7.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup7.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup7.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup7.WaitAsync());

#if ENABLE_IL2CPP
                mergeGroup8.WaitAsync().Forget();
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup8.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup8.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup8.WaitAsync());
#else
                var mergeGroup9 = mergeGroup8.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup8.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup8.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup8.WaitAsync());

                var mergeGroup10 = mergeGroup9.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup9.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup9.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup9.WaitAsync());

                var mergeGroup11 = mergeGroup10.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup10.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup10.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup10.WaitAsync());

                var mergeGroup12 = mergeGroup11.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup11.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup11.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup11.WaitAsync());

                var mergeGroup13 = mergeGroup12.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup12.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup12.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup12.WaitAsync());

                var mergeGroup14 = mergeGroup13.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup13.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup13.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup13.WaitAsync());

                var mergeGroup15 = mergeGroup14.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup14.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup14.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup14.WaitAsync());

                mergeGroup15.WaitAsync().Forget();
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup15.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup15.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup15.WaitAsync());
#endif

                voidPromise.Forget();
                intPromise.Forget();
            }
        }

        private static void AssertResult<TReject>(CompleteType completeType, Promise.ResultContainer resultContainer, TReject expectedReason)
        {
            Assert.AreEqual((Promise.State) completeType, resultContainer.State);
            if (completeType == CompleteType.Reject)
            {
                Assert.AreEqual(expectedReason, resultContainer.Reason);
            }
        }

        private static void AssertResult<TResult, TReject>(CompleteType completeType, Promise<TResult>.ResultContainer resultContainer, TResult expectedResult, TReject expectedReason)
        {
            Assert.AreEqual((Promise.State) completeType, resultContainer.State);
            if (completeType == CompleteType.Resolve)
            {
                Assert.AreEqual(expectedResult, resultContainer.Value);
            }
            else if (completeType == CompleteType.Reject)
            {
                Assert.AreEqual(expectedReason, resultContainer.Reason);
            }
        }

        [Test]
        public void PromiseMergeResultsGroupIsResolvedWhenAllPromisesAreCompleted_void_void(
            [Values] CancelationType cancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeResultsGroup.New(out _)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeResultsGroup.New(cancelationSource.Token, out _)
                    : PromiseMergeResultsGroup.New(CancelationToken.Canceled(), out _);

                Exception expectedException = new Exception("Bang!");

                bool completed = false;

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, expectedException, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, expectedException, out var tryCompleter2))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;
                        Assert.AreEqual(Promise.State.Resolved, result.State);
                        var (result1, result2) = result.Value;
                        AssertResult(completeType1, result1, expectedException);
                        AssertResult(completeType2, result2, expectedException);
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
        public void PromiseMergeResultsGroupIsResolvedWhenAllPromisesAreCompleted_T_T(
            [Values] CancelationType cancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeResultsGroup.New(out _)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeResultsGroup.New(cancelationSource.Token, out _)
                    : PromiseMergeResultsGroup.New(CancelationToken.Canceled(), out _);

                Exception expectedException = new Exception("Bang!");
                int value1 = 42;
                string value2 = "Success";

                bool completed = false;

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, value2, expectedException, out var tryCompleter2))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;
                        Assert.AreEqual(Promise.State.Resolved, result.State);
                        var (result1, result2) = result.Value;
                        AssertResult(completeType1, result1, expectedException);
                        AssertResult(completeType2, result2, expectedException);
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
        public void PromiseMergeResultsGroupIsResolvedWhenAllPromisesAreCompleted_void_T_void_T(
            [Values] CancelationType cancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values] CompleteType completeType2,
            [Values] bool alreadyComplete2,
            // Reduce number of tests.
            [Values(false)] bool alreadyComplete3and4,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeResultsGroup.New(out _)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeResultsGroup.New(cancelationSource.Token, out _)
                    : PromiseMergeResultsGroup.New(CancelationToken.Canceled(), out _);

                Exception expectedException = new Exception("Bang!");
                int value1 = 42;
                string value2 = "Success";

                bool completed = false;

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, expectedException, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, value1, expectedException, out var tryCompleter2))
                    .Add(TestHelper.BuildPromise(completeType3, alreadyComplete3and4, expectedException, out var tryCompleter3))
                    .Add(TestHelper.BuildPromise(completeType4, alreadyComplete3and4, value2, expectedException, out var tryCompleter4))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;
                        Assert.AreEqual(Promise.State.Resolved, result.State);
                        var (result1, result2, result3, result4) = result.Value;
                        AssertResult(completeType1, result1, expectedException);
                        AssertResult(completeType2, result2, value1, expectedException);
                        AssertResult(completeType3, result3, expectedException);
                        AssertResult(completeType4, result4, value2, expectedException);
                    })
                    .Forget();

                Assert.AreEqual(alreadyComplete1 && alreadyComplete2 && alreadyComplete3and4, completed);

                tryCompleter1();
                Assert.AreEqual(alreadyComplete2 && alreadyComplete3and4, completed);

                tryCompleter2();
                Assert.AreEqual(alreadyComplete3and4, completed);
                tryCompleter3();
                Assert.AreEqual(alreadyComplete3and4, completed);

                tryCompleter4();
                Assert.IsTrue(completed);
            }
        }

        [Test]
        public void PromiseMergeResultsGroupIsResolvedWhenAllPromisesAreCompleted_T_T_T_T_void_void_void(
            [Values] CancelationType cancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values] CompleteType completeType2,
            [Values] bool alreadyComplete2,
            // Reduce number of tests.
            [Values(false)] bool alreadyComplete3Through7,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5,
            [Values(CompleteType.Resolve)] CompleteType completeType6,
            [Values(CompleteType.Resolve)] CompleteType completeType7)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeResultsGroup.New(out _)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeResultsGroup.New(cancelationSource.Token, out _)
                    : PromiseMergeResultsGroup.New(CancelationToken.Canceled(), out _);

                Exception expectedException = new Exception("Bang!");
                int value1 = 42;
                string value2 = "Success";
                bool value3 = true;
                float value4 = 1.0f;

                bool completed = false;

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, value2, expectedException, out var tryCompleter2))
                    .Add(TestHelper.BuildPromise(completeType3, alreadyComplete3Through7, value3, expectedException, out var tryCompleter3))
                    .Add(TestHelper.BuildPromise(completeType4, alreadyComplete3Through7, value4, expectedException, out var tryCompleter4))
                    .Add(TestHelper.BuildPromise(completeType5, alreadyComplete3Through7, expectedException, out var tryCompleter5))
                    .Add(TestHelper.BuildPromise(completeType6, alreadyComplete3Through7, expectedException, out var tryCompleter6))
                    .Add(TestHelper.BuildPromise(completeType7, alreadyComplete3Through7, expectedException, out var tryCompleter7))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;
                        Assert.AreEqual(Promise.State.Resolved, result.State);
                        var (result1, result2, result3, result4, result5, result6, result7) = result.Value;
                        AssertResult(completeType1, result1, value1, expectedException);
                        AssertResult(completeType2, result2, value2, expectedException);
                        AssertResult(completeType3, result3, value3, expectedException);
                        AssertResult(completeType4, result4, value4, expectedException);
                        AssertResult(completeType5, result5, expectedException);
                        AssertResult(completeType6, result6, expectedException);
                        AssertResult(completeType7, result7, expectedException);
                    })
                    .Forget();

                Assert.AreEqual(alreadyComplete1 && alreadyComplete2 && alreadyComplete3Through7, completed);

                tryCompleter1();
                Assert.AreEqual(alreadyComplete2 && alreadyComplete3Through7, completed);

                tryCompleter2();
                Assert.AreEqual(alreadyComplete3Through7, completed);
                tryCompleter3();
                Assert.AreEqual(alreadyComplete3Through7, completed);
                tryCompleter4();
                Assert.AreEqual(alreadyComplete3Through7, completed);
                tryCompleter5();
                Assert.AreEqual(alreadyComplete3Through7, completed);
                tryCompleter6();
                Assert.AreEqual(alreadyComplete3Through7, completed);
                tryCompleter7();

                Assert.IsTrue(completed);
            }
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

        private static CompleteType GetExpectedState(int index, CancelationType cancelationType, (CompleteType completeType, bool alreadyComplete)[] completeValues)
        {
            if (cancelationType == CancelationType.Immediate)
            {
                return CompleteType.Cancel;
            }

            for (int i = 0; i < index; ++i)
            {
                var cv = completeValues[i];
                if (cv.alreadyComplete && cv.completeType != CompleteType.Resolve)
                {
                    return CompleteType.Cancel;
                }
            }

            if (completeValues[index].alreadyComplete)
            {
                return completeValues[index].completeType;
            }

            for (int i = index + 1; i < completeValues.Length; ++i)
            {
                var cv = completeValues[i];
                if (cv.alreadyComplete && cv.completeType != CompleteType.Resolve)
                {
                    return CompleteType.Cancel;
                }
            }

            if (cancelationType == CancelationType.Deferred)
            {
                // Cancelation source is canceled after the first promise is completed.
                if (index == 0)
                {
                    return completeValues[0].completeType;
                }

                return CompleteType.Cancel;
            }

            // CancelationType.None

            for (int i = 0; i < index; ++i)
            {
                if (completeValues[i].completeType != CompleteType.Resolve)
                {
                    return CompleteType.Cancel;
                }
            }

            return completeValues[index].completeType;
        }

        [Test]
        public void PromiseMergeResultsGroupIsResolvedWhenAllPromisesAreCompleted_WithCancelation_void_void(
            [Values] CancelationType cancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeResultsGroup.New(out var groupCancelationToken)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeResultsGroup.New(cancelationSource.Token, out groupCancelationToken)
                    : PromiseMergeResultsGroup.New(CancelationToken.Canceled(), out groupCancelationToken);

                Exception expectedException = new Exception("Bang!");

                bool completed = false;

                var completeValues = new[]
                {
                    (completeType1, alreadyComplete1),
                    (completeType2, alreadyComplete2)
                };

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, expectedException, groupCancelationToken, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, expectedException, groupCancelationToken, out var tryCompleter2))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;
                        Assert.AreEqual(Promise.State.Resolved, result.State);
                        var (result1, result2) = result.Value;
                        AssertResult(GetExpectedState(0, cancelationType, completeValues), result1, expectedException);
                        AssertResult(GetExpectedState(1, cancelationType, completeValues), result2, expectedException);
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
        public void PromiseMergeResultsGroupIsResolvedWhenAllPromisesAreCompleted_WithCancelation_T_T(
            [Values] CancelationType cancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeResultsGroup.New(out var groupCancelationToken)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeResultsGroup.New(cancelationSource.Token, out groupCancelationToken)
                    : PromiseMergeResultsGroup.New(CancelationToken.Canceled(), out groupCancelationToken);

                Exception expectedException = new Exception("Bang!");
                int value1 = 42;
                string value2 = "Success";

                bool completed = false;

                var completeValues = new[]
                {
                    (completeType1, alreadyComplete1),
                    (completeType2, alreadyComplete2)
                };

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, groupCancelationToken, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, value2, expectedException, groupCancelationToken, out var tryCompleter2))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;
                        Assert.AreEqual(Promise.State.Resolved, result.State);
                        var (result1, result2) = result.Value;
                        AssertResult(GetExpectedState(0, cancelationType, completeValues), result1, value1, expectedException);
                        AssertResult(GetExpectedState(1, cancelationType, completeValues), result2, value2, expectedException);
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
        public void PromiseMergeResultsGroupIsResolvedWhenAllPromisesAreCompleted_WithCancelation_void_T_void_T(
            [Values] CancelationType cancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values] CompleteType completeType2,
            [Values] bool alreadyComplete2,
            // Reduce number of tests.
            [Values(false)] bool alreadyComplete3and4,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeResultsGroup.New(out var groupCancelationToken)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeResultsGroup.New(cancelationSource.Token, out groupCancelationToken)
                    : PromiseMergeResultsGroup.New(CancelationToken.Canceled(), out groupCancelationToken);

                Exception expectedException = new Exception("Bang!");
                int value1 = 42;
                string value2 = "Success";

                bool completed = false;

                var completeValues = new[]
                {
                    (completeType1, alreadyComplete1),
                    (completeType2, alreadyComplete2),
                    (completeType3, alreadyComplete3and4),
                    (completeType4, alreadyComplete3and4)
                };

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, expectedException, groupCancelationToken, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, value1, expectedException, groupCancelationToken, out var tryCompleter2))
                    .Add(TestHelper.BuildPromise(completeType3, alreadyComplete3and4, expectedException, groupCancelationToken, out var tryCompleter3))
                    .Add(TestHelper.BuildPromise(completeType4, alreadyComplete3and4, value2, expectedException, groupCancelationToken, out var tryCompleter4))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;
                        Assert.AreEqual(Promise.State.Resolved, result.State);
                        var (result1, result2, result3, result4) = result.Value;
                        AssertResult(GetExpectedState(0, cancelationType, completeValues), result1, expectedException);
                        AssertResult(GetExpectedState(1, cancelationType, completeValues), result2, value1, expectedException);
                        AssertResult(GetExpectedState(2, cancelationType, completeValues), result3, expectedException);
                        AssertResult(GetExpectedState(3, cancelationType, completeValues), result4, value2, expectedException);
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
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(2, completeValues),
                    completed);

                tryCompleter3();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(3, completeValues),
                    completed);

                tryCompleter4();
                Assert.IsTrue(completed);
            }
        }

        [Test]
        public void PromiseMergeResultsGroupIsResolvedWhenAllPromisesAreCompleted_WithCancelation_T_T_T_T_void_void_void(
            [Values] CancelationType cancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values] CompleteType completeType2,
            [Values] bool alreadyComplete2,
            // Reduce number of tests.
            [Values(false)] bool alreadyComplete3Through7,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5,
            [Values(CompleteType.Resolve)] CompleteType completeType6,
            [Values(CompleteType.Resolve)] CompleteType completeType7)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeResultsGroup.New(out var groupCancelationToken)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeResultsGroup.New(cancelationSource.Token, out groupCancelationToken)
                    : PromiseMergeResultsGroup.New(CancelationToken.Canceled(), out groupCancelationToken);

                Exception expectedException = new Exception("Bang!");
                int value1 = 42;
                string value2 = "Success";
                bool value3 = true;
                float value4 = 1.0f;

                bool completed = false;

                var completeValues = new[]
                {
                    (completeType1, alreadyComplete1),
                    (completeType2, alreadyComplete2),
                    (completeType3, alreadyComplete3Through7),
                    (completeType4, alreadyComplete3Through7),
                    (completeType5, alreadyComplete3Through7),
                    (completeType6, alreadyComplete3Through7),
                    (completeType7, alreadyComplete3Through7)
                };

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, groupCancelationToken, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, value2, expectedException, groupCancelationToken, out var tryCompleter2))
                    .Add(TestHelper.BuildPromise(completeType3, alreadyComplete3Through7, value3, expectedException, groupCancelationToken, out var tryCompleter3))
                    .Add(TestHelper.BuildPromise(completeType4, alreadyComplete3Through7, value4, expectedException, groupCancelationToken, out var tryCompleter4))
                    .Add(TestHelper.BuildPromise(completeType5, alreadyComplete3Through7, expectedException, groupCancelationToken, out var tryCompleter5))
                    .Add(TestHelper.BuildPromise(completeType6, alreadyComplete3Through7, expectedException, groupCancelationToken, out var tryCompleter6))
                    .Add(TestHelper.BuildPromise(completeType7, alreadyComplete3Through7, expectedException, groupCancelationToken, out var tryCompleter7))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;
                        Assert.AreEqual(Promise.State.Resolved, result.State);
                        var (result1, result2, result3, result4, result5, result6, result7) = result.Value;
                        AssertResult(GetExpectedState(0, cancelationType, completeValues), result1, value1, expectedException);
                        AssertResult(GetExpectedState(1, cancelationType, completeValues), result2, value2, expectedException);
                        AssertResult(GetExpectedState(2, cancelationType, completeValues), result3, value3, expectedException);
                        AssertResult(GetExpectedState(3, cancelationType, completeValues), result4, value4, expectedException);
                        AssertResult(GetExpectedState(4, cancelationType, completeValues), result5, expectedException);
                        AssertResult(GetExpectedState(5, cancelationType, completeValues), result6, expectedException);
                        AssertResult(GetExpectedState(6, cancelationType, completeValues), result7, expectedException);
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
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(2, completeValues),
                    completed);

                tryCompleter3();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(3, completeValues),
                    completed);

                tryCompleter4();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(4, completeValues),
                    completed);

                tryCompleter5();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(5, completeValues),
                    completed);

                tryCompleter6();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(6, completeValues),
                    completed);

                tryCompleter7();
                Assert.IsTrue(completed);
            }
        }

        [Test]
        public void PromiseMergeResultsGroup_CancelationCallbackExceptionsArePropagated_2(
            [Values(CancelationType.None, CancelationType.Deferred)] CancelationType cancelationType,
            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeResultsGroup.New(out var groupCancelationToken)
                    : PromiseMergeResultsGroup.New(cancelationSource.Token, out groupCancelationToken);

                Exception expectedException = new Exception("Error in cancelation!");
                groupCancelationToken.Register(() => { throw expectedException; });

                bool completed = false;

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, new System.InvalidOperationException("Bang!"), out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, new System.InvalidOperationException("Bang!"), out var tryCompleter2))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;
                        Assert.AreEqual(Promise.State.Rejected, result.State);
                        Assert.IsAssignableFrom<AggregateException>(result.Reason);

                        var e = result.Reason.UnsafeAs<AggregateException>();
                        Assert.AreEqual(1, e.InnerExceptions.Count);
                        Assert.IsInstanceOf<AggregateException>(e.InnerExceptions[0]);
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

        // Using PromiseMergeResultsGroupExtended in IL2CPP causes the build to fail.
#if !ENABLE_IL2CPP
        [Test]
        public void PromiseMergeResultsGroupIsResolvedWhenAllPromisesAreCompleted_T_void_T_void_T_T_void_void(
            [Values] CancelationType cancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            // Reduce number of tests.
            [Values(false)] bool alreadyComplete2Through7,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5,
            [Values(CompleteType.Resolve)] CompleteType completeType6,
            [Values(CompleteType.Resolve)] CompleteType completeType7,
            [Values] CompleteType completeType8,
            [Values] bool alreadyComplete8)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeResultsGroup.New(out _)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeResultsGroup.New(cancelationSource.Token, out _)
                    : PromiseMergeResultsGroup.New(CancelationToken.Canceled(), out _);

                Exception expectedException = new Exception("Bang!");
                int value1 = 42;
                string value3 = "Success";
                bool value5 = true;
                float value6 = 1.0f;

                bool completed = false;

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2Through7, expectedException, out var tryCompleter2))
                    .Add(TestHelper.BuildPromise(completeType3, alreadyComplete2Through7, value3, expectedException, out var tryCompleter3))
                    .Add(TestHelper.BuildPromise(completeType4, alreadyComplete2Through7, expectedException, out var tryCompleter4))
                    .Add(TestHelper.BuildPromise(completeType5, alreadyComplete2Through7, value5, expectedException, out var tryCompleter5))
                    .Add(TestHelper.BuildPromise(completeType6, alreadyComplete2Through7, value6, expectedException, out var tryCompleter6))
                    .Add(TestHelper.BuildPromise(completeType7, alreadyComplete2Through7, expectedException, out var tryCompleter7))
                    .Add(TestHelper.BuildPromise(completeType8, alreadyComplete8, expectedException, out var tryCompleter8))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;
                        Assert.AreEqual(Promise.State.Resolved, result.State);
                        var ((result1, result2, result3, result4, result5, result6, result7), result8) = result.Value;
                        AssertResult(completeType1, result1, value1, expectedException);
                        AssertResult(completeType2, result2, expectedException);
                        AssertResult(completeType3, result3, value3, expectedException);
                        AssertResult(completeType4, result4, expectedException);
                        AssertResult(completeType5, result5, value5, expectedException);
                        AssertResult(completeType6, result6, value6, expectedException);
                        AssertResult(completeType7, result7, expectedException);
                        AssertResult(completeType8, result8, expectedException);
                    })
                    .Forget();

                Assert.AreEqual(alreadyComplete1 && alreadyComplete2Through7 && alreadyComplete8, completed);

                tryCompleter1();
                Assert.AreEqual(alreadyComplete2Through7 && alreadyComplete8, completed);

                tryCompleter2();
                Assert.AreEqual(alreadyComplete2Through7 && alreadyComplete8, completed);
                tryCompleter3();
                Assert.AreEqual(alreadyComplete2Through7 && alreadyComplete8, completed);
                tryCompleter4();
                Assert.AreEqual(alreadyComplete2Through7 && alreadyComplete8, completed);
                tryCompleter5();
                Assert.AreEqual(alreadyComplete2Through7 && alreadyComplete8, completed);
                tryCompleter6();
                Assert.AreEqual(alreadyComplete2Through7 && alreadyComplete8, completed);

                tryCompleter7();
                Assert.AreEqual(alreadyComplete8, completed);

                tryCompleter8();
                Assert.IsTrue(completed);
            }
        }

        [Test]
        public void PromiseMergeResultsGroupIsResolvedWhenAllPromisesAreCompleted_WithCancelation_T_void_T_void_T_T_void_T(
            [Values] CancelationType cancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            // Reduce number of tests.
            [Values(false)] bool alreadyComplete2Through7,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5,
            [Values(CompleteType.Resolve)] CompleteType completeType6,
            [Values(CompleteType.Resolve)] CompleteType completeType7,
            [Values] CompleteType completeType8,
            [Values] bool alreadyComplete8)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeResultsGroup.New(out var groupCancelationToken)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeResultsGroup.New(cancelationSource.Token, out groupCancelationToken)
                    : PromiseMergeResultsGroup.New(CancelationToken.Canceled(), out groupCancelationToken);

                Exception expectedException = new Exception("Bang!");
                int value1 = 42;
                string value3 = "Success";
                bool value5 = true;
                float value6 = 1.0f;
                TimeSpan value8 = TimeSpan.FromSeconds(2);

                bool completed = false;

                var completeValues = new[]
                {
                    (completeType1, alreadyComplete1),
                    (completeType2, alreadyComplete2Through7),
                    (completeType3, alreadyComplete2Through7),
                    (completeType4, alreadyComplete2Through7),
                    (completeType5, alreadyComplete2Through7),
                    (completeType6, alreadyComplete2Through7),
                    (completeType7, alreadyComplete2Through7),
                    (completeType8, alreadyComplete8)
                };

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, groupCancelationToken, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2Through7, expectedException, groupCancelationToken, out var tryCompleter2))
                    .Add(TestHelper.BuildPromise(completeType3, alreadyComplete2Through7, value3, expectedException, groupCancelationToken, out var tryCompleter3))
                    .Add(TestHelper.BuildPromise(completeType4, alreadyComplete2Through7, expectedException, groupCancelationToken, out var tryCompleter4))
                    .Add(TestHelper.BuildPromise(completeType5, alreadyComplete2Through7, value5, expectedException, groupCancelationToken, out var tryCompleter5))
                    .Add(TestHelper.BuildPromise(completeType6, alreadyComplete2Through7, value6, expectedException, groupCancelationToken, out var tryCompleter6))
                    .Add(TestHelper.BuildPromise(completeType7, alreadyComplete2Through7, expectedException, groupCancelationToken, out var tryCompleter7))
                    .Add(TestHelper.BuildPromise(completeType8, alreadyComplete8, value8, expectedException, groupCancelationToken, out var tryCompleter8))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;
                        Assert.AreEqual(Promise.State.Resolved, result.State);
                        var ((result1, result2, result3, result4, result5, result6, result7), result8) = result.Value;
                        AssertResult(GetExpectedState(0, cancelationType, completeValues), result1, value1, expectedException);
                        AssertResult(GetExpectedState(1, cancelationType, completeValues), result2, expectedException);
                        AssertResult(GetExpectedState(2, cancelationType, completeValues), result3, value3, expectedException);
                        AssertResult(GetExpectedState(3, cancelationType, completeValues), result4, expectedException);
                        AssertResult(GetExpectedState(4, cancelationType, completeValues), result5, value5, expectedException);
                        AssertResult(GetExpectedState(5, cancelationType, completeValues), result6, value6, expectedException);
                        AssertResult(GetExpectedState(6, cancelationType, completeValues), result7, expectedException);
                        AssertResult(GetExpectedState(7, cancelationType, completeValues), result8, value8, expectedException);
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
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(2, completeValues),
                    completed);

                tryCompleter3();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(3, completeValues),
                    completed);

                tryCompleter4();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(4, completeValues),
                    completed);

                tryCompleter5();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(5, completeValues),
                    completed);

                tryCompleter6();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(6, completeValues),
                    completed);

                tryCompleter7();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(7, completeValues),
                    completed);

                tryCompleter8();
                Assert.IsTrue(completed);
            }
        }

        [Test]
        public void PromiseMergeResultsGroupIsResolvedWhenAllPromisesAreCompleted_WithCancelation_14(
            [Values] CancelationType cancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            // Reduce number of tests.
            [Values(false)] bool alreadyComplete2Through13,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5,
            [Values(CompleteType.Resolve)] CompleteType completeType6,
            [Values(CompleteType.Resolve)] CompleteType completeType7,
            [Values(CompleteType.Resolve)] CompleteType completeType8,
            [Values(CompleteType.Resolve)] CompleteType completeType9,
            [Values(CompleteType.Resolve)] CompleteType completeType10,
            [Values(CompleteType.Resolve)] CompleteType completeType11,
            [Values(CompleteType.Resolve)] CompleteType completeType12,
            [Values(CompleteType.Resolve)] CompleteType completeType13,
            [Values] CompleteType completeType14,
            [Values] bool alreadyComplete14)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeResultsGroup.New(out var groupCancelationToken)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeResultsGroup.New(cancelationSource.Token, out groupCancelationToken)
                    : PromiseMergeResultsGroup.New(CancelationToken.Canceled(), out groupCancelationToken);

                Exception expectedException = new Exception("Bang!");
                int value1 = 42;
                string value3 = "Success";
                bool value5 = true;
                float value6 = 1.0f;
                TimeSpan value8 = TimeSpan.FromSeconds(2);
                long value14 = long.MaxValue;

                bool completed = false;

                var completeValues = new[]
                {
                    (completeType1, alreadyComplete1),
                    (completeType2, alreadyComplete2Through13),
                    (completeType3, alreadyComplete2Through13),
                    (completeType4, alreadyComplete2Through13),
                    (completeType5, alreadyComplete2Through13),
                    (completeType6, alreadyComplete2Through13),
                    (completeType7, alreadyComplete2Through13),
                    (completeType8, alreadyComplete2Through13),
                    (completeType9, alreadyComplete2Through13),
                    (completeType10, alreadyComplete2Through13),
                    (completeType11, alreadyComplete2Through13),
                    (completeType12, alreadyComplete2Through13),
                    (completeType13, alreadyComplete2Through13),
                    (completeType14, alreadyComplete14),
                };

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, groupCancelationToken, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2Through13, expectedException, groupCancelationToken, out var tryCompleter2))
                    .Add(TestHelper.BuildPromise(completeType3, alreadyComplete2Through13, value3, expectedException, groupCancelationToken, out var tryCompleter3))
                    .Add(TestHelper.BuildPromise(completeType4, alreadyComplete2Through13, expectedException, groupCancelationToken, out var tryCompleter4))
                    .Add(TestHelper.BuildPromise(completeType5, alreadyComplete2Through13, value5, expectedException, groupCancelationToken, out var tryCompleter5))
                    .Add(TestHelper.BuildPromise(completeType6, alreadyComplete2Through13, value6, expectedException, groupCancelationToken, out var tryCompleter6))
                    .Add(TestHelper.BuildPromise(completeType7, alreadyComplete2Through13, expectedException, groupCancelationToken, out var tryCompleter7))
                    .Add(TestHelper.BuildPromise(completeType8, alreadyComplete2Through13, value8, expectedException, groupCancelationToken, out var tryCompleter8))
                    .Add(TestHelper.BuildPromise(completeType9, alreadyComplete2Through13, expectedException, groupCancelationToken, out var tryCompleter9))
                    .Add(TestHelper.BuildPromise(completeType10, alreadyComplete2Through13, expectedException, groupCancelationToken, out var tryCompleter10))
                    .Add(TestHelper.BuildPromise(completeType11, alreadyComplete2Through13, expectedException, groupCancelationToken, out var tryCompleter11))
                    .Add(TestHelper.BuildPromise(completeType12, alreadyComplete2Through13, expectedException, groupCancelationToken, out var tryCompleter12))
                    .Add(TestHelper.BuildPromise(completeType13, alreadyComplete2Through13, expectedException, groupCancelationToken, out var tryCompleter13))
                    .Add(TestHelper.BuildPromise(completeType14, alreadyComplete14, value14, expectedException, groupCancelationToken, out var tryCompleter14))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;
                        Assert.AreEqual(Promise.State.Resolved, result.State);
                        var (((result1, result2, result3, result4, result5, result6, result7), result8, result9, result10, result11, result12, result13), result14) = result.Value;
                        AssertResult(GetExpectedState(0, cancelationType, completeValues), result1, value1, expectedException);
                        AssertResult(GetExpectedState(1, cancelationType, completeValues), result2, expectedException);
                        AssertResult(GetExpectedState(2, cancelationType, completeValues), result3, value3, expectedException);
                        AssertResult(GetExpectedState(3, cancelationType, completeValues), result4, expectedException);
                        AssertResult(GetExpectedState(4, cancelationType, completeValues), result5, value5, expectedException);
                        AssertResult(GetExpectedState(5, cancelationType, completeValues), result6, value6, expectedException);
                        AssertResult(GetExpectedState(6, cancelationType, completeValues), result7, expectedException);
                        AssertResult(GetExpectedState(7, cancelationType, completeValues), result8, value8, expectedException);
                        AssertResult(GetExpectedState(8, cancelationType, completeValues), result9, expectedException);
                        AssertResult(GetExpectedState(9, cancelationType, completeValues), result10, expectedException);
                        AssertResult(GetExpectedState(10, cancelationType, completeValues), result11, expectedException);
                        AssertResult(GetExpectedState(11, cancelationType, completeValues), result12, expectedException);
                        AssertResult(GetExpectedState(12, cancelationType, completeValues), result13, expectedException);
                        AssertResult(GetExpectedState(13, cancelationType, completeValues), result14, value14, expectedException);
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
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(2, completeValues),
                    completed);

                tryCompleter3();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(3, completeValues),
                    completed);

                tryCompleter4();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(4, completeValues),
                    completed);

                tryCompleter5();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(5, completeValues),
                    completed);

                tryCompleter6();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(6, completeValues),
                    completed);

                tryCompleter7();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(7, completeValues),
                    completed);

                tryCompleter8();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(8, completeValues),
                    completed);

                tryCompleter9();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(9, completeValues),
                    completed);

                tryCompleter10();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(10, completeValues),
                    completed);

                tryCompleter11();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(11, completeValues),
                    completed);

                tryCompleter12();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(12, completeValues),
                    completed);

                tryCompleter13();
                Assert.AreEqual(cancelationType == CancelationType.Immediate
                    || cancelationType == CancelationType.Deferred
                    || GetShouldBeComplete(13, completeValues),
                    completed);

                tryCompleter14();
                Assert.IsTrue(completed);
            }
        }

        [Test]
        public void PromiseMergeResultsGroup_CancelationCallbackExceptionsArePropagated_8(
            [Values(CancelationType.None, CancelationType.Deferred)] CancelationType cancelationType,
            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            // Reduce number of tests.
            [Values(false)] bool alreadyComplete2Through7,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5,
            [Values(CompleteType.Resolve)] CompleteType completeType6,
            [Values(CompleteType.Resolve)] CompleteType completeType7,
            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType8,
            [Values] bool alreadyComplete8)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeResultsGroup.New(out var groupCancelationToken)
                    : PromiseMergeResultsGroup.New(cancelationSource.Token, out groupCancelationToken);

                Exception expectedException = new Exception("Error in cancelation!");
                groupCancelationToken.Register(() => { throw expectedException; });

                bool completed = false;

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, new System.InvalidOperationException("Bang!"), out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2Through7, new System.InvalidOperationException("Bang!"), out var tryCompleter2))
                    .Add(TestHelper.BuildPromise(completeType3, alreadyComplete2Through7, new System.InvalidOperationException("Bang!"), out var tryCompleter3))
                    .Add(TestHelper.BuildPromise(completeType4, alreadyComplete2Through7, new System.InvalidOperationException("Bang!"), out var tryCompleter4))
                    .Add(TestHelper.BuildPromise(completeType5, alreadyComplete2Through7, new System.InvalidOperationException("Bang!"), out var tryCompleter5))
                    .Add(TestHelper.BuildPromise(completeType6, alreadyComplete2Through7, new System.InvalidOperationException("Bang!"), out var tryCompleter6))
                    .Add(TestHelper.BuildPromise(completeType7, alreadyComplete2Through7, new System.InvalidOperationException("Bang!"), out var tryCompleter7))
                    .Add(TestHelper.BuildPromise(completeType8, alreadyComplete8, new System.InvalidOperationException("Bang!"), out var tryCompleter8))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;
                        Assert.AreEqual(Promise.State.Rejected, result.State);
                        Assert.IsAssignableFrom<AggregateException>(result.Reason);

                        var e = result.Reason.UnsafeAs<AggregateException>();
                        Assert.AreEqual(1, e.InnerExceptions.Count);
                        Assert.IsInstanceOf<AggregateException>(e.InnerExceptions[0]);
                        Assert.AreEqual(1, e.InnerExceptions[0].UnsafeAs<AggregateException>().InnerExceptions.Count);
                        Assert.AreEqual(expectedException, e.InnerExceptions[0].UnsafeAs<AggregateException>().InnerExceptions[0]);
                    })
                    .Forget();

                Assert.AreEqual(alreadyComplete1 && alreadyComplete2Through7 && alreadyComplete8, completed);

                tryCompleter1();
                Assert.AreEqual(alreadyComplete2Through7 && alreadyComplete8, completed);

                tryCompleter2();
                Assert.AreEqual(alreadyComplete2Through7 && alreadyComplete8, completed);
                tryCompleter3();
                Assert.AreEqual(alreadyComplete2Through7 && alreadyComplete8, completed);
                tryCompleter4();
                Assert.AreEqual(alreadyComplete2Through7 && alreadyComplete8, completed);
                tryCompleter5();
                Assert.AreEqual(alreadyComplete2Through7 && alreadyComplete8, completed);
                tryCompleter6();
                Assert.AreEqual(alreadyComplete2Through7 && alreadyComplete8, completed);

                tryCompleter7();
                Assert.AreEqual(alreadyComplete8, completed);

                tryCompleter8();
                Assert.IsTrue(completed);
            }
        }

        [Test]
        public void PromiseMergeResultsGroup_CancelationCallbackExceptionsArePropagated_14(
            [Values(CancelationType.None, CancelationType.Deferred)] CancelationType cancelationType,
            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            // Reduce number of tests.
            [Values(false)] bool alreadyComplete2Through13,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5,
            [Values(CompleteType.Resolve)] CompleteType completeType6,
            [Values(CompleteType.Resolve)] CompleteType completeType7,
            [Values(CompleteType.Resolve)] CompleteType completeType8,
            [Values(CompleteType.Resolve)] CompleteType completeType9,
            [Values(CompleteType.Resolve)] CompleteType completeType10,
            [Values(CompleteType.Resolve)] CompleteType completeType11,
            [Values(CompleteType.Resolve)] CompleteType completeType12,
            [Values(CompleteType.Resolve)] CompleteType completeType13,
            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType14,
            [Values] bool alreadyComplete14)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeResultsGroup.New(out var groupCancelationToken)
                    : PromiseMergeResultsGroup.New(cancelationSource.Token, out groupCancelationToken);

                Exception expectedException = new Exception("Error in cancelation!");
                groupCancelationToken.Register(() => { throw expectedException; });

                bool completed = false;

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, new System.InvalidOperationException("Bang!"), out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2Through13, new System.InvalidOperationException("Bang!"), out var tryCompleter2))
                    .Add(TestHelper.BuildPromise(completeType3, alreadyComplete2Through13, new System.InvalidOperationException("Bang!"), out var tryCompleter3))
                    .Add(TestHelper.BuildPromise(completeType4, alreadyComplete2Through13, new System.InvalidOperationException("Bang!"), out var tryCompleter4))
                    .Add(TestHelper.BuildPromise(completeType5, alreadyComplete2Through13, new System.InvalidOperationException("Bang!"), out var tryCompleter5))
                    .Add(TestHelper.BuildPromise(completeType6, alreadyComplete2Through13, new System.InvalidOperationException("Bang!"), out var tryCompleter6))
                    .Add(TestHelper.BuildPromise(completeType7, alreadyComplete2Through13, new System.InvalidOperationException("Bang!"), out var tryCompleter7))
                    .Add(TestHelper.BuildPromise(completeType8, alreadyComplete2Through13, new System.InvalidOperationException("Bang!"), out var tryCompleter8))
                    .Add(TestHelper.BuildPromise(completeType9, alreadyComplete2Through13, new System.InvalidOperationException("Bang!"), out var tryCompleter9))
                    .Add(TestHelper.BuildPromise(completeType10, alreadyComplete2Through13, new System.InvalidOperationException("Bang!"), out var tryCompleter10))
                    .Add(TestHelper.BuildPromise(completeType11, alreadyComplete2Through13, new System.InvalidOperationException("Bang!"), out var tryCompleter11))
                    .Add(TestHelper.BuildPromise(completeType12, alreadyComplete2Through13, new System.InvalidOperationException("Bang!"), out var tryCompleter12))
                    .Add(TestHelper.BuildPromise(completeType13, alreadyComplete2Through13, new System.InvalidOperationException("Bang!"), out var tryCompleter13))
                    .Add(TestHelper.BuildPromise(completeType14, alreadyComplete14, new System.InvalidOperationException("Bang!"), out var tryCompleter14))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;
                        Assert.AreEqual(Promise.State.Rejected, result.State);
                        Assert.IsAssignableFrom<AggregateException>(result.Reason);

                        var e = result.Reason.UnsafeAs<AggregateException>();
                        Assert.AreEqual(1, e.InnerExceptions.Count);
                        Assert.IsInstanceOf<AggregateException>(e.InnerExceptions[0]);
                        Assert.AreEqual(1, e.InnerExceptions[0].UnsafeAs<AggregateException>().InnerExceptions.Count);
                        Assert.AreEqual(expectedException, e.InnerExceptions[0].UnsafeAs<AggregateException>().InnerExceptions[0]);
                    })
                    .Forget();

                Assert.AreEqual(alreadyComplete1 && alreadyComplete2Through13 && alreadyComplete14, completed);

                tryCompleter1();
                Assert.AreEqual(alreadyComplete2Through13 && alreadyComplete14, completed);

                tryCompleter2();
                Assert.AreEqual(alreadyComplete2Through13 && alreadyComplete14, completed);
                tryCompleter3();
                Assert.AreEqual(alreadyComplete2Through13 && alreadyComplete14, completed);
                tryCompleter4();
                Assert.AreEqual(alreadyComplete2Through13 && alreadyComplete14, completed);
                tryCompleter5();
                Assert.AreEqual(alreadyComplete2Through13 && alreadyComplete14, completed);
                tryCompleter6();
                Assert.AreEqual(alreadyComplete2Through13 && alreadyComplete14, completed);
                tryCompleter7();
                Assert.AreEqual(alreadyComplete2Through13 && alreadyComplete14, completed);
                tryCompleter8();
                Assert.AreEqual(alreadyComplete2Through13 && alreadyComplete14, completed);
                tryCompleter9();
                Assert.AreEqual(alreadyComplete2Through13 && alreadyComplete14, completed);
                tryCompleter10();
                Assert.AreEqual(alreadyComplete2Through13 && alreadyComplete14, completed);
                tryCompleter11();
                Assert.AreEqual(alreadyComplete2Through13 && alreadyComplete14, completed);
                tryCompleter12();
                Assert.AreEqual(alreadyComplete2Through13 && alreadyComplete14, completed);

                tryCompleter13();
                Assert.AreEqual(alreadyComplete14, completed);

                tryCompleter14();
                Assert.IsTrue(completed);
            }
        }
#endif
    }
}