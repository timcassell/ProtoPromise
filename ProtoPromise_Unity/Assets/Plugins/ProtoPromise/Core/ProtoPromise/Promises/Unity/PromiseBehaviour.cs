using System.Collections;
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
    }

    partial struct Promise<T>
    {
        static Promise()
        {
            Unity.PromiseBehaviour.Init();
        }
    }

    namespace Unity // I would have nested this within Promise, but you can only change the execution order of un-nested behaviours, so add a nested namespace instead.
    {
#if !PROTO_PROMISE_DEVELOPER_MODE
        [System.Diagnostics.DebuggerNonUserCode]
#endif
        public sealed class PromiseBehaviour : MonoBehaviour
        {
            // To prevent error:
			// UnityException: get_isPlayingOrWillChangePlaymode is not allowed to be called from a MonoBehaviour constructor (or instance field initializer),
			// call it in Awake or Start instead.
            private static class Dummy
            {
                // NoInlining is to ensure that the static constructor runs.
                [MethodImpl(MethodImplOptions.NoInlining)]
                public static void Init() { }

                static Dummy()
                {
                    // Set default rejection and warning handlers to route to UnityEngine.Debug.
                    Promises.Promise.Config.UncaughtRejectionHandler = Debug.LogException;
                    Promises.Promise.Config.WarningHandler = Debug.LogWarning;
#if UNITY_EDITOR
                    if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
#endif
                    {
                        // Create a PromiseBehaviour instance before any promise actions are made.
                        new GameObject("Proto.Promises.Unity.PromiseBehaviour").AddComponent<PromiseBehaviour>();
                    }
                }
            }

            private static PromiseBehaviour _instance;

            [MethodImpl(Promises.Internal.InlineOption)]
            public static void Init()
            {
                Dummy.Init();
            }

            private void Start()
            {
                if (_instance != null)
                {
                    Promise.Manager.LogWarning("There can only be one instance of PromiseBehaviour. Destroying new instance.");
                    Destroy(this);
                    return;
                }
                DontDestroyOnLoad(gameObject);
                gameObject.hideFlags = HideFlags.HideAndDontSave; // Don't show in hierarchy and don't destroy.
                _instance = this;
                StartCoroutine(UpdateRoutine());
            }

            private void OnDestroy()
            {
#if UNITY_EDITOR
                if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
#endif
                {
                    if (_instance == this)
                    {
                        Promise.Manager.LogWarning("PromiseBehaviour destroyed! Promise callbacks will no longer be automatically invoked!");
                        _instance = null;
                    }
                }
            }

            private IEnumerator UpdateRoutine()
            {
                while (true)
                {
                    yield return null;
                    // Invoke progress delegates during the normal coroutine cycle.
                    Promise.Manager.HandleCompletesAndProgress();
                }
            }

            private void Update()
            {
                Promise.Manager.HandleCompletes();
            }

            // Optionally add extra HandleCompletes calls for LateUpdate, FixedUpdate, WaitForEndOfFrame, etc.
        }
    }
}