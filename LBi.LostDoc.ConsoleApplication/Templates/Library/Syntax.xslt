<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                exclude-result-prefixes="msxsl">

  <xsl:include href="Syntax-csharp.xslt"/>
  
  <xsl:template match="*" mode="syntax">
    <dl class="syntax">
      <dt>C#</dt>
      <dd>
        <xsl:apply-templates select="." mode="syntax-cs"/>
      </dd>
    </dl>
  </xsl:template>

</xsl:stylesheet>
