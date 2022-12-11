using ColossalFramework;
using HarmonyLib;

namespace AutomaticVehicleNumbersAdjuster
{
    [HarmonyPatch(typeof(TransportLine))]
    [HarmonyPatch(nameof(TransportLine.CalculateTargetVehicleCount))]
    public static class CalculateTargetVehicleCountMod
    {
        public static bool Prefix(ref TransportLine __instance, ref int __result)
        {
            ushort LineID = GetLineID(ref __instance);

            if (VehicleNumbersManager.TransportLines.TryGetValue(LineID, out TransportLineUsage TransportLineUsageInstance))
            {
                int PossibleResult = TransportLineUsageInstance.GetRecommendedNumberOfVehicles();
                if (PossibleResult > 0)
                {
                    __result = PossibleResult;
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// This function compares TransportLines by the m_stops member variable. Hopefully that's unique per TransportLine
        /// </summary>
        private static ushort GetLineID(ref TransportLine instance)
        {
            TransportManager TransportManager = Singleton<TransportManager>.instance;

            //When called from the SimulationStep method this guess will be correct
            ushort LineIDGuess = (ushort)(Singleton<SimulationManager>.instance.m_currentFrameIndex & 255u);

            if (TransportManager.m_lines.m_buffer[LineIDGuess].m_stops == instance.m_stops)
            {
                return LineIDGuess;
            }

            for (ushort lineID = 0; lineID <= byte.MaxValue; ++lineID)
            {
                if (TransportManager.m_lines.m_buffer[lineID].m_stops == instance.m_stops)
                {
                    return lineID;
                }
            }

            return 0;
        }
    }
}