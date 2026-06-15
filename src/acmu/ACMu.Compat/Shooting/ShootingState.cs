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
    }
}
