using System;
using Modding.Modules;
using Modding.Serialization;

namespace ACMu.Compat.TestCannon
{
    [Serializable]
    public class TestCannonModule : BlockModule
    {
        internal const string FireKeyName = "acmu-fire";
        internal const string SpeedSliderName = "acmu-speed";

        public MKeyReference FireKey;
        public MSliderReference SpeedSlider;
    }
}
