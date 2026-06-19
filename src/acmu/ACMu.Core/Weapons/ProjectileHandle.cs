using System;

namespace ACMu.Core.Weapons
{
    /// <summary>
    /// 弾体の同期識別子。Id はホストが発番し、全ピアで同一の弾を指す。
    /// <para>不変条件: Id = 0 は常に無効(IsValid = false)。発番は単調増加で再利用しない。</para>
    /// <para>スレッド制約: なし(イミュータブル値型)。</para>
    /// </summary>
    public struct ProjectileHandle : IEquatable<ProjectileHandle>
    {
        public readonly int Id;
        public readonly int OwnerPlayerId;

        public ProjectileHandle(int id, int ownerPlayerId)
        {
            Id = id;
            OwnerPlayerId = ownerPlayerId;
        }

        public bool IsValid
        {
            get { return Id != 0; }
        }

        public static ProjectileHandle Invalid
        {
            get { return new ProjectileHandle(0, 0); }
        }

        public bool Equals(ProjectileHandle other)
        {
            // OwnerPlayerId を含めない: クライアントがプロキシを受信する際に owner=0 で再構築するケースがあり、
            // 同一弾体でも OwnerPlayerId が片側では 0 になることがある。Id のみが弾体の同一性を表す。
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is ProjectileHandle && Equals((ProjectileHandle)obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public static bool operator ==(ProjectileHandle a, ProjectileHandle b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ProjectileHandle a, ProjectileHandle b)
        {
            return !a.Equals(b);
        }

        public override string ToString()
        {
            return string.Format("Projectile#{0}(owner={1})", Id, OwnerPlayerId);
        }
    }
}
