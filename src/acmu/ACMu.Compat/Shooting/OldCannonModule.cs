using System.Collections.Generic;
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
        internal const string FuseTimerSliderName   = "fuse-timer";

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

        /// <summary>発射フラッシュエフェクトの位置。未設定なら ProjectileStart と同じ位置を使用。</summary>
        [XmlElement("ShotFlashPosition")]
        public XmlTransform ShotFlashPosition;

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

        /// <summary>useTimefuse=true のときに使用するフューズ時間(秒)。FuseTimerSlider が無い場合のデフォルト値。</summary>
        [XmlElement("FuseTime")]
        public float FuseTime = 3f;

        [XmlElement("useTimefuse")]
        public bool UseTimefuse = false;

        [XmlElement("RecoilMultiplier")]
        public float RecoilMultiplier = 0.6f;

        [XmlElement("RandomInterval")]
        public float RandomInterval = 0.03f;

        [XmlElement("RandomDiffusion")]
        public float RandomDiffusion = 0.01f;

        [XmlElement("useDelay")]
        public bool UseDelay = false;

        [XmlElement("DelayTime")]
        public float DelayTime = 0.05f;

        [XmlElement("useBurstShot")]
        public bool UseBurstShot = false;

        [XmlElement("RateOfBurst")]
        public float RateOfBurst = 4f;

        [XmlElement("BurstShotNum")]
        public int BurstShotNum = 3;

        [XmlElement("DefaultAmmo")]
        public int DefaultAmmo = 0;

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

        /// <summary>発射時に再生するAudioClipのリスト。複数指定するとランダム選択。</summary>
        [XmlArray("Sounds")]
        [XmlArrayItem("AudioClip", typeof(AssetBundleNameRef))]
        public List<AssetBundleNameRef> Sounds = new List<AssetBundleNameRef>();

        /// <summary>爆発時に再生するAudioClipのリスト(原ACM互換: 名前は「着弾音」だが実際は爆発トリガー時に再生される)。</summary>
        [XmlArray("HitSounds")]
        [XmlArrayItem("AudioClip", typeof(AssetBundleNameRef))]
        public List<AssetBundleNameRef> HitSounds = new List<AssetBundleNameRef>();
    }
}
