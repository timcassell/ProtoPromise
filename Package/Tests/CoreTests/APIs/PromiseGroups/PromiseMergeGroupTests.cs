#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using System;

namespace ProtoPromiseTests.APIs.PromiseGroups
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
            [Values] bool withCancelation)
        {
            var voidPromise = Promise.Resolved();
            var intPromise = Promise.Resolved(42);
            Assert.Catch<System.InvalidOperationException>(() => default(PromiseMergeGroup).Add(voidPromise));
            Assert.Catch<System.InvalidOperationException>(() => default(PromiseMergeGroup).Add(intPromise));
            Assert.Catch<System.InvalidOperationException>(() => default(PromiseMergeGroup).WaitAsync());

            using (var cancelationSource = CancelationSource.New())
            {
                var mergeGroup1 = withCancelation
                    ? PromiseMergeGroup.New(cancelationSource.Token, out _)
                    : PromiseMergeGroup.New(out _);

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
        public void PromiseMergeGroupIsResolvedWhenNoPromisesAreAdded()
        {
            bool resolved = false;

            PromiseMergeGroup.New(out _)
                .WaitAsync()
                .Then(() => resolved = true)
                .Forget();

            Assert.True(resolved);
        }

        [Test]
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_1_0(
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType,
            [Values] bool alreadyComplete)
        {
            Exception expectedException = new Exception("Bang!");

            bool completed = false;

            PromiseMergeGroup.New(out _)
                .Add(TestHelper.BuildPromise(completeType, alreadyComplete, expectedException, out var completer))
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

            completer();
            Assert.IsTrue(completed);
        }

        [Test]
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_2_0(
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            Exception expectedException = new Exception("Bang!");

            bool completed = false;

            PromiseMergeGroup.New(out _)
                .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, expectedException, out var completer1))
                .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, expectedException, out var completer2))
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

            completer1();
            Assert.AreEqual(alreadyComplete2, completed);

            completer2();
            Assert.IsTrue(completed);
        }

        [Test]
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_0_2(
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType2,
            [Values] bool alreadyComplete2)
        {
            Exception expectedException = new Exception("Bang!");
            int value1 = 42;
            string value2 = "Success";

            bool completed = false;

            PromiseMergeGroup.New(out _)
                .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, out var completer1))
                .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, value2, expectedException, out var completer2))
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

            completer1();
            Assert.AreEqual(alreadyComplete2, completed);

            completer2();
            Assert.IsTrue(completed);
        }

        [Test]
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_2_2(
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType2,
            [Values] bool alreadyComplete2,
            // Only test resolve for 3 and 4 to reduce the number of tests.
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values] bool alreadyComplete3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values] bool alreadyComplete4)
        {
            Exception expectedException = new Exception("Bang!");
            int value1 = 42;
            string value2 = "Success";

            bool completed = false;

            PromiseMergeGroup.New(out _)
                .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, expectedException, out var completer1))
                .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, value1, expectedException, out var completer2))
                .Add(TestHelper.BuildPromise(completeType3, alreadyComplete3, expectedException, out var completer3))
                .Add(TestHelper.BuildPromise(completeType4, alreadyComplete4, value2, expectedException, out var completer4))
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

            Assert.AreEqual(alreadyComplete1 && alreadyComplete2 && alreadyComplete3 && alreadyComplete4, completed);

            completer1();
            Assert.AreEqual(alreadyComplete2 && alreadyComplete3 && alreadyComplete4, completed);

            completer2();
            Assert.AreEqual(alreadyComplete3 && alreadyComplete4, completed);
            
            completer3();
            Assert.AreEqual(alreadyComplete4, completed);
            
            completer4();
            Assert.IsTrue(completed);
        }

        [Test]
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_1_3(
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            // Only test resolve for 2 and 3 to reduce the number of tests.
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values] bool alreadyComplete2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values] bool alreadyComplete3,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType4,
            [Values] bool alreadyComplete4)
        {
            Exception expectedException = new Exception("Bang!");
            int value1 = 42;
            string value2 = "Success";
            bool value3 = true;

            bool completed = false;

            PromiseMergeGroup.New(out _)
                .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, out var completer1))
                .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, expectedException, out var completer2))
                .Add(TestHelper.BuildPromise(completeType3, alreadyComplete3, value2, expectedException, out var completer3))
                .Add(TestHelper.BuildPromise(completeType4, alreadyComplete4, value3, expectedException, out var completer4))
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

            Assert.AreEqual(alreadyComplete1 && alreadyComplete2 && alreadyComplete3 && alreadyComplete4, completed);

            completer1();
            Assert.AreEqual(alreadyComplete2 && alreadyComplete3 && alreadyComplete4, completed);

            completer2();
            Assert.AreEqual(alreadyComplete3 && alreadyComplete4, completed);

            completer3();
            Assert.AreEqual(alreadyComplete4, completed);

            completer4();
            Assert.IsTrue(completed);
        }

        [Test]
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_0_7(
            // Only test all complete types for 1 and 2 to reduce the number of tests.
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType2,
            [Values] bool alreadyComplete2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values] bool alreadyComplete3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values] bool alreadyComplete4Through7,
            [Values(CompleteType.Resolve)] CompleteType completeType5,
            [Values(CompleteType.Resolve)] CompleteType completeType6,
            [Values(CompleteType.Resolve)] CompleteType completeType7)
        {
            Exception expectedException = new Exception("Bang!");
            int value1 = 42;
            string value2 = "Success";
            bool value3 = true;
            float value4 = 1.0f;
            ulong value5 = ulong.MaxValue;
            TimeSpan value6 = TimeSpan.FromSeconds(2);
            Promise.State value7 = Promise.State.Canceled;

            bool completed = false;

            PromiseMergeGroup.New(out _)
                .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, out var completer1))
                .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2, value2, expectedException, out var completer2))
                .Add(TestHelper.BuildPromise(completeType3, alreadyComplete3, value3, expectedException, out var completer3))
                .Add(TestHelper.BuildPromise(completeType4, alreadyComplete4Through7, value4, expectedException, out var completer4))
                .Add(TestHelper.BuildPromise(completeType5, alreadyComplete4Through7, value5, expectedException, out var completer5))
                .Add(TestHelper.BuildPromise(completeType6, alreadyComplete4Through7, value6, expectedException, out var completer6))
                .Add(TestHelper.BuildPromise(completeType7, alreadyComplete4Through7, value7, expectedException, out var completer7))
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

            Assert.AreEqual(alreadyComplete1 && alreadyComplete2 && alreadyComplete3 && alreadyComplete4Through7, completed);

            completer1();
            Assert.AreEqual(alreadyComplete2 && alreadyComplete3 && alreadyComplete4Through7, completed);

            completer2();
            Assert.AreEqual(alreadyComplete3 && alreadyComplete4Through7, completed);

            completer3();
            Assert.AreEqual(alreadyComplete4Through7, completed);

            completer4();
            Assert.AreEqual(alreadyComplete4Through7, completed);
            completer5();
            Assert.AreEqual(alreadyComplete4Through7, completed);
            completer6();
            Assert.AreEqual(alreadyComplete4Through7, completed);
            completer7();

            Assert.IsTrue(completed);
        }

        [Test]
        public void PromiseMergeGroupIsCompletedWhenAllPromisesAreCompleted_0_8(
            // Only test all complete types for 1 and 8 to reduce the number of tests.
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType1,
            [Values] bool alreadyComplete1,
            [Values] bool alreadyComplete2Through6,
            [Values(CompleteType.Resolve)] CompleteType completeType2,
            [Values(CompleteType.Resolve)] CompleteType completeType3,
            [Values(CompleteType.Resolve)] CompleteType completeType4,
            [Values(CompleteType.Resolve)] CompleteType completeType5,
            [Values(CompleteType.Resolve)] CompleteType completeType6,
            [Values(CompleteType.Resolve)] CompleteType completeType7,
            [Values] bool alreadyComplete7,
            [Values(CompleteType.Resolve, CompleteType.Reject, CompleteType.Cancel)] CompleteType completeType8,
            [Values] bool alreadyComplete8)
        {
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

            PromiseMergeGroup.New(out _)
                .Add(TestHelper.BuildPromise(completeType1, alreadyComplete1, value1, expectedException, out var completer1))
                .Add(TestHelper.BuildPromise(completeType2, alreadyComplete2Through6, value2, expectedException, out var completer2))
                .Add(TestHelper.BuildPromise(completeType3, alreadyComplete2Through6, value3, expectedException, out var completer3))
                .Add(TestHelper.BuildPromise(completeType4, alreadyComplete2Through6, value4, expectedException, out var completer4))
                .Add(TestHelper.BuildPromise(completeType5, alreadyComplete2Through6, value5, expectedException, out var completer5))
                .Add(TestHelper.BuildPromise(completeType6, alreadyComplete2Through6, value6, expectedException, out var completer6))
                .Add(TestHelper.BuildPromise(completeType7, alreadyComplete7, value7, expectedException, out var completer7))
                .Add(TestHelper.BuildPromise(completeType8, alreadyComplete8, value8, expectedException, out var completer8))
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

            Assert.AreEqual(alreadyComplete1 && alreadyComplete2Through6 && alreadyComplete7 && alreadyComplete8, completed);

            completer1();
            Assert.AreEqual(alreadyComplete2Through6 && alreadyComplete7 && alreadyComplete8, completed);

            completer2();
            Assert.AreEqual(alreadyComplete2Through6 && alreadyComplete7 && alreadyComplete8, completed);
            completer3();
            Assert.AreEqual(alreadyComplete2Through6 && alreadyComplete7 && alreadyComplete8, completed);
            completer4();
            Assert.AreEqual(alreadyComplete2Through6 && alreadyComplete7 && alreadyComplete8, completed);
            completer5();
            Assert.AreEqual(alreadyComplete2Through6 && alreadyComplete7 && alreadyComplete8, completed);

            completer6();
            Assert.AreEqual(alreadyComplete7 && alreadyComplete8, completed);
            
            completer7();
            Assert.AreEqual(alreadyComplete8, completed);

            completer8();
            Assert.IsTrue(completed);
        }

        // TODO: test cancelation
    }
}