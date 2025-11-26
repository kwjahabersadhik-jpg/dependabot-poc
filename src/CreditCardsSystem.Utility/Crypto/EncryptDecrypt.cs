using System.Security.Cryptography;
using System.Text;

namespace CreditCardsSystem.Utility.Crypto;

public class EncryptDecrypt
{
    private const string encryptionKey = "Aurora2024@KFH";

    private static string EncryptionKey => "Aurora2024@KFH";

    public static string Encrypt(string clearText)
    {
        string result = "";
        if (clearText == "")
        {
            return result;
        }

        Rijndael rijndael = Rijndael.Create();
        byte[] bytes = Encoding.ASCII.GetBytes(clearText);
        _ = (rijndael.IV = (rijndael.Key = Encoding.ASCII.GetBytes(EncryptionKey)));
        MemoryStream memoryStream = new();
        CryptoStream cryptoStream = new(memoryStream, rijndael.CreateEncryptor(), CryptoStreamMode.Write);
        cryptoStream.Write(bytes, 0, bytes.Length);
        cryptoStream.FlushFinalBlock();
        cryptoStream.Close();
        byte[] inArray = memoryStream.ToArray();
        memoryStream.Close();
        return Convert.ToBase64String(inArray);
    }

    public static string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        Rijndael rijndael = Rijndael.Create();
        byte[] array = Convert.FromBase64String(cipherText);
        _ = (rijndael.IV = (rijndael.Key = Encoding.ASCII.GetBytes(EncryptionKey)));
        MemoryStream memoryStream = new();
        CryptoStream cryptoStream = new(memoryStream, rijndael.CreateDecryptor(), CryptoStreamMode.Write);
        cryptoStream.Write(array, 0, array.Length);
        cryptoStream.Close();
        byte[] bytes2 = memoryStream.ToArray();
        memoryStream.Close();
        return Encoding.ASCII.GetString(bytes2);
    }
}

public class AesEncryptor
{
    // Replace this with a key from a secure source (e.g., Azure Key Vault).
    private static readonly byte[] Key = GenerateRandomBytes(32);
    public static byte[] GenerateRandomBytes(int length)
    {
        byte[] randomBytes = new byte[length];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return randomBytes;
    }
    public static string Encrypt(string dataToEncrypt)
    {
        if (Key.Length != 32)
        {
            throw new ArgumentException("Key must be 32 bytes for AES-256.");
        }
        using (Aes aes = Aes.Create())
        {
            aes.Key = Key;
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream())
            {
                ms.Write(aes.IV, 0, aes.IV.Length);  // Prepend IV to ciphertext for decryption
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var writer = new StreamWriter(cs))
                {
                    writer.Write(dataToEncrypt);
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
    public static string Decrypt(string encryptedData)
    {
        byte[] fullCipher = Convert.FromBase64String(encryptedData);
        using (Aes aes = Aes.Create())
        {
            var blockSize = aes.BlockSize / 8;
            aes.Key = Key;
            aes.IV = fullCipher[..blockSize]; // Extract IV from the beginning
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
            using (var ms = new MemoryStream(fullCipher[blockSize..]))
            using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
            using (var reader = new StreamReader(cs))
            {
                return reader.ReadToEnd();
            }
        }
    }
}

public static class SaltingExtension
{
    public static string SaltThis(this object? plain)
    {
        if (plain is null)
            return string.Empty;
        var str = plain.ToString();
        if (string.IsNullOrEmpty(str))
            return string.Empty;

        // To Hex
        var stringBytes = Encoding.UTF8.GetBytes(str);
        var sbBytes = new StringBuilder(stringBytes.Length * 2);
        foreach (var b in stringBytes)
        {
            sbBytes.Append($"{b:X2}");
        }

        return sbBytes.ToString();
    }

    public static string DeSaltThis(this object? salted)
    {
        if (salted is null)
            return string.Empty;


        var str = salted.ToString();
        if (string.IsNullOrEmpty(str))
            return string.Empty;
        var numberChars = str.Length;
        var bytes = new byte[numberChars / 2];
        for (var i = 0; i < numberChars; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(str.Substring(i, 2), 16);
        }

        return Encoding.UTF8.GetString(bytes);
    }
}