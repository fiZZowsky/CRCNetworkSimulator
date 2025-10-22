using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Http;
using System.Net.Http.Json;

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
        
        private readonly Action<string> _logger;

        public MainWindow()
        {
            InitializeComponent();
            
            _logger = (message) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    logBox.Items.Add(message);
                    if (logBox.Items.Count > 200)
                    {
                        logBox.Items.RemoveAt(0);
                    }
                    logBox.ScrollIntoView(logBox.Items[logBox.Items.Count - 1]);
                });
            };
            
            simulator = new NetworkSimulator(_logger);
            
            simulator.StartAllComputerServers();
        }

        #region Rysowanie i Edycja Grafu
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
                        _logger($"Edycja: Usunięto połączenie {selectedComputer.Name} <-> {clickedComputer.Name}");
                    }
                    else
                    {
                        selectedComputer.AddConnection(clickedComputer);
                        _logger($"Edycja: Dodano połączenie {selectedComputer.Name} <-> {clickedComputer.Name}");
                    }
                    selectedComputer = null;
                }
            }
            DrawNetworkGraph();
        }
        #endregion

        #region Metody Konwersji
        private string ConvertTextToBitString(string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            var sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                sb.Append(Convert.ToString(b, 2).PadLeft(8, '0'));
            }
            return sb.ToString();
        }

        private string ConvertPolyTextToBitString(string polyText)
        {
            string input = polyText.ToLower().Replace(" ", "").Replace("-", "+");
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Wielomian nie może być pusty.");

            string[] terms = input.Split('+');
            if (terms.Length == 0)
                throw new ArgumentException("Nieprawidłowy format wielomianu.");

            int maxDegree = 0;
            var powers = new List<int>();

            foreach (string term in terms)
            {
                if (term == "1")
                {
                    powers.Add(0);
                }
                else if (term == "x")
                {
                    powers.Add(1);
                    maxDegree = Math.Max(maxDegree, 1);
                }
                else if (term.StartsWith("x^"))
                {
                    if (int.TryParse(term.Substring(2), out int power))
                    {
                        powers.Add(power);
                        maxDegree = Math.Max(maxDegree, power);
                    }
                    else
                    {
                        throw new FormatException($"Nieprawidłowy składnik wielomianu: '{term}'");
                    }
                }
                else
                {
                    throw new FormatException($"Nieznany składnik wielomianu: '{term}'. Użyj formatu 'x^3+x+1'.");
                }
            }

            if (maxDegree == 0 && powers.Count > 0)
            {
                if (powers.All(p => p == 0)) return "1";
            }

            if (maxDegree < 1)
                throw new ArgumentException("Stopień wielomianu musi być większy lub równy 1.");

            char[] bits = new char[maxDegree + 1];
            for (int i = 0; i < bits.Length; i++)
                bits[i] = '0';

            foreach (int power in powers)
            {
                bits[power] = '1';
            }

            return new string(bits.Reverse().ToArray());
        }
        #endregion

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            logBox.Items.Clear();
            this.activePath = null;
            BuildPathSets();
            
            if (!int.TryParse(txtSource.Text, out int sourceId) || !int.TryParse(txtDestination.Text, out int destId))
            {
                _logger("BŁĄD: ID źródła i celu muszą być poprawnymi liczbami."); return;
            }
            if (simulator.Computers.All(c => c.Id != sourceId) || simulator.Computers.All(c => c.Id != destId))
            {
                _logger("BŁĄD: Nie znaleziono komputera o podanym ID."); return;
            }
            
            string messageText = txtMessage.Text;
            string polyText = txtPolynomial.Text;
            if (string.IsNullOrEmpty(messageText))
            {
                _logger("BŁĄD: Wiadomość nie może być pusta."); return;
            }

            string polynomialBits;
            string messageBits;
            try
            {
                polynomialBits = ConvertPolyTextToBitString(polyText);
                messageBits = ConvertTextToBitString(messageText);
                if (polynomialBits.Length <= 1 || polynomialBits[0] == '0')
                {
                    _logger($"BŁĄD: Wielomian {polyText} (binarnie {polynomialBits}) jest nieprawidłowy."); return;
                }
            }
            catch (Exception ex)
            {
                _logger($"BŁĄD PARSOWANIA: {ex.Message}"); return;
            }
            _logger($"Konwersja: Wiadomość '{messageText}' -> (bity) {messageBits}");
            _logger($"Konwersja: Wielomian '{polyText}' -> (bity) {polynomialBits}");
            
            try
            {
                this.activePath = simulator.FindPath(sourceId, destId);
                BuildPathSets();
                DrawNetworkGraph();

                await simulator.StartSimulationAsync(sourceId, destId, messageBits, polynomialBits);
            }
            catch (Exception ex)
            {
                _logger($"KRYTYCZNY BŁĄD SYMULACJI: {ex.Message}");
            }
        }

        private void btnClearLog_Click(object sender, RoutedEventArgs e)
        {
            _logger("Wyczyszczono logi.");
            logBox.Items.Clear();
            this.activePath = null;
            BuildPathSets();
            DrawNetworkGraph();
        }
    }
}