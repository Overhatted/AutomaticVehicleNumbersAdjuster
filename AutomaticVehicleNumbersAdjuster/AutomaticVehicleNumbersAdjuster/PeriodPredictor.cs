using ColossalFramework;
using System;

namespace AutomaticVehicleNumbersAdjuster
{
    public static class PeriodPredictor
    {
        public const int NumberOfFramesPerWaitCounter = 16;
        public const int MaximumWaitCounter = 12;

        public static int PredictPeriod(ushort LineID)
        {
            //https://www.khanacademy.org/science/physics/one-dimensional-motion/kinematic-formulas/a/what-are-the-kinematic-formulas

            TransportLine TransportLineInstance = Singleton<TransportManager>.instance.m_lines.m_buffer[LineID];

            VehicleInfo VehicleInfoInstance = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[TransportLineInstance.m_vehicles].Info;

            //Don't know the original units but dividing by 16 seems to give Meters per Second^2
            //Meters per Second^2 = 
            float Accelaration = VehicleInfoInstance.m_acceleration / 256;
            float Braking = VehicleInfoInstance.m_braking / 256;
            //Meters per SimulationStep
            //Meters per SimulationStep = Meters per (16 * Frame) = (1 / 16) Meters per Frame
            float MaxVehicleSpeed = VehicleInfoInstance.m_maxSpeed / 16;

            float Period = 0;

            NetManager NetManagerInstance = Singleton<NetManager>.instance;
            ushort FirstStopID = TransportLineInstance.m_stops;
            ushort CurrentStopID = FirstStopID;
            int ListLimiter = 0;
            while (CurrentStopID != 0)
            {
                float LengthBetweenStops = 0;
                float SpeedLimit = 0;
                ushort NextStopID = 0;
                for (int i = 0; i < 8; i++)
                {
                    ushort SegmentID = NetManagerInstance.m_nodes.m_buffer[CurrentStopID].GetSegment(i);
                    if (SegmentID != 0 && NetManagerInstance.m_segments.m_buffer[SegmentID].m_startNode == CurrentStopID)
                    {
                        NetSegment Segment = NetManagerInstance.m_segments.m_buffer[SegmentID];

                        PathManager PathManagerInstance = Singleton<PathManager>.instance;

                        float SumOfSpeedLimitsTimesLength = 0;
                        uint CurrentPathID = Segment.m_path;
                        int SecondListLimiter = 0;
                        while (CurrentPathID != 0u)
                        {
                            int j = 0;
                            while (PathManagerInstance.m_pathUnits.m_buffer[CurrentPathID].GetPosition(j, out PathUnit.Position Position))
                            {
                                NetSegment PartSegment = Singleton<NetManager>.instance.m_segments.m_buffer[Position.m_segment];
                                float Length = PartSegment.m_averageLength;
                                //Meters
                                LengthBetweenStops += Length;
                                //Squares per SimulationStep
                                //Squares per SimulationStep = (8 * Meters) per (16 * Frame) = 0.5 * Meters per Frame
                                SumOfSpeedLimitsTimesLength += PartSegment.Info.m_lanes[Position.m_lane].m_speedLimit * Length;

                                j++;
                            }

                            CurrentPathID = PathManagerInstance.m_pathUnits.m_buffer[CurrentPathID].m_nextPathUnit;

                            if (++SecondListLimiter >= 262144)
                            {
                                CODebugBase<LogChannel>.Error(LogChannel.Core, "Invalid list detected!\n" + Environment.StackTrace);
                                break;
                            }
                        }

                        if(LengthBetweenStops == 0)
                        {
                            SpeedLimit = 1;//Random value
                        }
                        else
                        {
                            SpeedLimit = SumOfSpeedLimitsTimesLength / LengthBetweenStops * 0.5f;
                        }

                        NextStopID = Segment.m_endNode;

                        break;
                    }
                }

                float MaxSpeed = Math.Min(SpeedLimit, MaxVehicleSpeed);

                //v^2 = v0^2 + 2 * a * delta_x
                float Delta_X_To_Accelerate_To_Top_Speed = (MaxSpeed * MaxSpeed) / (2 * Accelaration);
                float Delta_X_To_Brake_From_Top_Speed = (MaxSpeed * MaxSpeed) / (2 * Braking);
                float Delta_X_To_Reach_Max_Speed = Delta_X_To_Accelerate_To_Top_Speed + Delta_X_To_Brake_From_Top_Speed;

                if (LengthBetweenStops > Delta_X_To_Reach_Max_Speed)
                {
                    //v = v0 + a * t
                    float Time_To_Reach_Max_Speed = MaxSpeed / Accelaration;
                    float Time_To_Stop_From_Max_Speed = MaxSpeed / Braking;

                    Period += Time_To_Reach_Max_Speed + Time_To_Stop_From_Max_Speed;

                    //delta_x = v0 * t + 0.5 * a * t^2
                    Period += (LengthBetweenStops - Delta_X_To_Reach_Max_Speed) / MaxSpeed;
                }
                else
                {
                    //LengthBetweenStops = Delta_X_To_Accelerate + Delta_X_To_Brake
                    //Vmax^2 = 2 * acceleration * Delta_X_To_Accelerate
                    //Vmax^2 = 2 * braking * Delta_X_To_Brake
                    float Delta_X_To_Accelerate = LengthBetweenStops / (1 + Accelaration / Braking);
                    float Delta_X_To_Brake = LengthBetweenStops - Delta_X_To_Accelerate;

                    //delta_x = v0 * t + 0.5 * a * t^2
                    float Time_To_Accelerate = (float) Math.Sqrt(2 * Delta_X_To_Accelerate / Accelaration);
                    float Time_To_Brake = (float) Math.Sqrt(2 * Delta_X_To_Brake / Braking);

                    Period += Time_To_Accelerate + Time_To_Brake;
                }

                //Waiting time at station
                //NumberOfFramesPerWaitCounter is the number of frames per m_waitCounter
                //MaximumWaitCounter is the maximum m_waitCounter
                Period += NumberOfFramesPerWaitCounter * MaximumWaitCounter;

                CurrentStopID = NextStopID;
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

            return (int)Period;
        }
    }
}
