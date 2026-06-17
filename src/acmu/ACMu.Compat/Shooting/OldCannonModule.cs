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

        /// <summary>useTimefuse=true のときに使用するフューズ時間(秒)。0 の場合は実行時に 3f にフォールバック。[DefaultValue] で省略可能化(ETCM 互換)。</summary>
        [XmlElement("FuseTime")]
        [DefaultValue(0f)]
        public float FuseTime = 0f;

        [XmlElement("useTimefuse")]
        [DefaultValue(false)]
        public bool UseTimefuse = false;

        [XmlElement("RecoilMultiplier")]
        public float RecoilMultiplier = 0.6f;

        [XmlElement("RandomInterval")]
        public float RandomInterval = 0.03f;

        [XmlElement("RandomDiffusion")]
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
        public MSliderReference ThrustDelayTimerSlider;

        /// <summary>ブースター推力の方向(弾体ローカル空間)。null の場合は実行時に Vector3.forward にフォールバックする。</summary>
        [XmlElement("PurgeVector")]
        public XmlVector3 PurgeVector = null;

        /// <summary>パージ時/再着火時、両方で使用する推力。個別の着火推力キーは原ACM側で未確認のため共用する。</summary>
        [XmlElement("PurgePower")]
        [DefaultValue(0f)]
        public float PurgePower = 0f;

        /// <summary>着弾時に対象ブロック(と子ブロック)を再帰的に凍結する。docs/XML/ACMモジュール.xml 287行で確認済み。</summary>
        [XmlElement("useFreezingAttack")]
        [DefaultValue(false)]
        public bool UseFreezingAttack = false;

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
