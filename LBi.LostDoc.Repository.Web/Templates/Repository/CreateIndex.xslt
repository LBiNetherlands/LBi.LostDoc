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
                xmlns:doc="urn:doc"
                xmlns:hrc="urn:lostdoc-core:inheritance-hierarchy"
                xmlns:ld="urn:lostdoc-core"
                exclude-result-prefixes="msxsl doc hrc ld">
  <xsl:output method="xml"/>

  <xsl:include href="Naming.xslt"/>
  <xsl:include href="DocComments.xslt"/>

  <xsl:template match="/">
    <index>
      <xsl:apply-templates select="//*[@assetId and @phase='0']" mode="index"/>
    </index>
  </xsl:template>

  <xsl:template match="*[@assetId]" mode="index">
    <document assetId="{@assetId}">
      <name>
        <xsl:apply-templates select="ld:key('aid', @assetId)" mode="displayText"/>
      </name>
      <title>
        <xsl:apply-templates select="ld:key('aid', @assetId)" mode="title"/>
      </title>
      <summary>
        <xsl:variable name="resultSet">
          <xsl:apply-templates select="doc:summary" mode="doc"/>
        </xsl:variable>

        <xsl:copy-of select="msxsl:node-set($resultSet)//text()"/>
      </summary>

      <text>
        <xsl:variable name="resultSet">
          <xsl:apply-templates select=".//doc:*" mode="doc" />
        </xsl:variable>

        <xsl:copy-of select="msxsl:node-set($resultSet)//text()"/>
      </text>

      <type>
        <xsl:copy-of select="@isInternal | @isPrivate | @isProtected | @isSealed | @isStatic | @isPublic | @isProtectedAndInternal | @isProtectedOrInternal"/>
        <xsl:apply-templates select="." mode="type" />
      </type>

      <path>
        <xsl:variable name="path">
          <xsl:apply-templates select="." mode="path"/>
        </xsl:variable>

        <xsl:apply-templates select="msxsl:node-set($path)" mode="enrich-path"/>
      </path>

    </document>
  </xsl:template>

  <xsl:template match="fragment[@assetId]" mode="enrich-path">
    <xsl:copy>
      <xsl:copy-of select="@assetId"/>
      <xsl:attribute name="name">
        <xsl:apply-templates select="ld:key('aid', @assetId)[1]" mode="shortName"/>
      </xsl:attribute>
      <xsl:attribute name="url">
        <xsl:choose>
          <xsl:when test="ld:key('aid', @assetId)/@declaredAs">
            <xsl:value-of select="ld:resolve(ld:key('aid', @assetId)/@declaredAs)"/>
          </xsl:when>
          <xsl:otherwise>
            <xsl:value-of select="ld:resolve(@assetId)"/>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:attribute>
      <xsl:attribute name="type">
        <xsl:apply-templates select="ld:key('aid', @assetId)" mode="type"/>
      </xsl:attribute>
      <xsl:attribute name="blurb">
        <xsl:variable name="resultSet">
          <xsl:choose>
            <xsl:when test="ld:key('aid', @assetId)/@declaredAs">
              <xsl:apply-templates select="ld:key('aid', ld:key('aid', @assetId)/@declaredAs)/doc:summary" mode="doc"/>
            </xsl:when>
            <xsl:otherwise>
              <xsl:apply-templates select="ld:key('aid', @assetId)/doc:summary" mode="doc"/>
            </xsl:otherwise>
          </xsl:choose>
        </xsl:variable>
        <xsl:value-of select="msxsl:node-set($resultSet)//text()"/>
      </xsl:attribute>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="*[@assetId]" mode="path">
    <xsl:apply-templates select="parent::*" mode="path" />
    <fragment assetId ="{@assetId}" />
  </xsl:template>

  <xsl:template match="namespace[@assetId]" mode="path">
    <xsl:choose>
      <xsl:when test="contains(@name, '.')">
        <xsl:apply-templates select="parent::assembly/namespace[@name = ld:substringBeforeLast(current()/@name, '.')]" mode="path"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:apply-templates select="parent::assembly" mode="path"/>
      </xsl:otherwise>
    </xsl:choose>
    <fragment assetId ="{@assetId}" />
  </xsl:template>

  <xsl:template match="assembly[@assetId]" mode="path">
    <fragment assetId ="{@assetId}" />
  </xsl:template>

  <xsl:template match="*[@assetId]" mode="type">
    <xsl:choose>
      <xsl:when test="self::method and attribute and ld:asset(attribute/@type) = 'T:System.Runtime.CompilerServices.ExtensionAttribute'">
        <xsl:text>extension-method</xsl:text>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="name()"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
</xsl:stylesheet>


