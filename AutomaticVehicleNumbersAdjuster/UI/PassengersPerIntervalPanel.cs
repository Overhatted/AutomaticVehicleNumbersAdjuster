using ColossalFramework.UI;
using UnityEngine;

namespace AutomaticVehicleNumbersAdjuster.UI
{
    public class PassengersPerIntervalPanel : UIPanel
    {
        private const float TitleHeight = 40;

        private const float RowsLabelWidth = 110;
        private const float ColumnsLabelHeight = 20;

        private const float ColumnsWidth = 50;
        private const float RowsHeight = 30;

        private static PassengersPerIntervalPanel PassengersPerIntervalPanelRef;
        private static UIPanel TablePanelRef;

        public override void Start()
        {
            int NumberOfRows = VehicleNumbersManager.CurrentSettings.NumberOfDaysToStore;
            int NumberOfColumns = VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore;

            this.name = "PassengersPerInterval";
            this.width = RowsLabelWidth + NumberOfColumns * ColumnsWidth;
            this.height = TitleHeight + ColumnsLabelHeight + NumberOfRows * RowsHeight;
            this.backgroundSprite = "MenuPanel2";
            this.canFocus = true;
            this.isInteractive = true;
            this.isVisible = true;

            this.AlignTo(PassengersPerDayPanel.PassengersPerDayPanelRef, UIAlignAnchor.TopLeft);
            this.relativePosition = new Vector3(0, PassengersPerDayPanel.PassengersPerDayPanelRef.height);

            PassengersPerIntervalPanelRef = this;

            UILabel TitleUILabel = this.AddUIComponent<UILabel>();
            TitleUILabel.text = "Number of passengers per interval";
            TitleUILabel.textAlignment = UIHorizontalAlignment.Center;
            TitleUILabel.position = new Vector3(this.width / 2f - TitleUILabel.width / 2f, -20f + TitleUILabel.height / 2f);

            //Table
            //Top row
            TablePanelRef = this.AddUIComponent<UIPanel>();
            TablePanelRef.width = TablePanelRef.parent.width;
            TablePanelRef.height = ColumnsLabelHeight + NumberOfRows * RowsHeight;
            TablePanelRef.relativePosition = new Vector3(0, TitleHeight);

            //Empty cell top left
            UITextField EmptyCell = TablePanelRef.AddUIComponent<UITextField>();
            EmptyCell.width = RowsLabelWidth;
            EmptyCell.height = ColumnsLabelHeight;
            EmptyCell.relativePosition = new Vector3(0, 0);

            //Columns Labels
            for (int i = 0; i != NumberOfColumns; i++)
            {
                UITextField ColumnLabel = TablePanelRef.AddUIComponent<UITextField>();
                ColumnLabel.text = i.ToString();
                ColumnLabel.verticalAlignment = UIVerticalAlignment.Middle;
                ColumnLabel.width = ColumnsWidth;
                ColumnLabel.height = ColumnsLabelHeight;
                ColumnLabel.relativePosition = new Vector3(RowsLabelWidth + i * ColumnsWidth, 0);
            }

            //Rest of the table
            for (int i = 0; i != NumberOfRows; i++)
            {
                //Rows Labels
                UITextField RowLabel = TablePanelRef.AddUIComponent<UITextField>();
                int DaysAgo = NumberOfRows - i - 1;
                if(DaysAgo == 0)
                {
                    RowLabel.text = "Today";
                }
                else if(DaysAgo == 1)
                {
                    RowLabel.text = "Yesterday";
                }
                else
                {
                    RowLabel.text = DaysAgo + " Days Ago";
                }
                RowLabel.verticalAlignment = UIVerticalAlignment.Middle;
                RowLabel.width = RowsLabelWidth;
                RowLabel.height = RowsHeight;
                RowLabel.relativePosition = new Vector3(0, ColumnsLabelHeight + i * RowsHeight);

                for (int ii = 0; ii != NumberOfColumns; ii++)
                {
                    UITextField TableCell = TablePanelRef.AddUIComponent<UITextField>();
                    TableCell.verticalAlignment = UIVerticalAlignment.Middle;
                    TableCell.width = ColumnsWidth;
                    TableCell.height = RowsHeight;
                    TableCell.relativePosition = new Vector3(RowsLabelWidth + ii * ColumnsWidth, ColumnsLabelHeight + i * RowsHeight);
                }
            }
        }

        public static void ShowPanel(Stop CurrentStop)
        {
            int NumberOfRows = VehicleNumbersManager.CurrentSettings.NumberOfDaysToStore;
            int NumberOfColumns = VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore;

            ushort[,] PassengersPerIntervalTable = PassengersPerDayPanel.CurrentStop.GetPassengersPerIntervalTable();

            Component[] Children = PassengersPerIntervalPanelRef.GetComponentsInChildren(typeof(UITextField));

            for (int i = 0; i != NumberOfRows; i++)
            {
                for (int ii = 0; ii != NumberOfColumns; ii++)
                {
                    int ChildrenIndex = (i + 1) * (NumberOfColumns + 1) + (ii + 1);
                    UITextField TextField = (UITextField)Children[ChildrenIndex];
                    TextField.text = PassengersPerIntervalTable[i, ii].ToString();
                }
            }

            PassengersPerIntervalPanelRef.isVisible = true;
        }

        public static void HidePanel()
        {
            if (PassengersPerIntervalPanelRef != null)
            {
                PassengersPerIntervalPanelRef.isVisible = false;
            }
        }
    }
}