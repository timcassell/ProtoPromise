#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System;
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

                bool completed = false;

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType, alreadyComplete, expectedException, out var tryCompleter))
                    .WaitAsync()
                    .ContinueWith(result =>
                    {
                        completed = true;

                        if (completeType == CompleteType.Reject)
                        {
                            Assert.AreEqual(Promise.State.Rejected, result.State);
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
                        else
                        {
                            Assert.AreEqual(completeType, (CompleteType) result.State);
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

                bool completed = false;

                mergeGroup
                    .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, expectedException, out var tryCompleter1))
                    .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, expectedException, out var tryCompleter2))
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

                bool completed = false;

                mergeGroup
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
                            Assert.AreEqual((value1, value2), result.Value);
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

                        if (completeType1 == CompleteType.Reject || completeType2 == CompleteType.Reject || completeType3 == CompleteType.Reject || completeType4 == CompleteType.Reject)
                        {
                            Assert.AreEqual(Promise.State.Rejected, result.State);
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
                        else if (completeType1 == CompleteType.Cancel || completeType2 == CompleteType.Cancel || completeType3 == CompleteType.Cancel || completeType4 == CompleteType.Cancel)
                        {
                            Assert.AreEqual(Promise.State.Canceled, result.State);
                        }
                        else
                        {
                            Assert.AreEqual(Promise.State.Resolved, result.State);
                            Assert.AreEqual((value1, value2), result.Value);
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

                        if (completeType1 == CompleteType.Reject || completeType2 == CompleteType.Reject || completeType3 == CompleteType.Reject || completeType4 == CompleteType.Reject)
                        {
                            Assert.AreEqual(Promise.State.Rejected, result.State);
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
                        else if (completeType1 == CompleteType.Cancel || completeType2 == CompleteType.Cancel || completeType3 == CompleteType.Cancel || completeType4 == CompleteType.Cancel)
                        {
                            Assert.AreEqual(Promise.State.Canceled, result.State);
                        }
                        else
                        {
                            Assert.AreEqual(Promise.State.Resolved, result.State);
                            Assert.AreEqual((value1, value2, value3), result.Value);
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

                        if (completeType1 == CompleteType.Reject || completeType2 == CompleteType.Reject || completeType3 == CompleteType.Reject || completeType4 == CompleteType.Reject
                            || completeType5 == CompleteType.Reject || completeType6 == CompleteType.Reject || completeType7 == CompleteType.Reject)
                        {
                            Assert.AreEqual(Promise.State.Rejected, result.State);
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
                        else if (completeType1 == CompleteType.Cancel || completeType2 == CompleteType.Cancel || completeType3 == CompleteType.Cancel || completeType4 == CompleteType.Cancel
                            || completeType5 == CompleteType.Cancel || completeType6 == CompleteType.Cancel || completeType7 == CompleteType.Cancel)
                        {
                            Assert.AreEqual(Promise.State.Canceled, result.State);
                        }
                        else
                        {
                            Assert.AreEqual(Promise.State.Resolved, result.State);
                            Assert.AreEqual((value1, value2, value3, value4, value5, value6, value7), result.Value);
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

                        if (completeType1 == CompleteType.Reject || completeType2 == CompleteType.Reject || completeType3 == CompleteType.Reject || completeType4 == CompleteType.Reject
                            || completeType5 == CompleteType.Reject || completeType6 == CompleteType.Reject || completeType7 == CompleteType.Reject || completeType8 == CompleteType.Reject)
                        {
                            Assert.AreEqual(Promise.State.Rejected, result.State);
                            Assert.IsAssignableFrom<AggregateException>(result.Reason);
                            Assert.AreEqual(expectedException, result.Reason.UnsafeAs<AggregateException>().InnerExceptions[0]);
                        }
                        else if (completeType1 == CompleteType.Cancel || completeType2 == CompleteType.Cancel || completeType3 == CompleteType.Cancel || completeType4 == CompleteType.Cancel
                            || completeType5 == CompleteType.Cancel || completeType6 == CompleteType.Cancel || completeType7 == CompleteType.Cancel || completeType8 == CompleteType.Cancel)
                        {
                            Assert.AreEqual(Promise.State.Canceled, result.State);
                        }
                        else
                        {
                            Assert.AreEqual(Promise.State.Resolved, result.State);
                            Assert.AreEqual(((value1, value2, value3, value4, value5, value6, value7), value8), result.Value);
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
    }
}