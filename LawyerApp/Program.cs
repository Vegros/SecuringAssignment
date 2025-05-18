using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;


namespace LawyerApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var client = new HttpClient();
            RSACryptoServiceProvider rsa = new();
            string privateKey = rsa.ToXmlString(true);
            string publicKey = rsa.ToXmlString(false);


            var requestPayload = new
            {
                fileId = "dc548746-fb69-4abd-aaf6-9c564dba96aa",
                email = "mattias.tonna05@hotmail.com",
                accessCode = "40107510-ba8b-4537-943c-5c124489949a",
                lawyerPublicKey = publicKey
            };

            try
            {
                var response = await client.PostAsJsonAsync("https://localhost:7130/api/FileDownload/Download", requestPayload);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string base64EncryptedFile = root.GetProperty("file").GetString();
                string signature = root.GetProperty("signature").GetString();
                string serverPublicKey = root.GetProperty("serverPublicKey").GetString();



                byte[] encryptedFileBytes = Convert.FromBase64String(base64EncryptedFile);
                string savedPrivateKey = privateKey;
                RSACryptoServiceProvider lawyerRsa = new();
                lawyerRsa.FromXmlString(savedPrivateKey);

                int rsaKeySize = 128;
                byte[] encKey = encryptedFileBytes[..rsaKeySize];
                byte[] encIV = encryptedFileBytes[rsaKeySize..(rsaKeySize * 2)];
                byte[] cipherContent = encryptedFileBytes[(rsaKeySize * 2)..];

                byte[] aesKey = lawyerRsa.Decrypt(encKey, true);
                byte[] iv = lawyerRsa.Decrypt(encIV, true);


                Aes aes = Aes.Create();
                aes.Key = aesKey;
                aes.IV = iv;

                byte[] decrypted;
                using (MemoryStream msOut = new())
                using (CryptoStream cs = new(new MemoryStream(cipherContent), aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    cs.CopyTo(msOut);
                    decrypted = msOut.ToArray();
                }

                RSACryptoServiceProvider serverRsa = new();
                serverRsa.FromXmlString(serverPublicKey);

                bool isValid = serverRsa.VerifyData(decrypted, new SHA512CryptoServiceProvider(), Convert.FromBase64String(signature));
                Console.WriteLine($"Signature valid? {isValid}");

                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "Decrypted_Lawyer_File.docx");
                await File.WriteAllBytesAsync(path, decrypted);
                Console.WriteLine("File saved at: " + path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                Console.WriteLine(ex.Message);
            }
        }
    }
}
