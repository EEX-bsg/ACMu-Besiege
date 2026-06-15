using System.Xml.Serialization;
using Modding.Modules;
using Modding.Serialization;

namespace ACMu.Compat.Shooting
{
    [XmlRoot("AdShootingProp")]
    public class OldCannonModule : BlockModule
    {
        internal const string FireKeyName           = "fire";
        internal const string PowerSliderName       = "power";
        internal const string RateOfFireSliderName  = "rate-of-fire";
        internal const string HoldToShootToggleName = "hold-to-shoot";

        [XmlElement("ShootingState")]
        public ShootingState Shooting = new ShootingState();

        [XmlElement("FireKey")]
        public MKeyReference FireKey;

        [XmlElement("PowerSlider")]
        public MSliderReference PowerSlider;

        [XmlElement("RateOfFireSlider")]
        public MSliderReference RateOfFireSlider;

        [XmlElement("AssetBundleName")]
        public AssetBundleNameRef AssetBundleName = new AssetBundleNameRef();

        [XmlElement("ProjectileStart")]
        public XmlTransform ProjectileStart = new XmlTransform();

        [XmlElement("ProjectilesExplode")]
        public bool ProjectilesExplode = true;

        [XmlElement("ExplodeRadius")]
        public float ExplodeRadius = 3f;

        [XmlElement("ExplodePower")]
        public float ExplodePower = 10f;

        [XmlElement("ExplodeUpPower")]
        public float ExplodeUpPower = 0f;

        [XmlElement("ProjectilesDespawnImmediately")]
        public bool ProjectilesDespawnImmediately = false;

        [XmlElement("FuseDelayTime")]
        public float FuseDelayTime = 0f;

        [XmlElement("RandomFuseInterval")]
        public float RandomFuseInterval = 0f;

        [XmlElement("RecoilMultiplier")]
        public float RecoilMultiplier = 0.6f;

        [XmlElement("RandomInterval")]
        public float RandomInterval = 0.03f;

        [XmlElement("RandomDiffusion")]
        public float RandomDiffusion = 0.01f;

        [XmlElement("DefaultAmmo")]
        public int DefaultAmmo = 10;

        [XmlElement("PoolSize")]
        public int PoolSize = 100;

        [XmlElement("ExplodeEffect")]
        public string ExplodeEffect = "";

        [XmlElement("ShotFlashEffect")]
        public string ShotFlashEffect = "";

        [XmlElement("TrailEffect")]
        public string TrailEffect = "";

        [XmlElement("BulletEffect")]
        public string BulletEffect = "";
    }
}
