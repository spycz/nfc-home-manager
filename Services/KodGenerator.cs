using System.Security.Cryptography;

namespace NfcHomeManager.Services;

// Kratky kod pro URL verejne stranky polozky (/p/{kod}), ktera se zapise
// jako NDEF zaznam na NTAG215 stitek. Abeceda vynechava znaky snadno
// zamenitelne pri opisovani (0/O, 1/I).
public static class KodGenerator
{
    private const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";

    public static string Generate(int length = 7)
    {
        var bytes = RandomNumberGenerator.GetBytes(length);
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return new string(chars);
    }
}
