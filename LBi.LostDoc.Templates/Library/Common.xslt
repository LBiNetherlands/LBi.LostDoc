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

  <xsl:template match="typeparam">
    <dt>
      <xsl:value-of select="@name"/>
    </dt>
    <dd>
      <xsl:apply-templates select="xdc:summary"/>
      <xsl:if test="not(xdc:summary)">
        <xsl:call-template name="missing"/>
      </xsl:if>
    </dd>
  </xsl:template>

  <!-- LINK -->

  <!-- all links are routed through this template -->
  <xsl:template name="link">
    <xsl:param name="text" />
    <xsl:param name="assetId" />
    <xsl:choose>
      <xsl:when test="ld:canResolve($assetId)">
        <a href="{ld:resolve($assetId)}" title="{$text}">
          <xsl:value-of select="$text"/>
        </a>
      </xsl:when>
      <xsl:otherwise>
        <!-- TODO should this be a link to bing/google? -->
        <span class="unresolved-link">
          <xsl:value-of select="$text"/>
        </span>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="bundle" mode="link" priority="101">
    <xsl:call-template name="link">
      <xsl:with-param name="assetId" select="ld:toAssetId('*:*', '0.0.0.0')"/>
      <xsl:with-param name="text" select="'Library'"/>
    </xsl:call-template>
  </xsl:template>

  <!-- this is for explicit interface implemenations -->
  <xsl:template match="*[not(@declaredAs) and @isPrivate = 'true' and implements]" mode="link" priority="100">
    <xsl:param name="includeNoun" select="false()" />
    <xsl:choose>
      <xsl:when test="ld:canResolve(@assetId)">
        <xsl:call-template name="link">
          <xsl:with-param name="assetId" select="@assetId"/>
          <xsl:with-param name="text">
            <xsl:apply-templates select="ld:key('aid',current()/implements/@member)/.." mode="displayText" />
            <xsl:text>.</xsl:text>
            <xsl:apply-templates select="ld:key('aid',current()/implements/@member)" mode="displayText"/>
            <xsl:if test="$includeNoun">
              <xsl:text> </xsl:text>
              <xsl:apply-templates select="." mode="nounSingular" />
            </xsl:if>
          </xsl:with-param>
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <!-- recursive call with implements/@member -->
        <xsl:apply-templates select="ld:key('aid', current()/implements/@member)" mode="link" >
          <xsl:with-param name="includeNoun" select="$includeNoun" />
        </xsl:apply-templates>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="*[not(@declaredAs)]" mode="link" priority="99">
    <xsl:param name="includeNoun" select="false()" />
    <xsl:param name="includeParent" select="false()" />

    <xsl:call-template name="link">
      <xsl:with-param name="assetId" select="@assetId"/>
      <xsl:with-param name="text">
        <xsl:if test="$includeParent">
          <xsl:apply-templates select="parent::*" mode="displayText"/>
          <xsl:text>.</xsl:text>
        </xsl:if>
        <xsl:apply-templates select="." mode="displayText"/>
        <xsl:if test="$includeNoun">
          <xsl:text> </xsl:text>
          <xsl:apply-templates select="." mode="nounSingular" />
        </xsl:if>
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <!-- this is for explicit interface implemenations -->
  <xsl:template match="*[@declaredAs and @isPrivate = 'true' and implements]" mode="link" priority="100">
    <xsl:param name="includeNoun" select="false()" />
    <xsl:choose>
      <xsl:when test="ld:canResolve(@declaredAs)">
        <xsl:call-template name="link">
          <xsl:with-param name="assetId" select="@declaredAs"/>
          <xsl:with-param name="text">
            <xsl:apply-templates select="ld:key('aid', current()/implements/@member)/.." mode="displayText"/>
            <xsl:text>.</xsl:text>
            <xsl:apply-templates select="ld:key('aid', current()/implements/@member)" mode="displayText"/>
            <xsl:if test="$includeNoun">
              <xsl:text> </xsl:text>
              <xsl:apply-templates select="." mode="nounSingular" />
            </xsl:if>
          </xsl:with-param>
        </xsl:call-template>
      </xsl:when>
      <xsl:otherwise>
        <!--recursive call with implements/@member-->
        <xsl:apply-templates select="ld:key('aid', current()/implements/@member)" mode="link">
          <xsl:with-param name="includeNoun" select="$includeNoun" />
        </xsl:apply-templates>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="*[@declaredAs]" mode="link" priority="99">
    <xsl:param name="includeNoun" select="false()" />
    <xsl:param name="includeParent" select="false()" />

    <xsl:call-template name="link">
      <xsl:with-param name="assetId" select="@declaredAs"/>
      <xsl:with-param name="text">
        <xsl:if test="$includeParent">
          <xsl:apply-templates select="parent::*" mode="displayText"/>
          <xsl:text>.</xsl:text>
        </xsl:if>
        <xsl:apply-templates select="." mode="displayText"/>
        <xsl:if test="$includeNoun">
          <xsl:text> </xsl:text>
          <xsl:apply-templates select="." mode="nounSingular" />
        </xsl:if>
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <xsl:template match="*" mode="link-overload">
    <xsl:variable name="aid" select="substring-after(ld:asset(@assetId), ':')"/>
    <xsl:variable name="asset" select="ld:coalesce(substring-before($aid, '('), $aid)"/>
    <xsl:variable name="leading" select="ld:substringBeforeLast($asset, '.')"/>
    <xsl:variable name="leading-clean" select="ld:iif($leading, concat($leading, '.'), '')"/>
    <xsl:variable name="trailing" select="ld:coalesce(ld:substringAfterLast($asset, '.'), $asset)"/>
    <xsl:variable name="trailing-clean" select="ld:coalesce(substring-before($trailing, '`'), $trailing)"/>

    <xsl:call-template name="link">
      <xsl:with-param name="assetId" select="ld:toAssetId(concat('Overload:', $leading-clean, $trailing-clean), ld:version(@assetId))"/>
      <xsl:with-param name="text">
        <xsl:apply-templates select="." mode="overload-title" />
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <xsl:template match="method | property" mode="overload-title">
    <xsl:value-of select="@name"/>
    <xsl:text> </xsl:text>
    <xsl:apply-templates select="." mode="nounPlural"/>
  </xsl:template>

  <xsl:template match="operator" mode="overload-title">
    <xsl:value-of select="substring-after(@name, 'op_')"/>
    <xsl:text> </xsl:text>
    <xsl:apply-templates select="." mode="nounPlural"/>
  </xsl:template>

  <!-- Member table row -->

  <xsl:template match="method | property | constructor | field | operator" mode="member-row">
    <tr>
      <td class="icons">
        <xsl:if test="@isPrivate = 'true' and implements">
          <span class="icon-interface">
            <xsl:text>&#160;</xsl:text>
          </span>
        </xsl:if>
        <span>
          <xsl:attribute name="class">
            <xsl:text>icon-</xsl:text>
            <xsl:choose>
              <xsl:when test="self::method and attribute and ld:asset(attribute/@type) = 'T:System.Runtime.CompilerServices.ExtensionAttribute'">
                <xsl:text>extension-method</xsl:text>
              </xsl:when>
              <xsl:when test="self::constructor">
                <xsl:text>method</xsl:text>
              </xsl:when>
              <xsl:when test="self::operator">
                <xsl:text>operator</xsl:text>
              </xsl:when>
              <xsl:otherwise>
                <xsl:value-of select="name()"/>
              </xsl:otherwise>
            </xsl:choose>
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
        <xsl:if test="@isStatic and not(attribute and ld:asset(attribute/@type) = 'T:System.Runtime.CompilerServices.ExtensionAttribute')">
          <span class="icon-static">
            <xsl:text>&#160;</xsl:text>
          </span>
        </xsl:if>
      </td>
      <td>
        <xsl:choose>
          <xsl:when test="parent::enum and starts-with(ld:asset($assetId), 'T:')">
            <a name="{@name}">
              <xsl:apply-templates select="." mode="displayText"/>
            </a>
          </xsl:when>
          <xsl:otherwise>
            <xsl:apply-templates select="." mode="link"/>
          </xsl:otherwise>
        </xsl:choose>
      </td>
      <td>
        <xsl:apply-templates select="xdc:summary"/>
        <xsl:choose>
          <xsl:when test="@overrides">
            <xsl:if test="not(xdc:summary)">
              <xsl:apply-templates select="ld:key('aid', current()/@overrides)/xdc:summary"/>
              <xsl:if test="not(ld:key('aid', current()/@overrides)/xdc:summary)">
                <xsl:call-template name="missing"/>
              </xsl:if>
            </xsl:if>
            <xsl:text> (Overrides </xsl:text>
            <xsl:apply-templates select="ld:key('aid', @overrides)" mode="link">
              <xsl:with-param name="includeParent" select="true()" />
            </xsl:apply-templates>
            <xsl:text>)</xsl:text>
          </xsl:when>
          <xsl:when test="@declaredAs">
            <xsl:if test="not(xdc:summary)">
              <xsl:apply-templates select="ld:key('aid', current()/@declaredAs)/xdc:summary"/>
              <xsl:if test="not(ld:key('aid', current()/@declaredAs)/xdc:summary)">
                <xsl:call-template name="missing"/>
              </xsl:if>
            </xsl:if>
            <xsl:text> (Inherited from </xsl:text>
            <xsl:apply-templates select="ld:key('aid', current()/@declaredAs)/parent::*[@assetId]" mode="link" />
            <xsl:text>)</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:if test="not(xdc:summary)">
              <xsl:call-template name="missing"/>
            </xsl:if>
          </xsl:otherwise>
        </xsl:choose>
      </td>
    </tr>
  </xsl:template>

  <xsl:template match="@assetId" mode="version">
    <li>
      <xsl:call-template name="link">
        <xsl:with-param name="assetId" select="."/>
        <xsl:with-param name="text">
          <xsl:text>Version&#160;</xsl:text>
          <xsl:value-of select="ld:significantVersion(.)"/>
        </xsl:with-param>
      </xsl:call-template>
    </li>
  </xsl:template>
</xsl:stylesheet>
