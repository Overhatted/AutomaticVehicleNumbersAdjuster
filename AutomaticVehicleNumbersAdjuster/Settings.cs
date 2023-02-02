namespace AutomaticVehicleNumbersAdjuster
{
    public class Settings
    {
        public byte NumberOfDaysToStore = 8;
        public byte NumberOfHourIntervalsToStore = 4;
        public bool EnableExtraVehicles = false;

        public Settings()
        {

        }

        public void WriteSettings(FastList<byte> Data)
        {
            StorageData.WriteByte(this.NumberOfDaysToStore, Data);
            StorageData.WriteByte(this.NumberOfHourIntervalsToStore, Data);

            StorageData.WriteBool(this.EnableExtraVehicles, Data);

            //Size: 3
        }

        public Settings(ushort Version, byte[] Data, ref int Index)
        {
            this.NumberOfDaysToStore = StorageData.ReadByte(Data, ref Index);
            this.NumberOfHourIntervalsToStore = StorageData.ReadByte(Data, ref Index);

            this.EnableExtraVehicles = StorageData.ReadBool(Data, ref Index);
        }
    }
}
