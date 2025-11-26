using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

public class Computer
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public Point Position { get; set; }
    public List<Computer> Neighbors { get; private set; }
    public int Port { get; private set; }
    private NetworkSimulator _simulator;
    private Action<string> _logger;

    public Computer(int id, string name, int port, NetworkSimulator simulator, Action<string> logger)
    {
        Id = id; 
        Name = name; 
        Port = port; 
        _simulator = simulator; 
        _logger = logger; 
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

    public async Task StartServerAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Production }); 
        builder.WebHost.ConfigureKestrel(serverOptions => { serverOptions.ListenLocalhost(Port); }); 
        var app = builder.Build(); app.MapPost("/receivepacket", async (Packet packet, HttpContext context) 
            => { await HandlePacketAsync(packet, _simulator.CurrentPolynomial); return Results.Ok(); }); 
        _logger($"[K{Id}] Serwer uruchomiony na porcie {Port}"); await app.RunAsync();
    }

    private async Task HandlePacketAsync(Packet packet, string polynomial)
    {
        _logger($"[K{Id}] Odebrano pakiet (cel: K{packet.DestinationId}).");

        bool errorWasIntroduced = false;
        if (this.Id == packet.CorruptAtNodeId)
        {
            string corruptionLog = packet.CorruptData();
            _logger($"!!! [K{Id}] SYMULOWANIE BŁĘDU: {corruptionLog}");
            _logger($"!!! [K{Id}] Dane są teraz uszkodzone: {packet.DataWithChecksum}");

            packet.CorruptAtNodeId = -1;
            errorWasIntroduced = true;
        }

        bool isValid = false;
        try
        {
            isValid = CrcService.Validate(packet.DataWithChecksum, polynomial);
        }
        catch (Exception ex)
        {
            _logger($"[K{Id}] BŁĄD KRYTYCZNY walidacji: {ex.Message}");
            return;
        }

        if (!isValid)
        {
            if (errorWasIntroduced)
            {
                _logger($"-> [K{Id}] Sukces! Błąd CRC został poprawnie wykryty.");
            }
            else
            {
                _logger($"-> [K{Id}] Niespodziewany BŁĄD CRC! Pakiet odrzucony.");
            }
            return;
        }

        _logger($"[K{Id}] Walidacja CRC: OK.");

        if (errorWasIntroduced)
        {
            _logger($"!!! [K{Id}] BŁĄD KRYTYCZNY: Zasymulowano błąd, ale CRC go NIE wykryło!");
        }

        if (this.Id == packet.DestinationId)
        {
            _logger($"[K{Id}] Jestem celem! Odebrano wiadomość: {packet.OriginalData}");
            return;
        }

        if (packet.Route == null || packet.NextHopIndex >= packet.Route.Count)
        {
            _logger($"[K{Id}] Błąd trasy w pakiecie. Odrzucam.");
            return;
        }

        int nextHopId = packet.Route[packet.NextHopIndex];
        var nextHopComputer = _simulator.Computers.FirstOrDefault(c => c.Id == nextHopId);

        if (nextHopComputer == null)
        {
            _logger($"[K{Id}] Błąd: nie znam następnego komputera na trasie (ID: {nextHopId}).");
            return;
        }

        packet.NextHopIndex++;

        try
        {
            _logger($"[K{Id}] Przekazuję pakiet dalej do K{nextHopComputer.Id} (Port {nextHopComputer.Port})...");
            using (var client = new HttpClient())
            {
                await client.PostAsJsonAsync($"http://localhost:{nextHopComputer.Port}/receivepacket", packet);
            }
        }
        catch (Exception ex)
        {
            _logger($"[K{Id}] BŁĄD WYSYŁANIA do K{nextHopComputer.Id}: {ex.Message}. Transmisja przerwana.");
        }
    }

    public bool ReceivePacket(Packet packet, string polynomial, Action<string> logger)
    {
        return CrcService.Validate(packet.DataWithChecksum, polynomial);
    }
}