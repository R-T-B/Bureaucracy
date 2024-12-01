using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bureaucracy
{
    public class Costs
    {
        private int launchCostsVab;
        private int launchCostsSph;
        private bool costsDirty = true;
        public static Costs Instance;
        private double cachedCosts;

        public Costs()
        {
            Instance = this;
        }

        public void AddLaunch(ShipConstruct ship)
        {
            if (ship.shipFacility == EditorFacility.SPH) launchCostsSph += SettingsClass.Instance.LaunchCostSph;
            else launchCostsVab += SettingsClass.Instance.LaunchCostVab;
            Debug.Log("[Bureaucracy]: Launch Registered");
        }

        public void ResetLaunchCosts()
        {
            launchCostsSph = 0;
            launchCostsVab = 0;
            Debug.Log("[Bureaucracy]: Launch Costs Reset");
        }
        
        public double GetTotalMaintenanceCosts()
        {
            // if it's a new game, there's no bills or payroll due yet - the space center just opened!
            if (Utilities.Instance.IsBootstrapBudgetCycle) return 0;

            if (!costsDirty)
            {
                return cachedCosts;
            }
            Debug.Log("[Bureaucracy]: Costs are dirty. Recalculating");
            double costs = 0;
            costs += GetFacilityMaintenanceCosts();
            costs += GetWageCosts();
            costs += GetLaunchCosts();
            cachedCosts = costs;
            costsDirty = false;
            Debug.Log("[Bureaucracy]: Cached costs "+costs+". Setting Costs not dirty for next 5 seconds");
            Bureaucracy.Instance.Invoke(nameof(Bureaucracy.Instance.SetCalcsDirty), 5.0f);
            return Math.Round(costs);
        }

        public void SetCalcsDirty()
        {
            costsDirty = true;
            Debug.Log("[Bureaucracy]: Costs are dirty");
        }

        public double GetLaunchCosts()
        {
            return launchCostsSph + launchCostsVab;
        }

        public double GetWageCosts()
        {
            // if it's the initial cycle, return 0 (kerbal just been hired!)
            if (Utilities.Instance.IsBootstrapBudgetCycle) return 0;

            List<CrewMember> crew = CrewManager.Instance.Kerbals.Values.ToList();
            double wage = 0;
            for (int i = 0; i < crew.Count; i++)
            {
                CrewMember c = crew.ElementAt(i);
                if(c.CrewReference().rosterStatus == ProtoCrewMember.RosterStatus.Dead || c.CrewReference().rosterStatus == ProtoCrewMember.RosterStatus.Missing) continue;
                wage += c.Wage;
            }
            return Math.Round(wage);
        }

        public double GetFacilityMaintenanceCosts()
        {
            double d = 0;
            
            // if it's the bootstrap cycle, maintenance costs are zero - facilities just opened, no bills yet!
            if (!Utilities.Instance.IsBootstrapBudgetCycle)
            {
                for (int i = 0; i < FacilityManager.Instance.Facilities.Count; i++)
                {
                    BureaucracyFacility bf = FacilityManager.Instance.Facilities.ElementAt(i);
                    if (bf.IsClosed) continue;
                    d += bf.MaintenanceCost * FacilityManager.Instance.CostMultiplier;
                }
            }
            return Math.Round(d);
        }

        public void OnLoad(ConfigNode costsNode)
        {
            if (costsNode == null) return;
            int.TryParse(costsNode.GetValue("launchCostsVAB"), out launchCostsVab);
            int.TryParse(costsNode.GetValue("launchCostsSPH"), out launchCostsSph);
        }

        public void OnSave(ConfigNode cn)
        {
            ConfigNode costsNode = new ConfigNode("COSTS");
            costsNode.SetValue("launchCostsVAB", launchCostsVab, true);
            costsNode.SetValue("launchCostsSPH", launchCostsSph, true);
            cn.AddNode(costsNode);
        }
    }
}