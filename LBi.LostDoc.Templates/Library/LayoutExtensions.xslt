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
                xmlns:ld="urn:lostdoc-core"
                exclude-result-prefixes="msxsl">

  <xsl:template name="section-head-first" />
 
  <xsl:template name="section-head-last"/>

  <xsl:template name="section-body-first"/>

  <xsl:template name="section-header-before"/>

  <xsl:template name="section-header-first"/>

  <xsl:template name="section-header-last"/>

  <xsl:template name="section-header-after"/>

  <xsl:template name="section-main-before"/>
  
  <xsl:template name="section-main-first"/>
  
  <xsl:template name="section-main-last"/>
  
  <xsl:template name="section-main-after"/>
  
  <xsl:template name="section-body-last"/>
</xsl:stylesheet>
