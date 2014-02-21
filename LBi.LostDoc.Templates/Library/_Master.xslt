<?xml version="1.0" encoding="utf-8"?>
<!-- 
  
  Copyright 2014 DigitasLBi Netherlands B.V.
  
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

  <xsl:output method="xml" indent="yes"/>

  <xsl:include href="Naming.xslt"/>

  <xsl:template match="/">
    <display>
      <xsl:apply-templates select="descendant::*[@assetId]"/>
    </display>
  </xsl:template>

  <xsl:template match="*[@assetId]">
    <asset assetId="{@assetId}">
      <title xml:lang="en">
        <xsl:apply-templates select="." mode="title"/>
      </title>
      <name xml:lang="en">
        <xsl:apply-templates select="." mode="displayText"/>
      </name>
    </asset>
  </xsl:template>
</xsl:stylesheet>
