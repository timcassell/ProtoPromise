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
    public class CoreTypeAnnotationsTests
    {
#if PROTO_PROMISE_DEVELOPER_MODE
        [Test]
        public void NoCoreTypesAreAnnotatedWithNonUserCode()
        {
            TypeAnnotationsTestHelper.NoTypesAreAnnotatedWithNonUserCode(typeof(Promise).Assembly);
        }
#else
        [Test]
        public void AllCoreTypesWithExecutableCodeAreAnnotatedWithNonUserCode()
        {
            TypeAnnotationsTestHelper.AllTypesWithExecutableCodeAreAnnotatedWithNonUserCode(typeof(Promise).Assembly);
        }
#endif
    }
}