using System;
using System.Collections.Generic;
using System.Text;

namespace TreeNetSim
{
    public class SimulationResult
    {
        public double AvgDeliveryTimeMs { get; set; }
        public double TotalSimTimeMs { get; set; }
        public int DeliveredCount { get; set; }
        public int DroppedCount { get; set; }

        public SimulationResult()
        {
            AvgDeliveryTimeMs = 0;
            TotalSimTimeMs = 0;
            DeliveredCount = 0;
            DroppedCount = 0;
        }
    }
}
