using AutomaticVehicleNumbersAdjuster.UI;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using Harmony;
using ICities;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace AutomaticVehicleNumbersAdjuster
{
    public class AutomaticVehicleNumbersAdjuster : IUserMod
    {
        public string Name
        {
            get
            {
                return "Automatic Vehicle Numbers Adjuster";
            }
        }

        public string Description
        {
            get
            {
                return "Automatically adjusts the number of vehicles in public transport lines";
            }
        }
    }

    public static class Helper
    {
        public static void PrintError(string Message)
        {
#if DEBUG
            File.AppendAllText(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
        "\\AVNALog.txt", Message + Environment.NewLine);
#else
            Debug.Log("[Automatic Vehicle Numbers Adjuster] " + Message);
#endif
        }
    }

    public class Loader : ILoadingExtension
    {
        public void OnCreated(ILoading loading)
        {
#if DEBUG
            Helper.PrintError("");
#endif

            var harmony = HarmonyInstance.Create("com.overhatted.automaticvehiclenumbersadjuster");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void OnReleased()
        {

        }

        public void OnLevelLoaded(LoadMode mode)
        {
            if (mode == LoadMode.NewGame || mode == LoadMode.LoadGame)
            {
                VehicleNumbersManager.OnLoad();

                UIView.GetAView().AddUIComponent(typeof(PassengersPerDayPanel));
                UIView.GetAView().AddUIComponent(typeof(PassengersPerIntervalPanel));
            }
        }

        public void OnLevelUnloading()
        {

        }
    }
}