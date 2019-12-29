using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Upgradeables;

namespace Bureaucracy
{
    public class FacilityManager : Manager
    {
        public List<BureaucracyFacility> Facilities = new List<BureaucracyFacility>();
        public static FacilityManager Instance;
        public List<BureaucracyFacility> UnmaintainedFacilities = new List<BureaucracyFacility>();

        public FacilityManager()
        {
            InternalListeners.OnBudgetAboutToFire.Add(RunFacilityBudget);
            SpaceCenterFacility[] spaceCentreFacilities = (SpaceCenterFacility[]) Enum.GetValues(typeof(SpaceCenterFacility));
            for (int i = 0; i < spaceCentreFacilities.Length; i++)
            {
                SpaceCenterFacility spf = spaceCentreFacilities.ElementAt(i);
                Facilities.Add(new BureaucracyFacility(spf));
            }
            Name = "Facility Manager";
            Instance = this;
        }
        
        public override void UnregisterEvents()
        {
            InternalListeners.OnBudgetAboutToFire.Remove(RunFacilityBudget);
        }

        public override double GetAllocatedFunding()
        {
            return Math.Round(Utilities.Instance.GetNetBudget("Facilities"), 0);
        }

        protected override Report GetReport()
        {
            return new FacilityReport();
        }

        private void RunFacilityBudget()
        {
            ReopenAllFacilities();
            double facilityBudget = Utilities.Instance.GetNetBudget("Facilities");
            for (int i = 0; i < Facilities.Count; i++)
            {
                BureaucracyFacility bf = Facilities.ElementAt(i);
                if(!bf.Upgrading) continue;
                facilityBudget = bf.Upgrade.ProgressUpgrade(facilityBudget);
                if (facilityBudget <= 0.0f) return;
            }
        }
        
        public void OnLoad(ConfigNode cn)
        {
            ConfigNode managerNode = cn.GetNode("FACILITY_MANAGER");
            if (managerNode == null) return;
            ConfigNode[] facilityNodes = managerNode.GetNodes("FACILITY");
            for (int i = 0; i < Facilities.Count; i++)
            {
                BureaucracyFacility bf = Facilities.ElementAt(i);
                bf.OnLoad(facilityNodes);
            }
        }
        
        

        public void OnSave(ConfigNode cn)
        {
            ConfigNode managerNode = new ConfigNode("FACILITY_MANAGER");
            for (int i = 0; i < Facilities.Count; i++)
            {
                BureaucracyFacility bf = Facilities.ElementAt(i);
                bf.OnSave(managerNode);
            }

            cn.AddNode(managerNode);
        }

        public void StartUpgrade(UpgradeableFacility facility)
        {
            BureaucracyFacility facilityToUpgrade = UpgradeableToActualFacility(facility);
            if (facilityToUpgrade == null)
            {
                Debug.Log("[Bureaucracy]: Upgrade of "+facility.id+" requested but no facility found");
                return;
            }
            Debug.Log("[Bureaucracy]: Upgrade of "+facility.id+" requested");
            if (facilityToUpgrade.Upgrading)
            {
                Debug.Log("[Bureaucracy]: "+facility.id+" is already being upgraded");
                ScreenMessages.PostScreenMessage(facilityToUpgrade.Name + " is already being upgraded");
                return;
            }
            facilityToUpgrade.StartUpgrade(facility);
        }

        private BureaucracyFacility UpgradeableToActualFacility(UpgradeableFacility facility)
        {
            for (int i = 0; i < Facilities.Count; i++)
            {
                BureaucracyFacility bf = Facilities.ElementAt(i);
                if(!facility.id.Contains(bf.Name)) continue;
                return bf;
            }
            return null;
        }

        public BureaucracyFacility GetFacilityByName(string name)
        {
            for (int i = 0; i < Facilities.Count; i++)
            {
                BureaucracyFacility bf = Facilities.ElementAt(i);
                if (bf.Name == name) return bf;
            }

            return null;
        }

        public void ReopenAllFacilities()
        {
            for (int i = 0; i < Facilities.Count; i++)
            {
                BureaucracyFacility bf = Facilities.ElementAt(i);
                bf.ReopenFacility();
            }
        }
    }
}