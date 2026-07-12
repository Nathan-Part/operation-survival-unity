using System;

namespace Game.Net
{
    /// Vecteur 3D engine-free (aucune dépendance UnityEngine) : toute la simulation réseau
    /// reste ainsi déterministe et vérifiable hors moteur. La glue Unity convertit vers/depuis
    /// UnityEngine.Vector3 à la frontière (rendu, NavMesh).
    [Serializable]
    public struct NetVector3 : IEquatable<NetVector3>
    {
        public float x;
        public float y;
        public float z;

        public NetVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static NetVector3 Zero => new NetVector3(0f, 0f, 0f);

        public static NetVector3 operator +(NetVector3 a, NetVector3 b) => new NetVector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static NetVector3 operator -(NetVector3 a, NetVector3 b) => new NetVector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static NetVector3 operator *(NetVector3 a, float s) => new NetVector3(a.x * s, a.y * s, a.z * s);

        public float SqrMagnitude => x * x + y * y + z * z;
        public float Magnitude => (float)Math.Sqrt(SqrMagnitude);

        public static float Distance(NetVector3 a, NetVector3 b) => (a - b).Magnitude;
        public static float SqrDistance(NetVector3 a, NetVector3 b) => (a - b).SqrMagnitude;

        public static NetVector3 Lerp(NetVector3 a, NetVector3 b, float t)
        {
            if (t < 0f) t = 0f;
            else if (t > 1f) t = 1f;
            return new NetVector3(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t, a.z + (b.z - a.z) * t);
        }

        public bool Equals(NetVector3 other) => x == other.x && y == other.y && z == other.z;
        public override bool Equals(object obj) => obj is NetVector3 o && Equals(o);
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
        public override string ToString() => $"({x:0.##}, {y:0.##}, {z:0.##})";
    }
}
