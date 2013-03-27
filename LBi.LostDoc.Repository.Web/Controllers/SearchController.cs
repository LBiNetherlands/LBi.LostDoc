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
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Web.Http;
using LBi.LostDoc.Repository.Web.Models;

namespace LBi.LostDoc.Repository.Web.Controllers
{
    public class SearchController : ApiController
    {
        protected static ObjectCache _Cache;

        static SearchController()
        {
            _Cache = new MemoryCache("SearchController.ContentSearchers");
        }

        public ResultSet Get(string id, string searchTerms)
        {
            ContentSearcher search = null;
            try
            {
                if (id == "current")
                    search = this.GetSearcher(App.Instance.Content.ContentFolder);
                else
                    search = this.GetSearcher(id);

                var res = search.Search(searchTerms);

                return new ResultSet
                           {
                               HitCount = res.HitCount, 
                               Results = res.Results.Select(r => new Result
                                                                     {
                                                                         AssetId = r.AssetId.ToString(), 
                                                                         Title = r.Title, 
                                                                         Url = r.Url, 
                                                                         Blurb = r.Blurb
                                                                     }).ToArray()
                           };
            }
            finally
            {
                if (search != null)
                    this.ReleaseSeracher(search);
            }
        }

        private ContentSearcher GetSearcher(string contentFolder)
        {
            var lazyObj =
                new Lazy<ContentSearcher>(
                    () => new ContentSearcher(Path.Combine(AppConfig.ContentPath, contentFolder, "Index")).Retain(), 
                    LazyThreadSafetyMode.ExecutionAndPublication);

            object obj = _Cache.AddOrGetExisting(contentFolder, 
                                                 lazyObj, 
                                                 new CacheItemPolicy
                                                     {
                                                         RemovedCallback =
                                                             arguments =>
                                                             ((Lazy<ContentSearcher>)arguments.CacheItem.Value).Value
                                                                                                               .Release(), 
                                                         SlidingExpiration = TimeSpan.FromMinutes(5)
                                                     });

            if (obj != null)
                lazyObj = (Lazy<ContentSearcher>)obj;

            return lazyObj.Value.Retain();
        }

        private void ReleaseSeracher(ContentSearcher searcher)
        {
            searcher.Release();
        }
    }
}