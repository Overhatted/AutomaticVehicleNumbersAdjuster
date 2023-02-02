using HarmonyLib;
using System.Reflection;
using ColossalFramework.UI;
using UnityEngine;
using ColossalFramework;

namespace AutomaticVehicleNumbersAdjuster.UI
{
    [HarmonyPatch(typeof(PublicTransportWorldInfoPanel))]
    [HarmonyPatch("OnSetTarget")]
    public class OnSetTargetMod
    {
        public static void Postfix(PublicTransportWorldInfoPanel __instance)
        {
            var Method = __instance.GetType().GetMethod("GetLineID", BindingFlags.NonPublic | BindingFlags.Instance);
            ushort LineID = (ushort)Method.Invoke(__instance, new object[] { });

            if (VehicleNumbersManager.TransportLines.TryGetValue(LineID, out TransportLineUsage TransportLineUsageObject))
            {
                var Field = __instance.GetType().GetField("m_stopButtons", BindingFlags.NonPublic | BindingFlags.Instance);
                UITemplateList<UIButton> StopButtons = (UITemplateList<UIButton>)Field.GetValue(__instance);

                float MaximumNumberOfPassengersPerInterval;
                if (TransportLineUsageObject.MaximumNumberOfPassengersPerInterval == 0)
                {
                    MaximumNumberOfPassengersPerInterval = float.MaxValue;
                }
                else
                {
                    MaximumNumberOfPassengersPerInterval = TransportLineUsageObject.MaximumNumberOfPassengersPerInterval;
                }

                foreach (UIButton Current in StopButtons.items)
                {
                    ushort StopID = (ushort)Current.objectUserData;
                    if (TransportLineUsageObject.Stops.TryGetValue(StopID, out Stop StopObject))
                    {
                        ushort StopMaximumNumberOfPassengersPerInterval = 0;
                        for (byte i = 0; i != VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore; i++)
                        {
                            ushort NumberOfPassengersInThisInterval = StopObject.GetAverageIntervalPassengers(i);
                            if (NumberOfPassengersInThisInterval > StopMaximumNumberOfPassengersPerInterval)
                            {
                                StopMaximumNumberOfPassengersPerInterval = NumberOfPassengersInThisInterval;
                            }
                        }

                        float CapacityUsage = Mathf.Clamp01(StopMaximumNumberOfPassengersPerInterval / MaximumNumberOfPassengersPerInterval);

                        Current.color = Color.HSVToRGB((1 - CapacityUsage) / 3, 1f, 0.5f);
                    }
                }
            }
        }
    }

#if DEBUG
    [HarmonyPatch(typeof(PublicTransportWorldInfoPanel))]
    [HarmonyPatch("UpdateBindings")]
    public class UpdateBindingsMod
    {
        public static void Postfix(PublicTransportWorldInfoPanel __instance)
        {
            var Method = __instance.GetType().GetMethod("GetLineID", BindingFlags.NonPublic | BindingFlags.Instance);
            ushort LineID = (ushort)Method.Invoke(__instance, new object[] { });

            if (VehicleNumbersManager.TransportLines.TryGetValue(LineID, out TransportLineUsage TransportLineUsageObject))
            {
                var Field = __instance.GetType().GetField("m_VehicleAmount", BindingFlags.NonPublic | BindingFlags.Instance);
                UILabel VehicleAmountLabel = (UILabel)Field.GetValue(__instance);

                TransportLine TransportLineInstance = Singleton<TransportManager>.instance.m_lines.m_buffer[LineID];

                string VehicleAmountLabelText = ";" + TransportLineUsageObject.GetRecommendedNumberOfVehicles();

                if (VehicleNumbersManager.CurrentSettings.EnableExtraVehicles)
                {
                    VehicleAmountLabelText += ";E: " + TransportLineUsageObject.ExtraVehicles;
                }

                VehicleInfo VehicleInfoInstance = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[TransportLineInstance.m_vehicles].Info;

                VehicleAmountLabelText += ";" + TransportLineUsageObject.PeriodCalculator.Period;
                VehicleAmountLabelText += ";" + TransportLineUsageObject.PeriodCalculator.PredictedPeriod;
                VehicleAmountLabelText += ";" + PeriodPredictor.PredictPeriod(LineID);
                VehicleAmountLabelText += ";" + TransportLineInstance.m_totalLength + "|" + VehicleInfoInstance.m_acceleration + "|" + VehicleInfoInstance.m_braking + "|" + VehicleInfoInstance.m_maxSpeed;


                VehicleAmountLabel.text += VehicleAmountLabelText;
            }
        }
    }
#endif
}