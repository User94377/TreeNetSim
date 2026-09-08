using System;

namespace TreeNetSim
{
    public class SimulatorTest
    {
        public static void RunConsoleTest()
        {
            Console.WriteLine("=== Тест симулятора древовидной сети ===\n");

            SimulationParams parameters = new SimulationParams
            {
                StationsPerBranch = 2,
                ChannelSpeedMbps  = 100,
                PacketSizeBytes   = 512,
                SwitchQueueIn     = 10,
                SwitchQueueOut    = 10,
                SwitchProcessMs   = 1.0,
                PacketsPerStation = 5,
                PacketIntervalMs  = 10.0
            };

            Console.WriteLine("Параметры моделирования:");
            Console.WriteLine($"  Станций на ветвь: {parameters.StationsPerBranch}");
            Console.WriteLine($"  Пакетов на станцию: {parameters.PacketsPerStation}");
            Console.WriteLine($"  Скорость канала: {parameters.ChannelSpeedMbps} Мбит/с");
            Console.WriteLine($"  Размер пакета: {parameters.PacketSizeBytes} байт");
            Console.WriteLine($"  Входная очередь коммутатора: {parameters.SwitchQueueIn}");
            Console.WriteLine($"  Выходная очередь коммутатора: {parameters.SwitchQueueOut}");
            Console.WriteLine($"  Время обработки коммутатором: {parameters.SwitchProcessMs} мс");
            Console.WriteLine($"  Интервал между пакетами: {parameters.PacketIntervalMs} мс");

            double transferTime = parameters.CalculatePacketTransferMs();
            Console.WriteLine($"  Время передачи пакета: {transferTime:F4} мс");
            Console.WriteLine();

            Simulator simulator = new Simulator();

            int eventCount = 0;
            simulator.OnEventProcessed += (e) => { eventCount++; };

            Console.WriteLine("Запуск моделирования...\n");
            DateTime startTime = DateTime.Now;

            SimulationResult result = simulator.Run(parameters);

            TimeSpan executionTime = DateTime.Now - startTime;

            Console.WriteLine("=== Результаты моделирования ===");
            Console.WriteLine($"Обработано событий: {eventCount}");
            Console.WriteLine($"Время выполнения: {executionTime.TotalMilliseconds:F2} мс (реальное)\n");

            Console.WriteLine($"Модельное время: {result.TotalSimTimeMs:F2} мс");
            Console.WriteLine($"Доставлено пакетов: {result.DeliveredCount}");
            Console.WriteLine($"Отброшено пакетов: {result.DroppedCount}");

            int totalExpected = parameters.StationsPerBranch * 2 * parameters.PacketsPerStation;
            Console.WriteLine($"Ожидалось всего: {totalExpected}");

            if (result.DeliveredCount > 0)
            {
                Console.WriteLine($"\nСреднее время доставки: {result.AvgDeliveryTimeMs:F4} мс");
                double lossPercent = (result.DroppedCount * 100.0) / totalExpected;
                Console.WriteLine($"Процент потерь: {lossPercent:F2}%");
            }
            else
            {
                Console.WriteLine("\nНи один пакет не доставлен!");
            }

            Console.WriteLine("\n=== Тест завершен ===");
        }

        public static void RunMultipleTests()
        {
            Console.WriteLine("=== Множественные тесты ===\n");

            Console.WriteLine("--- Тест 1: Нормальные условия ---");
            TestWithParams(new SimulationParams());

            Console.WriteLine("\n--- Тест 2: Малая очередь (ожидаются потери) ---");
            TestWithParams(new SimulationParams
            {
                StationsPerBranch = 3,
                SwitchQueueIn     = 1,
                SwitchQueueOut    = 1,
                PacketsPerStation = 10,
                PacketIntervalMs  = 1.0
            });

            Console.WriteLine("\n--- Тест 3: Высокая нагрузка ---");
            TestWithParams(new SimulationParams
            {
                StationsPerBranch = 5,
                PacketsPerStation = 50,
                PacketIntervalMs  = 0.5,
                SwitchProcessMs   = 2.0
            });

            Console.WriteLine("\n--- Тест 4: Медленный канал ---");
            TestWithParams(new SimulationParams
            {
                StationsPerBranch = 2,
                ChannelSpeedMbps  = 10,
                PacketSizeBytes   = 1500,
                PacketsPerStation = 10
            });
        }

        private static void TestWithParams(SimulationParams p)
        {
            Simulator sim = new Simulator();
            SimulationResult result = sim.Run(p);

            int totalExpected = p.StationsPerBranch * 2 * p.PacketsPerStation;
            double lossPercent = totalExpected > 0
                ? (result.DroppedCount * 100.0) / totalExpected : 0;

            Console.WriteLine($"  Станций: {p.StationsPerBranch * 2}, Пакетов: {totalExpected}");
            Console.WriteLine($"  Доставлено: {result.DeliveredCount}, Отброшено: {result.DroppedCount} ({lossPercent:F1}%)");
            Console.WriteLine($"  Среднее время доставки: {result.AvgDeliveryTimeMs:F4} мс");
            Console.WriteLine($"  Модельное время: {result.TotalSimTimeMs:F2} мс");
        }
    }
}
