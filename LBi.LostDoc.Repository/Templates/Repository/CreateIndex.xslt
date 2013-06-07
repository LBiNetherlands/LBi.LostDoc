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
                xmlns:doc="urn:doc"
                xmlns:hrc="urn:lostdoc-core:inheritance-hierarchy"
                xmlns:ld="urn:lostdoc-core"
                exclude-result-prefixes="msxsl doc hrc ld">
  <xsl:output method="xml"/>

  <xsl:include href="Naming.xslt"/>
  <xsl:include href="DocComments.xslt"/>

  <xsl:template match="/">
    <index>
      <xsl:apply-templates select="//*[@assetId and @phase='0']" mode="index"/>
    </index>
  </xsl:template>

  <xsl:template match="*[@assetId]" mode="index">
    <document assetId="{@assetId}">
      <title>
        <xsl:apply-templates select="ld:key('aid', @assetId)" mode="title"/>
      </title>
      <summary>
        <xsl:variable name="resultSet">
          <xsl:apply-templates select="doc:summary" mode="doc"/>
        </xsl:variable>

        <xsl:copy-of select="msxsl:node-set($resultSet)//text()"/>
      </summary>

      <text>
        <xsl:variable name="resultSet">
          <xsl:apply-templates select=".//doc:*" mode="doc" />
        </xsl:variable>

        <xsl:copy-of select="msxsl:node-set($resultSet)//text()"/>
      </text>
    </document>
  </xsl:template>
</xsl:stylesheet>


