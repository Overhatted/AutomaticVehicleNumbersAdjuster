using HarmonyLib;
using ColossalFramework.UI;

namespace AutomaticVehicleNumbersAdjuster.UI
{
    [HarmonyPatch(typeof(PublicTransportStopButton))]
    [HarmonyPatch("OnMouseDown")]
    public static class OnMouseDownMod
    {
        public static void Postfix(UIComponent component)
        {
            ushort StopID = (ushort)component.objectUserData;
            PassengersPerDayPanel.ShowPanel(StopID);
        }
    }
}