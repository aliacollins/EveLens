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

  <!-- Renaming root name 'blueprints' in blueprints datafile to 'blueprintsDatafile' -->
  <xsl:template match="blueprints">
    <xsl:element name="blueprintsDatafile">
      <xsl:copy-of select="namespace::*"/>
      <xsl:apply-templates/>
    </xsl:element>
  </xsl:template>

  <!-- Renaming attribute 'Name' in items datafile to 'name' -->
  <xsl:template match="@Name">
    <xsl:attribute name="name">
      <xsl:value-of select="."/>
    </xsl:attribute>
  </xsl:template>

  <!-- Renaming attribute value 'Bpo' in items datafile to 'Blueprint' -->
  <xsl:template match="@family">
    <xsl:choose>
      <xsl:when test=".='Bpo'">
        <xsl:attribute name="family">
          <xsl:value-of select="'Blueprint'"/>
        </xsl:attribute>
      </xsl:when>
      <xsl:otherwise>
        <xsl:copy/>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

</xsl:stylesheet>
