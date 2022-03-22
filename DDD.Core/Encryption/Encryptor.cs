using System;
using System.Text;
using System.Security.Cryptography;

// from https://stackoverflow.com/questions/70235520/c-sharp-aes-encryption-need
public class Encryptor
{
    private const int MAX_IV_LENGTH = 16;
    private const int MAX_KEY_LENGTH = 32;
    private static string SEPARATOR = "|";
    public static string EncryptSubmissionId(string voteId, string submissionId, string passwordPhrase, long unixMsNow)
    {
        // we are adding a GUID as padding on the end to make the returned value even harder to reverse engineer
        var sessionUUIDAndTime = voteId + SEPARATOR + submissionId + SEPARATOR + unixMsNow + SEPARATOR + Guid.NewGuid().ToString();
        string encrypted = EncryptToBase64(sessionUUIDAndTime, passwordPhrase);

        Console.WriteLine(encrypted);
        return string.Empty;
    }

    public static Tuple<string, string, long> DecryptSubmissionId(string encryptedId, string passwordPhrase)
    {
        string response = DecryptFromBase64(encryptedId, passwordPhrase);

        Console.WriteLine(response);
        string[] parts = response.Split(SEPARATOR);
        return Tuple.Create(parts[0], parts[1], long.Parse(parts[1]));
    }

    private static byte[] GenerateValidKey(byte[] keyBytes)
    {
        byte[] ret = new byte[MAX_KEY_LENGTH];
        byte[] hash = new SHA256Managed().ComputeHash(keyBytes);
        Array.Copy(hash, ret, MAX_KEY_LENGTH);
        return ret;
    }

    private static byte[] EncryptRaw(byte[] PlainBytes, byte[] Key)
    {
        AesManaged AesAlgorithm = new AesManaged()
        {
            Key = GenerateValidKey(Key)
        };
        AesAlgorithm.GenerateIV();
        var Encrypted = AesAlgorithm.CreateEncryptor().TransformFinalBlock(PlainBytes, 0, PlainBytes.Length);
        byte[] ret = new byte[Encrypted.Length + MAX_IV_LENGTH];
        Array.Copy(Encrypted, ret, Encrypted.Length);
        Array.Copy(AesAlgorithm.IV, 0, ret, ret.Length - MAX_IV_LENGTH, MAX_IV_LENGTH);
        return ret;
    }

    private static byte[] DecryptRaw(byte[] CipherBytes, byte[] Key)
    {
        AesManaged AesAlgorithm = new AesManaged()
        {
            Key = GenerateValidKey(Key)
        };
        byte[] IV = new byte[MAX_IV_LENGTH];
        Array.Copy(CipherBytes, CipherBytes.Length - MAX_IV_LENGTH, IV, 0, MAX_IV_LENGTH);
        AesAlgorithm.IV = IV;
        byte[] RealBytes = new byte[CipherBytes.Length - MAX_IV_LENGTH];
        Array.Copy(CipherBytes, RealBytes, CipherBytes.Length - MAX_IV_LENGTH);
        return AesAlgorithm.CreateDecryptor().TransformFinalBlock(RealBytes, 0, RealBytes.Length); ;
    }


    private static String EncryptToBase64(String Plaintext, String Key)
    {
        byte[] PlainBytes = Encoding.UTF8.GetBytes(Plaintext);
        return ToBase64(EncryptRaw(PlainBytes, Encoding.UTF8.GetBytes(Key)), false);
    }

    private static String DecryptFromBase64(String CipherText, String Key)
    {
        byte[] CiPherBytes = Base64ToByteArray(CipherText);
        byte[] Encrypted = DecryptRaw(CiPherBytes, Encoding.UTF8.GetBytes(Key));
        return Encoding.UTF8.GetString(Encrypted, 0, Encrypted.Length);
    }

    private static byte[] Base64ToByteArray(String base64)
    {
        return Convert.FromBase64String(base64);
    }

    private static String ToBase64(byte[] data, Boolean insertLineBreaks = default(Boolean))
    {
        return insertLineBreaks ? Convert.ToBase64String(data, Base64FormattingOptions.InsertLineBreaks) : Convert.ToBase64String(data);
    }
}