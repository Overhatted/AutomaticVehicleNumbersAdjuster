using ColossalFramework;
using System;

namespace AutomaticVehicleNumbersAdjuster
{
    public class Stop
    {
        [Flags]
        public enum UsageFlag
        {
            Small = 0,
            High = 1,
        }

        public UsageFlag Usage
        {
            get
            {
                if(this.UsageScore == 0)
                {
                    return UsageFlag.Small;
                }
                else
                {
                    return UsageFlag.High;
                }
            }
        }
        private byte UsageScore = 0;//Save

        private int[] NumberOfDailyPassengers;//NumberOfPassengers[PreviousDay, CurrentDay]//Save
        private ushort[,] NumberOfPassengersPerInterval;//NumberOfPassengers[DayIndex, HourIntervalIndex]//Save

        private byte LastDayIndex = 0;//Save
        private byte LastHourIntervalIndex = 0;//Save if Usage == UsageFlag.High

        public byte NumberOfConsecutiveFilledVehicles = 0;//Save

        public Stop()
        {
            this.NumberOfDailyPassengers = new int[2];
        }

        public void AddPassengers(int NumberOfPassengersInVehicle, uint CurrentFrame)
        {
            if (this.Usage == UsageFlag.Small)
            {
                byte DayIndex = Stop.CurrentDayIndex(CurrentFrame);

                if (this.LastDayIndex == DayIndex)
                {
                    this.NumberOfDailyPassengers[1] += NumberOfPassengersInVehicle;
                }
                else
                {
                    this.NumberOfDailyPassengers[0] = this.NumberOfDailyPassengers[1];

                    this.NumberOfDailyPassengers[1] = NumberOfPassengersInVehicle;

                    this.LastDayIndex = DayIndex;
                }
            }
            else
            {
                byte DayIndex = Stop.CurrentDayIndex(CurrentFrame);
                byte HourIntervalIndex = Stop.CurrentHourIntervalIndex(CurrentFrame);

                if (this.LastDayIndex == DayIndex && this.LastHourIntervalIndex == HourIntervalIndex)
                {
                    this.NumberOfPassengersPerInterval[this.LastDayIndex, HourIntervalIndex] += (ushort)NumberOfPassengersInVehicle;
                }
                else
                {
                    this.NumberOfPassengersPerInterval[DayIndex, HourIntervalIndex] = (ushort)NumberOfPassengersInVehicle;

                    this.LastDayIndex = DayIndex;
                    this.LastHourIntervalIndex = HourIntervalIndex;
                }
            }
        }

        public void AddUsageScore(bool WasRelevant, uint CurrentFrame)
        {
            if (WasRelevant)
            {
                if(this.UsageScore == 0)
                {
                    ushort AverageNumberOfPassengersPerInterval = this.GetAverageIntervalPassengers(0);
                    ushort CurrentDayPassengers = (ushort) this.GetCurrentDayPassengers();

                    this.NumberOfPassengersPerInterval = new ushort[VehicleNumbersManager.CurrentSettings.NumberOfDaysToStore, VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore];

                    byte CurrentDayIndex = Stop.CurrentDayIndex(CurrentFrame);
                    byte PreviousDayIndex = (byte)MathFunctions.AddToIndex(CurrentDayIndex, -1, VehicleNumbersManager.CurrentSettings.NumberOfDaysToStore);

                    for (byte j = 0; j < VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore; j++)
                    {
                        this.NumberOfPassengersPerInterval[PreviousDayIndex, j] = AverageNumberOfPassengersPerInterval;
                    }

                    this.NumberOfPassengersPerInterval[CurrentDayIndex, 0] = CurrentDayPassengers;

                    this.UsageScore = VehicleNumbersManager.CurrentSettings.NumberOfDaysToStore;

                    this.NumberOfDailyPassengers = null;
                }
                else
                {
                    this.UsageScore = VehicleNumbersManager.CurrentSettings.NumberOfDaysToStore;
                }
            }
            else
            {
                if(this.UsageScore == 0)
                {

                }
                else if(this.UsageScore == 1)
                {
                    int AverageDailyPassengers = this.GetAverageDailyPassengers();
                    int CurrentDayPassengers = this.GetCurrentDayPassengers();

                    this.NumberOfDailyPassengers = new int[] { AverageDailyPassengers, CurrentDayPassengers };

                    this.UsageScore = 0;

                    this.NumberOfPassengersPerInterval = null;
                }
                else
                {
                    this.UsageScore--;
                }
            }
        }

        public int GetAverageDailyPassengers()
        {
            if(this.Usage == UsageFlag.Small)
            {
                return this.NumberOfDailyPassengers[0];
            }
            else
            {
                int SumOfPassengersFromEachInterval = 0;
                for (byte i = 0; i < VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore; i++)
                {
                    SumOfPassengersFromEachInterval += this.GetAverageIntervalPassengers(i);
                }

                return SumOfPassengersFromEachInterval;
            }
        }

        public int GetCurrentDayPassengers()
        {
            if (this.Usage == UsageFlag.Small)
            {
                return this.NumberOfDailyPassengers[1];
            }
            else
            {
                uint CurrentFrame = Singleton<SimulationManager>.instance.m_currentFrameIndex;
                byte CurrentDayIndex = Stop.CurrentDayIndex(CurrentFrame);

                if(CurrentDayIndex == this.LastDayIndex)
                {
                    byte CurrentHourIntervalIndex = Stop.CurrentHourIntervalIndex(CurrentFrame);

                    int SumOfPassengersFromEachInterval = 0;
                    for (byte i = 0; i < VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore; i++)
                    {
                        if (i <= CurrentHourIntervalIndex)
                        {
                            SumOfPassengersFromEachInterval += this.NumberOfPassengersPerInterval[CurrentDayIndex, i];
                        }
                    }

                    return SumOfPassengersFromEachInterval;
                }
                else
                {
                    return 0;
                }
            }
        }

        public ushort GetAverageIntervalPassengers(byte IntervalIndex)
        {
            if (this.Usage == UsageFlag.Small)
            {
                return (ushort) (this.NumberOfDailyPassengers[0] / VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore);
            }
            else
            {
                int SumOfPassengersInTheInterval = 0;
                int NumberOfDays = 0;
                if (IntervalIndex == this.LastHourIntervalIndex)//If IntervalIndex == CurrentInterval: ignore current day
                {
                    SumOfPassengersInTheInterval = - this.NumberOfPassengersPerInterval[this.LastDayIndex, this.LastHourIntervalIndex];
                    NumberOfDays--;
                }
                
                for (byte i = 0; i < VehicleNumbersManager.CurrentSettings.NumberOfDaysToStore; i++)
                {
                    if(this.NumberOfPassengersPerInterval[i, IntervalIndex] != 0)
                    {
                        SumOfPassengersInTheInterval += this.NumberOfPassengersPerInterval[i, IntervalIndex];
                        NumberOfDays++;
                    }
                }

                if(NumberOfDays > 0)
                {
                    return (ushort)(SumOfPassengersInTheInterval / NumberOfDays);
                }
                else
                {
                    return 0;
                }
            }
        }

        public ushort[,] GetPassengersPerIntervalTable()
        {
            uint CurrentFrame = Singleton<SimulationManager>.instance.m_currentFrameIndex;
            byte CurrentDayIndex = Stop.CurrentDayIndex(CurrentFrame);
            byte CurrentHourIntervalIndex = Stop.CurrentHourIntervalIndex(CurrentFrame);

            int NumberOfDaysToStore = VehicleNumbersManager.CurrentSettings.NumberOfDaysToStore;
            int NumberOfHourIntervalsToStore = VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore;

            ushort[,] PassengersPerIntervalTable = new ushort[NumberOfDaysToStore, NumberOfHourIntervalsToStore];

            //Fill Current Day Row (if CurrentDayIndex != this.LastDayIndex keep everything at zero)
            if (CurrentDayIndex == this.LastDayIndex)
            {
                for (int ii = 0; ii != NumberOfHourIntervalsToStore; ii++)
                {
                    if (ii <= CurrentHourIntervalIndex && ii <= this.LastHourIntervalIndex)
                    {
                        PassengersPerIntervalTable[NumberOfDaysToStore - 1, ii] = this.NumberOfPassengersPerInterval[CurrentDayIndex, ii];
                    }
                }
            }

            //Fill the rest of the days' row
            for (int i = 1; i != NumberOfDaysToStore; i++)
            {
                int RowIndexInNumberOfPassengersPerIntervalTable = MathFunctions.AddToIndex(CurrentDayIndex, -i, NumberOfDaysToStore);

                for (int ii = 0; ii != NumberOfHourIntervalsToStore; ii++)
                {
                    PassengersPerIntervalTable[NumberOfDaysToStore - 1 - i, ii] = this.NumberOfPassengersPerInterval[RowIndexInNumberOfPassengersPerIntervalTable, ii];
                }
            }

#if DEBUG
            for (int i = 0; i != NumberOfDaysToStore; i++)
            {
                for (int ii = 0; ii != NumberOfHourIntervalsToStore; ii++)
                {
                    PassengersPerIntervalTable[i, ii] = this.NumberOfPassengersPerInterval[i, ii];
                }
            }

            Helper.PrintError("CurrentDayIndex: " + CurrentDayIndex + "; CurrentHourIntervalIndex: " + CurrentHourIntervalIndex);
#endif

            return PassengersPerIntervalTable;
        }

        public static ushort CurrentDay(uint CurrentFrame)
        {
            return (ushort)(CurrentFrame / SimulationManager.DAYTIME_FRAMES);
        }

        public static byte CurrentDayIndex(uint CurrentFrame)
        {
            return (byte)(Stop.CurrentDay(CurrentFrame) % VehicleNumbersManager.CurrentSettings.NumberOfDaysToStore);
        }

        public static byte CurrentHourIntervalIndex(uint CurrentFrame)
        {
            float CurrentDayFrame = CurrentFrame % SimulationManager.DAYTIME_FRAMES;
            return (byte)(CurrentDayFrame / SimulationManager.DAYTIME_FRAMES * VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore);
        }

        public void WriteStop(FastList<byte> Data)
        {
            StorageData.WriteByte(this.LastDayIndex, Data);

            if (VehicleNumbersManager.CurrentSettings.EnableExtraVehicles)
            {
                StorageData.WriteByte(this.NumberOfConsecutiveFilledVehicles, Data);
            }

            StorageData.WriteByte(this.UsageScore, Data);

            if(this.Usage == UsageFlag.Small)
            {
                StorageData.WriteInt32ArrayWithoutLength(this.NumberOfDailyPassengers, Data);

                //Size 11
            }
            else
            {
                StorageData.WriteByte(this.LastHourIntervalIndex, Data);

                StorageData.WriteUInt16TwoDimensionalArrayWithoutLength(this.NumberOfPassengersPerInterval, Data);

                //Size 4 + 2 * Number Of Hour Intervals To Store * Number Of Days To Store = 60
            }
        }

        public Stop(ushort Version, byte[] Data, ref int Index, Settings LoadedSettings)
        {
            this.LastDayIndex = StorageData.ReadByte(Data, ref Index);

            if (LoadedSettings.EnableExtraVehicles)
            {
                this.NumberOfConsecutiveFilledVehicles = StorageData.ReadByte(Data, ref Index);
            }

            this.UsageScore = StorageData.ReadByte(Data, ref Index);

            if (this.Usage == UsageFlag.Small)
            {
                this.NumberOfDailyPassengers = StorageData.ReadInt32ArrayWithoutLength(Data, ref Index, 2);
            }
            else
            {
                this.LastHourIntervalIndex = StorageData.ReadByte(Data, ref Index);

                ushort[,] StoredNumberOfPassengersPerInterval = StorageData.ReadUInt16TwoDimensionalArrayWithoutLength(Data, ref Index, LoadedSettings.NumberOfDaysToStore, LoadedSettings.NumberOfHourIntervalsToStore);

                if (VehicleNumbersManager.CurrentSettings.NumberOfDaysToStore == LoadedSettings.NumberOfDaysToStore && VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore == LoadedSettings.NumberOfHourIntervalsToStore)
                {
                    this.NumberOfPassengersPerInterval = StoredNumberOfPassengersPerInterval;
                }
                else
                {
                    this.NumberOfPassengersPerInterval = new ushort[VehicleNumbersManager.CurrentSettings.NumberOfDaysToStore, VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore];

                    if (VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore == LoadedSettings.NumberOfHourIntervalsToStore)
                    {
                        for (byte i = 0; i != VehicleNumbersManager.CurrentSettings.NumberOfDaysToStore && i != LoadedSettings.NumberOfDaysToStore; i++)
                        {
                            for (byte j = 0; j != VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore; j++)
                            {
                                this.NumberOfPassengersPerInterval[i, j] = StoredNumberOfPassengersPerInterval[i, j];
                            }
                        }
                    }
                    else
                    {
                        uint[] DailyPassengers = new uint[LoadedSettings.NumberOfDaysToStore];

                        for (byte i = 0; i != LoadedSettings.NumberOfDaysToStore; i++)
                        {
                            for (byte j = 0; j != LoadedSettings.NumberOfHourIntervalsToStore; j++)
                            {
                                DailyPassengers[i] += StoredNumberOfPassengersPerInterval[i, j];
                            }
                        }

                        for (byte i = 0; i != VehicleNumbersManager.CurrentSettings.NumberOfDaysToStore && i != LoadedSettings.NumberOfDaysToStore; i++)
                        {
                            ushort NumberOfPassengersPerInterval = (ushort)(DailyPassengers[i] / VehicleNumbersManager.CurrentSettings.NumberOfDaysToStore);
                            for (byte j = 0; j != VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore; j++)
                            {
                                this.NumberOfPassengersPerInterval[i, j] = NumberOfPassengersPerInterval;
                            }
                        }
                    }
                }
            }
        }
    }
}
