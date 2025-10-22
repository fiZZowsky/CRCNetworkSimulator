using System;
using System.Collections.Generic;
using System.Linq;

public class NetworkSimulator
{
    public List<Computer> Computers { get; private set; }

    public NetworkSimulator()
    {
        Computers = new List<Computer>();

        for (int i = 0; i < 10; i++)
        {
            Computers.Add(new Computer(i, $"K{i}"));
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
    
    public List<Computer> FindPath(int startId, int endId)
    {
        var startNode = Computers.FirstOrDefault(c => c.Id == startId);
        var endNode = Computers.FirstOrDefault(c => c.Id == endId);

        if (startNode == null || endNode == null) return null;
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

        return null;
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
    
    public void StartSimulation(int startId, int endId, string message, string polynomial, Action<string> logger)
    {
        logger("Rozpoczynanie symulacji...");
        
        List<Computer> path = FindPath(startId, endId);
        if (path == null || path.Count == 0)
        {
            logger($"BŁĄD: Nie znaleziono ścieżki z K{startId} do K{endId}.");
            return;
        }
        
        logger($"Znaleziono ścieżkę: {string.Join(" -> ", path.Select(c => c.Name))}");
        
        Packet packet;
        try
        {
            packet = new Packet(message, startId, endId, polynomial);
            logger($"Pakiet utworzony. Dane: {message}, Wielomian: {polynomial}");
            logger($"Obliczone CRC: {packet.CrcChecksum}");
            logger($"Dane do wysłania: {packet.DataWithChecksum}");
        }
        catch (Exception ex)
        {
            logger($"BŁĄD tworzenia pakietu: {ex.Message}");
            return;
        }
        
        for (int i = 0; i < path.Count - 1; i++)
        {
            Computer currentHop = path[i];
            Computer nextHop = path[i + 1];

            logger($"Wysyłanie z {currentHop.Name} do {nextHop.Name}...");

            bool isValid = nextHop.ReceivePacket(packet, polynomial, logger);

            if (!isValid)
            {
                logger($"BŁĄD CRC! Pakiet odrzucony w {nextHop.Name}. Transmisja przerwana.");
                return;
            }
        }

        logger($"Transmisja zakończona. Pakiet pomyślnie dotarł do {path.Last().Name}.");
    }
}