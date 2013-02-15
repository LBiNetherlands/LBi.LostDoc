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
using System.Collections.Concurrent;
using System.ServiceModel;
using System.ServiceModel.Channels;
using LBi.LostDoc.MsdnContentService;

namespace LBi.LostDoc.Templating.AssetResolvers
{
    public class MsdnResolver : IAssetUriResolver
    {
        private const string UrlFormat = "http://msdn2.microsoft.com/{0}/library/{1}";

        private readonly ConcurrentDictionary<string, string> _cachedMsdnUrls = new ConcurrentDictionary<string, string>();
        private string _locale = "en-us";
        private BasicHttpBinding _msdnBinding;
        private EndpointAddress _msdnEndpoint;

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
            string ret;
            if (this._cachedMsdnUrls.TryGetValue(assetId, out ret))
            {
                if (ret != null)
                    return new Uri(string.Format(UrlFormat, this._locale, ret));

                return null;
            }

            // filter out non-MS namespaces
            string name = assetId.Substring(2);
            if (!name.StartsWith("System", StringComparison.OrdinalIgnoreCase) &&
                !name.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) &&
                !name.StartsWith("Accessibility", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            getContentRequest msdnRequest = new getContentRequest();
            msdnRequest.contentIdentifier = "AssetId:" + assetId;
            msdnRequest.locale = this._locale;

            string endpoint = null;

            ContentServicePortTypeClient client = new ContentServicePortTypeClient(_msdnBinding, _msdnEndpoint);
            try
            {
                getContentResponse msdnResponse = client.GetContent(new appId {value = "LostDoc"}, msdnRequest);
                endpoint = msdnResponse.contentId;
            }
            catch (FaultException<mtpsFaultDetailType>)
            {
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

            this._cachedMsdnUrls.TryAdd(assetId, endpoint);

            if (string.IsNullOrEmpty(endpoint))
                return null;
            
            return new Uri(string.Format(UrlFormat, this._locale, endpoint));
        }

        #endregion
    }
}
