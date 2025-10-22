using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

public class NetworkSimulator
{
    public List<Computer> Computers { get; private set; }
    public string CurrentPolynomial { get; private set; } = "1011";

    private Action<string> _logger;

    public NetworkSimulator(Action<string> logger)
    {
        _logger = logger;
        Computers = new List<Computer>();
        int basePort = 5000;

        for (int i = 0; i < 10; i++)
        {
            Computers.Add(new Computer(i, $"K{i}", basePort + i, this, _logger));
        }
        
        if (Computers.Count > 0)
        {
            for (int i = 0; i < Computers.Count - 1; i++)
            {
                Computers[i].AddConnection(Computers[i + 1]);
            }
            Computers[Computers.Count - 1].AddConnection(Computers[0]);
        }
    }

    public void StartAllComputerServers()
    {
        _logger("Uruchamianie serwerów dla 10 komputerów...");
        foreach (var comp in Computers)
        {
            Task.Run(async () => await comp.StartServerAsync());
        }
    }

    #region Wyszukiwanie ścieżki (BFS)
    public List<Computer> FindPath(int startId, int endId)
    {
        var startNode = Computers.FirstOrDefault(c => c.Id == startId);
        var endNode = Computers.FirstOrDefault(c => c.Id == endId);

        if (startNode == null || endNode == null) return new List<Computer>();
        if (startNode == endNode) return new List<Computer> { startNode };

        var queue = new Queue<Computer>();
        var visited = new HashSet<Computer>();
        var parentMap = new Dictionary<Computer, Computer>();

        queue.Enqueue(startNode);
        visited.Add(startNode);
        parentMap[startNode] = null;

        while (queue.Count > 0)
        {
            var currentNode = queue.Dequeue();

            if (currentNode == endNode)
            {
                return ReconstructPath(parentMap, endNode);
            }

            foreach (var neighbor in currentNode.Neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    parentMap[neighbor] = currentNode;
                    queue.Enqueue(neighbor);
                }
            }
        }

        return new List<Computer>();
    }

    private List<Computer> ReconstructPath(Dictionary<Computer, Computer> parentMap, Computer endNode)
    {
        var path = new List<Computer>();
        var currentNode = endNode;
        while (currentNode != null)
        {
            path.Add(currentNode);
            currentNode = parentMap[currentNode];
        }
        path.Reverse();
        return path;
    }
    #endregion
    
    public async Task StartSimulationAsync(int startId, int endId, string message, string polynomial)
    {
        _logger("Rozpoczynanie symulacji (tryb rozproszony)...");
        this.CurrentPolynomial = polynomial;

        List<Computer> path = FindPath(startId, endId);
        if (path == null || path.Count < 2)
        {
            _logger($"BŁĄD: Nie znaleziono ścieżki z K{startId} do K{endId}.");
            return;
        }

        _logger($"Znaleziono ścieżkę: {string.Join(" -> ", path.Select(c => c.Name))}");
        List<int> route = path.Select(c => c.Id).ToList();

        Packet packet;
        try
        {
            packet = new Packet(message, startId, endId, polynomial, route);
            _logger($"Pakiet utworzony. CRC: {packet.CrcChecksum}.");
        }
        catch (Exception ex)
        {
            _logger($"BŁĄD tworzenia pakietu: {ex.Message}");
            return;
        }

        Computer firstHop = path[1];
        int firstHopPort = firstHop.Port;

        try
        {
            _logger($"Inicjowanie wysyłki z K{startId} do K{firstHop.Id} (Port: {firstHopPort})...");
            using (var client = new HttpClient())
            {
                await client.PostAsJsonAsync($"http://localhost:{firstHopPort}/receivepacket", packet);
            }
            _logger("Wysyłka zainicjowana. Śledź logi serwerów...");
        }
        catch (Exception ex)
        {
            _logger($"BŁĄD KRYTYCZNY: Nie można połączyć się z K{firstHop.Id}: {ex.Message}");
        }
    }
}