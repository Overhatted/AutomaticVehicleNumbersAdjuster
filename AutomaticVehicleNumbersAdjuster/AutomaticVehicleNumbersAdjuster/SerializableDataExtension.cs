using ICities;
using System;
using System.Collections.Generic;

namespace AutomaticVehicleNumbersAdjuster
{
    public class SerializableDataExtension : ISerializableDataExtension
    {
        public const string DataID = "AutomaticVehicleNumbersAdjuster";
        public const ushort Version = 14;

        public static ISerializableData SerializableData;

        public void OnCreated(ISerializableData SerializedData)
        {
            SerializableDataExtension.SerializableData = SerializedData;
        }

        public void OnLoadData()
        {
            VehicleNumbersManager.Init();

#if DEBUG
            //SerializableDataExtension.SerializableData.EraseData(SerializableDataExtension.DataID);
            Helper.PrintError("Loading");
#endif
            try
            {
                byte[] Data = SerializableDataExtension.SerializableData.LoadData(SerializableDataExtension.DataID);

                ushort SaveFileVersion;
                int Index = 0;

                SaveFileVersion = StorageData.ReadUInt16(Data, ref Index);

#if DEBUG
                Helper.PrintError("Data length: " + Data.Length.ToString() + "; Data Version: " + SaveFileVersion);
#endif

                VehicleNumbersManager.ReadVehicleNumbersManager(SaveFileVersion, Data, ref Index);
#if DEBUG
                Helper.PrintError("Loaded successfully");
#endif
            }
            catch (Exception ex)
            {
                Helper.PrintError("Could not load transport line data. " + ex.Message);
            }
        }

        public void OnSaveData()
        {
#if DEBUG
            Helper.PrintError("Saving");
#endif
            try
            {
                FastList<byte> Data = new FastList<byte>();
                StorageData.WriteUInt16(Version, Data);
                VehicleNumbersManager.WriteVehicleNumbersManager(Data);
                SerializableDataExtension.SerializableData.SaveData(SerializableDataExtension.DataID, Data.ToArray());
#if DEBUG
                Helper.PrintError("Saved successfully - Data Size: " + Data.m_size);
                ushort NumberOfTransportLines = 0;
                ushort NumberOfTransportLinesWithExtraVehicles = 0;
                ushort NumberOfSmallUsageStops = 0;
                ushort NumberOfHighUsageStops = 0;
                ushort NumberOfValidEmpiricalPeriods = 0;
                foreach (KeyValuePair<ushort, TransportLineUsage> TransportLineUsageKV in VehicleNumbersManager.TransportLines)
                {
                    foreach (KeyValuePair<ushort, Stop> StopKV in TransportLineUsageKV.Value.Stops)
                    {
                        if (StopKV.Value.Usage == Stop.UsageFlag.Small)
                        {
                            NumberOfSmallUsageStops++;
                        }
                        else
                        {
                            NumberOfHighUsageStops++;
                        }
                    }
                    if (TransportLineUsageKV.Value.PeriodCalculator.EmpiricalPeriod() != 0)
                    {
                        NumberOfValidEmpiricalPeriods++;
                    }
                    if(TransportLineUsageKV.Value.ExtraVehicles != 0)
                    {
                        NumberOfTransportLinesWithExtraVehicles++;
                    }
                    NumberOfTransportLines++;
                }
                Helper.PrintError("NumberOfTransportLines: " + NumberOfTransportLines +
                    "; NumberOfTransportLinesWithExtraVehicles: " + NumberOfTransportLinesWithExtraVehicles +
                    "; NumberOfSmallUsageStops: " + NumberOfSmallUsageStops +
                    "; NumberOfHighUsageStops: " + NumberOfHighUsageStops +
                    "; NumberOfValidEmpiricalPeriods: " + NumberOfValidEmpiricalPeriods);
#endif
            }
            catch (Exception ex)
            {
                Helper.PrintError("Error while saving transport line data! " + ex.Message + " " + ex.InnerException);
            }
        }

        public void OnReleased()
        {
            SerializableData = null;
        }
    }
}
