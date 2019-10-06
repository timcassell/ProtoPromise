using NUnit.Framework;
using Proto.Promises;
using System;

namespace Tests
{
    public class APlus2_2
    {
        [TearDown]
        public void Teardown()
        {
            // Clean up.
            try
            {
                Promise.Manager.HandleCompletes();
            }
            catch (AggregateException) { }
        }

        [Test]
        public void IfOnFulfilledIsNullThrow_2_2_1_1()
        {
            var deferred = Promise.NewDeferred();

            Assert.AreEqual(Promise.State.Pending, deferred.State);

            var promise = deferred.Promise;

            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () =>
            {
                promise.Then(default(Action));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(default(Func<int>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(default(Func<Promise>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(default(Func<Promise<int>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(default(Func<Promise.Deferred>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(default(Func<Promise<int>.Deferred>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(default(Action), (object failValue) => { });
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(default(Func<int>), (object failValue) => default(int));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(default(Func<Promise>), (object failValue) => default(Promise));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(default(Func<Promise<int>>), (object failValue) => default(Promise<int>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(default(Func<Action<Promise.Deferred>>), (object failValue) => default(Action<Promise.Deferred>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(default(Func<Action<Promise<int>.Deferred>>), (object failValue) => default(Action<Promise<int>.Deferred>));
            });

            deferred.Cancel();

            var deferredInt = Promise.NewDeferred<int>();

            Assert.AreEqual(Promise.State.Pending, deferredInt.State);

            var promiseInt = deferredInt.Promise;

            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then(default(Action<int>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then(default(Func<int, int>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then(default(Func<int, Promise>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then(default(Func<int, Promise<int>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then(default(Func<int, Promise.Deferred>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then(default(Func<int, Promise<int>.Deferred>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then(default(Action<int>), (object failValue) => { });
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then(default(Func<int, int>), (object failValue) => default(int));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then(default(Func<int, Promise>), (object failValue) => default(Promise));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then(default(Func<int, Promise<int>>), (object failValue) => default(Promise<int>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then(default(Func<int, Action<Promise.Deferred>>), (object failValue) => default(Action<Promise.Deferred>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then(default(Func<int, Action<Promise<int>.Deferred>>), (object failValue) => default(Action<Promise<int>.Deferred>));
            });

            deferredInt.Cancel();
        }

        [Test]
        public void IfOnRejectedIsNullThrow_2_2_1_2()
        {
            var deferred = Promise.NewDeferred();

            Assert.AreEqual(Promise.State.Pending, deferred.State);

            var promise = deferred.Promise;

            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Catch(default(Action));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Catch<string>(default(Action));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Catch(default(Action<string>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Catch(default(Func<Promise>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Catch<string>(default(Func<Promise>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Catch(default(Func<string, Promise>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Catch(default(Func<Action<Promise.Deferred>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Catch<string>(default(Func<Action<Promise.Deferred>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Catch(default(Func<string, Action<Promise.Deferred>>));
            });

            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(() => { }, default(Action));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then<string>(() => { }, default(Action));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(() => { }, default(Action<string>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(() => default(Promise), default(Func<Promise>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then<string>(() => default(Promise), default(Func<Promise>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(() => default(Promise), default(Func<string, Promise>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(() => default(Action<Promise.Deferred>), default(Func<Action<Promise.Deferred>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then<string>(() => default(Action<Promise.Deferred>), default(Func<Action<Promise.Deferred>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(() => default(Action<Promise.Deferred>), default(Func<string, Action<Promise.Deferred>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(() => "string", default(Func<string>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then<string, Exception>(() => "string", default(Func<string>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(() => "string", default(Func<Exception, string>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(() => default(Promise<string>), default(Func<Promise<string>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then<string, Exception>(() => default(Promise<string>), default(Func<Promise<string>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(() => default(Promise<string>), default(Func<Exception, Promise<string>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(() => default(Action<Promise<string>.Deferred>), default(Func<Action<Promise<string>.Deferred>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then<string, Exception>(() => default(Action<Promise<string>.Deferred>), default(Func<Action<Promise<string>.Deferred>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promise.Then(() => default(Action<Promise<string>.Deferred>), default(Func<Exception, Action<Promise<string>.Deferred>>));
            });

            deferred.Cancel();

            var deferredInt = Promise.NewDeferred<int>();

            Assert.AreEqual(Promise.State.Pending, deferredInt.State);

            var promiseInt = deferredInt.Promise;

            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Catch(default(Func<int>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Catch<string>(default(Func<int>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Catch(default(Func<string, int>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Catch(default(Func<Promise<int>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Catch<string>(default(Func<Promise<int>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Catch(default(Func<string, Promise<int>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Catch(default(Func<Action<Promise<int>.Deferred>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Catch<string>(default(Func<Action<Promise<int>.Deferred>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Catch(default(Func<string, Action<Promise<int>.Deferred>>));
            });

            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then((int x) => { }, default(Action));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then<string>((int x) => { }, default(Action));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then((int x) => { }, default(Action<string>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then((int x) => default(Promise), default(Func<Promise>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then<string>((int x) => default(Promise), default(Func<Promise>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then((int x) => default(Promise), default(Func<string, Promise>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then((int x) => default(Action<Promise.Deferred>), default(Func<Action<Promise.Deferred>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then<string>(() => default(Action<Promise.Deferred>), default(Func<Action<Promise.Deferred>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then((int x) => default(Action<Promise.Deferred>), default(Func<string, Action<Promise.Deferred>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then((int x) => "string", default(Func<string>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then<string, Exception>((int x) => "string", default(Func<string>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then((int x) => "string", default(Func<Exception, string>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then((int x) => default(Promise<string>), default(Func<Promise<string>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then<string, Exception>((int x) => default(Promise<string>), default(Func<Promise<string>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then((int x) => default(Promise<string>), default(Func<Exception, Promise<string>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then((int x) => default(Action<Promise<string>.Deferred>), default(Func<Action<Promise<string>.Deferred>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then<string, Exception>((int x) => default(Action<Promise<string>.Deferred>), default(Func<Action<Promise<string>.Deferred>>));
            });
            Assert.Throws(typeof(Proto.Promises.ArgumentNullException), () => {
                promiseInt.Then((int x) => default(Action<Promise<string>.Deferred>), default(Func<Exception, Action<Promise<string>.Deferred>>));
            });

            deferredInt.Cancel();
        }
    }
}
