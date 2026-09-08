using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace TreeNetSim
{
    public class AnimationController
    {
        private Canvas canvas;

        public AnimationController(Canvas canvas)
        {
            this.canvas = canvas;
        }

        // Основной метод анимации пакета
        public void AnimatePacket(Point from, Point to, double durationMs, Color color, Action onCompleted = null)
        {
            // Создание визуального элемента пакета
            Ellipse packet = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 1
            };

            // Добавление на canvas
            canvas.Children.Add(packet);
            Canvas.SetLeft(packet, from.X - 5);
            Canvas.SetTop(packet, from.Y - 5);
            Canvas.SetZIndex(packet, 100); // Поверх всех элементов

            // Создание анимации перемещения
            Storyboard storyboard = new Storyboard();

            // Анимация по X
            DoubleAnimation animX = new DoubleAnimation
            {
                From = from.X - 5,
                To = to.X - 5,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(animX, packet);
            Storyboard.SetTargetProperty(animX, new PropertyPath(Canvas.LeftProperty));

            // Анимация по Y
            DoubleAnimation animY = new DoubleAnimation
            {
                From = from.Y - 5,
                To = to.Y - 5,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            Storyboard.SetTarget(animY, packet);
            Storyboard.SetTargetProperty(animY, new PropertyPath(Canvas.TopProperty));

            storyboard.Children.Add(animX);
            storyboard.Children.Add(animY);

            // Обработчик завершения анимации
            storyboard.Completed += (s, e) =>
            {
                canvas.Children.Remove(packet);
                onCompleted?.Invoke();
            };

            storyboard.Begin();
        }

        // Анимация с пульсацией
        public void AnimatePacketWithPulse(Point from, Point to, double durationMs, Color color)
        {
            Ellipse packet = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.White,
                StrokeThickness = 1
            };

            canvas.Children.Add(packet);
            Canvas.SetLeft(packet, from.X - 5);
            Canvas.SetTop(packet, from.Y - 5);
            Canvas.SetZIndex(packet, 100);

            Storyboard storyboard = new Storyboard();

            // Движение
            DoubleAnimation animX = new DoubleAnimation
            {
                From = from.X - 5,
                To = to.X - 5,
                Duration = TimeSpan.FromMilliseconds(durationMs)
            };
            Storyboard.SetTarget(animX, packet);
            Storyboard.SetTargetProperty(animX, new PropertyPath(Canvas.LeftProperty));

            DoubleAnimation animY = new DoubleAnimation
            {
                From = from.Y - 5,
                To = to.Y - 5,
                Duration = TimeSpan.FromMilliseconds(durationMs)
            };
            Storyboard.SetTarget(animY, packet);
            Storyboard.SetTargetProperty(animY, new PropertyPath(Canvas.TopProperty));

            // Пульсация размера
            DoubleAnimation pulseWidth = new DoubleAnimation
            {
                From = 10,
                To = 14,
                Duration = TimeSpan.FromMilliseconds(300),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            Storyboard.SetTarget(pulseWidth, packet);
            Storyboard.SetTargetProperty(pulseWidth, new PropertyPath(Ellipse.WidthProperty));

            DoubleAnimation pulseHeight = new DoubleAnimation
            {
                From = 10,
                To = 14,
                Duration = TimeSpan.FromMilliseconds(300),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            Storyboard.SetTarget(pulseHeight, packet);
            Storyboard.SetTargetProperty(pulseHeight, new PropertyPath(Ellipse.HeightProperty));

            storyboard.Children.Add(animX);
            storyboard.Children.Add(animY);
            storyboard.Children.Add(pulseWidth);
            storyboard.Children.Add(pulseHeight);

            storyboard.Completed += (s, e) =>
            {
                canvas.Children.Remove(packet);
            };

            storyboard.Begin();
        }

        // Показ отброшенного пакета
        public void ShowDroppedPacket(Point position)
        {
            // Создаём крестик из двух линий
            Line line1 = new Line
            {
                X1 = position.X - 8,
                Y1 = position.Y - 8,
                X2 = position.X + 8,
                Y2 = position.Y + 8,
                Stroke = Brushes.Red,
                StrokeThickness = 3
            };

            Line line2 = new Line
            {
                X1 = position.X + 8,
                Y1 = position.Y - 8,
                X2 = position.X - 8,
                Y2 = position.Y + 8,
                Stroke = Brushes.Red,
                StrokeThickness = 3
            };

            canvas.Children.Add(line1);
            canvas.Children.Add(line2);
            Canvas.SetZIndex(line1, 101);
            Canvas.SetZIndex(line2, 101);

            // Анимация исчезновения
            Storyboard storyboard = new Storyboard();

            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(1000),
                BeginTime = TimeSpan.FromMilliseconds(500)
            };

            Storyboard.SetTarget(fadeOut, line1);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));

            DoubleAnimation fadeOut2 = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(1000),
                BeginTime = TimeSpan.FromMilliseconds(500)
            };

            Storyboard.SetTarget(fadeOut2, line2);
            Storyboard.SetTargetProperty(fadeOut2, new PropertyPath(UIElement.OpacityProperty));

            storyboard.Children.Add(fadeOut);
            storyboard.Children.Add(fadeOut2);

            storyboard.Completed += (s, e) =>
            {
                canvas.Children.Remove(line1);
                canvas.Children.Remove(line2);
            };

            storyboard.Begin();
        }

        // Подсветка узла
        public void HighlightNode(Point position, Color color, double radius = 20)
        {
            Ellipse highlight = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2,
                Fill = new SolidColorBrush(Color.FromArgb(100, color.R, color.G, color.B)),
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 2
            };

            canvas.Children.Add(highlight);
            Canvas.SetLeft(highlight, position.X - radius);
            Canvas.SetTop(highlight, position.Y - radius);
            Canvas.SetZIndex(highlight, 50);

            // Анимация пульсации и исчезновения
            Storyboard storyboard = new Storyboard();

            DoubleAnimation scaleAnim = new DoubleAnimation
            {
                From = 1.0,
                To = 1.3,
                Duration = TimeSpan.FromMilliseconds(400),
                AutoReverse = true
            };

            ScaleTransform scaleTransform = new ScaleTransform(1, 1, radius, radius);
            highlight.RenderTransform = scaleTransform;

            Storyboard.SetTarget(scaleAnim, scaleTransform);
            Storyboard.SetTargetProperty(scaleAnim, new PropertyPath(ScaleTransform.ScaleXProperty));

            DoubleAnimation scaleAnimY = new DoubleAnimation
            {
                From = 1.0,
                To = 1.3,
                Duration = TimeSpan.FromMilliseconds(400),
                AutoReverse = true
            };

            Storyboard.SetTarget(scaleAnimY, scaleTransform);
            Storyboard.SetTargetProperty(scaleAnimY, new PropertyPath(ScaleTransform.ScaleYProperty));

            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(400),
                BeginTime = TimeSpan.FromMilliseconds(400)
            };

            Storyboard.SetTarget(fadeOut, highlight);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath(UIElement.OpacityProperty));

            storyboard.Children.Add(scaleAnim);
            storyboard.Children.Add(scaleAnimY);
            storyboard.Children.Add(fadeOut);

            storyboard.Completed += (s, e) =>
            {
                canvas.Children.Remove(highlight);
            };

            storyboard.Begin();
        }

        // Очистка всех анимаций
        public void ClearAnimations()
        {
            for (int i = canvas.Children.Count - 1; i >= 0; i--)
            {
                if (canvas.Children[i] is Ellipse ellipse)
                {
                    int zIndex = Canvas.GetZIndex(ellipse);
                    if (zIndex >= 50)
                    {
                        canvas.Children.RemoveAt(i);
                    }
                }
            }
        }
    }

    public static class PacketColors
    {
        public static Color StationToServer = Color.FromRgb(33, 150, 243);     
        public static Color InQueue = Color.FromRgb(255, 193, 7);              
        public static Color ServerToStation = Color.FromRgb(76, 175, 80);     
        public static Color Dropped = Color.FromRgb(158, 158, 158);          
        public static Color Processing = Color.FromRgb(156, 39, 176);      
    }
}