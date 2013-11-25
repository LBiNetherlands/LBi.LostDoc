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
  <xsl:include href="DocComments.xslt"/>
  <xsl:include href="Navigation2.xslt"/>
  <xsl:include href="Syntax.xslt"/>

  <xsl:template name="navigation">
    <xsl:apply-templates select="ld:key('aid', $assetId)" mode="xnav"/>
  </xsl:template>

  <xsl:template name="title">
    <xsl:apply-templates select="ld:key('aid', $assetId)" mode="title"/>
  </xsl:template>

  <xsl:template name="content">
    <xsl:apply-templates select="ld:key('aid', $assetId)"/>
  </xsl:template>

  <xsl:template match="class | struct" mode="inheritance">
    <xsl:param name="innerNodeSet" select="false()"/>
    <xsl:choose>
      <xsl:when test="inherits">

        <xsl:apply-templates select="ld:key('aid', current()/inherits/@type)" mode="inheritance">
          <xsl:with-param name="innerNodeSet">
            <ul>
              <li>
                <xsl:choose>
                  <xsl:when test="$innerNodeSet">
                    <xsl:apply-templates select="." mode="link"/>
                    <xsl:copy-of select="$innerNodeSet"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <span class="current">
                      <xsl:apply-templates select="." mode="displayText"/>
                    </span>

                    <xsl:if test="ld:key('aidInherits', current()/@assetId)">
                      <ul class="descendants">
                        <xsl:apply-templates select="ld:key('aidInherits', current()/@assetId)" mode="inheritance-descendants"/>
                      </ul>
                    </xsl:if>
                  </xsl:otherwise>
                </xsl:choose>
              </li>
            </ul>
          </xsl:with-param>
        </xsl:apply-templates>
      </xsl:when>
      <xsl:otherwise>
        <!-- found root -->
        <ul class="type-hierarchy">
          <li>
            <xsl:apply-templates select="." mode="link"/>
            <xsl:copy-of select="$innerNodeSet"/>
          </li>
        </ul>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="class | struct" mode="inheritance-descendants">
    <xsl:if test="@assetId = ld:key('aidNoVer', ld:asset(current()/@assetId))[1]/@assetId">
      <li>
        <xsl:apply-templates select="." mode="link"/>
        <xsl:if test="ld:key('aidInherits', current()/@assetId)">
          <ul>
            <xsl:apply-templates select="ld:key('aidInherits', current()/@assetId)" mode="inheritance-descendants">
              <xsl:sort select="ld:join((ancestor::*[ancestor::namespace] | self::*)/@name, '.')"/>
            </xsl:apply-templates>
          </ul>
        </xsl:if>
      </li>
    </xsl:if>
  </xsl:template>

  <xsl:template match="class | struct | enum | delegate | interface">
    <h1>
      <xsl:call-template name="title"/>
    </h1>
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

    <xsl:if  test="self::class or self::struct">
      <h2>Inheritance Hierarchy</h2>
      <xsl:apply-templates select="." mode="inheritance"/>
    </xsl:if>

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

    <p>
      <xsl:text>The </xsl:text>
      <xsl:apply-templates select="ld:key('aid', $assetId)" mode="displayText"/>
      <xsl:text> type exposes the following members.</xsl:text>
    </p>

    <xsl:if test="constructor">
      <h2>Constructors</h2>
      <table>
        <thead>
          <tr>
            <th></th>
            <th>Name</th>
            <th>Description</th>
          </tr>
        </thead>
        <tbody>
          <xsl:apply-templates select="./constructor" mode="member-row"/>
        </tbody>
      </table>
    </xsl:if>

    <xsl:if test="property[not(@isPrivate = 'true' and implements)]">
      <h2>Properties</h2>
      <table>
        <thead>
          <tr>
            <th></th>
            <th>Name</th>
            <th>Description</th>
          </tr>
        </thead>
        <tbody>
          <xsl:apply-templates select="property[not(@isPrivate = 'true' and implements)]" mode="member-row">
            <xsl:sort select="@name"/>
          </xsl:apply-templates>
        </tbody>
      </table>
    </xsl:if>

    <xsl:if test="field">
      <xsl:choose>
        <xsl:when test="self::enum">
          <h2>Members</h2>
        </xsl:when>
        <xsl:otherwise>
          <h2>Fields</h2>
        </xsl:otherwise>
      </xsl:choose>
      <table>
        <thead>
          <tr>
            <th></th>
            <th>Name</th>
            <th>Description</th>
          </tr>
        </thead>
        <tbody>
          <xsl:apply-templates select="field" mode="member-row">
            <xsl:sort select="@name"/>
          </xsl:apply-templates>
        </tbody>
      </table>
    </xsl:if>

    <xsl:if test="method[not(@isPrivate = 'true' and implements)] and not(self::enum)">
      <h2>Methods</h2>
      <table>
        <thead>
          <tr>
            <th></th>
            <th>Name</th>
            <th>Description</th>
          </tr>
        </thead>
        <tbody>
          <xsl:apply-templates select="method[not(@isPrivate = 'true' and implements)]" mode="member-row">
            <xsl:sort select="@name"/>
          </xsl:apply-templates>
        </tbody>
      </table>
    </xsl:if>

    <xsl:if test="event[not(@isPrivate = 'true' and implements)]">
      <h2>Events</h2>
      <table>
        <thead>
          <tr>
            <th></th>
            <th>Name</th>
            <th>Description</th>
          </tr>
        </thead>
        <tbody>
          <xsl:apply-templates select="event[not(@isPrivate = 'true' and implements)]" mode="member-row">
            <xsl:sort select="@name"/>
          </xsl:apply-templates>
        </tbody>
      </table>
    </xsl:if>

    <xsl:if test="operator">
      <h2>Operators</h2>
      <table>
        <thead>
          <tr>
            <th></th>
            <th>Name</th>
            <th>Description</th>
          </tr>
        </thead>
        <tbody>
          <xsl:apply-templates select="operator" mode="member-row">
            <xsl:sort select="@name"/>
          </xsl:apply-templates>
        </tbody>
      </table>
    </xsl:if>

    <!-- explicit interface implementation -->
    <xsl:if test="*[@isPrivate = 'true' and implements]">
      <h2>Explicit Interface Implementations</h2>
      <table>
        <thead>
          <tr>
            <th></th>
            <th>Name</th>
            <th>Description</th>
          </tr>
        </thead>
        <tbody>
          <xsl:apply-templates select="*[@isPrivate = 'true' and implements]" mode="member-row">
            <xsl:sort select="ld:key('aid', current()/implements/@member)/@name"/>
          </xsl:apply-templates>
        </tbody>
      </table>
    </xsl:if>

    <xsl:if test="/bundle/assembly/namespace//class[attribute and ld:asset(attribute/@type) = 'T:System.Runtime.CompilerServices.ExtensionAttribute']/method[attribute and ld:asset(attribute/@type) = 'T:System.Runtime.CompilerServices.ExtensionAttribute' and param[1]/@type = $assetId]">
      <h2>Extension methods</h2>
      <table>
        <thead>
          <tr>
            <th></th>
            <th>Name</th>
            <th>Description</th>
          </tr>
        </thead>
        <tbody>
          <xsl:apply-templates select="/bundle/assembly/namespace//class[attribute and ld:asset(attribute/@type) = 'T:System.Runtime.CompilerServices.ExtensionAttribute']/method[attribute and ld:asset(attribute/@type) = 'T:System.Runtime.CompilerServices.ExtensionAttribute' and param[1]/@type = $assetId]" mode="member-row">
            <xsl:sort select="@name"/>
          </xsl:apply-templates>
        </tbody>
      </table>
    </xsl:if>

    <xsl:if test="doc:remarks">
      <h2>Remarks</h2>
      <p>
        <xsl:apply-templates select="doc:remarks"/>
      </p>
    </xsl:if>

    <xsl:if test="doc:example">
      <section>
        <h2>Example</h2>
        <xsl:apply-templates select="doc:example"/>
      </section>
    </xsl:if>
  </xsl:template>
</xsl:stylesheet>
