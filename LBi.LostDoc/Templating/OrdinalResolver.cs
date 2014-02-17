/*
 * Copyright 2014 DigitasLBi Netherlands B.V.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. 
 */

using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace LBi.LostDoc.Templating
{
    public class OrdinalResolver<T>
    {
        private int[] _redirects;
        private Lazy<T>[] _values; 

        private readonly Lazy<T> _fallback;

        public OrdinalResolver(Lazy<T> fallback)
        {
            this._fallback = fallback;
            this._redirects = new int[0];
            this._values = new Lazy<T>[0];
        }

        public void Add(int ordinal, Lazy<T> value)
        {
            Contract.Requires<ArgumentOutOfRangeException>(ordinal >= 0, "ordinal cannot be negative.");

            int lastOrdinal;
            if (this._redirects.Length == 0)
                lastOrdinal = -1;
            else
                lastOrdinal = this._redirects[this._redirects.Length - 1];

            if (ordinal <= lastOrdinal)
                throw new ArgumentOutOfRangeException("ordinal", "Ordinals can only be added in order.");

            Array.Resize(ref this._redirects, ordinal + 1);

            for (int i = lastOrdinal + 1; i < ordinal; i++)
                this._redirects[i] = lastOrdinal;

            this._redirects[ordinal] = ordinal;

            Array.Resize(ref this._values, ordinal + 1);
            this._values[ordinal] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ResolveOrdinal(int ordinal)
        {
            Contract.Requires<ArgumentOutOfRangeException>(ordinal >= 0);
            Contract.Ensures(Contract.Result<int>() >= -1 && Contract.Result<int>() < ordinal);

            if (ordinal == 0 || this._redirects.Length == 0)
                return -1;

            if (ordinal >= this._redirects.Length)
                return this._redirects[this._redirects.Length - 1];

            return this._redirects[ordinal - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsFinal(int ordinal)
        {
            Contract.Requires<ArgumentOutOfRangeException>(ordinal >= 0);

            if (this._redirects.Length == 0)
                return true;

            return this._redirects[this._redirects.Length - 1] <= ordinal;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Lazy<T> Resolve(int ordinal)
        {
            Contract.Requires<ArgumentOutOfRangeException>(ordinal >= 0);

            int realOrdinal = this.ResolveOrdinal(ordinal);
            if (realOrdinal == -1)
                return this._fallback;

            return this._values[realOrdinal];
        }
    }

    //public class OrdinalResolver
    //{
    //    private int[] _redirects;

    //    public OrdinalResolver()
    //    {
    //        this._redirects = new int[0];
    //    }

    //    public void Add(int ordinal)
    //    {
    //        Contract.Requires<ArgumentOutOfRangeException>(ordinal >= 0, "ordinal cannot be negative.");

    //        int lastOrdinal;
    //        if (this._redirects.Length == 0)
    //            lastOrdinal = -1;
    //        else
    //            lastOrdinal = this._redirects[this._redirects.Length - 1];

    //        if (ordinal <= lastOrdinal)
    //            throw new ArgumentOutOfRangeException("ordinal", "Ordinals can only be added in order.");

    //        Array.Resize(ref this._redirects, ordinal + 1);

    //        for (int i = lastOrdinal + 1; i < ordinal; i++)
    //            this._redirects[i] = lastOrdinal;

    //        this._redirects[ordinal] = ordinal;

    //    }

    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    public int Resolve(int ordinal)
    //    {
    //        Contract.Requires<ArgumentOutOfRangeException>(ordinal >= 0);
    //        if (ordinal >= this._redirects.Length)
    //            return this._redirects[this._redirects.Length - 1];

    //        return this._redirects[ordinal];
    //    }
    //}

}