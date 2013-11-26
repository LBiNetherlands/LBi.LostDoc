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


  <xsl:template match="enum" mode="syntax-cs-naming">
    <xsl:value-of select="@name"/>
  </xsl:template>

  <xsl:template match="property | field" mode="syntax-cs-naming">
    <xsl:value-of select="@name"/>
  </xsl:template>

  <xsl:template match="class | interface | struct" mode="syntax-cs-naming">
    <xsl:param name="typeargs" select="false()"/>
    <xsl:param name="attributes" select="false()"/>

    <xsl:if test="position() > 1">
      <xsl:text>, </xsl:text>
    </xsl:if>
    <xsl:choose>
      <xsl:when test="ld:asset(@assetId) = 'T:System.Object' and $attributes and $attributes[ld:asset(@type) = 'T:System.Runtime.CompilerServices.DynamicAttribute']">
        <span class="keyword">
          <xsl:text>dynamic</xsl:text>
        </span>
      </xsl:when>
      <xsl:when test="ld:asset(@assetId) = 'T:System.Object'">
        <span class="keyword">
          <xsl:text>object</xsl:text>
        </span>
      </xsl:when>
      <xsl:when test="ld:asset(@assetId) = 'T:System.String'">
        <span class="keyword">
          <xsl:text>string</xsl:text>
        </span>
      </xsl:when>
      <xsl:when test="ld:asset(@assetId) = 'T:System.Boolean'">
        <span class="keyword">
          <xsl:text>bool</xsl:text>
        </span>
      </xsl:when>
      <xsl:when test="ld:asset(@assetId) = 'T:System.Int32'">
        <span class="keyword">
          <xsl:text>int</xsl:text>
        </span>
      </xsl:when>
      <xsl:when test="ld:asset(@assetId) = 'T:System.Int64'">
        <span class="keyword">
          <xsl:text>long</xsl:text>
        </span>
      </xsl:when>
      <xsl:when test="ld:asset(@assetId) = 'T:System.Int16'">
        <span class="keyword">
          <xsl:text>short</xsl:text>
        </span>
      </xsl:when>
      <xsl:when test="ld:asset(@assetId) = 'T:System.UInt32'">
        <span class="keyword">
          <xsl:text>uint</xsl:text>
        </span>
      </xsl:when>
      <xsl:when test="ld:asset(@assetId) = 'T:System.UInt64'">
        <span class="keyword">
          <xsl:text>ulong</xsl:text>
        </span>
      </xsl:when>
      <xsl:when test="ld:asset(@assetId) = 'T:System.UInt16'">
        <span class="keyword">
          <xsl:text>ushort</xsl:text>
        </span>
      </xsl:when>
      <xsl:when test="ld:asset(@assetId) = 'T:System.Byte'">
        <span class="keyword">
          <xsl:text>byte</xsl:text>
        </span>
      </xsl:when>
      <xsl:when test="ld:asset(@assetId) = 'T:System.SByte'">
        <span class="keyword">
          <xsl:text>sbyte</xsl:text>
        </span>
      </xsl:when>
      <xsl:when test="ld:asset(@assetId) = 'T:System.Decimal'">
        <span class="keyword">
          <xsl:text>decimal</xsl:text>
        </span>
      </xsl:when>
      <xsl:when test="ld:asset(@assetId) = 'T:System.Single'">
        <span class="keyword">
          <xsl:text>float</xsl:text>
        </span>
      </xsl:when>
      <xsl:when test="ld:asset(@assetId) = 'T:System.Double'">
        <span class="keyword">
          <xsl:text>double</xsl:text>
        </span>
      </xsl:when>
      <xsl:when test="$typeargs">
        <xsl:value-of select="substring-before(@name, '`')"/>
        <xsl:text>&lt;</xsl:text>
        <xsl:apply-templates select="$typeargs" mode="syntax-cs-naming"/>
        <xsl:text>&gt;</xsl:text>
      </xsl:when>
      <xsl:when test="typeparam">
        <xsl:value-of select="substring-before(@name, '`')"/>
        <xsl:text>&lt;</xsl:text>
        <xsl:apply-templates select="typeparam" mode="syntax-cs-naming"/>
        <xsl:text>&gt;</xsl:text>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="@name"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="@*" mode="syntax-cs-naming">
    <xsl:param name="attributes" select="false()"/>
    <xsl:choose>
      <xsl:when test="name() = 'param'">
        <xsl:value-of select="."/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:choose>
          <xsl:when test="parent::*/with">
            <xsl:value-of select="substring-before(ld:key('aid', current())/@name, '`')"/>
            <xsl:text>&lt;</xsl:text>
            <xsl:apply-templates select="parent::*/with" mode="syntax-cs-naming"/>
            <xsl:text>&gt;</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:apply-templates select="ld:key('aid', current())" mode="syntax-cs-naming">
              <xsl:with-param name="attributes" select="$attributes" />
            </xsl:apply-templates>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="with" mode="syntax-cs-naming">
    <xsl:if test="position() > 1">
      <xsl:text>, </xsl:text>
    </xsl:if>
    <xsl:apply-templates select="@type | @param" mode="syntax-cs-naming"/>
  </xsl:template>

  <xsl:template match="typeparam" mode="syntax-cs-naming">
    <xsl:if test="position() > 1">
      <xsl:text>, </xsl:text>
    </xsl:if>
    <xsl:value-of select="@name"/>
  </xsl:template>


  <xsl:template match="enum" mode="syntax-cs">
    <xsl:if test="attribute">
      <xsl:apply-templates select="attribute" mode="syntax-cs"/>
    </xsl:if>

    <span class="line">
      <xsl:apply-templates select="." mode="syntax-cs-access"/>
      <span class="keyword">
        <xsl:text> enum </xsl:text>
      </span>
      <xsl:apply-templates select="." mode="syntax-cs-naming"/>
      <xsl:text> : </xsl:text>
      <xsl:choose>
        <xsl:when test="ld:asset(@underlyingType) = 'T:System.SByte'">
          <span class="keyword">
            <xsl:text>sbyte</xsl:text>
          </span>
        </xsl:when>
        <xsl:when test="ld:asset(@underlyingType) = 'T:System.Int16'">
          <span class="keyword">
            <xsl:text>short</xsl:text>
          </span>
        </xsl:when>
        <xsl:when test="ld:asset(@underlyingType) = 'T:System.Int32'">
          <span class="keyword">
            <xsl:text>int</xsl:text>
          </span>
        </xsl:when>
        <xsl:when test="ld:asset(@underlyingType) = 'T:System.Int64'">
          <span class="keyword">
            <xsl:text>long</xsl:text>
          </span>
        </xsl:when>
        <xsl:when test="ld:asset(@underlyingType) = 'T:System.Byte'">
          <span class="keyword">
            <xsl:text>byte</xsl:text>
          </span>
        </xsl:when>
        <xsl:when test="ld:asset(@underlyingType) = 'T:System.UInt16'">
          <span class="keyword">
            <xsl:text>ushort</xsl:text>
          </span>
        </xsl:when>
        <xsl:when test="ld:asset(@underlyingType) = 'T:System.UInt32'">
          <span class="keyword">
            <xsl:text>uint</xsl:text>
          </span>
        </xsl:when>
        <xsl:when test="ld:asset(@underlyingType) = 'T:System.UInt64'">
          <span class="keyword">
            <xsl:text>ulong</xsl:text>
          </span>
        </xsl:when>
      </xsl:choose>
    </span>
  </xsl:template>

  <xsl:template match="class | interface | struct" mode="syntax-cs">

    <xsl:if test="attribute">
      <span class="line">
        <xsl:apply-templates select="attribute" mode="syntax-cs"/>
      </span>
    </xsl:if>

    <span class="line">
      <xsl:apply-templates select="." mode="syntax-cs-access"/>
      <xsl:text> </xsl:text>
      <xsl:choose>
        <xsl:when test="self::class">
          <span class="keyword">class</span>
        </xsl:when>
        <xsl:when test="self::interface">
          <span class="keyword">interface</span>
        </xsl:when>
        <xsl:when test="self::struct">
          <span class="keyword">struct</span>
        </xsl:when>
      </xsl:choose>

      <xsl:text> </xsl:text>
      <xsl:apply-templates select="." mode="syntax-cs-naming"/>
      <xsl:if test="(inherits and ld:asset(inherits/@type) != 'T:System.Object') or implements">
        <xsl:text> : </xsl:text>
      </xsl:if>
      <xsl:if test="inherits and ld:asset(inherits/@type) != 'T:System.Object'">
        <xsl:choose>
          <xsl:when test="inherits/with">
            <!--<xsl:apply-templates select="/bundle/assembly/namespace//*[@assetId = current()/inherits/@type]" mode="syntax-cs-naming">-->
            <xsl:apply-templates select="ld:key('aid', current()/inherits/@type)" mode="syntax-cs-naming">
              <xsl:with-param name="typeargs" select="inherits/with"/>
            </xsl:apply-templates>
          </xsl:when>
          <xsl:otherwise>
            <!--<xsl:apply-templates select="//bundle/assembly/namespace//*[@assetId = current()/inherits/@type]" mode="syntax-cs-naming"/>-->
            <xsl:apply-templates select="ld:key('aid', current()/inherits/@type)" mode="syntax-cs-naming"/>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:if test="implements">
          <xsl:text>, </xsl:text>
        </xsl:if>
      </xsl:if>
      <xsl:if test="implements">
        <xsl:apply-templates select="implements" mode="syntax-cs-naming"/>
      </xsl:if>
    </span>

    <xsl:apply-templates select="typeparam" mode="syntax-cs-generic-constraints"/>
  </xsl:template>

  <!-- generic type parameter constraints -->

  <xsl:template match="typeparam" mode="syntax-cs-generic-constraints">
    <xsl:variable name="constraints" select="constraint[not(@type) or ld:asset(@type) != 'T:System.ValueType']|@isValueType|@isReferenceType|@hasDefaultConstructor[not(../@isValueType)]" />

    <xsl:if test="$constraints">
      <span class="line">
        <span class="keyword">
          <xsl:text>where </xsl:text>
        </span>
        <xsl:value-of select="@name"/>
        <xsl:text> : </xsl:text>
        <xsl:apply-templates select="$constraints" mode="syntax-cs-generic-constraint-spec"/>
      </span>
    </xsl:if>
  </xsl:template>

  <xsl:template match="@isValueType" mode="syntax-cs-generic-constraint-spec">
    <xsl:if test="position() > 1">
      <xsl:text>, </xsl:text>
    </xsl:if>

    <span class="keyword">
      <xsl:text>struct</xsl:text>
    </span>
  </xsl:template>

  <xsl:template match="@isReferenceType" mode="syntax-cs-generic-constraint-spec">
    <xsl:if test="position() > 1">
      <xsl:text>, </xsl:text>
    </xsl:if>

    <span class="keyword">
      <xsl:text>class</xsl:text>
    </span>
  </xsl:template>

  <xsl:template match="@hasDefaultConstructor" mode="syntax-cs-generic-constraint-spec">
    <xsl:if test="position() > 1">
      <xsl:text>, </xsl:text>
    </xsl:if>

    <span class="keyword">
      <xsl:text>new()</xsl:text>
    </span>
  </xsl:template>

  <xsl:template match="constraint" mode="syntax-cs-generic-constraint-spec">
    <xsl:if test="position() > 1">
      <xsl:text>, </xsl:text>
    </xsl:if>

    <xsl:apply-templates select="@type | @param | arrayOf" mode="syntax-cs-naming"/>
  </xsl:template>

  <!-- //generic type parameter constraints -->

  <xsl:template match="implements" mode="syntax-cs-naming">
    <xsl:if test="position() > 1">
      <xsl:text>, </xsl:text>
    </xsl:if>
    <xsl:choose>
      <xsl:when test="with">
        <!--<xsl:apply-templates select="//bundle/assembly/namespace//*[@assetId = current()/@interface]" mode="syntax-cs-naming">-->
        <xsl:apply-templates select="ld:key('aid', current()/@interface)" mode="syntax-cs-naming">
          <xsl:with-param name="typeargs" select="with"/>
        </xsl:apply-templates>
      </xsl:when>
      <xsl:otherwise>
        <!--<xsl:apply-templates select="//bundle/assembly/namespace//*[@assetId = current()/@interface]" mode="syntax-cs-naming"/>-->
        <xsl:apply-templates select="ld:key('aid', current()/@interface)" mode="syntax-cs-naming"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="*" mode="syntax-cs-access">
    <xsl:variable name="keyword">
      <xsl:choose>
        <!-- explicit interface implementation -->
        <xsl:when test="@isPrivate and @isSealed and implements" />
        <xsl:when test="@isPublic = 'true'">
          <xsl:text>public </xsl:text>
        </xsl:when>
        <xsl:when test="@isProtected = 'true'">
          <xsl:text>protected </xsl:text>
        </xsl:when>
        <xsl:when test="@isPrivate = 'true'">
          <xsl:text>private </xsl:text>
        </xsl:when>
        <xsl:when test="@isProtectedOrInternal = 'true'">
          <xsl:text>protected internal </xsl:text>
        </xsl:when>
        <xsl:when test="@isProtectedAndInternal = 'true'">
          <xsl:text>[NOT SUPPORTED IN C#] </xsl:text>
        </xsl:when>
        <xsl:when test="@isInternal = 'true'">
          <xsl:text>internal </xsl:text>
        </xsl:when>
      </xsl:choose>
      <xsl:choose>
        <!-- explicit interface implementation -->
        <xsl:when test="@isPrivate and @isSealed and implements" />
        <xsl:when test="@isSealed = 'true' and @isAbstract = 'true'">
          <xsl:text>static </xsl:text>
        </xsl:when>
        <xsl:when test="@isSealed = 'true'">
          <xsl:text>sealed </xsl:text>
        </xsl:when>
        <xsl:when test="@isStatic = 'true'">
          <xsl:text>static </xsl:text>
        </xsl:when>
        <xsl:when test="@isAbstract = 'true'">
          <xsl:text>abstract </xsl:text>
        </xsl:when>
        <xsl:when test="@isVirtual = 'true'">
          <xsl:text>virtual </xsl:text>
        </xsl:when>
      </xsl:choose>
    </xsl:variable>
    <xsl:if test="$keyword != ''">
      <span class="keyword">
        <xsl:value-of select="$keyword"/>
      </span>
    </xsl:if>
  </xsl:template>

  <xsl:template match="operator" mode="syntax-cs">
    <xsl:if test="attribute">
      <xsl:apply-templates select="attribute" mode="syntax-cs"/>
    </xsl:if>
    <span class="line">
      <xsl:apply-templates select="returns/attribute" mode="syntax-cs">
        <xsl:with-param name="prefix" select="'return'"/>
      </xsl:apply-templates>

      <xsl:apply-templates select="." mode="syntax-cs-access"/>

      <xsl:apply-templates select="returns/@type | returns/@param | returns/arrayOf" mode="syntax-cs-naming">
        <xsl:with-param name="attributes" select="returns/attribute"/>
      </xsl:apply-templates>

      <xsl:choose>
        <xsl:when test="@name = 'op_Implicit'">
          <span class="keyword">
            <xsl:text> implicit</xsl:text>
          </span>
          <span class="keyword">
            <xsl:text> operator </xsl:text>
          </span>
        </xsl:when>
        <xsl:when test="@name = 'op_Explicit'">
          <span class="keyword">
            <xsl:text> explicit</xsl:text>
          </span>
          <span class="keyword">
            <xsl:text> operator </xsl:text>
          </span>
        </xsl:when>
        <xsl:otherwise>
          <span class="keyword">
            <xsl:text> operator </xsl:text>
          </span>
          <xsl:choose>
            <xsl:when test="@name = 'op_Addition'">
              <xsl:text>+</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_Subtraction'">
              <xsl:text>-</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_Multiply'">
              <xsl:text>*</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_Division'">
              <xsl:text>/</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_Modulus'">
              <xsl:text>%</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_ExclusiveOr'">
              <xsl:text>^</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_BitwiseAnd'">
              <xsl:text>&amp;</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_BitwiseOr'">
              <xsl:text>|</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_LogicalAnd'">
              <xsl:text>&amp;&amp;</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_LogicalOr'">
              <xsl:text>||</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_Assign'">
              <xsl:text>=</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_LeftShift'">
              <xsl:text>&lt;&lt;</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_RightShift'">
              <xsl:text>&gt;&gt;</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_SignedRightShift'">
              <xsl:text>&gt;&gt;</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_UnsignedRightShift'">
              <xsl:text>&gt;&gt;</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_Equality'">
              <xsl:text>==</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_GreaterThan'">
              <xsl:text>&gt;</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_LessThan'">
              <xsl:text>&lt;</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_Inequality'">
              <xsl:text>!=</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_GreaterThanOrEqual'">
              <xsl:text>&gt;=</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_LessThanOrEqual'">
              <xsl:text>&lt;=</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_MultiplicationAssignment'">
              <xsl:text>*=</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_SubtractionAssignment'">
              <xsl:text>-=</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_ExclusiveOrAssignment'">
              <xsl:text>^=</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_LeftShiftAssignment'">
              <xsl:text>&lt;&lt;</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_ModulusAssignment'">
              <xsl:text>%=</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_AdditionAssignment'">
              <xsl:text>+=</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_BitwiseAndAssignment'">
              <xsl:text>&amp;=</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_BitwiseOrAssignment'">
              <xsl:text>|=</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_Comma'">
              <xsl:text>,</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_DivisionAssignment'">
              <xsl:text>/=</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_Decrement'">
              <xsl:text>--</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_Increment'">
              <xsl:text>++</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_UnaryNegation'">
              <xsl:text>-</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_UnaryPlus'">
              <xsl:text>+</xsl:text>
            </xsl:when>
            <xsl:when test="@name = 'op_OnesComplement'">
              <xsl:text>~</xsl:text>
            </xsl:when>
          </xsl:choose>
        </xsl:otherwise>
      </xsl:choose>
      <xsl:text>(</xsl:text>
      <xsl:if test="not(param)">
        <xsl:text>)</xsl:text>
      </xsl:if>
    </span>
    <xsl:if test="param">
      <xsl:apply-templates select="param" mode="syntax-cs-naming"/>
      <span class="line">
        <xsl:text>)</xsl:text>
      </span>
    </xsl:if>
  </xsl:template>

  <xsl:template match="method" mode="syntax-cs">
    <xsl:if test="attribute">
      <xsl:apply-templates select="attribute" mode="syntax-cs"/>
    </xsl:if>
    <xsl:apply-templates select="returns/attribute" mode="syntax-cs">
      <xsl:with-param name="prefix" select="'return'"/>
    </xsl:apply-templates>
    <span class="line">
      <xsl:apply-templates select="." mode="syntax-cs-access"/>
      <xsl:choose>
        <xsl:when test="returns">
          <xsl:apply-templates select="returns/@type | returns/@param" mode="syntax-cs-naming">
            <xsl:with-param name="attributes" select="returns/attribute"/>
          </xsl:apply-templates>
        </xsl:when>
        <xsl:otherwise>
          <span class="keyword">void</span>
        </xsl:otherwise>
      </xsl:choose>
      <xsl:text> </xsl:text>
      <xsl:choose>
        <xsl:when test="@isPrivate and @isSealed and implements">
          <xsl:apply-templates select="ld:key('aid', implements/@member)/parent::interface" mode="syntax-cs-naming"/>
          <xsl:text>.</xsl:text>
          <xsl:value-of select="ld:substringAfterLast(@name, '.')"/>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="@name"/>
        </xsl:otherwise>
      </xsl:choose>
      <xsl:if test="typeparam">
        <xsl:text>&lt;</xsl:text>
        <xsl:apply-templates select="typeparam" mode="syntax-cs-naming"/>
        <xsl:text>&gt;</xsl:text>
      </xsl:if>
      <xsl:text>(</xsl:text>
      <xsl:if test="not(param)">
        <xsl:text>)</xsl:text>
      </xsl:if>
    </span>
    <xsl:if test="param">
      <xsl:apply-templates select="param" mode="syntax-cs-naming"/>
      <span class="line">
        <xsl:text>)</xsl:text>
      </span>
    </xsl:if>

    <xsl:apply-templates select="typeparam" mode="syntax-cs-generic-constraints"/>
  </xsl:template>


  <xsl:template match="constructor" mode="syntax-cs">
    <xsl:if test="attribute">
      <xsl:apply-templates select="attribute" mode="syntax-cs"/>
    </xsl:if>
    <span class="line">
      <xsl:apply-templates select="." mode="syntax-cs-access"/>
      <xsl:text> </xsl:text>
      <xsl:value-of select="substring-before(parent::*/@name, '`')"/>
      <xsl:if test="not(contains(parent::*/@name, '`'))">
        <xsl:value-of select="parent::*/@name"/>
      </xsl:if>
      <xsl:text>(</xsl:text>
      <xsl:if test="not(param)">
        <xsl:text>)</xsl:text>
      </xsl:if>
    </span>
    <xsl:if test="param">
      <xsl:apply-templates select="param" mode="syntax-cs-naming"/>
      <xsl:text>)</xsl:text>
    </xsl:if>
  </xsl:template>

  <xsl:template match="property" mode="syntax-cs">
    <xsl:if test="attribute">
      <xsl:apply-templates select="attribute" mode="syntax-cs"/>
    </xsl:if>
    <span class="line">
      <xsl:apply-templates select="." mode="syntax-cs-access"/>
      <xsl:apply-templates select="@type | @param" mode="syntax-cs-naming">
        <xsl:with-param name="attributes" select="attribute" />
      </xsl:apply-templates>
      <xsl:text> </xsl:text>
      <xsl:choose>
        <xsl:when test="../attribute[ld:asset(@type) = 'T:System.Reflection.DefaultMemberAttribute']/argument/@value = @name">
          <span class="keyword">
            <xsl:text>this</xsl:text>
          </span>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="@name"/>
        </xsl:otherwise>
      </xsl:choose>
      <xsl:if test="param">
        <xsl:text>[</xsl:text>
      </xsl:if>
      <xsl:if test="param">
        <xsl:apply-templates select="param" mode="syntax-cs-naming"/>
        <xsl:text>]</xsl:text>
      </xsl:if>
      <xsl:text> { </xsl:text>
      <xsl:if test="get">
        <xsl:apply-templates select="get" mode="syntax-cs-access"/>
        <xsl:text>get; </xsl:text>
      </xsl:if>
      <xsl:if test="set">
        <xsl:apply-templates select="set" mode="syntax-cs-access"/>
        <xsl:text>set; </xsl:text>
      </xsl:if>
      <xsl:text> }</xsl:text>
    </span>
  </xsl:template>


  <xsl:template match="field" mode="syntax-cs">
    <xsl:if test="attribute">
      <xsl:apply-templates select="attribute" mode="syntax-cs"/>
    </xsl:if>
    <span class="line">
      <xsl:apply-templates select="." mode="syntax-cs-access"/>
      <xsl:apply-templates select="@type | @param | arrayOf" mode="syntax-cs-naming"/>
      <xsl:text> </xsl:text>
      <xsl:value-of select="@name"/>
    </span>
  </xsl:template>


  <xsl:template match="param" mode="syntax-cs-naming">
    <span class="line indent">
      <xsl:if test="attribute">
        <xsl:apply-templates select="attribute" mode="syntax-cs"/>
      </xsl:if>
      <xsl:if test="@isOut">
        <span class="keyword">out </span>
      </xsl:if>
      <xsl:if test="@isRef">
        <span class="keyword">ref </span>
      </xsl:if>
      <xsl:if test="position() = 1 and parent::method/attribute and ld:asset(parent::method/attribute/@type) = 'T:System.Runtime.CompilerServices.ExtensionAttribute'">
        <span class="keyword">this </span>
      </xsl:if>
      <xsl:apply-templates select="@type | @param | arrayOf" mode="syntax-cs-naming">
        <xsl:with-param name="attributes" select="attribute" />
      </xsl:apply-templates>
      <xsl:text> </xsl:text>
      <xsl:value-of select="@name"/>
      <xsl:if test="position() != last()">
        <xsl:text>,</xsl:text>
      </xsl:if>
    </span>
  </xsl:template>


  <xsl:template match="arrayOf" mode="syntax-cs-naming">
    <xsl:apply-templates select="@type | @param  | arrayOf" mode="syntax-cs-naming"/>
    <xsl:text>[</xsl:text>
    <xsl:call-template name="array-rank-cs">
      <xsl:with-param name="rank" select="@rank" />
    </xsl:call-template>
    <xsl:text>]</xsl:text>
  </xsl:template>

  <xsl:template name="array-rank-cs">
    <xsl:param name="rank"/>
    <xsl:if test=" $rank &gt; 1">
      <xsl:text>,</xsl:text>
      <xsl:call-template name="array-rank-cs">
        <xsl:with-param name="rank" select=" $rank - 1 "/>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>

  <!-- attribute support -->

  <xsl:template match="attribute" mode="syntax-cs">
    <xsl:param name="prefix" select="''"/>

    <!-- skip the DynamicAttribute -->
    <xsl:if test="ld:asset(@type) != 'T:System.Runtime.CompilerServices.DynamicAttribute'">
      <!-- skip certain attributes -->
      <xsl:if test="ld:asset(@type) != 'T:System.Reflection.DefaultMemberAttribute' and ld:asset(@type) != 'T:System.Runtime.CompilerServices.ExtensionAttribute'">
        <span class="line">
          <xsl:text>[</xsl:text>
          <xsl:if test="$prefix">
            <span class="keyword">
              <xsl:value-of select="$prefix"/>
            </span>
            <xsl:text>: </xsl:text>
          </xsl:if>
          <xsl:variable name="name" select="/bundle/assembly/namespace//class[constructor/@assetId = current()/@constructor]/@name"/>
          <xsl:value-of select="ld:substringBeforeLast($name, 'Attribute')"/>
          <xsl:if test="not(contains($name, 'Attribute'))">
            <xsl:value-of select="$name"/>
          </xsl:if>
          <xsl:if test="argument">
            <xsl:text>(</xsl:text>
            <xsl:apply-templates select="argument" mode="syntax-cs-literal"/>
            <xsl:text>)</xsl:text>
          </xsl:if>
          <xsl:text>]</xsl:text>
        </span>
      </xsl:if>
    </xsl:if>
  </xsl:template>

  <xsl:template match="argument" mode="syntax-cs-literal">
    <xsl:if test="position() > 1">
      <xsl:text>, </xsl:text>
    </xsl:if>
    <xsl:if test="@member">
      <!--<xsl:apply-templates select="/bundle/assembly/namespace//*[@assetId = current()/@member]" mode="syntax-cs-naming"/>-->
      <xsl:apply-templates select="ld:key('aid', current()/@member)" mode="syntax-cs-naming"/>
      <xsl:text> = </xsl:text>
    </xsl:if>
    <xsl:apply-templates select="*" mode="syntax-cs-literal"/>
  </xsl:template>



  <!-- literal strings -->

  <xsl:template match="constant[ld:asset(@type) = 'T:System.String' and @value]" mode="syntax-cs-literal">
    <xsl:text>"</xsl:text>
    <xsl:value-of select="ld:replace(ld:replace(ld:replace(@value, '&#x9;', '\t'), '&#xA;', '\n'), '&#xD;', '\r')"/>
    <xsl:text>"</xsl:text>
  </xsl:template>

  <xsl:template match="constant[ld:asset(@type) = 'T:System.String' and not(@value)]" mode="syntax-cs-literal">
    <xsl:text>"</xsl:text>
    <xsl:apply-templates select="*|text()" mode="syntax-cs-literal-string"/>
    <xsl:text>"</xsl:text>
  </xsl:template>

  <xsl:template match="char[@value = '0']" mode="syntax-cs-literal-string">
    <xsl:text>\0</xsl:text>
  </xsl:template>

  <xsl:template match="char" mode="syntax-cs-literal-string">
    <xsl:text>\u</xsl:text>
    <xsl:variable name="hex">
      <xsl:call-template name="convert-decimal-to-hex">
        <xsl:with-param name="num" select="@value"/>
      </xsl:call-template>
    </xsl:variable>
    <xsl:value-of select="substring(concat('0000', @value), string-length($hex) + 1)"/>
  </xsl:template>

  <!-- TODO figure out how to deal with tab, newlines, and other entities -->
  <xsl:template match="text()" mode="syntax-cs-literal-string">
    <xsl:value-of select="ld:replace(ld:replace(ld:replace(., '&#x9;', '\t'), '&#xA;', '\n'), '&#xD;', '\r')"/>
  </xsl:template>

  <xsl:template name="convert-decimal-to-hex">
    <xsl:param name="num" />
    <xsl:if test="$num > 0">
      <xsl:call-template name="convert-decimal-to-hex">
        <xsl:with-param name="num" select="floor($num div 16)" />
      </xsl:call-template>
      <xsl:choose>
        <xsl:when test="$num mod 16 = 10">A</xsl:when>
        <xsl:when test="$num mod 16 = 11">B</xsl:when>
        <xsl:when test="$num mod 16 = 12">C</xsl:when>
        <xsl:when test="$num mod 16 = 13">D</xsl:when>
        <xsl:when test="$num mod 16 = 14">E</xsl:when>
        <xsl:when test="$num mod 16 = 15">F</xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="$num mod 16" />
        </xsl:otherwise>
      </xsl:choose>
    </xsl:if>
  </xsl:template>

  <!-- //literal strings -->

  <!-- literal boolean -->

  <xsl:template match="constant[ld:asset(@type) = 'T:System.Boolean' and @value]" mode="syntax-cs-literal">
    <span class="keyword">
      <xsl:text>true</xsl:text>
    </span>
  </xsl:template>

  <xsl:template match="constant[ld:asset(@type) = 'T:System.Boolean']" mode="syntax-cs-literal">
    <span class="keyword">
      <xsl:text>false</xsl:text>
    </span>
  </xsl:template>

  <!-- //literal boolean -->

  <!-- literal char -->

  <!-- TODO this isn't really good enough for null chars etc-->
  <xsl:template match="constant[ld:asset(@type) = 'T:System.Char']" mode="syntax-cs-literal">
    <xsl:text>'</xsl:text>
    <xsl:value-of select="@value"/>
    <xsl:text>'</xsl:text>
  </xsl:template>

  <!-- //literal char-->


  <!-- literal decimal -->

  <xsl:template match="constant[ld:asset(@type) = 'T:System.Decimal']" mode="syntax-cs-literal">
    <xsl:value-of select="@value"/>
    <xsl:text>m</xsl:text>
  </xsl:template>

  <!-- //literal decimal-->

  <!-- literal numbers -->

  <!-- TODO expand this for byte,sbyte,short,ushort,int,uint,long,ulong-->
  <xsl:template match="constant" mode="syntax-cs-literal">
    <xsl:value-of select="@value"/>
  </xsl:template>

  <!-- //literal numbers-->

  <!-- literal type -->

  <xsl:template match="typeRef" mode="syntax-cs-literal">
    <span class="keyword">
      <xsl:text>typeof</xsl:text>
    </span>
    <xsl:text>(</xsl:text>
    <xsl:apply-templates select="@type | arrayOf" mode="syntax-cs-naming"/>
    <xsl:text>)</xsl:text>
  </xsl:template>

  <!-- //literal type-->

  <!-- literal type -->

  <xsl:template match="arrayOf" mode="syntax-cs-literal">
    <span class="keyword">
      <xsl:text>new</xsl:text>
    </span>
    <xsl:text> </xsl:text>
    <xsl:apply-templates select="@type | arrayOf" mode="syntax-cs-naming"/>
    <xsl:text>[] { </xsl:text>
    <xsl:apply-templates select="element" mode="syntax-cs-literal"/>
    <xsl:text> }</xsl:text>
  </xsl:template>

  <xsl:template match="element" mode="syntax-cs-literal">
    <xsl:if test="position() &gt; 1">
      <xsl:text>, </xsl:text>
    </xsl:if>
    <xsl:apply-templates select="*" mode="syntax-cs-literal"/>
  </xsl:template>

  <!-- //literal type-->


  <!--<xsl:template match="flag" mode="syntax-cs-literal">
    <xsl:if test="position() > 1">
      <xsl:text> | </xsl:text>
    </xsl:if>
    <xsl:value-of select="ld:key('aid', current()/ancestor::argument/@type)/@name"/>
    <xsl:text>.</xsl:text>
    <xsl:value-of select="@value"/>
  </xsl:template>-->
</xsl:stylesheet>
