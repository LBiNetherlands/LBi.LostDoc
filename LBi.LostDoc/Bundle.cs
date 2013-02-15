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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using LBi.LostDoc.Templating;
using LBi.LostDoc.Diagnostics;

namespace LBi.LostDoc
{
    public class Bundle
    {
        private XDocument _bundle;
        private VersionComponent? _ignoreVersionComponent;

        public Bundle(VersionComponent? ignoreVersionComponent)
        {
            this._bundle = new XDocument(new XElement("bundle"));
            this._ignoreVersionComponent = ignoreVersionComponent;
        }

        public void Add(XDocument ldoc)
        {
            // copy attributes
            foreach (XAttribute xAttr in ldoc.Root.Attributes())
            {
                if (ldoc.Root.Attribute(xAttr.Name) == null)
                    this._bundle.Root.Add(xAttr);
            }

            MergeElements(ldoc.Root, this._bundle.Root);
        }

        public XDocument Merge(out AssetRedirectCollection assetRedirects)
        {
            // merge assemblies 
            Func<AssetIdentifier, string> keySelector;
            assetRedirects = new AssetRedirectCollection();
            if (this._ignoreVersionComponent.HasValue)
            {
                TraceSources.BundleSource.TraceInformation("Merging assembly sections with version mask: {0}",
                                                             this._ignoreVersionComponent.Value.ToString());

                keySelector =
                    aId =>
                    string.Format("{0}, {1}",
                                  aId.AssetId,
                                  aId.Version.ToString((int)this._ignoreVersionComponent.Value));


                IEnumerable<XElement> allAssemblies = this._bundle.Root.XPathSelectElements("assembly[@phase = '0']");

                foreach (XElement asm in allAssemblies)
                {
                    Debug.WriteLine(keySelector(AssetIdentifier.Parse(asm.Attribute("assetId").Value)));
                }

                IEnumerable<IOrderedEnumerable<XElement>> groupedAssemblies =
                    allAssemblies.GroupBy(xe => keySelector(AssetIdentifier.Parse(xe.Attribute("assetId").Value)),
                                          (key, grp) =>
                                          grp.OrderByDescending(
                                                                xe =>
                                                                AssetIdentifier.Parse(xe.Attribute("assetId").Value).
                                                                    Version));

                foreach (IOrderedEnumerable<XElement> assemblyGroup in groupedAssemblies)
                {
                    XElement primary = assemblyGroup.First();
                    IEnumerable<XElement> rest = assemblyGroup.Skip(1);

                    IEnumerable<XAttribute> assetAttrs =
                        ((IEnumerable)primary.XPathEvaluate(".//@assetId")).Cast<XAttribute>();
                    Dictionary<string, AssetIdentifier> assets =
                        assetAttrs.Select(a => AssetIdentifier.Parse(a.Value)).ToDictionary(a => a.AssetId);


                    TraceSources.BundleSource.TraceInformation("Primary assembly: " +
                                                                 primary.Attribute("assetId").Value);

                    foreach (XElement secondary in rest)
                    {
                        TraceSources.BundleSource.TraceInformation("Shadowed assembly: " +
                                                                     secondary.Attribute("assetId").Value);
                        secondary.Remove();
                        IEnumerable<XAttribute> secondaryAssetAttrs =
                            ((IEnumerable)secondary.XPathEvaluate(".//@assetId")).Cast<XAttribute>();
                        IEnumerable<AssetIdentifier> secondaryAssets =
                            secondaryAssetAttrs.Select(a => AssetIdentifier.Parse(a.Value));

                        foreach (AssetIdentifier asset in secondaryAssets)
                        {
                            AssetIdentifier primaryAsset;
                            if (assets.TryGetValue(asset.AssetId, out primaryAsset))
                            {
                                assetRedirects.Add(asset, primaryAsset);
                            }
                            else
                            {
                                // warnings, we merged and lost an asset!
                                TraceSources.BundleSource.TraceWarning(
                                                                       "Failed to redirect asset {0}, no matching asset found in assembly {1}",
                                                                       asset.ToString(),
                                                                       primary.Attribute("assetId"));
                            }
                        }
                    }
                }
            }

            TraceSources.BundleSource.TraceInformation("Sorting assemblies by version.");
            XElement[] asmElements =
                this._bundle.Root.Elements().OrderByDescending(
                                                               e =>
                                                               AssetIdentifier.Parse(e.Attribute("assetId").Value).
                                                                   Version).ToArray();
            this._bundle.Root.RemoveNodes();
            this._bundle.Root.Add(asmElements);
            return this._bundle;
        }


        private static void MergeElements(XElement sourceNode, XElement targetNode)
        {
            IEnumerable<XElement> sourceElements = sourceNode.XPathSelectElements("*[@assetId]");

            foreach (XElement sourceElement in sourceElements)
            {
                string srcNsPrefix = sourceElement.GetPrefixOfNamespace(sourceElement.Name.Namespace);
                string srcLocalNamne = sourceElement.Name.LocalName;
                string srcId = sourceElement.Attribute("assetId").Value;
                string targetXpath;
                if (string.IsNullOrEmpty(srcNsPrefix))
                    targetXpath = string.Format("{0}[@assetId = '{1}']", srcLocalNamne, srcId);
                else
                    targetXpath = string.Format("{0}:{1}[@assetId = '{2}']", srcNsPrefix, srcLocalNamne, srcId);

                XElement targetElement = targetNode.XPathSelectElement(targetXpath);

                if (targetElement == null)
                    targetNode.Add(sourceElement);
                else
                {
                    if (targetElement.Attribute("phase") != null)
                    {
                        targetElement.SetAttributeValue("phase",
                                                        Math.Min(int.Parse(targetElement.Attribute("phase").Value),
                                                                 int.Parse(sourceElement.Attribute("phase").Value)));
                    }

                    MergeElements(sourceElement, targetElement);
                }
            }
        }
    }
}
