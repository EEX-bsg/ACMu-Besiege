using System.Xml.Serialization;

namespace ACMu.Compat.Shooting
{
    // <AssetBundleName name="tank_effect" /> に対応
    public class AssetBundleNameRef
    {
        [XmlAttribute("name")]
        public string Name = "";
    }
}
