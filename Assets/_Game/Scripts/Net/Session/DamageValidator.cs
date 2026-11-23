using System.Collections.Generic;

namespace Game.Net
{
    /// Validation autoritaire (côté hôte) des réclamations de dégât client — anti-triche minimal.
    /// La règle : un client dit « j'ai touché », l'hôte décide si c'est plausible avant d'appliquer.
    public class DamageValidator
    {
        public enum Result
        {
            Accepted = 0,
            NonPositive = 1,
            AttackerInvalid = 2,
            UnknownEnemy = 3,
            EnemyDead = 4,
            OutOfRange = 5,
            RateExceeded = 6,
        }

        public float MaxHitRatePerSecond = 20f;  // TODO(coop-balance)
        public float MaxHitDistance = 200f;       // TODO(coop-balance)

        readonly Dictionary<int, float> windowStart = new Dictionary<int, float>();
        readonly Dictionary<int, int> hitsInWindow = new Dictionary<int, int>();

        public DamageValidator(CoopSessionConfig cfg)
        {
            // cfg réservé pour de futurs plafonds tirés des données ; signature stable.
        }

        public Result Validate(DamageClaim claim, CoopPlayerState attacker, EnemyRuntimeState enemy, float now)
        {
            if (claim.amount <= 0f) return Result.NonPositive;
            if (attacker == null || !attacker.IsAlive) return Result.AttackerInvalid;
            if (enemy == null) return Result.UnknownEnemy;
            if (!enemy.IsAlive) return Result.EnemyDead;
            if (NetVector3.Distance(attacker.position, claim.hitPoint) > MaxHitDistance) return Result.OutOfRange;
            if (ExceedsRate(claim.attacker.Id, now)) return Result.RateExceeded;
            return Result.Accepted;
        }

        bool ExceedsRate(int attackerId, float now)
        {
            if (!windowStart.TryGetValue(attackerId, out float start) || now - start >= 1f)
            {
                windowStart[attackerId] = now;
                hitsInWindow[attackerId] = 1;
                return false;
            }
            int count = hitsInWindow.TryGetValue(attackerId, out int c) ? c : 0;
            count++;
            hitsInWindow[attackerId] = count;
            return count > MaxHitRatePerSecond;
        }
    }
}
