#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using NUnit.Framework;
using Proto.Promises;
using Proto.Promises.Threading;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ProtoPromiseTests.APIs.Threading
{
#if UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
    public class AsyncReaderWriterLockTests
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

        [Flags]
        public enum LockType
        {
            None = 0,
            Reader = 1 << 0,
            Writer = 1 << 1,
            Upgradeable = 1 << 2
        }

        public enum CancelationType
        {
            NoToken,
            Default,
            Canceled,
            Pending
        }

        private static CancelationToken GetToken(CancelationSource cancelationSource, CancelationType cancelationType)
        {
            return cancelationType == CancelationType.Canceled ? CancelationToken.Canceled()
                : cancelationType == CancelationType.Pending ? cancelationSource.Token
                : CancelationToken.None;
        }

        [Test]
        public void AsyncReaderWriterLock_TryEnterReaderLock_ReturnsFalseIfLockIsHeldByWriter(
            [Values(LockType.Writer, LockType.Upgradeable)] LockType lockedType)
        {
            var rwl = new AsyncReaderWriterLock();
            AsyncReaderWriterLock.UpgradeableReaderKey upgraderkey = default;
            AsyncReaderWriterLock.WriterKey writerkey = default;
            if (lockedType == LockType.Upgradeable)
            {
                upgraderkey = rwl.UpgradeableReaderLock();
                writerkey = rwl.UpgradeToWriterLock(upgraderkey);
            }
            else
            {
                writerkey = rwl.WriterLock();
            }
            Assert.False(rwl.TryEnterReaderLock(out _));
            Assert.False(rwl.TryEnterReaderLock(out _, CancelationToken.Canceled()));
            writerkey.Dispose();
            if (lockedType == LockType.Upgradeable)
            {
                upgraderkey.Dispose();
            }
        }

        [Test]
        public void AsyncReaderWriterLock_TryEnterReaderLock_ReturnsTrueIfLockIsNotHeldOrReaderLocked(
            [Values] CancelationType cancelationType,
            [Values] bool isReaderLocked)
        {
            var rwl = new AsyncReaderWriterLock();
            var cancelationSource = CancelationSource.New();
            AsyncReaderWriterLock.ReaderKey key1 = default;
            if (isReaderLocked)
            {
                key1 = rwl.ReaderLock();
            }
            AsyncReaderWriterLock.ReaderKey key2;
            if (cancelationType == CancelationType.NoToken)
            {
                Assert.IsTrue(rwl.TryEnterReaderLock(out key2));
            }
            else
            {
                Assert.IsTrue(rwl.TryEnterReaderLock(out key2, GetToken(cancelationSource, cancelationType)));
            }
            key2.Dispose();
            cancelationSource.Dispose();
            if (isReaderLocked)
            {
                key1.Dispose();
            }
        }

        [Test]
        public void AsyncReaderWriterLock_TryEnterReaderLockAsync_TrueIfLockIsNotHeldOrReaderLocked(
            [Values(CancelationType.Default, CancelationType.Canceled, CancelationType.Pending)] CancelationType cancelationType,
            [Values] bool isReaderLocked)
        {
            var rwl = new AsyncReaderWriterLock();
            var cancelationSource = CancelationSource.New();
            AsyncReaderWriterLock.ReaderKey key1 = default;
            if (isReaderLocked)
            {
                key1 = rwl.ReaderLock();
            }
            rwl.TryEnterReaderLockAsync(GetToken(cancelationSource, cancelationType))
                .Then(tuple =>
                {
                    Assert.True(tuple.didEnter);
                    tuple.readerKey.Dispose();
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
            if (isReaderLocked)
            {
                key1.Dispose();
            }
        }

        [Test]
        public void AsyncReaderWriterLock_TryEnterReaderLockAsync_FalseIfLockIsHeldByWriter(
            [Values(CancelationType.Canceled, CancelationType.Pending)] CancelationType cancelationType,
            [Values(LockType.Writer, LockType.Upgradeable)] LockType lockedType)
        {
            var rwl = new AsyncReaderWriterLock();
            AsyncReaderWriterLock.UpgradeableReaderKey upgraderkey = default;
            AsyncReaderWriterLock.WriterKey writerkey = default;
            if (lockedType == LockType.Upgradeable)
            {
                upgraderkey = rwl.UpgradeableReaderLock();
                writerkey = rwl.UpgradeToWriterLock(upgraderkey);
            }
            else
            {
                writerkey = rwl.WriterLock();
            }

            var cancelationSource = CancelationSource.New();
            var promise = rwl.TryEnterReaderLockAsync(GetToken(cancelationSource, cancelationType))
                .Then(tuple =>
                {
                    Assert.False(tuple.didEnter);
                });
            cancelationSource.Cancel();
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
            writerkey.Dispose();
            if (lockedType == LockType.Upgradeable)
            {
                upgraderkey.Dispose();
            }
        }

        [Test]
        public void AsyncReaderWriterLock_TryEnterReaderLockAsync_TrueIfWriterLockIsReleased(
            [Values(CancelationType.Default, CancelationType.Pending)] CancelationType cancelationType,
            [Values(LockType.Writer, LockType.Upgradeable)] LockType lockedType)
        {
            var rwl = new AsyncReaderWriterLock();
            AsyncReaderWriterLock.UpgradeableReaderKey upgraderkey = default;
            AsyncReaderWriterLock.WriterKey writerkey = default;
            if (lockedType == LockType.Upgradeable)
            {
                upgraderkey = rwl.UpgradeableReaderLock();
                writerkey = rwl.UpgradeToWriterLock(upgraderkey);
            }
            else
            {
                writerkey = rwl.WriterLock();
            }

            var cancelationSource = CancelationSource.New();
            var promise = rwl.TryEnterReaderLockAsync(GetToken(cancelationSource, cancelationType))
                .Then(tuple =>
                {
                    Assert.True(tuple.didEnter);
                    tuple.readerKey.Dispose();
                });
            writerkey.Dispose();
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
            if (lockedType == LockType.Upgradeable)
            {
                upgraderkey.Dispose();
            }
        }

        [Test]
        public void AsyncReaderWriterLock_TryEnterWriterLock_ReturnsFalseIfLockIsHeld(
            [Values(LockType.Reader, LockType.Writer, LockType.Upgradeable)] LockType lockedType)
        {
            var rwl = new AsyncReaderWriterLock();
            AsyncReaderWriterLock.ReaderKey readerKey = default;
            AsyncReaderWriterLock.WriterKey writerkey = default;
            AsyncReaderWriterLock.UpgradeableReaderKey upgraderkey = default;
            if (lockedType == LockType.Reader)
            {
                readerKey = rwl.ReaderLock();
            }
            else if (lockedType == LockType.Writer)
            {
                writerkey = rwl.WriterLock();
            }
            else
            {
                upgraderkey = rwl.UpgradeableReaderLock();
            }
            Assert.False(rwl.TryEnterWriterLock(out _));
            Assert.False(rwl.TryEnterWriterLock(out _, CancelationToken.Canceled()));
            if (lockedType == LockType.Reader)
            {
                readerKey.Dispose();
            }
            else if (lockedType == LockType.Writer)
            {
                writerkey.Dispose();
            }
            else
            {
                upgraderkey.Dispose();
            }
        }

        [Test]
        public void AsyncReaderWriterLock_TryEnterWriterLock_ReturnsTrueIfLockIsNotHeld(
            [Values] CancelationType cancelationType)
        {
            var rwl = new AsyncReaderWriterLock();
            var cancelationSource = CancelationSource.New();
            AsyncReaderWriterLock.WriterKey key;
            if (cancelationType == CancelationType.NoToken)
            {
                Assert.IsTrue(rwl.TryEnterWriterLock(out key));
            }
            else
            {
                Assert.IsTrue(rwl.TryEnterWriterLock(out key, GetToken(cancelationSource, cancelationType)));
            }
            key.Dispose();
            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_TryEnterWriterLockAsync_TrueIfLockIsNotHeld(
            [Values(CancelationType.Default, CancelationType.Canceled, CancelationType.Pending)] CancelationType cancelationType)
        {
            var rwl = new AsyncReaderWriterLock();
            var cancelationSource = CancelationSource.New();
            rwl.TryEnterWriterLockAsync(GetToken(cancelationSource, cancelationType))
                .Then(tuple =>
                {
                    Assert.True(tuple.didEnter);
                    tuple.writerKey.Dispose();
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_TryEnterWriterLockAsync_FalseIfLockIsHeld(
            [Values(CancelationType.Canceled, CancelationType.Pending)] CancelationType cancelationType,
            [Values(LockType.Reader, LockType.Writer, LockType.Upgradeable)] LockType lockedType)
        {
            var rwl = new AsyncReaderWriterLock();
            AsyncReaderWriterLock.ReaderKey readerKey = default;
            AsyncReaderWriterLock.WriterKey writerkey = default;
            AsyncReaderWriterLock.UpgradeableReaderKey upgraderkey = default;
            if (lockedType == LockType.Reader)
            {
                readerKey = rwl.ReaderLock();
            }
            else if (lockedType == LockType.Writer)
            {
                writerkey = rwl.WriterLock();
            }
            else
            {
                upgraderkey = rwl.UpgradeableReaderLock();
            }

            var cancelationSource = CancelationSource.New();
            var promise = rwl.TryEnterWriterLockAsync(GetToken(cancelationSource, cancelationType))
                .Then(tuple =>
                {
                    Assert.False(tuple.didEnter);
                });
            cancelationSource.Cancel();
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
            if (lockedType == LockType.Reader)
            {
                readerKey.Dispose();
            }
            else if (lockedType == LockType.Writer)
            {
                writerkey.Dispose();
            }
            else
            {
                upgraderkey.Dispose();
            }
        }

        [Test]
        public void AsyncReaderWriterLock_TryEnterWriterLockAsync_TrueIfLockIsReleased(
            [Values(CancelationType.Default, CancelationType.Pending)] CancelationType cancelationType,
            [Values(LockType.Reader, LockType.Writer, LockType.Upgradeable)] LockType lockedType)
        {
            var rwl = new AsyncReaderWriterLock();
            AsyncReaderWriterLock.ReaderKey readerKey = default;
            AsyncReaderWriterLock.WriterKey writerkey = default;
            AsyncReaderWriterLock.UpgradeableReaderKey upgraderkey = default;
            if (lockedType == LockType.Reader)
            {
                readerKey = rwl.ReaderLock();
            }
            else if (lockedType == LockType.Writer)
            {
                writerkey = rwl.WriterLock();
            }
            else
            {
                upgraderkey = rwl.UpgradeableReaderLock();
            }

            var cancelationSource = CancelationSource.New();
            var promise = rwl.TryEnterWriterLockAsync(GetToken(cancelationSource, cancelationType))
                .Then(tuple =>
                {
                    Assert.True(tuple.didEnter);
                    tuple.writerKey.Dispose();
                });
            if (lockedType == LockType.Reader)
            {
                readerKey.Dispose();
            }
            else if (lockedType == LockType.Writer)
            {
                writerkey.Dispose();
            }
            else
            {
                upgraderkey.Dispose();
            }
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_TryEnterUpgradeableReaderLock_ReturnsFalseIfLockIsHeldByWriterOrUpgradeableReader(
            [Values(LockType.Writer, LockType.Upgradeable)] LockType lockedType)
        {
            var rwl = new AsyncReaderWriterLock();
            AsyncReaderWriterLock.UpgradeableReaderKey upgraderkey = default;
            AsyncReaderWriterLock.WriterKey writerkey = default;
            if (lockedType == LockType.Upgradeable)
            {
                upgraderkey = rwl.UpgradeableReaderLock();
            }
            else
            {
                writerkey = rwl.WriterLock();
            }
            Assert.False(rwl.TryEnterUpgradeableReaderLock(out _));
            Assert.False(rwl.TryEnterUpgradeableReaderLock(out _, CancelationToken.Canceled()));
            if (lockedType == LockType.Upgradeable)
            {
                upgraderkey.Dispose();
            }
            else
            {
                writerkey.Dispose();
            }
        }

        [Test]
        public void AsyncReaderWriterLock_TryEnterUpgradeableReaderLock_ReturnsTrueIfLockIsNotHeldOrReaderLocked(
            [Values] CancelationType cancelationType,
            [Values] bool isReaderLocked)
        {
            var rwl = new AsyncReaderWriterLock();
            var cancelationSource = CancelationSource.New();
            AsyncReaderWriterLock.ReaderKey key1 = default;
            if (isReaderLocked)
            {
                key1 = rwl.ReaderLock();
            }
            AsyncReaderWriterLock.UpgradeableReaderKey key2;
            if (cancelationType == CancelationType.NoToken)
            {
                Assert.IsTrue(rwl.TryEnterUpgradeableReaderLock(out key2));
            }
            else
            {
                Assert.IsTrue(rwl.TryEnterUpgradeableReaderLock(out key2, GetToken(cancelationSource, cancelationType)));
            }
            key2.Dispose();
            cancelationSource.Dispose();
            if (isReaderLocked)
            {
                key1.Dispose();
            }
        }

        [Test]
        public void AsyncReaderWriterLock_TryEnterUpgradeableReaderLockAsync_TrueIfLockIsNotHeldOrReaderLocked(
            [Values(CancelationType.Default, CancelationType.Canceled, CancelationType.Pending)] CancelationType cancelationType,
            [Values] bool isReaderLocked)
        {
            var rwl = new AsyncReaderWriterLock();
            var cancelationSource = CancelationSource.New();
            AsyncReaderWriterLock.ReaderKey key1 = default;
            if (isReaderLocked)
            {
                key1 = rwl.ReaderLock();
            }
            rwl.TryEnterUpgradeableReaderLockAsync(GetToken(cancelationSource, cancelationType))
                .Then(tuple =>
                {
                    Assert.True(tuple.didEnter);
                    tuple.readerKey.Dispose();
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
            if (isReaderLocked)
            {
                key1.Dispose();
            }
        }

        [Test]
        public void AsyncReaderWriterLock_TryEnterUpgradeableReaderLockAsync_FalseIfLockIsHeldByWriterOrUpgradeableReader(
            [Values(CancelationType.Canceled, CancelationType.Pending)] CancelationType cancelationType,
            [Values(LockType.Writer, LockType.Upgradeable)] LockType lockedType)
        {
            var rwl = new AsyncReaderWriterLock();
            AsyncReaderWriterLock.UpgradeableReaderKey upgraderkey = default;
            AsyncReaderWriterLock.WriterKey writerkey = default;
            if (lockedType == LockType.Upgradeable)
            {
                upgraderkey = rwl.UpgradeableReaderLock();
                writerkey = rwl.UpgradeToWriterLock(upgraderkey);
            }
            else
            {
                writerkey = rwl.WriterLock();
            }

            var cancelationSource = CancelationSource.New();
            var promise = rwl.TryEnterUpgradeableReaderLockAsync(GetToken(cancelationSource, cancelationType))
                .Then(tuple =>
                {
                    Assert.False(tuple.didEnter);
                });
            cancelationSource.Cancel();
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
            writerkey.Dispose();
            if (lockedType == LockType.Upgradeable)
            {
                upgraderkey.Dispose();
            }
        }

        [Test]
        public void AsyncReaderWriterLock_TryEnterUpgradeableReaderLockAsync_TrueIfWriterOrUpgradeableReaderLockIsReleased(
            [Values(CancelationType.Default, CancelationType.Pending)] CancelationType cancelationType,
            [Values(LockType.Writer, LockType.Upgradeable)] LockType lockedType)
        {
            var rwl = new AsyncReaderWriterLock();
            AsyncReaderWriterLock.UpgradeableReaderKey upgraderkey = default;
            AsyncReaderWriterLock.WriterKey writerkey = default;
            if (lockedType == LockType.Upgradeable)
            {
                upgraderkey = rwl.UpgradeableReaderLock();
                writerkey = rwl.UpgradeToWriterLock(upgraderkey);
            }
            else
            {
                writerkey = rwl.WriterLock();
            }

            var cancelationSource = CancelationSource.New();
            var promise = rwl.TryEnterUpgradeableReaderLockAsync(GetToken(cancelationSource, cancelationType))
                .Then(tuple =>
                {
                    Assert.True(tuple.didEnter);
                    tuple.readerKey.Dispose();
                });
            writerkey.Dispose();
            if (lockedType == LockType.Upgradeable)
            {
                upgraderkey.Dispose();
            }
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_TryUpgradeToWriterLock_ReturnsFalseIfReaderLockIsHeld()
        {
            var rwl = new AsyncReaderWriterLock();
            var upgraderkey = rwl.UpgradeableReaderLock();
            var readerKey = rwl.ReaderLock();
            Assert.False(rwl.TryUpgradeToWriterLock(upgraderkey, out _));
            Assert.False(rwl.TryUpgradeToWriterLock(upgraderkey, out _, CancelationToken.Canceled()));
            upgraderkey.Dispose();
            readerKey.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_TryUpgradeToWriterLock_ReturnsTrueIfLockIsNotHeld(
            [Values] CancelationType cancelationType)
        {
            var rwl = new AsyncReaderWriterLock();
            var cancelationSource = CancelationSource.New();
            var upgraderkey = rwl.UpgradeableReaderLock();
            AsyncReaderWriterLock.WriterKey key;
            if (cancelationType == CancelationType.NoToken)
            {
                Assert.IsTrue(rwl.TryUpgradeToWriterLock(upgraderkey, out key));
            }
            else
            {
                Assert.IsTrue(rwl.TryUpgradeToWriterLock(upgraderkey, out key, GetToken(cancelationSource, cancelationType)));
            }
            key.Dispose();
            upgraderkey.Dispose();
            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_TryUpgradeToWriterLockAsync_TrueIfLockIsNotHeld(
            [Values(CancelationType.Default, CancelationType.Canceled, CancelationType.Pending)] CancelationType cancelationType)
        {
            var rwl = new AsyncReaderWriterLock();
            var cancelationSource = CancelationSource.New();
            var upgraderkey = rwl.UpgradeableReaderLock();
            rwl.TryUpgradeToWriterLockAsync(upgraderkey, GetToken(cancelationSource, cancelationType))
                .Then(tuple =>
                {
                    Assert.True(tuple.didEnter);
                    tuple.writerKey.Dispose();
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            upgraderkey.Dispose();
            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_TryUpgradeToWriterLockAsync_FalseIfReaderLockIsHeld(
            [Values(CancelationType.Canceled, CancelationType.Pending)] CancelationType cancelationType)
        {
            var rwl = new AsyncReaderWriterLock();
            var upgraderkey = rwl.UpgradeableReaderLock();
            var readerKey = rwl.ReaderLock();

            var cancelationSource = CancelationSource.New();
            var promise = rwl.TryUpgradeToWriterLockAsync(upgraderkey, GetToken(cancelationSource, cancelationType))
                .Then(tuple =>
                {
                    Assert.False(tuple.didEnter);
                });
            cancelationSource.Cancel();
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
            upgraderkey.Dispose();
            readerKey.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_TryUpgradeToWriterLockAsync_TrueIfReaderLockIsReleased(
            [Values(CancelationType.Default, CancelationType.Pending)] CancelationType cancelationType)
        {
            var rwl = new AsyncReaderWriterLock();
            var upgraderkey = rwl.UpgradeableReaderLock();
            var readerKey = rwl.ReaderLock();

            var cancelationSource = CancelationSource.New();
            var promise = rwl.TryUpgradeToWriterLockAsync(upgraderkey, GetToken(cancelationSource, cancelationType))
                .Then(tuple =>
                {
                    Assert.True(tuple.didEnter);
                    tuple.writerKey.Dispose();
                });
            readerKey.Dispose();
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
            upgraderkey.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_ReaderLockIsEnteredWhenWriterLockIsCanceled(
            [Values(LockType.Writer, LockType.Upgradeable)] LockType lockedType)
        {
            var rwl = new AsyncReaderWriterLock();
            var cancelationSource = CancelationSource.New();

            var readerkey = rwl.ReaderLock();

            if (lockedType == LockType.Writer)
            {
                rwl.WriterLockAsync(cancelationSource.Token)
                    .Forget();
            }
            else
            {
                var upgraderKey = rwl.UpgradeableReaderLock();
                rwl.UpgradeToWriterLockAsync(upgraderKey, cancelationSource.Token)
                    .Finally(upgraderKey.Dispose)
                    .Forget();
            }

            var promise = rwl.ReaderLockAsync()
                .Then(key => key.Dispose());

            cancelationSource.Cancel();
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            readerkey.Dispose();
            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReaderLockIsEnteredWhenWriterLockIsCanceled()
        {
            var rwl = new AsyncReaderWriterLock();
            var cancelationSource = CancelationSource.New();

            var readerkey = rwl.ReaderLock();

            rwl.WriterLockAsync(cancelationSource.Token)
                    .Forget();

            var promise = rwl.UpgradeableReaderLockAsync()
                .Then(key => key.Dispose());

            cancelationSource.Cancel();
            promise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            readerkey.Dispose();
            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_WriterLockIsEnteredAfterLockIsCanceled_AndPreviousWriterLockIsExited(
            [Values(LockType.Reader, LockType.Writer, LockType.Upgradeable)] LockType lockedType)
        {
            var rwl = new AsyncReaderWriterLock();
            var cancelationSource = CancelationSource.New();

            var writerKey = rwl.WriterLock();

            if (lockedType == LockType.Reader)
            {
                rwl.ReaderLockAsync(cancelationSource.Token)
                    .Forget();
            }
            else if (lockedType == LockType.Writer)
            {
                rwl.WriterLockAsync(cancelationSource.Token)
                    .Forget();
            }
            else
            {
                rwl.UpgradeableReaderLockAsync(cancelationSource.Token)
                    .Forget();
            }
            cancelationSource.Cancel();
            writerKey.Dispose();

            rwl.WriterLockAsync()
                .Then(key => key.Dispose())
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_ReaderLockIsEnteredAfterWriterLockIsCanceled(
            [Values(LockType.Writer, LockType.Upgradeable)] LockType lockedType)
        {
            var rwl = new AsyncReaderWriterLock();
            var cancelationSource = CancelationSource.New();

            var readerKey = rwl.ReaderLock();

            if (lockedType == LockType.Writer)
            {
                rwl.WriterLockAsync(cancelationSource.Token)
                    .Forget();
            }
            else
            {
                var upgraderKey = rwl.UpgradeableReaderLock();
                rwl.UpgradeToWriterLockAsync(upgraderKey, cancelationSource.Token)
                    .Finally(upgraderKey.Dispose)
                    .Forget();
            }
            cancelationSource.Cancel();

            rwl.ReaderLockAsync()
                .Then(key => key.Dispose())
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            readerKey.Dispose();
            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReaderLockIsEnteredAFterLockIsCanceled_AndPreviousUpgradeableReaderLockIsExited(
            [Values(LockType.Reader, LockType.Writer, LockType.Upgradeable)] LockType lockedType)
        {
            var rwl = new AsyncReaderWriterLock();
            var cancelationSource = CancelationSource.New();

            var readerKey = rwl.UpgradeableReaderLock();
            var writerKey = rwl.UpgradeToWriterLock(readerKey);

            if (lockedType == LockType.Reader)
            {
                rwl.ReaderLockAsync(cancelationSource.Token)
                    .Forget();
            }
            else if (lockedType == LockType.Writer)
            {
                rwl.WriterLockAsync(cancelationSource.Token)
                    .Forget();
            }
            else
            {
                rwl.UpgradeableReaderLockAsync(cancelationSource.Token)
                    .Forget();
            }
            cancelationSource.Cancel();
            writerKey.Dispose();
            readerKey.Dispose();

            rwl.UpgradeableReaderLockAsync()
                .Then(key => key.Dispose())
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_Unlocked_PermitsWriterLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync()) { }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncReaderWriterLock_Unlocked_PermitsWriterLock_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            var key = rwl.WriterLock();
            key.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_Unlocked_PermitsTryWriterLock_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            bool entered = rwl.TryEnterWriterLock(out var key);
            Assert.True(entered);
            key.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_Unlocked_PermitsMultipleReaderLocks_Async()
        {
            int lockHolders = 0;
            var rwl = new AsyncReaderWriterLock();

            var readerPromise1 = rwl.ReaderLockAsync()
                .Then(key =>
                {
                    Interlocked.Increment(ref lockHolders);
                    return key;
                });
            var readerPromise2 = rwl.ReaderLockAsync()
                .Then(key =>
                {
                    Interlocked.Increment(ref lockHolders);
                    return key;
                });

            Assert.AreEqual(2, lockHolders);

            readerPromise1
                .Then(key => key.Dispose())
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
            readerPromise2
                .Then(key => key.Dispose())
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncReaderWriterLock_Unlocked_PermitsMultipleReaderLocks_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            var key1 = rwl.ReaderLock();
            var key2 = rwl.ReaderLock();
            key1.Dispose();
            key2.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_Unlocked_PermitsMultipleTryReaderLocks_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            bool entered1 = rwl.TryEnterReaderLock(out var key1);
            bool entered2 = rwl.TryEnterReaderLock(out var key2);
            Assert.True(entered1);
            Assert.True(entered2);
            key1.Dispose();
            key2.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_Unlocked_PermitsUpgradeableReaderLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync()) { }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncReaderWriterLock_Unlocked_PermitsUpgradeableReaderLock_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            var key = rwl.UpgradeableReaderLock();
            key.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_Unlocked_PermitsTryUpgradeableReaderLock_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            bool entered = rwl.TryEnterUpgradeableReaderLock(out var key);
            Assert.True(entered);
            key.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReaderLocked_PermitsUpgradeToWriterLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();

            Promise.Run(async () =>
            {
                using (var readerKey = await rwl.UpgradeableReaderLockAsync())
                {
                    using (await rwl.UpgradeToWriterLockAsync(readerKey)) { }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReaderLocked_PermitsUpgradeToWriterLock_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            var readerKey = rwl.UpgradeableReaderLock();
            var writerKey = rwl.UpgradeToWriterLock(readerKey);
            writerKey.Dispose();
            readerKey.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReaderLocked_PermitsTryUpgradeToWriterLock_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            var readerKey = rwl.UpgradeableReaderLock();
            bool isUpgraded = rwl.TryUpgradeToWriterLock(readerKey, out var writerKey);
            Assert.True(isUpgraded);
            writerKey.Dispose();
            readerKey.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReaderLocked_PermitsReaderLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync())
                {
                    using (await rwl.ReaderLockAsync()) { }
                }
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeout(TimeSpan.FromSeconds(1));
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReaderLocked_PermitsReaderLock_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            var upgradeableReaderKey = rwl.UpgradeableReaderLock();
            var readerKey = rwl.ReaderLock();
            readerKey.Dispose();
            upgradeableReaderKey.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReaderLocked_PermitsTryReaderLock_Sync()
        {
            var rwl = new AsyncReaderWriterLock();

            var upgradeableReaderKey = rwl.UpgradeableReaderLock();
            bool isUpgraded = rwl.TryEnterReaderLock(out var readerKey);
            Assert.True(isUpgraded);
            readerKey.Dispose();
            upgradeableReaderKey.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_WriterKey_DoubleDisposeThrows()
        {
            var rwl = new AsyncReaderWriterLock();

            bool didThrow = false;
            rwl.WriterLockAsync()
                .Then(key =>
                {
                    key.Dispose();
                    key.Dispose();
                })
                .Catch((System.InvalidOperationException e) => didThrow = true)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.True(didThrow);
        }

        [Test]
        public void AsyncReaderWriterLock_ReaderKey_DoubleDisposeThrows()
        {
            var rwl = new AsyncReaderWriterLock();

            bool didThrow = false;
            rwl.ReaderLockAsync()
                .Then(key =>
                {
                    key.Dispose();
                    key.Dispose();
                })
                // In DEBUG mode, InvalidOperationException is thrown. In RELEASE mode, it only checks for underflow.
                .Catch((System.InvalidOperationException e) => didThrow = true)
                .Catch((OverflowException e) => didThrow = true)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.True(didThrow);
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReaderKey_DoubleDisposeThrows()
        {
            var rwl = new AsyncReaderWriterLock();

            bool didThrow = false;
            rwl.ReaderLockAsync()
                .Then(key =>
                {
                    key.Dispose();
                    key.Dispose();
                })
                // In DEBUG mode, InvalidOperationException is thrown. In RELEASE mode, it only checks for underflow.
                .Catch((System.InvalidOperationException e) => didThrow = true)
                .Catch((OverflowException e) => didThrow = true)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.True(didThrow);
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradedWriterKey_DoubleDisposeThrows()
        {
            var rwl = new AsyncReaderWriterLock();

            bool didThrow = false;
            rwl.UpgradeableReaderLockAsync()
                .Then(readerKey =>
                    rwl.UpgradeToWriterLockAsync(readerKey)
                        .Then(writerKey =>
                        {
                            writerKey.Dispose();
                            writerKey.Dispose();
                        })
                        .Catch((System.InvalidOperationException e) => didThrow = true)
                        .Finally(() => readerKey.Dispose())
                )
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.True(didThrow);
        }

        [Test]
        public void AsyncReaderWriterLock_PreCanceledWriterLock_Unlocked_SynchronouslyCanceled()
        {
            var rwl = new AsyncReaderWriterLock();
            var token = CancelationToken.Canceled();
            bool canceled = false;

            rwl.WriterLockAsync(token)
                .CatchCancelation(() => canceled = true)
                .Forget();

            Assert.True(canceled);
            Assert.Catch<OperationCanceledException>(() => rwl.WriterLock(token));
        }

        [Test]
        public void AsyncReaderWriterLock_PreCanceledReaderLock_Unlocked_SynchronouslyCanceled()
        {
            var rwl = new AsyncReaderWriterLock();
            var token = CancelationToken.Canceled();
            bool canceled = false;

            rwl.ReaderLockAsync(token)
                .CatchCancelation(() => canceled = true)
                .Forget();

            Assert.True(canceled);
            Assert.Catch<OperationCanceledException>(() => rwl.ReaderLock(token));
        }

        [Test]
        public void AsyncReaderWriterLock_PreCanceledUpgradeableReaderLock_Unlocked_SynchronouslyCanceled()
        {
            var rwl = new AsyncReaderWriterLock();
            var token = CancelationToken.Canceled();
            bool canceled = false;

            rwl.UpgradeableReaderLockAsync(token)
                .CatchCancelation(() => canceled = true)
                .Forget();

            Assert.True(canceled);
            Assert.Catch<OperationCanceledException>(() => rwl.UpgradeableReaderLock(token));
        }

        [Test]
        public void AsyncReaderWriterLock_PreCanceledUpgradeWriterLock_Unlocked_SynchronouslyCanceled()
        {
            var rwl = new AsyncReaderWriterLock();
            var token = CancelationToken.Canceled();
            bool canceled = false;

            Promise.Run(async () =>
            {
                using (var key = await rwl.UpgradeableReaderLockAsync())
                {
                    using (await rwl.UpgradeToWriterLockAsync(key, token)) { }
                }
            }, SynchronizationOption.Synchronous)
                .CatchCancelation(() => canceled = true)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.True(canceled);
            Assert.Catch<OperationCanceledException>(() =>
            {
                using (var key = rwl.UpgradeableReaderLock())
                {
                    using (rwl.UpgradeToWriterLock(key, token)) { }
                }
            });
        }

        [Test]
        public void AsyncReaderWriterLock_PreCanceledWriterLock_Locked_SynchronouslyCancels()
        {
            var rwl = new AsyncReaderWriterLock();

            var lockPromise = rwl.WriterLockAsync();
            var token = CancelationToken.Canceled();

            var promise = rwl.WriterLockAsync(token);

            Promise.State state = Promise.State.Pending;
            promise
                .ContinueWith(r => state = r.State)
                .Forget();
            Assert.AreEqual(Promise.State.Canceled, state);

            lockPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1)).Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_PreCanceledReaderLock_Locked_SynchronouslyCancels()
        {
            var rwl = new AsyncReaderWriterLock();

            var lockPromise = rwl.WriterLockAsync();
            var token = CancelationToken.Canceled();

            var promise = rwl.ReaderLockAsync(token);

            Promise.State state = Promise.State.Pending;
            promise
                .ContinueWith(r => state = r.State)
                .Forget();
            Assert.AreEqual(Promise.State.Canceled, state);

            lockPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1)).Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_PreCanceledUpgradeableReaderLock_Locked_SynchronouslyCancels()
        {
            var rwl = new AsyncReaderWriterLock();

            var lockPromise = rwl.WriterLockAsync();
            var token = CancelationToken.Canceled();

            var promise = rwl.UpgradeableReaderLockAsync(token);

            Promise.State state = Promise.State.Pending;
            promise
                .ContinueWith(r => state = r.State)
                .Forget();
            Assert.AreEqual(Promise.State.Canceled, state);

            lockPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1)).Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_PreCanceledUpgradeWriterLock_ReaderLocked_SynchronouslyCancels()
        {
            var rwl = new AsyncReaderWriterLock();

            var lockPromise = rwl.ReaderLockAsync();
            var token = CancelationToken.Canceled();

            Promise.State state = Promise.State.Pending;
            Promise.Run(async () =>
            {
                using (var key = await rwl.UpgradeableReaderLockAsync())
                {
                    using (await rwl.UpgradeToWriterLockAsync(key, token)) { }
                }
            }, SynchronizationOption.Synchronous)
                .ContinueWith(r => state = r.State)
                .Forget();

            Assert.AreEqual(Promise.State.Canceled, state);

            lockPromise.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1)).Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_WriteLocked_PreventsAnotherWriterLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    isLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isLocked);

            bool promise2IsComplete = false;

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync()) { }
                promise2IsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(promise2IsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(promise2IsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_WriteLocked_PreventsReaderLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    isLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isLocked);

            bool promise2IsComplete = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync()) { }
                promise2IsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(promise2IsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(promise2IsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_WriteLocked_PreventsUpgradeableReaderLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    isLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isLocked);

            bool promise2IsComplete = false;

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync()) { }
                promise2IsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(promise2IsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(promise2IsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_ReadLocked_PreventsWriterLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    isReaderLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isReaderLocked);

            bool writerIsComplete = false;

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync()) { }
                writerIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(writerIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(writerIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_ReadLocked_EnterWriterLock_PreventsAnotherReaderLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    isReaderLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            bool isWriterLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    isWriterLocked = true;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isReaderLocked);
            Assert.False(isWriterLocked);

            bool secondReaderIsComplete = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync()) { }
                secondReaderIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(secondReaderIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isWriterLocked);
            Assert.True(secondReaderIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_ReadLocked_EnterWriterLock_PreventsUpgradeableReaderLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    isReaderLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            bool isWriterLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    isWriterLocked = true;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isReaderLocked);
            Assert.False(isWriterLocked);

            bool secondReaderIsComplete = false;

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync()) { }
                secondReaderIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(secondReaderIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isWriterLocked);
            Assert.True(secondReaderIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_ReadLocked_EnterUpgradeableReaderLock_PreventsWriterLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    isReaderLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            bool isUpgradeableReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync())
                {
                    isUpgradeableReaderLocked = true;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isReaderLocked);
            Assert.True(isUpgradeableReaderLocked);

            bool writerIsComplete = false;

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync()) { }
                writerIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(writerIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(writerIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReadLocked_PreventsWriterLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isUpgradeableReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync())
                {
                    isUpgradeableReaderLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isUpgradeableReaderLocked);

            bool writerIsComplete = false;

            Promise.Run(async () =>
            {
                var promise = rwl.WriterLockAsync();
                using (await promise) { }
                writerIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(writerIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(writerIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReadLocked_PreventsAnotherUpgradeableReaderLockUntilReleased_NoUpgrade_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isUpgradeableReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync())
                {
                    isUpgradeableReaderLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isUpgradeableReaderLocked);

            bool secondUpgradeableReaderIsComplete = false;

            Promise.Run(async () =>
            {
                var promise = rwl.UpgradeableReaderLockAsync();
                using (await promise) { }
                secondUpgradeableReaderIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(secondUpgradeableReaderIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(secondUpgradeableReaderIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReadLocked_PreventsAnotherUpgradeableReaderLockUntilReleased_AfterDowngrade_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isUpgradeableReaderDowngradedLocked = false;

            Promise.Run(async () =>
            {
                using (var key = await rwl.UpgradeableReaderLockAsync())
                {
                    using (await rwl.UpgradeToWriterLockAsync(key)) { }
                    isUpgradeableReaderDowngradedLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isUpgradeableReaderDowngradedLocked);

            bool secondUpgradeableReaderIsComplete = false;

            Promise.Run(async () =>
            {
                var promise = rwl.UpgradeableReaderLockAsync();
                using (await promise) { }
                secondUpgradeableReaderIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(secondUpgradeableReaderIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(secondUpgradeableReaderIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradedWriterLocked_PreventsAnotherUpgradeableReaderLockUntilUpgradeableReaderLockReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isUpgradedWriterLocked = false;

            Promise.Run(async () =>
            {
                using (var key = await rwl.UpgradeableReaderLockAsync())
                {
                    isUpgradedWriterLocked = true;
                    using (await rwl.UpgradeToWriterLockAsync(key))
                    {
                        await deferred1Continue.Promise;
                    }
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isUpgradedWriterLocked);

            bool secondUpgradeableReaderIsComplete = false;

            Promise.Run(async () =>
            {
                var promise = rwl.UpgradeableReaderLockAsync();
                using (await promise) { }
                secondUpgradeableReaderIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(secondUpgradeableReaderIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(secondUpgradeableReaderIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradeableReaderLocksTakeLockInOrder_AfterNormalLockWithPendingReaderLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred = Promise.NewDeferred();
            var promise = deferred.Promise.Preserve();

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    await promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            bool enteredReadLock = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    enteredReadLock = true;
                    await promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            bool enteredFirstUpgradeableReadLock = false;
            bool exitedFirstUpgradeableReadLock = false;

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync())
                {
                    enteredFirstUpgradeableReadLock = true;
                    await promise;
                    exitedFirstUpgradeableReadLock = true;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            Assert.False(enteredReadLock);
            Assert.False(enteredFirstUpgradeableReadLock);

            var d = deferred;
            deferred = Promise.NewDeferred();
            promise.Forget();
            promise = deferred.Promise.Preserve();
            d.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();

            Assert.True(enteredReadLock);
            Assert.True(enteredFirstUpgradeableReadLock);
            Assert.False(exitedFirstUpgradeableReadLock);

            bool enteredSecondUpgradeableReadLock = false;

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync())
                {
                    enteredSecondUpgradeableReadLock = true;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            Assert.False(exitedFirstUpgradeableReadLock);
            Assert.False(enteredSecondUpgradeableReadLock);

            deferred.Resolve();
            promise.Forget();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
        }

        [Test]
        public void AsyncReaderWriterLock_UpgradedWriterLocked_PreventsReaderLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isUpgradedWriterLocked = false;

            Promise.Run(async () =>
            {
                using (var key = await rwl.UpgradeableReaderLockAsync())
                {
                    using (await rwl.UpgradeToWriterLockAsync(key))
                    {
                        isUpgradedWriterLocked = true;
                        await deferred1Continue.Promise;
                    }
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isUpgradedWriterLocked);

            bool readerIsComplete = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync()) { }
                readerIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(readerIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(readerIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_ReadLocked_UpgradeToWriterLock_PreventsAnotherReaderLockUntilReleased_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred1Continue = Promise.NewDeferred();

            bool isReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    isReaderLocked = true;
                    await deferred1Continue.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            bool isUpgradedWriterLocked = false;

            Promise.Run(async () =>
            {
                using (var key = await rwl.UpgradeableReaderLockAsync())
                {
                    using (await rwl.UpgradeToWriterLockAsync(key))
                    {
                        isUpgradedWriterLocked = true;
                    }
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isReaderLocked);
            Assert.False(isUpgradedWriterLocked);

            bool secondReaderIsComplete = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync()) { }
                secondReaderIsComplete = true;
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(secondReaderIsComplete);

            deferred1Continue.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isUpgradedWriterLocked);
            Assert.True(secondReaderIsComplete);
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledWriterLock_CancelsPromise()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();
            bool canceled = false;

            rwl.WriterLockAsync()
                .Then(key =>
                {
                    var canceledLockPromise = rwl.WriterLockAsync(cts.Token);
                    cts.Cancel();

                    key.Dispose();
                    return canceledLockPromise
                        .CatchCancelation(() => canceled = true);
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
            Assert.True(canceled);
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledReaderLock_CancelsPromise()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();
            bool canceled = false;

            rwl.WriterLockAsync()
                .Then(key =>
                {
                    var canceledLockPromise = rwl.ReaderLockAsync(cts.Token);
                    cts.Cancel();

                    key.Dispose();
                    return canceledLockPromise
                        .CatchCancelation(() => canceled = true);
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
            Assert.True(canceled);
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledReaderLock_AllowsWriterLock()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();
            bool canceled = false;

            rwl.WriterLockAsync()
                .Then(key =>
                {
                    var canceledLockPromise = rwl.ReaderLockAsync(cts.Token);
                    cts.Cancel();

                    key.Dispose();
                    return canceledLockPromise
                        .CatchCancelation(() => canceled = true);
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
            Assert.True(canceled);

            Assert.True(rwl.TryEnterWriterLock(out var writerKey));
            writerKey.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledUpgradeableReaderLock_CancelsPromise()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();
            bool canceled = false;

            rwl.WriterLockAsync()
                .Then(key =>
                {
                    var canceledLockPromise = rwl.UpgradeableReaderLockAsync(cts.Token);
                    cts.Cancel();

                    key.Dispose();
                    return canceledLockPromise
                        .CatchCancelation(() => canceled = true);
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
            Assert.True(canceled);
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledUpgradeableReaderLock_AllowsReaderLock()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();
            Promise<AsyncReaderWriterLock.ReaderKey> readerLockPromise = default;

            rwl.WriterLockAsync()
                .Then(key =>
                {
                    var canceledLockPromise = rwl.UpgradeableReaderLockAsync(cts.Token);
                    readerLockPromise = rwl.ReaderLockAsync();
                    cts.Cancel();

                    key.Dispose();
                    return canceledLockPromise.CatchCancelation(() => { });
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            bool tookReaderLock = false;
            readerLockPromise.Then(key =>
            {
                tookReaderLock = true;
                key.Dispose();
            })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.True(tookReaderLock);
            cts.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledUpgradeableReaderLock_AllowsWriterLock()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();
            Promise<AsyncReaderWriterLock.WriterKey> writerLockPromise = default;

            rwl.WriterLockAsync()
                .Then(key =>
                {
                    var canceledLockPromise = rwl.UpgradeableReaderLockAsync(cts.Token);
                    writerLockPromise = rwl.WriterLockAsync();
                    cts.Cancel();

                    key.Dispose();
                    return canceledLockPromise.CatchCancelation(() => { });
                })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            bool tookWriterLock = false;
            writerLockPromise.Then(key =>
            {
                tookWriterLock = true;
                key.Dispose();
            })
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.True(tookWriterLock);
            cts.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledUpgradedWriterLock_CancelsPromise()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();
            bool canceled = false;

            Promise.Run(async () =>
            {
                Promise canceledPromise;
                using (var upgradeableReaderKey = await rwl.UpgradeableReaderLockAsync())
                {
                    using (await rwl.ReaderLockAsync())
                    {
                        canceledPromise = rwl.UpgradeToWriterLockAsync(upgradeableReaderKey, cts.Token)
                            .CatchCancelation(() => canceled = true);
                        cts.Cancel();
                    }
                }
                await canceledPromise;
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
            Assert.True(canceled);
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledTooLate_StillTakesWriterLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();

            Promise<AsyncReaderWriterLock.WriterKey> cancelableLockPromise;
            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    cancelableLockPromise = rwl.WriterLockAsync(cts.Token);
                }

                cts.Cancel();

                Promise.State state = Promise.State.Pending;
                var nextLocker = rwl.WriterLockAsync()
                    .ContinueWith(r =>
                    {
                        state = r.State;
                        r.Value.Dispose();
                    });
                Assert.AreEqual(Promise.State.Pending, state);

                var key = await cancelableLockPromise;
                key.Dispose();
                await nextLocker;
                Assert.AreEqual(Promise.State.Resolved, state);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledTooLate_StillTakesReaderLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();

            Promise<AsyncReaderWriterLock.ReaderKey> cancelableLockPromise;
            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    cancelableLockPromise = rwl.ReaderLockAsync(cts.Token);
                }

                cts.Cancel();

                Promise.State state = Promise.State.Pending;
                var nextLocker = rwl.WriterLockAsync()
                    .ContinueWith(r =>
                    {
                        state = r.State;
                        r.Value.Dispose();
                    });
                Assert.AreEqual(Promise.State.Pending, state);

                var key = await cancelableLockPromise;
                key.Dispose();
                await nextLocker;
                Assert.AreEqual(Promise.State.Resolved, state);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledTooLate_StillTakesUpgradeableReaderLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();

            Promise<AsyncReaderWriterLock.UpgradeableReaderKey> cancelableLockPromise;
            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    cancelableLockPromise = rwl.UpgradeableReaderLockAsync(cts.Token);
                }

                cts.Cancel();

                Promise.State state = Promise.State.Pending;
                var nextLocker = rwl.WriterLockAsync()
                    .ContinueWith(r =>
                    {
                        state = r.State;
                        r.Value.Dispose();
                    });
                Assert.AreEqual(Promise.State.Pending, state);

                var key = await cancelableLockPromise;
                key.Dispose();
                await nextLocker;
                Assert.AreEqual(Promise.State.Resolved, state);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_CanceledTooLate_StillTakesUpgradedWriterLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();
            var deferred = Promise.NewDeferred();

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    using (var upgradeableReaderKey = await rwl.UpgradeableReaderLockAsync())
                    {
                        var cancelableLockPromise = rwl.UpgradeToWriterLockAsync(upgradeableReaderKey, cts.Token);
                        await deferred.Promise;
                        using (await cancelableLockPromise) { }
                    }
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            Promise.Run(async () =>
            {
                cts.Cancel();

                Promise.State state = Promise.State.Pending;
                var nextLocker = rwl.WriterLockAsync()
                    .ContinueWith(r =>
                    {
                        state = r.State;
                        r.Value.Dispose();
                    });
                Assert.AreEqual(Promise.State.Pending, state);

                deferred.Resolve();
                await nextLocker;
                Assert.AreEqual(Promise.State.Resolved, state);
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            cts.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_SupportsMultipleAsynchronousLocks()
        {
            // This test will fail if continuations are executed synchronously (promise1 will run in the loop forever, blocking promise2 from completing).

            var AsyncReaderWriterLock = new AsyncReaderWriterLock();
            var cancelationSource = CancelationSource.New();
            var cancelationToken = cancelationSource.Token;

            var promise1 = Promise.Run(async () =>
            {
                while (!cancelationToken.IsCancelationRequested)
                {
                    using (await AsyncReaderWriterLock.WriterLockAsync())
                    {
                        Thread.Sleep(10);
                    }
                }
            }, forceAsync: true);

            var promise2 = Promise.Run(() =>
            {
                using (AsyncReaderWriterLock.WriterLock())
                {
                    Thread.Sleep(1000);
                }
            }, forceAsync: true);

            promise2.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(10));
            cancelationSource.Cancel();
            promise1.WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            cancelationSource.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_ReleaseWriterLock_ResolvesReaderLockAndUpgradeableReaderLock_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();
            var writerDeferred = Promise.NewDeferred();
            var readersDeferred = Promise.NewDeferred();
            var preservedPromise = readersDeferred.Promise.Preserve();

            Promise.Run(async () =>
            {
                using (await rwl.WriterLockAsync())
                {
                    await writerDeferred.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            bool isUpgradeableReaderLocked = false;
            bool isReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync())
                {
                    isUpgradeableReaderLocked = true;
                    await preservedPromise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    isReaderLocked = true;
                    await preservedPromise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(isUpgradeableReaderLocked);
            Assert.False(isReaderLocked);

            writerDeferred.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(isUpgradeableReaderLocked);
            Assert.True(isReaderLocked);

            readersDeferred.Resolve();
            TestHelper.ExecuteForegroundCallbacks();
            preservedPromise.Forget();
            cts.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_ReleaseUpgradedWriterLock_ResolvesReaderLockOnly_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var cts = CancelationSource.New();
            var writerDeferred = Promise.NewDeferred();
            var readersDeferred = Promise.NewDeferred();
            var preservedPromise = readersDeferred.Promise.Preserve();

            Promise.Run(async () =>
            {
                using (var upgradeableReaderKey = await rwl.UpgradeableReaderLockAsync())
                {
                    using (await rwl.UpgradeToWriterLockAsync(upgradeableReaderKey))
                    {
                        await writerDeferred.Promise;
                    }
                    await preservedPromise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            bool isUpgradeableReaderLocked = false;
            bool isReaderLocked = false;

            Promise.Run(async () =>
            {
                using (await rwl.UpgradeableReaderLockAsync())
                {
                    isUpgradeableReaderLocked = true;
                    await preservedPromise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    isReaderLocked = true;
                    await preservedPromise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(isUpgradeableReaderLocked);
            Assert.False(isReaderLocked);

            writerDeferred.Resolve();
            // Continuations are posted asynchronously to the current context, so we need to make sure it is executed.
            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(isUpgradeableReaderLocked);
            Assert.True(isReaderLocked);

            readersDeferred.Resolve();
            TestHelper.ExecuteForegroundCallbacks();
            preservedPromise.Forget();
            cts.Dispose();
        }

        [Test]
        public void AsyncReaderWriterLock_ReleaseUpgradeableReaderLock_BeforeReleaseUpgradedWriterLock_Throws_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            bool didThrow = false;

            Promise.Run(async () =>
            {
                AsyncReaderWriterLock.UpgradeableReaderKey upgradeableReaderKey = default;
                AsyncReaderWriterLock.WriterKey upgradedWriterKey = default;
                try
                {
                    using (upgradeableReaderKey = await rwl.UpgradeableReaderLockAsync())
                    {
                        upgradedWriterKey = await rwl.UpgradeToWriterLockAsync(upgradeableReaderKey);
                    }
                }
                catch (System.InvalidOperationException)
                {
                    didThrow = true;
                }
                // After we verify the bad case, dispose them properly.
                upgradedWriterKey.Dispose();
                upgradeableReaderKey.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.True(didThrow);
        }

        [Test]
        public void AsyncReaderWriterLock_ReleaseUpgradeableReaderLock_BeforeUpgradedWriterLockPromiseIsComplete_Throws_Async()
        {
            var rwl = new AsyncReaderWriterLock();
            var deferred = Promise.NewDeferred();
            AsyncReaderWriterLock.UpgradeableReaderKey upgradeableReaderKey = default;
            Promise<AsyncReaderWriterLock.WriterKey> upgradedWriterPromise = default;
            bool didThrow = false;

            Promise.Run(async () =>
            {
                using (await rwl.ReaderLockAsync())
                {
                    await deferred.Promise;
                }
            }, SynchronizationOption.Synchronous)
                .Forget();

            Promise.Run(async () =>
            {
                try
                {
                    using (upgradeableReaderKey = await rwl.UpgradeableReaderLockAsync())
                    {
                        upgradedWriterPromise = rwl.UpgradeToWriterLockAsync(upgradeableReaderKey);
                    }
                }
                catch (System.InvalidOperationException)
                {
                    didThrow = true;
                }
                deferred.Resolve();
            }, SynchronizationOption.Synchronous)
                .Forget();

            Promise.Run(async () =>
            {
                // After we verified the bad case, await and dispose them properly.
                using (await upgradedWriterPromise) { }
                upgradeableReaderKey.Dispose();
            }, SynchronizationOption.Synchronous)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));

            Assert.True(didThrow);
        }

        [Test]
        public void AsyncReaderWriterLock_ReaderLocksDoNotStarveWriterLocks_Async()
        {
            // A reader-preferring lock will fail this test, causing the readerRunner to loop forever, never allowing the writer lock access.

            var rwl = new AsyncReaderWriterLock();
            int writerCount = 0;
            const int expectedWriterCount = 10;
            var deferred = Promise.NewDeferred();

            var readerRunner = Promise.Run(async () =>
            {
                // We take the reader lock first, then always take another reader lock before releasing the current.
                var readerLockPromise = rwl.ReaderLockAsync();
                Assert.AreEqual(0, writerCount);

                await deferred.Promise; // Wait for the writer runner to start.

                while (writerCount < expectedWriterCount)
                {
                    var temp = readerLockPromise;
                    using (await temp)
                    {
                        readerLockPromise = rwl.ReaderLockAsync();
                    }
                }

                using (await readerLockPromise) { }
            }, SynchronizationOption.Synchronous);

            var writerRunner = Promise.Run(async () =>
            {
                var writerLockPromise = rwl.WriterLockAsync();
                deferred.Resolve();

                using (await writerLockPromise)
                {
                    ++writerCount;
                }

                while (writerCount < expectedWriterCount)
                {
                    using (await rwl.WriterLockAsync())
                    {
                        ++writerCount;
                    }
                }
            }, SynchronizationOption.Synchronous);

            Promise.All(readerRunner, writerRunner)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.AreEqual(expectedWriterCount, writerCount);
        }

        [Test]
        public void AsyncReaderWriterLock_WriterLocksDoNotStarveReaderLocks_Async()
        {
            // A writer-preferring lock will fail this test, causing the writerRunner to loop forever, never allowing the reader lock access.

            var rwl = new AsyncReaderWriterLock();
            int readerCount = 0;
            const int expectedReaderCount = 10;
            var deferred = Promise.NewDeferred();

            var writerRunner = Promise.Run(async () =>
            {
                // We take the writer lock first, then always take another writer lock before releasing the current.
                var writerLockPromise = rwl.WriterLockAsync();
                Assert.AreEqual(0, readerCount);

                await deferred.Promise; // Wait for the reader runner to start.

                while (readerCount < expectedReaderCount)
                {
                    var temp = writerLockPromise;
                    using (await temp)
                    {
                        writerLockPromise = rwl.WriterLockAsync();
                    }
                }

                using (await writerLockPromise) { }
            }, SynchronizationOption.Synchronous);

            var readerRunner = Promise.Run(async () =>
            {
                var readerLockPromise = rwl.ReaderLockAsync();
                deferred.Resolve();

                using (await readerLockPromise)
                {
                    ++readerCount;
                }

                while (readerCount < expectedReaderCount)
                {
                    using (await rwl.ReaderLockAsync())
                    {
                        ++readerCount;
                    }
                }
            }, SynchronizationOption.Synchronous);

            Promise.All(readerRunner, writerRunner)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            Assert.AreEqual(expectedReaderCount, readerCount);
        }

        public enum ReaderWriterLockType
        {
            Reader,
            Writer,
            Upgradeable
        }

        public enum TakeNextUpgradeablePlace
        {
            BeforeUpgrade,
            InsideWriterLock,
            AfterDowngrade
        }

        private static IEnumerable<TestCaseData> DontStarveLocksCases()
        {
            TakeNextUpgradeablePlace[] upgradeablePlaces = new[]
            {
                TakeNextUpgradeablePlace.BeforeUpgrade,
                TakeNextUpgradeablePlace.InsideWriterLock,
                TakeNextUpgradeablePlace.AfterDowngrade
            };
            SynchronizationOption[] runnerOptions = new[]
            {
                SynchronizationOption.Synchronous,
#if !UNITY_WEBGL
                SynchronizationOption.Background
#endif
            };
            foreach (var upgradeablePlace in upgradeablePlaces)
            foreach (var runnerOption in runnerOptions)
            {
                yield return new TestCaseData(ReaderWriterLockType.Reader, ReaderWriterLockType.Writer, ReaderWriterLockType.Upgradeable, upgradeablePlace, runnerOption);
                yield return new TestCaseData(ReaderWriterLockType.Reader, ReaderWriterLockType.Upgradeable, ReaderWriterLockType.Writer, upgradeablePlace, runnerOption);
                yield return new TestCaseData(ReaderWriterLockType.Writer, ReaderWriterLockType.Reader, ReaderWriterLockType.Upgradeable, upgradeablePlace, runnerOption);
                yield return new TestCaseData(ReaderWriterLockType.Writer, ReaderWriterLockType.Upgradeable, ReaderWriterLockType.Reader, upgradeablePlace, runnerOption);
                yield return new TestCaseData(ReaderWriterLockType.Upgradeable, ReaderWriterLockType.Reader, ReaderWriterLockType.Writer, upgradeablePlace, runnerOption);
                yield return new TestCaseData(ReaderWriterLockType.Upgradeable, ReaderWriterLockType.Writer, ReaderWriterLockType.Reader, upgradeablePlace, runnerOption);
            }
        }

        [Test, TestCaseSource(nameof(DontStarveLocksCases))]
        public void AsyncReaderWriterLock_LocksDoNotStarveOtherLocks_Async(
            ReaderWriterLockType first,
            ReaderWriterLockType second,
            ReaderWriterLockType third,
            TakeNextUpgradeablePlace upgradeablePlace,
            SynchronizationOption runnerOption)
        {
            // A lock that does not balance the types of locks acquired will fail this test (reader-preferred or writer-preferred),
            // causing the favored lock type to loop forever, never allowing the other lock types access.

            var rwl = new AsyncReaderWriterLock();

            int readerCount = 0;
            int writerCount = 0;
            int upgradeableReaderCount = 0;
            int upgradedWriterCount = 0;
            const int expectedCounts = 10;

            var readerStartDeferred = Promise.NewDeferred();
            var writerStartDeferred = Promise.NewDeferred();
            var upgradeableReaderStartDeferred = Promise.NewDeferred();

            var readerReadyDeferred = Promise.NewDeferred();
            var writerReadyDeferred = Promise.NewDeferred();
            var upgradeableReaderReadyDeferred = Promise.NewDeferred();
            var allReadyPromise = Promise.All(readerReadyDeferred.Promise, writerReadyDeferred.Promise, upgradeableReaderReadyDeferred.Promise).Preserve();

            var readerRunner = readerStartDeferred.Promise
                .WaitAsync(runnerOption)
                .Then(async () =>
                {
                    // We take the lock first, then always take another lock before releasing the current.
                    var lockPromise = rwl.ReaderLockAsync();

                    readerReadyDeferred.Resolve();
                    await allReadyPromise.WaitAsync(runnerOption); // Wait for the other runners to start.

                    while (readerCount < expectedCounts || writerCount < expectedCounts || upgradeableReaderCount < expectedCounts || upgradedWriterCount < expectedCounts)
                    {
                        using (await lockPromise)
                        {
                            ++readerCount;
                            lockPromise = rwl.ReaderLockAsync();
                        }
                    }

                    using (await lockPromise) { }
                });

            var writerRunner = writerStartDeferred.Promise
                .WaitAsync(runnerOption)
                .Then(async () =>
                {
                    // We take the lock first, then always take another lock before releasing the current.
                    var lockPromise = rwl.WriterLockAsync();

                    writerReadyDeferred.Resolve();
                    await allReadyPromise.WaitAsync(runnerOption); // Wait for the other runners to start.

                    while (readerCount < expectedCounts || writerCount < expectedCounts || upgradeableReaderCount < expectedCounts || upgradedWriterCount < expectedCounts)
                    {
                        using (await lockPromise)
                        {
                            ++writerCount;
                            lockPromise = rwl.WriterLockAsync();
                        }
                    }

                    using (await lockPromise) { }
                });

            var upgradeableReaderRunner = upgradeableReaderStartDeferred.Promise
                .WaitAsync(runnerOption)
                .Then(async () =>
                {
                    // We take the lock first, then always take another lock before releasing the current.
                    var lockPromise = rwl.UpgradeableReaderLockAsync();

                    upgradeableReaderReadyDeferred.Resolve();
                    await allReadyPromise.WaitAsync(runnerOption); // Wait for the other runners to start.

                    while (readerCount < expectedCounts || writerCount < expectedCounts || upgradeableReaderCount < expectedCounts || upgradedWriterCount < expectedCounts)
                    {
                        using (var key = await lockPromise)
                        {
                            ++upgradeableReaderCount;
                            if (upgradeablePlace == TakeNextUpgradeablePlace.BeforeUpgrade)
                            {
                                lockPromise = rwl.UpgradeableReaderLockAsync();
                            }
                            using (await rwl.UpgradeToWriterLockAsync(key))
                            {
                                ++upgradedWriterCount;
                                if (upgradeablePlace == TakeNextUpgradeablePlace.InsideWriterLock)
                                {
                                    lockPromise = rwl.UpgradeableReaderLockAsync();
                                }
                            }
                            if (upgradeablePlace == TakeNextUpgradeablePlace.AfterDowngrade)
                            {
                                lockPromise = rwl.UpgradeableReaderLockAsync();
                            }
                        }
                    }

                    using (await lockPromise) { }
                });

            StartRunner(first);
            StartRunner(second);
            StartRunner(third);

            Promise.All(readerRunner, writerRunner, upgradeableReaderRunner)
                .WaitWithTimeoutWhileExecutingForegroundContext(TimeSpan.FromSeconds(1));
            allReadyPromise.Forget();

            Assert.GreaterOrEqual(readerCount, expectedCounts);
            Assert.GreaterOrEqual(writerCount, expectedCounts);
            Assert.GreaterOrEqual(upgradeableReaderCount, expectedCounts);
            Assert.GreaterOrEqual(upgradedWriterCount, expectedCounts);

            void StartRunner(ReaderWriterLockType lockType)
            {
                if (lockType == ReaderWriterLockType.Reader)
                {
                    readerStartDeferred.Resolve();
                }
                else if (lockType == ReaderWriterLockType.Writer)
                {
                    writerStartDeferred.Resolve();
                }
                else
                {
                    upgradeableReaderStartDeferred.Resolve();
                }
            }
        }

        [Test]
        public void AsyncReaderWriterLock_PrioritizeWritersPrefersWriters()
        {
            var rwl = new AsyncReaderWriterLock(AsyncReaderWriterLock.ContentionStrategy.PrioritizeWriters);

            bool enteredReaderLock = false;
            bool enteredUpgradeableReaderLock = false;

            var writerKey = rwl.WriterLock();
            rwl.ReaderLockAsync()
                .Then(readerKey =>
                {
                    enteredReaderLock = true;
                    readerKey.Dispose();
                })
                .Forget();
            Assert.False(enteredReaderLock);

            rwl.UpgradeableReaderLockAsync()
                .Then(readerKey =>
                {
                    enteredUpgradeableReaderLock = true;
                    readerKey.Dispose();
                })
                .Forget();
            Assert.False(enteredUpgradeableReaderLock);

            rwl.WriterLockAsync()
                .Then(writerKey2 => writerKey = writerKey2)
                .Forget();

            writerKey.Dispose();
            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(enteredReaderLock);
            Assert.False(enteredUpgradeableReaderLock);

            writerKey.Dispose();
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(enteredReaderLock);
            Assert.True(enteredUpgradeableReaderLock);
        }

        [Test]
        public void AsyncReaderWriterLock_PrioritizeReadersPrefersReaders()
        {
            var rwl = new AsyncReaderWriterLock(AsyncReaderWriterLock.ContentionStrategy.PrioritizeReaders);

            bool enteredWriterLock = false;

            var readerKey1 = rwl.ReaderLock();
            rwl.WriterLockAsync()
                .Then(writerKey =>
                {
                    enteredWriterLock = true;
                    writerKey.Dispose();
                })
                .Forget();
            Assert.False(enteredWriterLock);

            bool enteredUpgradeableReaderLock = false;
            bool enteredUpgradedWriterLock = false;
            var upgradeableReaderKey = default(AsyncReaderWriterLock.UpgradeableReaderKey);
            var upgradedWriterKey = default(AsyncReaderWriterLock.WriterKey);

            rwl.UpgradeableReaderLockAsync()
                .Then(readerKey =>
                {
                    upgradeableReaderKey = readerKey;
                    enteredUpgradeableReaderLock = true;
                    rwl.UpgradeToWriterLockAsync(readerKey)
                        .Then(key =>
                        {
                            enteredUpgradedWriterLock = true;
                            upgradedWriterKey = key;
                        })
                        .Forget();
                })
                .Forget();
            Assert.True(enteredUpgradeableReaderLock);
            Assert.False(enteredUpgradedWriterLock);

            bool enteredReader2 = false;
            var readerKey2 = default(AsyncReaderWriterLock.ReaderKey);

            rwl.ReaderLockAsync()
                .Then(key =>
                {
                    enteredReader2 = true;
                    readerKey2 = key;
                })
                .Forget();

            Assert.True(enteredReader2);

            readerKey1.Dispose();
            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(enteredWriterLock);
            Assert.False(enteredUpgradedWriterLock);

            readerKey2.Dispose();
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(enteredUpgradedWriterLock);
            Assert.False(enteredWriterLock);

            upgradedWriterKey.Dispose();
            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(enteredWriterLock);

            upgradeableReaderKey.Dispose();
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(enteredWriterLock);
        }

        [Test]
        public void AsyncReaderWriterLock_PrioritizeUpgradeableReadersPrefersReadersAndUpgradedWriters()
        {
            var rwl = new AsyncReaderWriterLock(AsyncReaderWriterLock.ContentionStrategy.PrioritizeUpgradeableReaders);

            bool enteredWriterLock = false;

            var readerKey1 = rwl.ReaderLock();
            rwl.WriterLockAsync()
                .Then(writerKey =>
                {
                    enteredWriterLock = true;
                    writerKey.Dispose();
                })
                .Forget();
            Assert.False(enteredWriterLock);

            bool enteredUpgradeableReaderLock = false;
            bool enteredUpgradedWriterLock = false;
            var upgradeableReaderKey = default(AsyncReaderWriterLock.UpgradeableReaderKey);
            var upgradedWriterKey = default(AsyncReaderWriterLock.WriterKey);

            rwl.UpgradeableReaderLockAsync()
                .Then(readerKey =>
                {
                    upgradeableReaderKey = readerKey;
                    enteredUpgradeableReaderLock = true;
                    rwl.UpgradeToWriterLockAsync(readerKey)
                        .Then(key =>
                        {
                            enteredUpgradedWriterLock = true;
                            upgradedWriterKey = key;
                        })
                        .Forget();
                })
                .Forget();
            Assert.True(enteredUpgradeableReaderLock);
            Assert.False(enteredUpgradedWriterLock);

            bool enteredReader2 = false;
            var readerKey2 = default(AsyncReaderWriterLock.ReaderKey);

            rwl.ReaderLockAsync()
                .Then(key =>
                {
                    enteredReader2 = true;
                    readerKey2 = key;
                })
                .Forget();

            Assert.False(enteredReader2);

            readerKey1.Dispose();
            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(enteredReader2);
            Assert.False(enteredWriterLock);
            Assert.True(enteredUpgradedWriterLock);

            upgradedWriterKey.Dispose();
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(enteredReader2);
            Assert.False(enteredWriterLock);

            readerKey2.Dispose();
            TestHelper.ExecuteForegroundCallbacks();
            Assert.False(enteredWriterLock);

            upgradeableReaderKey.Dispose();
            TestHelper.ExecuteForegroundCallbacks();
            Assert.True(enteredWriterLock);
        }

#if PROTO_PROMISE_TEST_GC_ENABLED
        [Test]
        public void AsyncReaderWriterLock_AbandonedLockIsReported()
        {
            var currentRejectionHandler = Promise.Config.UncaughtRejectionHandler;
            AbandonedLockException abandonedLockException = null;
            Promise.Config.UncaughtRejectionHandler = ex =>
            {
                abandonedLockException = ex.Value as AbandonedLockException;
            };

            var rwl = new AsyncReaderWriterLock();
            EnterAndAbandonReaderLock(rwl);

            TestHelper.GcCollectAndWaitForFinalizers();
            Assert.IsNotNull(abandonedLockException);
            AssertThrowsAbandoned(rwl);

            abandonedLockException = null;
            rwl = new AsyncReaderWriterLock();
            var upgradeableReaderKey = rwl.UpgradeableReaderLock();
            EnterAndAbandonReaderLock(rwl);

            TestHelper.GcCollectAndWaitForFinalizers();
            Assert.IsNotNull(abandonedLockException);
            AssertThrowsAbandoned(rwl, upgradeableReaderKey);
            upgradeableReaderKey.Dispose();


            abandonedLockException = null;
            rwl = new AsyncReaderWriterLock();
            EnterAndAbandonWriterLock(rwl);

            TestHelper.GcCollectAndWaitForFinalizers();
            Assert.IsNotNull(abandonedLockException);
            AssertThrowsAbandoned(rwl);


            abandonedLockException = null;
            rwl = new AsyncReaderWriterLock();
            EnterAndAbandonUpgradeableReaderLock(rwl);

            TestHelper.GcCollectAndWaitForFinalizers();
            Assert.IsNotNull(abandonedLockException);
            AssertThrowsAbandoned(rwl);


            abandonedLockException = null;
            rwl = new AsyncReaderWriterLock();
            upgradeableReaderKey = rwl.UpgradeableReaderLock();
            EnterAndAbandonUpgradedWriterLock(rwl, upgradeableReaderKey);

            TestHelper.GcCollectAndWaitForFinalizers();
            Assert.IsNotNull(abandonedLockException);
            AssertThrowsAbandoned(rwl);
            // Upgraded writer is never released, so releasing the reader should throw.
            Assert.Catch<System.InvalidOperationException>(() => upgradeableReaderKey.Dispose());

            Promise.Config.UncaughtRejectionHandler = currentRejectionHandler;
        }

        private void AssertThrowsAbandoned(AsyncReaderWriterLock rwl)
        {
            Assert.Throws<AbandonedLockException>(() => rwl.ReaderLock());
            Assert.Throws<AbandonedLockException>(() => rwl.ReaderLockAsync());
            Assert.Throws<AbandonedLockException>(() => rwl.TryEnterReaderLock(out _));
            Assert.Throws<AbandonedLockException>(() => rwl.TryEnterReaderLock(out _, CancelationToken.Canceled()));
            Assert.Throws<AbandonedLockException>(() => rwl.TryEnterReaderLockAsync(CancelationToken.Canceled()));
            Assert.Throws<AbandonedLockException>(() => rwl.WriterLock());
            Assert.Throws<AbandonedLockException>(() => rwl.WriterLockAsync());
            Assert.Throws<AbandonedLockException>(() => rwl.TryEnterWriterLock(out _));
            Assert.Throws<AbandonedLockException>(() => rwl.TryEnterWriterLock(out _, CancelationToken.Canceled()));
            Assert.Throws<AbandonedLockException>(() => rwl.TryEnterWriterLockAsync(CancelationToken.Canceled()));
            Assert.Throws<AbandonedLockException>(() => rwl.UpgradeableReaderLock());
            Assert.Throws<AbandonedLockException>(() => rwl.UpgradeableReaderLockAsync());
            Assert.Throws<AbandonedLockException>(() => rwl.TryEnterUpgradeableReaderLock(out _));
            Assert.Throws<AbandonedLockException>(() => rwl.TryEnterUpgradeableReaderLock(out _, CancelationToken.Canceled()));
            Assert.Throws<AbandonedLockException>(() => rwl.TryEnterUpgradeableReaderLockAsync(CancelationToken.Canceled()));
        }

        private void AssertThrowsAbandoned(AsyncReaderWriterLock rwl, AsyncReaderWriterLock.UpgradeableReaderKey upgradeableReaderKey)
        {
            Assert.Throws<AbandonedLockException>(() => rwl.ReaderLock());
            Assert.Throws<AbandonedLockException>(() => rwl.ReaderLockAsync());
            Assert.Throws<AbandonedLockException>(() => rwl.TryEnterReaderLock(out _));
            Assert.Throws<AbandonedLockException>(() => rwl.TryEnterReaderLock(out _, CancelationToken.Canceled()));
            Assert.Throws<AbandonedLockException>(() => rwl.TryEnterReaderLockAsync(CancelationToken.Canceled()));
            Assert.Throws<AbandonedLockException>(() => rwl.WriterLock());
            Assert.Throws<AbandonedLockException>(() => rwl.WriterLockAsync());
            Assert.Throws<AbandonedLockException>(() => rwl.TryEnterWriterLock(out _));
            Assert.Throws<AbandonedLockException>(() => rwl.TryEnterWriterLock(out _, CancelationToken.Canceled()));
            Assert.Throws<AbandonedLockException>(() => rwl.TryEnterWriterLockAsync(CancelationToken.Canceled()));
            Assert.Throws<AbandonedLockException>(() => rwl.UpgradeableReaderLock());
            Assert.Throws<AbandonedLockException>(() => rwl.UpgradeableReaderLockAsync());
            Assert.Throws<AbandonedLockException>(() => rwl.TryEnterUpgradeableReaderLock(out _));
            Assert.Throws<AbandonedLockException>(() => rwl.TryEnterUpgradeableReaderLock(out _, CancelationToken.Canceled()));
            Assert.Throws<AbandonedLockException>(() => rwl.TryEnterUpgradeableReaderLockAsync(CancelationToken.Canceled()));
            Assert.Throws<AbandonedLockException>(() => rwl.UpgradeToWriterLock(upgradeableReaderKey));
            Assert.Throws<AbandonedLockException>(() => rwl.UpgradeToWriterLockAsync(upgradeableReaderKey));
            Assert.Throws<AbandonedLockException>(() => rwl.TryUpgradeToWriterLock(upgradeableReaderKey, out _));
            Assert.Throws<AbandonedLockException>(() => rwl.TryUpgradeToWriterLock(upgradeableReaderKey, out _, CancelationToken.Canceled()));
            Assert.Throws<AbandonedLockException>(() => rwl.TryUpgradeToWriterLockAsync(upgradeableReaderKey, CancelationToken.Canceled()));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EnterAndAbandonReaderLock(AsyncReaderWriterLock rwl)
        {
            rwl.ReaderLock();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EnterAndAbandonWriterLock(AsyncReaderWriterLock rwl)
        {
            rwl.WriterLock();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EnterAndAbandonUpgradeableReaderLock(AsyncReaderWriterLock rwl)
        {
            rwl.UpgradeableReaderLock();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EnterAndAbandonUpgradedWriterLock(AsyncReaderWriterLock rwl, AsyncReaderWriterLock.UpgradeableReaderKey upgradeableReaderKey)
        {
            rwl.UpgradeToWriterLock(upgradeableReaderKey);
        }
#endif // PROTO_PROMISE_TEST_GC_ENABLED
    }
#endif // UNITY_2021_2_OR_NEWER || NETSTANDARD2_1_OR_GREATER || NETCOREAPP
}