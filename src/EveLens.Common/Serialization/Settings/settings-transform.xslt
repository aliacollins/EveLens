<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
  <xsl:strip-space elements="*" />
  <xsl:output method="xml" indent="yes"/>

  <xsl:template match="@* | node()">
    <xsl:copy>
      <xsl:apply-templates select="@* | node()"/>
    </xsl:copy>
  </xsl:template>

  <!--Transforms the SerializableDictionary to its Modified version-->
  <xsl:template match="periods/dictionary">
    <xsl:for-each select ="item">
      <xsl:variable name="apiMethod" select="key/string"/>
      <!--As of version 1.5.2 the element name was renamed, so we try to catch that name too-->
      <xsl:variable name="apiMethodPre-Renamed" select="key/APIMethodsEnum"/>
      <xsl:variable name="updatePeriod" select="value/UpdatePeriod"/>
      <period updatePeriod ="{$updatePeriod}">
        <xsl:value-of select="$apiMethod|$apiMethodPre-Renamed"/>
      </period>
    </xsl:for-each>
  </xsl:template>

  <!--Transforms the SerializableDictionary to its Modified version-->
  <xsl:template match="categories/dictionary">
    <xsl:for-each select ="item">
      <xsl:variable name="notificationCategory" select="key/NotificationCategory"/>
      <xsl:variable name="toolTipBehaviour" select="value/NotificationCategorySettings/@toolTipBehaviour"/>
      <xsl:variable name="showOnMainWindow" select="value/NotificationCategorySettings/@showOnMainWindow"/>
      <!--As of version 1.3.3 the spelling of enumeration 'RepeatUntiClicked' was corrected and double supported since 1.6.1,
      though we need to catch that too for all those that haven't updated since-->
      <xsl:choose>
        <xsl:when test="$toolTipBehaviour='RepeatUntiClicked'">
          <category toolTipBehaviour ="RepeatUntilClicked" showOnMainWindow ="{$showOnMainWindow}">
            <xsl:value-of select="$notificationCategory"/>
          </category>
        </xsl:when>
        <xsl:otherwise>
          <category toolTipBehaviour ="{$toolTipBehaviour}" showOnMainWindow ="{$showOnMainWindow}">
            <xsl:value-of select="$notificationCategory"/>
          </category>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:for-each>
  </xsl:template>

  <!--Transforms the SerializableDictionary to its Modified version-->
  <xsl:template match="locations/dictionary">
    <xsl:for-each select ="item">
      <xsl:variable name="window" select="key/string"/>
      <xsl:variable name="left" select="value/SerializableRectangle/@left"/>
      <xsl:variable name="top" select="value/SerializableRectangle/@top"/>
      <xsl:variable name="width" select="value/SerializableRectangle/@width"/>
      <xsl:variable name="height" select="value/SerializableRectangle/@height"/>
      <location left="{$left}" top="{$top}" width="{$width}" height="{$height}">
        <xsl:value-of select="$window"/>
      </location>
    </xsl:for-each>
  </xsl:template>

  <!--Transforms the SerializableDictionary to its Modified version-->
  <xsl:template match="splitters/dictionary">
    <xsl:for-each select ="item">
      <xsl:variable name="splitter" select="key/string"/>
      <xsl:variable name="int" select="value/int"/>
      <int int="{$int}">
        <xsl:value-of select="$splitter"/>
      </int>
    </xsl:for-each>
  </xsl:template>

  <!-- Renaming element value 'None' to 'NoSlot' in item browser slot filtering -->
  <xsl:template match="slotFilter">
    <xsl:choose>
      <xsl:when test="text()='None'">
        <xsl:copy>
          <xsl:value-of select="'NoSlot'"/>
        </xsl:copy>
      </xsl:when>
      <xsl:otherwise>
        <xsl:copy-of select="."/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- Replacing element value 'Named' and 'Other' in item browser metaGroup filtering -->
  <xsl:template match="metaGroupFilter">
    <xsl:choose>
      <xsl:when test="contains(text(), 'Named') or contains(text(), 'Other')">
        <xsl:copy>
          <xsl:value-of select="'All'"/>
        </xsl:copy>
      </xsl:when>
      <xsl:otherwise>
        <xsl:copy-of select="."/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>
  
  <!-- Renaming element value 'Name' to 'None' in certificate browser sort filtering -->
  <xsl:template match="certificateBrowser/sort">
    <xsl:choose>
      <xsl:when test="text()='Name'">
        <xsl:copy>
          <xsl:value-of select="'None'"/>
        </xsl:copy>
      </xsl:when>
      <xsl:otherwise>
        <xsl:copy-of select="."/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

</xsl:stylesheet>
