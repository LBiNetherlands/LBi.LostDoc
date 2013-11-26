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
                exclude-result-prefixes="msxsl ld">

  <xsl:param name="logoUri"/>
  
  <xsl:include href="LayoutExtensions.xslt"/>

  <xsl:template match="/">
    <xsl:text disable-output-escaping="yes" xml:space="preserve">&lt;!DOCTYPE html&gt;&#xA;</xsl:text>
    <html>
      <head>
        <xsl:call-template name="section-head-first"/>
        <meta name="generator" content="{ld:generator()}" />
        <meta name="asset-id" content="{$assetId}" />
        <title>
          <xsl:call-template name="title"/>
        </title>
        <link rel="stylesheet" type="text/css">
          <xsl:attribute name="href">
            <xsl:value-of select="ld:relative('css/style.css')"/>
          </xsl:attribute>
        </link>
        <xsl:call-template name="section-head-last"/>
      </head>
      <body>
        <xsl:call-template name="section-body-first"/>
        <div id="wrapper">
          <div class="left-col-outer" >
            <div class="left-col-inner detachable">
              <xsl:call-template name="section-header-before"/>
              <header>
                <xsl:call-template name="section-header-first"/>
                <img src="{ld:relative($logoUri)}" alt="LostDoc Logo" class="logo"/> 
                <xsl:call-template name="section-header-last"/>
              </header>
              <xsl:call-template name="section-header-after"/>
              <nav>
                <xsl:call-template name="navigation"/>
              </nav>
            </div>
            <span class="handle"></span>
          </div>
          <!-- / Main navigation -->

          <div class="right-col-outer">
            <div class="right-col-inner">
              <xsl:call-template name="section-main-before"/>
              <main class="main-content detachable">
                <xsl:call-template name="section-main-first"/>
                <xsl:call-template name="content"/>
                <xsl:call-template name="section-main-last"/>
              </main>
              <xsl:call-template name="section-main-after"/>
            </div>
          </div>

        </div>
        <script src="{ld:relative('js/lib/zepto.js')}" >&#160;</script>
        <script src="{ld:relative('js/lostdoc.settings.js')}" >&#160;</script>
        <script src="{ld:relative('js/lostdoc.layout.js')}" >&#160;</script>
        <script src="{ld:relative('js/lostdoc.js')}" >&#160;</script>
        <xsl:call-template name="section-body-last"/>
      </body>
    </html>
  </xsl:template>

</xsl:stylesheet>
