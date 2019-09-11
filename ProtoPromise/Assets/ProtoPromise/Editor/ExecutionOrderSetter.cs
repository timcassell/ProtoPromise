using UnityEditor;
using System.Reflection;

namespace Proto.Promises
{
    partial class Promise
    {
        [InitializeOnLoad]
        public static class ExecutionOrderSetter
        {
            static ExecutionOrderSetter()
            {
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
}