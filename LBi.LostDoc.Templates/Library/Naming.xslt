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
                xmlns:ld="urn:lostdoc:template"
                exclude-result-prefixes="msxsl">


  <xsl:template match="*" mode="nounSingular">
    <xsl:variable name="type" select="name(.)"/>
    <xsl:choose>
      <xsl:when test="$type = 'class'">
        <xsl:text>Class</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'struct'">
        <xsl:text>Structure</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'interface'">
        <xsl:text>Interface</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'delegate'">
        <xsl:text>Delegate</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'enum'">
        <xsl:text>Enumeration</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'namespace'">
        <xsl:text>Namespace</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'property'">
        <xsl:text>Property</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'operator' and (@name = 'op_Explicit' or @name = 'op_Implicit')">
        <xsl:text>Conversion</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'operator'">
        <xsl:text>Operator</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'method'">
        <xsl:text>Method</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'constructor'">
        <xsl:text>Constructor</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'field'">
        <xsl:text>Field</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'assembly'">
        <xsl:text>Assembly</xsl:text>
      </xsl:when>
      <xsl:otherwise>
        <xsl:text> Unknown type: </xsl:text>
        <xsl:value-of select="$type"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="*" mode="nounPlural">
    <xsl:variable name="type" select="name(.)"/>
    <xsl:choose>
      <xsl:when test="$type = 'class'">
        <xsl:text>Classes</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'struct'">
        <xsl:text>Structures</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'interface'">
        <xsl:text>Interfaces</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'delegate'">
        <xsl:text>Delegates</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'enum'">
        <xsl:text>Enumerations</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'namespace'">
        <xsl:text>Namespaces</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'property'">
        <xsl:text>Properties</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'operator' and (@name = 'op_Explicit' or @name = 'op_Implicit')">
        <xsl:text>Conversions</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'operator'">
        <xsl:text>Operators</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'method'">
        <xsl:text>Methods</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'constructor'">
        <xsl:text>Constructors</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'field'">
        <xsl:text>Fields</xsl:text>
      </xsl:when>
      <xsl:when test="$type = 'assembly'">
        <xsl:text>Assemblies</xsl:text>
      </xsl:when>
      <xsl:otherwise>
        <xsl:text> Unknown type: </xsl:text>
        <xsl:value-of select="$type"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="*" mode="title">
    <xsl:apply-templates select="." mode="displayText"/>
    <xsl:text> </xsl:text>
    <xsl:apply-templates select="." mode="nounSingular"/>
  </xsl:template>

  <xsl:template match="operator[@name = 'op_Implicit' or @name = 'op_Explicit']" mode="title">
    <xsl:apply-templates select="." mode="displayText"/>
  </xsl:template>

  <xsl:template match="@*" mode="displayText">
    <xsl:param name="attributes" select="false()" />
    <xsl:choose>
      <xsl:when test="name() = 'param'">
        <xsl:value-of select="."/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:choose>
          <xsl:when test="parent::*/with">
            <xsl:value-of select="substring-before(ld:key('aid', current())/@name, '`')"/>
            <xsl:text>&lt;</xsl:text>
            <xsl:apply-templates select="parent::*/with" mode="displayText"/>
            <xsl:text>&gt;</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:apply-templates select="ld:key('aid', current())" mode="displayText">
              <xsl:with-param name="attributes" select="$attributes" />
            </xsl:apply-templates>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="with" mode="displayText">
    <xsl:if test="position() > 1">
      <xsl:text>, </xsl:text>
    </xsl:if>
    <xsl:apply-templates select="@type | @param" mode="displayText"/>
  </xsl:template>

  <xsl:template match="constructor" mode="displayText">
    <xsl:choose>
      <xsl:when test="../typeparam">
        <xsl:value-of select="substring-before(../@name, '`')"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="../@name"/>
      </xsl:otherwise>
    </xsl:choose>
    <xsl:if test="param">
      <xsl:text>(</xsl:text>
      <xsl:apply-templates select="param" mode="displayText"/>
      <xsl:text>)</xsl:text>
    </xsl:if>
  </xsl:template>


  <xsl:template match="assembly" mode="displayText">
    <xsl:value-of select="@name"/>
  </xsl:template>

  <xsl:template match="namespace" mode="displayText">
    <xsl:value-of select="@name"/>
    <xsl:if test="@name = ''">
      <xsl:text>global::</xsl:text>
    </xsl:if>
  </xsl:template>

  <!-- operators -->
  <xsl:template match="operator[@name = 'op_Implicit' or @name = 'op_Explicit']" mode="displayText">
    <xsl:value-of select="substring-after(@name, 'op_')"/>
    <xsl:text> Conversion </xsl:text>
    <xsl:choose>
      <xsl:when test="param/@type = parent::*/@assetId">
        <xsl:text>to </xsl:text>
        <xsl:apply-templates select="ld:key('aid', current()/returns/@type)" mode="displayText" />
      </xsl:when>
      <xsl:otherwise>
        <xsl:text>from </xsl:text>
        <xsl:apply-templates select="ld:key('aid', current()/param/@type)" mode="displayText" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="operator" mode="displayText">
    <xsl:value-of select="substring-after(@name, 'op_')"/>
  </xsl:template>

  <xsl:template match="method" mode="displayText">
    <xsl:value-of select="ld:substringAfterLast(@name, '.')"/>
    <xsl:if test="not(@isPrivate and @isSealed and implements)">
      <xsl:value-of select="@name" />
    </xsl:if>
    <xsl:if test="typeparam">
      <xsl:text>&lt;</xsl:text>
      <xsl:apply-templates select="typeparam" mode="displayText"/>
      <xsl:text>&gt;</xsl:text>
    </xsl:if>
    <xsl:if test="param">
      <xsl:text>(</xsl:text>
      <xsl:apply-templates select="param" mode="displayText"/>
      <xsl:text>)</xsl:text>
    </xsl:if>
  </xsl:template>

  <xsl:template match="property[@declaredAs]" mode="displayText" priority="50">
    <xsl:apply-templates select="ld:key('aid', current()/@declaredAs)" mode="displayText"/>
  </xsl:template>

  <xsl:template match="property" mode="displayText">
    <xsl:value-of select="@name"/>
    <xsl:if test="param">
      <xsl:text>[</xsl:text>
      <xsl:apply-templates select="param" mode="displayText"/>
      <xsl:text>]</xsl:text>
    </xsl:if>
  </xsl:template>

  <xsl:template match="field" mode="displayText">
    <xsl:value-of select="@name"/>
  </xsl:template>

  <xsl:template match="param" mode="displayText">
    <xsl:if test="position() > 1">
      <xsl:text>, </xsl:text>
    </xsl:if>
    <xsl:apply-templates select="@type | @param | arrayOf" mode="displayText">
      <xsl:with-param name="attributes" select="attribute" />
    </xsl:apply-templates>
  </xsl:template>

  <xsl:template match="arrayOf" mode="displayText">
    <xsl:apply-templates select="@type | @param  | arrayOf" mode="displayText"/>
    <xsl:text>[</xsl:text>
    <xsl:call-template name="arrayRank">
      <xsl:with-param name="rank" select="@rank" />
    </xsl:call-template>
    <xsl:text>]</xsl:text>
  </xsl:template>

  <xsl:template name="arrayRank">
    <xsl:param name="rank"/>
    <xsl:if test=" $rank &gt; 1">
      <xsl:text>,</xsl:text>
      <xsl:call-template name="arrayRank">
        <xsl:with-param name="rank" select=" $rank - 1 "/>
      </xsl:call-template>
    </xsl:if>
  </xsl:template>


  <!-- this is only for open generic types-->
  <xsl:template match="typeparam" mode="displayText">
    <xsl:value-of select="@name"/>
  </xsl:template>

  <xsl:template match="class | interface | struct" mode="displayText">
    <xsl:param name="attributes" select="false()" />
    
    <xsl:if test="parent::*[not(self::namespace)]">
      <xsl:apply-templates select="parent::*" mode="displayText"/>
      <xsl:text>.</xsl:text>
    </xsl:if>
    <xsl:choose>
      <xsl:when test="typeparam">
        <xsl:value-of select="substring-before(@name, '`')"/>
        <xsl:text>&lt;</xsl:text>
        <xsl:apply-templates select="typeparam" mode="displayText"/>
        <xsl:text>&gt;</xsl:text>
      </xsl:when>
      <xsl:when test="ld:asset(@assetId) = 'T:System.Object' and $attributes and $attributes[ld:asset(@type) = 'T:System.Runtime.CompilerServices.DynamicAttribute']">
        <xsl:text>dynamic</xsl:text>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="@name"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="enum" mode="displayText">
    <xsl:if test="not(@assetId)">
      <xsl:message terminate="yes">No asset id found on enum!</xsl:message>
    </xsl:if>
    <xsl:if test="parent::*[not(self::namespace)]">
      <xsl:apply-templates select="parent::*" mode="displayText"/>
      <xsl:text>.</xsl:text>
    </xsl:if>
    <xsl:value-of select="@name"/>
  </xsl:template>

  <xsl:template match="typearg" mode="displayText">
    <xsl:apply-templates select="ld:key('aid', current()/@type)" mode="displayText"/>
  </xsl:template>

  <xsl:template match="typeparam" mode="displayText">
    <xsl:if test="position() &gt; 1">
      <xsl:text>, </xsl:text>
    </xsl:if>
    <xsl:value-of select="@name"/>
  </xsl:template>


  <!-- SHORT NAMES -->
  <!-- TODO is this really needed? -->

  <xsl:template match="@*" mode="shortName">
    <xsl:param name="attributes" select="false()" />
    
    <xsl:choose>
      <xsl:when test="name() = 'param'">
        <xsl:value-of select="."/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:choose>
          <xsl:when test="parent::*/with">
            <xsl:value-of select="substring-before(ld:key('aid', current())/@name, '`')"/>
            <xsl:text>&lt;</xsl:text>
            <xsl:apply-templates select="parent::*/with" mode="shortName"/>
            <xsl:text>&gt;</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:apply-templates select="ld:key('aid', current())" mode="shortName">
              <xsl:with-param name="attributes" select="$attributes" />
            </xsl:apply-templates>
          </xsl:otherwise>
        </xsl:choose>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="with" mode="shortName">
    <xsl:if test="position() > 1">
      <xsl:text>, </xsl:text>
    </xsl:if>
    <xsl:apply-templates select="@type | @param" mode="shortName"/>
  </xsl:template>

  <xsl:template match="constructor" mode="shortName">
    <xsl:choose>
      <xsl:when test="../typeparam">
        <xsl:value-of select="substring-before(../@name, '`')"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="../@name"/>
      </xsl:otherwise>
    </xsl:choose>
    <xsl:if test="param">
      <xsl:text>(</xsl:text>
      <xsl:apply-templates select="param" mode="shortName"/>
      <xsl:text>)</xsl:text>
    </xsl:if>
  </xsl:template>

  <xsl:template match="assembly" mode="shortName">
    <xsl:value-of select="@name"/>
  </xsl:template>

  <xsl:template match="namespace" mode="shortName">
    <xsl:choose>
      <xsl:when test="contains(@name, '.')">
        <xsl:value-of select="ld:substringAfterLast(@name, '.')"/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="@name"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- operators -->
  <xsl:template match="operator[@name = 'op_Implicit' or @name = 'op_Explicit']" mode="shortName">
    <xsl:value-of select="substring-after(@name, 'op_')"/>
    <xsl:text> Conversion </xsl:text>
    <xsl:choose>
      <xsl:when test="param/@type = parent::*/@assetId">
        <xsl:text> to </xsl:text>
        <!--<xsl:apply-templates select="/bundle/assembly/namespace//*[@assetId = current()/returns/@type]" mode="shortName" />-->
        <xsl:apply-templates select="ld:key('aid', current()/returns/@type)" mode="shortName" />
      </xsl:when>
      <xsl:otherwise>
        <xsl:text> from </xsl:text>
        <!--<xsl:apply-templates select="/bundle/assembly/namespace//*[@assetId = current()/param/@type]" mode="shortName" />-->
        <xsl:apply-templates select="ld:key('aid', current()/param/@type)" mode="shortName" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="operator" mode="shortName">
    <xsl:value-of select="substring-after(@name, 'op_')"/>
  </xsl:template>

  <xsl:template match="method" mode="shortName">
    <xsl:choose>
      <xsl:when test="typeparam">
        <xsl:value-of select="@name"/>
        <xsl:text>&lt;</xsl:text>
        <xsl:apply-templates select="typeparam" mode="shortName"/>
        <xsl:text>&gt;</xsl:text>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="@name"/>
      </xsl:otherwise>
    </xsl:choose>
    <xsl:if test="param">
      <xsl:text>(</xsl:text>
      <xsl:apply-templates select="param" mode="shortName"/>
      <xsl:text>)</xsl:text>
    </xsl:if>
  </xsl:template>

  <xsl:template match="property[@declaredAs]" mode="shortName" priority="50">
    <!--<xsl:apply-templates select="//*[@assetId = current()/@declaredAs]" mode="shortName"/>-->
    <xsl:apply-templates select="ld:key('aid', current()/@declaredAs)" mode="shortName"/>
  </xsl:template>

  <xsl:template match="property" mode="shortName">
    <xsl:value-of select="@name"/>
    <xsl:if test="param">
      <xsl:text>[</xsl:text>
      <xsl:apply-templates select="param" mode="shortName"/>
      <xsl:text>]</xsl:text>
    </xsl:if>
  </xsl:template>

  <xsl:template match="field" mode="shortName">
    <xsl:value-of select="@name"/>
  </xsl:template>

  <xsl:template match="param" mode="shortName">
    <xsl:if test="position() > 1">
      <xsl:text>, </xsl:text>
    </xsl:if>
    <xsl:apply-templates select="@type | @param | arrayOf" mode="shortName">
      <xsl:with-param name="attributes" select="attribute" />
    </xsl:apply-templates>
  </xsl:template>

  <xsl:template match="arrayOf" mode="shortName">
    <xsl:apply-templates select="@type | @param  | arrayOf" mode="shortName"/>
    <xsl:text>[</xsl:text>
    <xsl:call-template name="arrayRank">
      <xsl:with-param name="rank" select="@rank" />
    </xsl:call-template>
    <xsl:text>]</xsl:text>
  </xsl:template>

  <!-- this is only for open generic types-->
  <xsl:template match="typeparam" mode="shortName">
    <xsl:value-of select="@name"/>
  </xsl:template>

  <xsl:template match="class | interface | struct" mode="shortName">
    <xsl:param name="attributes" select="false()" />
    
    <xsl:choose>
      <xsl:when test="typeparam">
        <xsl:value-of select="substring-before(@name, '`')"/>
        <xsl:text>&lt;</xsl:text>
        <xsl:apply-templates select="typeparam" mode="shortName"/>
        <xsl:text>&gt;</xsl:text>
      </xsl:when>
      <xsl:when test="ld:asset(@assetId) = 'T:System.Object' and $attributes and $attributes[ld:asset(@type) = 'T:System.Runtime.CompilerServices.DynamicAttribute']">
        <xsl:text>dynamic</xsl:text>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="@name"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="enum" mode="shortName">
    <xsl:if test="not(@assetId)">
      <xsl:message terminate="yes">No asset id found on enum!</xsl:message>
    </xsl:if>
    <xsl:value-of select="@name"/>
  </xsl:template>

  <xsl:template match="typearg" mode="shortName">
    <xsl:apply-templates select="ld:key('aid', current()/@type)" mode="shortName"/>
  </xsl:template>

  <xsl:template match="typeparam" mode="shortName">
    <xsl:if test="position() &gt; 1">
      <xsl:text>, </xsl:text>
    </xsl:if>
    <xsl:value-of select="@name"/>
  </xsl:template>


</xsl:stylesheet>
