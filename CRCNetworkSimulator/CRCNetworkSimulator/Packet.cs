public class Packet
{
    public string OriginalData { get; private set; }
    public string CrcChecksum { get; private set; }
    public int SourceId { get; private set; }
    public int DestinationId { get; private set; }
    
    public string DataWithChecksum => OriginalData + CrcChecksum;

    public Packet(string data, int source, int dest, string polynomial)
    {
        OriginalData = data;
        SourceId = source;
        DestinationId = dest;
        CrcChecksum = CrcService.Calculate(data, polynomial);
    }
}