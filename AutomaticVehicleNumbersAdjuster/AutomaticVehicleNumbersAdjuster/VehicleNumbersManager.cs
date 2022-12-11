using ColossalFramework;
using System.Collections.Generic;

namespace AutomaticVehicleNumbersAdjuster
{
    public static class VehicleNumbersManager
    {
        public static Settings CurrentSettings;

        //public const uint WeekTimeFrames = 4096;
        //public const int MinimumFrameDifferenceBetweenVehicleNumberChanges = 15000;
        public const int MaximumWaitingFrames = 15000;

        public static Dictionary<ushort, TransportLineUsage> TransportLines;//Save

        public static void Init()
        {
            CurrentSettings = new Settings();

            TransportLines = new Dictionary<ushort, TransportLineUsage>();
        }

        public static void UnInit()
        {
            TransportLines.Clear();
        }

        public static void OnLoad()
        {
            TransportManager TransportManagerInstance = Singleton<TransportManager>.instance;

            foreach (KeyValuePair<ushort, TransportLineUsage> TransportLineUsageKV in VehicleNumbersManager.TransportLines)
            {
                if (TransportManagerInstance.m_lines.m_buffer[TransportLineUsageKV.Key].Complete)
                {
                    TransportLineUsageKV.Value.RefreshTransportLineNumberOfPassengers();
                }
            }
        }

        public static void WriteVehicleNumbersManager(FastList<byte> Data)
        {
            CurrentSettings.WriteSettings(Data);

            //TransportLines
            TransportManager TransportManagerInstance = Singleton<TransportManager>.instance;

            byte NumberOfTransportLines = 0;

            foreach (KeyValuePair<ushort, TransportLineUsage> TransportLineUsageKV in TransportLines)
            {
                if (TransportManagerInstance.m_lines.m_buffer[TransportLineUsageKV.Key].Complete)
                {
                    NumberOfTransportLines++;
                }
            }

            StorageData.WriteByte(NumberOfTransportLines, Data);

            foreach (KeyValuePair<ushort, TransportLineUsage> TransportLineUsageKV in TransportLines)
            {
                if (TransportManagerInstance.m_lines.m_buffer[TransportLineUsageKV.Key].Complete)
                {
                    StorageData.WriteUInt16(TransportLineUsageKV.Key, Data);

                    TransportLineUsageKV.Value.WriteTransportLineUsage(Data);
                }
            }

            //Size: 1
        }

        public static void ReadVehicleNumbersManager(ushort Version, byte[] Data, ref int Index)
        {
            Settings LoadedSettings = new Settings(Version, Data, ref Index);

            //TransportLines
            byte SizeOfDictionary = StorageData.ReadByte(Data, ref Index);
            for (byte i = 0; i != SizeOfDictionary; i++)
            {
                ushort DictKey = StorageData.ReadUInt16(Data, ref Index);

                TransportLineUsage DictValue = new TransportLineUsage(DictKey, Version, Data, ref Index, LoadedSettings);

                TransportLines.Add(DictKey, DictValue);
            }
        }
    }
}
