using ColossalFramework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutomaticVehicleNumbersAdjuster
{
    public class TransportLineUsage
    {
        public const uint ShortRefreshExpirationTime = 20000;
        public readonly ushort ID;

        public byte ExtraVehicles = 0;//Save

        public ushort LastRefreshDay;
        public uint LastShortRefreshFrame;

        public bool IsAtLeastOneDayOld;//Save
        public uint CreationFrame;//Save if IsAtLeastOneDayOld is false

        public PeriodCalculator PeriodCalculator;//Save

        public float LineLength;
        public double VehicleCapacity;
        //public int CurrentNumberOfVehicles = 0;

        public ushort MaximumNumberOfPassengersPerInterval;
        //public ushort[] NumberOfPassengers = new ushort[VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore];
        /*public int NumberOfDailyPassengers
        {
            get
            {
                int NumberOfDailyPassengers = 0;
                for (byte i = 0; i != VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore; i++)
                {
                    NumberOfDailyPassengers += this.NumberOfPassengers[i];
                }
                return NumberOfDailyPassengers;
            }
        }*/

        public Dictionary<ushort, Stop> Stops = new Dictionary<ushort, Stop>();//Save

        public TransportLineUsage(ushort LineID, uint CurrentFrame)
        {
            this.ID = LineID;

            this.IsAtLeastOneDayOld = false;
            this.CreationFrame = CurrentFrame;

            this.PeriodCalculator = new PeriodCalculator();
        }

        public double MinimumNumberOfVehicles()
        {
            float PredictedPeriod = this.PeriodCalculator.PredictedPeriod;
            return Math.Ceiling(PredictedPeriod / VehicleNumbersManager.MaximumWaitingFrames);
        }

        public double MaximumNumberOfVehicles(TransportInfo.TransportType TransportType)
        {
            /*
            PassengersPerFrame = VehicleCapacity / NumberOfFramesPerStop
            PassengersPerFrame = PassengersPerPeriod / FramesPerPeriod
            PassengersPerPeriod = NumberOfVehicles * VehicleCapacity
            */
            int NumberOfFramesPerStop;
            switch (TransportType)
            {
                case TransportInfo.TransportType.Bus:
                    NumberOfFramesPerStop = 376;
                    break;
                case TransportInfo.TransportType.CableCar:
                    NumberOfFramesPerStop = 2134;//
                    break;
                case TransportInfo.TransportType.Ship:
                    NumberOfFramesPerStop = 840;
                    break;
                case TransportInfo.TransportType.Airplane:
                    NumberOfFramesPerStop = 2134;
                    break;
                case TransportInfo.TransportType.Train:
                    NumberOfFramesPerStop = 810;
                    break;
                case TransportInfo.TransportType.Metro:
                    NumberOfFramesPerStop = 690;
                    break;
                case TransportInfo.TransportType.Monorail:
                    NumberOfFramesPerStop = 592;
                    break;
                case TransportInfo.TransportType.Tram:
                    NumberOfFramesPerStop = 430;
                    break;
                default://This is never supposed to happen
#if DEBUG
                    Helper.PrintError("TransportLineID: " + this.ID + "; TransportType: " + TransportType);
#endif
                    NumberOfFramesPerStop = 2134;
                    break;
            }

            double PredictedPeriod = this.PeriodCalculator.PredictedPeriod;
            double MaximumNumberOfVehicles = Math.Floor(PredictedPeriod / NumberOfFramesPerStop);

            return Math.Max(MaximumNumberOfVehicles, 1);
        }

        public int GetRecommendedNumberOfVehicles()
        {
            //Refresh Period and other frequent things
            TransportLine TransportLineInstance = Singleton<TransportManager>.instance.m_lines.m_buffer[this.ID];

            if(!TransportLineInstance.Complete)
            {
                return 1;
            }

            TransportInfo TransportLineInfo = TransportLineInstance.Info;
            uint CurrentFrame = Singleton<SimulationManager>.instance.m_currentFrameIndex;

            ushort CurrentDay = Stop.CurrentDay(CurrentFrame);

            if (CurrentDay != this.LastRefreshDay || CurrentFrame - this.LastShortRefreshFrame > TransportLineUsage.ShortRefreshExpirationTime)
            {
                this.RefreshTransportLineNumberOfPassengers();
            }

            //This goes before PeriodCalculator.RefreshPeriod
            //to prevent executing PeriodPredictor.PredictPeriod(lineID) twice
            if (TransportLineInstance.m_totalLength < 1)//During the first moments after a line has been created it has a m_totalLength == 0 (i think, not tested)
            {
                if (Math.Abs(TransportLineInstance.m_totalLength - this.LineLength) > 10f)
                {
                    this.PeriodCalculator.RefreshPredictedPeriod(this.ID, CurrentFrame);
                }
            }

            //This needs to be sure Period is always greater than zero
            this.PeriodCalculator.RefreshPeriod(this.ID, CurrentFrame);

            if (!this.IsAtLeastOneDayOld)
            {
                if (CurrentFrame - this.CreationFrame > SimulationManager.DAYTIME_FRAMES)
                {
                    this.IsAtLeastOneDayOld = true;
                }
            }

            int Budget = Singleton<EconomyManager>.instance.GetBudget(TransportLineInstance.Info.m_class);

            /*
            PassengersPerInterval = PassengersPerFrame * FramesPerInterval
            PassengersPerFrame = PassengersPerPeriod / FramesPerPeriod
            PassengersPerPeriod = NumberOfVehicles * VehicleCapacity
            FramesPerInterval = NumberOfFramesPerDay / NumberOfIntervalsPerDay
            */

            if (this.IsAtLeastOneDayOld)
            {
                Budget = (Budget * TransportLineInstance.m_budget + 50) / 100;

                double BudgetMultiplier = 1.2;//Inverse of Vehicle Capacity Allowed To Be Filled
                if (Budget > 100)
                {
                    BudgetMultiplier += 0.016 * (Budget - 100);//Up to a maximum of 2 (No longer)
                }

                double RecommendedNumberOfVehiclesFromPassengers = BudgetMultiplier * this.MaximumNumberOfPassengersPerInterval * (this.PeriodCalculator.Period * VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore) / (this.VehicleCapacity * SimulationManager.DAYTIME_FRAMES);

                double RecommendedNumberOfVehicles = Math.Ceiling(RecommendedNumberOfVehiclesFromPassengers) + this.ExtraVehicles;

                if(Double.IsNaN(RecommendedNumberOfVehicles))
                {
                    return TransportLineMod.CalculateTargetVehicleCount(this.ID);
                }
                else
                {
                    return (int)MathFunctions.Clamp(RecommendedNumberOfVehicles, this.MinimumNumberOfVehicles(), this.MaximumNumberOfVehicles(TransportLineInfo.m_transportType));
                }
            }
            else
            {
                return TransportLineMod.CalculateTargetVehicleCount(this.ID);
            }
        }

        //For things that are somewhat expensive to compute. Cheaper things go into TransportLine.CalculateTargetVehicleCount
        public void RefreshTransportLineNumberOfPassengers()
        {
            //Calculate Vehicle Capacity
            TransportLine TransportLineInstance = Singleton<TransportManager>.instance.m_lines.m_buffer[this.ID];
            TransportInfo TransportLineInfo = TransportLineInstance.Info;

            bool IsPeriodVehiclePresent = false;
            int TotalVehicleCapacity = 0;
            int NumberOfActiveVehicles = 0;
            if (TransportLineInstance.m_vehicles != 0)
            {
                VehicleManager VehicleManagerInstance = Singleton<VehicleManager>.instance;
                ushort CurrentVehicleID = TransportLineInstance.m_vehicles;
                int ListLimiter = 0;
                while (CurrentVehicleID != 0)
                {
                    if (CurrentVehicleID == this.PeriodCalculator.VehicleID)
                    {
                        IsPeriodVehiclePresent = true;
                    }

                    Vehicle CurrentVehicle = VehicleManagerInstance.m_vehicles.m_buffer[CurrentVehicleID];

                    if ((CurrentVehicle.m_flags & Vehicle.Flags.GoingBack) == (Vehicle.Flags)0)
                    {
                        NumberOfActiveVehicles++;

                        CurrentVehicle.Info.m_vehicleAI.GetBufferStatus(CurrentVehicleID, ref CurrentVehicle, out string LocaleKey, out int CurrentNumberOfPassengers, out int CurrentVehicleCapacity);

                        TotalVehicleCapacity += CurrentVehicleCapacity;
                    }

                    CurrentVehicleID = CurrentVehicle.m_nextLineVehicle;
                    if (++ListLimiter > 16384)
                    {
                        CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                        break;
                    }
                }
            }

            if(NumberOfActiveVehicles == 0)
            {
                switch (TransportLineInfo.m_transportType)
                {
                    case TransportInfo.TransportType.Bus:
                        this.VehicleCapacity = 30;
                        break;
                    case TransportInfo.TransportType.CableCar:
                        this.VehicleCapacity = 30;//
                        break;
                    case TransportInfo.TransportType.Ship:
                        this.VehicleCapacity = 50;
                        break;
                    case TransportInfo.TransportType.Airplane:
                        this.VehicleCapacity = 35;
                        break;
                    case TransportInfo.TransportType.Train:
                        this.VehicleCapacity = 240;
                        break;
                    case TransportInfo.TransportType.Metro:
                        this.VehicleCapacity = 180;
                        break;
                    case TransportInfo.TransportType.Monorail:
                        this.VehicleCapacity = 180;
                        break;
                    case TransportInfo.TransportType.Tram:
                        this.VehicleCapacity = 90;
                        break;
                    default://This is never supposed to happen
#if DEBUG
                        Helper.PrintError("TransportLineID: " + this.ID + "; TransportType: " + TransportLineInfo.m_transportType);
#endif
                        this.VehicleCapacity = 30;
                        break;
                }
            }
            else
            {
                this.VehicleCapacity = ((double)TotalVehicleCapacity) / NumberOfActiveVehicles;
            }

            //Clean old stops
            bool IsPeriodStopPresent = false;
            List<ushort> CurrentStopIDs = new List<ushort>();

            if (TransportLineInstance.m_stops != 0)
            {
                ushort FirstStopID = TransportLineInstance.m_stops;
                ushort CurrentStopID = FirstStopID;
                int ListLimiter = 0;
                while (CurrentStopID != 0)
                {
                    if (CurrentStopID == this.PeriodCalculator.StopID)
                    {
                        IsPeriodStopPresent = true;
                    }

                    CurrentStopIDs.Add(CurrentStopID);

                    CurrentStopID = TransportLine.GetNextStop(CurrentStopID);
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

            List<ushort> StopIDsToRemove = new List<ushort>();
            foreach (ushort StopID in this.Stops.Keys)
            {
                if (!CurrentStopIDs.Contains(StopID))
                {
                    StopIDsToRemove.Add(StopID);
                }
            }

            foreach (ushort StopIDToRemove in StopIDsToRemove)
            {
                this.Stops.Remove(StopIDToRemove);
            }

            if (this.Stops.Count != 0 && NumberOfActiveVehicles != 0)
            {
                if (!IsPeriodVehiclePresent || !IsPeriodStopPresent)
                {
                    this.PeriodCalculator.ResetPeriodMeasurement(TransportLineInstance.m_vehicles, TransportLineInstance.m_stops);
                }
            }

            //Calculate maximum daily passengers
            int MaximumDailyPassengers = 0;
            ushort StopIDWithMaximumDailyPassengers = 0;

            foreach (KeyValuePair<ushort, Stop> StopKV in Stops)
            {
                int CurrentStopDailyPassengers = StopKV.Value.GetAverageDailyPassengers();

                if (CurrentStopDailyPassengers > MaximumDailyPassengers)
                {
                    MaximumDailyPassengers = CurrentStopDailyPassengers;
                    StopIDWithMaximumDailyPassengers = StopKV.Key;
                }
            }

            //Calculate number of passengers from the Stops with High Usage
            ushort MaximumNumberOfPassengersPerInterval = (ushort)(MaximumDailyPassengers / VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore);
            ushort HighUsageStopWithTheMostNumberOfPassengersPerInterval = 0;

            for (byte i = 0; i != VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore; i++)
            {
                foreach (KeyValuePair<ushort, Stop> StopKV in Stops)
                {
                    if (StopKV.Value.Usage == Stop.UsageFlag.High)
                    {
                        ushort NumberOfPassengersInThisStopInThisInterval = StopKV.Value.GetAverageIntervalPassengers(i);
                        if (NumberOfPassengersInThisStopInThisInterval > MaximumNumberOfPassengersPerInterval)
                        {
                            MaximumNumberOfPassengersPerInterval = NumberOfPassengersInThisStopInThisInterval;
                            HighUsageStopWithTheMostNumberOfPassengersPerInterval = StopKV.Key;
                        }
                    }
                }
            }

            //Calculate number of passengers from the stop with the most daily passengers
            ushort AverageNumberOfPassengersPerIntervalFromMostUsedStop = (ushort)(MaximumDailyPassengers / VehicleNumbersManager.CurrentSettings.NumberOfHourIntervalsToStore);

            if(AverageNumberOfPassengersPerIntervalFromMostUsedStop > MaximumNumberOfPassengersPerInterval)
            {
                MaximumNumberOfPassengersPerInterval = AverageNumberOfPassengersPerIntervalFromMostUsedStop;
            }

            this.MaximumNumberOfPassengersPerInterval = MaximumNumberOfPassengersPerInterval;

            //Add usage score to stops (Should only be done once per day)
            //WasRelevant is true if:
            //- Stop is the one with the most DailyPassengers
            //- Stop is the one with the MaximumNumberOfPassengersPerInterval

            uint CurrentFrame = Singleton<SimulationManager>.instance.m_currentFrameIndex;

            this.LastShortRefreshFrame = CurrentFrame;

            ushort CurrentDay = Stop.CurrentDay(CurrentFrame);

            if (CurrentDay != this.LastRefreshDay)
            {
                foreach (KeyValuePair<ushort, Stop> StopKV in this.Stops)
                {
                    if (StopKV.Key == StopIDWithMaximumDailyPassengers || StopKV.Key == HighUsageStopWithTheMostNumberOfPassengersPerInterval)
                    {
                        StopKV.Value.AddUsageScore(true, CurrentFrame);
                    }
                    else
                    {
                        StopKV.Value.AddUsageScore(false, CurrentFrame);
                    }
                }

                if (VehicleNumbersManager.CurrentSettings.EnableExtraVehicles && this.ExtraVehicles != byte.MinValue)
                {
                    this.ExtraVehicles--;
                }

                this.LastRefreshDay = CurrentDay;
            }
        }

        public static void AfterLoadPassengers(ushort VehicleID, ref Vehicle VehicleData, ushort CurrentStopID)
        {
            ushort LineID = VehicleData.m_transportLine;
            if (!VehicleNumbersManager.TransportLines.TryGetValue(LineID, out TransportLineUsage TransportLineUsageObj))
            {
                uint CurrentFrame = Singleton<SimulationManager>.instance.m_currentFrameIndex;

                TransportLineUsageObj = new TransportLineUsage(LineID, CurrentFrame);
                VehicleNumbersManager.TransportLines.Add(LineID, TransportLineUsageObj);
            }

            TransportLineUsageObj.AfterLoadPassengersOnThisLine(VehicleID, ref VehicleData, CurrentStopID);
        }

        public void AfterLoadPassengersOnThisLine(ushort VehicleID, ref Vehicle VehicleData, ushort CurrentStopID)
        {
            if (!this.Stops.TryGetValue(CurrentStopID, out Stop CurrentStop))
            {
                CurrentStop = new Stop();
                this.Stops.Add(CurrentStopID, CurrentStop);
            }

            uint CurrentFrame = Singleton<SimulationManager>.instance.m_currentFrameIndex;

            this.PeriodCalculator.AddArrivalTime(VehicleID, CurrentStopID, CurrentFrame);

            VehicleData.Info.m_vehicleAI.GetBufferStatus(VehicleID, ref VehicleData, out string LocaleKey, out int CurrentNumberOfPassengers, out int CurrentVehicleCapacity);

            if (VehicleNumbersManager.CurrentSettings.EnableExtraVehicles)
            {
                if (CurrentNumberOfPassengers == CurrentVehicleCapacity)
                {
                    CurrentStop.NumberOfConsecutiveFilledVehicles++;

                    float CurrentNumberOfVehicles = this.GetRecommendedNumberOfVehicles();

                    if (CurrentNumberOfVehicles != 0)
                    {
                        if (CurrentStop.NumberOfConsecutiveFilledVehicles * this.PeriodCalculator.Period / CurrentNumberOfVehicles > VehicleNumbersManager.MaximumWaitingFrames ||
                        CurrentStop.NumberOfConsecutiveFilledVehicles == byte.MaxValue)
                        {
                            if (this.ExtraVehicles != byte.MaxValue)
                            {
#if DEBUG
                                    Helper.PrintError("LineID: " + this.ID + "; CurrentFrame: " + CurrentFrame + "; this.ExtraVehicles++");
#endif
                                this.ExtraVehicles++;
                            }

                            foreach (KeyValuePair<ushort, Stop> StopKV in this.Stops)
                            {
                                StopKV.Value.NumberOfConsecutiveFilledVehicles = 0;
                            }
                        }
                    }
                }
                else
                {
                    CurrentStop.NumberOfConsecutiveFilledVehicles = 0;
                }
            }

            CurrentStop.AddPassengers(CurrentNumberOfPassengers, CurrentFrame);
        }

        public void WriteTransportLineUsage(FastList<byte> Data)
        {
            if (VehicleNumbersManager.CurrentSettings.EnableExtraVehicles)
            {
                StorageData.WriteByte(this.ExtraVehicles, Data);
            }

            StorageData.WriteUInt16(this.LastRefreshDay, Data);

            StorageData.WriteBool(this.IsAtLeastOneDayOld, Data);
            if (!this.IsAtLeastOneDayOld)
            {
                StorageData.WriteUInt32(this.CreationFrame, Data);
            }

            this.PeriodCalculator.WritePeriodCalculator(Data);

            //Stops
            StorageData.WriteUInt16((ushort) this.Stops.Count, Data);

            foreach (KeyValuePair<ushort, Stop> StopObj in this.Stops)
            {
                StorageData.WriteUInt16(StopObj.Key, Data);

                StopObj.Value.WriteStop(Data);
            }

            //Size: 5 (9 if !this.IsAtLeastOneDayOld)
        }

        public TransportLineUsage(ushort LineID, ushort Version, byte[] Data, ref int Index, Settings LoadedSettings)
        {
            this.ID = LineID;

            if (LoadedSettings.EnableExtraVehicles)
            {
                this.ExtraVehicles = StorageData.ReadByte(Data, ref Index);
            }
            else
            {
                this.ExtraVehicles = 0;
            }

            this.LastRefreshDay = StorageData.ReadUInt16(Data, ref Index);

            this.IsAtLeastOneDayOld = StorageData.ReadBool(Data, ref Index);
            if (!this.IsAtLeastOneDayOld)
            {
                this.CreationFrame = StorageData.ReadUInt32(Data, ref Index);
            }

            this.PeriodCalculator = new PeriodCalculator(Version, Data, ref Index, LoadedSettings);

            //Stops
            ushort SizeOfDictionary = StorageData.ReadUInt16(Data, ref Index);
            for (ushort i = 0; i != SizeOfDictionary; i++)
            {
                ushort DictKey = StorageData.ReadUInt16(Data, ref Index);

                Stop DictValue = new Stop(Version, Data, ref Index, LoadedSettings);

                this.Stops.Add(DictKey, DictValue);
            }
        }
    }
}
 