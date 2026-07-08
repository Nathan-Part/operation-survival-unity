using System.Collections.Generic;
using Game.Data.Save;

namespace Game.Data.Rules
{
    /// Contrats quotidiens (web #62) : 3 tirés déterministement d'un pool par un
    /// seed de date, reset au changement de jour.
    public static class DailyContractRules
    {
        public static long SeedFor(int year, int month, int day) => year * 10000L + month * 100L + day;

        public static List<string> Pick(long seed, IReadOnlyList<ContractData> pool, int count = 3)
        {
            var remaining = new List<ContractData>(pool);
            var picked = new List<string>();
            long s = seed;
            for (int i = 0; i < count && remaining.Count > 0; i++)
            {
                s = s * 1664525L + 1013904223L;                       // web: seededPick LCG
                int idx = (int)((s & 0x7fffffff) % remaining.Count);
                picked.Add(remaining[idx].id);
                remaining.RemoveAt(idx);
            }
            return picked;
        }

        public static void EnsureToday(SaveDaily daily, long seed, IReadOnlyList<ContractData> pool)
        {
            if (daily.daily_seed == seed && daily.contracts.Count > 0) return;
            daily.daily_seed = seed;
            daily.contracts.Clear();
            foreach (var id in Pick(seed, pool)) daily.contracts.Add(new SaveContract { id = id });
        }

        public static ContractData Advance(SaveDaily daily, IReadOnlyList<ContractData> pool, string type, int amount)
        {
            ContractData justDone = null;
            foreach (var sc in daily.contracts)
            {
                if (sc.done) continue;
                var def = Find(pool, sc.id);
                if (def == null || def.type != type) continue;
                sc.progress += amount;
                if (sc.progress >= def.target) { sc.done = true; justDone = def; }
            }
            return justDone;
        }

        static ContractData Find(IReadOnlyList<ContractData> pool, string id)
        {
            foreach (var c in pool) if (c.id == id) return c;
            return null;
        }
    }
}
