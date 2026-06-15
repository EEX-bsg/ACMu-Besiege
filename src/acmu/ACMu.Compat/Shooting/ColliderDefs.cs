using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace ACMu.Compat.Shooting
{
    // 弾頭プロジェクタイルに付与するコライダー定義の基底クラス。
    // XML では <CapsuleCollider>/<BoxCollider>/<SphereCollider> 要素として記述する。
    public abstract class ColliderDefBase
    {
        [XmlElement("Position")]
        public XmlVector3 Position = new XmlVector3();

        public abstract Collider AddTo(GameObject go);
    }

    // <Capsule direction="X" radius="0.075" height="0.30" />
    public class CapsuleElement
    {
        [XmlAttribute("direction")] public string Direction = "Y";
        [XmlAttribute("radius")]    public float Radius    = 0.5f;
        [XmlAttribute("height")]    public float Height    = 2f;

        public int DirectionIndex
        {
            get
            {
                if (Direction == "X") return 0;
                if (Direction == "Z") return 2;
                return 1;
            }
        }
    }

    public class CapsuleColliderDef : ColliderDefBase
    {
        [XmlElement("Capsule")]
        public CapsuleElement Capsule = new CapsuleElement();

        public override Collider AddTo(GameObject go)
        {
            var col = go.AddComponent<CapsuleCollider>();
            if (Capsule != null)
            {
                col.direction = Capsule.DirectionIndex;
                col.radius    = Capsule.Radius;
                col.height    = Capsule.Height;
            }
            if (Position != null)
                col.center = new Vector3(Position.x, Position.y, Position.z);
            return col;
        }
    }

    public class BoxColliderDef : ColliderDefBase
    {
        [XmlElement("Scale")]
        public XmlVector3 Scale = new XmlVector3 { x = 1f, y = 1f, z = 1f };

        public override Collider AddTo(GameObject go)
        {
            var col = go.AddComponent<BoxCollider>();
            if (Position != null)
                col.center = new Vector3(Position.x, Position.y, Position.z);
            if (Scale != null)
                col.size = new Vector3(Scale.x, Scale.y, Scale.z);
            return col;
        }
    }

    public class SphereColliderDef : ColliderDefBase
    {
        [XmlElement("Radius")]
        public float Radius = 0.5f;

        public override Collider AddTo(GameObject go)
        {
            var col = go.AddComponent<SphereCollider>();
            if (Position != null)
                col.center = new Vector3(Position.x, Position.y, Position.z);
            col.radius = Radius;
            return col;
        }
    }

    // <Colliders>
    //   <CapsuleCollider>...</CapsuleCollider>
    //   <BoxCollider>...</BoxCollider>
    // </Colliders>
    public class ProjectileColliderList
    {
        [XmlElement("CapsuleCollider", typeof(CapsuleColliderDef))]
        [XmlElement("BoxCollider",     typeof(BoxColliderDef))]
        [XmlElement("SphereCollider",  typeof(SphereColliderDef))]
        public List<ColliderDefBase> Items = new List<ColliderDefBase>();
    }
}
