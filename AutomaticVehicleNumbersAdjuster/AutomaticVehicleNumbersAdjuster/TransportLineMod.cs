using ColossalFramework;
using HarmonyLib;
using System;
using UnityEngine;

namespace AutomaticVehicleNumbersAdjuster
{
    [HarmonyPatch(typeof(TransportLine))]
    [HarmonyPatch(nameof(TransportLine.CalculateTargetVehicleCount))]
    public static class CalculateTargetVehicleCountMod
    {
        public static bool Prefix(ref TransportLine __instance, ref int __result)
        {
            ushort LineID = (ushort)(Singleton<SimulationManager>.instance.m_currentFrameIndex & 255u);

            if (VehicleNumbersManager.TransportLines.TryGetValue(LineID, out TransportLineUsage TransportLineUsageInstance))
            {
                __result = TransportLineUsageInstance.GetRecommendedNumberOfVehicles();
            }
            else
            {
                __result = TransportLineMod.CalculateTargetVehicleCount(LineID);
            }

            return false;
        }
    }

    public static class TransportLineMod
    {
        public static int CalculateTargetVehicleCount(ushort LineID)
        {
            TransportLine TransportLineInstance = Singleton<TransportManager>.instance.m_lines.m_buffer[LineID];
            TransportInfo TransportLineInfo = TransportLineInstance.Info;

            float LineLength = TransportLineInstance.m_totalLength;
            if (LineLength == 0f && TransportLineInstance.m_stops != 0)
            {
                NetManager NetManagerInstance = Singleton<NetManager>.instance;
                ushort FirstStopID = TransportLineInstance.m_stops;
                ushort CurrentStopID = FirstStopID;
                int ListLimiter = 0;
                while (CurrentStopID != 0)
                {
                    ushort NextStopID = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        ushort SegmentID = NetManagerInstance.m_nodes.m_buffer[(int)CurrentStopID].GetSegment(i);
                        if (SegmentID != 0 && NetManagerInstance.m_segments.m_buffer[(int)SegmentID].m_startNode == CurrentStopID)
                        {
                            LineLength += NetManagerInstance.m_segments.m_buffer[(int)SegmentID].m_averageLength;
                            NextStopID = NetManagerInstance.m_segments.m_buffer[(int)SegmentID].m_endNode;
                            break;
                        }
                    }
                    CurrentStopID = NextStopID;
                    if (CurrentStopID == FirstStopID)
                    {
                        break;
                    }
                    if (++ListLimiter >= 32768)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }
            }
            int Budget = Singleton<EconomyManager>.instance.GetBudget(TransportLineInfo.m_class);
            Budget = (Budget * (int)TransportLineInstance.m_budget + 50) / 100;
            return Mathf.CeilToInt((float)Budget * LineLength / (TransportLineInfo.m_defaultVehicleDistance * 100f));
        }
    }
}