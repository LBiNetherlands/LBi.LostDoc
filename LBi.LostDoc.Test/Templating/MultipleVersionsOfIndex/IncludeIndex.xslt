<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                xmlns:ld="urn:lostdoc:template"
                exclude-result-prefixes="msxsl ld">

  <xsl:output method="xml" indent="yes"/>

  <xsl:template match="/">
    <root>
      <xsl:value-of select="ld:key('test', '0')"/>
    </root>
  </xsl:template>
</xsl:stylesheet>
