namespace TreeNetSim
{
    public class SimulationParams
    {

        public int StationsPerBranch { get; set; }
        public double ChannelSpeedMbps { get; set; }
        public int PacketSizeBytes { get; set; }

        // Параметры всех коммутаторов (одинаковые для всех трёх)
        public int SwitchQueueIn { get; set; }      // размер входной очереди
        public int SwitchQueueOut { get; set; }     // размер выходной очереди
        public double SwitchProcessMs { get; set; } // время обработки пакета

        // Параметры станций
        public int PacketsPerStation { get; set; }
        public double PacketIntervalMs { get; set; }

        /// <summary>
        /// Тип распределения для генерации интервалов
        /// </summary>
        public DistributionType DistributionType { get; set; }

        /// <summary>
        /// Параметр α для распределения Парето (индекс)
        /// Типичные значения: 1.16 (web), 1.5 (P2P), 2.0 (умеренный)
        /// </summary>
        public double ParetoAlpha { get; set; }

        /// <summary>
        /// Стандартное отклонение для нормального распределения
        /// (в процентах от среднего интервала)
        /// </summary>
        public double NormalStdDevPercent { get; set; }

        public TrafficMode TrafficMode { get; set; }

        // Матрица трафика [от][к] = количество пакетов
        public int[,] TrafficMatrix { get; set; }

        public SimulationParams()
        {
            StationsPerBranch = 3;
            ChannelSpeedMbps  = 100;
            PacketSizeBytes   = 512;
            SwitchQueueIn     = 10;
            SwitchQueueOut    = 10;
            SwitchProcessMs   = 1.0;
            PacketsPerStation = 20;
            PacketIntervalMs  = 5.0;
            DistributionType = DistributionType.Deterministic; 
            ParetoAlpha = 1.5;          
            NormalStdDevPercent = 20.0; 

            TrafficMode = TrafficMode.Uniform;
            TrafficMatrix = null;
        }

        public double CalculatePacketTransferMs()
        {
            return (PacketSizeBytes * 8.0) / (ChannelSpeedMbps * 1_000_000.0) * 1000.0;
        }

        /// <summary>
        /// Получить стандартное отклонение для нормального распределения
        /// </summary>
        public double GetNormalStdDev()
        {
            return PacketIntervalMs * (NormalStdDevPercent / 100.0);
        }
    }
}
