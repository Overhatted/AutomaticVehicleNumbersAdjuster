using ColossalFramework;
using ColossalFramework.UI;
using System.Collections.Generic;
using UnityEngine;

namespace AutomaticVehicleNumbersAdjuster.UI
{
    public class PassengersPerDayPanel : UIPanel
    {
        private const float PanelHeight = TitleHeight + LabelsHeight + ColumnsHeight;

        private const float TitleHeight = 40;
        private const float LabelsHeight = 20;
        private const float ColumnsHeight = 30;

        private const float TableWidth = NumberOfColumns * ColumnsWidth;

        private const int NumberOfColumns = 2;
        private const float ColumnsWidth = 200;

        public static PassengersPerDayPanel PassengersPerDayPanelRef;
        private static UITextField PreviousDayUsageRef;
        private static UITextField CurrentDayUsageRef;

        public static Stop CurrentStop;

        public override void Start()
        {
            this.name = "StopDailyPassengers";
            this.width = TableWidth;
            this.height = PanelHeight;
            this.backgroundSprite = "MenuPanel2";
            this.canFocus = true;
            this.isInteractive = true;
            this.isVisible = false;
            this.eventVisibilityChanged += PassengersPerDayPanel.OnVisibilityChanged;

            PassengersPerDayPanelRef = this;

            UILabel TitleUILabel = this.AddUIComponent<UILabel>();
            TitleUILabel.text = "Stop Usage";
            TitleUILabel.textAlignment = UIHorizontalAlignment.Center;
            TitleUILabel.position = new Vector3(this.width / 2f - TitleUILabel.width / 2f, -20f + TitleUILabel.height / 2f);

            UIButton CloseButton = this.AddUIComponent<UIButton>();
            CloseButton.width = 32;
            CloseButton.height = 32;
            CloseButton.normalBgSprite = "buttonclose";
            CloseButton.hoveredBgSprite = "buttonclosehover";
            CloseButton.pressedBgSprite = "buttonclosepressed";
            CloseButton.relativePosition = new Vector3(this.width - CloseButton.width - 2f, 2f);
            CloseButton.eventClick += new MouseEventHandler(this.OnCloseButtonClick);

            //Labels
            UIPanel LabelsPanel = this.AddUIComponent<UIPanel>();
            LabelsPanel.width = LabelsPanel.parent.width;
            LabelsPanel.height = LabelsHeight;
            LabelsPanel.relativePosition = new Vector3(0, TitleHeight);

            UITextField PreviousDayUsageLabel = LabelsPanel.AddUIComponent<UITextField>();
            PreviousDayUsageLabel.text = "Average Daily Usage";
            PreviousDayUsageLabel.verticalAlignment = UIVerticalAlignment.Middle;
            PreviousDayUsageLabel.width = ColumnsWidth;
            PreviousDayUsageLabel.height = LabelsHeight;
            PreviousDayUsageLabel.relativePosition = new Vector3(0, 0);

            UITextField CurrentDayUsageLabel = LabelsPanel.AddUIComponent<UITextField>();
            CurrentDayUsageLabel.text = "Current Day Usage";
            CurrentDayUsageLabel.verticalAlignment = UIVerticalAlignment.Middle;
            CurrentDayUsageLabel.width = ColumnsWidth;
            CurrentDayUsageLabel.height = LabelsHeight;
            CurrentDayUsageLabel.relativePosition = new Vector3(ColumnsWidth, 0);

            //Table
            UIPanel UsagePanel = this.AddUIComponent<UIPanel>();
            UsagePanel.relativePosition = new Vector3(0, TitleHeight + LabelsHeight);
            UsagePanel.width = TableWidth;
            UsagePanel.height = ColumnsHeight;

            PreviousDayUsageRef = UsagePanel.AddUIComponent<UITextField>();
            PreviousDayUsageRef.verticalAlignment = UIVerticalAlignment.Middle;
            PreviousDayUsageRef.width = ColumnsWidth;
            PreviousDayUsageRef.height = ColumnsHeight;
            PreviousDayUsageRef.relativePosition = new Vector3(0, 0);

            CurrentDayUsageRef = UsagePanel.AddUIComponent<UITextField>();
            CurrentDayUsageRef.verticalAlignment = UIVerticalAlignment.Middle;
            CurrentDayUsageRef.width = ColumnsWidth;
            CurrentDayUsageRef.height = ColumnsHeight;
            CurrentDayUsageRef.relativePosition = new Vector3(ColumnsWidth, 0);
        }

        public static void ShowPanel(ushort StopID)
        {
            foreach (KeyValuePair<ushort, TransportLineUsage> TransportLineUsageKV in VehicleNumbersManager.TransportLines)
            {
                foreach (KeyValuePair<ushort, Stop> StopKV in TransportLineUsageKV.Value.Stops)
                {
                    if (StopKV.Key == StopID)
                    {
                        CurrentStop = StopKV.Value;

                        PassengersPerDayPanelRef.isVisible = true;

                        PreviousDayUsageRef.text = CurrentStop.GetAverageDailyPassengers().ToString();
                        CurrentDayUsageRef.text = CurrentStop.GetCurrentDayPassengers().ToString();

                        if (CurrentStop.Usage == Stop.UsageFlag.High)
                        {
                            PassengersPerIntervalPanel.ShowPanel(CurrentStop);
                        }
                        else
                        {
                            PassengersPerIntervalPanel.HidePanel();
                        }

                        return;
                    }
                }
            }
        }

        public static void OnVisibilityChanged(UIComponent Component, bool Value)
        {
            if (Value == false)
            {
                PassengersPerIntervalPanel.HidePanel();
            }
        }

        private void OnCloseButtonClick(UIComponent Component, UIMouseEventParameter EventParam)
        {
            this.isVisible = false;
        }
    }
}