using ClientWebApp.Models.DatabaseModels;
using System.Security.Cryptography;
using System.Text;

namespace ClientWebApp.Utlities
{
    public class EncryptionUtility
    {
        public string Hash(MemoryStream input)
        {
            input.Position = 0;
            using var sha = SHA512.Create();
            var hashBytes = sha.ComputeHash(input);
            return Convert.ToBase64String(hashBytes);
        }

        public SymmetricKeys GenerateSymmetricKeys(SymmetricAlgorithm myAlg)
        {
            myAlg.GenerateIV(); myAlg.GenerateKey();
            return new SymmetricKeys { IV = myAlg.IV, SecretKey = myAlg.Key };
        }

        public MemoryStream SymmetricEncrypt(MemoryStream inputStream, SymmetricAlgorithm alg, SymmetricKeys keys)
        {
            inputStream.Position = 0;
            alg.Key = keys.SecretKey;
            alg.IV = keys.IV;

            MemoryStream outputStream = new MemoryStream();
            using (CryptoStream cryptoStream = new CryptoStream(inputStream, alg.CreateEncryptor(), CryptoStreamMode.Read))
            {
                cryptoStream.CopyTo(outputStream);
                cryptoStream.Flush();
            }

            outputStream.Position = 0;
            return outputStream;
        }

        public AsymmetricKeys GenerateAsymmetricKeys()
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            return new AsymmetricKeys
            {
                PublicKey = rsa.ToXmlString(false),
                PrivateKey = rsa.ToXmlString(true)
            };
        }

        public byte[] AsymmetricEncryption(byte[] input, string publicKey)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(publicKey);
            return rsa.Encrypt(input, true);
        }

        public MemoryStream HybridEncrypt(MemoryStream input, string publicKey)
        {
            var symmKeys = GenerateSymmetricKeys(Aes.Create());
            var cipher = SymmetricEncrypt(input, Aes.Create(), symmKeys);
            var encKey = AsymmetricEncryption(symmKeys.SecretKey, publicKey);
            var encIv = AsymmetricEncryption(symmKeys.IV, publicKey);

            MemoryStream output = new MemoryStream();
            output.Write(encKey, 0, encKey.Length);
            output.Write(encIv, 0, encIv.Length);
            cipher.Position = 0;
            cipher.CopyTo(output);
            output.Position = 0;
            return output;
        }

        public string DigitallySign(MemoryStream input, string privateKey)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKey);
            byte[] inputAsBytes = input.ToArray();
            byte[] signatureAsBytes = rsa.SignData(inputAsBytes, new HashAlgorithmName("SHA512"), RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signatureAsBytes);
        }
    }

    public class SymmetricKeys
    {
        public byte[] SecretKey { get; set; }
        public byte[] IV { get; set; }
    }

    public class AsymmetricKeys
    {
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
    }

}