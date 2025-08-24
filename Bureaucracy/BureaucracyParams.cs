using System.Collections;
using System.Reflection;

namespace Bureaucracy
{
    class BureaucracyParams : GameParameters.CustomParameterNode
    {
        public override string Title { get { return ""; } } // column heading
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.CAREER; } }
        public override string Section { get { return "Bureaucracy"; } }
        public override string DisplaySection { get { return "Bureaucracy"; } }
        public override int SectionOrder { get { return 1; } }
        public override bool HasPresets { get { return false; } }

        [GameParameters.CustomIntParameterUI("Purchasable Parts Cost Adjustment (%)", minValue = 5, maxValue = 200, stepSize = 5,
            toolTip = "Costs to purchase parts in career mode will be adjusted by this factor \n" +
            "Default Value of 10 meaning purchasing cost for new parts will be adjusted to 10% of the original).",
            autoPersistance = true)]
        public int bureaucracyPurchasablePartsCostAdjustment = 10;
        
        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            return true; //otherwise return true
        }

        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {

            return true;
            //            return true; //otherwise return true
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }
}
