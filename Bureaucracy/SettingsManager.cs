
using UnityEngine;

namespace Bureaucracy
{
    public class SettingsManager
    {
        public static SettingsManager Instance;
        public int BudgetMultiplier = 2227;
        public float TimeBetweenBudgets = 30.0f;
        public bool StopTimeWarp = true;
        public bool UseItOrLoseIt = true;
        public bool HardMode = false;
        public bool RepDecayEnabled = false;
        public int RepDecayPercent = 0;
        public int AdminCost = 4000;
        public int AstronautComplexCost = 2000;
        public int MissionControlCost = 6000;
        public int SphCost = 8000;
        public int TrackingStationCost = 4000;
        public int RndCost = 8000;
        public int VabCost = 8000;
        public int OtherFacilityCost = 5000;
        public int launchCostSPH = 100;
        public int launchCostVAB = 1000;

        public SettingsManager()
        {
            Instance = this;
        }
        public void OnLoad(ConfigNode cn)
        {
            Debug.Log("[Bureaucracy]: Settings Class would have loaded if you'd written it");
        }

        public void OnSave(ConfigNode cn)
        {
            Debug.Log("[Bureaucracy]: Settings Class would have saved if you'd written it");
        }
    }
}