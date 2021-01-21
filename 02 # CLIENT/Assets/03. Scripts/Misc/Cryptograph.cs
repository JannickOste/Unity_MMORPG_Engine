using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
public static class Cryptograph
{
    private static readonly byte[] saltBytes = new byte[] { 6, 2, 5, 1, 2, 6, 8, 2 };
    private static readonly Encoding encoding = Encoding.Unicode;
    private static readonly int byteSize = 512;

    private static string ToBase64(byte[] input) => Convert.ToBase64String(input);
    private static byte[] FromBase64(string input) => Convert.FromBase64String(input);
    public static IEnumerable<byte[]> Split(this byte[] value, int bufferLength)
    {
        int countOfArray = value.Length / bufferLength;
        if (value.Length % bufferLength > 0)
            countOfArray++;
        for (int i = 0; i < countOfArray; i++)
        {
            yield return value.Skip(i * bufferLength).Take(bufferLength).ToArray();

        }
    }

    #region RSA Encrypting/Decypting
    public static RSAParameters GenerateKey(bool _privateKey)
    {
        return new RSACryptoServiceProvider().ExportParameters(_privateKey);
    }

    public static (RSAParameters pub, RSAParameters priv) GenerateKeySet()
    {
        RSACryptoServiceProvider crypt = new RSACryptoServiceProvider(byteSize);
        return (crypt.ExportParameters(false), crypt.ExportParameters(true));
    }

    public static RSAParameters StringToKey(string keyString)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(RSAParameters));
        RSAParameters key;

        using (TextReader text = new StringReader(keyString))
            return (RSAParameters)serializer.Deserialize(text);
    }

    public static string KeyToString(RSAParameters targetKey)
    {
        using (StringWriter writer = new StringWriter())
        {
            XmlSerializer serializer = new XmlSerializer(typeof(RSAParameters));
            serializer.Serialize(writer, targetKey);


            return writer.ToString();
        }
    }

    public static string Decrypt(RSAParameters privateKey, string slices) => Decrypt(privateKey, slices.Split(new char[] { '=', '=' }).Where(i => i.Length > 0).Select(i => FromBase64($"{i}==")));
    public static string Decrypt(RSAParameters privateKey, IEnumerable<byte[]> slices)
    {
        using (RSACryptoServiceProvider crypt = new RSACryptoServiceProvider(byteSize))
        {
            crypt.ImportParameters(privateKey);

            int offset = 0;
            byte[] objectBuffer = new byte[slices.Select(i => i.Length).Aggregate((last, cur) => last + cur)];

            foreach (byte[] rawBufferPiece in slices)
            {
                byte[] bufferPiece = crypt.Decrypt(rawBufferPiece, true);
                Array.Copy(bufferPiece, 0, objectBuffer, offset, bufferPiece.Length);
                offset += bufferPiece.Length;
            }

            return Encoding.Unicode.GetString(objectBuffer);
        }
    }

    public static string Encrypt(RSAParameters publicKey, string value) => string.Join("", EncryptToByteSlices(publicKey, value).Select(i => ToBase64(i)));
    public static IEnumerable<byte[]> EncryptToByteSlices(RSAParameters publicKey, string value)
    {
        byte[] valueBytes = Encoding.Unicode.GetBytes(value);

        using (RSACryptoServiceProvider crypt = new RSACryptoServiceProvider(byteSize))
        {
            IEnumerable<byte[]> slices = Split(Encoding.Unicode.GetBytes(value), (byteSize / 25 <= 116 ? byteSize / 25 : 116));
            string[] strBuff = new string[slices.Count()];

            crypt.ImportParameters(publicKey);
            for (int i = 0; i < slices.Count(); i++)
                yield return crypt.Encrypt(slices.ElementAt(i), true);
        }
    }
    #endregion

    #region SHA256 Encrypting/Decrypting
    public static string SHA256Encrypt(string text, string password)
    {
        if (text == null | password == null) return string.Empty;

        byte[] textBytes = encoding.GetBytes(text);
        byte[] passBytes = SHA256.Create().ComputeHash(encoding.GetBytes(password));

        return ToBase64(SHA256Action(textBytes, passBytes, encrypt: true));
    }

    public static string SHA256Decrypt(string text, string password)
    {
        if (text == null | password == null) return string.Empty;

        byte[] textBytes = FromBase64(text);
        byte[] passBytes = SHA256.Create().ComputeHash(encoding.GetBytes(password));

        return encoding.GetString(SHA256Action(textBytes, passBytes, encrypt: false));
    }

    private static byte[] SHA256Action(byte[] textBytes, byte[] password, bool encrypt = false)
    {
        using (MemoryStream memStream = new MemoryStream())
        {
            using (RijndaelManaged AES = new RijndaelManaged())
            {
                Rfc2898DeriveBytes AESKey = new Rfc2898DeriveBytes(password, saltBytes, 1000);

                AES.KeySize = 256;
                AES.BlockSize = 128;
                AES.Key = AESKey.GetBytes(AES.KeySize / 8);
                AES.IV = AESKey.GetBytes(AES.BlockSize / 8);
                AES.Mode = CipherMode.CBC;

                using (var crypt = new CryptoStream(memStream, (encrypt ? AES.CreateEncryptor() : AES.CreateDecryptor()), CryptoStreamMode.Write))
                {
                    crypt.Write(textBytes, 0, textBytes.Length);
                }

                return memStream.ToArray();
            }
        }
    }
    #endregion
}