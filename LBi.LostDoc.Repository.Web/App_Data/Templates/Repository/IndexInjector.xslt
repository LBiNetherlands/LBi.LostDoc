﻿<?xml version="1.0" encoding="utf-8"?>
<!-- 
  
  Copyright 2012 LBi Netherlands B.V.
  
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
                xmlns:ld="urn:lostdoc-core">

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
    <xsl:if test="$targets">
      <xsl:variable name="next" select="substring-after($targets, ',')"/>
      <xsl:variable name="first" select="normalize-space(ld:coalesce(substring-before($targets, ','), $targets))"/>
      <xsl:apply-templates select="/template/apply-stylesheet[@stylesheet = $first]" mode="inject"/>
      <xsl:if test="$next">
        <!-- if there is more data, inject own processing instructions -->
        <meta-template stylesheet="IndexInjector.xslt">
          <with-param name="targets" select="'{normalize-space($next)}'" />
        </meta-template>
      </xsl:if>
    </xsl:if>
  </xsl:template>

  <xsl:template match="apply-stylesheet"
                mode="inject">
    <apply-stylesheet name="{concat('Index for: ', ld:coalesce(@name, substring-before(@stylesheet, '.')))}"
                      stylesheet="CreateIndex.xslt"
                      assetId="concat(({@assetId}), '-index')"
                      output="'Index\index.xml'">
      <xsl:copy-of select="@*[local-name() != 'name' and local-name() != 'stylesheet' and local-name() != 'output' and local-name() != 'assetId']"/>
      <with-param name="assetId" select="@assetId" />
    </apply-stylesheet>
  </xsl:template>
</xsl:stylesheet>
