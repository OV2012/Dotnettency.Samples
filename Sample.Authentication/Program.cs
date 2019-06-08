using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using Serilog.Events;

namespace Sample.Authentication
{
    public class Program
    {

        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();


            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>

            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .ConfigureLogging((b)=> {
                    b.AddSerilog(Log.Logger); // this does!
                })
                .UseUrls("http://*:5000", "http://*:5001", "http://*:5002", "http://*:5003", "http://*:5004")
               // .UseSerilog(providers: Providers) // this doesn't work with dotnettency!
                .Build();
    }
}
