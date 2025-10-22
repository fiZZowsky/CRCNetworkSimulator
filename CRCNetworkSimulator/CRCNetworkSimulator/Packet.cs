using System;
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
    
    public int CorruptAtNodeId { get; set; } = -1;

    [JsonIgnore]
    public string DataWithChecksum => (OriginalData ?? "") + (CrcChecksum ?? "");

    public Packet() { }
    
    public Packet(string data, int source, int dest, string polynomial, List<int> route, int corruptAtNodeId)
    {
        OriginalData = data;
        SourceId = source;
        DestinationId = dest;
        CrcChecksum = CrcService.Calculate(data, polynomial);
        Route = route;
        NextHopIndex = 1;
        CorruptAtNodeId = corruptAtNodeId;
    }
    
    public string CorruptData()
    {
        if (string.IsNullOrEmpty(OriginalData))
            return "Brak danych do uszkodzenia.";

        Random rand = new Random();
        int indexToCorrupt = rand.Next(0, OriginalData.Length);

        char[] dataChars = OriginalData.ToCharArray();
        char oldBit = dataChars[indexToCorrupt];
        
        char newBit = (oldBit == '0' ? '1' : '0');
        dataChars[indexToCorrupt] = newBit;
        
        OriginalData = new string(dataChars);

        return $"Bit na pozycji {indexToCorrupt} zmieniony z {oldBit} na {newBit}";
    }
}