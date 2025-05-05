using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;

namespace Lawyer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var client = new HttpClient();

            var requestPayload = new
            {
                fileId = "your_file_id_here",
                email = "your_email_here",
                accessCode = "your_access_code_here",
                lawyerPublicKey = "your_RSA_PUBLIC_KEY_here"
            };

            var response = await client.PostAsJsonAsync("https://localhost:7130/api/FileDownload/Download", requestPayload);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string base64EncryptedFile = root.GetProperty("file").GetString();
            string signature = root.GetProperty("signature").GetString();
            string serverPublicKey = root.GetProperty("serverPublicKey").GetString();

            // Replace with your private key
            string lawyerPrivateKey = @"<RSAKeyValue>...</RSAKeyValue>";

            // Decrypt
            RSACryptoServiceProvider rsa = new();
            rsa.FromXmlString(lawyerPrivateKey);

            byte[] encryptedFileBytes = Convert.FromBase64String(base64EncryptedFile);
            int keySize = 256;

            byte[] encSymmetricKey = encryptedFileBytes[..keySize];
            byte[] encIV = encryptedFileBytes[keySize..(keySize * 2)];
            byte[] encryptedContent = encryptedFileBytes[(keySize * 2)..];

            byte[] symmetricKey = rsa.Decrypt(encSymmetricKey, true);
            byte[] iv = rsa.Decrypt(encIV, true);

            Aes aes = Aes.Create();
            aes.Key = symmetricKey;
            aes.IV = iv;

            using var output = new MemoryStream();
            using (CryptoStream cryptoStream = new(new MemoryStream(encryptedContent), aes.CreateDecryptor(), CryptoStreamMode.Read))
            {
                cryptoStream.CopyTo(output);
            }

            byte[] decryptedBytes = output.ToArray();

            RSACryptoServiceProvider serverRsa = new();
            serverRsa.FromXmlString(serverPublicKey);
            bool isValid = serverRsa.VerifyData(decryptedBytes, new SHA512CryptoServiceProvider(), Convert.FromBase64String(signature));
            Console.WriteLine("✅ Signature valid? " + isValid);

            string savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Decrypted_Lawyer_File.docx");
            File.WriteAllBytes(savePath, decryptedBytes);
            Console.WriteLine("📂 File saved to: " + savePath);
        }
    }
}
