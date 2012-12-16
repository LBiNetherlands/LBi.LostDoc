/*
 * Copyright 2012 LBi Netherlands B.V.
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
using System.Runtime.CompilerServices;

namespace LBi.LostDoc.Repository.Web
{
    public static class DisposableExtensions
    {
        /// <summary>
        /// Values in a ConditionalWeakTable need to be a reference type,
        /// so box the refcount int in a class.
        /// </summary>
        private class RefCount
        {
            public int refCount;
        }

        private static ConditionalWeakTable<IDisposable, RefCount> refCounts =
            new ConditionalWeakTable<IDisposable, RefCount>();

        /// <summary>
        /// Extension method for IDisposable.
        /// Increments the refCount for the given IDisposable object.
        /// Note: newly instantiated objects don't automatically have a refCount of 1!
        /// If you wish to use ref-counting, always call retain() whenever you want
        /// to take ownership of an object.
        /// </summary>
        /// <remarks>This method is thread-safe.</remarks>
        /// <param name="disposable">The disposable that should be retained.</param>
        public static T Retain<T>(this T disposable) where  T : IDisposable
        {
            lock (refCounts)
            {
                RefCount refCount = refCounts.GetOrCreateValue(disposable);
                refCount.refCount++;
            }
            return disposable;
        }

        /// <summary>
        /// Extension method for IDisposable.
        /// Decrements the refCount for the given disposable.
        /// </summary>
        /// <remarks>This method is thread-safe.</remarks>
        /// <param name="disposable">The disposable to release.</param>
        public static void Release(this IDisposable disposable)
        {
            lock (refCounts)
            {
                RefCount refCount = refCounts.GetOrCreateValue(disposable);
                if (refCount.refCount > 0)
                {
                    refCount.refCount--;
                    if (refCount.refCount == 0)
                    {
                        refCounts.Remove(disposable);
                        disposable.Dispose();
                    }
                }
                else
                {
                    // Retain() was never called, so assume there is only
                    // one reference, which is now calling Release()
                    disposable.Dispose();
                }
            }
        }
    }
}
