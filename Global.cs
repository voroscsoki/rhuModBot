using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

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
        public static WebClient webclient = new WebClient();
        public static HttpClient httpclient = new HttpClient();
        public static string ConfigFile = "Configuration.json";
        public static Configuration Config = new Configuration();
        public static string execDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static async Task<bool> SetupAsync()
        {
            LoadConfig();
            return true;
        }
        public static async Task StartAsync()
        {
            var runreddit = RedditService.Initialise();
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