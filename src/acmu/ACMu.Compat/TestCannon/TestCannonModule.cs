using System.Xml.Serialization;
using Modding.Modules;
using Modding.Serialization;

namespace ACMu.Compat.TestCannon
{
    [XmlRoot("AcmuTestCannon")]
    public class TestCannonModule : BlockModule
    {
        internal const string FireKeyName = "acmu-fire";
        internal const string SpeedSliderName = "acmu-speed";

        [XmlElement("FireKey")]
        public MKeyReference FireKey;

        [XmlElement("SpeedSlider")]
        public MSliderReference SpeedSlider;

        // ブロックローカル座標での発射原点オフセット (m)
        [XmlElement("MuzzleOffsetX")] public float MuzzleOffsetX = 0f;
        [XmlElement("MuzzleOffsetY")] public float MuzzleOffsetY = 0f;
        [XmlElement("MuzzleOffsetZ")] public float MuzzleOffsetZ = 0f;

        // ブロックローカル座標での発射方向ベクトル (正規化不要。ゼロなら transform.forward)
        [XmlElement("MuzzleForwardX")] public float MuzzleForwardX = 0f;
        [XmlElement("MuzzleForwardY")] public float MuzzleForwardY = 0f;
        [XmlElement("MuzzleForwardZ")] public float MuzzleForwardZ = 1f;
    }
}
