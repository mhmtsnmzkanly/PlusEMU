using System.Text;
using Plus.Communication.Encryption.Crypto.RSA;
using Plus.Communication.Encryption.KeyExchange;
using Plus.Communication.Encryption.Keys;
using Plus.Utilities;

namespace Plus.Communication.Encryption;

public static class HabboEncryptionV2
{
    private static RsaKey? _rsa;
    private static DiffieHellman? _diffieHellman;

    public static void Initialize(RsaKeys keys)
    {
        _rsa = RsaKey.ParsePrivateKey(keys.N, keys.E, keys.D);
        _diffieHellman = new();
    }

    private static string GetRsaStringEncrypted(string message)
    {
        if (_rsa == null)
            return "0";
        try
        {
            var m = Encoding.Default.GetBytes(message);
            var c = _rsa.Sign(m);
            return Converter.BytesToHexString(c);
        }
        catch
        {
            return "0";
        }
    }

    public static string GetRsaDiffieHellmanPrimeKey()
    {
        if (_diffieHellman == null)
            return "0";
        var key = _diffieHellman.Prime.ToString(10);
        return GetRsaStringEncrypted(key);
    }

    public static string GetRsaDiffieHellmanGeneratorKey()
    {
        if (_diffieHellman == null)
            return "0";
        var key = _diffieHellman.Generator.ToString(10);
        return GetRsaStringEncrypted(key);
    }

    public static string GetRsaDiffieHellmanPublicKey()
    {
        if (_diffieHellman == null)
            return "0";
        var key = _diffieHellman.PublicKey.ToString(10);
        return GetRsaStringEncrypted(key);
    }

    public static BigInteger CalculateDiffieHellmanSharedKey(string publicKey)
    {
        if (_rsa == null || _diffieHellman == null)
            return 0;
        try
        {
            var cbytes = Converter.HexStringToBytes(publicKey);
            var publicKeyBytes = _rsa.Verify(cbytes);
            var publicKeyString = Encoding.Default.GetString(publicKeyBytes);
            return _diffieHellman.CalculateSharedKey(new(publicKeyString, 10));
        }
        catch
        {
            return 0;
        }
    }
}
