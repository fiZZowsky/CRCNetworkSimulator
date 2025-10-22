using System.Net.Sockets;
using System.Windows;

public class Computer
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public Point Position { get; set; }
    public List<Computer> Neighbors { get; private set; }

    public Computer(int id, string name)
    {
        Id = id;
        Name = name;
        Neighbors = new List<Computer>();
        Position = new Point(0, 0);
    }
    
    public void AddConnection(Computer neighbor)
    {
        if (!Neighbors.Contains(neighbor))
        {
            Neighbors.Add(neighbor);
            if (!neighbor.Neighbors.Contains(this))
            {
                neighbor.Neighbors.Add(this);
            }
        }
    }

    public void RemoveConnection(Computer neighbor)
    {
        if (Neighbors.Contains(neighbor))
        {
            Neighbors.Remove(neighbor);
            if (neighbor.Neighbors.Contains(this))
            {
                neighbor.Neighbors.Remove(this);
            }
        }
    }
    
    public bool ReceivePacket(Packet packet, string polynomial)
    {
        bool isDataValid = CrcService.Validate(packet.DataWithChecksum, polynomial);

        if (isDataValid)
        {
            if (packet.DestinationId != this.Id)
            {
                // TODO: Znajdź następny krok na ścieżce i wyślij
            }
        }

        return isDataValid;
    }
}