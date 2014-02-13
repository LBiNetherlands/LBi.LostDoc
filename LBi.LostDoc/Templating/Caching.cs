/*
 * Copyright 2014 DigitasLBi Netherlands B.V.
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
using System.Globalization;
using System.IO;
using System.Runtime.Caching;
using System.Xml.Linq;
using System.Xml.XPath;
using LBi.LostDoc.Templating.IO;

namespace LBi.LostDoc.Templating
{
    public static class Caching
    {
        public static readonly string DocumentPrefx = "XDC|";

        public static readonly string XPathDocumentPrefix = "XPD|";

        public static string GetDocumentKey(Uri source, int order)
        {
            return DocumentPrefx + source.OriginalString + '|' + order.ToString(CultureInfo.InvariantCulture);
        }

        private static string GetXPathDocumentKey(Uri source, int order)
        {
            return XPathDocumentPrefix + source.OriginalString + '|' + order.ToString(CultureInfo.InvariantCulture);
        }

        public static void AddDocument(this ObjectCache cache, Uri docUri, int order, XDocument document)
        {
            string inputCacheKey = GetDocumentKey(docUri, order);
            cache.Add(inputCacheKey, document, new CacheItemPolicy());
        }
        public static XDocument GetDocument(this ObjectCache cache, Uri docUri, int order)
        {
            string documentKey = GetDocumentKey(docUri, order);
            return cache.Get(documentKey) as XDocument;
        }

        public static XPathDocument GetXPathDocument(this ObjectCache cache, Uri docUri, int order)
        {
            string documentKey = GetXPathDocumentKey(docUri, order);
            return cache.Get(documentKey) as XPathDocument;
        }

        public static void AddXPathDocument(this ObjectCache cache, Uri docUri, int order, XPathDocument document)
        {
            string inputCacheKey = GetXPathDocumentKey(docUri, order);
            cache.Add(inputCacheKey, document, new CacheItemPolicy());
        }


    }
}