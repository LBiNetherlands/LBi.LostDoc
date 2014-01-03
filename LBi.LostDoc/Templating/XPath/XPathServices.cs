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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace LBi.LostDoc.Templating.XPath
{
    internal static class XPathServices
    {
        internal static string ResultToString(object res)
        {
            string ret = res as string;
            if (ret == null)
            {
                if (res is IEnumerable)
                {
                    object first = ((IEnumerable)res).Cast<object>().FirstOrDefault();
                    ret = first as string;
                    if (ret == null)
                    {
                        if (first is XAttribute)
                            ret = ((XAttribute)first).Value;
                        else if (first is XCData)
                            ret = ((XCData)first).Value;
                        else if (first is XText)
                            ret = ((XText)first).Value;
                        else if (first is XElement)
                            ret = ((XElement)first).Value;
                        else if (first is XPathNavigator)
                        {
                            XPathNavigator navigator = (XPathNavigator)first;
                            ret = navigator.Value;
                        }
                    }
                }
            }

            return ret;
        }

        /*
         * a number is true if and only if it is neither positive or negative zero nor NaN
         * a node-set is true if and only if it is non-empty
         * a string is true if and only if its length is non-zero
         * an object of a type other than the four basic types is converted to a boolean in a way that is dependent on that type
         */

        internal static bool ResultToBool(object res)
        {
            bool ret;
            if (res == null)
                ret = false;
            else if (res is string)
                ret = ((string)res).Length > 0;
            else if (res is bool)
                ret = (bool)res;
            else if (res is double)
            {
                double d = (double)res;
                ret = (d > 0.0 || d < 0.0) && !Double.IsNaN(d);
            }
            else if (res is IEnumerable)
            {
                object first = ((IEnumerable)res).Cast<object>().FirstOrDefault();
                ret = ResultToBool(first);
            }
            else
                ret = false;


            return ret;
        }

        internal static IEnumerable<XNode> ToNodeSequence(object result)
        {
            IEnumerable<object> enumerable = result as IEnumerable<object>;
            if (enumerable != null)
            {
                foreach (XNode node in enumerable)
                {
                    yield return node;
                }
            }
        }
    }
}