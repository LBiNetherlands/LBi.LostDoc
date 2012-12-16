using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace LBi.LostDoc.Core
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