using System;
using System.Linq;

namespace Bureaucracy
{
    public class ManagerProgressEvent : BureaucracyEvent
    {
        public ManagerProgressEvent()
        {
            CompletionTime = Planetarium.GetUniversalTime() + FlightGlobals.GetHomeBody().solarDayLength;
            AddTimer();
        }
        public void BellCheck()
        {
            double lst = Utilities.LocalSolarTime(FlightGlobals.GetHomeBody());
            ManagerProgressEvent.solarDayLength = FlightGlobals.GetHomeBody().solarDayLength;
            if (lst > 24.0)
            {
                lst -= 24.0;
            }
            else if (lst < 0.0)
            {
                lst += 24.0;
            }
            lst = lst / 24.0 * ManagerProgressEvent.solarDayLength;
            if (ManagerProgressEvent.bellTime < 0.0)
            {
                if (lst > ManagerProgressEvent.solarDayLength * 0.1)
                {
                    return;
                }
                do
                {
                    ManagerProgressEvent.bellTime = (double)this.autoRand.Next((int)ManagerProgressEvent.solarDayLength) + this.autoRand.NextDouble();
                }
                while (ManagerProgressEvent.bellTime <= ManagerProgressEvent.solarDayLength * 0.1);
            }
            else if (ManagerProgressEvent.bellTime > ManagerProgressEvent.solarDayLength)
            {
                do
                {
                    ManagerProgressEvent.bellTime = (double)this.autoRand.Next((int)ManagerProgressEvent.solarDayLength) + this.autoRand.NextDouble();
                }
                while (ManagerProgressEvent.bellTime <= lst);
            }
            if (ManagerProgressEvent.bellTime <= lst)
            {
                for (int i = 0; i < Bureaucracy.Instance.registeredManagers.Count; i++)
                {
                    Bureaucracy.Instance.registeredManagers.ElementAt(i).ProgressTask();
                }
                Bureaucracy.Instance.lastProgressUpdate = Planetarium.GetUniversalTime();
                ManagerProgressEvent.bellTime = double.MinValue;
                RandomEventLoader.Instance.RollEvent();
                if (SettingsClass.Instance.RetirementEnabled)
                {
                    CrewManager.Instance.ProcessRetirees(); //We do this once a day too, because people don't need to wait a cycle to retire!
                }
                return;
            }
        }
        private static double bellTime = double.MaxValue;

        private Random autoRand = new Random();

        private static double solarDayLength = FlightGlobals.GetHomeBody().solarDayLength;
        public override void OnEventCompleted()
        {
            for (int i = 0; i < Bureaucracy.Instance.registeredManagers.Count; i++)
            {
                Manager m = Bureaucracy.Instance.registeredManagers.ElementAt(i);
                m.ProgressTask();
            }
            Bureaucracy.Instance.progressEvent = new ManagerProgressEvent();
            Bureaucracy.Instance.lastProgressUpdate = Planetarium.GetUniversalTime();
        }
    }
}