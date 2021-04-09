using System;
using System.Threading.Tasks;

namespace rhuModBot
{
    public class Program
    {
        public static void Main(string[] args) => new Program().RunAsync().GetAwaiter().GetResult();

        public async Task RunAsync()
        {
            if (await Global.SetupAsync())
            {
                await Global.StartAsync();
                await Task.Delay(-1);
            }
            else
            {
                Console.WriteLine("Please have a look at the configuration file!");
            }
        }
    }
}