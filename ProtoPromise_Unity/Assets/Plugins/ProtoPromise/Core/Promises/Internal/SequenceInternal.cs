#pragma warning disable IDE0034 // Simplify 'default' expression
#pragma warning disable CS0420 // A reference to a volatile field will not be treated as volatile

using System;
using System.Collections.Generic;
using System.Threading;

namespace Proto.Promises
{
    partial class Internal
    {
        partial class PromiseRef
        {
            public static Promise CreateSequence<TEnumerator>(TEnumerator promiseFuncs, CancelationToken cancelationToken = default(CancelationToken)) where TEnumerator : IEnumerator<Func<Promise>>
            {
                ValidateArgument(promiseFuncs, "promiseFuncs", 2);

                if (!promiseFuncs.MoveNext())
                {
                    return CreateResolved();
                }

                // Invoke funcs and normalize the progress.
                Promise promise;
                if (cancelationToken.IsCancelationRequested)
                {
                    var newPromise = CancelablePromiseResolvePromise<VoidResult, VoidResult, DelegateVoidPromise>.GetOrCreate(DelegateWrapper.Create(promiseFuncs.Current), cancelationToken, 0);
                    promise = new Promise(newPromise, newPromise.Id, 0);
                }
                else
                {
                    try
                    {
                        promise = CallbackHelper.AdoptDirect(promiseFuncs.Current.Invoke()._target, -1);
                    }
                    catch (OperationCanceledException e)
                    {
                        promise = Promise.Canceled(e);
                    }
                    catch (Exception e)
                    {
                        promise = Promise.Rejected(e);
                    }
                }

                while (promiseFuncs.MoveNext())
                {
                    promise = promise.Then(promiseFuncs.Current, cancelationToken);
                }
                return promise;
            }
        }
    }
}