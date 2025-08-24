using KSP.UI.Screens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Bureaucracy
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class PartsCostProcessor : MonoBehaviour
    {
        const int ORIGINAL_CONTROL_PART_COST = 2000;
        private static string controlPartName = "Aerodynamic Nose Cone";

        public static PartsCostProcessor Instance;
        public static int lastCostAdjustment = 100;
        public static bool isPartCostProcessed = false;
       
        protected void Awake()
        {
            DontDestroyOnLoad(this);
            Instance = this;
        }

        public void ProcessParts()
        {            
            var costAdjustmentReversion = CalculateAdjustmentBasedOnControlPart();

            var costAdjustment = HighLogic.CurrentGame.Parameters.CustomParams<BureaucracyParams>().bureaucracyPurchasablePartsCostAdjustment;

            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) return;

            // needs to process if:
            // - haven't been processed in this game session yet
            // - multiplier changed
            if (isPartCostProcessed && costAdjustment == lastCostAdjustment) return;

            Debug.Log("[Bureaucracy] Processing purchasing cost for installed Parts...");

            Debug.Log($"[Bureaucracy] Processing {PartLoader.LoadedPartsList.Count} parts...");
            foreach (var part in PartLoader.LoadedPartsList)
            {
                if (part.entryCost <= 0) continue;

                // reset to stock price
                part.SetEntryCost((int)Math.Floor(part.entryCost * ((float)costAdjustmentReversion / 100f)));

                // adjust cost
                part.SetEntryCost((int)Math.Floor(part.entryCost * ((float)costAdjustment / 100f)));
            }
            lastCostAdjustment = costAdjustment;
            isPartCostProcessed = true;

            Debug.Log("[Bureaucracy] Parts Cost Processed Successfully.");
        }

        public void OnLoad(ConfigNode cn)
        {
            ConfigNode partsCostNode = cn.GetNode("PARTS_COST_PROCESS");
            if (partsCostNode == null)
            {
                // for new game, "last" cost was stock cost, aka 100%
                lastCostAdjustment = 100;
                return;
            }

            int.TryParse(partsCostNode.GetValue("LastCostAdjustment"), out lastCostAdjustment);

            isPartCostProcessed = false;
        }

        public void OnSave(ConfigNode cn)
        {
            ConfigNode partsCostNode = new ConfigNode("PARTS_COST_PROCESS");
            partsCostNode.SetValue("LastCostAdjustment", lastCostAdjustment, true);
            cn.AddNode(partsCostNode);
        }

        public int CalculateAdjustmentBasedOnControlPart()
        {
            // use a common stock part to determine difference between stock and current cost
            var controlPart = PartLoader.LoadedPartsList.Find(p => p.title.ToUpper() == controlPartName.ToUpper());
            var controlPartCost = controlPart.entryCost;

            return (int)Math.Floor((float)ORIGINAL_CONTROL_PART_COST / (float)controlPartCost) * 100;
        }
    }
}
