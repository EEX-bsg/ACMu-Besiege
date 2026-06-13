using UnityEngine;

namespace ACMu.Core.Maths
{
    /// <summary>
    /// 倍精度3次元ベクトル。拡張EX(ボーダーレスワールド)のワールド座標表現に用いる。
    /// <para>不変条件: 値型であり共有状態を持たない。NaN / Infinity を含む値を契約境界へ渡してはならない。</para>
    /// <para>スレッド制約: なし(イミュータブルな値としての利用を推奨)。</para>
    /// </summary>
    public struct Vector3d
    {
        public double x;
        public double y;
        public double z;

        public Vector3d(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3d Zero
        {
            get { return new Vector3d(0.0, 0.0, 0.0); }
        }

        public double SqrMagnitude
        {
            get { return x * x + y * y + z * z; }
        }

        public double Magnitude
        {
            get { return System.Math.Sqrt(SqrMagnitude); }
        }

        public static Vector3d operator +(Vector3d a, Vector3d b)
        {
            return new Vector3d(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector3d operator -(Vector3d a, Vector3d b)
        {
            return new Vector3d(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vector3d operator *(Vector3d a, double s)
        {
            return new Vector3d(a.x * s, a.y * s, a.z * s);
        }

        public static double Distance(Vector3d a, Vector3d b)
        {
            return (a - b).Magnitude;
        }

        /// <summary>単精度へ縮退変換する。原点から遠い座標では精度が失われるため、通常は IWorldFrame.ToScene を経由すること。</summary>
        public Vector3 ToVector3()
        {
            return new Vector3((float)x, (float)y, (float)z);
        }

        public static Vector3d FromVector3(Vector3 v)
        {
            return new Vector3d(v.x, v.y, v.z);
        }

        public override string ToString()
        {
            return string.Format("({0:F3}, {1:F3}, {2:F3})", x, y, z);
        }
    }
}
