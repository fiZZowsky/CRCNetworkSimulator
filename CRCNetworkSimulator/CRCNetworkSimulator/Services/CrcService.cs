public static class CrcService
{
    public static bool IsValidBitString(string bits)
    {
        if (string.IsNullOrEmpty(bits)) return false;
        return bits.All(c => c == '0' || c == '1');
    }
    
    private static char Xor(char a, char b)
    {
        return (a == b) ? '0' : '1';
    }
    
    private static string PerformPolynomialDivision(string data, string polynomial)
    {
        int polyLen = polynomial.Length;
        char[] dataChars = data.ToCharArray();
        int dataLen = dataChars.Length;
        
        for (int i = 0; i <= dataLen - polyLen; i++)
        {
            if (dataChars[i] == '1')
            {
                for (int j = 0; j < polyLen; j++)
                {
                    dataChars[i + j] = Xor(dataChars[i + j], polynomial[j]);
                }
            }
        }
        
        int k = polyLen - 1;
        
        if (dataLen - k < 0)
        {
            throw new InvalidOperationException("Błąd wewnętrzny CRC: dane krótsze niż wielomian.");
        }
        
        return new string(dataChars, dataLen - k, k);
    }
    
    public static string Calculate(string dataBits, string polynomialBits)
    {
        if (!IsValidBitString(polynomialBits) || polynomialBits.Length <= 1 || polynomialBits[0] == '0')
            throw new ArgumentException("Wielomian jest nieprawidłowy (musi zaczynać się od '1' i mieć > 1 bit).");
        if (!IsValidBitString(dataBits))
            throw new ArgumentException("Dane zawierają nieprawidłowe znaki.");

        int k = polynomialBits.Length - 1;
        
        string paddedData = dataBits + new string('0', k);

        return PerformPolynomialDivision(paddedData, polynomialBits);
    }
    
    public static bool Validate(string dataWithChecksum, string polynomialBits)
    {
        if (!IsValidBitString(polynomialBits) || polynomialBits.Length <= 1 || polynomialBits[0] == '0')
            throw new ArgumentException("Wielomian jest nieprawidłowy.");
        if (!IsValidBitString(dataWithChecksum))
            throw new ArgumentException("Dane do walidacji zawierają nieprawidłowe znaki.");
        
        string remainder = PerformPolynomialDivision(dataWithChecksum, polynomialBits);
        
        return remainder.All(c => c == '0');
    }
}