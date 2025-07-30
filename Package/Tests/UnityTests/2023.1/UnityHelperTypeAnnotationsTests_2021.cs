#if UNITY_2023_1_OR_NEWER

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
using ProtoPromise.Tests.Annotations;

namespace ProtoPromise.Tests.Unity.Annotations
{
    public class UnityHelperTypeAnnotationsTests_2021
    {
#if PROTO_PROMISE_DEVELOPER_MODE
        [Test]
        public void NoUnityHelperTypesAreAnnotatedWithNonUserCode()
        {
            TypeAnnotationsTestHelper.NoTypesAreAnnotatedWithNonUserCode(typeof(AwaitableExtensions).Assembly);
        }
#else
        [Test]
        public void AllUnityHelperTypesWithExecutableCodeAreAnnotatedWithNonUserCode()
        {
            TypeAnnotationsTestHelper.AllTypesWithExecutableCodeAreAnnotatedWithNonUserCode(typeof(AwaitableExtensions).Assembly);
        }
#endif
    }
}

#endif // UNITY_2023_1_OR_NEWER