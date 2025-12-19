using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bureaucracy
{
    public static class BudgetStats
    {
        public static double lastCycleStratCost = 0;
        public static float lastCycleStratPercentageAsMult = 0;
        public static double lastMonthsTotalNetBudget = 0;
        public static void recalcBudgetFigures()
        {
            float oldRep = Reputation.Instance.reputation;
            float oldSci = ResearchAndDevelopment.Instance.Science;
            double oldFunds = Funding.Instance.Funds;
            // bootstrap does not need help.
            if (Utilities.Instance.IsBootstrapBudgetCycle) return;
            lastMonthsTotalNetBudget = Utilities.Instance.GetNetBudget("Budget");
            double funding = lastMonthsTotalNetBudget;
            funding -= CrewManager.Instance.Bonuses(funding, true);
            double facilityDebt = Costs.Instance.GetFacilityMaintenanceCosts();
            double wageDebt = Math.Abs(funding + facilityDebt);
            if (funding <= 0)
            {
                //pay wages first then facilities
                Utilities.Instance.PayWageDebt(wageDebt, true);
                Utilities.Instance.PayFacilityDebt(facilityDebt, wageDebt, true);
            }

            // if running bootstrap cycle, abort
            if (Utilities.Instance.IsBootstrapBudgetCycle)
                return;

            if (SettingsClass.Instance.UseItOrLoseIt && funding > Funding.Instance.Funds) Funding.Instance.SetFunds(0.0d, TransactionReasons.Contracts);
            if (!SettingsClass.Instance.UseItOrLoseIt || Funding.Instance.Funds <= 0.0d || funding <= 0.0d || Utilities.Instance.IsBootstrapBudgetCycle)
            {
                Funding.Instance.AddFunds(funding, TransactionReasons.Contracts);
                double fundsAfter = Funding.Instance.Funds;
                if (funding >= 0.0)
                {
                    lastMonthsTotalNetBudget = (fundsAfter - oldFunds);
                    lastCycleStratCost = funding - lastMonthsTotalNetBudget;
                    lastCycleStratPercentageAsMult = (float)(lastCycleStratCost / funding);
                }
                else
                {
                    lastCycleStratCost = funding;
                    lastCycleStratPercentageAsMult = 1f;
                }
                Reputation.Instance.SetReputation(oldRep, TransactionReasons.None);
                ResearchAndDevelopment.Instance.SetScience(oldSci, TransactionReasons.None);
                Funding.Instance.SetFunds(oldFunds,TransactionReasons.None);
            }
        }
    }
}
