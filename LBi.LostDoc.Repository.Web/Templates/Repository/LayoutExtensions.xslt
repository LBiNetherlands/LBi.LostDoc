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

  <xsl:output method="html" indent="yes" omit-xml-declaration="yes"/>

  <xsl:template name="section-head-first" />

  <xsl:template name="section-head-last">
    <link rel="stylesheet" href="{ld:relative('css/search.css')}" type="text/css"/>
  </xsl:template>

  <xsl:template name="section-body-first"/>

  <xsl:template name="section-header-before"/>

  <xsl:template name="section-header-first"/>

  <xsl:template name="section-header-last"/>

  <xsl:template name="section-header-after">
    <div class="search">
      <form class="form-wrapper cf" action="{ld:relative('search')}" method="get">
        <input type="text" placeholder="Search" required="required"/>
        <button type="submit">
          <span/>
        </button>
      </form>
      <div id="instant-results" data-bind="css: {{ hidden: !instant() }}, with: instant">
        <ul data-bind="foreach: results, css: {{ hidden: !hasResults() }}">
          <li data-bind="css: {{selected: $parent.selected() == $data }}">
            <a data-bind="attr: {{ href: url, title: title }}">
              <h4 data-bind="text: title">&#160;</h4>
              <p data-bind="text: blurb">&#160;</p>
            </a>
          </li>
        </ul>
      </div>
    </div>
    <!-- / Search -->
  </xsl:template>

  <xsl:template name="section-main-before"/>

  <xsl:template name="section-main-first">
    <xsl:attribute name="data-bind">css: { hidden: resultSet() }</xsl:attribute>
  </xsl:template>

  <xsl:template name="section-main-last">
    <!--<div id="full-results" class="hidden">
      <h1>Results</h1>
    </div>-->
  </xsl:template>

  <xsl:template name="section-main-after">
    <div id="full-results" data-bind="css: {{ hidden: !resultSet() }}, with: resultSet">
      <h1>
        Results for '<span class="query" data-bind="text: query">&#160;</span>'
      </h1>
      <div class="no-results" data-bind="css: {{ hidden: !noResults() }}">
        No results found.
      </div>
      <ul class="search-results" data-bind="foreach: results, css: {{ hidden: !hasResults() }}">
        <li>
          <a data-bind="attr: {{ href: url, title: title }}">
            <h4 data-bind="text: title">&#160;</h4>
          </a>
          <p data-bind="text: blurb">&#160;</p>
        </li>
      </ul>
      <button data-bind="click: fetchNext, css: {{ hidden: !hasMore() }}">Load more results</button>
      <div class="loader" data-bind="visible: loading">Loading...</div>
      <!--<pre data-bind="text: ko.toJSON($data, null, 2)">x</pre>-->
    </div>
  </xsl:template>

  <xsl:template name="section-body-last">
    <script src="{ld:relative('js/lib/knockout.js')}">&#160;</script>
    <script src="{ld:relative('js/search.js')}">&#160;</script>
  </xsl:template>
</xsl:stylesheet>
