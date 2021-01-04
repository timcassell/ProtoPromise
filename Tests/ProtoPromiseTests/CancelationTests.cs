#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

#pragma warning disable IDE0018 // Inline variable declaration
#pragma warning disable IDE0059 // Unnecessary assignment of a value

using System;
using NUnit.Framework;

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
                void Test()
                {
                    CancelationSource cancelationSource = new CancelationSource();
                    Assert.IsFalse(cancelationSource.IsValid);

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSourceInvalidOperations()
            {
                void Test()
                {
                    CancelationSource cancelationSource = new CancelationSource();
                    Assert.Throws<InvalidOperationException>(() => { var _ = cancelationSource.Token; });
                    Assert.Throws<InvalidOperationException>(() => { cancelationSource.Cancel(); });
                    Assert.Throws<InvalidOperationException>(() => { cancelationSource.Cancel("Cancel"); });
                    Assert.Throws<InvalidOperationException>(() => { cancelationSource.Dispose(); });

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSourceNewIsValid()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    Assert.IsTrue(cancelationSource.IsValid);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSourceIsNotValidAfterDispose()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    cancelationSource.Dispose();
                    Assert.IsFalse(cancelationSource.IsValid);

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSourceIsValidAfterCancel0()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    cancelationSource.Cancel();
                    Assert.IsTrue(cancelationSource.IsValid);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSourceIsValidAfterCancel1()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    cancelationSource.Cancel("Canceled");
                    Assert.IsTrue(cancelationSource.IsValid);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSourceNoCancelationRequestedBeforeCanceled()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    Assert.IsFalse(cancelationSource.IsCancelationRequested);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSourceCancelationRequestedAfterCanceled0()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    cancelationSource.Cancel();
                    Assert.IsTrue(cancelationSource.IsCancelationRequested);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSourceCancelationRequestedAfterCanceled1()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    cancelationSource.Cancel("Canceled");
                    Assert.IsTrue(cancelationSource.IsCancelationRequested);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSource2WithTokenCancelationRequestedAfterToken1Canceled()
            {
                void Test()
                {
                    CancelationSource cancelationSource1 = CancelationSource.New();
                    CancelationSource cancelationSource2 = CancelationSource.New(cancelationSource1.Token);
                    cancelationSource1.Cancel();
                    Assert.IsTrue(cancelationSource2.IsCancelationRequested);
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSource1WithTokenNotCancelationRequestedAfterToken2Canceled()
            {
                void Test()
                {
                    CancelationSource cancelationSource1 = CancelationSource.New();
                    CancelationSource cancelationSource2 = CancelationSource.New(cancelationSource1.Token);
                    cancelationSource2.Cancel();
                    Assert.IsFalse(cancelationSource1.IsCancelationRequested);
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSource2CanceledWithSameValueAsToken1()
            {
                void Test()
                {
                    CancelationSource cancelationSource1 = CancelationSource.New();
                    CancelationSource cancelationSource2 = CancelationSource.New(cancelationSource1.Token);
                    string cancelValue = "CancelValue";
                    bool invoked = false;
                    cancelationSource2.Token.Register(reason =>
                    {
                        Assert.AreEqual(cancelValue, reason.Value);
                        invoked = true;
                    });
                    cancelationSource1.Cancel(cancelValue);
                    Assert.IsTrue(invoked);
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSource3WithTokensCancelationRequestedAfterToken1Canceled_0()
            {
                void Test()
                {
                    CancelationSource cancelationSource1 = CancelationSource.New();
                    CancelationSource cancelationSource2 = CancelationSource.New();
                    CancelationSource cancelationSource3 = CancelationSource.New(cancelationSource1.Token, cancelationSource2.Token);
                    cancelationSource1.Cancel();
                    Assert.IsTrue(cancelationSource3.IsCancelationRequested);
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                    cancelationSource3.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSource3WithTokensCancelationRequestedAfterToken2Canceled_0()
            {
                void Test()
                {
                    CancelationSource cancelationSource1 = CancelationSource.New();
                    CancelationSource cancelationSource2 = CancelationSource.New();
                    CancelationSource cancelationSource3 = CancelationSource.New(cancelationSource1.Token, cancelationSource2.Token);
                    cancelationSource2.Cancel();
                    Assert.IsTrue(cancelationSource3.IsCancelationRequested);
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                    cancelationSource3.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSource1WithTokensNotCancelationRequestedAfterToken2Canceled_0()
            {
                void Test()
                {
                    CancelationSource cancelationSource1 = CancelationSource.New();
                    CancelationSource cancelationSource2 = CancelationSource.New();
                    CancelationSource cancelationSource3 = CancelationSource.New(cancelationSource1.Token, cancelationSource2.Token);
                    cancelationSource2.Cancel();
                    Assert.IsFalse(cancelationSource1.IsCancelationRequested);
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                    cancelationSource3.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSource1And2WithTokensNotCancelationRequestedAfterToken3Canceled_0()
            {
                void Test()
                {
                    CancelationSource cancelationSource1 = CancelationSource.New();
                    CancelationSource cancelationSource2 = CancelationSource.New();
                    CancelationSource cancelationSource3 = CancelationSource.New(cancelationSource1.Token, cancelationSource2.Token);
                    cancelationSource3.Cancel();
                    Assert.IsFalse(cancelationSource1.IsCancelationRequested);
                    Assert.IsFalse(cancelationSource2.IsCancelationRequested);
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                    cancelationSource3.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSource3CanceledWithSameValueAsToken1_0()
            {
                void Test()
                {
                    CancelationSource cancelationSource1 = CancelationSource.New();
                    CancelationSource cancelationSource2 = CancelationSource.New();
                    CancelationSource cancelationSource3 = CancelationSource.New(cancelationSource1.Token, cancelationSource2.Token);
                    string cancelValue = "CancelValue";
                    bool invoked = false;
                    cancelationSource3.Token.Register(reason =>
                    {
                        Assert.AreEqual(cancelValue, reason.Value);
                        invoked = true;
                    });
                    cancelationSource1.Cancel(cancelValue);
                    Assert.IsTrue(invoked);
                    invoked = false;
                    cancelationSource2.Cancel("Different value");
                    Assert.IsFalse(invoked);
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                    cancelationSource3.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSource3CanceledWithSameValueAsToken2_0()
            {
                void Test()
                {
                    CancelationSource cancelationSource1 = CancelationSource.New();
                    CancelationSource cancelationSource2 = CancelationSource.New();
                    CancelationSource cancelationSource3 = CancelationSource.New(cancelationSource1.Token, cancelationSource2.Token);
                    string cancelValue = "CancelValue";
                    bool invoked = false;
                    cancelationSource3.Token.Register(reason =>
                    {
                        Assert.AreEqual(cancelValue, reason.Value);
                        invoked = true;
                    });
                    cancelationSource2.Cancel(cancelValue);
                    Assert.IsTrue(invoked);
                    invoked = false;
                    cancelationSource1.Cancel("Different value");
                    Assert.IsFalse(invoked);
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                    cancelationSource3.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSource3WithTokensCancelationRequestedAfterToken1Canceled_1()
            {
                void Test()
                {
                    CancelationSource cancelationSource1 = CancelationSource.New();
                    CancelationSource cancelationSource2 = CancelationSource.New();
                    CancelationSource cancelationSource3 = CancelationSource.New(new CancelationToken[] { cancelationSource1.Token, cancelationSource2.Token });
                    cancelationSource1.Cancel();
                    Assert.IsTrue(cancelationSource3.IsCancelationRequested);
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                    cancelationSource3.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSource3WithTokensCancelationRequestedAfterToken2Canceled_1()
            {
                void Test()
                {
                    CancelationSource cancelationSource1 = CancelationSource.New();
                    CancelationSource cancelationSource2 = CancelationSource.New();
                    CancelationSource cancelationSource3 = CancelationSource.New(new CancelationToken[] { cancelationSource1.Token, cancelationSource2.Token });
                    cancelationSource2.Cancel();
                    Assert.IsTrue(cancelationSource3.IsCancelationRequested);
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                    cancelationSource3.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSource1WithTokensNotCancelationRequestedAfterToken2Canceled_1()
            {
                void Test()
                {
                    CancelationSource cancelationSource1 = CancelationSource.New();
                    CancelationSource cancelationSource2 = CancelationSource.New();
                    CancelationSource cancelationSource3 = CancelationSource.New(new CancelationToken[] { cancelationSource1.Token, cancelationSource2.Token });
                    cancelationSource2.Cancel();
                    Assert.IsFalse(cancelationSource1.IsCancelationRequested);
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                    cancelationSource3.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSource1And2WithTokensNotCancelationRequestedAfterToken3Canceled_1()
            {
                void Test()
                {
                    CancelationSource cancelationSource1 = CancelationSource.New();
                    CancelationSource cancelationSource2 = CancelationSource.New();
                    CancelationSource cancelationSource3 = CancelationSource.New(new CancelationToken[] { cancelationSource1.Token, cancelationSource2.Token });
                    cancelationSource3.Cancel();
                    Assert.IsFalse(cancelationSource1.IsCancelationRequested);
                    Assert.IsFalse(cancelationSource2.IsCancelationRequested);
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                    cancelationSource3.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSource3CanceledWithSameValueAsToken1_1()
            {
                void Test()
                {
                    CancelationSource cancelationSource1 = CancelationSource.New();
                    CancelationSource cancelationSource2 = CancelationSource.New();
                    CancelationSource cancelationSource3 = CancelationSource.New(new CancelationToken[] { cancelationSource1.Token, cancelationSource2.Token });
                    string cancelValue = "CancelValue";
                    bool invoked = false;
                    cancelationSource3.Token.Register(reason =>
                    {
                        Assert.AreEqual(cancelValue, reason.Value);
                        invoked = true;
                    });
                    cancelationSource1.Cancel(cancelValue);
                    Assert.IsTrue(invoked);
                    invoked = false;
                    cancelationSource2.Cancel("Different value");
                    Assert.IsFalse(invoked);
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                    cancelationSource3.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationSource3CanceledWithSameValueAsToken2_1()
            {
                void Test()
                {
                    CancelationSource cancelationSource1 = CancelationSource.New();
                    CancelationSource cancelationSource2 = CancelationSource.New();
                    CancelationSource cancelationSource3 = CancelationSource.New(new CancelationToken[] { cancelationSource1.Token, cancelationSource2.Token });
                    string cancelValue = "CancelValue";
                    bool invoked = false;
                    cancelationSource3.Token.Register(reason =>
                    {
                        Assert.AreEqual(cancelValue, reason.Value);
                        invoked = true;
                    });
                    cancelationSource2.Cancel(cancelValue);
                    Assert.IsTrue(invoked);
                    invoked = false;
                    cancelationSource1.Cancel("Different value");
                    Assert.IsFalse(invoked);
                    cancelationSource1.Dispose();
                    cancelationSource2.Dispose();
                    cancelationSource3.Dispose();

                }

                Test();
                TestHelper.Cleanup();
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
                void Test()
                {
                    CancelationToken cancelationToken = new CancelationToken();
                    Assert.IsFalse(cancelationToken.CanBeCanceled);

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void NewCancelationTokenNoCancelationRequested()
            {
                void Test()
                {
                    CancelationToken cancelationToken = new CancelationToken();
                    Assert.IsFalse(cancelationToken.IsCancelationRequested);

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenNoCancelationRequestedBeforeCanceled()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    Assert.IsFalse(cancelationToken.IsCancelationRequested);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenCancelationRequestedAfterCanceled0()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Cancel();
                    Assert.IsTrue(cancelationToken.IsCancelationRequested);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenCancelationRequestedAfterCanceled1()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Cancel("Canceled");
                    Assert.IsTrue(cancelationToken.IsCancelationRequested);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenInvalidOperations()
            {
                void Test()
                {
                    CancelationToken cancelationToken = new CancelationToken();
                    Assert.Throws<InvalidOperationException>(() => { var _ = cancelationToken.CancelationValue; });
                    Assert.Throws<InvalidOperationException>(() => { var _ = cancelationToken.CancelationValueType; });
                    Assert.Throws<InvalidOperationException>(() => { cancelationToken.Register(_ => { }); });
                    Assert.Throws<InvalidOperationException>(() => { cancelationToken.Register(1, (i, _) => { }); });
                    Assert.Throws<InvalidOperationException>(() => { string _; cancelationToken.TryGetCancelationValueAs(out _); });
                    Assert.Throws<InvalidOperationException>(cancelationToken.Retain);
                    Assert.Throws<InvalidOperationException>(cancelationToken.Release);

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenFromSourceCanBeCanceled()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    Assert.IsTrue(cancelationToken.CanBeCanceled);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenFromSourceCannotBeCanceledAfterSourceIsDisposed()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Dispose();
                    Assert.IsFalse(cancelationToken.CanBeCanceled);

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenFromSourceCancelationIsRequested0()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Cancel();
                    Assert.IsTrue(cancelationToken.IsCancelationRequested);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenFromSourceCancelationIsRequested1()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Cancel("Cancel");
                    Assert.IsTrue(cancelationToken.IsCancelationRequested);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenFromSourceCancelationIsNotRequestedAfterSourceIsDisposed0()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Cancel();
                    cancelationSource.Dispose();
                    Assert.IsFalse(cancelationToken.IsCancelationRequested);

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenFromSourceCancelationIsNotRequestedAfterSourceIsDisposed1()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Cancel("Cancel");
                    cancelationSource.Dispose();
                    Assert.IsFalse(cancelationToken.IsCancelationRequested);

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenValueTypeIsNull0()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Cancel();
                    Assert.IsNull(cancelationToken.CancelationValueType);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenValueTypeIsNull1()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Cancel(default(string));
                    Assert.IsNull(cancelationToken.CancelationValueType);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenValueTypeIsString()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Cancel("Cancel");
                    Assert.IsTrue(cancelationToken.CancelationValueType == typeof(string));
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenValueIsNull0()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Cancel();
                    Assert.IsNull(cancelationToken.CancelationValue);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenValueIsNull1()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Cancel(default(string));
                    Assert.IsNull(cancelationToken.CancelationValue);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenValueMatchesCancelValue()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    string cancelValue = "Cancel";
                    cancelationSource.Cancel(cancelValue);
                    Assert.AreEqual(cancelValue, cancelationToken.CancelationValue);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenNullValueCannotBeGottenAsString0()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Cancel();
                    string val;
                    Assert.IsFalse(cancelationToken.TryGetCancelationValueAs(out val));
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenNullValueCannotBeGottenAsString1()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Cancel(default(string));
                    string val;
                    Assert.IsFalse(cancelationToken.TryGetCancelationValueAs(out val));
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenStringValueCanBeGottenAsString()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Cancel("Cancel");
                    string val;
                    Assert.IsTrue(cancelationToken.TryGetCancelationValueAs(out val));
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void RetainedCancelationTokenFromSourceCanBeCanceledAfterSourceIsDisposed()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationToken.Retain();
                    cancelationSource.Dispose();
                    Assert.IsTrue(cancelationToken.CanBeCanceled);
                    cancelationToken.Release();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void ReleasedCancelationTokenFromSourceCanBeCanceledAfterSourceIsDisposed()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationToken.Retain();
                    cancelationSource.Dispose();
                    cancelationToken.Release();
                    Assert.IsFalse(cancelationToken.CanBeCanceled);

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void RetainedCancelationTokenFromSourceCancelationIsRequestedAfterSourceIsDisposed0()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Cancel();
                    cancelationToken.Retain();
                    cancelationSource.Dispose();
                    Assert.IsTrue(cancelationToken.IsCancelationRequested);
                    cancelationToken.Release();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void RetainedCancelationTokenFromSourceCancelationIsRequestedAfterSourceIsDisposed1()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Cancel("Cancel");
                    cancelationToken.Retain();
                    cancelationSource.Dispose();
                    Assert.IsTrue(cancelationToken.IsCancelationRequested);
                    cancelationToken.Release();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void ReleasedCancelationTokenFromSourceCancelationIsNotRequestedAfterSourceIsDisposed0()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Cancel();
                    cancelationToken.Retain();
                    cancelationSource.Dispose();
                    cancelationToken.Release();
                    Assert.IsFalse(cancelationToken.IsCancelationRequested);

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void ReleasedCancelationTokenFromSourceCancelationIsNotRequestedAfterSourceIsDisposed1()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationSource.Cancel("Cancel");
                    cancelationToken.Retain();
                    cancelationSource.Dispose();
                    cancelationToken.Release();
                    Assert.IsFalse(cancelationToken.IsCancelationRequested);

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenThrowIfCancelationRequested0()
            {
                void Test()
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

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenThrowIfCancelationRequested1()
            {
                void Test()
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

                }

                Test();
                TestHelper.Cleanup();
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
                void Test()
                {
                    CancelationRegistration cancelationRegistration = new CancelationRegistration();
                    Assert.IsFalse(cancelationRegistration.IsRegistered);

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void RegistrationFromCancelationTokenIsRegistered0()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    CancelationRegistration cancelationRegistration = cancelationToken.Register(_ => { });
                    Assert.IsTrue(cancelationRegistration.IsRegistered);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void RegistrationFromCancelationTokenIsRegistered1()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    CancelationRegistration cancelationRegistration = cancelationToken.Register(0, (i, _) => { });
                    Assert.IsTrue(cancelationRegistration.IsRegistered);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void RegistrationFromCancelationTokenIsNotRegisteredAfterInvoked0()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    CancelationRegistration cancelationRegistration = cancelationToken.Register(_ => { });
                    cancelationSource.Cancel();
                    Assert.IsFalse(cancelationRegistration.IsRegistered);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void RegistrationFromCancelationTokenIsNotRegisteredAfterInvoked1()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    CancelationRegistration cancelationRegistration = cancelationToken.Register(0, (i, _) => { });
                    cancelationSource.Cancel();
                    Assert.IsFalse(cancelationRegistration.IsRegistered);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void RegistrationFromCancelationTokenIsNotRegisteredAfterInvoked2()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    CancelationRegistration cancelationRegistration = new CancelationRegistration();
                    cancelationRegistration = cancelationToken.Register(_ => Assert.IsFalse(cancelationRegistration.IsRegistered));
                    cancelationSource.Cancel();
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void RegistrationFromCancelationTokenIsNotRegisteredAfterInvoked3()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    CancelationRegistration cancelationRegistration = new CancelationRegistration();
                    cancelationRegistration = cancelationToken.Register(0, (i, _) => Assert.IsFalse(cancelationRegistration.IsRegistered));
                    cancelationSource.Cancel();
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void RegistrationFromCancelationTokenIsNotRegisteredAfterSourceIsDisposed0()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    CancelationRegistration cancelationRegistration = cancelationToken.Register(_ => { });
                    cancelationSource.Dispose();
                    Assert.IsFalse(cancelationRegistration.IsRegistered);

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void RegistrationFromCancelationTokenIsNotRegisteredAfterSourceIsDisposed1()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    CancelationRegistration cancelationRegistration = cancelationToken.Register(0, (i, _) => { });
                    cancelationSource.Dispose();
                    Assert.IsFalse(cancelationRegistration.IsRegistered);

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenRegisterCallbackIsInvoked0()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    bool invoked = false;
                    cancelationToken.Register(_ => invoked = true);
                    cancelationSource.Cancel();
                    Assert.IsTrue(invoked);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenRegisterCallbackIsInvoked1()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    bool invoked = false;
                    cancelationToken.Register(0, (i, _) => invoked = true);
                    cancelationSource.Cancel();
                    Assert.IsTrue(invoked);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenRegisterCallbackIsInvoked2()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    bool invoked = false;
                    cancelationToken.Register(_ => invoked = true);
                    cancelationSource.Cancel("Cancel");
                    Assert.IsTrue(invoked);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenRegisterCallbackIsInvoked3()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    bool invoked = false;
                    cancelationToken.Register(0, (i, _) => invoked = true);
                    cancelationSource.Cancel("Cancel");
                    Assert.IsTrue(invoked);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationRegistrationUnregisterCallbackIsNotInvoked0()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    bool invoked = false;
                    CancelationRegistration cancelationRegistration = cancelationToken.Register(_ => invoked = true);
                    cancelationRegistration.Unregister();
                    cancelationSource.Cancel();
                    Assert.IsFalse(invoked);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationRegistrationUnregisterCallbackIsNotInvoked1()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    bool invoked = false;
                    CancelationRegistration cancelationRegistration = cancelationToken.Register(0, (i, _) => invoked = true);
                    cancelationRegistration.Unregister();
                    cancelationSource.Cancel();
                    Assert.IsFalse(invoked);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationRegistrationUnregisterCallbackIsNotInvoked2()
            {
                void Test()
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

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationRegistrationUnregisterCallbackIsNotInvoked3()
            {
                void Test()
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

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void CancelationTokenRegisterCaptureVariableMatches()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    string expected = "Captured";
                    cancelationToken.Register(expected, (cv, _) => Assert.AreEqual(expected, cv));
                    cancelationSource.Cancel();
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void RegisteredCallbacksAreInvokedAfterSourceIsDisposed()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    bool invoked = false;
                    // This should never be done in practice!
                    cancelationToken.Register(_ => cancelationSource.Dispose());
                    cancelationToken.Register(_ => invoked = true);
                    cancelationSource.Cancel();
                    Assert.IsTrue(invoked);

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void RegisteredCallbackIsRegisteredAfterSourceIsDisposedDuringInvocation()
            {
                void Test()
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

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void RegisteredCallbackExceptionPropagatesToCancelCaller()
            {
                void Test()
                {
                    CancelationSource cancelationSource = CancelationSource.New();
                    CancelationToken cancelationToken = cancelationSource.Token;
                    cancelationToken.Register(_ =>
                    {
                        throw new Exception();
                    });
                    Assert.Throws<AggregateException>(cancelationSource.Cancel);
                    cancelationSource.Dispose();

                }

                Test();
                TestHelper.Cleanup();
            }

            [Test]
            public void RegisteredCallbacksAreInvokedEvenWhenAnExceptionOccurs()
            {
                void Test()
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

                }

                Test();
                TestHelper.Cleanup();
            }
        }
    }
}