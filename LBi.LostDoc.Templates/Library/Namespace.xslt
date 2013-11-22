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
                xmlns:doc="urn:doc"
                xmlns:hrc="urn:lostdoc-core:inheritance-hierarchy"
                xmlns:ld="urn:lostdoc-core"
                exclude-result-prefixes="msxsl doc hrc ld">

  <xsl:output method="html" indent="yes" omit-xml-declaration="yes"/>

  <xsl:param name="namespace"/>
  <xsl:param name="assetId"/>

  <xsl:include href="Layout.xslt"/>
  <xsl:include href="Naming.xslt"/>
  <xsl:include href="Common.xslt"/>
  <xsl:include href="Navigation2.xslt"/>
  <xsl:include href="DocComments.xslt"/>

  <xsl:template name="title">
    <xsl:if test="not($namespace)">
      <xsl:message terminate="yes">$namespace is empty!</xsl:message>
    </xsl:if>
    <xsl:text>Namespace </xsl:text>
    <xsl:value-of select="$namespace"/>
  </xsl:template>

  <xsl:template name="navigation">
    
    <xsl:apply-templates select="ld:key('aid', $assetId)[1]" mode="xnav"/>
  </xsl:template>

  <xsl:template name="content">
    <xsl:apply-templates select="(/bundle/assembly/namespace[@name=$namespace])[1]"/>
  </xsl:template>

  <xsl:template match="namespace">
    <h1>
      <xsl:value-of select="$namespace"/>
      <xsl:text> Namespace</xsl:text>
    </h1>
    <p>
      <xsl:apply-templates select="doc:summary"/>
      <xsl:if test="not(doc:summary)">
        <xsl:call-template name="missing"/>
      </xsl:if>
    </p>

    <xsl:call-template name="list-types">
      <xsl:with-param name="nodes" select="/bundle/assembly[@phase = '0']/namespace[@name = current()/@name]/class[@phase = '0']"/>
    </xsl:call-template>

    <xsl:call-template name="list-types">
      <xsl:with-param name="nodes" select="/bundle/assembly[@phase = '0']/namespace[@name = current()/@name]/struct[@phase = '0']"/>
    </xsl:call-template>

    <xsl:call-template name="list-types">
      <xsl:with-param name="nodes" select="/bundle/assembly[@phase = '0']/namespace[@name = current()/@name]/enum[@phase = '0']"/>
    </xsl:call-template>

    <xsl:call-template name="list-types">
      <xsl:with-param name="nodes" select="/bundle/assembly[@phase = '0']/namespace[@name = current()/@name]/delegate[@phase = '0']"/>
    </xsl:call-template>

    <xsl:call-template name="list-types">
      <xsl:with-param name="nodes" select="/bundle/assembly[@phase = '0']/namespace[@name = current()/@name]/interface[@phase = '0']"/>
    </xsl:call-template>

    <xsl:call-template name="list-types">
      <xsl:with-param name="nodes" select="/bundle/assembly[@phase = '0']/namespace[@phase = '0' and ld:substringBeforeLast(@name, '.') = $namespace ]"/>
    </xsl:call-template>

  </xsl:template>



  <xsl:template name="list-types">
    <xsl:param name="nodes"/>
    <xsl:if test="$nodes">
      <h2>
        <xsl:apply-templates select="$nodes[1]" mode="nounPlural"/>
      </h2>
      <table>
        <thead>
          <tr>
            <th>

            </th>
            <th>
              <xsl:apply-templates select="$nodes[1]" mode="nounSingular"/>
            </th>
            <th>
              <xsl:text>Description</xsl:text>
            </th>
          </tr>
        </thead>
        <tbody>
          <xsl:apply-templates select="$nodes" mode="list-item"/>
        </tbody>
      </table>
    </xsl:if>
  </xsl:template>



  <xsl:template match="class | struct | interface | delegate | enum | assembly" mode="list-item">
    <xsl:variable name="aid" select="ld:nover(@assetId)"/>
    <xsl:if test="count(ld:key('aidNoVer', ld:asset(@assetId))) = 1 or @assetId = ld:key('aidNoVer', ld:asset(@assetId))[1]/@assetId">
    <!--<xsl:if test="not(preceding::*[@assetId and ld:nover(@assetId) = $aid])">-->
      <tr>
        <td class="icons">
          <span>
            <xsl:attribute name="class">
              <xsl:text>icon-</xsl:text>
              <xsl:value-of select="name()"/>
              <xsl:choose>
                <xsl:when test="@isInternal">
                  <xsl:text>-internal</xsl:text>
                </xsl:when>
                <xsl:when test="@isPrivate">
                  <xsl:text>-private</xsl:text>
                </xsl:when>
                <xsl:when test="@isProtected">
                  <xsl:text>-protected</xsl:text>
                </xsl:when>
                <xsl:when test="@isSealed">
                  <xsl:text>-sealed</xsl:text>
                </xsl:when>
              </xsl:choose>
            </xsl:attribute>
            <xsl:text>&#160;</xsl:text>
          </span>
          <xsl:if test="@isStatic">
            <span class="icon-static">
              <xsl:text>&#160;</xsl:text>
            </span>
          </xsl:if>
        </td>
        <td>
          <a href="{ld:resolve(@assetId)}">
            <xsl:apply-templates select="." mode="displayText"/>
          </a>
        </td>
        <td>
          <xsl:apply-templates select="doc:summary"/>
          <xsl:if test="not(doc:summary)">
            <xsl:call-template name="missing"/>
          </xsl:if>
        </td>
      </tr>
    </xsl:if>
  </xsl:template>

  <xsl:template match="namespace" mode="list-item">
    <xsl:variable name="aid" select="ld:nover(@assetId)"/>
    <!--<xsl:if test="not(preceding::*[@assetId and ld:nover(@assetId) = $aid])">-->
    <xsl:if test="count(ld:key('aidNoVer', ld:asset(@assetId))) = 1 or @assetId = ld:key('aidNoVer', ld:asset(@assetId))[1]/@assetId">
      <tr>
        <td class="icons">
          <span class="icon-namespace">
          </span>
        </td>
        <td>
          <a>
            <xsl:attribute name="href">
              <xsl:value-of select="ld:resolve(ld:nover(@assetId))"/>
            </xsl:attribute>
            <xsl:apply-templates select="." mode="displayText"/>
          </a>
        </td>
        <td>
          <xsl:apply-templates select="doc:summary"/>
          <xsl:if test="not(doc:summary)">
            <xsl:call-template name="missing"/>
          </xsl:if>
        </td>
      </tr>
    </xsl:if>
  </xsl:template>


</xsl:stylesheet>
