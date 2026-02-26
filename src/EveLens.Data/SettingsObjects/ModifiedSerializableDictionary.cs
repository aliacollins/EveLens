// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using EveLens.Common.Constants;
using EveLens.Common.Extensions;

namespace EveLens.Common.SettingsObjects
{
    [Serializable]
    public sealed class ModifiedSerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
        where TKey : notnull
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public ModifiedSerializableDictionary()
        {
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="source"></param>
        public ModifiedSerializableDictionary(IDictionary<TKey, TValue> source)
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
        public XmlSchema? GetSchema() => null;

        /// <summary>
        /// Generates an object from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.Xml.XmlReader"/> stream from which the object is deserialized.</param>
        /// <exception cref="System.ArgumentNullException">reader</exception>
        public void ReadXml(XmlReader reader)
        {
            reader.ThrowIfNull(nameof(reader));

            if (reader.IsEmptyElement)
                return;

            reader.Read();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                TValue? value = default;

                // Does the element have attributes?
                if (reader.HasAttributes)
                {
                    Type valueType = typeof(TValue);

                    // Does type have properties ?
                    if (valueType.GetProperties().Length == 0)
                    {
                        TypeConverter converter = TypeDescriptor.GetConverter(valueType);

                        string? attribute = reader.GetAttribute(0);

                        if (string.IsNullOrEmpty(attribute))
                            break;

                        // Assign the value
                        value = (TValue?)converter.ConvertFromInvariantString(attribute);
                    }
                    else
                    {
                        // Assign the value
                        value = Activator.CreateInstance<TValue>();

                        // Assign values to the properties
                        foreach (PropertyInfo property in valueType.GetProperties().Where(
                            property => property.CanWrite && !Attribute.IsDefined(property, typeof(XmlIgnoreAttribute))))
                        {
                            // Get property name from XmlElement/XmlAttribute or use property name
                            Attribute? attr = property.GetCustomAttributes(false).Where(
                                x => x is XmlElementAttribute || x is XmlAttributeAttribute).Cast<Attribute>().FirstOrDefault();

                            XmlElementAttribute? xmlElement = attr as XmlElementAttribute;
                            XmlAttributeAttribute? xmlAttribute = attr as XmlAttributeAttribute;

                            string propertyName;
                            if (xmlElement != null && !string.IsNullOrWhiteSpace(xmlElement.ElementName))
                                propertyName = xmlElement.ElementName;
                            else if (xmlAttribute != null && !string.IsNullOrWhiteSpace(xmlAttribute.AttributeName))
                                propertyName = xmlAttribute.AttributeName;
                            else
                                propertyName = property.Name;

                            TypeConverter converter = TypeDescriptor.GetConverter(property.PropertyType);

                            string? attribute = reader.GetAttribute(propertyName);

                            if (string.IsNullOrEmpty(attribute))
                                continue;

                            object? propertyValue = converter.ConvertFromInvariantString(attribute);

                            property.SetValue(value, propertyValue, null);
                        }
                    }
                }

                reader.Read();

                TypeConverter typeConverter = TypeDescriptor.GetConverter(typeof(TKey));

                // Assign the key
                object? keyValue = typeConverter.ConvertFrom(reader.Value);
                if (keyValue is TKey key && value is TValue val)
                    Add(key, val);

                reader.Read();
                reader.ReadEndElement();
            }

            reader.ReadEndElement();
        }

        /// <summary>
        /// Converts an object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Xml.XmlWriter" /> stream to which the object is serialized.</param>
        /// <exception cref="System.ArgumentNullException">writer</exception>
        /// <exception cref="System.ArgumentException">
        /// </exception>
        public void WriteXml(XmlWriter writer)
        {
            writer.ThrowIfNull(nameof(writer));

            Type keyType = typeof(TKey);
            Type valueType = typeof(TValue);

            // Check that each type we use is serializable
#pragma warning disable SYSLIB0050 // Type.IsSerializable is obsolete but still useful for XML serialization validation
            if (!keyType.IsSerializable)
                throw new ArgumentException($"{keyType} is not serializable", keyType.ToString());

            if (!valueType.IsSerializable)
                throw new ArgumentException($"{valueType} is not serializable", valueType.ToString());
#pragma warning restore SYSLIB0050

            // Serialize each dictionary element as Xml
            foreach (TKey key in Keys)
            {
                XmlRootAttribute? rootAttribute =
                    valueType.GetCustomAttributes(typeof(XmlRootAttribute), false).Cast<XmlRootAttribute>().FirstOrDefault();

                // Get the name specified in XmlRootAttribute or use the type name
                string elementName = rootAttribute != null && !string.IsNullOrWhiteSpace(rootAttribute.ElementName)
                    ? rootAttribute.ElementName
                    : TypeName;

                // Write as XmlElement
                writer.WriteStartElement(elementName);

                // Do we have properties ?
                if (valueType.GetProperties().Length == 0)
                {
                    string attributeName = TypeName;

                    // Write the value as XmlAttribute
                    writer.WriteAttributeString(attributeName, this[key]?.ToString() ?? string.Empty);
                }
                else
                {
                    // Note: Best practice is to serialize properties instead of fields;
                    // however if you have to serialize fields use the 'GetFields()' method
                    // and adjust the code accordingly using the 'NonSerializedAttribute'

                    // Write each property value as XmlAttribute excluding those that have the 'XmlIgnoreAttribute'
                    foreach (PropertyInfo property in valueType.GetProperties().Where(
                        property => !Attribute.IsDefined(property, typeof(XmlIgnoreAttribute))))
                    {
                        Attribute? attribute = property.GetCustomAttributes(false).Where(
                            x => x is XmlElementAttribute || x is XmlAttributeAttribute).Cast<Attribute>().FirstOrDefault();

                        XmlElementAttribute? xmlElement = attribute as XmlElementAttribute;
                        XmlAttributeAttribute? xmlAttribute = attribute as XmlAttributeAttribute;

                        // Get the name specified in XmlElement/XmlAttribute or use the property name
                        string attributeName;
                        if (xmlElement != null && !string.IsNullOrWhiteSpace(xmlElement.ElementName))
                            attributeName = xmlElement.ElementName;
                        else if (xmlAttribute != null && !string.IsNullOrWhiteSpace(xmlAttribute.AttributeName))
                            attributeName = xmlAttribute.AttributeName;
                        else
                            attributeName = property.Name;

                        object? propertyValue = property.GetValue(this[key], null);

                        // Special condition for Boolean type (because "Boolean.ToString()" returns "True"/"False")
                        string? propertyValueString = propertyValue is bool boolValue
                            ? XmlConvert.ToString(boolValue)
                            : propertyValue?.ToString();

                        writer.WriteAttributeString(attributeName, propertyValueString ?? string.Empty);
                    }
                }

                // Write the key as XmlText
                writer.WriteValue(key.ToString() ?? string.Empty);

                writer.WriteEndElement();
            }
        }

        /// <summary>
        /// Gets the name of the type.
        /// </summary>
        /// <value>The name of the type.</value>
        private static string TypeName
        {
            get
            {
                Type type = typeof(TValue);
                if (type.Namespace != "System")
                    return type.Name.ConvertUpperToLowerCamelCase();

                switch (type.Name)
                {
                    case "Int16":
                        return "short";
                    case "Int32":
                        return "int";
                    case "Int64":
                        return "long";
                    case "UInt16":
                        return "unsignedShort";
                    case "UInt32":
                        return "unsignedInt";
                    case "UInt64":
                        return "unsignedLong";
                    case "Single":
                        return "float";
                    case "DateTime":
                        return "dateTime";
                }
                return type.Name.ToLower(CultureConstants.InvariantCulture);
            }
        }
    }
}