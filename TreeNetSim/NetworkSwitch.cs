using System.Collections.Generic;

namespace TreeNetSim
{
    public class NetworkSwitch
    {
        public int Id { get; set; }
        public Queue<Packet> Queue { get; private set; }
        public int MaxQueueSize { get; set; }
        public int DroppedCount { get; set; }

        public NetworkSwitch(int id, int maxQueueSize)
        {
            Id = id;
            MaxQueueSize = maxQueueSize;
            Queue = new Queue<Packet>();
            DroppedCount = 0;
        }

        public bool TryEnqueue(Packet packet)
        {
            if (Queue.Count < MaxQueueSize)
            {
                Queue.Enqueue(packet);
                return true;
            }
            else
            {
                DroppedCount++;
                return false;
            }
        }

        public Packet Dequeue()
        {
            if (Queue.Count > 0)
            {
                return Queue.Dequeue();
            }
            return null;
        }

        public bool HasPackets()
        {
            return Queue.Count > 0;
        }
    }
}