using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace KayCareLIS.Infrastructure.Security;

public class EncryptionHelper
{
    private readonly byte[] _key;

    public EncryptionHelper(IConfiguration config)
    {
        var keyStr = config["Encryption:Key"] ?? Environment.GetEnvironmentVariable("ENCRYPTION_KEY");
        if (string.IsNullOrEmpty(keyStr))
        {
            // Default dev key - 32 bytes (256 bits)
            _key = Encoding.UTF8.GetBytes("dev_encryption_key_32_bytes_long");
        }
        else
        {
            try
            {
                _key = Convert.FromBase64String(keyStr);
                if (_key.Length != 32)
                {
                    throw new ArgumentException("Encryption key must be exactly 32 bytes (256-bit) when Base64-decoded.");
                }
            }
            catch
            {
                // Fallback to padded key if base64 decode fails
                var bytes = Encoding.UTF8.GetBytes(keyStr);
                _key = new byte[32];
                Array.Copy(bytes, _key, Math.Min(bytes.Length, 32));
            }
        }
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext;

        byte[] nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12 bytes
        RandomNumberGenerator.Fill(nonce);

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] ciphertext = new byte[plaintextBytes.Length];
        byte[] tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16 bytes

        using var aesGcm = new AesGcm(_key, tag.Length);
        aesGcm.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        // Packet format: [Nonce (12B)] [Tag (16B)] [Ciphertext (Variable)]
        byte[] packet = new byte[nonce.Length + tag.Length + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, packet, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, packet, nonce.Length, tag.Length);
        Buffer.BlockCopy(ciphertext, 0, packet, nonce.Length + tag.Length, ciphertext.Length);

        return Convert.ToBase64String(packet);
    }

    public string Decrypt(string ciphertextBase64)
    {
        if (string.IsNullOrEmpty(ciphertextBase64)) return ciphertextBase64;

        try
        {
            byte[] packet = Convert.FromBase64String(ciphertextBase64);
            int nonceSize = AesGcm.NonceByteSizes.MaxSize;
            int tagSize = AesGcm.TagByteSizes.MaxSize;

            if (packet.Length < nonceSize + tagSize) return ciphertextBase64;

            byte[] nonce = new byte[nonceSize];
            byte[] tag = new byte[tagSize];
            byte[] ciphertext = new byte[packet.Length - nonceSize - tagSize];

            Buffer.BlockCopy(packet, 0, nonce, 0, nonce.Length);
            Buffer.BlockCopy(packet, nonce.Length, tag, 0, tag.Length);
            Buffer.BlockCopy(packet, nonce.Length + tag.Length, ciphertext, 0, ciphertext.Length);

            byte[] plaintextBytes = new byte[ciphertext.Length];
            using var aesGcm = new AesGcm(_key, tag.Length);
            aesGcm.Decrypt(nonce, ciphertext, tag, plaintextBytes);

            return Encoding.UTF8.GetString(plaintextBytes);
        }
        catch
        {
            // If decryption fails, return as-is (e.g. legacy plaintext data)
            return ciphertextBase64;
        }
    }
}
