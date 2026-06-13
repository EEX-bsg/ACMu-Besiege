using ACMu.Core.Maths;
using ACMu.Core.World;
using UnityEngine;

namespace ACMu.Host.Null
{
    internal class NullWorldFrame : IWorldFrame
    {
        public event WorldOriginShiftedHandler OriginShifted { add { } remove { } }

        public Vector3d Origin { get { return Vector3d.Zero; } }

        public Vector3 ToScene(Vector3d worldPosition)
        {
            return worldPosition.ToVector3();
        }

        public Vector3d ToWorld(Vector3 scenePosition)
        {
            return Vector3d.FromVector3(scenePosition);
        }

        public void RequestOriginShift(Vector3d newOrigin) { }
    }
}
