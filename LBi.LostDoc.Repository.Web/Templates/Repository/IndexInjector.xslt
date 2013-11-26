<?xml version="1.0" encoding="utf-8"?>
<!-- 
  
  Copyright 2012 DigitasLBi Netherlands B.V.
  
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
                exclude-result-prefixes="msxsl"
                xmlns:ld="urn:lostdoc:template"
                xmlns:xdc="urn:lostdoc:xml-doc-comment">

  <xsl:output method="xml" indent="yes"/>

  <xsl:param name="targets"/>

  <!-- by default just copy everything through -->
  <xsl:template match="@* | node()"
                priority="-1">
    <xsl:copy>
      <xsl:apply-templates select="@* | node()"/>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="meta-template[@stylesheet='IndexInjector.xslt']">

    <!--<xsl:variable name="selector">
      <xsl:call-template name="get-selector">
        <xsl:with-param name="targets" select="$targets"/>
      </xsl:call-template>
    </xsl:variable>-->

    <apply-stylesheet name="Index"
                      stylesheet="CreateIndex.xslt"
                      select="/"
                      assetId="'IX:*'"
                      version="'0.0.0.0'"
                      output="'index.xml'">
    </apply-stylesheet>
  </xsl:template>

  <!--<xsl:template name="get-selector">
    <xsl:param name="targets"/>
    <xsl:if test="$targets">
      <xsl:variable name="next" select="substring-after($targets, ',')"/>
      <xsl:variable name="first" select="normalize-space(ld:coalesce(substring-before($targets, ','), $targets))"/>
      <xsl:value-of select="/template/apply-stylesheet[@stylesheet = $first]"/>
      <xsl:call-template name="get-selector">
        <xsl:with-param name="targets" select="$next"/>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>-->
</xsl:stylesheet>
