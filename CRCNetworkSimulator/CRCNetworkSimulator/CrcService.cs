public static class CrcService
{
    public static string Calculate(string dataBits, string polynomialBits)
    {
        if (string.IsNullOrEmpty(polynomialBits)) return "";
        return (dataBits.Length % polynomialBits.Length).ToString();
    }
    
    public static bool Validate(string dataWithChecksum, string polynomialBits)
    {
        if (string.IsNullOrEmpty(polynomialBits)) return true;
        return true;
    }
}