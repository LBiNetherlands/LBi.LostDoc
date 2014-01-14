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
                xmlns:ld="urn:lostdoc:template"
                exclude-result-prefixes="msxsl ld">

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

      <xsl:apply-templates select="." mode="link"/>

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

      <xsl:apply-templates select="." mode="link">
        <xsl:with-param name="includeNoun" select="true()" />
      </xsl:apply-templates>

      <xsl:if test="$children">
        <xsl:copy-of select="$children"/>
      </xsl:if>
    </li>
  </xsl:template>

  <xsl:template match="/bundle/assembly/namespace" mode="xnav-ancestor">
    <xsl:param name="current"/>
    <xsl:param name="children" select="false()"/>

    <xsl:choose>
      <xsl:when test="@name != '' and /bundle/assembly/namespace[@phase = '0' and @name = ld:substringBeforeLast(current()/@name, '.')]">
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

      <xsl:apply-templates select="." mode="link">
        <xsl:with-param name="includeNoun" select="true()" />
      </xsl:apply-templates>

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

      <xsl:apply-templates select="." mode="link">
        <xsl:with-param name="includeNoun" select="true()" />
      </xsl:apply-templates>

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

      <xsl:apply-templates select="." mode="link">
        <xsl:with-param name="includeNoun" select="true()" />
      </xsl:apply-templates>

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
                  <xsl:apply-templates select="." mode="link">
                    <xsl:with-param name="includeNoun" select="true()" />
                  </xsl:apply-templates>
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
                  <xsl:apply-templates select="." mode="link">
                    <xsl:with-param name="includeNoun" select="true()" />
                  </xsl:apply-templates>
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

  <xsl:template match="/bundle/assembly/namespace//*[(self::class | self::struct | self::enum | self::delegate | self::interface) and @assetId]" mode="xnav">
    <ul>
      <xsl:apply-templates select="ancestor::namespace" mode="xnav-ancestor">
        <xsl:with-param name="current" select="@assetId"/>
        <xsl:with-param name="children">
          <ul>
            <li class="nav-current">
              <xsl:apply-templates select="." mode="link">
                <xsl:with-param name="includeNoun" select="true()" />
              </xsl:apply-templates>
              <ul>
                <!-- list members -->
                <xsl:apply-templates select="*[@assetId and @phase = '0' and (self::method | self::property | self::field | self::event | self::constructor | self::operator)]" mode="xnav-item">
                  <xsl:with-param name="current" select="@assetId"/>
                  <xsl:sort select="ld:iif(@isPrivate = 'true' and implements, ld:join( (ld:key('aid', current()/implements/@member)/ancestor::*[ancestor::namespace] | ld:key('aid', current()/implements/@member))/@name, '.'), ld:iif(self::operator, substring-after(@name, 'op_'),  @name))"/>
                </xsl:apply-templates>
              </ul>
            </li>
          </ul>
        </xsl:with-param>
      </xsl:apply-templates>
    </ul>
  </xsl:template>

  <xsl:template match="/bundle/assembly/namespace/*[(self::class | self::struct | self::enum | self::delegate | self::interface)]//*[(self::method | self::property | self::field | self::event | self::constructor | self::operator)]" mode="xnav">
    <ul>
      <xsl:choose>
        <xsl:when test="(self::method) and (preceding-sibling::method/@name = @name or following-sibling::method/@name = @name)">
          <xsl:apply-templates select="parent::*" mode="xnav-ancestor">
            <xsl:with-param name="current" select="@assetId"/>
            <xsl:with-param name="children">
              <ul>
                <li>
                  <xsl:apply-templates select="." mode="link-overload"/>
                  
                  <ul>
                    <!-- list members -->
                    <xsl:apply-templates select="parent::*/method[@phase = '0' and @name = current()/@name]" mode="xnav-item-disambiguation">
                      <xsl:with-param name="current" select="@assetId"/>
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
                  <xsl:apply-templates select="." mode="link-overload"/>

                  <ul>
                    <!-- list members -->
                    <xsl:apply-templates select="parent::*/property[@phase = '0' and @name = current()/@name]" mode="xnav-item-disambiguation">
                      <xsl:with-param name="current" select="@assetId"/>
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
                  <xsl:apply-templates select="." mode="link-overload"/>

                  <ul>
                    <!-- list members -->
                    <xsl:apply-templates select="parent::*/operator[@phase = '0' and @name = current()/@name]" mode="xnav-item-disambiguation">
                      <xsl:with-param name="current" select="@assetId"/>
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
                  <xsl:apply-templates select="parent::*" mode="link">
                    <xsl:with-param name="includeNoun" select="true()" />
                  </xsl:apply-templates>
                  <ul>
                    <!-- list members -->
                    <xsl:apply-templates select="parent::*/*[(self::method | self::property | self::field | self::event | self::constructor | self::operator) and @assetId and @phase = '0']" mode="xnav-item">
                      <xsl:with-param name="current" select="@assetId"/>
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
              <xsl:apply-templates select="." mode="link-overload" />

              <ul>
                <!-- list members -->
                <xsl:variable name="nodeType" select="local-name()"/>
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
      <xsl:apply-templates select="." mode="link"/>
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
      <xsl:apply-templates select="." mode="link">
        <xsl:with-param name="includeNoun" select="true()" />
      </xsl:apply-templates>
    </li>
  </xsl:template>

  <xsl:template match="/bundle/assembly/namespace//*[(self::class | self::struct | self::enum | self::delegate)]" mode="xnav-item">
    <xsl:param name="current"/>
    <xsl:if test="count(ld:key('aidNoVer', ld:asset(@assetId))) = 1 or @assetId = ld:key('aidNoVer', ld:asset(@assetId))[1]/@assetId">
      <li>
        <xsl:if test="$current = @assetId">
          <xsl:attribute name="class">
            <xsl:text>nav-current</xsl:text>
          </xsl:attribute>
        </xsl:if>
        <xsl:apply-templates select="." mode="link">
          <xsl:with-param name="includeNoun" select="true()" />
        </xsl:apply-templates>
      </li>
    </xsl:if>
  </xsl:template>

  <xsl:template match="method" mode="xnav-item">
    <xsl:param name="current"/>
    <xsl:choose>
      <xsl:when test="not(preceding-sibling::method[@name = current()/@name]) and following-sibling::method[@name = current()/@name]">
        <li>
          <xsl:if test="$current = @assetId">
            <xsl:attribute name="class">
              <xsl:text>nav-current</xsl:text>
            </xsl:attribute>
          </xsl:if>

          <xsl:apply-templates select="." mode="link-overload"/>
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
        </li>
      </xsl:when>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="property" mode="xnav-item">
    <xsl:param name="current"/>
    <xsl:choose>
      <xsl:when test="not(preceding-sibling::property[@name = current()/@name]) and following-sibling::property[@name = current()/@name]">
        <li>
          <xsl:if test="$current = @assetId">
            <xsl:attribute name="class">
              <xsl:text>nav-current</xsl:text>
            </xsl:attribute>
          </xsl:if>

          <xsl:apply-templates select="." mode="link-overload"/>
        </li>
      </xsl:when>
      <xsl:when test="not(preceding-sibling::property[@name = current()/@name]) and not(following-sibling::property[@name = current()/@name])" >
        <li>
          <xsl:if test="$current = @assetId">
            <xsl:attribute name="class">
              <xsl:text>nav-current</xsl:text>
            </xsl:attribute>
          </xsl:if>
          <xsl:apply-templates select="." mode="link">
            <xsl:with-param name="includeNoun" select="true()" />
          </xsl:apply-templates>
        </li>
      </xsl:when>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="operator" mode="xnav-item">
    <xsl:param name="current"/>
    <xsl:choose>
      <xsl:when test="not(preceding-sibling::operator[@name = current()/@name]) and following-sibling::operator[@name = current()/@name]">
        <li>
          <xsl:if test="$current = @assetId">
            <xsl:attribute name="class">
              <xsl:text>nav-current</xsl:text>
            </xsl:attribute>
          </xsl:if>
          
          <xsl:apply-templates select="." mode="link-overload" />
        </li>
      </xsl:when>
      <xsl:when test="not(preceding-sibling::operator[@name = current()/@name]) and not(following-sibling::operator[@name = current()/@name])" >
        <li>
          <xsl:if test="$current = @assetId">
            <xsl:attribute name="class">
              <xsl:text>nav-current</xsl:text>
            </xsl:attribute>
          </xsl:if>
          <xsl:apply-templates select="." mode="link">
            <xsl:with-param name="includeNoun" select="true()" />
          </xsl:apply-templates>
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

      <xsl:apply-templates select="." mode="link">
        <xsl:with-param name="includeNoun" select="true()" />
      </xsl:apply-templates>
    </li>
  </xsl:template>
</xsl:stylesheet>
