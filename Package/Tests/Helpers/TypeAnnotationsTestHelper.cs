#if PROTO_PROMISE_DEBUG_ENABLE || (!PROTO_PROMISE_DEBUG_DISABLE && DEBUG)
#define PROMISE_DEBUG
#else
#undef PROMISE_DEBUG
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Proto.Promises;
using NUnit.Framework;

namespace ProtoPromise.Tests.Annotations
{
    public static class TypeAnnotationsTestHelper
    {
#if PROTO_PROMISE_DEVELOPER_MODE
        public static void NoTypesAreAnnotatedWithNonUserCode(Assembly assembly)
        {
            var annotated = new List<string>();

            foreach (var type in GetAllTypes(assembly))
            {
                bool hasDebuggerNonUserCode = type.GetCustomAttributes(typeof(DebuggerNonUserCodeAttribute), false).Any();
                bool hasStackTraceHidden = type.GetCustomAttributes(typeof(StackTraceHiddenAttribute), false).Any();

                if (hasDebuggerNonUserCode || hasStackTraceHidden)
                {
                    annotated.Add($"{type.FullName} (Annotated: " +
                        $"{(hasDebuggerNonUserCode ? "" : "[DebuggerNonUserCode] ")}" +
                        $"{(hasStackTraceHidden ? "" : "[StackTraceHidden]")})");
                }
            }

            if (annotated.Count > 0)
            {
                Assert.Fail("The following types are annotated:\n" + string.Join("\n", annotated));
            }
        }
#else
        public static void AllTypesWithExecutableCodeAreAnnotatedWithNonUserCode(Assembly assembly)
        {
            var notAnnotated = new List<string>();

            foreach (var type in GetAllTypes(assembly))
            {
                // Only check classes and structs (not interfaces, enums, delegates, etc.), excluding custom attributes.
                if (!(type.IsClass || (type.IsValueType && !type.IsEnum))
                    || typeof(Attribute).IsAssignableFrom(type))
                    continue;

                // Skip compiler-generated types (e.g. display classes, state machines, types with compiler-generated naming patterns)
                if (type.FullName.Contains("<>")
                    || type.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false).Any())
                    continue;

                // Only check types that contain executable code: at least one method, property, or constructor (excluding compiler-generated and implicit struct constructors)
                bool hasUserDefinedConstructor =
                    type.IsClass
                        // Classes can have default constructors, which we would like to filter out of this test, but is impossible to check via reflection.
                        ? type.GetConstructors(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                            .Any(ctor => !ctor.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false))
                        // Structs can have user-define parameterless constructors in C#10/.Net 6, but we target older netstandard2.0 that doesn't support it, so this check is safe.
                        : type.GetConstructors(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                            .Any(ctor => !ctor.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false) && ctor.GetParameters().Length > 0);

                bool hasExecutableCode =
                    hasUserDefinedConstructor
                    || type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                        .Any(m => !m.IsSpecialName && !m.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false))
                    || type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                        .Any(p => !p.IsSpecialName && !p.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false));

                if (!hasExecutableCode)
                    continue;

                bool hasDebuggerNonUserCode = type.GetCustomAttributes(typeof(DebuggerNonUserCodeAttribute), false).Any();
                bool isException = typeof(Exception).IsAssignableFrom(type);
                bool hasStackTraceHidden = type.GetCustomAttributes(typeof(StackTraceHiddenAttribute), false).Any();

                if (!hasDebuggerNonUserCode || (!isException && !hasStackTraceHidden))
                {
                    notAnnotated.Add($"{type.FullName} (Missing: " +
                        $"{(hasDebuggerNonUserCode ? "" : "[DebuggerNonUserCode] ")}" +
                        $"{(isException || hasStackTraceHidden ? "" : "[StackTraceHidden]")})");
                }
            }

            if (notAnnotated.Count > 0)
            {
                Assert.Fail("The following types with executable code are missing required annotations:\n" + string.Join("\n", notAnnotated));
            }
        }
#endif

        private static IEnumerable<Type> GetAllTypes(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                yield return type;
                foreach (var nested in GetNestedTypesRecursive(type))
                    yield return nested;
            }
        }

        private static IEnumerable<Type> GetNestedTypesRecursive(Type type)
        {
            foreach (var nested in type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                yield return nested;
                foreach (var deeper in GetNestedTypesRecursive(nested))
                    yield return deeper;
            }
        }
    }
}