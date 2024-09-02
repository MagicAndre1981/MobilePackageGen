﻿using System.Collections.Generic;
using System.Xml.Serialization;

namespace ToCBS.XmlMum
{
    [XmlRoot(ElementName = "registryKey", Namespace = "urn:schemas-microsoft-com:asm.v3")]
    public class RegistryKey
    {

        [XmlElement(ElementName = "registryValue", Namespace = "urn:schemas-microsoft-com:asm.v3")]
        public List<RegistryValue> RegistryValues
        {
            get; set;
        }
        [XmlAttribute(AttributeName = "keyName", Namespace = "urn:schemas-microsoft-com:asm.v3")]
        public string KeyName
        {
            get; set;
        }
        [XmlElement(ElementName = "securityDescriptor", Namespace = "urn:schemas-microsoft-com:asm.v3")]
        public SecurityDescriptor SecurityDescriptor
        {
            get; set;
        }
    }
}
