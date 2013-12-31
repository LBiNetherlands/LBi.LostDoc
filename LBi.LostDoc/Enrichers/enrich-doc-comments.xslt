<?xml version="1.0" encoding="utf-8"?>
<!-- 
  
  Copyright 2012-2013 DigitasLBi Netherlands B.V.
  
  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at
  
      http://www.apache.org/licenses/LICENSE-2.0
  
  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License. 
  
-->

<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                exclude-result-prefixes="msxsl ld ext"
                xmlns:ld="urn:lostdoc:template"
                xmlns:ext="urn:lostdoc-core:ext">
  <xsl:output method="xml" indent="yes" />


  <xsl:template match="@* | node()">
    <xsl:copy>
      <xsl:apply-templates select="@* | node()" />
    </xsl:copy>
  </xsl:template>

  <xsl:template match="@cref" priority="100">
    <xsl:attribute name="cref">
      <xsl:value-of select="ld:getVersionedId(.)" />
    </xsl:attribute>
  </xsl:template>

  <msxsl:script implements-prefix="ext"
             xmlns:ms="urn:schemas-microsoft-com:xslt"
             language="C#">
    <msxsl:assembly name="System.Core" />
    <msxsl:using namespace="System.Linq"/>

    public string trimStart(XPathNavigator node, bool trimStart, bool trimEnd) {
      string ret;
      IXmlLineInfo lineInfo = node as IXmlLineInfo;
      string code = node.Value.Trim('\n');
      if (trimStart) {
        string[] lines = code.Split('\n');
        int trim = lines.Where(l => l.Length &gt; 0).Min(l => l.Length - l.TrimStart().Length);
        for (int l = 0; l &lt; lines.Length; l++) {
          if (lines[l].Length > 0) {
            lines[l] = lines[l].Substring(trim);
          }
        }
        ret = string.Join("\n", lines).TrimStart();
      } else {
        ret = code;
      }
     
      if (trimEnd) {
        ret = ret.TrimEnd();
      }
        
      return ret;
    }
  </msxsl:script>

  <xsl:template match="text()" priority="100">
    <xsl:value-of select="ext:trimStart(., not(preceding-sibling::*), not(following-sibling::*))"/>
  </xsl:template>

</xsl:stylesheet>
