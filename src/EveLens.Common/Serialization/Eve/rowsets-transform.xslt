<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
  <xsl:strip-space elements="*" />
  <xsl:output method="xml" indent="yes" />

  <xsl:template match="@* | node()">
    <xsl:copy>
      <xsl:apply-templates select="@* | node()" />
    </xsl:copy>
  </xsl:template>

  <!-- Rowsets are transformed into something else-->
  <xsl:template match="rowset">
    <!-- Select the set and row names. -->
    <xsl:choose>
      <!-- (skillqueue, row) are transformed into (queue, skill) -->
      <xsl:when test="@name='skillqueue'">
        <xsl:call-template name="rowsets">
          <xsl:with-param name="setName" select="'queue'" />
          <xsl:with-param name="rowName" select="'skill'" />
        </xsl:call-template>
      </xsl:when>
      <!-- (research, row) are transformed into (research, points) -->
      <xsl:when test="@name='research'">
        <xsl:call-template name="rowsets">
          <xsl:with-param name="setName" select="'research'" />
          <xsl:with-param name="rowName" select="'points'" />
        </xsl:call-template>
      </xsl:when>
      <!-- (employmentHistory, row) are transformed into (employmentHistory, record) -->
      <xsl:when test="@name='employmentHistory'">
        <xsl:call-template name="rowsets">
          <xsl:with-param name="setName" select="@name" />
          <xsl:with-param name="rowName" select="'record'" />
        </xsl:call-template>
      </xsl:when>
      <!-- (contractList, row) are transformed into (contracts, contract) -->
      <xsl:when test="@name='contractList'">
        <xsl:call-template name="rowsets">
          <xsl:with-param name="setName" select="'contracts'" />
          <xsl:with-param name="rowName" select="'contract'" />
        </xsl:call-template>
      </xsl:when>
      <!-- (itemList, row) are transformed into (contractItems, contractItem) -->
      <xsl:when test="@name='itemList'">
        <xsl:call-template name="rowsets">
          <xsl:with-param name="setName" select="'contractItems'" />
          <xsl:with-param name="rowName" select="'contractItem'" />
        </xsl:call-template>
      </xsl:when>
      <!-- (bidList, row) are transformed into (bids, bid) -->
      <xsl:when test="@name='bidList'">
        <xsl:call-template name="rowsets">
          <xsl:with-param name="setName" select="'bids'" />
          <xsl:with-param name="rowName" select="'bid'" />
        </xsl:call-template>
      </xsl:when>
      <!-- (contactList, row) are transformed into (contacts, contact) -->
      <xsl:when test="@name='contactList'">
        <xsl:call-template name="rowsets">
          <xsl:with-param name="setName" select="'contacts'" />
          <xsl:with-param name="rowName" select="'contact'" />
        </xsl:call-template>
      </xsl:when>
      <!-- (corporateContactList, row) are transformed into (corporateContacts, corporateContact) -->
      <xsl:when test="@name='corporateContactList'">
        <xsl:call-template name="rowsets">
          <xsl:with-param name="setName" select="'corporateContacts'" />
          <xsl:with-param name="rowName" select="'corporateContact'" />
        </xsl:call-template>
      </xsl:when>
      <!-- (allianceContactList, row) are transformed into (allianceContacts, allianceContact) -->
      <xsl:when test="@name='allianceContactList'">
        <xsl:call-template name="rowsets">
          <xsl:with-param name="setName" select="'allianceContacts'" />
          <xsl:with-param name="rowName" select="'allianceContact'" />
        </xsl:call-template>
      </xsl:when>
      <!-- (currentCorporation, row) are transformed into (currentCorporation, medal) -->
      <xsl:when test="@name='currentCorporation'">
        <xsl:call-template name="rowsets">
          <xsl:with-param name="setName" select="@name" />
          <xsl:with-param name="rowName" select="'medal'" />
        </xsl:call-template>
      </xsl:when>
      <!-- (otherCorporations, row) are transformed into (otherCorporations, medal) -->
      <xsl:when test="@name='otherCorporations'">
        <xsl:call-template name="rowsets">
          <xsl:with-param name="setName" select="@name" />
          <xsl:with-param name="rowName" select="'medal'" />
        </xsl:call-template>
      </xsl:when>
      <!-- (colonies, row) are transformed into (colonies, colony) -->
      <xsl:when test="@name='colonies'">
        <xsl:call-template name="rowsets">
          <xsl:with-param name="setName" select="@name" />
          <xsl:with-param name="rowName" select="'colony'" />
        </xsl:call-template>
      </xsl:when>
      <!-- By default behaviour, the rowset is a plural so we just remove the last character to get the row name-->
      <xsl:otherwise>
        <xsl:call-template name="rowsets">
          <xsl:with-param name="setName" select="@name" />
          <xsl:with-param name="rowName" select="substring(@name, 1, string-length(@name) - 1)" />
        </xsl:call-template>
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <!-- Transform 'characterName' to 'name'-->
  <xsl:template match="@characterName">
    <xsl:attribute name="name">
      <xsl:value-of select="." />
    </xsl:attribute>
  </xsl:template>

  <!-- Template applied to rowsets-->
  <xsl:template name="rowsets">
    <xsl:param name="setName">rowset</xsl:param>
    <xsl:param name="rowName">row</xsl:param>
    <xsl:element name="{$setName}">
      <xsl:for-each select="row">
        <xsl:element name="{$rowName}">
          <xsl:for-each select="@* | node()">
            <xsl:apply-templates select="." />
          </xsl:for-each>
        </xsl:element>
      </xsl:for-each>
    </xsl:element>
  </xsl:template>

</xsl:stylesheet>