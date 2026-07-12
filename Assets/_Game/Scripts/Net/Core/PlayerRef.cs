using System;

namespace Game.Net
{
    /// Identité réseau d'un joueur. Id 0 = invalide (None). Comparable et sérialisable ;
    /// l'hôte est un joueur comme les autres (il porte aussi un PlayerRef).
    [Serializable]
    public struct PlayerRef : IEquatable<PlayerRef>
    {
        public int Id;

        public PlayerRef(int id) { Id = id; }

        public static PlayerRef None => new PlayerRef(0);
        public bool IsValid => Id != 0;

        public bool Equals(PlayerRef other) => Id == other.Id;
        public override bool Equals(object obj) => obj is PlayerRef o && Equals(o);
        public override int GetHashCode() => Id;
        public override string ToString() => IsValid ? $"P{Id}" : "P-none";

        public static bool operator ==(PlayerRef a, PlayerRef b) => a.Id == b.Id;
        public static bool operator !=(PlayerRef a, PlayerRef b) => a.Id != b.Id;
    }
}
