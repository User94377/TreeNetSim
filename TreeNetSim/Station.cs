using System;
using System.Collections.Generic;
using System.Text;

using System.Collections.Generic;

namespace TreeNetSim
{
    public class Station
    {
        public int StationId { get; set; }
        public int BranchId { get; set; }
        public int PacketsSent { get; set; }
        public int TotalPackets { get; set; }

        public Dictionary<int, int> PacketsToSend { get; set; }

        public Station(int stationId, int branchId)
        {
            StationId = stationId;
            BranchId = branchId;
            PacketsToSend = new Dictionary<int, int>();
            PacketsSent = 0;
            TotalPackets = 0;
        }

        public bool HasMorePackets()
        {
            return PacketsSent < TotalPackets;
        }

        public int GetNextDestination(Random rng)
        {
            var pending = PacketsToSend.Where(kvp => kvp.Value > 0).ToList();
            if (pending.Count == 0) return -1;

            return pending[rng.Next(pending.Count)].Key;
        }

        public void DecrementPacketsTo(int destId)
        {
            if (PacketsToSend.ContainsKey(destId) && PacketsToSend[destId] > 0)
                PacketsToSend[destId]--;
        }

    }
}
