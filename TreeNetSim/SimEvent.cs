using System;

namespace TreeNetSim
{
    public enum EventType
    {
        PacketGenerated,
        PacketArrived,
        ServerStarted,
        ServerFinished,
        PacketSent,
        PacketDelivered,
        PacketArrivedAtSwitch, 
        PacketLeftSwitch       
    }

    public class SimEvent
    {
        public int Id { get; set; }
        public EventType Type { get; set; }
        public double Time { get; set; }
        public Packet Packet { get; set; }
        public Station Station { get; set; }
        public int SwitchId { get; set; }          
        public bool IsReturnPath { get; set; }    

        public SimEvent(EventType type, double time, Packet packet = null, Station station = null)
        {
            Type = type;
            Time = time;
            Packet = packet;
            Station = station;
            SwitchId = -1;
            IsReturnPath = false;
        }

        public SimEvent()
        {
            SwitchId = -1;
            IsReturnPath = false;
        }

        public override string ToString()
        {
            string switchInfo = SwitchId >= 0 ? $", Switch:{SwitchId}" : "";
            return $"[{Time:F2}ms] {Type} - Packet:{Packet?.Id}, Station:{Station?.StationId}{switchInfo}";
        }
    }
}