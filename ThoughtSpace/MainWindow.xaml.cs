using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ThoughtSpace
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Title = "ThoughtSpace";
        }

        private Brush currentBrush = new SolidColorBrush(Microsoft.UI.Colors.Black);
        private bool isDrawing = false;
        private bool isErasing = false;
        private Point startPoint;
        private List<UIElement> elementsToRemove = new List<UIElement>();
        private int SelectedIndex = 0;
        private Ellipse currentEllipse;
        private Rectangle currentRectangle;
        private Polygon currentPolygon;

        private enum DrawingMode
        {
            Circle,
            Rectangle,
            Triangle,
        }

        private DrawingMode currentDrawingMode = DrawingMode.Rectangle;

        public List<double> BrushThickness { get; } = new List<double>()
        {
            8, 12, 18
        };

        // Control input handling
        private async void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType.Equals(PointerDeviceType.Mouse) || e.Pointer.PointerDeviceType.Equals(PointerDeviceType.Pen))
            {
                editCanvas(sender, e);
            }
        }

        private async void editCanvas(object sender, PointerRoutedEventArgs e)
        {
            if (colorPickerButton.IsChecked == true)
            {
                var pointerPosition = e.GetCurrentPoint(canvas);
                int x = (int)pointerPosition.Position.X;
                int y = (int)pointerPosition.Position.Y;

                RenderTargetBitmap renderBitmap = new RenderTargetBitmap();
                await renderBitmap.RenderAsync(canvas);

                var pixelBuffer = await renderBitmap.GetPixelsAsync();
                var pixelData = pixelBuffer.ToArray();

                int pixelIndex = (y * renderBitmap.PixelWidth + x) * 4;

                byte[] pixelColor = new byte[4];
                Array.Copy(pixelData, pixelIndex, pixelColor, 0, 4);

                Color color = Color.FromArgb(pixelColor[3], pixelColor[2], pixelColor[1], pixelColor[0]);

                CurrentColor.Background = currentBrush = new SolidColorBrush(color);
            }
            else
            {
                isDrawing = true;
                startPoint = e.GetCurrentPoint(canvas).Position;

                if (shapesButton.IsChecked == true)
                {
                    switch (currentDrawingMode)
                    {
                        case DrawingMode.Circle:
                            currentEllipse = new Ellipse { Width = 0, Height = 0, Stroke = currentBrush, StrokeThickness = BrushThickness[SelectedIndex] };
                            canvas.Children.Add(currentEllipse);
                            Canvas.SetLeft(currentEllipse, startPoint.X);
                            Canvas.SetTop(currentEllipse, startPoint.Y);
                            break;
                        case DrawingMode.Rectangle:
                            currentRectangle = new Rectangle { Width = 0, Height = 0, Stroke = currentBrush, StrokeThickness = BrushThickness[SelectedIndex] };
                            canvas.Children.Add(currentRectangle);
                            Canvas.SetLeft(currentRectangle, startPoint.X);
                            Canvas.SetTop(currentRectangle, startPoint.Y);
                            break;
                        case DrawingMode.Triangle:
                            currentPolygon = new Polygon { Stroke = currentBrush, StrokeThickness = BrushThickness[SelectedIndex] };
                            currentPolygon.Points.Add(startPoint);
                            currentPolygon.Points.Add(startPoint);
                            currentPolygon.Points.Add(startPoint);
                            canvas.Children.Add(currentRectangle);
                            Canvas.SetLeft(currentRectangle, startPoint.X);
                            Canvas.SetTop(currentRectangle, startPoint.Y);
                            break;
                    }
                }
            }
        }

        private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isDrawing)
            {
                if (shapesButton.IsChecked == true)
                {
                    Point currentPoint = e.GetCurrentPoint(canvas).Position;
                    switch (currentDrawingMode)
                    {
                        case DrawingMode.Circle:
                            double newWidth = Math.Abs(currentPoint.X - startPoint.X) * 2;
                            double newHeight = Math.Abs(currentPoint.Y - startPoint.Y) * 2;
                            currentEllipse.Width = newWidth;
                            currentEllipse.Height = newHeight;
                            double left = Math.Min(startPoint.X, currentPoint.X);
                            double top = Math.Min(startPoint.Y, currentPoint.Y);
                            Canvas.SetLeft(currentEllipse, left);
                            Canvas.SetTop(currentEllipse, top);
                            break;
                        case DrawingMode.Rectangle:
                            newWidth = Math.Abs(currentPoint.X - startPoint.X);
                            newHeight = Math.Abs(currentPoint.Y - startPoint.Y);
                            currentRectangle.Width = newWidth;
                            currentRectangle.Height = newHeight;
                            left = Math.Min(startPoint.X, currentPoint.X);
                            top = Math.Min(startPoint.Y, startPoint.Y);
                            Canvas.SetLeft(currentRectangle, left);
                            Canvas.SetTop(currentRectangle, top);
                            break;
                        case DrawingMode.Triangle:
                            double centerX = (startPoint.X + currentPoint.X) / 2;
                            double centerY = (startPoint.Y + currentPoint.Y) / 2;
                            double sideLength = Math.Min(Math.Abs(currentPoint.X - startPoint.X), Math.Abs(currentPoint.Y + startPoint.Y));
                            Point vertex1 = new Point(centerX, centerY - (sideLength / 2));
                            Point vertex2 = new Point(centerX - (sideLength / 2), centerY + (sideLength / 2));
                            Point vertex3 = new Point(centerX + (sideLength / 2), centerY + (sideLength / 2));
                            currentPolygon.Points.Clear();
                            currentPolygon.Points.Add(vertex1);
                            currentPolygon.Points.Add(vertex2);
                            currentPolygon.Points.Add(vertex3);
                            currentPolygon.Points.Add(vertex1);
                            break;

                    }

                }
                else if (brushButton.IsChecked == true)
                {
                    Line line = new Line
                    {
                        X1 = startPoint.X,
                        Y1 = startPoint.Y,
                        X2 = e.GetCurrentPoint(canvas).Position.X,
                        Y2 = e.GetCurrentPoint(canvas).Position.Y,
                        Stroke = currentBrush,
                        StrokeThickness = BrushThickness[SelectedIndex],
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeLineJoin = PenLineJoin.Round,
                    };

                    Line lineBlur = new Line
                    {
                        X1 = startPoint.X,
                        Y1 = startPoint.Y,
                        X2 = e.GetCurrentPoint(canvas).Position.X,
                        Y2 = e.GetCurrentPoint(canvas).Position.Y,
                        Stroke = currentBrush,
                        StrokeThickness = BrushThickness[SelectedIndex] + 10,
                        StrokeEndLineCap = PenLineCap.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeLineJoin = PenLineJoin.Round,
                        Opacity = 0.2,
                    };

                    canvas.Children.Add(lineBlur);
                    canvas.Children.Add(line);
                    startPoint = e.GetCurrentPoint(canvas).Position;
                }
                else if (eraserButton.IsChecked == true)
                {

                }
                else
                {
                    Line line = new Line
                    {
                        X1 = startPoint.X,
                        Y1 = startPoint.Y,
                        X2 = e.GetCurrentPoint(canvas).Position.X,
                        Y2 = e.GetCurrentPoint(canvas).Position.Y,
                        Stroke = currentBrush,
                        StrokeThickness = 3,
                        StrokeLineJoin = PenLineJoin.Round,
                    };
                    line.PointerEntered += Line_PointerEntered;
                    canvas.Children.Add(line);
                    startPoint = e.GetCurrentPoint(canvas).Position;

                }

            }
        }

        private void Line_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (isErasing)
            {
                Line line = sender as Line;
                canvas.Children.Remove(line);
            }
        }

        private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            isDrawing = false;
            isErasing = false;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e) 
        {
            canvas.Children.Clear();
        }

        private void ColorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs e)
        {
            currentBrush = new SolidColorBrush(e.NewColor);
            CurrentColor.Background = new SolidColorBrush(e.NewColor);
        }

        private void CoboBoxBrushThickness_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedIndex = CoboBoxBrushThickness.SelectedIndex;
        }

        private void PencilButton_Click(Object sender, RoutedEventArgs e)
        {
            ToggleButton clickedButton = sender as ToggleButton;

            brushButton.IsChecked = false;
            eraserButton.IsChecked = false;
            colorPickerButton.IsChecked = false;
            shapesButton.IsChecked = false;
            pencilButton.IsChecked = true;
        }

        private void BrushButton_Click(Object sender, RoutedEventArgs e)
        {
            ToggleButton clickedButton = sender as ToggleButton;

            brushButton.IsChecked = true;
            eraserButton.IsChecked = false;
            colorPickerButton.IsChecked = false;
            shapesButton.IsChecked = false;
            pencilButton.IsChecked = false;
        }

        private void EraserButton_Click(object sender, RoutedEventArgs e)
        {
            isErasing = !isErasing;

            brushButton.IsChecked = false;
            pencilButton.IsChecked = false;
            colorPickerButton.IsChecked = false;
            shapesButton.IsChecked = false;
        }

        private void CoboBoxShapes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        { 
            switch (CoboBoxShapes.SelectedIndex)
            {
                case 0:
                    currentDrawingMode = DrawingMode.Circle;
                    break;
                case 1:
                    currentDrawingMode = DrawingMode.Rectangle;
                    break;
                case 2:
                    currentDrawingMode = DrawingMode.Triangle;
                    break;
            }       
        }

        private void PickerButton_Click(Object sender, RoutedEventArgs e)
        {
            brushButton.IsChecked = false;
            pencilButton.IsChecked = false;
            colorPickerButton.IsChecked = true;
            shapesButton.IsChecked = false;
            eraserButton.IsChecked = false;
        }

        private void ShapesButton_Click(object sender, RoutedEventArgs e)
        {
            brushButton.IsChecked = false;
            pencilButton.IsChecked = false;
            colorPickerButton.IsChecked = false;
            shapesButton.IsChecked = true;
            eraserButton.IsChecked = false;
        } 
    }
}
