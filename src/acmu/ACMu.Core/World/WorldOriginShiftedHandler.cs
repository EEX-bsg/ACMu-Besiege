using ACMu.Core.Maths;

namespace ACMu.Core.World
{
    /// <summary>原点シフト通知のデリゲート。previousOrigin / newOrigin はワールド座標(double)。</summary>
    public delegate void WorldOriginShiftedHandler(Vector3d previousOrigin, Vector3d newOrigin);
}
