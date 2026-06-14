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
    }
}
