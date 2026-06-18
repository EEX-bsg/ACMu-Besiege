using System.Collections.Generic;
using System.ComponentModel;
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
        internal const string ReloadKeyName             = "reload";
        internal const string AutoReloadToggleName      = "auto-reload";
        internal const string MagazineCapacitySliderName = "magazine-capacity";
        internal const string ReloadTimeSliderName        = "reload-time";
        internal const string ThrustDelayTimerSliderName  = "thrust-delay-timer";
        internal const string MassSliderName              = "bullet-mass";

        [XmlElement("ShootingState")]
        public ShootingState Shooting = new ShootingState();

        [XmlElement("MassSlider")]
        [DefaultValue(null)]
        public MSliderReference MassSlider;

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
        [DefaultValue(false)]
        public bool ProjectilesExplode = false;

        [XmlElement("ExplodeRadius")]
        [DefaultValue(3f)]
        public float ExplodeRadius = 3f;

        [XmlElement("ExplodePower")]
        [DefaultValue(10f)]
        public float ExplodePower = 10f;

        [XmlElement("ExplodeUpPower")]
        [DefaultValue(0f)]
        public float ExplodeUpPower = 0f;

        [XmlElement("ProjectilesDespawnImmediately")]
        [DefaultValue(false)]
        public bool ProjectilesDespawnImmediately = false;

        [XmlElement("FuseDelayTime")]
        [DefaultValue(0f)]
        public float FuseDelayTime = 0f;

        [XmlElement("RandomFuseInterval")]
        [DefaultValue(0f)]
        public float RandomFuseInterval = 0f;

        [XmlElement("FuseTime")]
        [DefaultValue(0f)]
        public float FuseTime = 0f;

        [XmlElement("useTimefuse")]
        [DefaultValue(false)]
        public bool UseTimefuse = false;

        [XmlElement("RecoilMultiplier")]
        [DefaultValue(0.6f)]
        public float RecoilMultiplier = 0.6f;

        [XmlElement("RandomInterval")]
        [DefaultValue(0.03f)]
        public float RandomInterval = 0.03f;

        [XmlElement("RandomDiffusion")]
        [DefaultValue(0.01f)]
        public float RandomDiffusion = 0.01f;

        [XmlElement("useDelay")]
        [DefaultValue(false)]
        public bool UseDelay = false;

        [XmlElement("DelayTime")]
        [DefaultValue(0f)]
        public float DelayTime = 0f;

        [XmlElement("useBurstShot")]
        [DefaultValue(false)]
        public bool UseBurstShot = false;

        [XmlElement("RateOfBurst")]
        [DefaultValue(0f)]
        public float RateOfBurst = 0f;

        [XmlElement("BurstShotNum")]
        [DefaultValue(0)]
        public int BurstShotNum = 0;

        [XmlElement("DefaultAmmo")]
        public int DefaultAmmo = 0;

        [XmlElement("useMagazine")]
        [DefaultValue(false)]
        public bool UseMagazine = false;

        [XmlElement("MagazineInfo")]
        [DefaultValue(null)]
        public MagazineState MagazineInfo = null;

        /// <summary>ブースター機能を有効化するフラグ。trueで発射時にPurgeVector/PurgePowerによるパージ推力が掛かる。docs/XML/ACMモジュール.xml 254行。</summary>
        [XmlElement("useBooster")]
        [DefaultValue(false)]
        public bool UseBooster = false;

        /// <summary>ブースター起動遅延を有効化するフラグ。trueのときのみ、パージ後ThrustDelayTimerSlider秒後に再着火する。falseならパージのみで再着火は発生しない。docs/XML/ACMモジュール.xml 218行。</summary>
        [XmlElement("useThrustDelayTimer")]
        [DefaultValue(false)]
        public bool UseThrustDelayTimer = false;

        [XmlElement("ThrustDelayTimerSlider")]
        [DefaultValue(null)]
        public MSliderReference ThrustDelayTimerSlider;

        /// <summary>ブースター推力の方向(弾体ローカル空間)。null の場合は実行時に Vector3.forward にフォールバックする。</summary>
        [XmlElement("PurgeVector")]
        [DefaultValue(null)]
        public XmlVector3 PurgeVector = null;

        /// <summary>パージ時/再着火時、両方で使用する推力。個別の着火推力キーは原ACM側で未確認のため共用する。</summary>
        [XmlElement("PurgePower")]
        [DefaultValue(0f)]
        public float PurgePower = 0f;

        /// <summary>着弾時に対象ブロック(と子ブロック)を再帰的に凍結する。</summary>
        [XmlElement("useFreezingAttack")]
        [DefaultValue(false)]
        public bool UseFreezingAttack = false;

        [XmlElement("PoolSize")]
        public int PoolSize = 100;

        [XmlElement("ExplodeEffect")]
        public string ExplodeEffect = "";

        [XmlElement("ShotFlashEffect")]
        [DefaultValue(null)]
        [CanBeEmpty]
        public string ShotFlashEffect = "";

        [XmlElement("TrailEffect")]
        [DefaultValue(null)]
        [CanBeEmpty]
        public string TrailEffect = "";

        [XmlElement("BulletEffect")]
        [DefaultValue(null)]
        [CanBeEmpty]
        public string BulletEffect = "";

        /// <summary>発射時に再生するAudioClipのリスト。複数指定するとランダム選択。</summary>
        [XmlArray("Sounds")]
        [XmlArrayItem("AudioClip", typeof(AssetBundleNameRef))]
        [DefaultValue(null)]
        public List<AssetBundleNameRef> Sounds = new List<AssetBundleNameRef>();

        /// <summary>爆発時に再生するAudioClipのリスト(原ACM互換: 名前は「着弾音」だが実際は爆発トリガー時に再生される)。</summary>
        [XmlArray("HitSounds")]
        [XmlArrayItem("AudioClip", typeof(AssetBundleNameRef))]
        [DefaultValue(null)]
        public List<AssetBundleNameRef> HitSounds = new List<AssetBundleNameRef>();
    }
}
