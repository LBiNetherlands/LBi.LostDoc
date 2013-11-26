﻿<?xml version="1.0" encoding="utf-8"?>
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

  <xsl:output method="html" indent="yes" omit-xml-declaration="yes"/>
  <xsl:param name="assetId"/>
  <xsl:param name="type"/>
  <xsl:param name="name"/>
  <xsl:param name="memberType" select="method"/>

  <xsl:include href="Layout.xslt"/>
  <xsl:include href="Naming.xslt"/>
  <xsl:include href="Common.xslt"/>
  <xsl:include href="DocComments.xslt"/>
  <xsl:include href="Navigation2.xslt"/>

  <xsl:template name="title">
    <xsl:apply-templates select="ld:key('aid', $type)" mode="displayText"/>
    <xsl:choose>
      <xsl:when test="$memberType = 'operator'">
        <xsl:text> </xsl:text>
        <xsl:value-of select="substring-after($name, 'op_')"/>
        <xsl:text> Conversions</xsl:text>
      </xsl:when>
      <xsl:otherwise>
        <xsl:text>.</xsl:text>
        <xsl:value-of select="$name"/>

        <xsl:choose>
          <xsl:when test="$memberType = 'property'">
            <xsl:text> Properties</xsl:text>
          </xsl:when>
          <xsl:when test="$memberType = 'method'">
            <xsl:text> Methods</xsl:text>
          </xsl:when>
        </xsl:choose>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="content">
    <xsl:apply-templates select="ld:key('aid', $type)"/>
  </xsl:template>

  <xsl:template match="class | struct | interface | enum">
    <h1>
      <xsl:call-template name="title"/>
    </h1>
    <div class="version">
      <span>
        Version <xsl:value-of select="ld:significantVersion(@assetId)"/>
      </span>
      <xsl:choose>
        <xsl:when test="$memberType = 'property' and ld:key('aidNoVer', ld:asset(current()/@assetId))[preceding-sibling::property/@name = @name or following-sibling::property/@name = @name]/@assetId">
          <xsl:text>&#160;|&#160;</xsl:text>
          <div class="version-selector">
            <a href="javascript:void(0);">
              <xsl:text>Other Versions</xsl:text>
            </a>
            <ul>
              <xsl:apply-templates select="ld:key('aidNoVer', ld:asset(current()/@assetId))[preceding-sibling::property/@name = @name or following-sibling::property/@name = @name]/@assetId" mode="version"/>
            </ul>
          </div>
        </xsl:when>
        <xsl:when test="$memberType = 'method' and ld:key('aidNoVer', ld:asset(current()/@assetId))[preceding-sibling::method/@name = @name or following-sibling::method/@name = @name]/@assetId">
          <xsl:text>&#160;|&#160;</xsl:text>
          <div class="version-selector">
            <a href="javascript:void(0);">
              <xsl:text>Other Versions</xsl:text>
            </a>
            <ul>
              <xsl:apply-templates select="ld:key('aidNoVer', ld:asset(current()/@assetId))[preceding-sibling::method/@name = @name or following-sibling::method/@name = @name]/@assetId" mode="version"/>
            </ul>
          </div>
        </xsl:when>
        <xsl:when test="$memberType = 'operator' and ld:key('aidNoVer', ld:asset(current()/@assetId))[preceding-sibling::operator/@name = @name or following-sibling::operator/@name = @name]/@assetId">
          <xsl:text>&#160;|&#160;</xsl:text>
          <div class="version-selector">
            <a href="javascript:void(0);">
              <xsl:text>Other Versions</xsl:text>
            </a>
            <ul>
              <xsl:apply-templates select="ld:key('aidNoVer', ld:asset(current()/@assetId))[preceding-sibling::operator/@name = @name or following-sibling::operator/@name = @name]/@assetId" mode="version"/>
            </ul>
          </div>
        </xsl:when>
      </xsl:choose>
    </div>

    <h2>Overload List</h2>
    <table>
      <thead>
        <tr>
          <th>

          </th>
          <th>Name</th>
          <th>
            <xsl:text>Description</xsl:text>
          </th>
        </tr>
      </thead>
      <tbody>
        <xsl:choose>
          <xsl:when test="$memberType = 'property'">
            <xsl:apply-templates select="property[@name = $name]" mode="member-row"/>
          </xsl:when>
          <xsl:when test="$memberType = 'method'">
            <xsl:apply-templates select="method[@name = $name]" mode="member-row"/>
          </xsl:when>
          <xsl:when test="$memberType = 'operator'">
            <xsl:apply-templates select="operator[@name = $name]" mode="member-row"/>
          </xsl:when>
        </xsl:choose>
      </tbody>
    </table>
  </xsl:template>

  <xsl:template name="navigation">
    <xsl:apply-templates select="(ld:key('aid', $type)/*[@name = $name and local-name() = $memberType])[1]" mode="xnav-disambiguation"/>
  </xsl:template>
</xsl:stylesheet>
