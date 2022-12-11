namespace AutomaticVehicleNumbersAdjuster
{
    public static class MathFunctions
    {
        public static int AddToIndex(int CurrentIndex, int ValueToAdd, int NumberOfPossibleIndexes)
        {
            //CurrentIndex needs to be: -1 < CurrentIndex < NumberOfPossibleIndexes
            //ValueToAdd needs to be: -(NumberOfPossibleIndexes - 1) < ValueToAdd < NumberOfPossibleIndexes - 1
            int CalculatedValue = CurrentIndex + ValueToAdd;

            if(CalculatedValue >= NumberOfPossibleIndexes)
            {
                CalculatedValue = CalculatedValue - NumberOfPossibleIndexes;
            }
            else if(CalculatedValue < 0)
            {
                CalculatedValue = CalculatedValue + NumberOfPossibleIndexes;
            }

            return CalculatedValue;
        }

        public static double Clamp(double Value, double Min, double Max)
        {
            if(Value > Max)
            {
                return Max;
            }
            else if(Value < Min)
            {
                return Min;
            }
            else
            {
                return Value;
            }
        }
    }
}
