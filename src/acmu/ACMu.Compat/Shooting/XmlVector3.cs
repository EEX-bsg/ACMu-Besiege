using System.Xml.Serialization;

namespace ACMu.Compat.Shooting
{
    public class XmlVector3
    {
        [XmlAttribute("x")] public float x;
        [XmlAttribute("y")] public float y;
        [XmlAttribute("z")] public float z;
    }
}
