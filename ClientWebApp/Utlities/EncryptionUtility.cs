using System.Security.Cryptography;

namespace ClientWebApp.Utlities
{
    public class EncryptionUtility
    {
        private readonly ILogger<EncryptionUtility> _logger;

        public EncryptionUtility(ILogger<EncryptionUtility> logger)
        {
            _logger = logger;
        }

        public string Hash(MemoryStream input)
        {
            try
            {
                input.Position = 0;
                using var sha = SHA512.Create();
                var hashBytes = sha.ComputeHash(input);
                _logger.LogInformation("SHA-512 hash generated for input stream.");
                return Convert.ToBase64String(hashBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hash memory stream.");
                throw;
            }
        }

        public SymmetricKeys GenerateSymmetricKeys(SymmetricAlgorithm myAlg)
        {
            myAlg.GenerateIV();
            myAlg.GenerateKey();
            _logger.LogInformation("Symmetric AES keys generated.");
            return new SymmetricKeys { IV = myAlg.IV, SecretKey = myAlg.Key };
        }

        public MemoryStream SymmetricEncrypt(MemoryStream inputStream, SymmetricAlgorithm alg, SymmetricKeys keys)
        {
            try
            {
                inputStream.Position = 0;
                alg.Key = keys.SecretKey;
                alg.IV = keys.IV;

                MemoryStream outputStream = new();
                using (CryptoStream cryptoStream = new(inputStream, alg.CreateEncryptor(), CryptoStreamMode.Read))
                {
                    cryptoStream.CopyTo(outputStream);
                    cryptoStream.Flush();
                }

                outputStream.Position = 0;
                _logger.LogInformation("Symmetric encryption completed using AES.");
                return outputStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Symmetric encryption failed.");
                throw;
            }
        }

        public AsymmetricKeys GenerateAsymmetricKeys()
        {
            var rsa = new RSACryptoServiceProvider();
            _logger.LogInformation("Asymmetric RSA key pair generated.");
            return new AsymmetricKeys
            {
                PublicKey = rsa.ToXmlString(false),
                PrivateKey = rsa.ToXmlString(true)
            };
        }

        public byte[] AsymmetricEncryption(byte[] input, string publicKey)
        {
            try
            {
                var rsa = new RSACryptoServiceProvider();
                rsa.FromXmlString(publicKey);
                var encrypted = rsa.Encrypt(input, true);
                _logger.LogInformation("Data encrypted using RSA public key.");
                return encrypted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Asymmetric encryption failed.");
                throw;
            }
        }

        public MemoryStream HybridEncrypt(MemoryStream input, string publicKey)
        {
            try
            {
                var symmKeys = GenerateSymmetricKeys(Aes.Create());
                var cipher = SymmetricEncrypt(input, Aes.Create(), symmKeys);

                var encKey = AsymmetricEncryption(symmKeys.SecretKey, publicKey);
                var encIv = AsymmetricEncryption(symmKeys.IV, publicKey);

                MemoryStream output = new();
                output.Write(encKey, 0, encKey.Length);
                output.Write(encIv, 0, encIv.Length);

                cipher.Position = 0;
                cipher.CopyTo(output);
                output.Position = 0;

                _logger.LogInformation("Hybrid encryption completed: AES data + RSA-encrypted key/IV.");
                return output;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hybrid encryption failed.");
                throw;
            }
        }

        public string DigitallySign(MemoryStream input, string privateKey)
        {
            try
            {
                RSACryptoServiceProvider rsa = new();
                rsa.FromXmlString(privateKey);

                byte[] inputAsBytes = input.ToArray();
                byte[] signatureAsBytes = rsa.SignData(inputAsBytes, new HashAlgorithmName("SHA512"), RSASignaturePadding.Pkcs1);

                _logger.LogInformation("Digital signature successfully created.");
                return Convert.ToBase64String(signatureAsBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Digital signature creation failed.");
                throw;
            }
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
