using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CRCNetworkSimulator
{
    public partial class MainWindow : Window
    {
        private NetworkSimulator simulator;

        private bool isEditMode = false;
        private Computer selectedComputer = null;
        
        private List<Computer> activePath = null;
        private HashSet<Computer> pathNodes = new HashSet<Computer>();
        private HashSet<(int, int)> pathEdges = new HashSet<(int, int)>();

        public MainWindow()
        {
            InitializeComponent();
            simulator = new NetworkSimulator();
        }

        private void NetworkCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (simulator == null || simulator.Computers.Count == 0)
                return;

            SetupComputerPositions(NetworkCanvas.ActualWidth, NetworkCanvas.ActualHeight);
            DrawNetworkGraph();
        }

        private void SetupComputerPositions(double canvasWidth, double canvasHeight)
        {
            double centerX = canvasWidth / 2;
            double centerY = canvasHeight / 2;
            double radius = Math.Min(canvasWidth, canvasHeight) * 0.4;

            int count = simulator.Computers.Count;
            if (count == 0) return;

            double angleStep = 2 * Math.PI / count;

            for (int i = 0; i < count; i++)
            {
                double angle = (i * angleStep) - (Math.PI / 2);
                double x = centerX + radius * Math.Cos(angle);
                double y = centerY + radius * Math.Sin(angle);

                simulator.Computers[i].Position = new Point(x, y);
            }
        }
        
        private void BuildPathSets()
        {
            pathNodes.Clear();
            pathEdges.Clear();

            if (this.activePath == null || this.activePath.Count < 2)
                return;

            for (int i = 0; i < activePath.Count; i++)
            {
                pathNodes.Add(activePath[i]);
                
                if (i < activePath.Count - 1)
                {
                    var comp1 = activePath[i];
                    var comp2 = activePath[i + 1];
                    pathEdges.Add((comp1.Id, comp2.Id));
                    pathEdges.Add((comp2.Id, comp1.Id));
                }
            }
        }
        
        private void DrawNetworkGraph()
        {
            NetworkCanvas.Children.Clear();
            if (simulator.Computers == null) return;
            
            foreach (var comp in simulator.Computers)
            {
                foreach (var neighbor in comp.Neighbors)
                {
                    if (comp.Id < neighbor.Id)
                    {
                        Brush strokeBrush = Brushes.Gray;
                        double strokeThickness = 1;
                        
                        if (pathEdges.Contains((comp.Id, neighbor.Id)))
                        {
                            strokeBrush = Brushes.Red;
                            strokeThickness = 3;
                        }

                        Line line = new Line
                        {
                            X1 = comp.Position.X,
                            Y1 = comp.Position.Y,
                            X2 = neighbor.Position.X,
                            Y2 = neighbor.Position.Y,
                            Stroke = strokeBrush,
                            StrokeThickness = strokeThickness
                        };
                        NetworkCanvas.Children.Add(line);
                    }
                }
            }
            
            foreach (var comp in simulator.Computers)
            {
                Brush fillBrush = Brushes.LightBlue;

                if (pathNodes.Contains(comp))
                {
                    fillBrush = Brushes.Orange;
                }

                if (isEditMode && selectedComputer == comp)
                {
                    fillBrush = Brushes.LawnGreen;
                }

                Ellipse ellipse = new Ellipse
                {
                    Width = 20,
                    Height = 20,
                    Fill = fillBrush,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2,
                    Tag = comp
                };
                ellipse.MouseLeftButtonDown += Computer_MouseLeftButtonDown;

                Canvas.SetLeft(ellipse, comp.Position.X - 10);
                Canvas.SetTop(ellipse, comp.Position.Y - 10);
                NetworkCanvas.Children.Add(ellipse);

                TextBlock text = new TextBlock
                {
                    Text = comp.Name,
                    FontSize = 10,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(text, comp.Position.X - 15);
                Canvas.SetTop(text, comp.Position.Y + 10);
                NetworkCanvas.Children.Add(text);
            }
        }

        private void chkEditMode_Changed(object sender, RoutedEventArgs e)
        {
            isEditMode = chkEditMode.IsChecked ?? false;

            if (!isEditMode)
            {
                selectedComputer = null;
            }
            
            this.activePath = null;
            BuildPathSets();

            txtSource.IsEnabled = !isEditMode;
            txtDestination.IsEnabled = !isEditMode;
            txtMessage.IsEnabled = !isEditMode;
            txtPolynomial.IsEnabled = !isEditMode;
            btnSend.IsEnabled = !isEditMode;

            DrawNetworkGraph();
        }

        private void Computer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!isEditMode) return;
            
            this.activePath = null;
            BuildPathSets();

            Ellipse clickedEllipse = sender as Ellipse;
            Computer clickedComputer = clickedEllipse?.Tag as Computer;
            if (clickedComputer == null) return;

            if (selectedComputer == null)
            {
                selectedComputer = clickedComputer;
            }
            else
            {
                if (selectedComputer == clickedComputer)
                {
                    selectedComputer = null;
                }
                else
                {
                    if (selectedComputer.Neighbors.Contains(clickedComputer))
                    {
                        selectedComputer.RemoveConnection(clickedComputer);
                        logBox.Items.Add($"Edycja: Usunięto połączenie {selectedComputer.Name} <-> {clickedComputer.Name}");
                    }
                    else
                    {
                        selectedComputer.AddConnection(clickedComputer);
                        logBox.Items.Add($"Edycja: Dodano połączenie {selectedComputer.Name} <-> {clickedComputer.Name}");
                    }
                    selectedComputer = null;
                }
            }
            DrawNetworkGraph();
        }
        
        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            logBox.Items.Clear();
            
            this.activePath = null;
            BuildPathSets();

            Action<string> logger = (logMessage) =>
            {
                logBox.Items.Add(logMessage);
                logBox.ScrollIntoView(logBox.Items[logBox.Items.Count - 1]);
            };
            
            if (!int.TryParse(txtSource.Text, out int sourceId) ||
                !int.TryParse(txtDestination.Text, out int destId))
            {
                logger("BŁĄD: ID źródła i celu muszą być poprawnymi liczbami.");
                return;
            }
            if (simulator.Computers.All(c => c.Id != sourceId) ||
                simulator.Computers.All(c => c.Id != destId))
            {
                logger("BŁĄD: Nie znaleziono komputera o podanym ID.");
                return;
            }
            string message = txtMessage.Text;
            string polynomial = txtPolynomial.Text;
            if (!CrcService.IsValidBitString(message) || message.Length == 0)
            {
                logger("BŁĄD: Wiadomość może zawierać tylko '0' lub '1' i nie być pusta.");
                return;
            }
            if (!CrcService.IsValidBitString(polynomial) || polynomial.Length <= 1 || polynomial[0] == '0')
            {
                logger("BŁĄD: Wielomian musi zawierać '0' lub '1', mieć min. 2 bity i zaczynać się od '1'.");
                return;
            }

            try
            {
                this.activePath = simulator.FindPath(sourceId, destId);
                
                BuildPathSets();
                
                simulator.StartSimulation(sourceId, destId, message, polynomial, logger);
                
                DrawNetworkGraph();
            }
            catch (Exception ex)
            {
                logger($"KRYTYCZNY BŁĄD SYMULACJI: {ex.Message}");
            }
        }
        
        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            logBox.Items.Clear();
            
            this.activePath = null;
            BuildPathSets();
            DrawNetworkGraph();
        }
    }
}