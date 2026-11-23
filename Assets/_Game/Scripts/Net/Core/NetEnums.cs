using System;

namespace Game.Net
{
    /// Rôle réseau du process local.
    public enum NetworkRole
    {
        Offline = 0,
        Host = 1,
        Client = 2,
    }

    /// État global d'une session coop, arbitré par l'hôte.
    public enum SessionPhase
    {
        Lobby = 0,
        Active = 1,
        GameOver = 2,
    }

    /// Cycle de vie d'un ennemi côté simulation. La glue Unity joue l'anim de mort pendant
    /// Dying, puis appelle CoopSession.RemoveEnemy quand le corps disparaît.
    public enum EnemyLifecycle
    {
        Alive = 0,
        Dying = 1,
        Dead = 2,
    }

    /// Boutons d'entrée d'un joueur, empaquetés dans un octet (client -> hôte).
    [Flags]
    public enum ButtonFlags : byte
    {
        None = 0,
        Jump = 1 << 0,
        Dash = 1 << 1,
        Reload = 1 << 2,
        Fire = 1 << 3,
    }
}
