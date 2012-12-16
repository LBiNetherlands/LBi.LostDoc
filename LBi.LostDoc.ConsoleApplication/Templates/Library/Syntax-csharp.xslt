<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                xmlns:ld="urn:lostdoc-core"
                exclude-result-prefixes="msxsl">


  <xsl:template match="enum" mode="syntax-cs-naming">
    <xsl:value-of select="@name"/>
  </xsl:template>

  <xsl:template match="property | field" mode="syntax-cs-naming">
    <xsl:value-of select="@name"/>
  </xsl:template>

  <xsl:template match="class | interface | struct" mode="syntax-cs-naming">
    <xsl:param name="typeargs" select="false()"/>
    <xsl:if test="position() > 1">
      <xsl:text>, </xsl:text>
    </xsl:if>
    <xsl:choose>
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
    <xsl:choose>
      <xsl:when test="name() = 'param'">
        <xsl:value-of select="."/>
      </xsl:when>
      <xsl:otherwise>
        <xsl:choose>
          <xsl:when test="parent::*/with">
            <xsl:value-of select="substring-before(/bundle/assembly/namespace//*[@assetId = current()]/@name, '`')"/>
            <xsl:text>&lt;</xsl:text>
            <xsl:apply-templates select="parent::*/with" mode="syntax-cs-naming"/>
            <xsl:text>&gt;</xsl:text>
          </xsl:when>
          <xsl:otherwise>
            <xsl:apply-templates select="//*[@assetId = current()]" mode="syntax-cs-naming"/>
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
            <xsl:text>bbyte</xsl:text>
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
            <xsl:apply-templates select="/bundle/assembly/namespace//*[@assetId = current()/inherits/@type]" mode="syntax-cs-naming">
              <xsl:with-param name="typeargs" select="inherits/with"/>
            </xsl:apply-templates>
          </xsl:when>
          <xsl:otherwise>
            <xsl:apply-templates select="//bundle/assembly/namespace//*[@assetId = current()/inherits/@type]" mode="syntax-cs-naming"/>
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
  </xsl:template>



  <xsl:template match="implements" mode="syntax-cs-naming">
    <xsl:if test="position() > 1">
      <xsl:text>, </xsl:text>
    </xsl:if>
    <xsl:choose>
      <xsl:when test="with">
        <xsl:apply-templates select="//bundle/assembly/namespace//*[@assetId = current()/@interface]" mode="syntax-cs-naming">
          <xsl:with-param name="typeargs" select="with"/>
        </xsl:apply-templates>
      </xsl:when>
      <xsl:otherwise>
        <xsl:apply-templates select="//bundle/assembly/namespace//*[@assetId = current()/@interface]" mode="syntax-cs-naming"/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="*" mode="syntax-cs-access">
    <xsl:variable name="keyword">
      <xsl:choose>
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
    <xsl:if test="$keyword">
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
      <xsl:apply-templates select="." mode="syntax-cs-access"/>
      <xsl:apply-templates select="returns/@type | returns/@param | returns/arrayOf" mode="syntax-cs-naming"/>

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
    <span class="line">
      <xsl:apply-templates select="." mode="syntax-cs-access"/>
      <xsl:choose>
        <xsl:when test="returns">
          <xsl:apply-templates select="returns/@type | returns/@param" mode="syntax-cs-naming"/>
        </xsl:when>
        <xsl:otherwise>
          <span class="keyword">void</span>
        </xsl:otherwise>
      </xsl:choose>
      <xsl:text> </xsl:text>
      <xsl:value-of select="@name"/>
      <xsl:if test="typeparam">
        <xsl:text>&lt;</xsl:text>
        <xsl:apply-templates select="typeparam" mode="syntax-cs-naming"/>
        <xsl:text>&gt;</xsl:text>
      </xsl:if>
      <xsl:text>(</xsl:text>
      <xsl:if test="not(param)">
        <xsl:text>)</xsl:text>
        <xsl:if test="typeparam/constraint">
          <xsl:text> where : </xsl:text>
          <xsl:apply-templates select="typeparam/constraint" mode="syntax-cs"/>
        </xsl:if>
      </xsl:if>
    </span>
    <xsl:if test="param">
      <xsl:apply-templates select="param" mode="syntax-cs-naming"/>
      <span class="line">
        <xsl:text>)</xsl:text>
        <xsl:if test="typeparam/constraint">
          <xsl:text> where : </xsl:text>
          <xsl:apply-templates select="typeparam/constraint" mode="syntax-cs"/>
        </xsl:if>
      </span>
    </xsl:if>
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
      <xsl:apply-templates select="@type | @param" mode="syntax-cs-naming"/>
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
        <xsl:apply-templates select="attribute"/>
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
      <xsl:apply-templates select="@type | @param | arrayOf" mode="syntax-cs-naming"/>
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


  <xsl:template match="attribute" mode="syntax-cs">
    <!-- skip certain attributes -->
    <xsl:if test="ld:asset(@type) != 'T:System.Reflection.DefaultMemberAttribute' and ld:asset(@type) != 'T:System.Runtime.CompilerServices.ExtensionAttribute'">
      <span class="line">
        <xsl:text>[</xsl:text>
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
  </xsl:template>

  <xsl:template match="*" mode="syntax-cs-literal">
    <xsl:if test="position() > 1">
      <xsl:text>, </xsl:text>
    </xsl:if>
    <xsl:if test="@member">
      <xsl:apply-templates select="/bundle/assembly/namespace//*[@assetId = current()/@member]" mode="syntax-cs-naming"/>
      <xsl:text> = </xsl:text>
    </xsl:if>
    <xsl:choose>
      <xsl:when test="array">
        <span class="keyword">
          <xsl:text>new </xsl:text>
        </span>
        <xsl:apply-templates select="array/@type" mode="syntax-cs-naming"/>
        <!--<xsl:value-of select="/bundle/assembly/namespace//*[@assetId = current()/array/@type]/@name"/>-->
        <xsl:text>[] {</xsl:text>
        <xsl:apply-templates select="array/element" mode="syntax-cs-literal"/>
        <xsl:text>}</xsl:text>
      </xsl:when>
      <xsl:when test="enum/flag">
        <xsl:apply-templates select="enum/flag" mode="syntax-cs-literal"/>
      </xsl:when>
      <xsl:when test="ld:asset(@type) = 'T:System.Type'">
        <span class="keyword">
          <xsl:text>typeof</xsl:text>
        </span>
        <xsl:text>(</xsl:text>
        <xsl:apply-templates select="@value" mode="syntax-cs-naming"/>
        <xsl:text>)</xsl:text>
      </xsl:when>
      <xsl:when test="ld:asset(@type) = 'T:System.String'">
        <xsl:text>"</xsl:text>
        <xsl:value-of select="@value"/>
        <xsl:text>"</xsl:text>
      </xsl:when>
      <xsl:when test="ld:asset(@type) = 'T:System.Char'">
        <xsl:text>'</xsl:text>
        <xsl:value-of select="@value"/>
        <xsl:text>'</xsl:text>
      </xsl:when>
      <xsl:when test="ld:asset(@type) = 'T:System.Decimal'">
        <xsl:value-of select="@value"/>
        <xsl:text>m</xsl:text>
      </xsl:when>
      <xsl:when test="ld:asset(@type) = 'T:System.Boolean' and @value = 'True'">
        <span class="keyword">
          <xsl:text>true</xsl:text>
        </span>
      </xsl:when>
      <xsl:when test="ld:asset(@type) = 'T:System.Boolean' and @value = 'False'">
        <span class="keyword">
          <xsl:text>false</xsl:text>
        </span>
      </xsl:when>
      <xsl:otherwise>
        <xsl:value-of select="@value"/>
      </xsl:otherwise>
    </xsl:choose>

  </xsl:template>

  <xsl:template match="flag" mode="syntax-cs-literal">
    <xsl:if test="position() > 1">
      <xsl:text> | </xsl:text>
    </xsl:if>
    <xsl:value-of select="/bundle/assembly/namespace//enum[@assetId = current()/ancestor::argument/@type]/@name"/>
    <xsl:text>.</xsl:text>
    <xsl:value-of select="@value"/>
  </xsl:template>
</xsl:stylesheet>
