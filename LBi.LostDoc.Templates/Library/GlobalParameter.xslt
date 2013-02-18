<?xml version="1.0" encoding="utf-8"?>
<!-- 
  
  Copyright 2013 LBi Netherlands B.V.
  
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
                exclude-result-prefixes="msxsl"
                xmlns:ld="urn:lostdoc-core">

  <xsl:output method="xml" indent="yes"/>
<xsl:param name="name"/>
  <!-- by default just copy everything through -->
  <xsl:template match="@* | node()"
                priority="-1">
    <xsl:copy>
      <xsl:apply-templates select="@* | node()"/>
    </xsl:copy>
  </xsl:template>

  <xsl:template match="meta-template[@stylesheet='GlobalParameter.xslt' and position() = 1]">
    <xsl:apply-templates select="/template/apply-stylesheet" mode="inject">
      <xsl:with-param name="name" select="$name"/>
      <xsl:with-param name="value" select="with-param[@name='value']/@select"/>
    </xsl:apply-templates>
  </xsl:template>

  <xsl:template match="apply-stylesheet"
                mode="inject">
    <xsl:param name="name"/>
    <xsl:param name="value"/>
    <apply-stylesheet>
      <xsl:copy-of select="@* | node()"/>
      <with-param name="{$name}" select="{$value}" />
    </apply-stylesheet>
  </xsl:template>
</xsl:stylesheet>
