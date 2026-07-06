using System;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public class DashSettings { public float distance; public float speed; public float cooldown; }

    [Serializable]
    public class StaminaSettings { public float max; public float drainPerSec; public float regenPerSec; public float minToRun; }

    [Serializable]
    public class FallDamageSettings { public float safeHeight; public float damagePerMeter; public float maxDamage; }

    [CreateAssetMenu(menuName = "Game/Config/Movement Config", fileName = "MovementConfig")]
    public class MovementConfig : ScriptableObject
    {
        public float walkSpeed;
        public float runSpeed;
        public float jumpSpeed;
        public float gravity;
        public float turnLerp;
        public float stepUpMax;
        public float stepDownSnap;
        public DashSettings dash;
        public StaminaSettings stamina;
        public FallDamageSettings fallDamage;
        public float secondWindInvulnSeconds;
        public int secondWindUsesPerRun;
    }
}
