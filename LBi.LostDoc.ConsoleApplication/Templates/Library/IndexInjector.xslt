<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                exclude-result-prefixes="msxsl"
                xmlns:ld="urn:lostdoc-core">

  <xsl:output method="xml" indent="yes"/>

  <xsl:param name="targets"/>

  <!-- by default just copy everything through -->
  <xsl:template match="@* | node()" priority="-1">
    <xsl:copy>
      <xsl:apply-templates select="@* | node()"/>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="meta-template[@stylesheet='IndexInjector.xslt']">
    <xsl:if test="$targets">
      <xsl:variable name="next" select="substring-after($targets, ',')"/>
      <xsl:variable name="first" select="normalize-space(substring-before($targets, ','))"/>
      <xsl:apply-templates select="template/apply-template[@stylesheet = $first]" mode="inject"/>
      <xsl:if test="$next">
        <!-- if there is more data, inject own processing instructions -->
        <meta-template stylesheet="IndexInjector.xslt">
          <with-param name="targets" select="$next" />
        </meta-template>
      </xsl:if>
    </xsl:if>
  </xsl:template>

  <xsl:template match="apply-template" mode="inject">
    <xsl:copy />

    <apply-template name="{concat('Index for: ', ld:coalesce(@name, substring-before(@stylesheet, '.')))}"
                    stylesheet="CreateIndex.xslt">
      <xsl:copy-of select="@*[local-name() != 'name' and local-name() != 'stylesheet']"/>
    </apply-template>
  </xsl:template>
</xsl:stylesheet>
