using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using KSP.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace Bureaucracy
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class UiControllerSpaceCentre : UiController
    {
        
    }
    
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class UiControllerFlight : UiController
    {
        
    }
    
    public class UiController : MonoBehaviour
    {
        private ApplicationLauncherButton toolbarButton;
        public static UiController Instance;
        private PopupDialog mainWindow;
        private PopupDialog facilitiesWindow;
        private PopupDialog researchWindow;
        public PopupDialog allocationWindow;
        public PopupDialog crewWindow;
        public PopupDialog cycleReportWindow;

        [UsedImplicitly] public PopupDialog errorWindow;
        private int padding;
        private const int PadFactor = 10;

        private void Awake()
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            GameEvents.onGUIApplicationLauncherReady.Add(SetupToolbarButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Add(RemoveToolbarButton);
        }

        private int GetAllocation(Manager manager)
        {
            return (int)Math.Round((manager.FundingAllocation*100), 0);
        }

        public void SetupToolbarButton()
        {
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && this.toolbarButton == null)
            {
                //TODO: Rename the icon file
                this.toolbarButton = ApplicationLauncher.Instance.AddModApplication(new Callback(this.ToggleUI), new Callback(this.ToggleUI), null, null, null, null, ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT, GameDatabase.Instance.GetTexture("Bureaucracy/MainIcon", false));
            }
        }

        private void ToggleUI()
        {
            if (UiInactive())
            {
				BudgetStats.recalcBudgetFigures(false);
				ActivateUi("main");
			}
            else DismissAllWindows();
        }

        private bool UiInactive()
        {
            return mainWindow == null && facilitiesWindow == null && researchWindow == null && crewWindow == null;
        }

        private void ActivateUi(string screen)
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) return;
           DismissAllWindows();
            switch (screen)
            {
                case "main":
                    mainWindow = DrawMainUi();
                    break;
                case "facility":
                    facilitiesWindow = DrawFacilityUi();
                    break;
                case "research":
                    researchWindow = DrawResearchUi();
                    break;
                case "allocation":
                    allocationWindow = DrawBudgetAllocationUi();
                    break;
                case "crew":
                    crewWindow = DrawCrewUI();
                    break;
            }
        }

        private PopupDialog DrawCrewUI()
        {
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            List<DialogGUIBase> innerElements = new List<DialogGUIBase>();
            innerElements.Add(new DialogGUIContentSizer(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.MinSize, true));
            innerElements.Add(new DialogGUISpace(10));
            DialogGUIBase[] horizontal;
            for (int i = 0; i < CrewManager.Instance.Kerbals.Count; i++)
            {
                KeyValuePair<string, CrewMember> crew = CrewManager.Instance.Kerbals.ElementAt(i);
                if (crew.Value.CrewReference().rosterStatus != ProtoCrewMember.RosterStatus.Available) continue;
                if (crew.Value.CrewReference().inactive) continue;
                if (crew.Value.CrewReference().experienceLevel >= 5) continue;
                horizontal = new DialogGUIBase[4];
                horizontal[0] = new DialogGUISpace(30);
                horizontal[1] = new DialogGUILabel(crew.Key, MessageStyle(true, true), true);
                
                var buttonGuiSlot = new DialogGUIBase[2];
                buttonGuiSlot[0] = new DialogGUISpace(1);
                buttonGuiSlot[1] = new DialogGUIButton("Train", () => TrainKerbal(crew.Value), 70, 24, false);

                horizontal[2] = new DialogGUIVerticalLayout(buttonGuiSlot);
                horizontal[3] = new DialogGUISpace(40);
                var horizontalGroupCrew = new DialogGUIHorizontalLayout(horizontal) { anchor = TextAnchor.MiddleLeft };
                innerElements.Add(horizontalGroupCrew);
            }
            DialogGUIVerticalLayout vertical = new DialogGUIVerticalLayout(innerElements.ToArray());
            dialogElements.Add(new DialogGUIScrollList(new Vector2(300, 300), false, true, vertical));

            
            DialogGUIBase[] buttons = new DialogGUIBase[2];
            buttons[0] = GetTopBoxes("crew", false);
            buttons[1] = GetBottomBoxes("crew", false);
            dialogElements.Add(new DialogGUIVerticalLayout(buttons));
            
            var multiOptDialog = new MultiOptionDialog("BureaucracyCrew", "", "Bureaucracy: Crew Manager", UISkinManager.GetSkin("MainMenuSkin"), GetRect(dialogElements), dialogElements.ToArray());
            var popup = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), multiOptDialog, false, UISkinManager.GetSkin("MainMenuSkin"), false);
            return popup;
        }

        private void TrainKerbal(CrewMember crewMember)
        {
            int newLevel = crewMember.CrewReference().experienceLevel + 1;
            float trainingFee = newLevel * SettingsClass.Instance.BaseTrainingFee;
            if (crewMember.CrewReference().inactive)
            {
                ScreenMessages.PostScreenMessage(crewMember.Name + " is already in training");
                return;
            }
            if (!Funding.CanAfford(trainingFee))
            {
                ScreenMessages.PostScreenMessage("Cannot afford training fee of $" + trainingFee);
                return;
            }
            Funding.Instance.AddFunds(-trainingFee, TransactionReasons.CrewRecruited);
            ScreenMessages.PostScreenMessage(crewMember.Name + " in training for " + newLevel + " months");
            crewMember.Train();
        }

        private PopupDialog DrawBudgetAllocationUi()
        {
            padding = 0;
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            List<DialogGUIBase> innerElements = new List<DialogGUIBase>();
            innerElements.Add(new DialogGUIContentSizer(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.MinSize, true));
            innerElements.Add(new DialogGUISpace(10));
            
            DialogGUIBase[] horizontalArray = new DialogGUIBase[4];
            horizontalArray[0] = new DialogGUISpace(10);
            horizontalArray[1] = new DialogGUILabel("Funds & Strategy", MessageStyle(true, true), true);            
            horizontalArray[2] = new DialogGUITextInput(GetAllocation(BudgetManager.Instance).ToString(), false, 3, s => SetAllocation("Budget", s), 40.0f, 30.0f);
            horizontalArray[3] = new DialogGUISpace(100);
            innerElements.Add(new DialogGUIHorizontalLayout(horizontalArray) { anchor = TextAnchor.MiddleLeft } );
            
            horizontalArray = new DialogGUIBase[4];
            horizontalArray[0] = new DialogGUISpace(10);
            horizontalArray[1] = new DialogGUILabel("Construction", MessageStyle(true, true), true);
            horizontalArray[2] = new DialogGUITextInput(GetAllocation(FacilityManager.Instance).ToString(), false, 3, s => SetAllocation("Construction", s), 40.0f, 30.0f);
            horizontalArray[3] = new DialogGUISpace(100);            
            innerElements.Add(new DialogGUIHorizontalLayout(horizontalArray) { anchor = TextAnchor.MiddleLeft });
            
            horizontalArray = new DialogGUIBase[4];
            horizontalArray[0] = new DialogGUISpace(10);
            horizontalArray[1] = new DialogGUILabel("Research", MessageStyle(true, true), true);
            horizontalArray[2] = new DialogGUITextInput(GetAllocation(ResearchManager.Instance).ToString(), false, 3, s => SetAllocation("Research", s), 40.0f, 30.0f);
            horizontalArray[3] = new DialogGUISpace(100);            
            innerElements.Add(new DialogGUIHorizontalLayout(horizontalArray) { anchor = TextAnchor.MiddleLeft });
            
            innerElements.Add(new DialogGUISpace(15));
            Manager netIncomeManager = null;
            for (int i = 0; i < Bureaucracy.Instance.registeredManagers.Count; i++)
            {
                Manager m = Bureaucracy.Instance.registeredManagers.ElementAt(i);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (m.Name.Equals("Budget"))
                {
                    if (Utilities.Instance.GetNetBudget(m.Name) == -1.0f) continue;
                    horizontalArray = new DialogGUIBase[3];
                    horizontalArray[0] = new DialogGUISpace(10);
                    horizontalArray[1] = new DialogGUILabel("Net Income: ");
                    horizontalArray[2] = new DialogGUILabel(() => ShowFunding(m, false, false));
                    innerElements.Add(new DialogGUIHorizontalLayout(horizontalArray));
                    if (Utilities.Instance.GetNetBudget(m.Name) == -1.0f) continue;
                    horizontalArray = new DialogGUIBase[3];
                    horizontalArray[0] = new DialogGUISpace(10);
                    horizontalArray[1] = new DialogGUILabel("Strategies: ");
                    horizontalArray[2] = new DialogGUILabel(() => ShowFunding(m, true, true));
                    innerElements.Add(new DialogGUIHorizontalLayout(horizontalArray));
                    netIncomeManager = m;
                }
                else
                {
                    if (Utilities.Instance.GetNetBudget(m.Name) == -1.0f) continue;
                    horizontalArray = new DialogGUIBase[3];
                    horizontalArray[0] = new DialogGUISpace(10);
                    horizontalArray[1] = new DialogGUILabel(m.Name + ": ");
                    horizontalArray[2] = new DialogGUILabel(() => ShowFunding(m, false, false));
                    innerElements.Add(new DialogGUIHorizontalLayout(horizontalArray));
                }
            }
            horizontalArray = new DialogGUIBase[3];
            horizontalArray[0] = new DialogGUISpace(10);
            horizontalArray[1] = new DialogGUILabel("General Funds: ");
            horizontalArray[2] = new DialogGUILabel(() => ShowFunding(netIncomeManager, true, false));
            innerElements.Add(new DialogGUIHorizontalLayout(horizontalArray));
            horizontalArray = new DialogGUIBase[2];
            horizontalArray[0] = new DialogGUISpace(10);
            horizontalArray[1] = new DialogGUIButton("Load Settings", () => SettingsClass.Instance.InGameLoad(), false); 
            innerElements.Add(new DialogGUIHorizontalLayout(horizontalArray));
            DialogGUIVerticalLayout vertical = new DialogGUIVerticalLayout(innerElements.ToArray());
            dialogElements.Add(new DialogGUIScrollList(new Vector2(300, 300), false, true, vertical));

            DialogGUIBase[] buttons = new DialogGUIBase[2];
            buttons[0] = GetTopBoxes("allocation", false);
            buttons[1] = GetBottomBoxes("allocation", false);
            dialogElements.Add(new DialogGUIVerticalLayout(350, 0, buttons));

            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new MultiOptionDialog("BureaucracyBudgetAllocation", "", "Bureaucracy: Budget Allocation", UISkinManager.GetSkin("MainMenuSkin"),
                    GetRect(dialogElements), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin"), false);
        }

        private string ShowFunding(Manager manager, bool breakDownBudget, bool getStratPortion)
        {
            if (!breakDownBudget)
            {
                return Utilities.Instance.FundsSymbol + Math.Round(Utilities.Instance.GetNetBudget(manager.Name), 0).ToString("N0", CultureInfo.CurrentCulture);
            }
            else
            {
                double netResult = 0;
                if (getStratPortion)
                {
                    netResult = Utilities.Instance.GetNetBudget(manager.Name) * BudgetStats.projectedStratPercentageAsMult;
                }
                else
                {
                    netResult = Utilities.Instance.GetNetBudget(manager.Name) * (1 - BudgetStats.projectedStratPercentageAsMult); 
                }
                return Utilities.Instance.FundsSymbol + Math.Round(netResult, 0).ToString("N0", CultureInfo.CurrentCulture);
            }
        }

        private string SetAllocation(string managerName, string passedString)
        {
            int.TryParse(passedString, out int i);
            float actualAllocation = i / 100.0f;
            Manager m = Utilities.Instance.GetManagerByName(managerName);
            m.FundingAllocation = actualAllocation;
            switch (managerName)
            {
                case "Budget":
                    return GetAllocation(BudgetManager.Instance).ToString(CultureInfo.CurrentCulture);
                case "Research":
                    return GetAllocation(ResearchManager.Instance).ToString(CultureInfo.CurrentCulture);
                case "Construction":
                    return GetAllocation(FacilityManager.Instance).ToString(CultureInfo.CurrentCulture);
            }

            return passedString;
        }

        private void DismissAllWindows()
        {
            if (mainWindow != null) mainWindow.Dismiss();
            if (facilitiesWindow != null) facilitiesWindow.Dismiss();
            if (researchWindow != null) researchWindow.Dismiss();
            if (allocationWindow != null) allocationWindow.Dismiss();
            if(crewWindow != null) crewWindow.Dismiss();
        }

        private void DismissCycleReport()
        {
            cycleReportWindow.Dismiss();
        }

        private PopupDialog DrawMainUi()
        {
            padding = 0;
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            List<DialogGUIBase> innerElements = new List<DialogGUIBase>();            
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)  innerElements.Add(new DialogGUILabel("Bureaucracy is only available in Career Games"));
            else
            {
                innerElements.Add(new DialogGUISpace(10));
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel("Next Budget: " + Utilities.Instance.ConvertUtToKspTimeStamp(BudgetManager.Instance.NextBudget.CompletionTime), false)));
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel($"Gross Budget: {Utilities.Instance.FundsSymbol}{Utilities.Instance.GetGrossBudget().ToString("N0", CultureInfo.CurrentCulture)}", false)));
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel($"Wage Costs: {Utilities.Instance.FundsSymbol}{Costs.Instance.GetWageCosts().ToString("N0", CultureInfo.CurrentCulture)}", false)));
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel($"Facility Maintenance Costs: {Utilities.Instance.FundsSymbol}{Costs.Instance.GetFacilityMaintenanceCosts().ToString("N0", CultureInfo.CurrentCulture)}", false)));
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel($"Launch Costs: {Utilities.Instance.FundsSymbol}{Costs.Instance.GetLaunchCosts().ToString("N0", CultureInfo.CurrentCulture)}", false)));
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel($"Mission Bonuses: {Utilities.Instance.FundsSymbol}{GetBonusesToPay().ToString("N0", CultureInfo.CurrentCulture)}", false)));
                double departmentFunding = 0;
                for (int i = 0; i < Bureaucracy.Instance.registeredManagers.Count; i++)
                {
                    Manager m = Bureaucracy.Instance.registeredManagers.ElementAt(i);
                    if (m.Name == "Budget") continue;
                    departmentFunding = Math.Round(Utilities.Instance.GetNetBudget(m.Name), 0);
                    if (departmentFunding < 0.0f) continue;
                    innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel(m.Name + " Department Funding: " + Utilities.Instance.FundsSymbol + departmentFunding.ToString("N0", CultureInfo.CurrentCulture), false)));
                }
                departmentFunding = BudgetStats.projectedStratCost;
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel($"Strategy Funding: " + Utilities.Instance.FundsSymbol + departmentFunding.ToString("N0", CultureInfo.CurrentCulture), false)));
                departmentFunding = BudgetStats.projectedNetBudget;
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel($"Net Budget: {Utilities.Instance.FundsSymbol}{departmentFunding.ToString("N0", CultureInfo.CurrentCulture)}", false)));
                DialogGUIVerticalLayout vertical = new DialogGUIVerticalLayout(innerElements.ToArray());
                vertical.AddChild(new DialogGUIContentSizer(widthMode: ContentSizeFitter.FitMode.Unconstrained, heightMode: ContentSizeFitter.FitMode.MinSize));
                dialogElements.Add(new DialogGUIScrollList(new Vector2(300, 300), false, true, vertical));
                DialogGUIBase[] horizontal = new DialogGUIBase[6];
                horizontal[0] = new DialogGUILabel("Allocations: ");
                horizontal[1] = new DialogGUILabel("Funds & Strategy: "+GetAllocation(BudgetManager.Instance)+"%");
                horizontal[2] = new DialogGUILabel("|");
                horizontal[3] = new DialogGUILabel("Construction: "+GetAllocation(FacilityManager.Instance)+"%");
                horizontal[4] = new DialogGUILabel("|");
                horizontal[5] = new DialogGUILabel("Research: "+GetAllocation(ResearchManager.Instance)+"%");
                

                dialogElements.Add(new DialogGUIHorizontalLayout(horizontal));

                DialogGUIBase[] buttons = new DialogGUIBase[2];
                buttons[0] = GetTopBoxes("main");
                buttons[1] = GetBottomBoxes("main");
                dialogElements.Add(new DialogGUIVerticalLayout(300, 0, buttons));
            }
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new MultiOptionDialog("BureaucracyMain", "", "Bureaucracy: Budget", UISkinManager.GetSkin("MainMenuSkin"),
                    GetRect(dialogElements), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin"), false);
        }

        private Rect GetRect(List<DialogGUIBase> dialogElements)
        {
            return new Rect(0.5f, 0.5f, 350, 265) {height = 150 + 50 * dialogElements.Count, width = Math.Max(padding, 320)};
        }

        // Cycle Report is a bit wider than other windows to accomodate research labels
        private Rect GetCycleReportRect(List<DialogGUIBase> dialogElements)
        {
            return new Rect(0.5f, 0.5f, 410, 265) { height = 150 + 50 * dialogElements.Count, width = Math.Max(padding, 370) };
        }

        private DialogGUIBase[] PaddedLabel(string stringToPad, bool largePrint)
        {
            DialogGUIBase[] paddedLayout = new DialogGUIBase[2];
            paddedLayout[0] = new DialogGUISpace(10);
            EvaluatePadding(stringToPad);
            paddedLayout[1] = new DialogGUILabel(stringToPad, MessageStyle(largePrint));
            return paddedLayout;
        }

        private void EvaluatePadding(string stringToEvaluate)
        {
            if (stringToEvaluate.Length *PadFactor > padding) padding = stringToEvaluate.Length * PadFactor;
        }

        private UIStyle MessageStyle(bool largePrint, bool crewMessage = false)
        {
            UIStyle style = new UIStyle
            {
                fontSize = 12,

                fontStyle = FontStyle.Bold,
                alignment = crewMessage ? TextAnchor.LowerLeft :  TextAnchor.LowerCenter,
                stretchWidth = crewMessage ? false : true,
                normal = new UIStyleState
                {
                    textColor = new Color(0.89f, 0.86f, 0.72f)
                }
            };
            if (largePrint) style.fontSize = 23;
            return style;
        }

        private int GetBonusesToPay()
        { 
            int pay = CrewManager.Instance.LastIssuedBonus;
            if (pay.Equals(int.MinValue))
            {
                pay = CrewManager.Instance.LastBonus;
                CrewManager.Instance.LastIssuedBonus = pay;
            }
            return pay;
        }

        private PopupDialog DrawFacilityUi()
        {
            padding = 0;
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            List<DialogGUIBase> innerElements = new List<DialogGUIBase>();
            int upgradeCount = 0;
            innerElements.Add(new DialogGUISpace(10));
            float investmentNeeded = 0;
            innerElements.Add(new DialogGUIContentSizer(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize, true));
            innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel($"This Month's Budget: {Utilities.Instance.FundsSymbol}{Math.Round(FacilityManager.Instance.ThisMonthsBudget, 0).ToString("N0", CultureInfo.CurrentCulture)}", false)));
            for (int i = 0; i < FacilityManager.Instance.Facilities.Count; i++)
            {
                BureaucracyFacility bf = FacilityManager.Instance.Facilities.ElementAt(i);
                if (!bf.Upgrading) continue;
                upgradeCount++;
                investmentNeeded += bf.Upgrade.RemainingInvestment;
                float percentage = bf.Upgrade.OriginalCost - bf.Upgrade.RemainingInvestment;
                percentage = (float)Math.Round(percentage / bf.Upgrade.OriginalCost * 100,0);
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel(bf.Name + " "+percentage + "% ($" + bf.Upgrade.RemainingInvestment + " needed)", false)));
            }
            if (upgradeCount == 0) innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel("No Facility Upgrades in progress", false)));
            DialogGUIVerticalLayout vertical = new DialogGUIVerticalLayout(innerElements.ToArray());
            dialogElements.Add(new DialogGUIScrollList(new Vector2(300, 300), false, true, vertical));
            DialogGUIBase[] horizontal = new DialogGUIBase[3];
            horizontal[0] = new DialogGUILabel($"Total Investment Needed: {Utilities.Instance.FundsSymbol}{investmentNeeded.ToString("N0", CultureInfo.CurrentCulture)}");
            horizontal[1] = new DialogGUILabel("|");
            horizontal[2] = new DialogGUILabel("Chance of Fire: "+Math.Round(FacilityManager.Instance.FireChance*100, 0)+"%");
            dialogElements.Add(new DialogGUIHorizontalLayout(horizontal));

            DialogGUIBase[] buttons = new DialogGUIBase[2];
            buttons[0] = GetTopBoxes("facility");
            buttons[1] = GetBottomBoxes("facility");
            dialogElements.Add(new DialogGUIVerticalLayout(350, 0, buttons));

            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("BureaucracyFacilities", "", "Bureaucracy: Facilities", UISkinManager.GetSkin("MainMenuSkin"), new Rect(0.5f, 0.5f, 320, 350), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin"));
        }

        private PopupDialog DrawResearchUi()
        {
            padding = 0;
            float scienceCount = 0;
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            List<DialogGUIBase> innerElements = new List<DialogGUIBase>();
            innerElements.Add(new DialogGUIContentSizer(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize, true));
            innerElements.Add(new DialogGUISpace(10));
            if(ResearchManager.Instance.ProcessingScience.Count == 0) innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel("No research in progress", false)));
            for (int i = 0; i < ResearchManager.Instance.ProcessingScience.Count; i++)
            {
                ScienceEvent se = ResearchManager.Instance.ProcessingScience.ElementAt(i).Value;
                if (se.IsComplete) continue;
                scienceCount += se.RemainingScience;
                innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel(se.UiName+": "+Math.Round(se.OriginalScience-se.RemainingScience, 1)+"/"+Math.Round(se.OriginalScience, 1), false)));
            }

            dialogElements.Add(new DialogGUIScrollList(new Vector2(300, 300), false, true, new DialogGUIVerticalLayout(10, 100, 4, new RectOffset(6, 24, 10, 10), TextAnchor.UpperLeft, innerElements.ToArray())));
            DialogGUIBase[] horizontal = new DialogGUIBase[3];
            horizontal[0] = new DialogGUILabel("Processing Science: " + Math.Round(scienceCount, 1));
            horizontal[1] = new DialogGUILabel("|");
            double scienceOutput = ResearchManager.Instance.ThisMonthsBudget / SettingsClass.Instance.ScienceMultiplier * ResearchManager.Instance.ScienceMultiplier;
            horizontal[2] = new DialogGUILabel($"Research Output: { Math.Floor(Utilities.Instance.ScienceProcessedCurrentCycle * 10) / 10} / { Math.Round(scienceOutput, 1) }");
            dialogElements.Add(new DialogGUIHorizontalLayout(horizontal));

            DialogGUIBase[] buttons = new DialogGUIBase[2];
            buttons[0] = GetTopBoxes("research");
            buttons[1] = GetBottomBoxes("research");
            dialogElements.Add(new DialogGUIVerticalLayout(350, 0, buttons));

            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("BureaucracyResearch", "", "Bureaucracy: Research", UISkinManager.GetSkin("MainMenuSkin"), GetRect(dialogElements), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin"));
        }

        private DialogGUIHorizontalLayout GetTopBoxes(string passingUi, bool allocations = true)
        {
            int arrayPointer = 0;
            DialogGUIBase[] horizontal = new DialogGUIBase[allocations ? 2 : 3];
            if (passingUi != "main")
            {
                horizontal[arrayPointer] = new DialogGUIButton("Budget", ()=> ActivateUi("main"));
                arrayPointer++;
            }
            if (passingUi != "facility")
            {
                horizontal[arrayPointer] = new DialogGUIButton("Construction", () => ActivateUi("facility"));
                arrayPointer++;
            }
            if (passingUi != "research")
            {
             horizontal[arrayPointer] = new DialogGUIButton("Research", () => ActivateUi("research"));
             arrayPointer++;
            }            

            return new DialogGUIHorizontalLayout(280, 30, horizontal) { stretchWidth = true };
        }

        private DialogGUIHorizontalLayout GetBottomBoxes(string passingUi, bool allocations = true)
        {
            int arrayPointer = 0;
            DialogGUIBase[] horizontal = new DialogGUIBase[allocations ? 3 : 2];
            if (passingUi != "allocation")
            {
                horizontal[arrayPointer] = new DialogGUIButton("Allocation", () => ActivateUi("allocation"));
                arrayPointer++;
            }
            if (passingUi != "crew")
            {
                horizontal[arrayPointer] = new DialogGUIButton("Crew", () => ActivateUi("crew"));
                arrayPointer++;
            }
            horizontal[arrayPointer] = new DialogGUIButton("Close", ValidateAllocations, false);
            return new DialogGUIHorizontalLayout(280, 30, horizontal) { stretchWidth = true };
        }

        public void ValidateAllocations()
        {
            int allocations = GetAllocation(BudgetManager.Instance);
            allocations += GetAllocation(ResearchManager.Instance);
            allocations+=  + GetAllocation(FacilityManager.Instance);
            if (allocations <99.9 || allocations >100.1) errorWindow = AllocationErrorWindow();
            else DismissAllWindows();
        }

        private PopupDialog AllocationErrorWindow()
        {
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            dialogElements.Add(new DialogGUILabel("Allocations do not add up to 100%"));
            dialogElements.Add(new DialogGUIButton("OK", () => { }, true));
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("AllocationError", "", "Bureaucracy: Error", UISkinManager.GetSkin("MainMenuSkin"), new Rect(0.5f, 0.5f, 200,90), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin"));
        }

        public void RemoveToolbarButton(GameScenes data)
        {
            if (toolbarButton == null) return;
            ApplicationLauncher.Instance.RemoveModApplication(toolbarButton);
        }

        private void OnDisable()
        {
            RemoveToolbarButton(HighLogic.LoadedScene);
        }

        public PopupDialog NoHireWindow()
        {
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            dialogElements.Add(new DialogGUILabel("Due to reduced staffing levels we are unable to take on any new kerbals at this time"));
            dialogElements.Add(new DialogGUIButton("OK", () => { }, true));
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("NoHire", "", "Can't Hire!", UISkinManager.GetSkin("MainMenuSkin"), new Rect(0.5f, 0.5f, 100, 200), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin"));
        }
        
        public PopupDialog GeneralError(string error)
        {
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            dialogElements.Add(new DialogGUILabel(error));
            dialogElements.Add(new DialogGUIButton("OK", () => { }, true));
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("GeneralErrorDialog", "", "Bureaucracy: Error", UISkinManager.GetSkin("MainMenuSkin"), new Rect(0.5f, 0.5f, 200,200), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin"));
        }
        

        public PopupDialog NoLaunchesWindow()
        {
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            dialogElements.Add(new DialogGUILabel("Due to reduced funding levels, we were unable to afford any fuel"));
            dialogElements.Add(new DialogGUISpace(20));
            dialogElements.Add(new DialogGUILabel("No fuel will be available until the end of the month."));
            dialogElements.Add(new DialogGUIButton("OK", () => { }, true));
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("NoFuel", "", "No Fuel Available!", UISkinManager.GetSkin("MainMenuSkin"), new Rect(0.5f, 0.5f, 200,160), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin"));
        }

        public PopupDialog KctError()
        {
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            dialogElements.Add(new DialogGUILabel("It looks like you have Kerbal Construction Time installed. You should not use KCT's Facility Upgrade and Bureaucracy's Facility Upgrade at the same time. Bad things will happen."));
            dialogElements.Add(new DialogGUIButton("OK", () => { }, true));
            return PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new MultiOptionDialog("KCTError", "", "KCT Detected!", UISkinManager.GetSkin("MainMenuSkin"), new Rect(0.5f, 0.5f, 400,100), dialogElements.ToArray()), false, UISkinManager.GetSkin("MainMenuSkin"));
        }

        // budget report window, shown at the end of each budget cycle
        public PopupDialog BudgetCycleReportWindow(string report)
        {
            padding = 0;
            List<DialogGUIBase> dialogElements = new List<DialogGUIBase>();
            List<DialogGUIBase> innerElements = new List<DialogGUIBase>();
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) innerElements.Add(new DialogGUILabel("Bureaucracy is only available in Career Games"));
            else
            {
                // display report contents
                var reportLines = ("\r\n" + report.Replace("\r\n\r\n\r\n", "\r\n\r\n")).Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                innerElements.Add(new DialogGUISpace(7));
                for (int i = 1; i < reportLines.Length; i++)
                {
                    if (String.IsNullOrEmpty(reportLines[i]))
                    {
                        innerElements.Add(new DialogGUISpace(15));
                    }
                    else
                    {
                        innerElements.Add(new DialogGUIHorizontalLayout(PaddedLabel(reportLines[i], false)));
                    }
                }

                DialogGUIVerticalLayout vertical = new DialogGUIVerticalLayout(innerElements.ToArray()) { anchor = TextAnchor.UpperLeft };
                vertical.AddChild(new DialogGUIContentSizer(ContentSizeFitter.FitMode.Unconstrained, ContentSizeFitter.FitMode.PreferredSize, true));
                var contentsScrollList = new DialogGUIScrollList(new Vector2(400, 300), false, true, vertical);
                dialogElements.Add(contentsScrollList);
                
                // allocations footer
                DialogGUIBase[] horizontal = new DialogGUIBase[7];
                horizontal[0] = new DialogGUISpace(2);
                horizontal[1] = new DialogGUILabel("Allocations: ");
                horizontal[2] = new DialogGUILabel("Funds & Strategy: " + GetAllocation(BudgetManager.Instance) + "%");
                horizontal[3] = new DialogGUILabel("|");
                horizontal[4] = new DialogGUILabel("Construction: " + GetAllocation(FacilityManager.Instance) + "%");
                horizontal[5] = new DialogGUILabel("|");
                horizontal[6] = new DialogGUILabel("Research: " + GetAllocation(ResearchManager.Instance) + "%");
                dialogElements.Add(new DialogGUIHorizontalLayout(horizontal));
                
                // close button
                var closeBbutton = new DialogGUIButton("Close", DismissCycleReport, false);
                dialogElements.Add(new DialogGUIHorizontalLayout(380, 30, closeBbutton) { stretchWidth = true });
            }
            cycleReportWindow = PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new MultiOptionDialog("BureaucracyMain", "", 
                    $"Bureaucracy: Budget Cycle Report - {KSPUtil.PrintDate(Planetarium.GetUniversalTime(), includeTime: false)}", 
                    UISkinManager.GetSkin("MainMenuSkin"), GetCycleReportRect(dialogElements), dialogElements.ToArray()), false, 
                    UISkinManager.GetSkin("MainMenuSkin"), false);

            
            Invoke("SetCycleBudgetScroll", 0.1f);
            

            return cycleReportWindow;
        }

        private void SetCycleBudgetScroll()
        {
            var contentScrollRect = GameObject.Find("_UIMaster/DialogCanvas/BureaucracyMain dialog handler/UIScrollViewPrefab(Clone)/ScrollList/");
            contentScrollRect.GetComponent<ScrollRect>().verticalNormalizedPosition = 1f;
        }

        private void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(SetupToolbarButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Remove(RemoveToolbarButton);
        }
    }
}