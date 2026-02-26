// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using EveLens.Common.Extensions;

namespace EveLens.Common.SettingsObjects
{
    [Serializable]
    public sealed class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public SerializableDictionary()
        {
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="source"></param>
        public SerializableDictionary(IDictionary<TKey, TValue> source)
            : base(source)
        {
        }

        /// <summary>
        /// This method is reserved and should not be used.
        /// When implementing the IXmlSerializable interface,
        /// you should return null (Nothing in Visual Basic) from this method,
        /// and instead, if specifying a custom schema is required,
        /// apply the <see cref="T:System.Xml.Serialization.XmlSchemaProviderAttribute"/> to the class.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Xml.Schema.XmlSchema"/> that describes the XML representation of the object
        /// that is produced by the <see cref="M:System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter)"/> method
        /// and consumed by the <see cref="M:System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader)"/> method.
        /// </returns>
        public XmlSchema GetSchema() => null;

        /// <summary>
        /// Generates an object from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.Xml.XmlReader"/> stream from which the object is deserialized.</param>
        /// <exception cref="System.ArgumentNullException">reader</exception>
        public void ReadXml(XmlReader reader)
        {
            reader.ThrowIfNull(nameof(reader));

            XmlSerializer keySer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSer = new XmlSerializer(typeof(TValue));

            reader.Read();
            reader.ReadStartElement("dictionary");
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                reader.ReadStartElement("item");

                reader.ReadStartElement("key");
                TKey key = (TKey)keySer.Deserialize(reader);
                reader.ReadEndElement();

                reader.ReadStartElement("value");
                TValue value = (TValue)valueSer.Deserialize(reader);
                reader.ReadEndElement();

                Add(key, value);

                reader.ReadEndElement();
                reader.MoveToContent();
            }

            if (Count != 0)
                reader.ReadEndElement();
            reader.ReadEndElement();
        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Xml.XmlWriter" /> stream to which the object is serialized.</param>
        /// <exception cref="System.ArgumentNullException">writer</exception>
        public void WriteXml(XmlWriter writer)
        {
            writer.ThrowIfNull(nameof(writer));

            XmlSerializer keySer = new XmlSerializer(typeof(TKey));
            XmlSerializer valueSer = new XmlSerializer(typeof(TValue));

            writer.WriteStartElement("dictionary");
            foreach (TKey key in Keys)
            {
                writer.WriteStartElement("item");

                writer.WriteStartElement("key");
                keySer.Serialize(writer, key);
                writer.WriteEndElement();

                writer.WriteStartElement("value");
                valueSer.Serialize(writer, this[key]);
                writer.WriteEndElement();

                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }
    }
}