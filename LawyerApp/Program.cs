using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;


namespace LawyerApp
{
    internal class Program
    {
        public static async Task Menu()
        {
            Console.WriteLine("******************MENU******************");
            Console.WriteLine("1. Download");
            Console.WriteLine("2. Exit");
            Console.Write("Enter your choice: ");
            int option = Convert.ToInt32(Console.ReadLine());
            switch (option)
            {
                case 1: await DownloadFile(); break;
                case 2: Environment.Exit(0); break;
                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }

        }

        public static async Task DownloadFile()
        {
            Console.Write("Enter email: ");
            String email = Console.ReadLine();
            Console.Write("Enter accessCode: ");
            String accessCode = Console.ReadLine();

            await Download(email, accessCode);
        }

        public static async Task Download(String email, String accessCode)
        {
            var client = new HttpClient();
            RSACryptoServiceProvider rsa = new();
            string privateKey = rsa.ToXmlString(true);
            string publicKey = rsa.ToXmlString(false);


            var requestPayload = new
            {
                email = email,
                accessCode = accessCode,
                lawyerPublicKey = publicKey
            };


            try
            {
                var response = await client.PostAsJsonAsync("https://localhost:7130/Download", requestPayload);

                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    var errorMessage = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Access Denied: " + errorMessage);
                    return;
                }
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string fileName = root.GetProperty("fileName").GetString();
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

                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", fileName);
                await File.WriteAllBytesAsync(path, decrypted);
                Console.WriteLine("File saved at: " + path);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        static async Task Main(string[] args)
        {
            while (true)
            {
                await Menu();
            }
        }
    }
}
