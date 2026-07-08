using UnityEngine;

namespace Game.Data
{
    /// Contrat quotidien (web #62, main.js:3131-3160). type + target pilotent
    /// DailyContractRules.Advance — pas de logique en dur par contrat.
    [CreateAssetMenu(menuName = "Game/Contract Data", fileName = "ContractData")]
    public class ContractData : ScriptableObject
    {
        public string id;
        [TextArea] public string desc;
        public string type;
        public int target;
        public int reward;
    }
}
