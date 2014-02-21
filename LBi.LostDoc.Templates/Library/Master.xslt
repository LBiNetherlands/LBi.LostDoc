﻿<?xml version="1.0" encoding="utf-8"?>
<!-- 
  
  Copyright 2014 DigitasLBi Netherlands B.V.
  
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

  <xsl:output method="xml" indent="yes"/>

  <xsl:include href="Naming.xslt"/>

  <xsl:template match="*" mode="display">
    <xsl:param name="overload" select="false()" />

    <title xml:lang="en">
      <xsl:choose>
        <xsl:when test="$overload">
          <xsl:apply-templates select="." mode="overload-title"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:apply-templates select="." mode="title"/>
        </xsl:otherwise>
      </xsl:choose>
    </title>
    <name xml:lang="en">
      <xsl:apply-templates select="." mode="displayText"/>
    </name>
  </xsl:template>

  <xsl:template match="/bundle">
    <node text="Library" assetId="">
      <!-- target="ld:resolveAsset('*:*', '0.0.0.0')" assetId="{{*:*,0.0.0.0}} -->

      <!-- assemblies -->
      <xsl:apply-templates select="assembly[@phase = '0']">
        <xsl:sort select="@name"/>
      </xsl:apply-templates>

      <!-- global namespace -->
      <xsl:apply-templates select="assembly/namespace[@name = '' and @phase = '0']"/>

      <!-- rest of namespaces -->
      <xsl:apply-templates select="assembly/namespace[@name != '' and @phase = '0']" mode="named-root-namespace">
        <xsl:sort select="@name"/>
      </xsl:apply-templates>
    </node>
  </xsl:template>

  <xsl:template match="assembly">
    <xsl:if test="not(preceding-sibling::assembly[@name=current()/@name and @phase = '0'])">
      <node assetId="{@assetId}">
        <xsl:apply-templates select="." mode="display"/>
      </node>
    </xsl:if>
  </xsl:template>

  <xsl:template match="namespace" mode="named-root-namespace">
    <xsl:if test="not(preceding::namespace[@name=current()/@name and @phase = '0'] | //namespace[starts-with(current()/@name, concat(@name, '.'))])">
      <node assetId="{@assetId}">
        <xsl:apply-templates select="." mode="display"/>

        <!-- child namespaces -->
        <xsl:apply-templates select="/bundle/assembly/namespace[starts-with(@name, concat(current()/@name, '.')) and @phase = '0']">
          <xsl:sort select="@name"/>
          <xsl:with-param name="parent" select="@name"/>
        </xsl:apply-templates>
      </node>
    </xsl:if>
  </xsl:template>

  <xsl:template match="namespace">
    <xsl:param name="parent" />
    <xsl:if test="not(preceding::namespace[@name=current()/@name and @phase = '0'])">
      <xsl:if test="not(contains(substring-after(@name, concat($parent, '.')), '.')) or not(//namespace[starts-with(current()/@name, concat(@name, '.')) and not(contains(substring-after(current()/@name, concat(@name, '.')), '.'))])">
        <node assetId="{@assetId}">
          <xsl:apply-templates select="." mode="display"/>

          <!-- child namespaces -->
          <xsl:apply-templates select="/bundle/assembly/namespace[starts-with(@name, concat(current()/@name, '.')) and @phase = '0']">
            <xsl:sort select="@name"/>
            <xsl:with-param name="parent" select="@name"/>
          </xsl:apply-templates>

          <xsl:apply-templates select="*"/>
        </node>
      </xsl:if>
    </xsl:if>
  </xsl:template>

  <xsl:template match="*[parent::namespace]">
    <node assetId="{@assetId}">
      <xsl:apply-templates select="." mode="display"/>

      <!-- constructors -->
      <xsl:apply-templates select="constructor"/>

      <!-- other members -->
      <xsl:apply-templates select="method | field | operator | event">
        <xsl:sort select="@name"/>
      </xsl:apply-templates>
    </node>
  </xsl:template>

  <!-- overloads -->
  <xsl:template match="method[preceding-sibling::method/@name = @name or following-sibling::method/@name = @name]">
    <xsl:if test="not(preceding-sibling::method/@name = @name)">

      <!-- TODO see if we can't add an ld:replace() + regex to do this instead -->
      <xsl:variable name="aid" select="substring-after(ld:asset(@assetId), ':')" />
      <xsl:variable name="asset" select="ld:coalesce(substring-before($aid, '('), $aid)" />
      <xsl:variable name="leading" select="ld:substringBeforeLast($asset, '.')"/>
      <xsl:variable name="leading-clean" select="ld:iif($leading, concat($leading, '.'), '')"/>
      <xsl:variable name="trailing" select="ld:coalesce(ld:substringAfterLast($asset, '.'), $asset)"/>
      <xsl:variable name="trailing-clean" select="ld:coalesce(substring-before($trailing, '`'), $trailing)"/>
      <xsl:variable name="assetId" select="concat('Overload:', $leading-clean, $trailing-clean)"/>
      
      <node assetId="{ld:toAssetId($assetId, ld:version(@assetId))}" >
        <xsl:apply-templates select="." mode="display">
          <xsl:with-param name="overload" select="true()" />
        </xsl:apply-templates>

        <xsl:apply-templates select="following-sibling::method/@name = @name" />
      </node>
    </xsl:if>
  </xsl:template>

  <xsl:template match="method | field | constructor | operator | event">
    <node assetId="{@assetId}" >
      <xsl:apply-templates select="." mode="display"/>
    </node>
  </xsl:template>

  <xsl:template match="@* | node()" mode="copy">
    <xsl:copy>
      <xsl:apply-templates select="@* | node()"/>
    </xsl:copy>
  </xsl:template>
</xsl:stylesheet>
