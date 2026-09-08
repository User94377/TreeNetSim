using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TreeNetSim
{
    public partial class MainWindow : Window
    {
        private NetworkViewModel viewModel;
        private SimulationParams currentParams;
        private SimulationResult currentResult;
        private int currentStationsPerBranch = 3;
        private Dictionary<string, Point> nodePositions = new Dictionary<string, Point>();

        // Матрица трафика
        private TextBox[,] matrixCells;

        public MainWindow()
        {
            InitializeComponent();
            viewModel = new NetworkViewModel();
            DataContext = viewModel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTrafficModeUI();
            DrawNetwork(currentStationsPerBranch);
            ResetParameters();
        }

        // ===== РЕЖИМ ТРАФИКА =====

        private void TrafficMode_Changed(object sender, RoutedEventArgs e)
        {
            UpdateTrafficModeUI();
        }

        private void UpdateTrafficModeUI()
        {
            if (RbUniform == null || PanelUniform == null || TrafficMatrixContainer == null)
                return;

            bool isUniform = RbUniform.IsChecked == true;


            PanelUniform.Visibility = isUniform ? Visibility.Visible : Visibility.Collapsed;


            TrafficMatrixContainer.IsEnabled = !isUniform;
            TrafficMatrixContainer.Opacity = isUniform ? 0.4 : 1.0;
        }

        // ===== МАТРИЦА ТРАФИКА =====



        private void BuildTrafficMatrix(int stationsCount, int[,] existing)
        {

            if (MatrixGrid == null)
                return;

            MatrixGrid.Children.Clear();
            MatrixGrid.RowDefinitions.Clear();
            MatrixGrid.ColumnDefinitions.Clear();

            matrixCells = new TextBox[stationsCount, stationsCount];

            int cellWidth = 40;
            int headerWidth = 40;
            int cellHeight = 28;
            int headerHeight = 28;

            MatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(headerWidth) });
            for (int i = 0; i < stationsCount; i++)
                MatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(cellWidth) });

            MatrixGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(headerHeight) });
            for (int i = 0; i < stationsCount; i++)
                MatrixGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(cellHeight) });

            AddMatrixLabel("От\\К", 0, 0, true);

            for (int col = 0; col < stationsCount; col++)
                AddMatrixLabel($"{col}", 0, col + 1, true);

            for (int row = 0; row < stationsCount; row++)
                AddMatrixLabel($"{row}", row + 1, 0, true);

            for (int row = 0; row < stationsCount; row++)
            {
                for (int col = 0; col < stationsCount; col++)
                {
                    if (row == col)
                    {
                        AddMatrixLabel("-", row + 1, col + 1, false, "#E0E0E0");
                    }
                    else
                    {
                        int value = existing?[row, col] ?? 0;
                        var textBox = AddMatrixTextBox(value, row + 1, col + 1);
                        matrixCells[row, col] = textBox;
                    }
                }
            }
        }

        private void AddMatrixLabel(string text, int row, int col, bool isHeader, string bgColor = null)
        {
            var border = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(0.5),
                Background = isHeader
                    ? new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    : (bgColor != null ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgColor)) : Brushes.White)
            };

            var tb = new TextBlock
            {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = isHeader ? FontWeights.Bold : FontWeights.Normal,
                Foreground = isHeader ? Brushes.White : Brushes.Black,
                FontSize = 10,
                Padding = new Thickness(2)
            };

            border.Child = tb;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            MatrixGrid.Children.Add(border);
        }

        private TextBox AddMatrixTextBox(int value, int row, int col)
        {
            var textBox = new TextBox
            {
                Text = value.ToString(),
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                FontSize = 10,
                Margin = new Thickness(0.5),
                Padding = new Thickness(2),
                BorderThickness = new Thickness(0.5),
                BorderBrush = Brushes.Gray
            };

            Grid.SetRow(textBox, row);
            Grid.SetColumn(textBox, col);
            MatrixGrid.Children.Add(textBox);

            return textBox;
        }

        private int[,] GetTrafficMatrixFromUI()
        {
            int totalStations = currentStationsPerBranch * 2;
            int[,] matrix = new int[totalStations, totalStations];

            for (int row = 0; row < totalStations; row++)
            {
                for (int col = 0; col < totalStations; col++)
                {
                    if (row != col)
                    {
                        if (int.TryParse(matrixCells[row, col].Text, out int value) && value >= 0)
                            matrix[row, col] = value;
                        else
                            throw new ValidationException($"Некорректное значение в матрице [{row}, {col}]", matrixCells[row, col]);
                    }
                }
            }

            return matrix;
        }

        // ===== ОТРИСОВКА СХЕМЫ СЕТИ =====

        private void DrawNetwork(int stationsPerBranch)
        {
            if (NetworkCanvas == null || NetworkCanvas.ActualWidth < 1) return;

            NetworkCanvas.Children.Clear();
            nodePositions.Clear();

            double W = NetworkCanvas.ActualWidth;
            double H = NetworkCanvas.ActualHeight;

            if (W < 10 || H < 10) return;

            double cx = W / 2;
            double centralY = H * 0.12;
            double branchY = H * 0.42;
            double stationY = H * 0.78;

            double switch0X = cx - W * 0.24;
            double switch1X = cx + W * 0.24;

            nodePositions["swC"] = new Point(cx, centralY);
            nodePositions["sw0"] = new Point(switch0X, branchY);
            nodePositions["sw1"] = new Point(switch1X, branchY);

            var branch0X = BranchXPositions(stationsPerBranch, switch0X, W * 0.40);
            var branch1X = BranchXPositions(stationsPerBranch, switch1X, W * 0.40);

            for (int i = 0; i < stationsPerBranch; i++)
            {
                nodePositions[$"st0_{i}"] = new Point(branch0X[i], stationY);
                nodePositions[$"st1_{i}"] = new Point(branch1X[i], stationY);
            }

            DrawLine(nodePositions["swC"], nodePositions["sw0"]);
            DrawLine(nodePositions["swC"], nodePositions["sw1"]);

            for (int i = 0; i < stationsPerBranch; i++)
            {
                DrawLine(nodePositions["sw0"], nodePositions[$"st0_{i}"]);
                DrawLine(nodePositions["sw1"], nodePositions[$"st1_{i}"]);
            }

            DrawSwitchNode(nodePositions["swC"], "К1", isCentral: true);
            DrawSwitchNode(nodePositions["sw0"], "К0", isCentral: false);
            DrawSwitchNode(nodePositions["sw1"], "К2", isCentral: false);

            for (int i = 0; i < stationsPerBranch; i++)
            {
                int id0 = i;
                int id1 = stationsPerBranch + i;
                DrawStationNode(nodePositions[$"st0_{i}"], id0.ToString(), id0);
                DrawStationNode(nodePositions[$"st1_{i}"], id1.ToString(), id1);
            }

            DrawBranchLabel(switch0X, stationY + 32, "Ветка 0");
            DrawBranchLabel(switch1X, stationY + 32, "Ветка 1");
        }

        private List<double> BranchXPositions(int count, double centerX, double totalWidth)
        {
            var result = new List<double>();
            if (count == 1)
            {
                result.Add(centerX);
                return result;
            }

            double step = totalWidth / (count + 1);
            double startX = centerX - totalWidth / 2 + step;

            for (int i = 0; i < count; i++)
                result.Add(startX + i * step);

            return result;
        }

        private void DrawLine(Point from, Point to)
        {
            Line line = new Line
            {
                X1 = from.X,
                Y1 = from.Y,
                X2 = to.X,
                Y2 = to.Y,
                Stroke = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 6, 3 }
            };
            Canvas.SetZIndex(line, 0);
            NetworkCanvas.Children.Add(line);
        }

        private void DrawSwitchNode(Point pos, string label, bool isCentral = false)
        {
            double size = isCentral ? 44 : 36;
            var fillColor = isCentral ? Color.FromRgb(33, 150, 243) : Color.FromRgb(255, 152, 0);
            var strokeColor = isCentral ? Color.FromRgb(20, 100, 180) : Color.FromRgb(180, 100, 0);

            Rectangle rect = new Rectangle
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(fillColor),
                Stroke = new SolidColorBrush(strokeColor),
                StrokeThickness = 2,
                RadiusX = 5,
                RadiusY = 5
            };

            Canvas.SetLeft(rect, pos.X - size / 2);
            Canvas.SetTop(rect, pos.Y - size / 2);
            Canvas.SetZIndex(rect, 2);
            NetworkCanvas.Children.Add(rect);

            TextBlock txt = new TextBlock
            {
                Text = label,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };

            txt.Measure(new Size(100, 100));
            Canvas.SetLeft(txt, pos.X - txt.DesiredSize.Width / 2);
            Canvas.SetTop(txt, pos.Y - txt.DesiredSize.Height / 2);
            Canvas.SetZIndex(txt, 3);
            NetworkCanvas.Children.Add(txt);

            TextBlock sub = new TextBlock
            {
                Text = "Коммутатор",
                FontSize = 9,
                Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100))
            };

            sub.Measure(new Size(100, 100));
            Canvas.SetLeft(sub, pos.X - sub.DesiredSize.Width / 2);
            Canvas.SetTop(sub, pos.Y + size / 2 + 4);
            Canvas.SetZIndex(sub, 3);
            NetworkCanvas.Children.Add(sub);
        }

        private void DrawStationNode(Point pos, string id, int stationIndex)
        {
            double r = 18;
            Color color = StationColors[stationIndex % StationColors.Length];
            var fill = new SolidColorBrush(color);
            DrawCircleNode(pos, r, fill, id, 11, Colors.White, aboveLabel: false);
        }

        private void DrawCircleNode(Point pos, double r, SolidColorBrush fill,
            string label, double fontSize, Color textColor, bool aboveLabel)
        {
            Ellipse circle = new Ellipse
            {
                Width = r * 2,
                Height = r * 2,
                Fill = fill,
                Stroke = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                StrokeThickness = 2
            };

            Canvas.SetLeft(circle, pos.X - r);
            Canvas.SetTop(circle, pos.Y - r);
            Canvas.SetZIndex(circle, 2);
            NetworkCanvas.Children.Add(circle);

            TextBlock txt = new TextBlock
            {
                Text = label,
                FontSize = fontSize,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(textColor)
            };

            txt.Measure(new Size(200, 100));
            double tw = txt.DesiredSize.Width;
            double th = txt.DesiredSize.Height;

            Canvas.SetLeft(txt, pos.X - tw / 2);
            Canvas.SetTop(txt, pos.Y - th / 2);
            Canvas.SetZIndex(txt, 3);
            NetworkCanvas.Children.Add(txt);
        }

        private void DrawBranchLabel(double centerX, double y, string text)
        {
            TextBlock lbl = new TextBlock
            {
                Text = text,
                FontSize = 11,
                FontStyle = FontStyles.Italic,
                Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120))
            };

            lbl.Measure(new Size(200, 100));
            Canvas.SetLeft(lbl, centerX - lbl.DesiredSize.Width / 2);
            Canvas.SetTop(lbl, y);
            Canvas.SetZIndex(lbl, 3);
            NetworkCanvas.Children.Add(lbl);
        }

        private void NetworkCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DrawNetwork(currentStationsPerBranch);
        }

        private void TxtStationsPerBranch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(TxtStationsPerBranch.Text, out int n) && n >= 1 && n <= 20)
            {
                currentStationsPerBranch = n;
                DrawNetwork(currentStationsPerBranch);

                BuildTrafficMatrix(n * 2, null);
            }
        }

        // ===== ЗАПУСК СИМУЛЯЦИИ =====

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateAndReadParameters(out SimulationParams parameters))
                return;

            currentParams = parameters;
            viewModel.IsRunning = true;
            viewModel.ResetResults();

            try
            {
                SimulationResult result = await Task.Run(() =>
                {
                    Simulator sim = new Simulator();
                    return sim.Run(parameters);
                });

                currentResult = result;
                viewModel.UpdateResults(result, parameters);

                MessageBox.Show(
                    $"Моделирование завершено!\n\n" +
                    $"Доставлено пакетов: {result.DeliveredCount}\n" +
                    $"Отброшено пакетов: {result.DroppedCount}\n" +
                    $"Среднее время доставки: {result.AvgDeliveryTimeMs:F2} мс",
                    "Результаты", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при моделировании:\n{ex.Message}\n\n{ex.StackTrace}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                viewModel.IsRunning = false;
            }
        }

        // ===== ВАЛИДАЦИЯ ПАРАМЕТРОВ =====

        private bool ValidateAndReadParameters(out SimulationParams parameters)
        {
            parameters = new SimulationParams();

            try
            {
                parameters.ChannelSpeedMbps = ParseDouble(TxtChannelSpeed, "Скорость канала", 1, 100000);
                parameters.PacketSizeBytes = ParseInt(TxtPacketSize, "Размер пакета", 64, 65535);
                parameters.SwitchQueueIn = ParseInt(TxtSwitchQueueIn, "Входная очередь", 1, 10000);
                parameters.SwitchQueueOut = ParseInt(TxtSwitchQueueOut, "Выходная очередь", 1, 10000);
                parameters.SwitchProcessMs = ParseDouble(TxtSwitchProcessMs, "Время обработки", 0.001, 10000);
                parameters.StationsPerBranch = ParseInt(TxtStationsPerBranch, "Станций на ветвь", 1, 50);
                parameters.PacketIntervalMs = ParseDouble(TxtPacketInterval, "Интервал между пакетами", 0.01, 100000);

                if (RbUniform.IsChecked == true)
                {
                    parameters.TrafficMode = TrafficMode.Uniform;
                    parameters.PacketsPerStation = ParseInt(TxtPacketsPerStation, "Пакетов на станцию", 1, 100000);
                }
                else
                {
                    parameters.TrafficMode = TrafficMode.TrafficMatrix;
                    parameters.TrafficMatrix = GetTrafficMatrixFromUI();
                }

                parameters.DistributionType = DistributionType.Exponential;

                return true;
            }
            catch (ValidationException ex)
            {
                MessageBox.Show(ex.Message, "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                ex.TextBox?.Focus();
                return false;
            }
        }

        private int ParseInt(TextBox tb, string name, int min, int max)
        {
            tb.BorderBrush = SystemColors.ControlDarkBrush;
            if (!int.TryParse(tb.Text, out int v))
                throw new ValidationException($"Поле «{name}» должно содержать целое число.", tb);
            if (v < min || v > max)
                throw new ValidationException($"Поле «{name}» должно быть в диапазоне {min}–{max}.", tb);
            return v;
        }

        private double ParseDouble(TextBox tb, string name, double min, double max)
        {
            tb.BorderBrush = SystemColors.ControlDarkBrush;
            string text = tb.Text.Replace(',', '.');
            if (!double.TryParse(text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double v))
                throw new ValidationException($"Поле «{name}» должно содержать число.", tb);
            if (v < min || v > max)
                throw new ValidationException($"Поле «{name}» должно быть в диапазоне {min}–{max}.", tb);
            return v;
        }

        // ===== ПРОЧИЕ ОБРАБОТЧИКИ =====

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            ResetParameters();
            viewModel.ResetResults();
            DrawNetwork(currentStationsPerBranch);
        }

        private void ResetParameters()
        {
            SimulationParams d = new SimulationParams();

            TxtChannelSpeed.Text = d.ChannelSpeedMbps.ToString();
            TxtPacketSize.Text = d.PacketSizeBytes.ToString();
            TxtSwitchQueueIn.Text = d.SwitchQueueIn.ToString();
            TxtSwitchQueueOut.Text = d.SwitchQueueOut.ToString();
            TxtSwitchProcessMs.Text = d.SwitchProcessMs.ToString();
            TxtStationsPerBranch.Text = d.StationsPerBranch.ToString();
            TxtPacketInterval.Text = d.PacketIntervalMs.ToString();

            RbUniform.IsChecked = true;
            TxtPacketsPerStation.Text = d.PacketsPerStation.ToString();

            currentStationsPerBranch = d.StationsPerBranch;

            // Инициализируем матрицу (с проверкой на null внутри метода)
            BuildTrafficMatrix(currentStationsPerBranch * 2, null);
        }

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            string help =
@"СПРАВКА ПО ПРОГРАММЕ
────────────────────────────────────
Программа выполняет имитационное моделирование передачи
пакетов в древовидной локальной сети методом
дискретно-событийного моделирования (DES).

ТОПОЛОГИЯ СЕТИ:
Центральный коммутатор соединён с двумя коммутаторами веток.
К каждому коммутатору подключено N рабочих станций.

ПАРАМЕТРЫ:
• Скорость канала - пропускная способность, Мбит/с
• Размер пакета - размер передаваемых данных, байт
• Входная/выходная очередь - буферы коммутаторов, пакетов
• Время обработки - задержка обработки на коммутаторе, мс

РЕЖИМЫ ТРАФИКА:
• Максимальной нагрузки - все станции отправляют одинаковое количество пакетов
• Адресации - количество пакетов задаётся индивидуально для каждой пары станций

РЕЗУЛЬТАТЫ:
• Среднее время доставки - от генерации до получения адресатом
• Время моделирования - общее модельное время
• Доставлено / Отброшено - количество пакетов
";

            MessageBox.Show(help, "Справка", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (currentResult == null || currentParams == null)
            {
                MessageBox.Show("Нет результатов для сохранения.\nСначала запустите моделирование.",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = $"simulation_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    SaveResults(dlg.FileName);
                    MessageBox.Show($"Результаты сохранены:\n{dlg.FileName}",
                        "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении:\n{ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveResults(string fileName)
        {
            int totalStations = currentParams.StationsPerBranch * 2;
            int totalExpected = currentParams.TrafficMode == TrafficMode.Uniform
                ? totalStations * currentParams.PacketsPerStation
                : currentParams.TrafficMatrix.Cast<int>().Sum();

            double deliveryRate = totalExpected > 0
                ? currentResult.DeliveredCount * 100.0 / totalExpected : 0;
            double lossRate = totalExpected > 0
                ? currentResult.DroppedCount * 100.0 / totalExpected : 0;

            using (StreamWriter w = new StreamWriter(fileName))
            {
                w.WriteLine("=== РЕЗУЛЬТАТЫ МОДЕЛИРОВАНИЯ ДРЕВОВИДНОЙ СЕТИ ===");
                w.WriteLine($"Дата: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                w.WriteLine();

                w.WriteLine("ТОПОЛОГИЯ:");
                w.WriteLine($"  Станций на ветвь: {currentParams.StationsPerBranch}");
                w.WriteLine($"  Всего станций: {totalStations}");
                w.WriteLine($"  Коммутаторов: 3 (1 центральный + 2 ветки)");
                w.WriteLine();

                w.WriteLine("ПАРАМЕТРЫ:");
                w.WriteLine($"  Скорость канала: {currentParams.ChannelSpeedMbps} Мбит/с");
                w.WriteLine($"  Размер пакета: {currentParams.PacketSizeBytes} байт");
                w.WriteLine($"  Входная очередь: {currentParams.SwitchQueueIn} пакетов");
                w.WriteLine($"  Выходная очередь: {currentParams.SwitchQueueOut} пакетов");
                w.WriteLine($"  Время обработки: {currentParams.SwitchProcessMs} мс");
                w.WriteLine($"  Интервал пакетов: {currentParams.PacketIntervalMs} мс");
                w.WriteLine($"  Время передачи пакета: {currentParams.CalculatePacketTransferMs():F4} мс");
                w.WriteLine();

                w.WriteLine("РЕЖИМ ТРАФИКА:");
                w.WriteLine($"  {(currentParams.TrafficMode == TrafficMode.Uniform ? "Равномерный" : "Матрица трафика")}");
                if (currentParams.TrafficMode == TrafficMode.Uniform)
                {
                    w.WriteLine($"  Пакетов на станцию: {currentParams.PacketsPerStation}");
                }
                w.WriteLine();

                w.WriteLine("РЕЗУЛЬТАТЫ:");
                w.WriteLine($"  Среднее время доставки: {currentResult.AvgDeliveryTimeMs:F4} мс");
                w.WriteLine($"  Время моделирования: {currentResult.TotalSimTimeMs:F2} мс");
                w.WriteLine($"  Доставлено пакетов: {currentResult.DeliveredCount} из {totalExpected}");
                w.WriteLine($"  Отброшено пакетов: {currentResult.DroppedCount}");
                w.WriteLine($"  Процент доставки: {deliveryRate:F2}%");
                w.WriteLine($"  Процент потерь: {lossRate:F2}%");
                w.WriteLine();

                w.WriteLine("=== КОНЕЦ ОТЧЁТА ===");
            }
        }

        //private void BtnDistributionInfo_Click(object sender, RoutedEventArgs e)
        //{
        //    var infoWindow = new DistributionInfoWindow();
        //    infoWindow.ShowDialog();
        //}

        private static readonly Color[] StationColors = new Color[]
{
    Color.FromRgb(76,  175,  80),   // Зелёный
    Color.FromRgb(33,  150, 243),   // Синий
    Color.FromRgb(244,  67,  54),   // Красный
    Color.FromRgb(156,  39, 176),   // Фиолетовый
    Color.FromRgb(255, 193,   7),   // Жёлтый
    Color.FromRgb(0,   188, 212),   // Голубой
    Color.FromRgb(255,  87,  34),   // Оранжевый
    Color.FromRgb(233,  30,  99),   // Розовый
    Color.FromRgb(63,   81, 181),   // Индиго
    Color.FromRgb(0,   150, 136),   // Бирюзовый
    Color.FromRgb(139, 195,  74),   // Лайм
    Color.FromRgb(121,  85,  72),   // Коричневый
    Color.FromRgb(255, 152,   0),   // Апельсин
    Color.FromRgb(103,  58, 183),   // Глубокий фиолет
    Color.FromRgb(0,   137, 123),   // Тёмная бирюза
    Color.FromRgb(205, 220,  57),   // Жёлто-зелёный
    Color.FromRgb(255,  64, 129),   // Яркий розовый
    Color.FromRgb(48,   63, 159),   // Тёмный индиго
    Color.FromRgb(38,  166, 154),   // Морской
    Color.FromRgb(255, 183,  77),   // Светлый оранжевый
};

        private int logEventCount = 0;

        private void AddLog(string message)
        {
            // Заглушка
        }

        private void ClearLog()
        {
            logEventCount = 0;
            TxtLog.Text = "Готово к запуску...";
            TxtLogCount.Text = "Событий: 0";
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            ClearLog();
        }


    }

    public class ValidationException : Exception
    {
        public TextBox TextBox { get; }

        public ValidationException(string message, TextBox textBox) : base(message)
        {
            TextBox = textBox;
        }
    }


}