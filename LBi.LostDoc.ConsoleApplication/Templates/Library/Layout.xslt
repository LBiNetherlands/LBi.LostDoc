<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                xmlns:ld="urn:lostdoc-core"
                exclude-result-prefixes="msxsl">

  <xsl:output method="html" indent="yes" omit-xml-declaration="yes"/>

  <!--<xsl:include href="Navigation.xslt"/>-->

  <xsl:template match="/">
    <xsl:text disable-output-escaping="yes" xml:space="preserve">&lt;!DOCTYPE html&gt;
    </xsl:text>
    <html>
      <head>
        <link rel="stylesheet" type="text/css">
          <xsl:attribute name="href">
            <xsl:value-of select="ld:resource('style.css')"/>
          </xsl:attribute>
        </link>
        <title>
          <xsl:call-template name="title"/>
        </title>
      </head>
      <body>
        <div id="wrapper">
          <div id="col-left">
            <div id="navigation">
              <xsl:call-template name="navigation"/>
            </div>
          </div>
          <div id="col-right">
            <div id="content">
              <xsl:call-template name="content"/>
            </div>
          </div>
        </div>
      </body>
    </html>
  </xsl:template>

</xsl:stylesheet>
