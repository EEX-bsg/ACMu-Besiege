using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using Modding.Modules;
using Modding.Serialization;

namespace ACMu.Compat.Shooting
{
    /// <summary>マガジン/リロード設定(原ACM互換: AdShootingModule.cs MagazineState, docs/ACM/detailed-design.md 11-8)。</summary>
    public class MagazineState
    {
        [XmlElement("useReloadKey")]
        [DefaultValue(false)]
        public bool UseReloadKey = false;

        [XmlElement("ReloadKey")]
        [DefaultValue(null)]
        public MKeyReference ReloadKey;

        [XmlElement("forceAutoReload")]
        [DefaultValue(false)]
        public bool ForceAutoReload = false;

        [XmlElement("useAutoReloadToggle")]
        [DefaultValue(false)]
        public bool UseAutoReloadToggle = false;

        [XmlElement("AutoReloadToggle")]
        [DefaultValue(null)]
        public MToggleReference AutoReloadToggle;

        [XmlElement("MagazineCapacitySlider")]
        [DefaultValue(null)]
        public MSliderReference MagazineCapacitySlider;

        [XmlElement("ReloadTimeSlider")]
        [DefaultValue(null)]
        public MSliderReference ReloadTimeSlider;

        /// <summary>リロード時に再生するAudioClipのリスト。複数指定するとランダム選択。</summary>
        [XmlArray("ReloadSounds")]
        [XmlArrayItem("AudioClip", typeof(AssetBundleNameRef))]
        [DefaultValue(null)]
        public List<AssetBundleNameRef> ReloadSounds = new List<AssetBundleNameRef>();
    }
}
