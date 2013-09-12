/*
 * Copyright 2012-2013 LBi Netherlands B.V.
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
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Web.Http;
using LBi.LostDoc.Repository.Web.Configuration;
using LBi.LostDoc.Repository.Web.Models;

namespace LBi.LostDoc.Repository.Web.Host.Controllers
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class SearchController : ApiController
    {
        private static readonly ObjectCache _Cache;
        private readonly ContentManager _content;

        static SearchController()
        {
            _Cache = new MemoryCache("SearchController.ContentSearchers");
        }

        [ImportingConstructor]
        public SearchController(ContentManager contentManager)
        {
            this._content = contentManager;
        }

        public ResultSet Get(string id, string searchTerms, int offset = 0, int count = 200)
        {
            ContentSearcher search = null;
            Func<Uri, Uri> createUri;
            try
            {
                if (id == "current")
                {
                    search = this.GetSearcher(this._content.ContentFolder);
                    createUri = uri => new Uri(Url.Route(RouteConstants.LibraryRouteName, new { path = uri.ToString() }), UriKind.RelativeOrAbsolute);
                }
                else
                {
                    search = this.GetSearcher(id);
                    createUri = uri => new Uri(Url.Route(RouteConstants.ArchiveRouteName, new { id, path = uri.ToString() }), UriKind.RelativeOrAbsolute);
                }
                var res = search.Search(searchTerms, offset, count);

                return new ResultSet
                           {
                               HitCount = res.HitCount,
                               Results = res.Results.Select(r => new Result
                                                                     {
                                                                         AssetId = r.AssetId.ToString(),
                                                                         Title = r.Title,
                                                                         Url = createUri(r.Url),
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
            var indexPath = Path.Combine(AppConfig.ContentPath, contentFolder, "Index");
            var lazyObj = new Lazy<ContentSearcher>(() => new ContentSearcher(indexPath).Retain(),
                                                    LazyThreadSafetyMode.ExecutionAndPublication);

            object obj = _Cache.AddOrGetExisting(
                contentFolder,
                lazyObj,
                new CacheItemPolicy
                    {
                        RemovedCallback = args => ((Lazy<ContentSearcher>)args.CacheItem.Value).Value.Release(),
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