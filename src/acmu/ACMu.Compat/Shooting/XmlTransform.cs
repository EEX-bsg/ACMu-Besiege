using System.Xml.Serialization;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    public class XmlTransform
    {
        [XmlElement("Position")] public XmlVector3 Position = new XmlVector3();
        [XmlElement("Rotation")] public XmlVector3 Rotation = new XmlVector3();
        [XmlElement("Scale")]    public XmlVector3 Scale    = new XmlVector3 { x = 1f, y = 1f, z = 1f };

        public Vector3 ToPosition()
        {
            return Position != null ? new Vector3(Position.x, Position.y, Position.z) : Vector3.zero;
        }

        public Quaternion ToRotation()
        {
            return Rotation != null
                ? Quaternion.Euler(Rotation.x, Rotation.y, Rotation.z)
                : Quaternion.identity;
        }
    }
}
