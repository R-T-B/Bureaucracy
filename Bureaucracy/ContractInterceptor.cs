using Contracts;
using System.Linq;
using UnityEngine;

namespace Bureaucracy
{
    //Classic Contract Interceptor, not much has changed.
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class ContractInterceptor : MonoBehaviour
    {
        public static ContractInterceptor Instance;
        public static bool ContractsAlreadyProcessed = false;

        protected void Awake()
        {
            DontDestroyOnLoad(this);
            Instance = this;
            ContractConfiguratorBridge.Initialize();
        }

        public System.Collections.IEnumerator ProcessContractList()
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER || ContractsAlreadyProcessed)
                yield break;

            for (int i = 0; i <= 5; i++)
                yield return new WaitForEndOfFrame();

            foreach (var contract in ContractSystem.Instance.Contracts)
                ProcessContract(contract);
            

            foreach (var contract in ContractConfiguratorBridge.GetActiveContracts())            
                ProcessContract(contract);
            

            Debug.Log("[Bureaucracy] Finished processing contracts.");
            ContractsAlreadyProcessed = true;
        }

        public void ProcessContract(Contract contract)
        {
            if (!SettingsClass.Instance.ContractInterceptor) return;
            if (contract.FundsCompletion <= 0) return;
            //Set Failure Penalty to Advance - Failure Rep.
            float rep = (float)contract.FundsAdvance / 10000 * -1 - (float)contract.FundsFailure / 10000;
            contract.FundsFailure = 0;
            contract.ReputationFailure = rep - contract.ReputationFailure;
            //Convert rewards to Rep @ 1:10000 Ratio
            rep = (float)contract.FundsAdvance / 10000 + (float)contract.FundsCompletion / 10000;
            for (int i = 0; i < contract.AllParameters.Count(); i++)
            {
                ContractParameter p = contract.AllParameters.ElementAt(i);
                rep += (float)p.FundsCompletion / 10000;
                p.FundsCompletion = 0;
            }
            contract.ReputationCompletion += rep;
            if (contract.ReputationCompletion < 1) contract.ReputationCompletion = 1;
            contract.FundsAdvance = 0;
            contract.FundsCompletion = 0;
        }
    }
}
