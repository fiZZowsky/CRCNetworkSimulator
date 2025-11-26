using CRCNetworkSimulator.Enums;
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
    public ErrorType SelectedErrorType { get; set; } = ErrorType.SingleBit;

    [JsonIgnore]
    public string DataWithChecksum => (OriginalData ?? "") + (CrcChecksum ?? "");

    public Packet() { }
    
    public Packet(string data, int source, int dest, string polynomial, List<int> route, int corruptAtNodeId, ErrorType errorType)
    {
        OriginalData = data;
        SourceId = source;
        DestinationId = dest;
        CrcChecksum = CrcService.Calculate(data, polynomial);
        Route = route;
        NextHopIndex = 1;
        CorruptAtNodeId = corruptAtNodeId;
        SelectedErrorType = errorType;
    }
    
    public string CorruptData()
    {
        if (string.IsNullOrEmpty(OriginalData))
            return "Brak danych do uszkodzenia.";

        Random rand = new Random();
        char[] dataChars = OriginalData.ToCharArray();
        string logMessage = "";

        switch (SelectedErrorType)
        {
            case ErrorType.SingleBit:
                {
                    int index = rand.Next(0, dataChars.Length);
                    FlipBit(dataChars, index);
                    logMessage = $"Typ: Pojedynczy. Zmieniono bit na pozycji {index}.";
                }
                break;

            case ErrorType.TwoIsolatedBits:
                {
                    if (dataChars.Length < 2) return "Dane zbyt krótkie na błąd 2 bitów (zrobiono 1).";

                    int idx1 = rand.Next(0, dataChars.Length);
                    int idx2 = rand.Next(0, dataChars.Length);

                    while (idx1 == idx2) idx2 = rand.Next(0, dataChars.Length);

                    FlipBit(dataChars, idx1);
                    FlipBit(dataChars, idx2);
                    logMessage = $"Typ: Dwa bity. Zmieniono bity na pozycjach {idx1} oraz {idx2}.";
                }
                break;

            case ErrorType.Burst:
                {
                    int burstLength = 3;
                    if (dataChars.Length < burstLength) burstLength = dataChars.Length;

                    int startIdx = rand.Next(0, dataChars.Length - burstLength + 1);

                    for (int i = 0; i < burstLength; i++)
                    {
                        FlipBit(dataChars, startIdx + i);
                    }
                    logMessage = $"Typ: Seryjny (Burst). Zmieniono {burstLength} bitów od pozycji {startIdx}.";
                }
                break;
        }

        OriginalData = new string(dataChars);
        return logMessage;
    }

    private void FlipBit(char[] chars, int index)
    {
        chars[index] = (chars[index] == '0' ? '1' : '0');
    }
}