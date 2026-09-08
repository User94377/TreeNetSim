using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TreeNetSim
{
    /// <summary>
    /// Дискретно-событийный симулятор древовидной сети.
    /// Топология: Центральный коммутатор -> Коммутатор0 + Коммутатор1 -> N станций.
    /// Маршрутизация:
    ///   - Внутри ветки: Станция -> КоммутаторВетки -> Станция
    ///   - Между ветками: Станция -> КоммутаторВетки -> ЦентральныйКоммутатор -> КоммутаторВетки -> Станция
    /// </summary>
    public class Simulator
    {
        public const int SWITCH_CENTRAL = 0;  // Центральный коммутатор
        public const int SWITCH_BRANCH0 = 1;  // Коммутатор ветки 0
        public const int SWITCH_BRANCH1 = 2;  // Коммутатор ветки 1

        private EventQueue calendar;
        private List<Station> stations;
        private Dictionary<int, SwitchNode> switches;

        private double currentTime;
        private double packetTransferMs;
        private SimulationParams parameters;
        private Random rng;

        private List<double> deliveryTimes;
        private int droppedCount;
        private int packetIdCounter;
        private int totalPacketsExpected;
        private int packetsFinished;

        public event Action<SimEvent> OnEventProcessed;

        public Simulator()
        {
            calendar = new EventQueue();
            stations = new List<Station>();
            switches = new Dictionary<int, SwitchNode>();
            deliveryTimes = new List<double>();
        }

        public SimulationResult Run(SimulationParams p)
        {
            parameters = p;
            Initialize(p);

            while (!calendar.IsEmpty && packetsFinished < totalPacketsExpected)
            {
                SimEvent e = calendar.Dequeue();
                currentTime = e.Time;
                HandleEvent(e);
                OnEventProcessed?.Invoke(e);
            }

            var result = CalculateResults();

            // ── ИТОГОВАЯ ОТЛАДКА ──────────────────────────────────────────
            Debug.WriteLine($"\n=== РЕЗУЛЬТАТЫ ===");
            Debug.WriteLine($"Доставлено:  {result.DeliveredCount}");
            Debug.WriteLine($"Отброшено:   {result.DroppedCount}");
            Debug.WriteLine($"Ср. время:   {result.AvgDeliveryTimeMs:F3} мс");
            Debug.WriteLine($"Время мод.:  {result.TotalSimTimeMs:F3} мс");

            // Статистика по коммутаторам
            string[] swNames = { "Центральный", "Ветка0", "Ветка1" };
            foreach (var kv in switches)
            {
                Debug.WriteLine($"Комм.{kv.Key} ({swNames[kv.Key]}): " +
                    $"очередь вх={kv.Value.InputQueue.Count} " +
                    $"(макс={kv.Value.MaxQueueIn})");
            }

            return result;
        }

        // ========== ИНИЦИАЛИЗАЦИЯ ==========

        private void Initialize(SimulationParams p)
        {
            calendar.Clear();
            stations.Clear();
            switches.Clear();
            deliveryTimes.Clear();
            currentTime = 0;
            packetIdCounter = 0;
            droppedCount = 0;
            packetsFinished = 0;
            rng = new Random();
            packetTransferMs = p.CalculatePacketTransferMs();

            // Три коммутатора с одинаковыми параметрами очередей
            switches[SWITCH_CENTRAL] = new SwitchNode(SWITCH_CENTRAL, p.SwitchQueueIn, p.SwitchQueueOut);
            switches[SWITCH_BRANCH0] = new SwitchNode(SWITCH_BRANCH0, p.SwitchQueueIn, p.SwitchQueueOut);
            switches[SWITCH_BRANCH1] = new SwitchNode(SWITCH_BRANCH1, p.SwitchQueueIn, p.SwitchQueueOut);

            int totalStations = p.StationsPerBranch * 2;
            totalPacketsExpected = 0;

            // ── ОТЛАДКА: параметры канала ──────────────────────────────────
            Debug.WriteLine($"\n=== ИНИЦИАЛИЗАЦИЯ ===");
            Debug.WriteLine($"Режим:             {p.TrafficMode}");
            Debug.WriteLine($"Распределение:     {p.DistributionType}");
            Debug.WriteLine($"Скорость канала:   {p.ChannelSpeedMbps} Мбит/с");
            Debug.WriteLine($"Размер пакета:     {p.PacketSizeBytes} байт");
            Debug.WriteLine($"Время передачи:    {packetTransferMs:F4} мс");
            Debug.WriteLine($"Время обработки:   {p.SwitchProcessMs} мс");
            Debug.WriteLine($"Интервал пакетов:  {p.PacketIntervalMs} мс");
            Debug.WriteLine($"Очередь (вх/вых):  {p.SwitchQueueIn} / {p.SwitchQueueOut}");
            Debug.WriteLine($"Станций всего:     {totalStations}");

            // ── Теоретическое минимальное время доставки ──────────────────
            double minTimeIntra = 2 * packetTransferMs + 1 * p.SwitchProcessMs;
            double minTimeInter = 4 * packetTransferMs + 3 * p.SwitchProcessMs;
            Debug.WriteLine($"Теор. мин. время (внутри ветки):  {minTimeIntra:F3} мс");
            Debug.WriteLine($"Теор. мин. время (между ветками): {minTimeInter:F3} мс");

            // ========== ИНИЦИАЛИЗАЦИЯ СТАНЦИЙ ==========

            if (p.TrafficMode == TrafficMode.Uniform)
            {
                // РЕЖИМ 1: Равномерное распределение
                int packetsPerDestination = p.PacketsPerStation / (totalStations - 1);
                int remainder = p.PacketsPerStation % (totalStations - 1);

                for (int i = 0; i < totalStations; i++)
                {
                    int branchId = i < p.StationsPerBranch ? 0 : 1;
                    Station station = new Station(i, branchId);

                    int distributed = 0;
                    for (int j = 0; j < totalStations; j++)
                    {
                        if (j != i)
                        {
                            int packetsToThisDest = packetsPerDestination;
                            if (distributed < remainder)
                            {
                                packetsToThisDest++;
                                distributed++;
                            }
                            station.PacketsToSend[j] = packetsToThisDest;
                            station.TotalPackets += packetsToThisDest;
                        }
                    }

                    totalPacketsExpected += station.TotalPackets;
                    stations.Add(station);

                    if (station.TotalPackets > 0)
                    {
                        double firstGenTime = rng.NextDouble() * p.PacketIntervalMs;
                        Debug.WriteLine($"  С{i} (ветка {branchId}): {station.TotalPackets} пакетов, старт в {firstGenTime:F3} мс");
                        calendar.Enqueue(new SimEvent(EventType.PacketGenerated, firstGenTime, null, station));
                    }
                }
            }
            else
            {
                // РЕЖИМ 2: Матрица трафика
                for (int i = 0; i < totalStations; i++)
                {
                    int branchId = i < p.StationsPerBranch ? 0 : 1;
                    Station station = new Station(i, branchId);

                    for (int j = 0; j < totalStations; j++)
                    {
                        if (i != j && p.TrafficMatrix != null && p.TrafficMatrix[i, j] > 0)
                        {
                            station.PacketsToSend[j] = p.TrafficMatrix[i, j];
                            station.TotalPackets += p.TrafficMatrix[i, j];
                        }
                    }

                    totalPacketsExpected += station.TotalPackets;
                    stations.Add(station);

                    if (station.TotalPackets > 0)
                    {
                        double firstGenTime = rng.NextDouble() * p.PacketIntervalMs;
                        Debug.WriteLine($"  С{i} (ветка {branchId}): {station.TotalPackets} пакетов, старт в {firstGenTime:F3} мс");
                        calendar.Enqueue(new SimEvent(EventType.PacketGenerated, firstGenTime, null, station));
                    }
                }
            }

            Debug.WriteLine($"Итого ожидается пакетов: {totalPacketsExpected}");
        }

        // ========== ДИСПЕТЧЕР СОБЫТИЙ ==========

        private void HandleEvent(SimEvent e)
        {
            switch (e.Type)
            {
                case EventType.PacketGenerated: OnPacketGenerated(e); break;
                case EventType.PacketArrivedAtSwitch: OnPacketArrivedAtSwitch(e); break;
                case EventType.PacketLeftSwitch: OnPacketLeftSwitch(e); break;
                case EventType.PacketDelivered: OnPacketDelivered(e); break;
            }
        }

        // ========== ОБРАБОТЧИКИ СОБЫТИЙ ==========

        // 1. Станция генерирует пакет и отправляет к коммутатору своей ветки
        private void OnPacketGenerated(SimEvent e)
        {
            Station station = e.Station;
            if (!station.HasMorePackets()) return;

            int destId = station.GetNextDestination(rng);
            if (destId < 0) return;

            Packet packet = new Packet(packetIdCounter++, station.StationId, destId, currentTime);
            station.PacketsSent++;
            station.DecrementPacketsTo(destId);

            int srcSwitch = GetBranchSwitch(station.StationId);

            Debug.WriteLine($"t={currentTime:F3} | ГЕНЕРАЦИЯ  | С{station.StationId}→С{destId} " +
                $"(пакет #{packet.Id}) → Комм.{srcSwitch}");

            ScheduleSwitchArrival(packet, srcSwitch);

            if (station.HasMorePackets())
            {
                double nextInterval = GenerateInterval();
                calendar.Enqueue(new SimEvent(
                    EventType.PacketGenerated,
                    currentTime + nextInterval,
                    null, station));
            }
        }

        // 2. Пакет прибыл на коммутатор
        private void OnPacketArrivedAtSwitch(SimEvent e)
        {
            SwitchNode sw = switches[e.SwitchId];
            Packet packet = e.Packet;

            if (!sw.TryEnqueueInput(packet))
            {
                Debug.WriteLine($"t={currentTime:F3} | ОТБРОШЕН   | пакет #{packet.Id} " +
                    $"(С{packet.SourceStation}→С{packet.DestStation}) на Комм.{e.SwitchId} " +
                    $"[очередь заполнена: {sw.InputQueue.Count}/{sw.MaxQueueIn}]");
                droppedCount++;
                packetsFinished++;
                return;
            }

            Debug.WriteLine($"t={currentTime:F3} | ПРИБЫТИЕ   | пакет #{packet.Id} " +
                $"(С{packet.SourceStation}→С{packet.DestStation}) на Комм.{e.SwitchId} " +
                $"[очередь: {sw.InputQueue.Count}/{sw.MaxQueueIn}]");

            if (!sw.IsBusy)
                StartSwitchProcessing(e.SwitchId);
        }

        // Коммутатор берёт пакет из очереди и начинает обработку
        private void StartSwitchProcessing(int switchId)
        {
            SwitchNode sw = switches[switchId];
            if (sw.IsBusy || !sw.HasPendingInput()) return;

            Packet packet = sw.DequeueInput();
            sw.IsBusy = true;
            sw.CurrentPacket = packet;

            Debug.WriteLine($"t={currentTime:F3} | ОБРАБОТКА  | пакет #{packet.Id} начат на Комм.{switchId}");

            SimEvent leaveEvent = new SimEvent(
                EventType.PacketLeftSwitch,
                currentTime + parameters.SwitchProcessMs,
                packet, null);
            leaveEvent.SwitchId = switchId;
            calendar.Enqueue(leaveEvent);
        }

        // 3. Коммутатор завершил обработку — определяем следующий узел
        private void OnPacketLeftSwitch(SimEvent e)
        {
            int switchId = e.SwitchId;
            SwitchNode sw = switches[switchId];
            Packet packet = e.Packet;

            sw.IsBusy = false;
            sw.CurrentPacket = null;

            if (sw.HasPendingInput())
                StartSwitchProcessing(switchId);

            int srcBranch = GetBranch(packet.SourceStation);
            int dstBranch = GetBranch(packet.DestStation);
            int dstSwitch = GetBranchSwitch(packet.DestStation);

            string nextNode;

            if (switchId == SWITCH_CENTRAL)
            {
                packet.PassedCentral = true;
                ScheduleSwitchArrival(packet, dstSwitch);
                nextNode = $"Комм.{dstSwitch} (ветка получателя)";
            }
            else if (packet.PassedCentral)
            {
                ScheduleDelivery(packet);
                nextNode = $"С{packet.DestStation} (доставка)";
            }
            else if (srcBranch == dstBranch)
            {
                ScheduleDelivery(packet);
                nextNode = $"С{packet.DestStation} (внутри ветки)";
            }
            else
            {
                ScheduleSwitchArrival(packet, SWITCH_CENTRAL);
                nextNode = "Комм.0 (центральный)";
            }

            Debug.WriteLine($"t={currentTime:F3} | УХОД       | пакет #{packet.Id} " +
                $"с Комм.{switchId} → {nextNode}");
        }

        // 4. Пакет доставлен получателю
        private void OnPacketDelivered(SimEvent e)
        {
            Packet packet = e.Packet;
            packet.DeliveryTime = currentTime;
            double deliveryTime = currentTime - packet.BirthTime;
            deliveryTimes.Add(deliveryTime);
            packetsFinished++;

            Debug.WriteLine($"t={currentTime:F3} | ДОСТАВЛЕН  | пакет #{packet.Id} " +
                $"(С{packet.SourceStation}→С{packet.DestStation}) " +
                $"время доставки={deliveryTime:F3} мс");
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

        private int GetBranch(int stationId)
        {
            return stationId < parameters.StationsPerBranch ? 0 : 1;
        }

        private int GetBranchSwitch(int stationId)
        {
            return GetBranch(stationId) == 0 ? SWITCH_BRANCH0 : SWITCH_BRANCH1;
        }

        private void ScheduleSwitchArrival(Packet packet, int switchId)
        {
            SimEvent ev = new SimEvent(
                EventType.PacketArrivedAtSwitch,
                currentTime + packetTransferMs,
                packet, null);
            ev.SwitchId = switchId;
            calendar.Enqueue(ev);
        }

        private void ScheduleDelivery(Packet packet)
        {
            calendar.Enqueue(new SimEvent(
                EventType.PacketDelivered,
                currentTime + packetTransferMs,
                packet, null));
        }

        // ========== РЕЗУЛЬТАТЫ ==========

        private SimulationResult CalculateResults()
        {
            return new SimulationResult
            {
                DeliveredCount = deliveryTimes.Count,
                DroppedCount = droppedCount,
                TotalSimTimeMs = currentTime,
                AvgDeliveryTimeMs = deliveryTimes.Count > 0 ? deliveryTimes.Average() : 0
            };
        }

        public (int inputCount, int outputCount, int maxIn, int maxOut) GetSwitchState(int switchId)
        {
            if (!switches.ContainsKey(switchId)) return (0, 0, 0, 0);
            var sw = switches[switchId];
            return (sw.InputQueue.Count, sw.OutputQueue.Count, sw.MaxQueueIn, sw.MaxQueueOut);
        }

        public List<Station> GetStations() => stations;

        // ===== ГЕНЕРАТОРЫ РАСПРЕДЕЛЕНИЙ =====

        private double GenerateInterval()
        {
            switch (parameters.DistributionType)
            {
                case DistributionType.Deterministic:
                    return GenerateDeterministicInterval();
                case DistributionType.Exponential:
                    return GenerateExponentialInterval(parameters.PacketIntervalMs);
                case DistributionType.Pareto:
                    return GenerateParetoInterval(
                        minInterval: parameters.PacketIntervalMs * 0.5,
                        alpha: parameters.ParetoAlpha);
                case DistributionType.Normal:
                    return GenerateNormalIntervalSafe(
                        mean: parameters.PacketIntervalMs,
                        stdDev: parameters.GetNormalStdDev());
                case DistributionType.Uniform:
                    return GenerateUniformInterval(
                        min: parameters.PacketIntervalMs * 0.5,
                        max: parameters.PacketIntervalMs * 1.5);
                default:
                    return parameters.PacketIntervalMs;
            }
        }

        private double GenerateDeterministicInterval() => parameters.PacketIntervalMs;

        private double GenerateExponentialInterval(double meanIntervalMs)
            => -Math.Log(rng.NextDouble()) * meanIntervalMs;

        private double GenerateParetoInterval(double minInterval, double alpha)
        {
            double u = rng.NextDouble();
            if (u < 1e-10) u = 1e-10;
            double interval = minInterval / Math.Pow(u, 1.0 / alpha);
            return Math.Min(interval, parameters.PacketIntervalMs * 20);
        }

        private double GenerateNormalIntervalSafe(double mean, double stdDev)
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            return Math.Max(0.01, mean + stdDev * z);
        }

        private double GenerateUniformInterval(double min, double max)
            => min + rng.NextDouble() * (max - min);
    }

    // ===== Узел коммутатора =====
    public class SwitchNode
    {
        public int Id { get; set; }
        public int MaxQueueIn { get; set; }
        public int MaxQueueOut { get; set; }
        public bool IsBusy { get; set; }
        public Packet CurrentPacket { get; set; }

        public Queue<Packet> InputQueue { get; private set; }
        public Queue<Packet> OutputQueue { get; private set; }

        public SwitchNode(int id, int maxQueueIn, int maxQueueOut)
        {
            Id = id;
            MaxQueueIn = maxQueueIn;
            MaxQueueOut = maxQueueOut;
            InputQueue = new Queue<Packet>();
            OutputQueue = new Queue<Packet>();
            IsBusy = false;
        }

        public bool TryEnqueueInput(Packet p)
        {
            if (InputQueue.Count >= MaxQueueIn) return false;
            InputQueue.Enqueue(p);
            return true;
        }

        public Packet DequeueInput()
            => InputQueue.Count > 0 ? InputQueue.Dequeue() : null;

        public bool HasPendingInput() => InputQueue.Count > 0;
    }
}