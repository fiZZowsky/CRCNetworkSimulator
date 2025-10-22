using System.Collections.Generic;
using System.Windows;
using System;

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
    
    public bool ReceivePacket(Packet packet, string polynomial, Action<string> logger)
    {
        logger($"  -> {this.Name} odebrał pakiet. Sprawdzanie CRC...");

        bool isDataValid;
        try
        {
            isDataValid = CrcService.Validate(packet.DataWithChecksum, polynomial);
        }
        catch (Exception ex)
        {
            logger($"  -> BŁĄD KRYTYCZNY walidacji w {this.Name}: {ex.Message}");
            return false;
        }


        if (isDataValid)
        {
            logger($"  -> Walidacja w {this.Name}: OK.");
            
            if (packet.DestinationId == this.Id)
            {
                logger($"  -> {this.Name} jest celem. Odbieram dane: {packet.OriginalData}");
            }
            else
            {
                logger($"  -> {this.Name} nie jest celem. Oznaczam jako sprawdzony.");
            }
        }
        else
        {
            logger($"  -> Walidacja w {this.Name}: BŁĄD! Dane uszkodzone.");
        }

        return isDataValid;
    }
}