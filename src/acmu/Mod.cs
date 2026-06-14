using Modding;
using ACMu.Host;

namespace ACMu
{
	/// <summary>Modロード起動点。Besiegeが起動時に OnLoad() を一度だけ呼び出す。</summary>
	public class Mod : ModEntryPoint
	{
		public override void OnLoad()
		{
			AcmuCoreBootstrap.Initialize();
		}
	}
}
