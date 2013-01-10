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
                xmlns:ld="urn:lostdoc-core"
                exclude-result-prefixes="msxsl">

  <!-- ancestry -->

  <xsl:template match="/bundle" mode="xnav-ancestor">
    <xsl:param name="current"/>
    <xsl:param name="children" select="false()"/>

    <li>
      <xsl:attribute name="class">
        <xsl:text>nav-root</xsl:text>
        <xsl:if test="not($current)">
          <xsl:text> nav-current</xsl:text>
        </xsl:if>
      </xsl:attribute>

      <a title="Library" href="{ld:resolveAsset('*:*', '0.0.0.0')}">
        <xsl:text>Library</xsl:text>
      </a>

      <xsl:if test="$children">
        <xsl:copy-of select="$children"/>
      </xsl:if>
    </li>
  </xsl:template>

  <xsl:template match="/bundle/assembly" mode="xnav-ancestor">
    <xsl:param name="current"/>
    <xsl:param name="children" select="false()"/>

    <xsl:apply-templates select="parent::bundle" mode="xnav-ancestor">
      <xsl:with-param name="current" select="$current"/>
    </xsl:apply-templates>

    <li>
      <xsl:attribute name="class">
        <xsl:text>nav-child</xsl:text>
        <xsl:if test="$current = @assetId">
          <xsl:text> nav-current</xsl:text>
        </xsl:if>
      </xsl:attribute>

      <xsl:variable name="title">
        <xsl:apply-templates select="." mode="title"/>
      </xsl:variable>
      <a title="{$title}" href="{ld:resolve(@assetId)}">
        <xsl:value-of select="$title"/>
      </a>

      <xsl:if test="$children">
        <xsl:copy-of select="$children"/>
      </xsl:if>
    </li>
  </xsl:template>

  <xsl:template match="/bundle/assembly/namespace" mode="xnav-ancestor">
    <xsl:param name="current"/>
    <xsl:param name="children" select="false()"/>

    <xsl:choose>
      <xsl:when test="/bundle/assembly/namespace[@phase = '0' and @name = ld:substringBeforeLast(current()/@name, '.')]">
        <xsl:apply-templates select="(/bundle/assembly/namespace[@phase = '0' and @name = ld:substringBeforeLast(current()/@name, '.')])[1]" mode="xnav-ancestor">
          <xsl:with-param name="current" select="$current"/>
        </xsl:apply-templates>
      </xsl:when>
      <xsl:otherwise>
        <xsl:apply-templates select="ancestor::bundle" mode="xnav-ancestor">
          <xsl:with-param name="current" select="$current"/>
        </xsl:apply-templates>
      </xsl:otherwise>
    </xsl:choose>

    <li>
      <xsl:attribute name="class">
        <xsl:text>nav-ancestor</xsl:text>
        <xsl:if test="$current = @assetId">
          <xsl:text> nav-current</xsl:text>
        </xsl:if>
      </xsl:attribute>

      <xsl:variable name="title">
        <xsl:apply-templates select="." mode="title"/>
      </xsl:variable>
      <a title="{$title}" href="{ld:resolve(@assetId)}">
        <xsl:value-of select="$title"/>
      </a>

      <xsl:if test="$children">
        <xsl:copy-of select="$children"/>
      </xsl:if>
    </li>
  </xsl:template>

  <xsl:template match="/bundle/assembly/namespace/*" mode="xnav-ancestor">
    <xsl:param name="current"/>
    <xsl:param name="children" select="false()"/>


    <xsl:apply-templates select="parent::*" mode="xnav-ancestor">
      <xsl:with-param name="current" select="$current"/>
    </xsl:apply-templates>

    <li>
      <xsl:attribute name="class">
        <xsl:text>nav-ancestor</xsl:text>
        <xsl:if test="$current = @assetId">
          <xsl:text> nav-current</xsl:text>
        </xsl:if>
      </xsl:attribute>

      <xsl:variable name="title">
        <xsl:apply-templates select="." mode="title"/>
      </xsl:variable>
      <a title="{$title}" href="{ld:resolve(@assetId)}">
        <xsl:value-of select="$title"/>
      </a>

      <xsl:if test="$children">
        <xsl:copy-of select="$children"/>
      </xsl:if>
    </li>
  </xsl:template>

  <xsl:template match="/bundle/assembly/namespace/*/*" mode="xnav-ancestor">
    <xsl:param name="current"/>
    <xsl:param name="children" select="false()"/>

    <xsl:apply-templates select="ancestor::namespace" mode="xnav-ancestor">
      <xsl:with-param name="current" select="$current"/>
    </xsl:apply-templates>

    <li>
      <xsl:attribute name="class">
        <xsl:text>nav-ancestor</xsl:text>
        <xsl:if test="$current = @assetId">
          <xsl:text> nav-current</xsl:text>
        </xsl:if>
      </xsl:attribute>

      <xsl:variable name="title">
        <xsl:apply-templates select="." mode="title"/>
      </xsl:variable>
      <a title="{$title}" href="{ld:resolve(@assetId)}">
        <xsl:value-of select="$title"/>
      </a>

      <xsl:if test="$children">
        <xsl:copy-of select="$children"/>
      </xsl:if>
    </li>
  </xsl:template>

  <xsl:template match="/bundle | /bundle/assembly" mode="xnav">
    <ul>
      <xsl:apply-templates select="/bundle" mode="xnav-ancestor">
        <xsl:with-param name="current" select="@assetId"/>
        <xsl:with-param name="children">
          <ul>
            <!-- list sibling assemblies and root namespace-->
            <xsl:apply-templates select="/bundle/assembly[@phase = '0' and not(preceding::assembly/@name = @name)]" mode="xnav-item">
              <xsl:with-param name="current" select="@assetId"/>
              <xsl:sort select="@name"/>
            </xsl:apply-templates>
            <xsl:apply-templates select="/bundle/assembly/namespace[@phase = '0' and not(contains(@name, '.')) and not(preceding::namespace/@name = @name)]" mode="xnav-item">
              <xsl:with-param name="current" select="@assetId"/>
              <xsl:sort select="@name"/>
            </xsl:apply-templates>
          </ul>
        </xsl:with-param>
      </xsl:apply-templates>
    </ul>
  </xsl:template>

  <xsl:template match="/bundle/assembly/namespace" mode="xnav">
    <ul>
      <xsl:choose>
        <xsl:when test="/bundle/assembly/namespace[@phase = '0' and @name = ld:substringBeforeLast(current()/@name, '.')]">
          <xsl:apply-templates select="(/bundle/assembly/namespace[@phase = '0' and @name = ld:substringBeforeLast(current()/@name, '.')])[1]" mode="xnav-ancestor">
            <xsl:with-param name="current" select="@assetId"/>
            <xsl:with-param name="children">
              <ul>
                <li class="nav-current">
                  <xsl:variable name="title">
                    <xsl:apply-templates select="." mode="title"/>
                  </xsl:variable>
                  <a title="{$title}" href="{ld:resolve(@assetId)}">
                    <xsl:value-of select="$title"/>
                  </a>
                  <ul>
                    <!-- list child namespaces -->
                    <xsl:apply-templates select="/bundle/assembly/namespace[@phase = '0' and current()/@name = ld:substringBeforeLast(@name, '.') and not(preceding::namespace/@name = @name)]" mode="xnav-item">
                      <xsl:with-param name="current" select="@assetId"/>
                      <xsl:sort select="@name"/>
                    </xsl:apply-templates>
                    <!-- list types -->
                    <xsl:apply-templates select="/bundle/assembly/namespace[current()/@name = @name]//*[(self::class | self::struct | self::enum | self::delegate) and @phase = '0']"
                                         mode="xnav-item">
                      <xsl:sort select="ld:join((ancestor::*[ancestor::namespace] | self::*)/@name, '.')"/>
                    </xsl:apply-templates>
                  </ul>
                </li>
              </ul>
            </xsl:with-param>
          </xsl:apply-templates>
        </xsl:when>
        <xsl:otherwise>
          <xsl:apply-templates select="ancestor::bundle" mode="xnav-ancestor">
            <xsl:with-param name="current" select="@assetId"/>
            <xsl:with-param name="children">
              <ul>
                <li class="nav-current">
                  <xsl:variable name="title">
                    <xsl:apply-templates select="." mode="title"/>
                  </xsl:variable>
                  <a title="{$title}" href="{ld:resolve(@assetId)}">
                    <xsl:value-of select="$title"/>
                  </a>
                  <ul>
                    <!-- list child namespaces -->
                    <xsl:apply-templates select="/bundle/assembly/namespace[@phase = '0' and current()/@name = ld:substringBeforeLast(@name, '.') and not(preceding::namespace/@name = @name)]" mode="xnav-item">
                      <xsl:with-param name="current" select="@assetId"/>
                      <xsl:sort select="@name"/>
                    </xsl:apply-templates>
                    <!-- list types -->
                    <xsl:apply-templates select="/bundle/assembly/namespace[current()/@name = @name]//*[(self::class | self::struct | self::enum | self::delegate) and @phase = '0']"
                                         mode="xnav-item">
                      <xsl:sort select="ld:join((ancestor::*[ancestor::namespace] | self::*)/@name, '.')"/>
                    </xsl:apply-templates>
                  </ul>
                </li>
              </ul>
            </xsl:with-param>
          </xsl:apply-templates>
        </xsl:otherwise>
      </xsl:choose>

    </ul>
  </xsl:template>

  <xsl:template match="/bundle/assembly/namespace//*[(self::class | self::struct | self::enum | self::delegate)]" mode="xnav">
    <ul>

      <xsl:apply-templates select="ancestor::namespace" mode="xnav-ancestor">
        <xsl:with-param name="current" select="@assetId"/>
        <xsl:with-param name="children">
          <ul>
            <li class="nav-current">
              <xsl:variable name="title">
                    <xsl:apply-templates select="." mode="title"/>
                  </xsl:variable>
                  <a title="{$title}" href="{ld:resolve(@assetId)}">
                <xsl:apply-templates select="." mode="title"/>
              </a>
              <ul>
                <!-- list members -->
                <xsl:apply-templates select="*[@assetId and @phase = '0' and (self::method | self::property | self::field | self::event | self::constructor | self::operator)]" mode="xnav-item">
                  <xsl:with-param name="current" select="@assetId"/>
                  <!--<xsl:sort select="ld:iif(@isPrivate = 'true' and implements, ld:join( (/bundle/assembly/namespace//*[@assetId = current()/implements/@member]/ancestor::*[ancestor::namespace] | /bundle/assembly/namespace//*[@assetId = current()/implements/@member])/@name, '.'), ld:iif(self::operator, substring-after(@name, 'op_'),  @name))"/>-->
                  <xsl:sort select="ld:iif(@isPrivate = 'true' and implements, ld:join( (ld:key('aid', current()/implements/@member)/ancestor::*[ancestor::namespace] | ld:key('aid', current()/implements/@member))/@name, '.'), ld:iif(self::operator, substring-after(@name, 'op_'),  @name))"/>
                </xsl:apply-templates>
              </ul>
            </li>
          </ul>
        </xsl:with-param>
      </xsl:apply-templates>
    </ul>
  </xsl:template>

  <xsl:template match="/bundle/assembly/namespace/*[(self::class | self::struct | self::enum | self::delegate)]//*[(self::method | self::property | self::field | self::event | self::constructor | self::operator)]" mode="xnav">
    <ul>
      <xsl:choose>
        <xsl:when test="(self::method) and (preceding-sibling::method/@name = @name or following-sibling::method/@name = @name)">
          <xsl:apply-templates select="parent::*" mode="xnav-ancestor">
            <xsl:with-param name="current" select="@assetId"/>
            <xsl:with-param name="children">
              <ul>
                <li>
                  <xsl:variable name="title">
                    <xsl:value-of select="@name"/>
                    <xsl:text> </xsl:text>
                    <xsl:apply-templates select="." mode="nounPlural"/>
                  </xsl:variable>
                  <xsl:variable name="aid" select="substring-after(ld:asset(@assetId), ':')"/>
                  <xsl:variable name="asset" select="ld:coalesce(substring-before($aid, '('), $aid)"/>
                  <xsl:variable name="leading" select="ld:substringBeforeLast($asset, '.')"/>
                  <xsl:variable name="leading-clean" select="ld:iif($leading, concat($leading, '.'), '')"/>
                  <xsl:variable name="trailing" select="ld:coalesce(ld:substringAfterLast($asset, '.'), $asset)"/>
                  <xsl:variable name="trailing-clean" select="ld:coalesce(ld:substringBeforeLast($trailing, '`'), $trailing)"/>
                  
                  <a title="{$title}" href="{ld:resolveAsset(concat('Overload:', $leading-clean, $trailing-clean), ld:version(@assetId))}">
                    <xsl:value-of select="$title"/>
                  </a>

                  <ul>
                    <!-- list members -->
                    <!--<xsl:apply-templates select="/bundle/assembly/namespace//*[@phase = '0' and ld:nover(@assetId) = ld:nover(current()/parent::*/@assetId)]/*[@assetId and @phase = '0']" mode="xnav-item">-->
                    <xsl:apply-templates select="parent::*/method[@phase = '0' and @name = current()/@name]" mode="xnav-item-disambiguation">
                      <xsl:with-param name="current" select="@assetId"/>
                      <!--<xsl:sort select="ld:iif(@isPrivate = 'true' and implements, ld:join( (/bundle/assembly/namespace//*[@assetId = current()/implements/@member]/ancestor::*[ancestor::namespace] | /bundle/assembly/namespace//*[@assetId = current()/implements/@member])/@name, '.'), ld:iif(self::operator, substring-after(@name, 'op_'),  @name))"/>-->
                      <xsl:sort select="ld:iif(@isPrivate = 'true' and implements, ld:join( (ld:key('aid', current()/implements/@member)/ancestor::*[ancestor::namespace] | ld:key('aid', current()/implements/@member))/@name, '.'), ld:iif(self::operator, substring-after(@name, 'op_'),  @name))"/>
                    </xsl:apply-templates>
                  </ul>
                </li>
              </ul>
            </xsl:with-param>
          </xsl:apply-templates>
        </xsl:when>
        <xsl:when test="(self::property) and (preceding-sibling::property/@name = @name or following-sibling::property/@name = @name)">
          <xsl:apply-templates select="parent::*" mode="xnav-ancestor">
            <xsl:with-param name="current" select="@assetId"/>
            <xsl:with-param name="children">
              <ul>
                <li>
                  <xsl:variable name="title">
                    <xsl:value-of select="@name"/>
                    <xsl:text> </xsl:text>
                    <xsl:apply-templates select="." mode="nounPlural"/>
                  </xsl:variable>
                  <xsl:variable name="aid" select="substring-after(ld:asset(@assetId), ':')"/>
                  <xsl:variable name="asset" select="ld:coalesce(substring-before($aid, '('), $aid)"/>
                  <xsl:variable name="leading" select="ld:substringBeforeLast($asset, '.')"/>
                  <xsl:variable name="leading-clean" select="ld:iif($leading, concat($leading, '.'), '')"/>
                  <xsl:variable name="trailing" select="ld:coalesce(ld:substringAfterLast($asset, '.'), $asset)"/>
                  <xsl:variable name="trailing-clean" select="ld:coalesce(ld:substringBeforeLast($trailing, '`'), $trailing)"/>

                  <a title="{$title}" href="{ld:resolveAsset(concat('Overload:', $leading-clean, $trailing-clean), ld:version(@assetId))}">
                    <xsl:value-of select="$title"/>
                  </a>

                  <ul>
                    <!-- list members -->
                    <!--<xsl:apply-templates select="/bundle/assembly/namespace//*[@phase = '0' and ld:nover(@assetId) = ld:nover(current()/parent::*/@assetId)]/*[@assetId and @phase = '0']" mode="xnav-item">-->
                    <xsl:apply-templates select="parent::*/property[@phase = '0' and @name = current()/@name]" mode="xnav-item-disambiguation">
                      <xsl:with-param name="current" select="@assetId"/>
                      <!--<xsl:sort select="ld:iif(@isPrivate = 'true' and implements, ld:join( (/bundle/assembly/namespace//*[@assetId = current()/implements/@member]/ancestor::*[ancestor::namespace] | /bundle/assembly/namespace//*[@assetId = current()/implements/@member])/@name, '.'), ld:iif(self::operator, substring-after(@name, 'op_'),  @name))"/>-->
                      <xsl:sort select="ld:iif(@isPrivate = 'true' and implements, ld:join( (ld:key('aid', current()/implements/@member)/ancestor::*[ancestor::namespace] | ld:key('aid', current()/implements/@member))/@name, '.'), ld:iif(self::operator, substring-after(@name, 'op_'),  @name))"/>
                    </xsl:apply-templates>
                  </ul>
                </li>
              </ul>
            </xsl:with-param>
          </xsl:apply-templates>
        </xsl:when>
        <xsl:when test="(self::operator) and (preceding-sibling::operator/@name = @name or following-sibling::operator/@name = @name)">
          <xsl:apply-templates select="parent::*" mode="xnav-ancestor">
            <xsl:with-param name="current" select="@assetId"/>
            <xsl:with-param name="children">
              <ul>
                <li>
                  <xsl:variable name="title">
                    <xsl:value-of select="substring-after(@name, 'op_')"/>
                    <xsl:text> </xsl:text>
                    <xsl:apply-templates select="." mode="nounPlural"/>
                  </xsl:variable>
                  <xsl:variable name="aid" select="substring-after(ld:asset(@assetId), ':')"/>
                  <xsl:variable name="asset" select="ld:coalesce(substring-before($aid, '('), $aid)"/>
                  <xsl:variable name="leading" select="ld:substringBeforeLast($asset, '.')"/>
                  <xsl:variable name="leading-clean" select="ld:iif($leading, concat($leading, '.'), '')"/>
                  <xsl:variable name="trailing" select="ld:coalesce(ld:substringAfterLast($asset, '.'), $asset)"/>
                  <xsl:variable name="trailing-clean" select="ld:coalesce(ld:substringBeforeLast($trailing, '`'), $trailing)"/>

                  <a title="{$title}" href="{ld:resolveAsset(concat('Overload:', $leading-clean, $trailing-clean), ld:version(@assetId))}">
                    <xsl:value-of select="$title"/>
                  </a>

                  <ul>
                    <!-- list members -->
                    <!--<xsl:apply-templates select="/bundle/assembly/namespace//*[@phase = '0' and ld:nover(@assetId) = ld:nover(current()/parent::*/@assetId)]/*[@assetId and @phase = '0']" mode="xnav-item">-->
                    <xsl:apply-templates select="parent::*/operator[@phase = '0' and @name = current()/@name]" mode="xnav-item-disambiguation">
                      <xsl:with-param name="current" select="@assetId"/>
                      <!--<xsl:sort select="ld:iif(@isPrivate = 'true' and implements, ld:join( (/bundle/assembly/namespace//*[@assetId = current()/implements/@member]/ancestor::*[ancestor::namespace] | /bundle/assembly/namespace//*[@assetId = current()/implements/@member])/@name, '.'), ld:iif(self::operator, substring-after(@name, 'op_'),  @name))"/>-->
                      <xsl:sort select="ld:iif(@isPrivate = 'true' and implements, ld:join( (ld:key('aid', current()/implements/@member)/ancestor::*[ancestor::namespace] | ld:key('aid', current()/implements/@member))/@name, '.'), ld:iif(self::operator, substring-after(@name, 'op_'),  @name))"/>
                    </xsl:apply-templates>
                  </ul>
                </li>
              </ul>
            </xsl:with-param>
          </xsl:apply-templates>
        </xsl:when>
        <xsl:otherwise>
          <xsl:apply-templates select="ancestor::namespace" mode="xnav-ancestor">
            <xsl:with-param name="current" select="@assetId"/>
            <xsl:with-param name="children">
              <ul>
                <li>
                  <xsl:variable name="title">
                    <xsl:apply-templates select="parent::*" mode="title"/>
                  </xsl:variable>
                  <a title="{$title}" href="{ld:resolve(parent::*/@assetId)}">
                    <xsl:value-of select="$title"/>
                  </a>
                  <ul>
                    <!-- list members -->
                    <!--<xsl:apply-templates select="/bundle/assembly/namespace//*[@phase = '0' and ld:nover(@assetId) = ld:nover(current()/parent::*/@assetId)]/*[@assetId and @phase = '0']" mode="xnav-item">-->
                    
                    <xsl:apply-templates select="parent::*/*[(self::method | self::property | self::field | self::event | self::constructor | self::operator) and @assetId and @phase = '0']" mode="xnav-item">
                      <xsl:with-param name="current" select="@assetId"/>
                      <!--<xsl:sort select="ld:iif(@isPrivate = 'true' and implements, ld:join( (/bundle/assembly/namespace//*[@assetId = current()/implements/@member]/ancestor::*[ancestor::namespace] | /bundle/assembly/namespace//*[@assetId = current()/implements/@member])/@name, '.'), ld:iif(self::operator, substring-after(@name, 'op_'),  @name))"/>-->
                      <xsl:sort select="ld:iif(@isPrivate = 'true' and implements, ld:join( (ld:key('aid', current()/implements/@member)/ancestor::*[ancestor::namespace] | ld:key('aid', current()/implements/@member))/@name, '.'), ld:iif(self::operator, substring-after(@name, 'op_'),  @name))"/>
                    </xsl:apply-templates>
                  </ul>
                </li>
              </ul>
            </xsl:with-param>
          </xsl:apply-templates>
        </xsl:otherwise>
      </xsl:choose>
    </ul>
  </xsl:template>

  <xsl:template match="/bundle/assembly/namespace/*[(self::class | self::struct | self::enum | self::delegate)]//*[(self::method | self::property | self::field | self::event | self::constructor | self::operator)]" mode="xnav-disambiguation">
    <ul>
      <xsl:apply-templates select="parent::*" mode="xnav-ancestor">
        <xsl:with-param name="current" select="@assetId"/>
        <xsl:with-param name="children">
          <ul>
            <li class="nav-current">
              <xsl:variable name="nodeType" select="local-name()"/>
              <xsl:variable name="title">
                <xsl:value-of select="ld:iif(self::operator, substring-after(@name, 'op_'), @name)"/>
                <xsl:text> </xsl:text>
                <xsl:apply-templates select="." mode="nounPlural"/>
              </xsl:variable>
              <xsl:variable name="aid" select="substring-after(ld:asset(@assetId), ':')"/>
              <xsl:variable name="asset" select="ld:coalesce(substring-before($aid, '('), $aid)"/>
              <xsl:variable name="leading" select="ld:substringBeforeLast($asset, '.')"/>
              <xsl:variable name="leading-clean" select="ld:iif($leading, concat($leading, '.'), '')"/>
              <xsl:variable name="trailing" select="ld:coalesce(ld:substringAfterLast($asset, '.'), $asset)"/>
              <xsl:variable name="trailing-clean" select="ld:coalesce(ld:substringBeforeLast($trailing, '`'), $trailing)"/>

              <a title="{$title}" href="{ld:resolveAsset(concat('Overload:', $leading-clean, $trailing-clean), ld:version(@assetId))}">
                <xsl:value-of select="$title"/>
              </a>

              <ul>
                <!-- list members -->
                <xsl:apply-templates select="parent::*/*[local-name() = $nodeType and @phase = '0' and @name = current()/@name]" mode="xnav-item-disambiguation">
                  <xsl:with-param name="current" select="false()"/>
                  <xsl:sort select="@name"/>
                </xsl:apply-templates>
              </ul>
            </li>
          </ul>
        </xsl:with-param>
      </xsl:apply-templates>
    </ul>
  </xsl:template>

  <xsl:template match="/bundle" mode="xnav-item">
    <xsl:param name="current"/>
    <li>
      <xsl:if test="$current = '*:*'">
        <xsl:attribute name="class">
          <xsl:text>nav-current</xsl:text>
        </xsl:attribute>
      </xsl:if>
      <xsl:variable name="title">
        <xsl:apply-templates select="." mode="title"/>
      </xsl:variable>
      <a title="{$title}" href="{ld:resolveAsset('*:*', '0.0.0.0')}">
        <xsl:value-of select="$title"/>
      </a>
    </li>
  </xsl:template>

  <xsl:template match="*" mode="xnav-item">
    <xsl:param name="current"/>
    <li>
      <xsl:if test="$current = @assetId">
        <xsl:attribute name="class">
          <xsl:text>nav-current</xsl:text>
        </xsl:attribute>
      </xsl:if>
      <xsl:variable name="title">
        <xsl:apply-templates select="." mode="title"/>
      </xsl:variable>
      <xsl:choose>
        <xsl:when test="@declaredAs">
          <a title="{$title}" href="{ld:resolve(@declaredAs)}">
            <xsl:value-of select="$title"/>
          </a>
        </xsl:when>
        <xsl:otherwise>
          <a title="{$title}" href="{ld:resolve(@assetId)}">
            <xsl:value-of select="$title"/>
          </a>
        </xsl:otherwise>
      </xsl:choose>
    </li>
  </xsl:template>

  <xsl:template match="/bundle/assembly/namespace//*[(self::class | self::struct | self::enum | self::delegate)]" mode="xnav-item">
    <xsl:param name="current"/>
    <!--<xsl:if test="not(preceding::*[@assetId and ld:cmpnover(@assetId , current()/@assetId)])">-->
    <!--<xsl:if test="count(ld:key('aidNoVer', ld:nover(@assetId))) = 1 or @assetId = ld:key('aidNoVer', ld:nover(@assetId))[1]/@assetId">-->
    <xsl:if test="count(ld:key('aidNoVer', ld:asset(@assetId))) = 1 or @assetId = ld:key('aidNoVer', ld:asset(@assetId))[1]/@assetId">
      <li>
        <xsl:if test="$current = @assetId">
          <xsl:attribute name="class">
            <xsl:text>nav-current</xsl:text>
          </xsl:attribute>
        </xsl:if>
        <xsl:variable name="title">
          <xsl:apply-templates select="." mode="title"/>
        </xsl:variable>
        <a title="{$title}" href="{ld:resolve(@assetId)}">
          <xsl:value-of select="$title"/>
        </a>
      </li>
    </xsl:if>
  </xsl:template>

  <xsl:template match="method" mode="xnav-item">
    <xsl:param name="current"/>
    <xsl:choose>
      <xsl:when test="not(preceding-sibling::method[@name = current()/@name]) and following-sibling::method[@name = current()/@name]">
        <xsl:variable name="title">
          <xsl:value-of select="@name"/>
          <xsl:text> </xsl:text>
          <xsl:apply-templates select="." mode="nounPlural"/>
        </xsl:variable>
        <li>
          <xsl:if test="$current = @assetId">
            <xsl:attribute name="class">
              <xsl:text>nav-current</xsl:text>
            </xsl:attribute>
          </xsl:if>
          <xsl:variable name="aid" select="substring-after(ld:asset(@assetId), ':')"/>
          <xsl:variable name="asset" select="ld:coalesce(substring-before($aid, '('), $aid)"/>
          <xsl:variable name="leading" select="ld:substringBeforeLast($asset, '.')"/>
          <xsl:variable name="leading-clean" select="ld:iif($leading, concat($leading, '.'), '')"/>
          <xsl:variable name="trailing" select="ld:coalesce(ld:substringAfterLast($asset, '.'), $asset)"/>
          <xsl:variable name="trailing-clean" select="ld:coalesce(ld:substringBeforeLast($trailing, '`'), $trailing)"/>

          <a title="{$title}" href="{ld:resolveAsset(concat('Overload:', $leading-clean, $trailing-clean), ld:version(@assetId))}">
            <xsl:value-of select="$title"/>
          </a>
        </li>
      </xsl:when>
      <xsl:when test="not(preceding-sibling::method[@name = current()/@name]) and not(following-sibling::method[@name = current()/@name])" >
        <li>
          <xsl:if test="$current = @assetId">
            <xsl:attribute name="class">
              <xsl:text>nav-current</xsl:text>
            </xsl:attribute>
          </xsl:if>
          <xsl:apply-templates select="." mode="link">
            <xsl:with-param name="includeNoun" select="true()"/>
          </xsl:apply-templates>
          <!--<xsl:variable name="title">
            <xsl:apply-templates select="." mode="title"/>
          </xsl:variable>
          <xsl:choose>
            <xsl:when test="@declaredAs">
              <a title="{$title}" href="{ld:resolve(@declaredAs)}">
                <xsl:value-of select="$title"/>
              </a>
            </xsl:when>
            <xsl:otherwise>
              <a title="{$title}" href="{ld:resolve(@assetId)}">
                <xsl:value-of select="$title"/>
              </a>
            </xsl:otherwise>
          </xsl:choose>-->
        </li>
      </xsl:when>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="property" mode="xnav-item">
    <xsl:param name="current"/>
    <xsl:choose>
      <xsl:when test="not(preceding-sibling::property[@name = current()/@name]) and following-sibling::property[@name = current()/@name]">
        <xsl:variable name="title">
          <xsl:value-of select="@name"/>
          <xsl:text> </xsl:text>
          <xsl:apply-templates select="." mode="nounPlural"/>
        </xsl:variable>
        <li>
          <xsl:if test="$current = @assetId">
            <xsl:attribute name="class">
              <xsl:text>nav-current</xsl:text>
            </xsl:attribute>
          </xsl:if>
          <xsl:variable name="aid" select="substring-after(ld:asset(@assetId), ':')"/>
          <xsl:variable name="asset" select="ld:coalesce(substring-before($aid, '('), $aid)"/>
          <xsl:variable name="leading" select="ld:substringBeforeLast($asset, '.')"/>
          <xsl:variable name="leading-clean" select="ld:iif($leading, concat($leading, '.'), '')"/>
          <xsl:variable name="trailing" select="ld:coalesce(ld:substringAfterLast($asset, '.'), $asset)"/>
          <xsl:variable name="trailing-clean" select="ld:coalesce(ld:substringBeforeLast($trailing, '`'), $trailing)"/>

          <a title="{$title}" href="{ld:resolveAsset(concat('Overload:', $leading-clean, $trailing-clean), ld:version(@assetId))}">
            <xsl:value-of select="$title"/>
          </a>
        </li>
      </xsl:when>
      <xsl:when test="not(preceding-sibling::property[@name = current()/@name]) and not(following-sibling::property[@name = current()/@name])" >
        <li>
          <xsl:if test="$current = @assetId">
            <xsl:attribute name="class">
              <xsl:text>nav-current</xsl:text>
            </xsl:attribute>
          </xsl:if>
          <xsl:variable name="title">
            <xsl:apply-templates select="." mode="title"/>
          </xsl:variable>
          <xsl:choose>
            <xsl:when test="@declaredAs">
              <a title="{$title}" href="{ld:resolve(@declaredAs)}">
                <xsl:value-of select="$title"/>
              </a>
            </xsl:when>
            <xsl:otherwise>
              <a title="{$title}" href="{ld:resolve(@assetId)}">
                <xsl:value-of select="$title"/>
              </a>
            </xsl:otherwise>
          </xsl:choose>
        </li>
      </xsl:when>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="operator" mode="xnav-item">
    <xsl:param name="current"/>
    <xsl:choose>
      <xsl:when test="not(preceding-sibling::operator[@name = current()/@name]) and following-sibling::operator[@name = current()/@name]">
        <xsl:variable name="title">
          <xsl:value-of select="substring-after(@name, 'op_')"/>
          <xsl:text> </xsl:text>
          <xsl:apply-templates select="." mode="nounPlural"/>
        </xsl:variable>
        <li>
          <xsl:if test="$current = @assetId">
            <xsl:attribute name="class">
              <xsl:text>nav-current</xsl:text>
            </xsl:attribute>
          </xsl:if>
          <xsl:variable name="aid" select="substring-after(ld:asset(@assetId), ':')"/>
          <xsl:variable name="asset" select="ld:coalesce(substring-before($aid, '('), $aid)"/>
          <xsl:variable name="leading" select="ld:substringBeforeLast($asset, '.')"/>
          <xsl:variable name="leading-clean" select="ld:iif($leading, concat($leading, '.'), '')"/>
          <xsl:variable name="trailing" select="ld:coalesce(ld:substringAfterLast($asset, '.'), $asset)"/>
          <xsl:variable name="trailing-clean" select="ld:coalesce(ld:substringBeforeLast($trailing, '`'), $trailing)"/>

          <a title="{$title}" href="{ld:resolveAsset(concat('Overload:', $leading-clean, $trailing-clean), ld:version(@assetId))}">
            <xsl:value-of select="$title"/>
          </a>
        </li>
      </xsl:when>
      <xsl:when test="not(preceding-sibling::operator[@name = current()/@name]) and not(following-sibling::operator[@name = current()/@name])" >
        <li>
          <xsl:if test="$current = @assetId">
            <xsl:attribute name="class">
              <xsl:text>nav-current</xsl:text>
            </xsl:attribute>
          </xsl:if>
          <xsl:variable name="title">
            <xsl:apply-templates select="." mode="title"/>
          </xsl:variable>
          <xsl:choose>
            <xsl:when test="@declaredAs">
              <a title="{$title}" href="{ld:resolve(@declaredAs)}">
                <xsl:value-of select="$title"/>
              </a>
            </xsl:when>
            <xsl:otherwise>
              <a title="{$title}" href="{ld:resolve(@assetId)}">
                <xsl:value-of select="$title"/>
              </a>
            </xsl:otherwise>
          </xsl:choose>
        </li>
      </xsl:when>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="method | property | operator" mode="xnav-item-disambiguation">
    <xsl:param name="current"/>
    <li>
      <xsl:if test="$current = @assetId">
        <xsl:attribute name="class">
          <xsl:text>nav-current</xsl:text>
        </xsl:attribute>
      </xsl:if>

      <xsl:variable name="title">
        <xsl:apply-templates select="." mode="title"/>
      </xsl:variable>
      <xsl:choose>
        <xsl:when test="@declaredAs">
          <a title="{$title}" href="{ld:resolve(@declaredAs)}">
            <xsl:value-of select="$title"/>
          </a>
        </xsl:when>
        <xsl:otherwise>
          <a title="{$title}" href="{ld:resolve(@assetId)}">
            <xsl:value-of select="$title"/>
          </a>
        </xsl:otherwise>
      </xsl:choose>

    </li>
  </xsl:template>
</xsl:stylesheet>
