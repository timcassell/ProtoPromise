#pragma warning disable IDE0051 // Remove unused private members

using Proto.Promises.Threading;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Proto.Promises
{
    partial struct Promise
    {
        static Promise()
        {
            Unity.PromiseBehaviour.Init();
        }

        partial class Config
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

    namespace Unity
    {
        // I would have nested this within Internal, but it had to be public for old versions, and I don't want to cause a compile error if for some strange reason a user is relying on this type.
        // So I added the EditorBrowsableAttribute to hide it in the IDE, and AddComponentMenuAttribute to hide it in the editor instead.
#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        [EditorBrowsable(EditorBrowsableState.Never)]
        [AddComponentMenu("")]
        public sealed class PromiseBehaviour : MonoBehaviour
        {
            // Dummy class is to prevent error:
            // UnityException: get_isPlayingOrWillChangePlaymode is not allowed to be called from a MonoBehaviour constructor (or instance field initializer),
            // call it in Awake or Start instead.
#if !PROTO_PROMISE_DEVELOPER_MODE
            [DebuggerNonUserCode, StackTraceHidden]
#endif
            private static class Dummy
            {
                // NoInlining is to ensure that the static constructor runs.
                [MethodImpl(MethodImplOptions.NoInlining)]
                public static void Init() { }
#if UNITY_EDITOR
                private static readonly System.Threading.SynchronizationContext s_unityContext;
#endif

                static Dummy()
                {
#pragma warning disable 0612 // Type or member is obsolete
                    // Set default warning handler to route to UnityEngine.Debug.
                    Promise.Config.WarningHandler = UnityEngine.Debug.LogWarning;
#pragma warning restore 0612 // Type or member is obsolete

#if UNITY_EDITOR
                    if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        // If we're in edit mode, just use Unity's synchronization context instead of ours.
                        // It may not exist in older Unity versions, in which case we just warn the user.
                        s_unityContext = System.Threading.SynchronizationContext.Current;
                        if (s_unityContext == null)
                        {
                            Promise.Config.UncaughtRejectionHandler = UnityEngine.Debug.LogException;
                            UnityEngine.Debug.LogWarning("There is no current SynchronizationContext, scheduling continuations on the foreground context may not work in edit mode. Set Promise.Config.ForegroundContext to enable foreground scheduling.");
                            return;
                        }
                        Promise.Config.ForegroundContext = s_unityContext;
                        Promise.Config.UncaughtRejectionHandler = e =>
                        {
                            // Route the exception through the context to avoid extra stack traces in the log.
                            s_unityContext.Post(ex => UnityEngine.Debug.LogException(ex as System.Exception), e);
                        };
                        return;
                    }
#endif
                    // Create a PromiseBehaviour instance before any promise actions are made.
                    // Unity will throw if this is not ran on the main thread.
                    new GameObject("Proto.Promises.Unity.PromiseBehaviour")
                        .AddComponent<PromiseBehaviour>()
                        .SetSynchronizationContext();
                }
            }

            private static PromiseBehaviour s_instance;

            private readonly PromiseSynchronizationContext _syncContext = new PromiseSynchronizationContext();
            private Queue<UnhandledException> _currentlyReportingExceptions = new Queue<UnhandledException>();
            private Queue<UnhandledException> _unhandledExceptions = new Queue<UnhandledException>();

            [MethodImpl(Internal.InlineOption)]
            internal static void Init()
            {
                Dummy.Init();
            }

            private void SetSynchronizationContext()
            {
                if (s_instance == null)
                {
                    Promise.Config.ForegroundContext = _syncContext;
                    // Intercept uncaught rejections and report them in Update instead of directly sending them to UnityEngine.Debug.LogException
                    // so that we can minimize the extra stack frames in the logs that we don't care about.
                    Promise.Config.UncaughtRejectionHandler = HandleRejection;
                }
            }

            private void Start()
            {
                if (s_instance != null)
                {
                    UnityEngine.Debug.LogWarning("There can only be one instance of PromiseBehaviour. Destroying new instance.");
                    Destroy(this);
                    return;
                }
                DontDestroyOnLoad(gameObject);
                gameObject.hideFlags = HideFlags.HideAndDontSave; // Don't show in hierarchy and don't destroy.
                s_instance = this;
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
                    if (s_instance == this)
                    {
                        UnityEngine.Debug.LogWarning("PromiseBehaviour destroyed! Removing PromiseSynchronizationContext from Promise.Config.ForegroundContext.");
                        s_instance = null;
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
                lock (_unhandledExceptions)
                {
                    _unhandledExceptions.Enqueue(exception);
                }
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
                    catch (AggregateException e)
                    {
                        UnityEngine.Debug.LogException(e);
                    }
                }
            }

            private void Update()
            {
                // Pop and pass to UnityEngine.Debug here so Unity won't add extra stackframes that we don't care about.
                object locker = _unhandledExceptions;
                lock (locker)
                {
                    var temp = _unhandledExceptions;
                    _unhandledExceptions = _currentlyReportingExceptions;
                    _currentlyReportingExceptions = temp;
                }

                while (_currentlyReportingExceptions.Count > 0)
                {
                    // Unfortunately, Unity does not provide a means to completely eliminate the stack trace at the point of calling `Debug.Log`, so the log will always have at least 1 extra stack frame.
                    // This implementation minimizes it to 1 extra stack frame always (because `Update()` is called from Unity's side, and they do not include their own internal stack traces).
                    UnityEngine.Debug.LogException(_currentlyReportingExceptions.Dequeue());
                }
            }
        }
    }
}