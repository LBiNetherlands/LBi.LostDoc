<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                xmlns:doc="urn:doc"
                xmlns:ld="urn:lostdoc-core"
                exclude-result-prefixes="msxsl">

  <xsl:template match="doc:summary">
    <xsl:apply-templates select="node()" mode="doc"/>
  </xsl:template>

  <xsl:template match="doc:returns">
    <xsl:apply-templates select="node()" mode="doc"/>
  </xsl:template>
  
  <xsl:template match="see" mode="doc">
    <a href="{ld:resolve(@cref)}">
      <xsl:apply-templates select="//*[@assetId = current()/@cref]" mode="displayText"/>
    </a>
  </xsl:template>


  <xsl:template match="paramref" mode="doc">
    <em>
      <xsl:value-of select="@name"/>
    </em>
  </xsl:template>



  <xsl:template name="missing">
    <span class="error">missing</span>
  </xsl:template>
</xsl:stylesheet>
