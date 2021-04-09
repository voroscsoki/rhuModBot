using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace rhuModBot
{
    public struct Configuration
    {
        public int RedditTesting { get; set; }
        public string RedditUsername { get; set; }
        public string RedditPW { get; set; }
        public string RedditAppId { get; set; }
        public string RedditAppSecret { get; set; }
        public DateTime RedditTimeOverride { get; set; }
    }

    public static class Global
    {
        public static string ConfigFile = "Configuration.json";
        public static Configuration Config = new Configuration();
        public static async Task<bool> SetupAsync()
        {
            LoadConfig();
            Console.WriteLine("setup complete");
            return true;
        }
        public static async Task StartAsync()
        {
            Console.WriteLine("initiated");
            await RedditService.Initialise();
        }
        public static string GetTime()
        {
            return DateTime.Now.ToString("HH:mm:ss");
        }
        private static bool LoadConfig()
        {
            if (File.Exists(ConfigFile))
            {
                Config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(ConfigFile));
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}