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
                xmlns:ld="urn:lostdoc-core"
                exclude-result-prefixes="msxsl doc ld">

  <xsl:output method="html" indent="yes" omit-xml-declaration="yes"/>

  <xsl:param name="assetId"/>

  <xsl:include href="Layout.xslt"/>
  <xsl:include href="Naming.xslt"/>
  <xsl:include href="Common.xslt"/>
  <xsl:include href="Navigation2.xslt"/>
  <xsl:include href="DocComments.xslt"/>

  <xsl:template name="title">
    <xsl:apply-templates select="ld:key('aid', $assetId)" mode="title"/>
  </xsl:template>

  <xsl:template name="navigation">
    <xsl:apply-templates select="ld:key('aid', $assetId)" mode="xnav"/>
  </xsl:template>

  <xsl:template name="content">
    <h1>
      <xsl:call-template name="title"/>
    </h1>
    <xsl:apply-templates select="ld:key('aid', $assetId)"/>

  </xsl:template>

  <xsl:template match="assembly">
    <div class="version">
      <span>
        Version <xsl:value-of select="ld:significantVersion(@assetId)"/>
      </span>
      <xsl:if test="count(ld:key('aidNoVer', ld:asset(current()/@assetId))) &gt; 1">
        <xsl:text>&#160;|&#160;</xsl:text>
        <div class="version-selector">
          <a href="javascript:void(0);">
            <xsl:text>Other Versions</xsl:text>
          </a>
          <ul>
            <xsl:apply-templates select="ld:key('aidNoVer', ld:asset(current()/@assetId))/@assetId" mode="version"/>
          </ul>
        </div>
      </xsl:if>
    </div>
    <p class="summary">
      <xsl:apply-templates select="doc:summary"/>
      <xsl:if test="not(doc:summary)">
        <xsl:call-template name="missing"/>
      </xsl:if>
    </p>
    <xsl:apply-templates select="namespace[@phase = '0']">
      <xsl:sort select="@name"/>
    </xsl:apply-templates>
  </xsl:template>

  <xsl:template match="namespace">
    <xsl:if test="class | struct | enum | delegate | interface">
      <h2>
        <xsl:apply-templates select="." mode="title"/>
      </h2>

      <xsl:call-template name="list-types">
        <xsl:with-param name="nodes" select=".//class[@phase = '0']"/>
      </xsl:call-template>

      <xsl:call-template name="list-types">
        <xsl:with-param name="nodes" select=".//struct[@phase = '0']"/>
      </xsl:call-template>

      <xsl:call-template name="list-types">
        <xsl:with-param name="nodes" select=".//enum[@phase = '0']"/>
      </xsl:call-template>

      <xsl:call-template name="list-types">
        <xsl:with-param name="nodes" select=".//delegate[@phase = '0']"/>
      </xsl:call-template>

      <xsl:call-template name="list-types">
        <xsl:with-param name="nodes" select=".//interface[@phase = '0']"/>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>


  <xsl:template name="list-types">
    <xsl:param name="nodes"/>
    <xsl:if test="$nodes">
      <h3>
        <xsl:apply-templates select="$nodes[1]" mode="nounPlural"/>
      </h3>
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
          <xsl:apply-templates select="$nodes" mode="list-item">
            <xsl:sort select="@name"/>
          </xsl:apply-templates>
        </tbody>
      </table>
    </xsl:if>
  </xsl:template>


  <xsl:template match="class | struct | interface | delegate | enum | assembly" mode="list-item">
    <xsl:variable name="aid" select="ld:nover(@assetId)"/>
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
        </span>
      </td>
      <td>
        <xsl:apply-templates select="." mode="link" />
      </td>
      <td>
        <xsl:apply-templates select="doc:summary"/>
        <xsl:if test="not(doc:summary)">
          <xsl:call-template name="missing"/>
        </xsl:if>
      </td>
    </tr>
  </xsl:template>

</xsl:stylesheet>
