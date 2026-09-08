namespace TreeNetSim
{
    public class Packet
    {
        public int Id { get; set; }
        public int SourceStation { get; set; }
        public int DestStation { get; set; }
        public double BirthTime { get; set; }
        public double DeliveryTime { get; set; }
        public bool PassedCentral { get; set; } 

        public Packet(int id, int sourceStation, int destStation, double birthTime)
        {
            Id = id;
            SourceStation = sourceStation;
            DestStation = destStation;
            BirthTime = birthTime;
            DeliveryTime = 0;
            PassedCentral = false;
        }

        public Packet() { }
    }
}