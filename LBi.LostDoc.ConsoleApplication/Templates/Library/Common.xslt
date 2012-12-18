<?xml version="1.0" encoding="utf-8"?>
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
                xmlns:doc="urn:doc"
                xmlns:ld="urn:lostdoc-core"
                exclude-result-prefixes="msxsl doc ld">

  <xsl:template match="typeparam">
    <dt>
      <xsl:value-of select="@name"/>
    </dt>
    <dd>
      <xsl:apply-templates select="doc:summary"/>
      <xsl:if test="not(doc:summary)">
        <xsl:call-template name="missing"/>
      </xsl:if>
    </dd>
  </xsl:template>

  <!--<xsl:template match="*[not(@declaredAs)]" mode="link">
    <a href="{ld:resolve(@assetId)}">
      <xsl:apply-templates select="." mode="displayText"/>
    </a>
  </xsl:template>

  <xsl:template match="*[@declaredAs]" mode="link">
    <a href="{ld:resolve(@declaredAs)}">
      <xsl:apply-templates select="." mode="displayText"/>
    </a>
  </xsl:template>-->

  <xsl:template match="*[not(@declaredAs)]" mode="link">
    <xsl:param name="includeNoun" select="false()" />
    <xsl:choose>
      <xsl:when test="@isPrivate = 'true' and implements">
        <xsl:choose>
          <xsl:when test="ld:canResolve(@assetId)">
            <xsl:variable name="title">
              <xsl:apply-templates select="/bundle/assembly/namespace//*[*/@assetId = current()/implements/@member]" mode="displayText" />
              <xsl:text>.</xsl:text>
              <xsl:apply-templates select="/bundle/assembly/namespace//*/*[@assetId = current()/implements/@member]" mode="displayText"/>
              <xsl:if test="$includeNoun">
                <xsl:text> </xsl:text>
                <xsl:apply-templates select="." mode="nounSingular" />
              </xsl:if>
            </xsl:variable>

            <a href="{ld:resolve(@assetId)}" title="{$title}">
              <xsl:value-of select="$title"/>
            </a>
          </xsl:when>
          <xsl:otherwise>
            <!-- recursive call with implements/@member -->
            <xsl:apply-templates select="/bundle/assembly/namespace//*/*[@assetId = current()/implements/@member]" mode="link" >
              <xsl:with-param name="includeNoun" select="$includeNoun" />
            </xsl:apply-templates>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:when>
      <xsl:otherwise>
        <xsl:variable name="title">
          <xsl:apply-templates select="." mode="displayText"/>
          <xsl:if test="$includeNoun">
            <xsl:text> </xsl:text>
            <xsl:apply-templates select="." mode="nounSingular" />
          </xsl:if>
        </xsl:variable>

        <a href="{ld:resolve(@assetId)}" title="{$title}">
          <xsl:value-of select="$title"/>
        </a>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="*[@declaredAs]" mode="link">
    <xsl:param name="includeNoun" select="false()" />
    <xsl:choose>
      <xsl:when test="@isPrivate = 'true' and implements">
        <xsl:choose>
          <xsl:when test="ld:canResolve(@declaredAs)">
            <xsl:variable name="title">
              <xsl:apply-templates select="/bundle/assembly/namespace//*[*/@assetId = current()/implements/@member]" mode="displayText" />
              <xsl:text>.</xsl:text>
              <xsl:apply-templates select="/bundle/assembly/namespace//*/*[@assetId = current()/implements/@member]" mode="displayText"/>
              <xsl:if test="$includeNoun">
                <xsl:text> </xsl:text>
                <xsl:apply-templates select="." mode="nounSingular" />
              </xsl:if>
            </xsl:variable>

            <a href="{ld:resolve(@declaredAs)}" title="{$title}">
              <xsl:value-of select="$title"/>
            </a>
          </xsl:when>
          <xsl:otherwise>
            <!--recursive call with implements/@member-->
            <xsl:apply-templates select="/bundle/assembly/namespace//*/*[@assetId = current()/implements/@member]" mode="link">
              <xsl:with-param name="includeNoun" select="$includeNoun" />
            </xsl:apply-templates>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:when>
      <xsl:otherwise>
        <xsl:variable name="title">
          <xsl:apply-templates select="." mode="displayText"/>
          <xsl:if test="$includeNoun">
            <xsl:text> </xsl:text>
            <xsl:apply-templates select="." mode="nounSingular" />
          </xsl:if>
        </xsl:variable>

        <a href="{ld:resolve(@declaredAs)}" title="{$title}">
          <xsl:value-of select="$title"/>
        </a>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>


  <xsl:template match="@assetId" mode="version">
    <li>
      <a href="{ld:resolve(.)}">
        <xsl:text>Version&#160;</xsl:text>
        <xsl:value-of select="ld:significantVersion(.)"/>
      </a>
    </li>
  </xsl:template>



</xsl:stylesheet>
