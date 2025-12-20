using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bureaucracy
{
    public static class BudgetStats
    {
        //Recorded lastCycle stats for report generation, don't need to store longterm, regenerated also on load
        public static double lastCycleStratCost = 0;
        public static float lastCycleStratPercentageAsMult = 0;
        public static double lastCycleNetBudget = 0;
        //Projected budget figures, updated every time the GUI opens via recalcBudgetFigures()
        public static double projectedStratCost = 0;
        public static float projectedStratPercentageAsMult = 0;
        public static double projectedNetBudget = 0;

        public static void recalcBudgetFigures(bool forPreviousMonth)
        {
            float oldRep = Reputation.Instance.reputation;
            float oldSci = ResearchAndDevelopment.Instance.Science;
            double oldFunds = Funding.Instance.Funds;
            double funding = 0;
            double facilityDebt = 0;
            double wageDebt = 0;
            if (!forPreviousMonth)
            {
                // bootstrap does not need help.
                if (Utilities.Instance.IsBootstrapBudgetCycle) return;
                projectedNetBudget = Utilities.Instance.GetNetBudget("Budget");
                funding = projectedNetBudget;
                funding -= CrewManager.Instance.Bonuses(funding, false);
                facilityDebt = Costs.Instance.GetFacilityMaintenanceCosts();
                wageDebt = Math.Abs(funding + facilityDebt);
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
                    //Restore state
                    Reputation.Instance.SetReputation(oldRep, TransactionReasons.None);
                    ResearchAndDevelopment.Instance.SetScience(oldSci, TransactionReasons.None);
                    Funding.Instance.SetFunds(oldFunds, TransactionReasons.None);
                    if (funding >= 0.0)
                    {
                        projectedNetBudget = (fundsAfter - oldFunds);
                        projectedStratCost = funding - projectedNetBudget;
                        projectedStratPercentageAsMult = (float)(projectedStratCost / Utilities.Instance.GetNetBudget("Budget"));
                    }
                    else
                    {
                        projectedStratCost = funding;
                        projectedStratPercentageAsMult = 1f;
                    }
                }
                else
                {
                    // bootstrap does not need help.
                    if (Utilities.Instance.IsBootstrapBudgetCycle) return;
                    lastCycleNetBudget = Utilities.Instance.GetNetBudget("Budget");
                    funding = lastCycleNetBudget;
                    funding -= CrewManager.Instance.Bonuses(funding, false);
                    facilityDebt = Costs.Instance.GetFacilityMaintenanceCosts();
                    wageDebt = Math.Abs(funding + facilityDebt);
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
                        //Restore state
                        Reputation.Instance.SetReputation(oldRep, TransactionReasons.None);
                        ResearchAndDevelopment.Instance.SetScience(oldSci, TransactionReasons.None);
                        Funding.Instance.SetFunds(oldFunds, TransactionReasons.None);
                        if (funding >= 0.0)
                        {
                            lastCycleNetBudget = (fundsAfter - oldFunds);
                            lastCycleStratCost = funding - lastCycleNetBudget;
                            lastCycleStratPercentageAsMult = (float)(lastCycleStratCost / Utilities.Instance.GetNetBudget("Budget"));
                        }
                        else
                        {
                            lastCycleStratCost = funding;
                            lastCycleStratPercentageAsMult = 1f;
                        }
                    }
                }
            }
        }
    }
}
