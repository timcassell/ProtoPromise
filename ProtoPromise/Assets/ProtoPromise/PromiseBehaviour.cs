using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Proto.Promises
{
    partial class Promise
    {
        static Promise()
        {
            new GameObject("ProtoPromise.PromiseBehaviour")
            {
                hideFlags = HideFlags.HideAndDontSave // Don't show in hierarchy and don't destroy.
            }
            .AddComponent<PromiseBehaviour>();
        }

        private sealed class PromiseBehaviour : MonoBehaviour
        {
            private void Awake()
            {
                StartCoroutine(_Enumerator());
            }

            private IEnumerator _Enumerator()
            {
                var waitForEndOfFrame = new WaitForEndOfFrame();
                while (true)
                {
                    yield return waitForEndOfFrame;
                    Manager.HandleCompletes();
                    yield return null;
                    // Invoke progress delegates during the normal coroutine cycle.
                    Manager.HandleCompletesAndProgress();
                }
            }

            private void Update()
            {
                Manager.HandleCompletes();
            }

            // Optionally add extra calls for LateUpdate, FixedUpdate, etc.
        }
    }
}