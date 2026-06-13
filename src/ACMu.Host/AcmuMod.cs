using Modding;

namespace ACMu.Host
{
    public class AcmuMod : ModEntryPoint
    {
        public override void OnLoad()
        {
            AcmuCoreBootstrap.Initialize();
        }
    }
}
