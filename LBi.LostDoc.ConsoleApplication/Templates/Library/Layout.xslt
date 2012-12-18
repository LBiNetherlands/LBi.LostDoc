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
