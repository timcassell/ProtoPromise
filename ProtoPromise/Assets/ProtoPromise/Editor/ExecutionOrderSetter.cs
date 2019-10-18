using UnityEditor;
using System.Reflection;

namespace Proto.Promises
{
    [InitializeOnLoad]
    internal static class ExecutionOrderSetter
    {
        static ExecutionOrderSetter()
        {
#if CSHARP_7_OR_LATER
            // Get the max execution order. Use reflection to get the const value just in case Unity decides to change it in the future.
            int maxExecutionOrder = int.MaxValue;

            // For some dumb reason, type.GetRuntimeField() only returns public fields. GetRuntimeFields() returns private fields.
            foreach (var field in typeof(MonoImporter).Assembly.GetType("UnityEditor.ScriptExecutionOrderInspector").GetRuntimeFields())
            {
                if (field.Name == "kOrderRangeMax")
                {
                    maxExecutionOrder = (int) field.GetRawConstantValue();
                    break;
                }
            }
#else
                // GetRuntimeFields doesn't exist prior to .Net 4.0, and GetFields doesn't get private const fields.
                // Also, we can be confident that the max execution order will forever be 32000 in the deprecated scripting runtime version.
                int maxExecutionOrder = 32000;
#endif

            foreach (MonoScript monoScript in MonoImporter.GetAllRuntimeMonoScripts())
            {
                if (monoScript.GetClass() == typeof(PromiseBehaviour))
                {
                    var currentOrder = MonoImporter.GetExecutionOrder(monoScript);
                    if (currentOrder != maxExecutionOrder)
                    {
                        // Set the PromiseBehaviour to always execute last.
                        MonoImporter.SetExecutionOrder(monoScript, maxExecutionOrder);
                    }
                    break;
                }
            }
        }
    }
}