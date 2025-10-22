using System.Collections.Generic;
using System.Text.Json.Serialization;

public class Packet
{
    public string OriginalData { get; set; }
    public string CrcChecksum { get; set; }
    public int SourceId { get; set; }
    public int DestinationId { get; set; }
    public List<int>? Route { get; set; }
    public int NextHopIndex { get; set; }

    [JsonIgnore]
    public string DataWithChecksum => (OriginalData ?? "") + (CrcChecksum ?? "");
    
    public Packet() { }

    public Packet(string data, int source, int dest, string polynomial, List<int> route)
    {
        OriginalData = data;
        SourceId = source;
        DestinationId = dest;
        CrcChecksum = CrcService.Calculate(data, polynomial);
        Route = route;
        NextHopIndex = 1;
    }
}