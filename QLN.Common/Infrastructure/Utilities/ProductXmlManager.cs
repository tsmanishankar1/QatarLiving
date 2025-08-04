using QLN.Common.DTO_s.ClassifiedsBo;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace QLN.Classified.MS.Utilities
{
    public class ProductXmlManager
    {
        private readonly string _xsdPath;

        public ProductXmlManager(string xsdPath = "./Data/Products.XSD")
        {
            _xsdPath = xsdPath;
        }

        public void Serialize(Products products, string outputPath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Products));
            using StreamWriter writer = new StreamWriter(outputPath);
            serializer.Serialize(writer, products);
        }

        public Products Deserialize(string xmlPath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Products));
            using FileStream stream = new FileStream(xmlPath, FileMode.Open);
            return (Products)serializer.Deserialize(stream);
        }

        public string ValidateXml(string xmlPath)
        {
            string errors = string.Empty;
            bool isValid = true;

            XmlSchemaSet schemaSet = new XmlSchemaSet();
            schemaSet.Add(null, _xsdPath);

            XmlReaderSettings settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = schemaSet
            };

            settings.ValidationEventHandler += (s, e) =>
            {
                isValid = false;
                errors += $"{e.Severity}: {e.Message}\n";
            };

            using XmlReader reader = XmlReader.Create(xmlPath, settings);
            while (reader.Read()) { }

            return errors;
        }
    }
}
