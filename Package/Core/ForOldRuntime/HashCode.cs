// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*

The xxHash32 implementation is based on the code published by Yann Collet:
https://raw.githubusercontent.com/Cyan4973/xxHash/5c174cfa4e45a42f94082dc0d4539b39696afea1/xxhash.c

  xxHash - Fast Hash algorithm
  Copyright (C) 2012-2016, Yann Collet

  BSD 2-Clause License (http://www.opensource.org/licenses/bsd-license.php)

  Redistribution and use in source and binary forms, with or without
  modification, are permitted provided that the following conditions are
  met:

  * Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.
  * Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following disclaimer
  in the documentation and/or other materials provided with the
  distribution.

  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
  OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
  SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
  LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
  DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
  THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
  (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
  OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

  You can contact the author at :
  - xxHash homepage: http://www.xxhash.com
  - xxHash source repository : https://github.com/Cyan4973/xxHash

*/

#if UNITY_2018_3_OR_NEWER && !UNITY_2021_2_OR_NEWER

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

#pragma warning disable CA1066 // Implement IEquatable when overriding Object.Equals

namespace System
{
    // xxHash32 is used for the hash code.
    // https://github.com/Cyan4973/xxHash

    internal struct HashCode
    {
        private int hashCode;

        public void Add<T>(T value)
        {
            hashCode = Hash(hashCode, value);
        }

        public void Add<T>(T value, IEqualityComparer<T> comparer)
        {
            hashCode = Hash(hashCode, value, comparer);
        }

        public int ToHashCode() => hashCode;

        public static int Combine<T1>(T1 value1)
        {
            int hashCode = 0;
            hashCode = Hash(hashCode, value1);
            return hashCode;
        }

        public static int Combine<T1, T2>(T1 value1, T2 value2)
        {
            int hashCode = 0;
            hashCode = Hash(hashCode, value1);
            hashCode = Hash(hashCode, value2);
            return hashCode;
        }

        public static int Combine<T1, T2, T3>(T1 value1, T2 value2, T3 value3)
        {
            int hashCode = 0;
            hashCode = Hash(hashCode, value1);
            hashCode = Hash(hashCode, value2);
            hashCode = Hash(hashCode, value3);
            return hashCode;
        }

        public static int Combine<T1, T2, T3, T4>(T1 value1, T2 value2, T3 value3, T4 value4)
        {
            int hashCode = 0;
            hashCode = Hash(hashCode, value1);
            hashCode = Hash(hashCode, value2);
            hashCode = Hash(hashCode, value3);
            hashCode = Hash(hashCode, value4);
            return hashCode;
        }

        public static int Combine<T1, T2, T3, T4, T5>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
        {
            int hashCode = 0;
            hashCode = Hash(hashCode, value1);
            hashCode = Hash(hashCode, value2);
            hashCode = Hash(hashCode, value3);
            hashCode = Hash(hashCode, value4);
            hashCode = Hash(hashCode, value5);
            return hashCode;
        }

        public static int Combine<T1, T2, T3, T4, T5, T6>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
        {
            int hashCode = 0;
            hashCode = Hash(hashCode, value1);
            hashCode = Hash(hashCode, value2);
            hashCode = Hash(hashCode, value3);
            hashCode = Hash(hashCode, value4);
            hashCode = Hash(hashCode, value5);
            hashCode = Hash(hashCode, value6);
            return hashCode;
        }

        public static int Combine<T1, T2, T3, T4, T5, T6, T7>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)
        {
            int hashCode = 0;
            hashCode = Hash(hashCode, value1);
            hashCode = Hash(hashCode, value2);
            hashCode = Hash(hashCode, value3);
            hashCode = Hash(hashCode, value4);
            hashCode = Hash(hashCode, value5);
            hashCode = Hash(hashCode, value6);
            hashCode = Hash(hashCode, value7);
            return hashCode;
        }

        public static int Combine<T1, T2, T3, T4, T5, T6, T7, T8>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)
        {
            int hashCode = 0;
            hashCode = Hash(hashCode, value1);
            hashCode = Hash(hashCode, value2);
            hashCode = Hash(hashCode, value3);
            hashCode = Hash(hashCode, value4);
            hashCode = Hash(hashCode, value5);
            hashCode = Hash(hashCode, value6);
            hashCode = Hash(hashCode, value7);
            hashCode = Hash(hashCode, value8);
            return hashCode;
        }

        public static int Combine<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7,
            T8 value8, T9 value9, T10 value10)
        {
            int hashCode = 0;
            hashCode = Hash(hashCode, value1);
            hashCode = Hash(hashCode, value2);
            hashCode = Hash(hashCode, value3);
            hashCode = Hash(hashCode, value4);
            hashCode = Hash(hashCode, value5);
            hashCode = Hash(hashCode, value6);
            hashCode = Hash(hashCode, value7);
            hashCode = Hash(hashCode, value8);
            hashCode = Hash(hashCode, value9);
            hashCode = Hash(hashCode, value10);
            return hashCode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Hash<T>(int hashCode, T value)
        {
            unchecked
            {
                return (hashCode * 397) ^ (value?.GetHashCode() ?? 0);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int Hash<T>(int hashCode, T value, IEqualityComparer<T> comparer)
        {
            unchecked
            {
                return (hashCode * 397) ^ (value == null ? 0 : (comparer?.GetHashCode(value) ?? value.GetHashCode()));
            }
        }

#pragma warning disable CS0809 // Obsolete member 'HashCode.GetHashCode()' overrides non-obsolete member 'object.GetHashCode()'
        [Obsolete("HashCode is a mutable struct and should not be compared with other HashCodes. Use ToHashCode to retrieve the computed hash code.",
            error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new NotSupportedException();

        [Obsolete("HashCode is a mutable struct and should not be compared with other HashCodes.", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new NotSupportedException();
#pragma warning restore CS0809 // Obsolete member 'HashCode.GetHashCode()' overrides non-obsolete member 'object.GetHashCode()'
    }
}

#endif // UNITY_2018_3_OR_NEWER && !UNITY_2021_2_OR_NEWER