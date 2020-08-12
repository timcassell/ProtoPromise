using System; using System.Collections.Generic;  namespace Proto.Promises {     partial class Promise     {         partial class InternalProtected         {             public static Promise CreateSequence<TEnumerator>(TEnumerator promiseFuncs, CancelationToken cancelationToken = default(CancelationToken)) where TEnumerator : IEnumerator<Func<Promise>>             {                 ValidateArgument(promiseFuncs, "promiseFuncs", 2);                  if (!promiseFuncs.MoveNext())                 {                     return Resolved();                 }

                // Invoke funcs async and normalize the progress.
                Promise rootPromise;
                if (cancelationToken.CanBeCanceled)
                {
                    var newPromise = PromiseResolvePromise<DelegateVoidPromiseCancel>.GetOrCreate();
                    newPromise.resolver = new DelegateVoidPromiseCancel(promiseFuncs.Current, cancelationToken.RegisterInternal(newPromise));
                    // Set resolved value only if cancelation token wasn't already canceled (_valueOrPrevious will be a cancel value from being invoked synchronously).
                    if (newPromise._valueOrPrevious == null)
                    {
                        newPromise._valueOrPrevious = Internal.ResolveContainerVoid.GetOrCreate();
                    }
                    rootPromise = newPromise;
                }
                else
                {
                    var newPromise = PromiseResolvePromise<DelegateVoidPromise>.GetOrCreate();
                    newPromise.resolver = new DelegateVoidPromise(promiseFuncs.Current);
                    newPromise._valueOrPrevious = Internal.ResolveContainerVoid.GetOrCreate();
                    rootPromise = newPromise;
                }
                rootPromise.ResetDepth();                  Promise promise = rootPromise;                 while (promiseFuncs.MoveNext())                 {                     promise = promise.Then(promiseFuncs.Current, cancelationToken);                 }
                Internal.AddToHandleQueueBack(rootPromise);                 return promise;             }         }     } }