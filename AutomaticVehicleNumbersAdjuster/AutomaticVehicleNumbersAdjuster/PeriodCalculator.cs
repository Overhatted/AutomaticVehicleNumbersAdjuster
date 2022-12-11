using ColossalFramework;
using System;

namespace AutomaticVehicleNumbersAdjuster
{
    public class PeriodCalculator
    {
        public int PredictedPeriod;
        public int Period;//Save

        public uint LastFramePredictedPeriodCalculated;
        public const uint CalculatePredictedPeriodEveryThisFrames = 4096;

        public ushort VehicleID;//Save
        public ushort StopID;//Save

        private const int NumberOfArrivalTimesToKeep = 4;
        private uint[] ArrivalTimes = new uint[NumberOfArrivalTimesToKeep];//Save

        public PeriodCalculator()
        {
            
        }

        public void AddArrivalTime(ushort CurrentVehicleID, ushort CurrentStopID, uint CurrentFrame)
        {
            if (CurrentVehicleID == this.VehicleID && CurrentStopID == this.StopID)
            {
                int IndexOfEarliestArrivalTime = 0;
                for (int i = 1; i != this.ArrivalTimes.Length; i++)
                {
                    if (this.ArrivalTimes[i] < this.ArrivalTimes[IndexOfEarliestArrivalTime])
                    {
                        IndexOfEarliestArrivalTime = i;
                    }
                }
                this.ArrivalTimes[IndexOfEarliestArrivalTime] = CurrentFrame;
            }
        }

        public void ResetPeriodMeasurement(ushort VehicleID, ushort StopID)
        {
            this.VehicleID = VehicleID;
            this.StopID = StopID;

            this.ArrivalTimes = new uint[NumberOfArrivalTimesToKeep];

#if DEBUG
            Helper.PrintError("LineID: " + Singleton<VehicleManager>.instance.m_vehicles.m_buffer[VehicleID].m_transportLine + "; ResetPeriodMeasurement");
#endif
        }

        public void RefreshPeriod(ushort LineID, uint CurrentFrame)
        {
            if(CurrentFrame - this.LastFramePredictedPeriodCalculated > CalculatePredictedPeriodEveryThisFrames)
            {
                this.RefreshPredictedPeriod(LineID, CurrentFrame);
            }

            int NewEmpiricalPeriod = this.EmpiricalPeriod();

            if(NewEmpiricalPeriod == 0)
            {
                if(this.Period == 0)
                {
                    this.Period = this.PredictedPeriod;
                }
            }
            else
            {
                this.Period = NewEmpiricalPeriod;
            }
        }

        public void RefreshPredictedPeriod(ushort LineID, uint CurrentFrame)
        {
            this.PredictedPeriod = PeriodPredictor.PredictPeriod(LineID);
            this.LastFramePredictedPeriodCalculated = CurrentFrame;
        }

        public int EmpiricalPeriod()
        {
            //Calculate period
            Array.Sort(this.ArrivalTimes);//Ascending Order

            long SumOfDifferencesFromPreviousIndex = 0;
            int NumberOfValidDifferencesFromPreviousIndex = 0;
            for (int i = 1; i != this.ArrivalTimes.Length; i++)
            {
                if (this.ArrivalTimes[i] != 0 && this.ArrivalTimes[i - 1] != 0)
                {
                    uint DifferenceFromPreviousIndex = this.ArrivalTimes[i] - this.ArrivalTimes[i - 1];
                    SumOfDifferencesFromPreviousIndex += DifferenceFromPreviousIndex;
                    NumberOfValidDifferencesFromPreviousIndex++;
                }
            }

            if (NumberOfValidDifferencesFromPreviousIndex != 0)
            {
                return (int)(SumOfDifferencesFromPreviousIndex / NumberOfValidDifferencesFromPreviousIndex);
            }
            else
            {
                return 0;
            }
        }

        public void WritePeriodCalculator(FastList<byte> Data)
        {
            StorageData.WriteUInt16(this.VehicleID, Data);
            StorageData.WriteUInt16(this.StopID, Data);

            StorageData.WriteUInt32ArrayWithoutLength(this.ArrivalTimes, Data);

            //Size: 4 + NumberOfArrivalTimesToKeep * 4 = 20
        }

        public PeriodCalculator(ushort Version, byte[] Data, ref int Index, Settings LoadedSettings)
        {
            this.VehicleID = StorageData.ReadUInt16(Data, ref Index);
            this.StopID = StorageData.ReadUInt16(Data, ref Index);

            this.ArrivalTimes = StorageData.ReadUInt32ArrayWithoutLength(Data, ref Index, NumberOfArrivalTimesToKeep);
        }
    }
}
