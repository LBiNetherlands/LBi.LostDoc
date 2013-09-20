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
using System.IO;
using System.Net;
using System.Xml;

namespace LBi.LostDoc.Templating
{
    public class XmlFileProviderResolver : XmlResolver
    {
        private readonly string _basePath;
        private readonly IFileProvider _fileProvider;

        // TODO investigate whether basePath can be deleted entirely
        public XmlFileProviderResolver(IFileProvider fileProvider, string basePath = null)
        {
            this._fileProvider = fileProvider;
            this._basePath = basePath;
        }

        /// <summary>
        ///   When overridden in a derived class, sets the credentials used to authenticate Web requests.
        /// </summary>
        /// <returns> An <see cref="T:System.Net.ICredentials" /> object. If this property is not set, the value defaults to null; that is, the XmlResolver has no user credentials. </returns>
        public override ICredentials Credentials
        {
            set { }
        }

        /// <summary>
        /// When overridden in a derived class, maps a URI to an object containing the actual resource.
        /// </summary>
        /// <returns>
        /// A System.IO.Stream object or null if a type other than stream is specified. 
        /// </returns>
        /// <param name="absoluteUri">
        /// The URI returned from <see cref="M:System.Xml.XmlResolver.ResolveUri(System.Uri,System.String)"/> . 
        /// </param>
        /// <param name="role">
        /// The current version does not use this parameter when resolving URIs. This is provided for future extensibility purposes. For example, this can be mapped to the xlink:role and used as an implementation specific argument in other scenarios. 
        /// </param>
        /// <param name="ofObjectToReturn">
        /// The type of object to return. The current version only returns System.IO.Stream objects. 
        /// </param>
        /// <exception cref="T:System.Xml.XmlException">
        /// <paramref name="ofObjectToReturn"/>
        ///   is not a Stream type.
        /// </exception>
        /// <exception cref="T:System.UriFormatException">
        /// The specified URI is not an absolute URI.
        /// </exception>
        /// <exception cref="T:System.ArgumentNullException">
        /// <paramref name="absoluteUri"/>
        ///   is null.
        /// </exception>
        /// <exception cref="T:System.Exception">
        /// There is a runtime error (for example, an interrupted server connection).
        /// </exception>
        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            string uri = absoluteUri.ToString();

            if (this._fileProvider.FileExists(uri))
                return this._fileProvider.OpenFile(uri, FileMode.Open);

            throw new FileNotFoundException("File not found: " + uri, uri);
        }

        public override Uri ResolveUri(Uri baseUri, string relativeUri)
        {
            if (this._basePath != null)
                return new Uri(this._basePath + "/" + relativeUri, UriKind.Relative);

            return new Uri(relativeUri, UriKind.RelativeOrAbsolute);
        }
    }
}
