using Spectre.Console;
using System.Text.Json;

namespace AccountCreator
{
    internal static class Program
    {
        public static Settings? appSettings;
        private static int PromosCreated = 0;
        private static int ThreadsRunning;
        private static readonly ReaderWriterLockSlim LogLock = new ReaderWriterLockSlim();

        public static void Log(string fileName, string message)
        {
            if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            string filePath = fileName + ".txt";

            try
            {
                LogLock.EnterWriteLock();

                File.AppendAllText(filePath, message + Environment.NewLine);
            }
            catch (Exception) { }
            finally
            {
                if (LogLock.IsWriteLockHeld)
                {
                    LogLock.ExitWriteLock();
                }
            }
        }

        [STAThread]
        static async Task Main()
        {
            AnsiConsole.Write(
                new FigletText("Nitro Promo Gen")
                    .LeftJustified()
                    .Color(Spectre.Console.Color.Magenta2));

            appSettings = new("config.ini");

            AnsiConsole.MarkupLine("[red]Press Enter to start.[/]");
            UpdateTitle();
            Console.ReadKey();

            AnsiConsole.MarkupLine("[red]Starting............[/]");

            var tasks = new Task[appSettings.MaxThreads];
            for (int i = 0; i < appSettings.MaxThreads; i++)
            {
                tasks[i] = MainProcess();
            }

            await Task.WhenAll(tasks);
        }

        static void UpdateTitle()
        {
            int currentThreadsRunning = Interlocked.CompareExchange(ref ThreadsRunning, 0, 0);
            int currentAccountsCreated = Interlocked.CompareExchange(ref PromosCreated, 0, 0);
            Console.Title = $"Threads: {currentThreadsRunning}/{appSettings.MaxThreads} " +
                            $"Promos Created: {currentAccountsCreated}";
        }

        public class PROMOJSON
        {
            public string partnerUserId { get; set; }
        }

        public class RESPONSEJSON
        {
            public string token { get; set; }
        }


        static async Task MainProcess()
        {
            Interlocked.Increment(ref ThreadsRunning);
            UpdateTitle();

            try
            {
                using (var httpClient = new HttpClient())
                {
                    PROMOJSON promoJson = new PROMOJSON 
                    { 
                        partnerUserId = "0d031b676b3c6343c53d10c849e03cd331d5dbcd71ed1965eee3d7164005b1cd" 
                    };

                    string jsonPayload = JsonSerializer.Serialize(promoJson);

                    StringContent content = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");


                    HttpResponseMessage response = await httpClient.PostAsync("https://api.discord.gx.games/v1/direct-fulfillment", content);

                    RESPONSEJSON responsePayload = JsonSerializer.Deserialize<RESPONSEJSON>(await response.Content.ReadAsStringAsync());
                    string promo_link = $"https://discord.com/billing/partner-promotions/1180231712274387115/{responsePayload.token}";
                    if (response.IsSuccessStatusCode)
                    {
                        AnsiConsole.MarkupLine($"[green]{promo_link}[/]");
                        Log("promos", promo_link);
                        Interlocked.Increment(ref PromosCreated);
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[red]Failed to Create Nitro Promo[/]");
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]Failed to Create Nitro Promo[/]");

                Log("errors", ex.ToString());
            }
            finally
            {
                Interlocked.Decrement(ref ThreadsRunning);
                UpdateTitle();

                await Task.Delay(TimeSpan.FromSeconds(1));
                await MainProcess();
            }
        }
    }
}