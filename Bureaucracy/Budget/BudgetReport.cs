using System;

namespace Bureaucracy
{
    public class BudgetReport : Report
    {
        public BudgetReport()
        {
            ReportTitle = "Budget Report";
        }

        public override string ReportBody()
        {
            ReportBuilder.Clear();
            ReportBuilder.AppendLine($"Gross Budget: {Utilities.Instance.FundsSymbol}{Utilities.Instance.GetGrossBudget()}");
            ReportBuilder.AppendLine($"Staff Wages: {Utilities.Instance.FundsSymbol}{Costs.Instance.GetWageCosts()}");
            ReportBuilder.AppendLine($"Facility Maintenance Costs: {Utilities.Instance.FundsSymbol}{Costs.Instance.GetFacilityMaintenanceCosts()}");
            ReportBuilder.AppendLine($"Launch Costs: {Utilities.Instance.FundsSymbol}{Costs.Instance.GetLaunchCosts()}");
            ReportBuilder.AppendLine($"Total Maintenance Costs: {Utilities.Instance.FundsSymbol}{Costs.Instance.GetTotalMaintenanceCosts()}");
            ReportBuilder.AppendLine($"Mission Bonuses: {Utilities.Instance.FundsSymbol}{CrewManager.Instance.LastBonus}");
            ReportBuilder.AppendLine($"Construction Department: {Utilities.Instance.FundsSymbol}{FacilityManager.Instance.GetAllocatedFunding()}");
            ReportBuilder.AppendLine($"Research Department: {Utilities.Instance.FundsSymbol}{ResearchManager.Instance.GetAllocatedFunding()}");
            double netBudget = Utilities.Instance.GetNetBudget("Budget");
            ReportBuilder.AppendLine($"Net Budget: {Utilities.Instance.FundsSymbol}{Math.Max(0, netBudget)}");
            if (netBudget > 0 && netBudget < Funding.Instance.Funds) ReportBuilder.AppendLine("We can't justify extending your funding");
            // ReSharper disable once InvertIf
            if (netBudget < 0)
            {
                ReportBuilder.AppendLine("The budget didn't fully cover your space programs costs.");
                ReportBuilder.Append($"A penalty of {Utilities.Instance.FundsSymbol}{Math.Round(netBudget, 0)} will be applied");
            }
            return ReportBuilder.ToString();
        }
    }
}