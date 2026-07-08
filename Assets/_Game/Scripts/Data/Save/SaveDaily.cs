using System.Collections.Generic;

namespace Game.Data.Save
{
    /// Contrats quotidiens (web #62) : état persistant — seed du jour tiré et
    /// progression des 3 contrats. Piloté par DailyContractRules.
    public class SaveDaily
    {
        public long daily_seed;
        public List<SaveContract> contracts = new List<SaveContract>();
    }

    public class SaveContract
    {
        public string id;
        public int progress;
        public bool done;
    }
}
