using Proto.Promises.Threading;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Proto.Promises
{
    partial struct Promise
    {
        // Promise is backed by Promise<Internal.VoidResult>, so we don't need a static constructor for it.
        public static partial class Config
        {
            // Static constructor on Config to make sure the ForegroundContext is set (in case users want to copy it to the BackgroundContext for WebGL).
            // This also prevents the PromiseBehaviour from overwriting it in case users set their own ForegroundContext before the Promise<T> static constructor is ran.
            static Config()
            {
                Unity.PromiseBehaviour.Init();
            }

        }
    }

    partial struct Promise<T>
    {
        static Promise()
        {
            Unity.PromiseBehaviour.Init();
        }
    }

    namespace Unity // I would have nested this within Internal, but you can only change the execution order of public, un-nested behaviours, so add a nested namespace instead.
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public sealed class PromiseBehaviour : MonoBehaviour
        {
            // Dummy class is to prevent error:
            // UnityException: get_isPlayingOrWillChangePlaymode is not allowed to be called from a MonoBehaviour constructor (or instance field initializer),
            // call it in Awake or Start instead.
            private static class Dummy
            {
                // NoInlining is to ensure that the static constructor runs.
                [MethodImpl(MethodImplOptions.NoInlining)]
                public static void Init() { }

                static Dummy()
                {
#pragma warning disable 0612 // Type or member is obsolete
                    // Set default warning handler to route to UnityEngine.Debug.
                    Promise.Config.WarningHandler = Debug.LogWarning;
#pragma warning restore 0612 // Type or member is obsolete

#if UNITY_EDITOR
                    // TODO: make foreground context work in edit mode also.
                    if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
#endif
                    {
                        // Create a PromiseBehaviour instance before any promise actions are made.
                        // Unity will throw if this is not ran on the main thread.
                        new GameObject("Proto.Promises.Unity.PromiseBehaviour")
                            .AddComponent<PromiseBehaviour>()
                            .SetSynchronizationContext();
                    }
                }
            }

            private static PromiseBehaviour _instance;

            private readonly PromiseSynchronizationContext _syncContext = new PromiseSynchronizationContext();
            // These must not be readonly.
            private Internal.ValueLinkedStack<UnhandledException> _unhandledExceptions = new Internal.ValueLinkedStack<UnhandledException>();
            private Internal.SpinLocker _unhandledExceptionsLocker;

            [MethodImpl(Internal.InlineOption)]
            internal static void Init()
            {
                Dummy.Init();
            }

            private void SetSynchronizationContext()
            {
                if (_instance == null)
                {
                    Promise.Config.ForegroundContext = _syncContext;
                    // Intercept uncaught rejections and report them in UpdateRoutine instead of directly sending them to UnityEngine.Debug.LogException
                    // so that we can minimize the extra stack frames in the logs that we don't care about.
                    Promise.Config.UncaughtRejectionHandler = HandleRejection;
                }
            }

            private void Start()
            {
                if (_instance != null)
                {
                    Debug.LogWarning("There can only be one instance of PromiseBehaviour. Destroying new instance.");
                    Destroy(this);
                    return;
                }
                DontDestroyOnLoad(gameObject);
                gameObject.hideFlags = HideFlags.HideAndDontSave; // Don't show in hierarchy and don't destroy.
                _instance = this;
                StartCoroutine(UpdateRoutine());
            }

            // This should never be called except when the application is shutting down.
            // Users would have to go out of their way to find and destroy the PromiseBehaviour instance.
            private void OnDestroy()
            {
#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
#endif
                {
                    if (_instance == this)
                    {
                        Debug.LogWarning("PromiseBehaviour destroyed! Removing PromiseSynchronizationContext from Promise.Config.ForegroundContext.");
                        _instance = null;
                        if (Promise.Config.ForegroundContext == _syncContext)
                        {
                            Promise.Config.ForegroundContext = null;
                        }
                        if (Promise.Config.UncaughtRejectionHandler == HandleRejection)
                        {
                            Promise.Config.UncaughtRejectionHandler = null;
                        }
                        _syncContext.Execute(); // Clear out any pending callbacks.
                    }
                }
            }

            private void HandleRejection(UnhandledException exception)
            {
                _unhandledExceptionsLocker.Enter();
                _unhandledExceptions.Push(exception);
                _unhandledExceptionsLocker.Exit();
            }

            // Execute SynchronizationContext callback in Coroutine rather than in Update.
            private IEnumerator UpdateRoutine()
            {
                // We end up missing the first frame here, but that's not a big deal.
                while (true)
                {
                    yield return null;
                    try
                    {
                        _syncContext.Execute();
                    }
                    // In case someone clears `Promise.Config.UncaughtRejectionHandler`, we catch the AggregateException here and log it so that the coroutine won't stop.
                    catch (System.AggregateException e)
                    {
                        Debug.LogException(e);
                    }

                    // Pop and pass to UnityEngine.Debug here so Unity won't add extra stackframes that we don't care about.
                    _unhandledExceptionsLocker.Enter();
                    var unhandledExceptions = _unhandledExceptions;
                    _unhandledExceptions = new Internal.ValueLinkedStack<UnhandledException>();
                    _unhandledExceptionsLocker.Exit();

                    while (unhandledExceptions.IsNotEmpty)
                    {
                        // Unfortunately, Unity does not provide a means to completely eliminate the stack trace at the point of calling `Debug.Log`, so the log will always have at least 1 extra stack frame.
                        // This implementation minimizes it to 1 extra stack frame always (because `IEnumerator.MoveNext()` is called from Unity's side, and they do not include their own internal stack traces).
                        Debug.LogException(unhandledExceptions.Pop());
                    }
                }
            }
        }
    }
}