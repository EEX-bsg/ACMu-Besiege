using System.Collections.Generic;
using System.Xml.Serialization;

namespace ACMu.Compat.Shooting
{
    public class ShootingState
    {
        [XmlElement("Mass")]          public float Mass          = 0.4f;
        [XmlElement("Drag")]          public float Drag          = 0f;
        [XmlElement("AngularDrag")]   public float AngularDrag   = 5f;
        [XmlElement("IgnoreGravity")] public bool  IgnoreGravity = false;
        [XmlElement("EntityDamage")]  public float EntityDamage  = 100f;
        [XmlElement("BlockDamage")]   public float BlockDamage   = 1f;
        [XmlElement("Attaches")]      public bool  Attaches      = false;
        [XmlElement("CollisionTypeS")] public string CollisionTypeS = "ContinuousDynamic";

        // PhysicMaterial — bounce
        [XmlElement("BounceCombineType")] public string BounceCombineType = "Average";
        [XmlElement("BounceStr")]         public float  BounceStr         = 0f;

        // PhysicMaterial — friction
        [XmlElement("FrictionCombineType")] public string FrictionCombineType = "Average";
        [XmlElement("FrictionStr")]         public float  FrictionStr         = 0f;

        /// <summary>弾頭メッシュ名と位置オフセット。ModResource優先、なければAssetBundleから取得。</summary>
        [XmlElement("Mesh")]
        public MeshTransformRef Mesh = new MeshTransformRef();

        /// <summary>弾頭テクスチャ名。ModResource優先、なければAssetBundleから取得。<Texture name="..." /> 形式。</summary>
        [XmlElement("Texture")]
        public AssetBundleNameRef Texture = new AssetBundleNameRef();

        /// <summary>弾頭に付与するカスタムコライダー群。未設定なら既定球体コライダーを使用。</summary>
        [XmlElement("Colliders")]
        public ProjectileColliderList CollidersConfig = new ProjectileColliderList();

        public List<ColliderDefBase> ProjectileColliders
        {
            get { return CollidersConfig != null ? CollidersConfig.Items : null; }
        }
    }
}
