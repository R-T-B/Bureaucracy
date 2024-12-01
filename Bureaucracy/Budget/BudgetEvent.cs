
using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Bureaucracy
{
    public class BudgetEvent : BureaucracyEvent
    {
        public readonly float MonthLength;
        public BudgetEvent(double budgetTime, BudgetManager manager, bool newKacAlarm)
        {
            MonthLength = SettingsClass.Instance.TimeBetweenBudgets;
            CompletionTime = budgetTime;
            Name = "Next Budget";
            ParentManager = manager;
            if(newKacAlarm) Utilities.Instance.NewStockAlarm("Next Budget", "Next Budget", CompletionTime);
            StopTimewarpOnCompletion = true;
            AddTimer();
        }

        public override void OnEventCompleted()
        {
            // if this is not the bootstrap budget cycle, then undo the flag
            if (CompletionTime > 0)
                Utilities.Instance.IsBootstrapBudgetCycle = false;
            
            Debug.Log("Bureaucracy]: OnBudgetAboutToFire");
            //Allows other Managers to do pre-budget work, as once the budget is done alot of stuff gets reset.
            InternalListeners.OnBudgetAboutToFire.Fire();
            RepDecay repDecay = new RepDecay();
            repDecay.ApplyHardMode();

            // save Initial funds value for processing bootstrap cycle
            if (Utilities.Instance.IsBootstrapBudgetCycle) Utilities.Instance.InitialFunds = Funding.Instance.Funds;

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

            // if running bootstrap cycle, zero current funds before awarding initial surplus from calculations
            if (Utilities.Instance.IsBootstrapBudgetCycle)
                Funding.Instance.SetFunds(0, TransactionReasons.None);

            if(SettingsClass.Instance.UseItOrLoseIt && funding > Funding.Instance.Funds) Funding.Instance.SetFunds(0.0d, TransactionReasons.Contracts);
            if(!SettingsClass.Instance.UseItOrLoseIt || Funding.Instance.Funds <= 0.0d || funding <= 0.0d || Utilities.Instance.IsBootstrapBudgetCycle) Funding.Instance.AddFunds(funding, TransactionReasons.Contracts);
            Debug.Log("[Bureaucracy]: OnBudgetAwarded. Awarding "+funding+" Costs: "+facilityDebt);
            InternalListeners.OnBudgetAwarded.Fire(funding, facilityDebt);
            Costs.Instance.ResetLaunchCosts();
            repDecay.ApplyRepDecay(Bureaucracy.Instance.settings.RepDecayPercent);
            

            //stringbuilder for budget report
            if (!Utilities.Instance.IsBootstrapBudgetCycle)
            {
                StringBuilder reportBuilder = new StringBuilder();
            for (int i = 0; i < Bureaucracy.Instance.registeredManagers.Count; i++)
            {
                Manager m = Bureaucracy.Instance.registeredManagers.ElementAt(i);
                m.ThisMonthsBudget = Utilities.Instance.GetNetBudget(m.Name);

                // build cycle report for the manager
                var r = m.GetReport();
                reportBuilder.AppendLine(r.ReportTitle.ToUpper().Replace("REPORT", "DETAILS"));
                reportBuilder.AppendLine("==================================");
                reportBuilder.AppendLine(r.ReportBody());
                reportBuilder.AppendLine(String.Empty);
            }

            // show budget cycle window
            UiController.Instance.BudgetCycleReportWindow(reportBuilder.ToString());
            }
            
            InformParent();
        }
        
    }
}