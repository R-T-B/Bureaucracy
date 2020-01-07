
using System;
using UnityEngine;

namespace Bureaucracy
{
    public class BudgetEvent : BureaucracyEvent
    {
        public float monthLength;
        public BudgetEvent(double budgetTime, BudgetManager manager, bool newKacAlarm)
        {
            monthLength = SettingsClass.Instance.TimeBetweenBudgets;
            CompletionTime = budgetTime;
            Name = "Next Budget";
            ParentManager = manager;
            if(newKacAlarm) Utilities.Instance.NewKacAlarm("Next Budget", CompletionTime);
            AddTimer();
        }

        public override void OnEventCompleted()
        {
            Debug.Log("Bureaucracy]: OnBudgetAboutToFire");
            InternalListeners.OnBudgetAboutToFire.Fire();
            RepDecay repDecay = new RepDecay();
            repDecay.ApplyHardMode();
            double funding = Utilities.Instance.GetNetBudget("Budget");
            funding -= CrewManager.Instance.Bonuses(funding, true);
            double facilityDebt = Costs.Instance.GetFacilityMaintenanceCosts();
            double wageDebt = Math.Abs(funding + facilityDebt);
            if (funding < 0)
            {
                Debug.Log("[Bureaucracy]: Funding < 0. Paying debts");
                //pay wages first then facilities
                Utilities.Instance.PayWageDebt(wageDebt);
                Utilities.Instance.PayFacilityDebt(facilityDebt, wageDebt);
            }
            CrewManager.Instance.ProcessUnhappyCrew();
            if(SettingsClass.Instance.UseItOrLoseIt && funding > Funding.Instance.Funds) Funding.Instance.SetFunds(0.0d, TransactionReasons.Contracts);
            if(!SettingsClass.Instance.UseItOrLoseIt || Funding.Instance.Funds <= 0.0d || funding <= 0.0d) Funding.Instance.AddFunds(funding, TransactionReasons.Contracts);
            Debug.Log("[Bureaucracy]: OnBudgetAwarded. Awarding "+funding+" Costs: "+facilityDebt);
            InternalListeners.OnBudgetAwarded.Fire(funding, facilityDebt);
            Costs.Instance.ResetLaunchCosts();
            repDecay.ApplyRepDecay(Bureaucracy.Instance.settings.RepDecayPercent);
            InformParent();
        }
        
    }
}