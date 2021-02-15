#pragma warning disable IDE0034 // Simplify 'default' expression

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

                // Invoke funcs async and normalize the progress.
                PromiseRef rootPromise = cancelationToken.CanBeCanceled
                    ? CancelablePromiseResolvePromise<DelegateVoidPromise>.GetOrCreate(DelegateWrapper.Create(promiseFuncs.Current), cancelationToken)
                    : (PromiseRef) PromiseResolvePromise<DelegateVoidPromise>.GetOrCreate(DelegateWrapper.Create(promiseFuncs.Current));
                rootPromise.ResetDepth();
                Interlocked.CompareExchange(ref rootPromise._valueOrPrevious, ResolveContainerVoid.GetOrCreate(), null);

                Promise promise = new Promise(rootPromise, rootPromise.Id);
                while (promiseFuncs.MoveNext())
                {
                    promise = promise.Then(promiseFuncs.Current, cancelationToken);
                }
                AddToHandleQueueBack(rootPromise);
                return promise;
            }
        }
    }
}