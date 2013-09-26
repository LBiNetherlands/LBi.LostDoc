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
                xmlns:hrc="urn:lostdoc-core:inheritance-hierarchy"
                xmlns:ld="urn:lostdoc-core"
                exclude-result-prefixes="msxsl doc hrc ld">

  <xsl:param name="assetId"/>

  <xsl:include href="Layout.xslt"/>
  <xsl:include href="Naming.xslt"/>
  <xsl:include href="Common.xslt"/>
  <xsl:include href="DocComments.xslt"/>
  <xsl:include href="Navigation2.xslt"/>
  <xsl:include href="Syntax.xslt"/>


  <xsl:template name="navigation">
    <!--<xsl:apply-templates select="/bundle/assembly/namespace//*[@assetId = $assetId]" mode="xnav"/>-->
    <xsl:apply-templates select="ld:key('aid', $assetId)" mode="xnav"/>
  </xsl:template>

  <xsl:template name="title">
    <xsl:choose>
      <!--<xsl:when test="/bundle/assembly/namespace//operator[@assetId = $assetId]">-->
      <xsl:when test="ld:key('aid', $assetId)[self::operator]">
        <!--<xsl:apply-templates select="/bundle/assembly/namespace//*[*/@assetId = $assetId]" mode="displayText"/>-->
        <xsl:apply-templates select="ld:key('aid', $assetId)/.." mode="displayText"/>
        <xsl:text> </xsl:text>        
      </xsl:when>
      <!--<xsl:when test="/bundle/assembly/namespace//*[@assetId = $assetId and not(self::constructor)]">-->
      <xsl:when test="ld:key('aid', $assetId)[not(self::constructor)]">
        <!--<xsl:apply-templates select="/bundle/assembly/namespace//*[*/@assetId = $assetId]" mode="displayText"/>-->
        <xsl:apply-templates select="ld:key('aid', $assetId)/.." mode="displayText"/>
        <xsl:text>.</xsl:text>
      </xsl:when>
    </xsl:choose>
    <!--<xsl:apply-templates select="/bundle/assembly/namespace//*/*[@assetId = $assetId]" mode="title"/>-->
    <xsl:apply-templates select="ld:key('aid', $assetId)" mode="title"/>
  </xsl:template>

  <xsl:template name="content">
    <!--<xsl:apply-templates select="/bundle/assembly/namespace//*/*[@assetId = $assetId]"/>-->
    <xsl:apply-templates select="ld:key('aid', $assetId)"/>    
  </xsl:template>

  <xsl:template match="constructor | method | property | field | event | operator">
    <h1>
      <xsl:call-template name="title"/>
    </h1>
    <div class="version">
      <span>
        Version <xsl:value-of select="ld:significantVersion(@assetId)"/>
      </span>
      <!--<xsl:if test="/bundle/assembly[ld:cmpnover(@assetId, current()/ancestor::assembly/@assetId)]/namespace//*[@assetId and ld:cmpnover(@assetId, current()/@assetId)]/@assetId">-->
      <xsl:if test="count(ld:key('aidNoVer', ld:asset(@assetId))) &gt; 1">
        <xsl:text>&#160;|&#160;</xsl:text>
        <div class="version-selector">
          <a href="javascript:void(0);">
            <xsl:text>Other Versions</xsl:text>
          </a>
          <ul>
            <xsl:apply-templates select="ld:key('aidNoVer', ld:asset(@assetId))/@assetId" mode="version"/>
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
    <dl class="origin">
      <dt>Namespace:</dt>
      <dd>
        <xsl:value-of select="ancestor::namespace/@name"/>
      </dd>
      <dt>Assembly:</dt>
      <dd>
        <xsl:value-of select="ancestor::assembly/@filename"/>
      </dd>
    </dl>

    <h2>Syntax</h2>
    <xsl:apply-templates select="." mode="syntax"/>

    <xsl:if test="typeparam">
      <h3>Type Parameters</h3>
      <dl>
        <xsl:apply-templates select="typeparam"/>
      </dl>
    </xsl:if>

    <xsl:if test="param">
      <h3>Parameters</h3>
      <dl class="parameters">
        <xsl:apply-templates select="param"/>
      </dl>
    </xsl:if>

    <xsl:if test="self::property">
      <h3>Property Value</h3>
      <dl>
        <dt>
          <xsl:text>Type: </xsl:text>
          <!--<xsl:apply-templates select="/bundle/assembly/namespace//*[@assetId = current()/@type]" mode="link" />-->
          <xsl:apply-templates select="ld:key('aid', current()/@type)" mode="link" />
        </dt>
        <dd>
          <xsl:apply-templates select="doc:returns"/>
          <xsl:if test="not(doc:returns)">
            <xsl:call-template name="missing"/>
          </xsl:if>
        </dd>
      </dl>
    </xsl:if>

    <xsl:if test="returns">
      <h3>Return Value</h3>
      <span>
        <xsl:apply-templates select="returns" mode="typeInfo"/>
      </span>
    </xsl:if>

    <xsl:if test="implements">
      <h3>Implements</h3>
      <ul>
        <xsl:apply-templates select="implements"/>
      </ul>
    </xsl:if>

    <xsl:if test="doc:exception">
      <h2>Exceptions</h2>
      <table>
        <thead>
          <tr>
            <th>
              <xsl:text>Exception</xsl:text>
            </th>
            <th>
              <xsl:text>Condition</xsl:text>
            </th>
          </tr>
        </thead>
        <tbody>
          <xsl:apply-templates select="doc:exception"/>
        </tbody>
      </table>
    </xsl:if>

    <xsl:if test="doc:remarks">
      <h2>Remarks</h2>
      <span>
        <xsl:apply-templates select="doc:remarks"/>
      </span>
    </xsl:if>

    <xsl:if test="doc:example">
      <h2>Example</h2>
      <span>
        <xsl:apply-templates select="doc:example"/>
      </span>
    </xsl:if>
  </xsl:template>

  <xsl:template match="param">
    <dt>
      <xsl:value-of select="@name"/>
    </dt>
    <dd>
      <xsl:apply-templates select="." mode="typeInfo"/>
    </dd>
  </xsl:template>

  <xsl:template match="implements">
    <li>
      <a href="{ld:resolve(@member)}">
        <!--<xsl:apply-templates select="//bundle/assembly/namespace//*[*/@assetId = current()/@member]/@assetId" mode="displayText"/>-->
        <xsl:apply-templates select="ld:key('aid', current()/@member)/../@assetId" mode="displayText"/>
        <xsl:text>.</xsl:text>
        <xsl:apply-templates select="@member" mode="displayText"/>
      </a>
    </li>
  </xsl:template>

  <xsl:template match="*" mode="typeInfo">
    <xsl:text>Type: </xsl:text>
    <xsl:choose>
      <xsl:when test="@param and (preceding-sibling::typeparam | following-sibling::typeparam)/@name = @param">
        <a href="{ld:resolve($assetId)}">
          <xsl:apply-templates select="@param" mode="displayText"/>
        </a>
      </xsl:when>
      <xsl:when test="@param">
        <a href="{ld:resolve(../../@assetId)}">
          <xsl:apply-templates select="@param" mode="displayText"/>
        </a>
      </xsl:when>
      <xsl:when test="@type">
        <a href="{ld:resolve(@type)}">
          <xsl:apply-templates select="@type" mode="displayText">
            <xsl:with-param name="attributes" select="attribute" />
          </xsl:apply-templates>
        </a>
      </xsl:when>
      <xsl:when test="arrayOf">
        <a href="{ld:resolve(descendant-or-self::arrayOf[@type]/@type | descendant-or-self::arrayOf[@param]/@param)}">
          <xsl:apply-templates select="." mode="displayText"/>
        </a>
      </xsl:when>
    </xsl:choose>
    <br/>
    <xsl:value-of select="doc:summary"/>
    <xsl:if test="parent::method[@declaredAs] and not(doc:summary)">
      <!--<xsl:apply-templates select="//*[@assetId = current()/parent::*/@declaredAs]/param[@name = current()/@name]/doc:summary"/>-->
      <xsl:apply-templates select="ld:key('aid', current()/parent::*/@declaredAs)/param[@name = current()/@name]/doc:summary"/>
      <!--<xsl:if test="not(//*[@assetId = current()/parent::*/@declaredAs]/doc:summary)">-->
      <xsl:if test="not(ld:key('aid', current()/parent::*/@declaredAs)/doc:summary)">
        <xsl:call-template name="missing"/>
      </xsl:if>
    </xsl:if>
  </xsl:template>

  <xsl:template match="doc:exception">
    <tr>
      <td>
        <a href="{ld:resolve(@cref)}">
          <xsl:apply-templates select="@cref" mode="displayText"/>
        </a>
      </td>
      <td>
        <xsl:apply-templates select="node()"/>
      </td>
    </tr>
  </xsl:template>
</xsl:stylesheet>


