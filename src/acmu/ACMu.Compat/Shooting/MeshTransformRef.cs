using System.Xml.Serialization;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    // <Mesh name="assetName">
    //   <Position x="0" y="0" z="-0.1" />
    //   <Rotation x="0" y="-90" z="0" />
    //   <Scale x="0.75" y="0.75" z="0.75" />
    // </Mesh>
    public class MeshTransformRef
    {
        [XmlAttribute("name")]
        public string Name = "";

        [XmlElement("Position")]
        public XmlVector3 Position = new XmlVector3();

        [XmlElement("Rotation")]
        public XmlVector3 Rotation = new XmlVector3();

        [XmlElement("Scale")]
        public XmlVector3 Scale = new XmlVector3 { x = 1f, y = 1f, z = 1f };

        public bool HasMesh { get { return !string.IsNullOrEmpty(Name); } }

        public Vector3 GetPosition()
        {
            return Position != null ? new Vector3(Position.x, Position.y, Position.z) : Vector3.zero;
        }

        public Quaternion GetRotation()
        {
            return Rotation != null ? Quaternion.Euler(Rotation.x, Rotation.y, Rotation.z) : Quaternion.identity;
        }

        public Vector3 GetScale()
        {
            if (Scale == null || (Scale.x == 0f && Scale.y == 0f && Scale.z == 0f))
                return Vector3.one;
            return new Vector3(Scale.x, Scale.y, Scale.z);
        }
    }
}
