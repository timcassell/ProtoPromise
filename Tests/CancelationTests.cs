#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0059 // Unnecessary assignment of a value

using System;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Proto.Promises.Tests
{
    public class CancelationTests
    {
        public class Source
        {
            [SetUp]
            public void Setup()
            {
                TestHelper.cachedRejectionHandler = Promise.Config.UncaughtRejectionHandler;
                Promise.Config.UncaughtRejectionHandler = null;
            }

            [TearDown]
            public void Teardown()
            {
                Promise.Config.UncaughtRejectionHandler = TestHelper.cachedRejectionHandler;
            }

            [Test]
            public void NewCancelationSourceIsNotValid()
            {
                CancelationSource cancelationSource = new CancelationSource();
                Assert.IsFalse(cancelationSource.IsValid);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationSourceInvalidOperations()
            {
                CancelationSource cancelationSource = new CancelationSource();
                Assert.Throws<InvalidOperationException>(() => { var _ = cancelationSource.Token; });
                Assert.Throws<InvalidOperationException>(() => { cancelationSource.Cancel(); });
                Assert.Throws<InvalidOperationException>(() => { cancelationSource.Cancel("Cancel"); });
                Assert.Throws<InvalidOperationException>(() => { cancelationSource.Dispose(); });

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationSourceNewIsValid()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                Assert.IsTrue(cancelationSource.IsValid);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationSourceIsNotValidAfterDispose()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Dispose();
                Assert.IsFalse(cancelationSource.IsValid);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationSourceIsValidAfterCancel0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();
                Assert.IsTrue(cancelationSource.IsValid);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationSourceIsValidAfterCancel1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel("Canceled");
                Assert.IsTrue(cancelationSource.IsValid);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationSourceNoCancelationRequestedBeforeCanceled()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                Assert.IsFalse(cancelationSource.IsCancelationRequested);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationSourceCancelationRequestedAfterCanceled0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel();
                Assert.IsTrue(cancelationSource.IsCancelationRequested);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationSourceCancelationRequestedAfterCanceled1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                cancelationSource.Cancel("Canceled");
                Assert.IsTrue(cancelationSource.IsCancelationRequested);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }
        }

        public class Token
        {
            [SetUp]
            public void Setup()
            {
                TestHelper.cachedRejectionHandler = Promise.Config.UncaughtRejectionHandler;
                Promise.Config.UncaughtRejectionHandler = null;
            }

            [TearDown]
            public void Teardown()
            {
                Promise.Config.UncaughtRejectionHandler = TestHelper.cachedRejectionHandler;
            }

            [Test]
            public void NewCancelationTokenCannotBeCanceled()
            {
                CancelationToken cancelationToken = new CancelationToken();
                Assert.IsFalse(cancelationToken.CanBeCanceled);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void NewCancelationTokenNoCancelationRequested()
            {
                CancelationToken cancelationToken = new CancelationToken();
                Assert.IsFalse(cancelationToken.IsCancelationRequested);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenNoCancelationRequestedBeforeCanceled()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                Assert.IsFalse(cancelationToken.IsCancelationRequested);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenCancelationRequestedAfterCanceled0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel();
                Assert.IsTrue(cancelationToken.IsCancelationRequested);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenCancelationRequestedAfterCanceled1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel("Canceled");
                Assert.IsTrue(cancelationToken.IsCancelationRequested);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenInvalidOperations()
            {
                CancelationToken cancelationToken = new CancelationToken();
                Assert.Throws<InvalidOperationException>(() => { var _ = cancelationToken.CancelationValue; });
                Assert.Throws<InvalidOperationException>(() => { var _ = cancelationToken.CancelationValueType; });
                Assert.Throws<InvalidOperationException>(() => { cancelationToken.Register(_ => { }); });
                Assert.Throws<InvalidOperationException>(() => { cancelationToken.Register(1, (i, _) => { }); });
                Assert.Throws<InvalidOperationException>(() => { string _; cancelationToken.TryGetCancelationValueAs(out _); });
                Assert.Throws<InvalidOperationException>(cancelationToken.Retain);
                Assert.Throws<InvalidOperationException>(cancelationToken.Release);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenFromSourceCanBeCanceled()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                Assert.IsTrue(cancelationToken.CanBeCanceled);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenFromSourceCannotBeCanceledAfterSourceIsDisposed()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Dispose();
                Assert.IsFalse(cancelationToken.CanBeCanceled);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenFromSourceCancelationIsRequested0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel();
                Assert.IsTrue(cancelationToken.IsCancelationRequested);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenFromSourceCancelationIsRequested1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel("Cancel");
                Assert.IsTrue(cancelationToken.IsCancelationRequested);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenFromSourceCancelationIsNotRequestedAfterSourceIsDisposed0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel();
                cancelationSource.Dispose();
                Assert.IsFalse(cancelationToken.IsCancelationRequested);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenFromSourceCancelationIsNotRequestedAfterSourceIsDisposed1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel("Cancel");
                cancelationSource.Dispose();
                Assert.IsFalse(cancelationToken.IsCancelationRequested);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenValueTypeIsNull0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel();
                Assert.IsNull(cancelationToken.CancelationValueType);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenValueTypeIsNull1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel(default(string));
                Assert.IsNull(cancelationToken.CancelationValueType);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenValueTypeIsString()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel("Cancel");
                Assert.IsTrue(cancelationToken.CancelationValueType == typeof(string));
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenValueIsNull0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel();
                Assert.IsNull(cancelationToken.CancelationValue);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenValueIsNull1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel(default(string));
                Assert.IsNull(cancelationToken.CancelationValue);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenValueMatchesCancelValue()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                string cancelValue = "Cancel";
                cancelationSource.Cancel(cancelValue);
                Assert.AreEqual(cancelValue, cancelationToken.CancelationValue);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenNullValueCannotBeGottenAsString0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel();
                string val;
                Assert.IsFalse(cancelationToken.TryGetCancelationValueAs(out val));
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenNullValueCannotBeGottenAsString1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel(default(string));
                string val;
                Assert.IsFalse(cancelationToken.TryGetCancelationValueAs(out val));
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenStringValueCanBeGottenAsString()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel("Cancel");
                string val;
                Assert.IsTrue(cancelationToken.TryGetCancelationValueAs(out val));
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void RetainedCancelationTokenFromSourceCanBeCanceledAfterSourceIsDisposed()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationToken.Retain();
                cancelationSource.Dispose();
                Assert.IsTrue(cancelationToken.CanBeCanceled);
                cancelationToken.Release();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void ReleasedCancelationTokenFromSourceCanBeCanceledAfterSourceIsDisposed()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationToken.Retain();
                cancelationSource.Dispose();
                cancelationToken.Release();
                Assert.IsFalse(cancelationToken.CanBeCanceled);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void RetainedCancelationTokenFromSourceCancelationIsRequestedAfterSourceIsDisposed0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel();
                cancelationToken.Retain();
                cancelationSource.Dispose();
                Assert.IsTrue(cancelationToken.IsCancelationRequested);
                cancelationToken.Release();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void RetainedCancelationTokenFromSourceCancelationIsRequestedAfterSourceIsDisposed1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel("Cancel");
                cancelationToken.Retain();
                cancelationSource.Dispose();
                Assert.IsTrue(cancelationToken.IsCancelationRequested);
                cancelationToken.Release();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void ReleasedCancelationTokenFromSourceCancelationIsNotRequestedAfterSourceIsDisposed0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel();
                cancelationToken.Retain();
                cancelationSource.Dispose();
                cancelationToken.Release();
                Assert.IsFalse(cancelationToken.IsCancelationRequested);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void ReleasedCancelationTokenFromSourceCancelationIsNotRequestedAfterSourceIsDisposed1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel("Cancel");
                cancelationToken.Retain();
                cancelationSource.Dispose();
                cancelationToken.Release();
                Assert.IsFalse(cancelationToken.IsCancelationRequested);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenThrowIfCancelationRequested0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel();
                bool caughtException = false;
                try
                {
                    cancelationToken.ThrowIfCancelationRequested();
                }
                catch (CancelException)
                {
                    caughtException = true;
                }
                cancelationSource.Dispose();
                Assert.IsTrue(caughtException);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenThrowIfCancelationRequested1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationSource.Cancel("Cancel");
                bool caughtException = false;
                try
                {
                    cancelationToken.ThrowIfCancelationRequested();
                }
                catch (CancelException)
                {
                    caughtException = true;
                }
                cancelationSource.Dispose();
                Assert.IsTrue(caughtException);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }
        }

        public class Registration
        {
            [SetUp]
            public void Setup()
            {
                TestHelper.cachedRejectionHandler = Promise.Config.UncaughtRejectionHandler;
                Promise.Config.UncaughtRejectionHandler = null;
            }

            [TearDown]
            public void Teardown()
            {
                Promise.Config.UncaughtRejectionHandler = TestHelper.cachedRejectionHandler;
            }

            [Test]
            public void NewCancelationRegistrationIsNotRegistered()
            {
                CancelationRegistration cancelationRegistration = new CancelationRegistration();
                Assert.IsFalse(cancelationRegistration.IsRegistered);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void RegistrationFromCancelationTokenIsRegistered0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                CancelationRegistration cancelationRegistration = cancelationToken.Register(_ => { });
                Assert.IsTrue(cancelationRegistration.IsRegistered);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void RegistrationFromCancelationTokenIsRegistered1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                CancelationRegistration cancelationRegistration = cancelationToken.Register(0, (i, _) => { });
                Assert.IsTrue(cancelationRegistration.IsRegistered);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void RegistrationFromCancelationTokenIsNotRegisteredAfterInvoked0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                CancelationRegistration cancelationRegistration = cancelationToken.Register(_ => { });
                cancelationSource.Cancel();
                Assert.IsFalse(cancelationRegistration.IsRegistered);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void RegistrationFromCancelationTokenIsNotRegisteredAfterInvoked1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                CancelationRegistration cancelationRegistration = cancelationToken.Register(0, (i, _) => { });
                cancelationSource.Cancel();
                Assert.IsFalse(cancelationRegistration.IsRegistered);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void RegistrationFromCancelationTokenIsNotRegisteredAfterInvoked2()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                CancelationRegistration cancelationRegistration = new CancelationRegistration();
                cancelationRegistration = cancelationToken.Register(_ => Assert.IsFalse(cancelationRegistration.IsRegistered));
                cancelationSource.Cancel();
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void RegistrationFromCancelationTokenIsNotRegisteredAfterInvoked3()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                CancelationRegistration cancelationRegistration = new CancelationRegistration();
                cancelationRegistration = cancelationToken.Register(0, (i, _) => Assert.IsFalse(cancelationRegistration.IsRegistered));
                cancelationSource.Cancel();
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void RegistrationFromCancelationTokenIsNotRegisteredAfterSourceIsDisposed0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                CancelationRegistration cancelationRegistration = cancelationToken.Register(_ => { });
                cancelationSource.Dispose();
                Assert.IsFalse(cancelationRegistration.IsRegistered);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void RegistrationFromCancelationTokenIsNotRegisteredAfterSourceIsDisposed1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                CancelationRegistration cancelationRegistration = cancelationToken.Register(0, (i, _) => { });
                cancelationSource.Dispose();
                Assert.IsFalse(cancelationRegistration.IsRegistered);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenRegisterCallbackIsInvoked0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                bool invoked = false;
                cancelationToken.Register(_ => invoked = true);
                cancelationSource.Cancel();
                Assert.IsTrue(invoked);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenRegisterCallbackIsInvoked1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                bool invoked = false;
                cancelationToken.Register(0, (i, _) => invoked = true);
                cancelationSource.Cancel();
                Assert.IsTrue(invoked);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenRegisterCallbackIsInvoked2()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                bool invoked = false;
                cancelationToken.Register(_ => invoked = true);
                cancelationSource.Cancel("Cancel");
                Assert.IsTrue(invoked);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenRegisterCallbackIsInvoked3()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                bool invoked = false;
                cancelationToken.Register(0, (i, _) => invoked = true);
                cancelationSource.Cancel("Cancel");
                Assert.IsTrue(invoked);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationRegistrationUnregisterCallbackIsNotInvoked0()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                bool invoked = false;
                CancelationRegistration cancelationRegistration = cancelationToken.Register(_ => invoked = true);
                cancelationRegistration.Unregister();
                cancelationSource.Cancel();
                Assert.IsFalse(invoked);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationRegistrationUnregisterCallbackIsNotInvoked1()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                bool invoked = false;
                CancelationRegistration cancelationRegistration = cancelationToken.Register(0, (i, _) => invoked = true);
                cancelationRegistration.Unregister();
                cancelationSource.Cancel();
                Assert.IsFalse(invoked);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationRegistrationUnregisterCallbackIsNotInvoked2()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                bool invoked = false;
                CancelationRegistration cancelationRegistration = new CancelationRegistration();
                cancelationToken.Register(_ => cancelationRegistration.Unregister());
                cancelationRegistration = cancelationToken.Register(_ => invoked = true);
                cancelationSource.Cancel();
                Assert.IsFalse(invoked);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationRegistrationUnregisterCallbackIsNotInvoked3()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                bool invoked = false;
                CancelationRegistration cancelationRegistration = new CancelationRegistration();
                cancelationToken.Register(_ => cancelationRegistration.Unregister());
                cancelationRegistration = cancelationToken.Register(0, (i, _) => invoked = true);
                cancelationSource.Cancel();
                Assert.IsFalse(invoked);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void CancelationTokenRegisterCaptureVariableMatches()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                string expected = "Captured";
                cancelationToken.Register(expected, (cv, _) => Assert.AreEqual(expected, cv));
                cancelationSource.Cancel();
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void RegisteredCallbacksAreInvokedAfterSourceIsDisposed()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                bool invoked = false;
                // This should never be done in practice!
                cancelationToken.Register(_ => cancelationSource.Dispose());
                cancelationToken.Register(_ => invoked = true);
                cancelationSource.Cancel();
                Assert.IsTrue(invoked);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void RegisteredCallbackIsRegisteredAfterSourceIsDisposedDuringInvocation()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                // This should never be done in practice!
                CancelationRegistration cancelationRegistration = new CancelationRegistration();
                cancelationToken.Register(_ =>
                {
                    cancelationSource.Dispose();
                    Assert.IsTrue(cancelationRegistration.IsRegistered);
                });
                cancelationRegistration = cancelationToken.Register(_ => { });
                cancelationSource.Cancel();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void RegisteredCallbackExceptionPropagatesToCancelCaller()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                cancelationToken.Register(_ =>
                {
                    throw new Exception();
                });
                Assert.Throws<AggregateException>(cancelationSource.Cancel);
                cancelationSource.Dispose();

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }

            [Test]
            public void RegisteredCallbacksAreInvokedEvenWhenAnExceptionOccurs()
            {
                CancelationSource cancelationSource = CancelationSource.New();
                CancelationToken cancelationToken = cancelationSource.Token;
                int callbackCount = 0;
                cancelationToken.Register(_ => ++callbackCount);
                cancelationToken.Register(_ =>
                {
                    ++callbackCount;
                    throw new Exception();
                });
                cancelationToken.Register(_ => ++callbackCount);
                try
                {
                    cancelationSource.Cancel();
                }
                catch (Exception) { }
                cancelationSource.Dispose();
                Assert.AreEqual(3, callbackCount);

                // Clean up.
                GC.Collect();
                Promise.Manager.HandleCompletesAndProgress();
                LogAssert.NoUnexpectedReceived();
            }
        }
    }
}