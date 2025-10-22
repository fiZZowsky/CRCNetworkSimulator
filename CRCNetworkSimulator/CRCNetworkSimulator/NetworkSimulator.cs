public class NetworkSimulator
{
    public List<Computer> Computers { get; private set; }

    public NetworkSimulator()
    {
        Computers = new List<Computer>();

        for (int i = 0; i < 10; i++)
        {
            Computers.Add(new Computer(i, $"Komputer {i}"));
        }

        for (int i = 0; i < Computers.Count - 1; i++)
        {
            Computers[i].AddConnection(Computers[i + 1]);
        }

        Computers[Computers.Count - 1].AddConnection(Computers[0]);
    }
    
    public List<Computer> FindPath(int startId, int endId)
    {
        return new List<Computer>();
    }
    
    public void StartSimulation(int startId, int endId, string message, string polynomial)
    {
        List<Computer> path = FindPath(startId, endId);
        if (path == null || path.Count == 0)
        {
            return;
        }

        Packet packet = new Packet(message, startId, endId, polynomial);
        
        for (int i = 0; i < path.Count - 1; i++)
        {
            Computer currentHop = path[i];
            Computer nextHop = path[i + 1];
            
            bool isValid = nextHop.ReceivePacket(packet, polynomial);
            

            if (!isValid)
            {
                break;
            }
        }
    }
}