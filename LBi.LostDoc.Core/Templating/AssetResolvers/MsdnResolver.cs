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
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using LBi.LostDoc.Core.MsdnContentService;

namespace LBi.LostDoc.Core.Templating.AssetResolvers
{
    public class MsdnResolver : IAssetUriResolver
    {
        private const string UrlFormat = "http://msdn2.microsoft.com/{0}/library/{1}";

        private readonly Dictionary<string, string> _cachedMsdnUrls = new Dictionary<string, string>();
        private string _locale = "en-us";

        public string Locale
        {
            get { return this._locale; }
            set { this._locale = value; }
        }

        #region IAssetUriResolver Members

        public Uri ResolveAssetId(string assetId, Version version)
        {
            if (this._cachedMsdnUrls.ContainsKey(assetId) && this._cachedMsdnUrls[assetId] != null)
                return new Uri(string.Format(UrlFormat, this._locale, this._cachedMsdnUrls[assetId]));

            getContentRequest msdnRequest = new getContentRequest();
            msdnRequest.contentIdentifier = "AssetId:" + assetId;
            msdnRequest.locale = this._locale;

            string endpoint = null;

            Binding msdnBinding = new BasicHttpBinding(BasicHttpSecurityMode.None);
            EndpointAddress msdnEndpoint =
                new EndpointAddress("http://services.msdn.microsoft.com/ContentServices/ContentService.asmx");
            ContentServicePortTypeClient client =
                new ContentServicePortTypeClient(msdnBinding, msdnEndpoint);
            try
            {
                getContentResponse msdnResponse =
                    client.GetContent(new appId {value = "LostDoc"}, msdnRequest);
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

                if (client is IDisposable)
                    ((IDisposable)client).Dispose();
            }

            this._cachedMsdnUrls[assetId] = endpoint;

            if (string.IsNullOrEmpty(endpoint))
            {
                return null;
            }
            else
            {
                return new Uri(string.Format(UrlFormat, this._locale, endpoint));
            }
        }

        #endregion
    }
}
