#pragma warning disable IDE0041 // Use 'is null' check
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0074 // Use compound assignment
#pragma warning disable IDE0090 // Use 'new(...)'
#pragma warning disable IDE0180 // Use tuple to swap values

using Proto.Promises.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace Proto.Promises
{
#if !PROTO_PROMISE_DEVELOPER_MODE
    [DebuggerNonUserCode, StackTraceHidden]
#endif
    internal static partial class InternalHelper
    {
        // We initialize the config as early as possible. Ideally we would just do this in static constructors of Promise(<T>) and Promise.Config,
        // but since this is in a separate assembly, that's not possible.
        // Also, using static constructors would slightly slow down promises in IL2CPP where it would have to check if it already ran on every call.
        // We can't use SubsystemRegistration or AfterAssembliesLoaded which run before BeforeSceneLoad, because it forcibly destroys the MonoBehaviour.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void InitializePromiseConfig()
        {
            PromiseBehaviour.Initialize();
        }

        // AppDomain reload could be disabled in editor, so we need to explicitly reset static fields.
        // See https://github.com/timcassell/ProtoPromise/issues/204
        // https://docs.unity3d.com/Manual/DomainReloading.html
        [RuntimeInitializeOnLoadMethod((RuntimeInitializeLoadType) 4)] // SubsystemRegistration
        internal static void ResetStaticState()
        {
            PromiseBehaviour.ResetStaticState();
        }

#if !PROTO_PROMISE_DEVELOPER_MODE
        [DebuggerNonUserCode, StackTraceHidden]
#endif
        [AddComponentMenu("")] // Hide this in the add component menu.
        internal sealed partial class PromiseBehaviour : MonoBehaviour
        {
            private static PromiseBehaviour s_instance;
            internal static PromiseBehaviour Instance
            {
                [MethodImpl(Internal.InlineOption)]
                get { return s_instance; }
            }

            internal readonly PromiseSynchronizationContext _syncContext = new PromiseSynchronizationContext();
            private Queue<UnhandledException> _currentlyReportingExceptions = new Queue<UnhandledException>();
            private Queue<UnhandledException> _unhandledExceptions = new Queue<UnhandledException>();
            private SynchronizationContext _oldContext;
            private bool _isApplicationQuitting = false;

            internal static void Initialize()
            {
                // Even though we try to initialize this as early as possible, it is possible for other code to run before this.
                // So we need to be careful to not overwrite non-default values.

#pragma warning disable 0612 // Type or member is obsolete
                if (Promise.Config.WarningHandler == null)
                {
                    // Set default warning handler to route to UnityEngine.Debug.
                    Promise.Config.WarningHandler = UnityEngine.Debug.LogWarning;
                }
#pragma warning restore 0612 // Type or member is obsolete

                // Create a PromiseBehaviour instance before any promise actions are made.
                // Unity will throw if this is not ran on the main thread.
                new GameObject("Proto.Promises.Unity.PromiseBehaviour")
                    .AddComponent<PromiseBehaviour>()
                    .SetSynchronizationContext();

                StaticInit();
            }

            private void SetSynchronizationContext()
            {
                if (Promise.Config.ForegroundContext == null)
                {
                    Promise.Config.ForegroundContext = _syncContext;
                }
                if (Promise.Config.UncaughtRejectionHandler == null)
                {
                    Promise.Config.UncaughtRejectionHandler = HandleRejection;
                }

                // We set the current context even when UnitySynchronizationContext exists, because it has a poor implementation.
                // We store the old context in case this gets destroyed for some reason.
                _oldContext = SynchronizationContext.Current;
                SynchronizationContext.SetSynchronizationContext(_syncContext);
                Promise.Manager.ThreadStaticSynchronizationContext = _syncContext;
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
                Init();
            }

            static partial void StaticInit();
            partial void Init();

            // This should never be called except when the application is shutting down.
            // Users would have to go out of their way to find and destroy the PromiseBehaviour instance.
            private void OnDestroy()
            {
                if (!_isApplicationQuitting & s_instance == this)
                {
                    UnityEngine.Debug.LogError("PromiseBehaviour destroyed! Removing PromiseSynchronizationContext from Promise.Config.ForegroundContext. PromiseYielder functions will stop working.");
                    ResetConfig();
                    ResetStaticState();
                }
            }

            private void HandleRejection(UnhandledException exception)
            {
                // We report uncaught rejections in Update instead of directly sending them to UnityEngine.Debug.LogException,
                // so that we can minimize the extra stack frames in the logs that we don't care about.
                lock (_unhandledExceptions)
                {
                    _unhandledExceptions.Enqueue(exception);
                }
            }

            private void Update()
            {
                try
                {
                    _syncContext.Execute();
                }
                // In case someone clears `Promise.Config.UncaughtRejectionHandler`, we catch the exception here and log it.
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }

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

                ProcessUpdate();
            }

            partial void ProcessUpdate();

            private void OnApplicationQuit()
            {
                _isApplicationQuitting = true;
                if (Application.isEditor & s_instance == this)
                {
                    // Destroy this to prevent a memory leak.
                    Destroy(this);
                }
            }

            partial void ResetProcessors();

            private void ResetConfig()
            {
                if (Promise.Config.ForegroundContext == _syncContext)
                {
                    Promise.Config.ForegroundContext = null;
                }
                if (Promise.Config.UncaughtRejectionHandler == HandleRejection)
                {
                    Promise.Config.UncaughtRejectionHandler = null;
                }
                if (Promise.Manager.ThreadStaticSynchronizationContext == _syncContext)
                {
                    Promise.Manager.ThreadStaticSynchronizationContext = null;
                }
                if (SynchronizationContext.Current == _syncContext)
                {
                    SynchronizationContext.SetSynchronizationContext(_oldContext);
                }
            }

            internal static void ResetStaticState()
            {
                if (!ReferenceEquals(s_instance, null))
                {
                    s_instance.ResetProcessors();
                    s_instance.ResetConfig();
                    s_instance = null;
                }
            }
        }
    }
}