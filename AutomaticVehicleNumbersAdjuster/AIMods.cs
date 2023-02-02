using HarmonyLib;

namespace AutomaticVehicleNumbersAdjuster
{
    [HarmonyPatch(typeof(BusAI))]
    [HarmonyPatch("LoadPassengers")]
    public class BusAIMod
    {
        public static void Postfix(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
            TransportLineUsage.AfterLoadPassengers(vehicleID, ref data, currentStop);
        }
    }

    /*[HarmonyPatch(typeof(CableCarAI))]
    [HarmonyPatch("LoadPassengers")]
    public class CableCarAIMod
    {
        public static void Postfix(ushort vehicleID, ref Vehicle data, ushort currentStop)
        {
            TransportLineUsage TransportLineUsageObj;
            if (VehicleNumbersManager.TransportLines.TryGetValue(data.m_transportLine, out TransportLineUsageObj))
            {
                TransportLineUsageObj.AfterLoadPassengers(vehicleID, ref data, currentStop);
            }
        }
    }*/

    [HarmonyPatch(typeof(PassengerFerryAI))]
    [HarmonyPatch("LoadPassengers")]
    public class PassengerFerryAIMod
    {
        public static void Postfix(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
            TransportLineUsage.AfterLoadPassengers(vehicleID, ref data, currentStop);
        }
    }

    [HarmonyPatch(typeof(PassengerBlimpAI))]
    [HarmonyPatch("LoadPassengers")]
    public class PassengerBlimpAIMod
    {
        public static void Postfix(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
            TransportLineUsage.AfterLoadPassengers(vehicleID, ref data, currentStop);
        }
    }

    [HarmonyPatch(typeof(PassengerTrainAI))]
    [HarmonyPatch("LoadPassengers")]
    public class PassengerTrainAIMod
    {
        public static void Postfix(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
            TransportLineUsage.AfterLoadPassengers(vehicleID, ref data, currentStop);
        }
    }

    [HarmonyPatch(typeof(TramAI))]
    [HarmonyPatch("LoadPassengers")]
    public class TramAIMod
    {
        public static void Postfix(ushort vehicleID, ref Vehicle data, ushort currentStop, ushort nextStop)
        {
            TransportLineUsage.AfterLoadPassengers(vehicleID, ref data, currentStop);
        }
    }
}