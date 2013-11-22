/*
 * Copyright 2012 DigitasLBi Netherlands B.V.
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace LBi.LostDoc
{
    /// <summary>
    /// This type contains a version map, from a version that was
    /// </summary>
    public class AssetRedirectCollection : IEnumerable<KeyValuePair<AssetIdentifier, AssetIdentifier>>
    {
        private readonly Dictionary<AssetIdentifier, AssetIdentifier> _mappings;

        /// <summary>
        ///   Initializes a new instance of the <see cref="AssetRedirectCollection" /> class. Creates and empty assembly redirect collection.
        /// </summary>
        public AssetRedirectCollection()
        {
            this._mappings = new Dictionary<AssetIdentifier, AssetIdentifier>();
        }

        #region IEnumerable<KeyValuePair<AssetIdentifier,AssetIdentifier>> Members

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection. 
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<KeyValuePair<AssetIdentifier, AssetIdentifier>> GetEnumerator()
        {
            return this._mappings.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection. 
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Adds an assembly redirect.
        /// </summary>
        /// <param name="mapFrom">
        /// Source assembly name 
        /// </param>
        /// <param name="mapTo">
        /// Target assembly name 
        /// </param>
        public void Add(AssetIdentifier mapFrom, AssetIdentifier mapTo)
        {
            this._mappings.Add(mapFrom, mapTo);
        }

        /// <summary>
        /// This removes a single mapping given the starting <see cref="AssemblyName"/> .
        /// </summary>
        /// <param name="mapFrom">
        /// Source assembly name 
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when
        ///   <paramref name="mapFrom"/>
        ///   doesn't exist in collection.
        /// </exception>
        public void Remove(AssetIdentifier mapFrom)
        {
            this._mappings.Remove(mapFrom);
        }

        /// <summary>
        /// This clears the collection.
        /// </summary>
        public void Clear()
        {
            this._mappings.Clear();
        }

        public bool TryGet(AssetIdentifier mapFrom, out AssetIdentifier mapTo)
        {
            return this._mappings.TryGetValue(mapFrom, out mapTo);
        }
    }
}
