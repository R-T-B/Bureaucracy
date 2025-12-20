using System;
using System.Globalization;

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
            ReportBuilder.AppendLine($"Gross Budget: {Utilities.Instance.FundsSymbol}{Utilities.Instance.GetGrossBudget().ToString("N0", CultureInfo.CurrentCulture)}");
            ReportBuilder.AppendLine($"Staff Wages: {Utilities.Instance.FundsSymbol}{Costs.Instance.GetWageCosts().ToString("N0", CultureInfo.CurrentCulture)}");
            ReportBuilder.AppendLine($"Facility Maintenance Costs: {Utilities.Instance.FundsSymbol}{Costs.Instance.GetFacilityMaintenanceCosts().ToString("N0", CultureInfo.CurrentCulture)}");
            ReportBuilder.AppendLine($"Launch Costs: {Utilities.Instance.FundsSymbol}{Costs.Instance.GetLaunchCosts().ToString("N0", CultureInfo.CurrentCulture)}");
            ReportBuilder.AppendLine($"Total Maintenance Costs: {Utilities.Instance.FundsSymbol}{Costs.Instance.GetTotalMaintenanceCosts().ToString("N0", CultureInfo.CurrentCulture)}");
            ReportBuilder.AppendLine($"Mission Bonuses: {Utilities.Instance.FundsSymbol}{CrewManager.Instance.LastBonus}");
            ReportBuilder.AppendLine($"Construction Department: {Utilities.Instance.FundsSymbol}{FacilityManager.Instance.GetAllocatedFunding().ToString("N0", CultureInfo.CurrentCulture)}");
            ReportBuilder.AppendLine($"Research Department: {Utilities.Instance.FundsSymbol}{ResearchManager.Instance.GetAllocatedFunding().ToString("N0", CultureInfo.CurrentCulture)}");
            double stratCost = BudgetStats.lastCycleStratCost;
            this.ReportBuilder.AppendLine("Strategy Budget: " + Utilities.Instance.FundsSymbol + Math.Max(0.0, stratCost).ToString("N0", CultureInfo.CurrentCulture));
            double netBudget = BudgetStats.lastCycleNetBudget;
            ReportBuilder.AppendLine($"General Budget: {Utilities.Instance.FundsSymbol}{Math.Max(0, netBudget).ToString("N0", CultureInfo.CurrentCulture)}");
            if (netBudget > 0 && netBudget < Funding.Instance.Funds) ReportBuilder.AppendLine("We can't justify extending your funding");
            // ReSharper disable once InvertIf
            if (netBudget + Utilities.Instance.fundsStored < 0)
            {
                ReportBuilder.AppendLine("The budget didn't fully cover your space programs costs.");
                ReportBuilder.Append($"A budget shortfall of {Utilities.Instance.FundsSymbol}{Math.Round(netBudget + Utilities.Instance.fundsStored, 0).ToString("N0", CultureInfo.CurrentCulture)} was experienced.");
            }
            else if (netBudget < 0)
            {
                ReportBuilder.AppendLine("The budget didn't fully cover your space programs costs, so we used your remaining treasury to pay dues.");
                ReportBuilder.Append($"This cost {Utilities.Instance.FundsSymbol}{Math.Round(Math.Abs(netBudget), 0).ToString("N0", CultureInfo.CurrentCulture)} from our treasury.");
            }
            return ReportBuilder.ToString();
        }
    }
}