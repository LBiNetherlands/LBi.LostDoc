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
                xmlns:xdc="urn:lostdoc:xml-doc-comment"
                xmlns:ld="urn:lostdoc:template"
                exclude-result-prefixes="msxsl xdc ld">

  <xsl:template match="xdc:summary">
    <xsl:apply-templates select="node()" mode="doc"/>
  </xsl:template>

  <xsl:template match="xdc:returns">
    <xsl:apply-templates select="node()" mode="doc"/>
  </xsl:template>
  
  <xsl:template match="see" mode="doc">
    <xsl:apply-templates select="ld:key('aid', @cref)" mode="link" />
  </xsl:template>


  <xsl:template match="paramref" mode="doc">
    <em>
      <xsl:value-of select="@name"/>
    </em>
  </xsl:template>

  <xsl:template match="xdc:example">
    <xsl:apply-templates select="node()" mode="doc"/>
  </xsl:template>

  <xsl:template match="code" mode="doc">
    <code>
      <xsl:apply-templates select="node()" mode="doc"/>
    </code>
  </xsl:template>

  <xsl:template name="missing">
    <span class="error">missing</span>
  </xsl:template>
</xsl:stylesheet>
