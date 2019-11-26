﻿// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

using Newtonsoft.Json;

namespace Rock.Web.Cache
{
    /// <summary>
    /// Generic Item Cache
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    [DataContract]
    public abstract class ItemCache<T> : IItemCache
        where T : IItemCache
    {
        private static readonly string KeyPrefix = $"{typeof( T ).Name}";

        internal static string AllItemsKey => $"{typeof( T ).Name}:AllItems";

        internal static string KeysCacheKey => $"{typeof( T ).Name}:KeysCache";

        #region Protected Methods

        /// <summary>
        /// Returns the key prefixed with the type of object being cached.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        internal protected static string QualifiedKey( string key )
        {
            return $"{KeyPrefix}:{key}";
        }

        /// <summary>
        /// Gets if T is <see cref="IUseCacheRegion"></see>, returns the Region for the Cache to store this in
        /// </summary>
        /// <returns></returns>
        internal protected static string RegionKey => $"Region:{KeyPrefix}";

        /// <summary>
        /// Gets an item from cache, and if not found, executes the itemFactory to create item and add to cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="itemFactory">The item factory.</param>
        /// <returns></returns>
        internal protected static T GetOrAddExisting( int key, Func<T> itemFactory )
        {
            return GetOrAddExisting( key.ToString(), itemFactory );
        }

        /// <summary>
        /// Gets an item from cache, and if not found, executes the itemFactory to create item and add to cache with an expiration timespan.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="itemFactory">The item factory.</param>
        /// <param name="expiration">The expiration.</param>
        /// <returns></returns>
        internal protected static T GetOrAddExisting( int key, Func<T> itemFactory, TimeSpan expiration )
        {
            return GetOrAddExisting( key.ToString(), itemFactory, expiration );
        }

        /// <summary>
        /// Gets an item from cache, and if not found, executes the itemFactory to create item and add to cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="itemFactory">The item factory.</param>
        /// <returns></returns>
        internal protected static T GetOrAddExisting( string key, Func<T> itemFactory )
        {
            return GetOrAddExisting( key, itemFactory, TimeSpan.MaxValue );
        }

        /// <summary>
        /// Gets an item from cache, and if not found, executes the itemFactory to create item and add to cache with an expiration timespan.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="itemFactory">The item factory.</param>
        /// <param name="expiration">The expiration.</param>
        /// <returns></returns>
        internal protected static T GetOrAddExisting( string key, Func<T> itemFactory, TimeSpan expiration )
        {
            string qualifiedKey = QualifiedKey( key );

            T value = RockCacheManager<T>.Instance.Cache.Get( qualifiedKey, RegionKey );

            if ( value != null )
            {
                return value;
            }

            if ( itemFactory == null )
                return default( T );

            value = itemFactory();
            if ( value != null )
            {
                UpdateCacheItem( key, value, expiration );
            }

            return value;
        }

        /// <summary>
        /// Updates the cache item.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="expiration">The expiration.</param>
        internal protected static void UpdateCacheItem( string key, T item, TimeSpan expiration )
        {
            string qualifiedKey = QualifiedKey( key );

            // Add the item to cache
            RockCacheManager<T>.Instance.AddOrUpdate( qualifiedKey, RegionKey, item, expiration );

            // Do any postcache processing that this item cache type may need to do
            item.PostCached();

            AddToAllIds( key );
        }

        /// <summary>
        /// Ensure that the Key is part of the AllIds list (if All() has been called)
        /// </summary>
        /// <param name="key">The key.</param>
        private static void AddToAllIds( string key )
        {
            AllKeysCache.AddKey( AllItemsKey, key );
        }

        /// <summary>
        /// If not already called, recreates the list of keys for every entity using the keyFactory. (Expensive)
        /// </summary>
        /// <returns></returns>
        /// <param name="keyFactory">All keys factory.</param>
        internal protected static List<string> GetOrAddKeys( Func<List<string>> keyFactory )
        {
            return AllKeysCache.GetAllKeysOrAddExisting( AllItemsKey, keyFactory );
        }

        /// <summary>
        /// Recreates the list of keys for every entity using the keyFactory. (Expensive)
        /// </summary>
        /// <param name="keyFactory">All keys factory.</param>
        internal protected static List<string> AddKeys( Func<List<string>> keyFactory )
        {
            return AllKeysCache.AddAllKeys( AllItemsKey, keyFactory );
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Converts item to json string
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject( this, Formatting.None,
                new JsonSerializerSettings()
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                } );
        }

        /// <summary>
        /// Creates a cache object from Json string
        /// </summary>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        public static T FromJson( string json )
        {
            return JsonConvert.DeserializeObject<T>( json );
        }

        /// <summary>
        /// Removes the specified key from cache and from AllIds. Call this if Deleting the object from the database.
        /// </summary>
        /// <param name="key">The key.</param>
        public static void Remove( int key )
        {
            Remove( key.ToString() );
        }

        /// <summary>
        /// Flushes the object from the cache without removing it from AllIds.
        /// Call this to force the cache to reload the object from the database the next time it is requested. 
        /// </summary>
        /// <param name="key">The key.</param>
        internal static void FlushItem( int key )
        {
            FlushItem( key.ToString() );
        }

        /// <summary>
        /// Ensure that the Key is part of the AllIds list (if All() has been called)
        /// </summary>
        /// <param name="key">The key.</param>
        internal static void AddToAllIds( int key )
        {
            AddToAllIds( key.ToString() );
        }

        /// <summary>
        /// Flushes the object from the cache without removing it from AllIds.
        /// Call this to force the cache to reload the object from the database the next time it is requested. 
        /// </summary>
        /// <param name="key">The key.</param>
        internal static void FlushItem( string key )
        {
            var qualifiedKey = QualifiedKey( key );

            RockCacheManager<T>.Instance.Cache.Remove( qualifiedKey, RegionKey );
        }

        /// <summary>
        /// Removes the specified key from cache and from AllIds. Call this if Deleting the object from the database.
        /// </summary>
        /// <param name="key">The key.</param>
        public static void Remove( string key )
        {
            FlushItem( key );

            AllKeysCache.RemoveKey( AllItemsKey, key );
        }

        /// <summary>
        /// Removes all items of this type from cache.
        /// </summary>
        [Obsolete( "This can cause performance issues. Use ClearCachedItems instead" )]
        public static void Clear()
        {
            RockCacheManager<T>.Instance.Cache.ClearRegion( RegionKey );
            AllKeysCache.FlushItem( AllItemsKey );
        }

        public static void ClearCachedItems()
        {
            RockCacheManager<T>.Instance.Cache.ClearRegion( RegionKey );
            AllKeysCache.FlushItem( AllItemsKey );
        }

        /// <summary>
        /// Method that is called by the framework immediately after being added to cache
        /// </summary>
        public virtual void PostCached()
        {
        }

        #endregion
    }
}
