#pragma warning disable IDE0017 // Simplify object initialization
#pragma warning disable IDE0034 // Simplify 'default' expression

using System;
using System.Collections.Generic;

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
                PromiseRef rootPromise;
                if (cancelationToken.CanBeCanceled)
                {
                    rootPromise = RefCreator.CreateResolveWait(DelegateWrapper.CreateCancelable(promiseFuncs.Current), cancelationToken);
                    if (rootPromise._valueOrPrevious != null)
                    {
                        // Cancelation token was already canceled, return the canceled promise.
                        return new Promise(rootPromise, rootPromise.Id);
                    }
                }
                else
                {
                    rootPromise = RefCreator.CreateResolveWait(DelegateWrapper.Create(promiseFuncs.Current));
                }
                rootPromise._valueOrPrevious = ResolveContainerVoid.GetOrCreate();
                rootPromise.ResetDepth();

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