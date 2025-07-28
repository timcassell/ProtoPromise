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

namespace ProtoPromise.Tests.APIs.PromiseGroups
{
    public class PromiseMergeGroupTests
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
        public void PromiseMergeGroup_UsingInvalidatedGroupThrows(
            [Values] CancelationType cancelationType)
        {
            var voidPromise = Promise.Resolved();
            var intPromise = Promise.Resolved(42);
            Assert.Catch<System.InvalidOperationException>(() => default(PromiseMergeGroup).Add(voidPromise));
            Assert.Catch<System.InvalidOperationException>(() => default(PromiseMergeGroup).Add(intPromise));
            Assert.Catch<System.InvalidOperationException>(() => default(PromiseMergeGroup).WaitAsync());

            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup1 = cancelationType == CancelationType.None ? PromiseMergeGroup.New(out _)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeGroup.New(cancelationSource.Token, out _)
                    : PromiseMergeGroup.New(CancelationToken.Canceled(), out _);

                var mergeGroup2 = mergeGroup1.Add(Promise.Resolved());
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup1.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup1.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup1.WaitAsync());

                var mergeGroup3 = mergeGroup2.Add(Promise.Resolved(2));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup2.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup2.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup2.WaitAsync());

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

                mergeGroup11.WaitAsync().Forget();
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup11.Add(voidPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup11.Add(intPromise));
                Assert.Catch<System.InvalidOperationException>(() => mergeGroup11.WaitAsync());

                voidPromise.Forget();
                intPromise.Forget();
            }
        }

        [Test]
        public void PromiseMergeGroupIsResolvedWhenNoPromisesAreAdded(
            [Values] CancelationType cancelationType)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                bool resolved = false;

                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeGroup.New(out _)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeGroup.New(cancelationSource.Token, out _)
                    : PromiseMergeGroup.New(CancelationToken.Canceled(), out _);
                mergeGroup
                    .WaitAsync()
                    .Then(() => resolved = true)
                    .Forget();

                Assert.True(resolved);
            }
        }

        [Test]
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_1_0(
            [Values] CancelationType cancelationType,
            [Values] CompleteType completeType,
            [Values] bool alreadyComplete)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeGroup.New(out _)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeGroup.New(cancelationSource.Token, out _)
                    : PromiseMergeGroup.New(CancelationToken.Canceled(), out _);

                Exception expectedException = new Exception("Bang!");

                Promise.State expectedState = cancelationType != CancelationType.Immediate ? (Promise.State) completeType
                    : completeType == CompleteType.Reject ? Promise.State.Rejected
                    : Promise.State.Canceled;

                bool completed = false;

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType, alreadyComplete, expectedException, out var tryCompleter))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;

                        Assert.AreEqual(expectedState, result.State);
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
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_2_0(
            [Values] CancelationType cancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeGroup.New(out _)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeGroup.New(cancelationSource.Token, out _)
                    : PromiseMergeGroup.New(CancelationToken.Canceled(), out _);

                Exception expectedException = new Exception("Bang!");

                Promise.State expectedPromiseState =
                    completeType1 == CompleteType.Reject || completeType2 == CompleteType.Reject
                        ? Promise.State.Rejected
                    : completeType1 == CompleteType.Cancel || completeType2 == CompleteType.Cancel
                        ? Promise.State.Canceled
                    : Promise.State.Resolved;
                Promise.State expectedFinalState = cancelationType != CancelationType.Immediate ? expectedPromiseState
                    : expectedPromiseState == Promise.State.Rejected ? Promise.State.Rejected
                    : Promise.State.Canceled;

                bool completed = false;

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, expectedException, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, expectedException, out var tryCompleter2))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;

                        Assert.AreEqual(expectedFinalState, result.State);
                        if (expectedFinalState == Promise.State.Rejected)
                        {
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
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

        [Test]
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_0_2(
            [Values] CancelationType cancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeGroup.New(out _)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeGroup.New(cancelationSource.Token, out _)
                    : PromiseMergeGroup.New(CancelationToken.Canceled(), out _);

                Exception expectedException = new Exception("Bang!");
                int value1 = 42;
                string value2 = "Success";

                Promise.State expectedPromiseState = completeType1 == CompleteType.Reject || completeType2 == CompleteType.Reject ? Promise.State.Rejected
                    : completeType1 == CompleteType.Cancel || completeType2 == CompleteType.Cancel ? Promise.State.Canceled
                    : Promise.State.Resolved;
                Promise.State expectedFinalState = cancelationType != CancelationType.Immediate ? expectedPromiseState
                    : expectedPromiseState == Promise.State.Rejected ? Promise.State.Rejected
                    : Promise.State.Canceled;

                bool completed = false;

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, value2, expectedException, out var tryCompleter2))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;

                        Assert.AreEqual(expectedFinalState, result.State);
                        if (expectedFinalState == Promise.State.Resolved)
                        {
                            Assert.AreEqual((value1, value2), result.Value);
                        }
                        else if (expectedFinalState == Promise.State.Rejected)
                        {
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
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

        [Test]
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_2_2(
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
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeGroup.New(out _)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeGroup.New(cancelationSource.Token, out _)
                    : PromiseMergeGroup.New(CancelationToken.Canceled(), out _);

                Exception expectedException = new Exception("Bang!");
                int value1 = 42;
                string value2 = "Success";

                Promise.State expectedPromiseState =
                    completeType1 == CompleteType.Reject || completeType2 == CompleteType.Reject || completeType3 == CompleteType.Reject || completeType4 == CompleteType.Reject
                        ? Promise.State.Rejected
                    : completeType1 == CompleteType.Cancel || completeType2 == CompleteType.Cancel || completeType3 == CompleteType.Cancel || completeType4 == CompleteType.Cancel
                        ? Promise.State.Canceled
                    : Promise.State.Resolved;
                Promise.State expectedFinalState = cancelationType != CancelationType.Immediate ? expectedPromiseState
                    : expectedPromiseState == Promise.State.Rejected ? Promise.State.Rejected
                    : Promise.State.Canceled;

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

                        Assert.AreEqual(expectedFinalState, result.State);
                        if (expectedFinalState == Promise.State.Resolved)
                        {
                            Assert.AreEqual((value1, value2), result.Value);
                        }
                        else if (expectedFinalState == Promise.State.Rejected)
                        {
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
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
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_1_3(
            [Values] CancelationType cancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values] bool alreadyComplete2,
            // Reduce number of tests.
            [Values(false)] bool alreadyComplete3and4,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values] CompleteType completeType4)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeGroup.New(out _)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeGroup.New(cancelationSource.Token, out _)
                    : PromiseMergeGroup.New(CancelationToken.Canceled(), out _);

                Exception expectedException = new Exception("Bang!");
                int value1 = 42;
                string value2 = "Success";
                bool value3 = true;

                Promise.State expectedPromiseState =
                    completeType1 == CompleteType.Reject || completeType2 == CompleteType.Reject || completeType3 == CompleteType.Reject || completeType4 == CompleteType.Reject
                        ? Promise.State.Rejected
                    : completeType1 == CompleteType.Cancel || completeType2 == CompleteType.Cancel || completeType3 == CompleteType.Cancel || completeType4 == CompleteType.Cancel
                        ? Promise.State.Canceled
                    : Promise.State.Resolved;
                Promise.State expectedFinalState = cancelationType != CancelationType.Immediate ? expectedPromiseState
                    : expectedPromiseState == Promise.State.Rejected ? Promise.State.Rejected
                    : Promise.State.Canceled;

                bool completed = false;

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, expectedException, out var tryCompleter2))
                    .Add(TestHelper.BuildPromise(completeType3, alreadyComplete3and4, value2, expectedException, out var tryCompleter3))
                    .Add(TestHelper.BuildPromise(completeType4, alreadyComplete3and4, value3, expectedException, out var tryCompleter4))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;

                        Assert.AreEqual(expectedFinalState, result.State);
                        if (expectedFinalState == Promise.State.Resolved)
                        {
                            Assert.AreEqual((value1, value2, value3), result.Value);
                        }
                        else if (expectedFinalState == Promise.State.Rejected)
                        {
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
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
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_0_7(
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
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeGroup.New(out _)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeGroup.New(cancelationSource.Token, out _)
                    : PromiseMergeGroup.New(CancelationToken.Canceled(), out _);

                Exception expectedException = new Exception("Bang!");
                int value1 = 42;
                string value2 = "Success";
                bool value3 = true;
                float value4 = 1.0f;
                ulong value5 = ulong.MaxValue;
                TimeSpan value6 = TimeSpan.FromSeconds(2);
                Promise.State value7 = Promise.State.Canceled;

                Promise.State expectedPromiseState =
                    completeType1 == CompleteType.Reject || completeType2 == CompleteType.Reject || completeType3 == CompleteType.Reject || completeType4 == CompleteType.Reject
                    || completeType5 == CompleteType.Reject || completeType6 == CompleteType.Reject || completeType7 == CompleteType.Reject
                        ? Promise.State.Rejected
                    : completeType1 == CompleteType.Cancel || completeType2 == CompleteType.Cancel || completeType3 == CompleteType.Cancel || completeType4 == CompleteType.Cancel
                    || completeType5 == CompleteType.Cancel || completeType6 == CompleteType.Cancel || completeType7 == CompleteType.Cancel
                        ? Promise.State.Canceled
                    : Promise.State.Resolved;
                Promise.State expectedFinalState = cancelationType != CancelationType.Immediate ? expectedPromiseState
                    : expectedPromiseState == Promise.State.Rejected ? Promise.State.Rejected
                    : Promise.State.Canceled;

                bool completed = false;

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, value2, expectedException, out var tryCompleter2))
                    .Add(TestHelper.BuildPromise(completeType3, alreadyComplete3Through7, value3, expectedException, out var tryCompleter3))
                    .Add(TestHelper.BuildPromise(completeType4, alreadyComplete3Through7, value4, expectedException, out var tryCompleter4))
                    .Add(TestHelper.BuildPromise(completeType5, alreadyComplete3Through7, value5, expectedException, out var tryCompleter5))
                    .Add(TestHelper.BuildPromise(completeType6, alreadyComplete3Through7, value6, expectedException, out var tryCompleter6))
                    .Add(TestHelper.BuildPromise(completeType7, alreadyComplete3Through7, value7, expectedException, out var tryCompleter7))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;

                        Assert.AreEqual(expectedFinalState, result.State);
                        if (expectedFinalState == Promise.State.Resolved)
                        {
                            Assert.AreEqual((value1, value2, value3, value4, value5, value6, value7), result.Value);
                        }
                        else if (expectedFinalState == Promise.State.Rejected)
                        {
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
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

        [Test]
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_0_8(
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
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeGroup.New(out _)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeGroup.New(cancelationSource.Token, out _)
                    : PromiseMergeGroup.New(CancelationToken.Canceled(), out _);

                Exception expectedException = new Exception("Bang!");
                int value1 = 42;
                string value2 = "Success";
                bool value3 = true;
                float value4 = 1.0f;
                ulong value5 = ulong.MaxValue;
                TimeSpan value6 = TimeSpan.FromSeconds(2);
                Promise.State value7 = Promise.State.Canceled;
                int value8 = 2;

                Promise.State expectedPromiseState =
                    completeType1 == CompleteType.Reject || completeType2 == CompleteType.Reject || completeType3 == CompleteType.Reject || completeType4 == CompleteType.Reject
                    || completeType5 == CompleteType.Reject || completeType6 == CompleteType.Reject || completeType7 == CompleteType.Reject || completeType8 == CompleteType.Reject
                        ? Promise.State.Rejected
                    : completeType1 == CompleteType.Cancel || completeType2 == CompleteType.Cancel || completeType3 == CompleteType.Cancel || completeType4 == CompleteType.Cancel
                    || completeType5 == CompleteType.Cancel || completeType6 == CompleteType.Cancel || completeType7 == CompleteType.Cancel || completeType8 == CompleteType.Cancel
                        ? Promise.State.Canceled
                    : Promise.State.Resolved;
                Promise.State expectedFinalState = cancelationType != CancelationType.Immediate ? expectedPromiseState
                    : expectedPromiseState == Promise.State.Rejected ? Promise.State.Rejected
                    : Promise.State.Canceled;

                bool completed = false;

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2Through7, value2, expectedException, out var tryCompleter2))
                    .Add(TestHelper.BuildPromise(completeType3, alreadyComplete2Through7, value3, expectedException, out var tryCompleter3))
                    .Add(TestHelper.BuildPromise(completeType4, alreadyComplete2Through7, value4, expectedException, out var tryCompleter4))
                    .Add(TestHelper.BuildPromise(completeType5, alreadyComplete2Through7, value5, expectedException, out var tryCompleter5))
                    .Add(TestHelper.BuildPromise(completeType6, alreadyComplete2Through7, value6, expectedException, out var tryCompleter6))
                    .Add(TestHelper.BuildPromise(completeType7, alreadyComplete2Through7, value7, expectedException, out var tryCompleter7))
                    .Add(TestHelper.BuildPromise(completeType8, alreadyComplete8, value8, expectedException, out var tryCompleter8))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;

                        Assert.AreEqual(expectedFinalState, result.State);
                        if (expectedFinalState == Promise.State.Resolved)
                        {
                            Assert.AreEqual(((value1, value2, value3, value4, value5, value6, value7), value8), result.Value);
                        }
                        else if (expectedFinalState == Promise.State.Rejected)
                        {
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
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
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_WithCancelation_2_0(
            [Values] CancelationType cancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeGroup.New(out var groupCancelationToken)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeGroup.New(cancelationSource.Token, out groupCancelationToken)
                    : PromiseMergeGroup.New(CancelationToken.Canceled(), out groupCancelationToken);

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

                        var expectedState = GetExpectedState(cancelationType, completeValues);
                        Assert.AreEqual(expectedState, result.State);
                        if (expectedState == Promise.State.Rejected)
                        {
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
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
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_WithCancelation_0_2(
            [Values] CancelationType cancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeGroup.New(out var groupCancelationToken)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeGroup.New(cancelationSource.Token, out groupCancelationToken)
                    : PromiseMergeGroup.New(CancelationToken.Canceled(), out groupCancelationToken);

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

                        var expectedState = GetExpectedState(cancelationType, completeValues);
                        Assert.AreEqual(expectedState, result.State);
                        if (expectedState == Promise.State.Rejected)
                        {
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
                        else if (expectedState == Promise.State.Resolved)
                        {
                            Assert.AreEqual((value1, value2), result.Value);
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
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_WithCancelation_2_2(
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
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeGroup.New(out var groupCancelationToken)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeGroup.New(cancelationSource.Token, out groupCancelationToken)
                    : PromiseMergeGroup.New(CancelationToken.Canceled(), out groupCancelationToken);

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

                        var expectedState = GetExpectedState(cancelationType, completeValues);
                        Assert.AreEqual(expectedState, result.State);
                        if (expectedState == Promise.State.Rejected)
                        {
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
                        else if (expectedState == Promise.State.Resolved)
                        {
                            Assert.AreEqual((value1, value2), result.Value);
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
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_WithCancelation_0_7(
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
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeGroup.New(out var groupCancelationToken)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeGroup.New(cancelationSource.Token, out groupCancelationToken)
                    : PromiseMergeGroup.New(CancelationToken.Canceled(), out groupCancelationToken);

                Exception expectedException = new Exception("Bang!");
                int value1 = 42;
                string value2 = "Success";
                bool value3 = true;
                float value4 = 1.0f;
                ulong value5 = ulong.MaxValue;
                TimeSpan value6 = TimeSpan.FromSeconds(2);
                Promise.State value7 = Promise.State.Canceled;

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
                    .Add(TestHelper.BuildPromise(completeType5, alreadyComplete3Through7, value5, expectedException, groupCancelationToken, out var tryCompleter5))
                    .Add(TestHelper.BuildPromise(completeType6, alreadyComplete3Through7, value6, expectedException, groupCancelationToken, out var tryCompleter6))
                    .Add(TestHelper.BuildPromise(completeType7, alreadyComplete3Through7, value7, expectedException, groupCancelationToken, out var tryCompleter7))
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
                            Assert.AreEqual((value1, value2, value3, value4, value5, value6, value7), result.Value);
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
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_WithCancelation_0_8(
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
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeGroup.New(out var groupCancelationToken)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeGroup.New(cancelationSource.Token, out groupCancelationToken)
                    : PromiseMergeGroup.New(CancelationToken.Canceled(), out groupCancelationToken);

                Exception expectedException = new Exception("Bang!");
                int value1 = 42;
                string value2 = "Success";
                bool value3 = true;
                float value4 = 1.0f;
                ulong value5 = ulong.MaxValue;
                TimeSpan value6 = TimeSpan.FromSeconds(2);
                Promise.State value7 = Promise.State.Canceled;
                int value8 = 2;

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
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2Through7, value2, expectedException, groupCancelationToken, out var tryCompleter2))
                    .Add(TestHelper.BuildPromise(completeType3, alreadyComplete2Through7, value3, expectedException, groupCancelationToken, out var tryCompleter3))
                    .Add(TestHelper.BuildPromise(completeType4, alreadyComplete2Through7, value4, expectedException, groupCancelationToken, out var tryCompleter4))
                    .Add(TestHelper.BuildPromise(completeType5, alreadyComplete2Through7, value5, expectedException, groupCancelationToken, out var tryCompleter5))
                    .Add(TestHelper.BuildPromise(completeType6, alreadyComplete2Through7, value6, expectedException, groupCancelationToken, out var tryCompleter6))
                    .Add(TestHelper.BuildPromise(completeType7, alreadyComplete2Through7, value7, expectedException, groupCancelationToken, out var tryCompleter7))
                    .Add(TestHelper.BuildPromise(completeType8, alreadyComplete8, value8, expectedException, groupCancelationToken, out var tryCompleter8))
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
                            Assert.AreEqual(((value1, value2, value3, value4, value5, value6, value7), value8), result.Value);
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
        public void PromiseMergeGroup_CancelationCallbackExceptionsArePropagated_2(
            [Values(CancelationType.None, CancelationType.Deferred)] CancelationType cancelationType,
            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeGroup.New(out var groupCancelationToken)
                    : PromiseMergeGroup.New(cancelationSource.Token, out groupCancelationToken);

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

        [Test]
        public void PromiseMergeGroup_CancelationCallbackExceptionsArePropagated_8(
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
                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeGroup.New(out var groupCancelationToken)
                    : PromiseMergeGroup.New(cancelationSource.Token, out groupCancelationToken);

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

                        int expectedExceptionCount = 1;
                        if (completeType1 == CompleteType.Reject)
                        {
                            ++expectedExceptionCount;
                        }
                        if (completeType8 == CompleteType.Reject)
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
        public void PromiseMergeGroup_OnCleanupIsInvokedCorrectly_1(
            [Values] CancelationType cancelationType,
            [Values] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values] bool alreadyComplete2)
        {
            using (var cancelationSource = CancelationSource.New())
            {
                int expectedValue = 42;
                int invokeCount = 0;

                void OnCleanup(int value)
                {
                    ++invokeCount;
                    Assert.AreEqual(expectedValue, value);
                }

                Promise.State expectedState = cancelationType != CancelationType.Immediate ? (Promise.State) completeType1
                    : completeType1 == CompleteType.Reject ? Promise.State.Rejected
                    : Promise.State.Canceled;

                var mergeGroup = cancelationType == CancelationType.None ? PromiseMergeGroup.New(out _)
                    : cancelationType == CancelationType.Deferred ? PromiseMergeGroup.New(cancelationSource.Token, out _)
                    : PromiseMergeGroup.New(CancelationToken.Canceled(), out _);

                bool completed = false;
                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, new System.InvalidOperationException("Bang!"), out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(CompleteType.Resolve, alreadyComplete2, expectedValue, new System.InvalidOperationException("Bang!"), out var tryCompleter2), v => OnCleanup(v))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;
                        Assert.AreEqual(expectedState, result.State);
                        if (expectedState == Promise.State.Resolved)
                        {
                            Assert.AreEqual(expectedValue, result.Value);
                        }
                        else if (expectedState == Promise.State.Rejected)
                        {
                            Assert.IsInstanceOf<AggregateException>(result.Reason);
                            Assert.AreEqual(1, result.Reason.UnsafeAs<AggregateException>().InnerExceptions.Count);
                        }
                    })
                    .Forget();

                Assert.AreEqual(alreadyComplete1 && alreadyComplete2, completed);

                tryCompleter1();
                Assert.AreEqual(alreadyComplete2, completed);

                tryCompleter2();
                Assert.True(completed);

                int expectedInvokeCount = expectedState == Promise.State.Resolved ? 0 : 1;
                Assert.AreEqual(expectedInvokeCount, invokeCount);
            }
        }

        private static IEnumerable<TestCaseData> GetOnCleanupIsInvokedCorrectlyArgs()
        {
            // Don't test captures for every combination to reduce number of tests.
            var testCleanupTypes = new[] { CleanupType.None, CleanupType.Sync, CleanupType.Async };

            foreach (CleanupType cleanupType1 in testCleanupTypes)
            foreach (CleanupType cleanupType2 in testCleanupTypes)
            {
                if (cleanupType1 == CleanupType.None && cleanupType2 == CleanupType.None) continue;

                foreach (CompleteType completeType1 in Enum.GetValues(typeof(CompleteType)))
                foreach (var alreadyComplete1 in new[] { true, false })
                foreach (CompleteType completeType2 in Enum.GetValues(typeof(CompleteType)))
                foreach (var alreadyComplete2 in new[] { true, false })
                {
                    CompleteType[] cleanupCompleteType1s = cleanupType1 == CleanupType.None
                        ? new[] { CompleteType.Resolve }
                        : new[] { CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel };
                    bool[] cleanupAlreadyComplete1s = cleanupType1 < CleanupType.Async
                        ? new[] { true }
                        : new[] { true, false };
                    CompleteType[] cleanupCompleteType2s = cleanupType1 == CleanupType.None
                        ? new[] { CompleteType.Resolve }
                        : new[] { CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel };
                    bool[] cleanupAlreadyComplete2s = cleanupType2 < CleanupType.Async
                        ? new[] { true }
                        : new[] { true, false };
                    foreach (CompleteType cleanupCompleteType1 in cleanupCompleteType1s)
                    foreach (var cleanupAlreadyComplete1 in cleanupAlreadyComplete1s)
                    foreach (CompleteType cleanupCompleteType2 in cleanupCompleteType2s)
                    foreach (var cleanupAlreadyComplete2 in cleanupAlreadyComplete2s)
                    {
                        // Skip most cases where cleanup is not invoked.
                        bool expectedInvoke1 = completeType1 == CompleteType.Resolve && completeType2 != CompleteType.Resolve && cleanupType1 != CleanupType.None;
                        bool expectedInvoke2 = completeType2 == CompleteType.Resolve && completeType1 != CompleteType.Resolve && cleanupType2 != CleanupType.None;
                        if (!expectedInvoke1 && !expectedInvoke2 && cleanupCompleteType1 != CompleteType.Resolve && cleanupCompleteType2 != CompleteType.Resolve) continue;

                        yield return new TestCaseData(completeType1, alreadyComplete1, completeType2, alreadyComplete2, cleanupType1, cleanupType2,
                            cleanupCompleteType1, cleanupAlreadyComplete1, cleanupCompleteType2, cleanupAlreadyComplete2);
                    }
                }
            }

            // Just test a few cases for captures.
            yield return new TestCaseData(CompleteType.Resolve, true, CompleteType.Reject, true, CleanupType.SyncCapture, CleanupType.None, CompleteType.Resolve, true, CompleteType.Resolve, true);
            yield return new TestCaseData(CompleteType.Resolve, true, CompleteType.Reject, true, CleanupType.AsyncCapture, CleanupType.None, CompleteType.Resolve, true, CompleteType.Resolve, true);
            yield return new TestCaseData(CompleteType.Reject, true, CompleteType.Resolve, true, CleanupType.None, CleanupType.SyncCapture, CompleteType.Resolve, true, CompleteType.Resolve, true);
            yield return new TestCaseData(CompleteType.Reject, true, CompleteType.Resolve, true, CleanupType.None, CleanupType.AsyncCapture, CompleteType.Resolve, true, CompleteType.Resolve, true);
        }

        [Test, TestCaseSource(nameof(GetOnCleanupIsInvokedCorrectlyArgs))]
        public void PromiseMergeGroup_OnCleanupIsInvokedCorrectly_2(
            CompleteType completeType1,
            bool alreadyComplete1,
            CompleteType completeType2,
            bool alreadyComplete2,
            CleanupType cleanupType1,
            CleanupType cleanupType2,
            CompleteType cleanupCompleteType1,
            bool cleanupAlreadyComplete1,
            CompleteType cleanupCompleteType2,
            bool cleanupAlreadyComplete2)
        {
            var mergeGroup = PromiseMergeGroup.New(out _);

            const string captureValue = "CaptureValue";
            bool expectedInvoke1 = completeType1 == CompleteType.Resolve && completeType2 != CompleteType.Resolve && cleanupType1 != CleanupType.None;
            bool expectedInvoke2 = completeType2 == CompleteType.Resolve && completeType1 != CompleteType.Resolve && cleanupType2 != CleanupType.None;
            Promise.State expectedState = completeType1 == CompleteType.Resolve && completeType2 == CompleteType.Resolve
                ? Promise.State.Resolved
                : completeType1 == CompleteType.Reject || completeType2 == CompleteType.Reject
                    || (expectedInvoke1 && cleanupCompleteType1 == CompleteType.Reject)
                    || (expectedInvoke2 && cleanupCompleteType2 == CompleteType.Reject)
                ? Promise.State.Rejected : Promise.State.Canceled;

            bool didInvoke1 = false;
            bool didInvoke2 = false;
            bool completed = false;

            var promise1 = TestHelper.BuildPromise(completeType1, alreadyComplete1, 1, new System.InvalidOperationException("Bang!"), out var tryCompleter1);
            var promise2 = TestHelper.BuildPromise(completeType2, alreadyComplete2, 2, new System.InvalidOperationException("Bang!"), out var tryCompleter2);

            var cleanupPromise1 = TestHelper.BuildPromise(cleanupCompleteType1, cleanupAlreadyComplete1, new System.InvalidOperationException("Bang!"), out var cleanupTryCompleter1);
            var cleanupPromise2 = TestHelper.BuildPromise(cleanupCompleteType2, cleanupAlreadyComplete2, new System.InvalidOperationException("Bang!"), out var cleanupTryCompleter2);
            if (!expectedInvoke1 || cleanupType1 < CleanupType.Async)
            {
                cleanupAlreadyComplete1 = true;
                cleanupPromise1.Catch(() => { }).Forget();
            }
            if (!expectedInvoke2 || cleanupType2 < CleanupType.Async)
            {
                cleanupAlreadyComplete2 = true;
                cleanupPromise2.Catch(() => { }).Forget();
            }

            void MaybeThrow(CompleteType completeType)
            {
                if (completeType == CompleteType.Cancel) throw Promise.CancelException();
                if (completeType == CompleteType.Reject) throw new System.InvalidOperationException("Bang!");
            }

            var mergeGroup1 = cleanupType1 == CleanupType.None ? mergeGroup.Add(promise1)
                : cleanupType1 == CleanupType.Sync ? mergeGroup.Add(promise1, v => { Assert.AreEqual(v, 1); didInvoke1 = true; MaybeThrow(cleanupCompleteType1); })
                : cleanupType1 == CleanupType.SyncCapture ? mergeGroup.Add(promise1, captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(v, 1); didInvoke1 = true; MaybeThrow(cleanupCompleteType1); })
                : cleanupType1 == CleanupType.Async ? mergeGroup.Add(promise1, v => { Assert.AreEqual(v, 1); didInvoke1 = true; return cleanupPromise1; })
                : mergeGroup.Add(promise1, captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(v, 1); didInvoke1 = true; return cleanupPromise1; });

            var mergeGroup2 = cleanupType2 == CleanupType.None ? mergeGroup1.Add(promise2)
                : cleanupType2 == CleanupType.Sync ? mergeGroup1.Add(promise2, v => { Assert.AreEqual(v, 2); didInvoke2 = true; MaybeThrow(cleanupCompleteType2); })
                : cleanupType2 == CleanupType.SyncCapture ? mergeGroup1.Add(promise2, captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(v, 2); didInvoke2 = true; MaybeThrow(cleanupCompleteType2); })
                : cleanupType2 == CleanupType.Async ? mergeGroup1.Add(promise2, v => { Assert.AreEqual(v, 2); didInvoke2 = true; return cleanupPromise2; })
                : mergeGroup1.Add(promise2, captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(v, 2); didInvoke2 = true; return cleanupPromise2; });

            mergeGroup2
                .WaitAsync()
                .ContinueWith(result =>
                {
                    completed = true;
                    Assert.AreEqual(expectedState, result.State);
                    if (expectedState == Promise.State.Resolved)
                    {
                        Assert.AreEqual((1, 2), result.Value);
                        return;
                    }

                    if (expectedState == Promise.State.Canceled)
                    {
                        return;
                    }

                    int expectedExceptionCount = 0;
                    if (completeType1 == CompleteType.Reject
                        || (expectedInvoke1 && cleanupCompleteType1 == CompleteType.Reject))
                    {
                        ++expectedExceptionCount;
                    }
                    if (completeType2 == CompleteType.Reject
                        || (expectedInvoke2 && cleanupCompleteType2 == CompleteType.Reject))
                    {
                        ++expectedExceptionCount;
                    }

                    Assert.Greater(expectedExceptionCount, 0);

                    Assert.IsInstanceOf<AggregateException>(result.Reason);
                    Assert.AreEqual(expectedExceptionCount, result.Reason.UnsafeAs<AggregateException>().InnerExceptions.Count);
                })
                .Forget();

            Assert.AreEqual(alreadyComplete1 && alreadyComplete2 && cleanupAlreadyComplete1 && cleanupAlreadyComplete2, completed);

            tryCompleter1();
            Assert.AreEqual(alreadyComplete2 && cleanupAlreadyComplete1 && cleanupAlreadyComplete2, completed);

            tryCompleter2();
            Assert.AreEqual(cleanupAlreadyComplete1 && cleanupAlreadyComplete2, completed);

            Assert.AreEqual(expectedInvoke1, didInvoke1);
            Assert.AreEqual(expectedInvoke2, didInvoke2);

            cleanupTryCompleter1();
            Assert.AreEqual(cleanupAlreadyComplete2, completed);

            cleanupTryCompleter2();
            Assert.IsTrue(completed);
        }

        [Test, TestCaseSource(nameof(GetOnCleanupIsInvokedCorrectlyArgs))]
        public void PromiseMergeGroup_OnCleanupIsInvokedCorrectly_8(
            CompleteType completeType1,
            bool alreadyComplete1,
            CompleteType completeType2,
            bool alreadyComplete2,
            CleanupType cleanupType1,
            CleanupType cleanupType2,
            CompleteType cleanupCompleteType1,
            bool cleanupAlreadyComplete1,
            CompleteType cleanupCompleteType2,
            bool cleanupAlreadyComplete2)
        {
            var mergeGroup = PromiseMergeGroup.New(out _);

            const string captureValue = "CaptureValue";
            bool expectedInvoke1 = completeType1 == CompleteType.Resolve && completeType2 != CompleteType.Resolve && cleanupType1 != CleanupType.None;
            bool expectedInvoke2 = completeType2 == CompleteType.Resolve && completeType1 != CompleteType.Resolve && cleanupType2 != CleanupType.None;
            Promise.State expectedState = completeType1 == CompleteType.Resolve && completeType2 == CompleteType.Resolve
                ? Promise.State.Resolved
                : completeType1 == CompleteType.Reject || completeType2 == CompleteType.Reject
                    || (expectedInvoke1 && cleanupCompleteType1 == CompleteType.Reject)
                    || (expectedInvoke2 && cleanupCompleteType2 == CompleteType.Reject)
                ? Promise.State.Rejected : Promise.State.Canceled;

            bool didInvoke1 = false;
            bool didInvoke2 = false;
            bool completed = false;

            var promise1 = TestHelper.BuildPromise(completeType1, alreadyComplete1, 1, new System.InvalidOperationException("Bang!"), out var tryCompleter1);
            var promise2 = TestHelper.BuildPromise(completeType2, alreadyComplete2, 8, new System.InvalidOperationException("Bang!"), out var tryCompleter2);

            var cleanupPromise1 = TestHelper.BuildPromise(cleanupCompleteType1, cleanupAlreadyComplete1, new System.InvalidOperationException("Bang!"), out var cleanupTryCompleter1);
            var cleanupPromise2 = TestHelper.BuildPromise(cleanupCompleteType2, cleanupAlreadyComplete2, new System.InvalidOperationException("Bang!"), out var cleanupTryCompleter2);
            if (!expectedInvoke1 || cleanupType1 < CleanupType.Async)
            {
                cleanupAlreadyComplete1 = true;
                cleanupPromise1.Catch(() => { }).Forget();
            }
            if (!expectedInvoke2 || cleanupType2 < CleanupType.Async)
            {
                cleanupAlreadyComplete2 = true;
                cleanupPromise2.Catch(() => { }).Forget();
            }

            void MaybeThrow(CompleteType completeType)
            {
                if (completeType == CompleteType.Cancel)
                    throw Promise.CancelException();
                if (completeType == CompleteType.Reject)
                    throw new System.InvalidOperationException("Bang!");
            }

            var mergeGroup1 = cleanupType1 == CleanupType.None ? mergeGroup.Add(promise1)
                : cleanupType1 == CleanupType.Sync ? mergeGroup.Add(promise1, v => { Assert.AreEqual(v, 1); didInvoke1 = true; MaybeThrow(cleanupCompleteType1); })
                : cleanupType1 == CleanupType.SyncCapture ? mergeGroup.Add(promise1, captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(v, 1); didInvoke1 = true; MaybeThrow(cleanupCompleteType1); })
                : cleanupType1 == CleanupType.Async ? mergeGroup.Add(promise1, v => { Assert.AreEqual(v, 1); didInvoke1 = true; return cleanupPromise1; })
                : mergeGroup.Add(promise1, captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(v, 1); didInvoke1 = true; return cleanupPromise1; });

            var mergeGroup7 = mergeGroup1
                .Add(Promise.Resolved(2))
                .Add(Promise.Resolved(3))
                .Add(Promise.Resolved(4))
                .Add(Promise.Resolved(5))
                .Add(Promise.Resolved(6))
                .Add(Promise.Resolved(7));

            var mergeGroup8 = cleanupType2 == CleanupType.None ? mergeGroup7.Add(promise2)
                : cleanupType2 == CleanupType.Sync ? mergeGroup7.Add(promise2, v => { Assert.AreEqual(v, 8); didInvoke2 = true; MaybeThrow(cleanupCompleteType2); })
                : cleanupType2 == CleanupType.SyncCapture ? mergeGroup7.Add(promise2, captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(v, 8); didInvoke2 = true; MaybeThrow(cleanupCompleteType2); })
                : cleanupType2 == CleanupType.Async ? mergeGroup7.Add(promise2, v => { Assert.AreEqual(v, 8); didInvoke2 = true; return cleanupPromise2; })
                : mergeGroup7.Add(promise2, captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(v, 8); didInvoke2 = true; return cleanupPromise2; });

            mergeGroup8
                .WaitAsync()
                .ContinueWith(result =>
                {
                    completed = true;
                    Assert.AreEqual(expectedState, result.State);
                    if (expectedState == Promise.State.Resolved)
                    {
                        Assert.AreEqual(((1, 2, 3, 4, 5, 6, 7), 8), result.Value);
                        return;
                    }

                    if (expectedState == Promise.State.Canceled)
                    {
                        return;
                    }

                    int expectedExceptionCount = 0;
                    if (completeType1 == CompleteType.Reject
                        || (expectedInvoke1 && cleanupCompleteType1 == CompleteType.Reject))
                    {
                        ++expectedExceptionCount;
                    }
                    if (completeType2 == CompleteType.Reject
                        || (expectedInvoke2 && cleanupCompleteType2 == CompleteType.Reject))
                    {
                        ++expectedExceptionCount;
                    }

                    Assert.Greater(expectedExceptionCount, 0);

                    Assert.IsInstanceOf<AggregateException>(result.Reason);
                    Assert.AreEqual(expectedExceptionCount, result.Reason.UnsafeAs<AggregateException>().InnerExceptions.Count);
                })
                .Forget();

            Assert.AreEqual(alreadyComplete1 && alreadyComplete2 && cleanupAlreadyComplete1 && cleanupAlreadyComplete2, completed);

            tryCompleter1();
            Assert.AreEqual(alreadyComplete2 && cleanupAlreadyComplete1 && cleanupAlreadyComplete2, completed);

            tryCompleter2();
            Assert.AreEqual(cleanupAlreadyComplete1 && cleanupAlreadyComplete2, completed);

            Assert.AreEqual(expectedInvoke1, didInvoke1);
            Assert.AreEqual(expectedInvoke2, didInvoke2);

            cleanupTryCompleter1();
            Assert.AreEqual(cleanupAlreadyComplete2, completed);

            cleanupTryCompleter2();
            Assert.IsTrue(completed);
        }

        // Simpler, non-exhaustive tests to make sure all cleanups are invoked for each group arity.
        [Test]
        public void PromiseMergeGroup_AllOnCleanupAreInvoked_Sync(
            [Range(1, 8)] int count)
        {
            int invokedCount = 0;

            object mergeGroup = PromiseMergeGroup.New(out _)
                .Add(Promise.Canceled());

            for (int i = 1; i <= count; ++i)
            {
                switch (i)
                {
                    case 1:
                        mergeGroup = ((PromiseMergeGroup) mergeGroup).Add(Promise.Resolved(1), v => { Assert.AreEqual(1, v); ++invokedCount; });
                        break;
                    case 2:
                        mergeGroup = ((PromiseMergeGroup<int>) mergeGroup).Add(Promise.Resolved(2), v => { Assert.AreEqual(2, v); ++invokedCount; });
                        break;
                    case 3:
                        mergeGroup = ((PromiseMergeGroup<int, int>) mergeGroup).Add(Promise.Resolved(3), v => { Assert.AreEqual(3, v); ++invokedCount; });
                        break;
                    case 4:
                        mergeGroup = ((PromiseMergeGroup<int, int, int>) mergeGroup).Add(Promise.Resolved(4), v => { Assert.AreEqual(4, v); ++invokedCount; });
                        break;
                    case 5:
                        mergeGroup = ((PromiseMergeGroup<int, int, int, int>) mergeGroup).Add(Promise.Resolved(5), v => { Assert.AreEqual(5, v); ++invokedCount; });
                        break;
                    case 6:
                        mergeGroup = ((PromiseMergeGroup<int, int, int, int, int>) mergeGroup).Add(Promise.Resolved(6), v => { Assert.AreEqual(6, v); ++invokedCount; });
                        break;
                    case 7:
                        mergeGroup = ((PromiseMergeGroup<int, int, int, int, int, int>) mergeGroup).Add(Promise.Resolved(7), v => { Assert.AreEqual(7, v); ++invokedCount; });
                        break;
                    case 8:
                        mergeGroup = ((PromiseMergeGroup<int, int, int, int, int, int, int>) mergeGroup).Add(Promise.Resolved(8), v => { Assert.AreEqual(8, v); ++invokedCount; });
                        break;
                    default:
                        throw new System.ArgumentOutOfRangeException(nameof(i));
                }
            }

            Promise promise;
            switch (count)
            {
                case 1:
                    promise = ((PromiseMergeGroup<int>) mergeGroup).WaitAsync();
                    break;
                case 2:
                    promise = ((PromiseMergeGroup<int, int>) mergeGroup).WaitAsync();
                    break;
                case 3:
                    promise = ((PromiseMergeGroup<int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 4:
                    promise = ((PromiseMergeGroup<int, int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 5:
                    promise = ((PromiseMergeGroup<int, int, int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 6:
                    promise = ((PromiseMergeGroup<int, int, int, int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 7:
                    promise = ((PromiseMergeGroup<int, int, int, int, int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 8:
                    promise = ((PromiseMergeGroup<(int, int, int, int, int, int, int), int>) mergeGroup).WaitAsync();
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(count));
            }

            promise
                .CatchCancelation(() => { })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.AreEqual(count, invokedCount);
        }

        [Test]
        public void PromiseMergeGroup_AllOnCleanupAreInvoked_SyncCapture(
    [Range(1, 8)] int count)
        {
            int invokedCount = 0;
            const string captureValue = "CaptureValue";

            object mergeGroup = PromiseMergeGroup.New(out _)
                .Add(Promise.Canceled());

            for (int i = 1; i <= count; ++i)
            {
                switch (i)
                {
                    case 1:
                        mergeGroup = ((PromiseMergeGroup) mergeGroup).Add(Promise.Resolved(1), captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(1, v); ++invokedCount; });
                        break;
                    case 2:
                        mergeGroup = ((PromiseMergeGroup<int>) mergeGroup).Add(Promise.Resolved(2), captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(2, v); ++invokedCount; });
                        break;
                    case 3:
                        mergeGroup = ((PromiseMergeGroup<int, int>) mergeGroup).Add(Promise.Resolved(3), captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(3, v); ++invokedCount; });
                        break;
                    case 4:
                        mergeGroup = ((PromiseMergeGroup<int, int, int>) mergeGroup).Add(Promise.Resolved(4), captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(4, v); ++invokedCount; });
                        break;
                    case 5:
                        mergeGroup = ((PromiseMergeGroup<int, int, int, int>) mergeGroup).Add(Promise.Resolved(5), captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(5, v); ++invokedCount; });
                        break;
                    case 6:
                        mergeGroup = ((PromiseMergeGroup<int, int, int, int, int>) mergeGroup).Add(Promise.Resolved(6), captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(6, v); ++invokedCount; });
                        break;
                    case 7:
                        mergeGroup = ((PromiseMergeGroup<int, int, int, int, int, int>) mergeGroup).Add(Promise.Resolved(7), captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(7, v); ++invokedCount; });
                        break;
                    case 8:
                        mergeGroup = ((PromiseMergeGroup<int, int, int, int, int, int, int>) mergeGroup).Add(Promise.Resolved(8), captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(8, v); ++invokedCount; });
                        break;
                    default:
                        throw new System.ArgumentOutOfRangeException(nameof(i));
                }
            }

            Promise promise;
            switch (count)
            {
                case 1:
                    promise = ((PromiseMergeGroup<int>) mergeGroup).WaitAsync();
                    break;
                case 2:
                    promise = ((PromiseMergeGroup<int, int>) mergeGroup).WaitAsync();
                    break;
                case 3:
                    promise = ((PromiseMergeGroup<int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 4:
                    promise = ((PromiseMergeGroup<int, int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 5:
                    promise = ((PromiseMergeGroup<int, int, int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 6:
                    promise = ((PromiseMergeGroup<int, int, int, int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 7:
                    promise = ((PromiseMergeGroup<int, int, int, int, int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 8:
                    promise = ((PromiseMergeGroup<(int, int, int, int, int, int, int), int>) mergeGroup).WaitAsync();
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(count));
            }

            promise
                .CatchCancelation(() => { })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.AreEqual(count, invokedCount);
        }

        [Test]
        public void PromiseMergeGroup_AllOnCleanupAreInvoked_Async(
            [Range(1, 8)] int count)
        {
            int invokedCount = 0;

            object mergeGroup = PromiseMergeGroup.New(out _)
                .Add(Promise.Canceled());

            for (int i = 1; i <= count; ++i)
            {
                switch (i)
                {
                    case 1:
                        mergeGroup = ((PromiseMergeGroup) mergeGroup).Add(Promise.Resolved(1), v => { Assert.AreEqual(1, v); ++invokedCount; return Promise.Resolved(); });
                        break;
                    case 2:
                        mergeGroup = ((PromiseMergeGroup<int>) mergeGroup).Add(Promise.Resolved(2), v => { Assert.AreEqual(2, v); ++invokedCount; return Promise.Resolved(); });
                        break;
                    case 3:
                        mergeGroup = ((PromiseMergeGroup<int, int>) mergeGroup).Add(Promise.Resolved(3), v => { Assert.AreEqual(3, v); ++invokedCount; return Promise.Resolved(); });
                        break;
                    case 4:
                        mergeGroup = ((PromiseMergeGroup<int, int, int>) mergeGroup).Add(Promise.Resolved(4), v => { Assert.AreEqual(4, v); ++invokedCount; return Promise.Resolved(); });
                        break;
                    case 5:
                        mergeGroup = ((PromiseMergeGroup<int, int, int, int>) mergeGroup).Add(Promise.Resolved(5), v => { Assert.AreEqual(5, v); ++invokedCount; return Promise.Resolved(); });
                        break;
                    case 6:
                        mergeGroup = ((PromiseMergeGroup<int, int, int, int, int>) mergeGroup).Add(Promise.Resolved(6), v => { Assert.AreEqual(6, v); ++invokedCount; return Promise.Resolved(); });
                        break;
                    case 7:
                        mergeGroup = ((PromiseMergeGroup<int, int, int, int, int, int>) mergeGroup).Add(Promise.Resolved(7), v => { Assert.AreEqual(7, v); ++invokedCount; return Promise.Resolved(); });
                        break;
                    case 8:
                        mergeGroup = ((PromiseMergeGroup<int, int, int, int, int, int, int>) mergeGroup).Add(Promise.Resolved(8), v => { Assert.AreEqual(8, v); ++invokedCount; return Promise.Resolved(); });
                        break;
                    default:
                        throw new System.ArgumentOutOfRangeException(nameof(i));
                }
            }

            Promise promise;
            switch (count)
            {
                case 1:
                    promise = ((PromiseMergeGroup<int>) mergeGroup).WaitAsync();
                    break;
                case 2:
                    promise = ((PromiseMergeGroup<int, int>) mergeGroup).WaitAsync();
                    break;
                case 3:
                    promise = ((PromiseMergeGroup<int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 4:
                    promise = ((PromiseMergeGroup<int, int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 5:
                    promise = ((PromiseMergeGroup<int, int, int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 6:
                    promise = ((PromiseMergeGroup<int, int, int, int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 7:
                    promise = ((PromiseMergeGroup<int, int, int, int, int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 8:
                    promise = ((PromiseMergeGroup<(int, int, int, int, int, int, int), int>) mergeGroup).WaitAsync();
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(count));
            }

            promise
                .CatchCancelation(() => { })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.AreEqual(count, invokedCount);
        }

        [Test]
        public void PromiseMergeGroup_AllOnCleanupAreInvoked_AsyncCapture(
            [Range(1, 8)] int count)
        {
            int invokedCount = 0;
            const string captureValue = "CaptureValue";

            object mergeGroup = PromiseMergeGroup.New(out _)
                .Add(Promise.Canceled());

            for (int i = 1; i <= count; ++i)
            {
                switch (i)
                {
                    case 1:
                        mergeGroup = ((PromiseMergeGroup) mergeGroup).Add(Promise.Resolved(1), captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(1, v); ++invokedCount; return Promise.Resolved(); });
                        break;
                    case 2:
                        mergeGroup = ((PromiseMergeGroup<int>) mergeGroup).Add(Promise.Resolved(2), captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(2, v); ++invokedCount; return Promise.Resolved(); });
                        break;
                    case 3:
                        mergeGroup = ((PromiseMergeGroup<int, int>) mergeGroup).Add(Promise.Resolved(3), captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(3, v); ++invokedCount; return Promise.Resolved(); });
                        break;
                    case 4:
                        mergeGroup = ((PromiseMergeGroup<int, int, int>) mergeGroup).Add(Promise.Resolved(4), captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(4, v); ++invokedCount; return Promise.Resolved(); });
                        break;
                    case 5:
                        mergeGroup = ((PromiseMergeGroup<int, int, int, int>) mergeGroup).Add(Promise.Resolved(5), captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(5, v); ++invokedCount; return Promise.Resolved(); });
                        break;
                    case 6:
                        mergeGroup = ((PromiseMergeGroup<int, int, int, int, int>) mergeGroup).Add(Promise.Resolved(6), captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(6, v); ++invokedCount; return Promise.Resolved(); });
                        break;
                    case 7:
                        mergeGroup = ((PromiseMergeGroup<int, int, int, int, int, int>) mergeGroup).Add(Promise.Resolved(7), captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(7, v); ++invokedCount; return Promise.Resolved(); });
                        break;
                    case 8:
                        mergeGroup = ((PromiseMergeGroup<int, int, int, int, int, int, int>) mergeGroup).Add(Promise.Resolved(8), captureValue, (cv, v) => { Assert.AreEqual(captureValue, cv); Assert.AreEqual(8, v); ++invokedCount; return Promise.Resolved(); });
                        break;
                    default:
                        throw new System.ArgumentOutOfRangeException(nameof(i));
                }
            }

            Promise promise;
            switch (count)
            {
                case 1:
                    promise = ((PromiseMergeGroup<int>) mergeGroup).WaitAsync();
                    break;
                case 2:
                    promise = ((PromiseMergeGroup<int, int>) mergeGroup).WaitAsync();
                    break;
                case 3:
                    promise = ((PromiseMergeGroup<int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 4:
                    promise = ((PromiseMergeGroup<int, int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 5:
                    promise = ((PromiseMergeGroup<int, int, int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 6:
                    promise = ((PromiseMergeGroup<int, int, int, int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 7:
                    promise = ((PromiseMergeGroup<int, int, int, int, int, int, int>) mergeGroup).WaitAsync();
                    break;
                case 8:
                    promise = ((PromiseMergeGroup<(int, int, int, int, int, int, int), int>) mergeGroup).WaitAsync();
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(count));
            }

            promise
                .CatchCancelation(() => { })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.AreEqual(count, invokedCount);
        }
    }
}