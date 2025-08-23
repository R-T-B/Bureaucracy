using System.Linq;
using UnityEngine;

namespace Bureaucracy
{
    public class BudgetManager : Manager
    {
        public BudgetEvent NextBudget;

        // ReSharper disable once UnusedMember.Local
        private Costs costs = new Costs();
        public static BudgetManager Instance;

        public BudgetManager()
        {
            Name = "Budget";
            Instance = this;
            FundingAllocation = 0.4f;
            Debug.Log("[Bureaucracy]: Budget Manager is ready");
        }

        public override Report GetReport()
        {
            return new BudgetReport();
        }

        public override void OnEventCompletedManagerActions(BureaucracyEvent eventCompleted)
        {
            Debug.Log("[Bureaucracy]: Budget Event completed. Setting next budget");
            NextBudget = new BudgetEvent(GetNextBudgetTime(), this, true);
        }

        public double GetNextBudgetTime()
        {
            double time = SettingsClass.Instance.TimeBetweenBudgets;
            time *= FlightGlobals.GetHomeBody().solarDayLength;
            double offset = 0;
            if (NextBudget != null) offset = Planetarium.GetUniversalTime() - NextBudget.CompletionTime;
            time += Planetarium.GetUniversalTime() - offset;
            Debug.Log("[Bureaucracy]: Next Budget at "+time);
            return time;
        }
        
        public void OnLoad(ConfigNode cn)
        {
            Debug.Log("[Bureaucracy]: Budget Manager: OnLoad");
            ConfigNode managerNode = cn.GetNode("BUDGET_MANAGER");
            double nextBudgetTime = GetNextBudgetTime();
            if (managerNode != null)
            {
                bool.TryParse(managerNode.GetValue("IsBootstrapBudgetCycle"), out Utilities.Instance.IsBootstrapBudgetCycle);
                double.TryParse(managerNode.GetValue("ScienceProcessedCurrentCycle"), out Utilities.Instance.ScienceProcessedCurrentCycle);
                double.TryParse(managerNode.GetValue("InitialFunds"), out Utilities.Instance.InitialFunds);
                float.TryParse(managerNode.GetValue("FundingAllocation"), out FundingAllocation);
                double.TryParse(managerNode.GetValue("nextBudget"), out nextBudgetTime);
                CreateNewBudget(nextBudgetTime);
            }
            else
            {
                OnSave(cn);
                managerNode = cn.GetNode("BUDGET_MANAGER");
                Bureaucracy.Instance.YieldAndCreateBudgetOnNewGame();       
            }
            ConfigNode costsNode = managerNode.GetNode("COSTS");
            Costs.Instance.OnLoad(costsNode);
            Debug.Log("[Bureaucracy]: Budget Manager: OnLoad Complete");
        }

        private bool NeedNewKacAlarm()
        {
            if (!SettingsClass.Instance.StopTimeWarp) return false;
            double UT = Planetarium.GetUniversalTime();
            AlarmTypeBase alarmCheck = AlarmClockScenario.GetNextAlarm(UT);
            AlarmTypeBase thisAlarm = alarmCheck;
            while (true)
            {
                try
                {
                    if (thisAlarm.title.Equals("Next Budget"))
                    {
                        return false;
                    }

                    alarmCheck = thisAlarm;
                    thisAlarm = AlarmClockScenario.GetNextAlarm(alarmCheck.ut);
                }
                catch
                {
                    return true;
                }
            }
        }

        public void OnSave(ConfigNode cn)
        {
            Debug.Log("[Bureaucracy]: Budget Manager: OnSave");
            ConfigNode managerNode = new ConfigNode("BUDGET_MANAGER");
            managerNode.SetValue("IsBootstrapBudgetCycle", Utilities.Instance.IsBootstrapBudgetCycle, true);
            managerNode.SetValue("ScienceProcessedCurrentCycle", Utilities.Instance.ScienceProcessedCurrentCycle, true);
            managerNode.SetValue("InitialFunds", Utilities.Instance.InitialFunds, true);
            managerNode.SetValue("FundingAllocation", FundingAllocation, true);
            if (NextBudget != null) managerNode.SetValue("nextBudget", NextBudget.CompletionTime, true);
            cn.AddNode(managerNode);
            Costs.Instance.OnSave(managerNode);
            Debug.Log("[Bureaucracy]: Budget Manager: OnSave Complete");
        }

        public void CreateNewBudget(double budgetTime = 0)
        {
            NextBudget = new BudgetEvent(budgetTime == 0 ? GetNextBudgetTime(): budgetTime, this, NeedNewKacAlarm());
        }

        // called only for a new game save to fire bootstrap budget cycle.
        public void CreateBootstrapBudget(double budgetTime = 0)
        {
            Utilities.Instance.IsBootstrapBudgetCycle = true;
            NextBudget = new BudgetEvent(0, this, false);
        }
    }
}