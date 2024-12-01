using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Policy;

namespace Bureaucracy
{
    [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyleForMemberAccess")]
    public class FacilityReport : Report
    {
        public FacilityReport()
        {
            ReportTitle = "Facilities Report";
        }

        public override string ReportBody()
        {
            bool facilityClosed = false;
            ReportBuilder.Clear();
            for (int i = 0; i < FacilityManager.Instance.Facilities.Count; i++)
            {
                BureaucracyFacility bf = FacilityManager.Instance.Facilities.ElementAt(i);
                string s = bf.GetProgressReport(bf.Upgrade);
                if (bf.IsClosed)
                {
                    ReportBuilder.AppendLine(bf.Name + " is closed");
                    facilityClosed = true;
                }
                if(s == String.Empty) continue;
                ReportBuilder.AppendLine(s);
            }
            if (!facilityClosed)
            {
                ReportBuilder.AppendLine("All Facilities fully funded!");
            }
            string report = ReportBuilder.ToString();
            if (report.Length <= 31)
            {
                ReportBuilder.Clear();
                ReportBuilder.AppendLine("All Facilities fully funded!");
                ReportBuilder.AppendLine("No Facility updates to report");
                report = ReportBuilder.ToString();
            }
            return report;
        }
    }
}