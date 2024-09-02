﻿using System.Collections.Generic;
using System.Xml.Serialization;

namespace ToCBS.XmlMum
{
    [XmlRoot(ElementName = "customInformation", Namespace = "urn:schemas-microsoft-com:asm.v3")]
    public class CustomInformation
    {
        [XmlElement(ElementName = "phoneInformation", Namespace = "urn:schemas-microsoft-com:asm.v3")]
        public PhoneInformation PhoneInformation
        {
            get; set;
        }
        [XmlElement(ElementName = "file", Namespace = "urn:schemas-microsoft-com:asm.v3")]
        public List<File> File
        {
            get; set;
        }
    }
}
