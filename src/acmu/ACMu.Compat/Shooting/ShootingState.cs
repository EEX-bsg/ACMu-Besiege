using System.Xml.Serialization;

namespace ACMu.Compat.Shooting
{
    public class ShootingState
    {
        [XmlElement("Mass")]          public float Mass         = 0.4f;
        [XmlElement("Drag")]          public float Drag         = 0f;
        [XmlElement("AngularDrag")]   public float AngularDrag  = 5f;
        [XmlElement("IgnoreGravity")] public bool  IgnoreGravity = false;
        [XmlElement("EntityDamage")]  public float EntityDamage = 100f;
        [XmlElement("BlockDamage")]   public float BlockDamage  = 1f;
        [XmlElement("Attaches")]      public bool  Attaches     = false;
        [XmlElement("CollisionTypeS")] public string CollisionTypeS = "ContinuousDynamic";

        /// <summary>弾頭メッシュ名。ModResource(Mod.xml に登録)を優先参照し、なければ AssetBundle から取得。<Mesh name="cannon_ball" /> 形式。省略なら既定球体。</summary>
        [XmlElement("Mesh")]    public AssetBundleNameRef Mesh    = new AssetBundleNameRef();

        /// <summary>弾頭テクスチャ名。ModResource(Mod.xml に登録)を優先参照し、なければ AssetBundle から取得。<Texture name="cannon_tex" /> 形式。省略なら既定マテリアル。</summary>
        [XmlElement("Texture")] public AssetBundleNameRef Texture = new AssetBundleNameRef();
    }
}
