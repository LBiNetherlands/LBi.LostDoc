/*
 * Copyright 2012-2013 DigitasLBi Netherlands B.V.
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
using System.Collections.Concurrent;
using System.ServiceModel;
using LBi.LostDoc.MsdnContentService;

namespace LBi.LostDoc.Templating.AssetResolvers
{
    public class MsdnResolver : IAssetUriResolver
    {
        private const string UrlFormat = "http://msdn2.microsoft.com/{0}/library/{1}";

        private readonly ConcurrentDictionary<string, Uri> _cachedMsdnUrls = new ConcurrentDictionary<string, Uri>();
        private string _locale = "en-us";
        private readonly BasicHttpBinding _msdnBinding;
        private readonly EndpointAddress _msdnEndpoint;

        public MsdnResolver()
        {
            _msdnBinding = new BasicHttpBinding(BasicHttpSecurityMode.None);
            _msdnEndpoint = new EndpointAddress("http://services.msdn.microsoft.com/ContentServices/ContentService.asmx");
        }

        public string Locale
        {
            get { return this._locale; }
            set { this._locale = value; }
        }

        #region IAssetUriResolver Members

        public Uri ResolveAssetId(string assetId, Version version)
        {
            Uri ret;
            if (this._cachedMsdnUrls.TryGetValue(assetId, out ret))
                return ret;

            // filter out non-MS namespaces

            string name = assetId.Substring(assetId.IndexOf(':') + 1);
            if (!name.StartsWith("System", StringComparison.OrdinalIgnoreCase) &&
                !name.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) &&
                !name.StartsWith("Accessibility", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            getContentRequest msdnRequest = new getContentRequest();
            msdnRequest.contentIdentifier = "AssetId:" + assetId;
            msdnRequest.locale = this._locale;

            int retries = 3;
            do
            {
                ContentServicePortTypeClient client = new ContentServicePortTypeClient(_msdnBinding, _msdnEndpoint);
                try
                {
                    getContentResponse msdnResponse = client.GetContent(new appId { value = "LostDoc" }, msdnRequest);
                    if (msdnResponse.contentId != null)
                        ret = new Uri(string.Format(UrlFormat, this._locale, msdnResponse.contentId));

                    break;
                }
                catch (TimeoutException)
                {
                    // retry
                }
                catch (FaultException<mtpsFaultDetailType> fe)
                {
                    // this is a fallback because MSDN doesn't have links for enumeration fields
                    // so we assume that any unresolved field will be represented on it's parent type page
                    if (assetId.StartsWith("F:"))
                    {
                        // TODO add logging for this
                        string parentEnum = "T:" + assetId.Substring("F:".Length);
                        parentEnum = parentEnum.Substring(0, parentEnum.LastIndexOf('.'));
                        ret = this.ResolveAssetId(parentEnum, version);
                    }

                    break;
                }
                finally
                {
                    try
                    {
                        client.Close();
                    }
                    catch
                    {
                        client.Abort();
                    }

                    ((IDisposable)client).Dispose();
                }
            } while (--retries > 0);

            this._cachedMsdnUrls.TryAdd(assetId, ret);

            return ret;
        }

        #endregion
    }
}
