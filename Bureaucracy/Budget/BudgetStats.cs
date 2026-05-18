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
                projectedNetBudget = Utilities.Instance.GetNetBudget("Budget");
                funding = projectedNetBudget + Costs.Instance.GetFacilityMaintenanceCosts() + CrewManager.Instance.Bonuses(Utilities.Instance.GetGrossBudget(false), false);
                wageDebt = CrewManager.Instance.Bonuses(funding, false);
                funding -= wageDebt;
                facilityDebt = Costs.Instance.GetFacilityMaintenanceCosts();
                funding -= facilityDebt;

                if (SettingsClass.Instance.UseItOrLoseIt && funding > Funding.Instance.Funds) Funding.Instance.SetFunds(0.0d, TransactionReasons.Contracts);
                if (!SettingsClass.Instance.UseItOrLoseIt || Funding.Instance.Funds <= 0.0d || funding <= 0.0d || Utilities.Instance.IsBootstrapBudgetCycle)
                {
                    double fundsBefore = Funding.Instance.Funds;
                    Funding.Instance.AddFunds(funding, TransactionReasons.Contracts);
                    double fundsAfter = Funding.Instance.Funds;
                    //Restore state
                    Reputation.Instance.SetReputation(oldRep, TransactionReasons.None);
                    ResearchAndDevelopment.Instance.SetScience(oldSci, TransactionReasons.None);
                    Funding.Instance.SetFunds(oldFunds, TransactionReasons.None);
                    if (funding != 0.0)
                    {
                        projectedNetBudget = (fundsAfter - fundsBefore);
                        projectedStratCost = Math.Abs(funding - (fundsAfter - fundsBefore));
                        projectedStratPercentageAsMult = (float)(projectedStratCost / funding);
                    }
                    else
                    {
                        projectedStratCost = 0;
                        projectedStratPercentageAsMult = 0f;
                    }
                }
            }
            else
            {
                // bootstrap does not need help.
                if (Utilities.Instance.IsBootstrapBudgetCycle)
                {
                    Utilities.Instance.InitialFunds = HighLogic.CurrentGame.Parameters.Career.StartingFunds;
                    BudgetStats.lastCycleNetBudget = Utilities.Instance.InitialFunds;
                }
                else
                {
                    BudgetStats.lastCycleNetBudget = Utilities.Instance.GetNetBudget("Budget");
                }
                funding = lastCycleNetBudget + Costs.Instance.GetFacilityMaintenanceCosts() + CrewManager.Instance.Bonuses(Utilities.Instance.GetGrossBudget(true), false);
                wageDebt = CrewManager.Instance.Bonuses(funding, false);
                funding -= wageDebt;
                facilityDebt = Costs.Instance.GetFacilityMaintenanceCosts();
                funding -= facilityDebt;

                if (SettingsClass.Instance.UseItOrLoseIt && funding > Funding.Instance.Funds) 
                    Funding.Instance.SetFunds(0.0d, TransactionReasons.Contracts);

                if (!SettingsClass.Instance.UseItOrLoseIt || Funding.Instance.Funds <= 0.0d || funding <= 0.0d || Utilities.Instance.IsBootstrapBudgetCycle)
                {
                    double fundsBefore = Funding.Instance.Funds;
                    Funding.Instance.AddFunds(funding, TransactionReasons.Contracts);
                    double fundsAfter = Funding.Instance.Funds;
                    //Restore state
                    Reputation.Instance.SetReputation(oldRep, TransactionReasons.None);
                    ResearchAndDevelopment.Instance.SetScience(oldSci, TransactionReasons.None);
                    Funding.Instance.SetFunds(oldFunds, TransactionReasons.None);
                    if (funding != 0.0)
                    {
                        lastCycleNetBudget = (fundsAfter - fundsBefore);
                        if (Utilities.Instance.IsBootstrapBudgetCycle)
                        {
                            lastCycleStratCost = 0;
                            lastCycleStratPercentageAsMult = 0;
                        }
                        else
                        {
                            lastCycleStratCost = Math.Abs(funding - (fundsAfter - fundsBefore));
                            lastCycleStratPercentageAsMult = (float)(lastCycleStratCost / funding);
                        }

                    }
                    else
                    {
                        lastCycleStratCost = 0;
                        lastCycleStratPercentageAsMult = 0f;
                    }
                }
            }
        }
    }
}
