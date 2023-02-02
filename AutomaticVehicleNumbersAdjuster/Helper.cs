using AutomaticVehicleNumbersAdjuster.UI;
using ColossalFramework.UI;
using HarmonyLib;
using CitiesHarmony.API;
using ICities;
using System;
using System.IO;
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

    public static class Patcher
    {
        private const string HarmonyId = "Overhatted.AutomaticVehicleNumbersAdjuster";
        private static bool patched = false;

        public static void PatchAll()
        {
            if (patched) return;

            patched = true;
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(typeof(Patcher).Assembly);
        }

        public static void UnpatchAll()
        {
            if (!patched) return;

            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);
            patched = false;
        }
    }

    public class Loader : ILoadingExtension
    {
        public void OnCreated(ILoading loading)
        {
#if DEBUG
            Helper.PrintError("");
#endif

            if (HarmonyHelper.IsHarmonyInstalled) Patcher.PatchAll();
        }

        public void OnReleased()
        {
            if (HarmonyHelper.IsHarmonyInstalled) Patcher.UnpatchAll();
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