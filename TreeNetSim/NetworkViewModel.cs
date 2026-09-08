using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TreeNetSim
{
    public class NetworkViewModel : INotifyPropertyChanged
    {
        private string _avgDeliveryText;
        private string _totalSimTimeText;
        private string _deliveredText;
        private string _droppedText;
        private double _queueInFill;
        private double _queueOutFill;
        private bool _isRunning;
        private string _currentEvent; 
        private int _totalEvents;


        public string AvgDeliveryText
        {
            get => _avgDeliveryText;
            set { _avgDeliveryText = value; OnPropertyChanged(); }
        }

        public string TotalSimTimeText
        {
            get => _totalSimTimeText;
            set { _totalSimTimeText = value; OnPropertyChanged(); }
        }

        public string DeliveredText
        {
            get => _deliveredText;
            set { _deliveredText = value; OnPropertyChanged(); }
        }

        public string DroppedText
        {
            get => _droppedText;
            set { _droppedText = value; OnPropertyChanged(); }
        }

        public double QueueInFill
        {
            get => _queueInFill;
            set { _queueInFill = value; OnPropertyChanged(); }
        }

        public double QueueOutFill
        {
            get => _queueOutFill;
            set { _queueOutFill = value; OnPropertyChanged(); }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set { _isRunning = value; OnPropertyChanged(); }
        }

        public string CurrentEvent
        {
            get => _currentEvent;
            set { _currentEvent = value; OnPropertyChanged(); }
        }

        public int TotalEvents
        {
            get => _totalEvents;
            set { _totalEvents = value; OnPropertyChanged(); }
        }

        public ObservableCollection<EventLogItem> EventLog { get; set; }

        public NetworkViewModel()
        {

            AvgDeliveryText = "0.00 мс";
            TotalSimTimeText = "0.00 мс";
            DeliveredText = "0 пакетов";
            DroppedText = "0 пакетов (0%)";
            QueueInFill = 0;
            QueueOutFill = 0;
            IsRunning = false;
            CurrentEvent = "Ожидание запуска...";
            TotalEvents = 0;

            EventLog = new ObservableCollection<EventLogItem>();
        }

        // Обновление результатов моделирования
        public void UpdateResults(SimulationResult result, SimulationParams parameters)
        {
            AvgDeliveryText = $"{result.AvgDeliveryTimeMs:F2} мс";
            TotalSimTimeText = $"{result.TotalSimTimeMs:F2} мс";
            DeliveredText = $"{result.DeliveredCount} пакетов";

            int totalExpected = parameters.TrafficMode == TrafficMode.Uniform
                ? parameters.StationsPerBranch * 2 * parameters.PacketsPerStation
                : parameters.TrafficMatrix.Cast<int>().Sum();
            double dropPercent = totalExpected > 0
                ? (result.DroppedCount * 100.0) / totalExpected
                : 0;

            DroppedText = $"{result.DroppedCount} пакетов ({dropPercent:F1}%)";
            CurrentEvent = "Моделирование завершено";
        }

        // Обновление заполненности очередей
        public void UpdateQueueStatus(int inputCount, int inputMax, int outputCount, int outputMax)
        {
            QueueInFill = inputMax > 0 ? (double)inputCount / inputMax : 0;
            QueueOutFill = outputMax > 0 ? (double)outputCount / outputMax : 0;
        }

        // Добавление события в лог
        public void AddEventLog(SimEvent simEvent, int eventNumber)
        {

            System.Diagnostics.Debug.WriteLine($"AddEventLog вызван: {eventNumber} - {simEvent.Type}");

            TotalEvents = eventNumber;

            string description = GetEventDescription(simEvent);
            CurrentEvent = description;

            var logItem = new EventLogItem
            {
                Number = eventNumber,
                Time = simEvent.Time,
                Type = simEvent.Type.ToString(),
                Description = description,
                Severity = GetEventSeverity(simEvent.Type)
            };


            EventLog.Insert(0, logItem);


            while (EventLog.Count > 100)
            {
                EventLog.RemoveAt(EventLog.Count - 1);
            }
        }

        // Описание события
        private string GetEventDescription(SimEvent simEvent)
        {
            switch (simEvent.Type)
            {
                case EventType.PacketGenerated:
                    return $"Станция {simEvent.Station?.StationId} сгенерировала пакет #{simEvent.Packet?.Id}";

                case EventType.PacketArrived:
                    return $"Пакет #{simEvent.Packet?.Id} прибыл на сервер от станции {simEvent.Packet?.SourceStation}";

                case EventType.ServerStarted:
                    return $"Сервер начал обработку пакета #{simEvent.Packet?.Id}";

                case EventType.ServerFinished:
                    return $"Сервер завершил обработку пакета #{simEvent.Packet?.Id}";

                case EventType.PacketSent:
                    return $"Пакет #{simEvent.Packet?.Id} отправлен к станции {simEvent.Packet?.DestStation}";

                case EventType.PacketDelivered:
                    return $"Пакет #{simEvent.Packet?.Id} доставлен на станцию {simEvent.Packet?.DestStation}";

                case EventType.PacketArrivedAtSwitch: 
                    string direction = simEvent.IsReturnPath ? "от сервера" : "от станции";
                    return $"Пакет #{simEvent.Packet?.Id} прибыл на коммутатор {simEvent.SwitchId} {direction}";

                case EventType.PacketLeftSwitch: 
                    return $"Пакет #{simEvent.Packet?.Id} покинул коммутатор {simEvent.SwitchId}";

                default:
                    return "Неизвестное событие";
            }
        }

        private string GetEventSeverity(EventType type)
        {
            switch (type)
            {
                case EventType.PacketGenerated:
                    return "Info";
                case EventType.PacketArrived:
                    return "Info";
                case EventType.PacketArrivedAtSwitch: 
                    return "Info";
                case EventType.PacketLeftSwitch:
                    return "Processing";
                case EventType.ServerStarted:
                    return "Processing";
                case EventType.ServerFinished:
                    return "Processing";
                case EventType.PacketSent:
                    return "Success";
                case EventType.PacketDelivered:
                    return "Success";
                default:
                    return "Info";
            }
        }


        public void ResetResults()
        {
            AvgDeliveryText = "0.00 мс";
            TotalSimTimeText = "0.00 мс";
            DeliveredText = "0 пакетов";
            DroppedText = "0 пакетов (0%)";
            QueueInFill = 0;
            QueueOutFill = 0;
            CurrentEvent = "Запуск моделирования...";
            TotalEvents = 0;
            EventLog.Clear();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Класс для элемента лога событий
    public class EventLogItem
    {
        public int Number { get; set; }
        public double Time { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string Severity { get; set; }

        public string TimeFormatted => $"{Time:F2} мс";
    }
}