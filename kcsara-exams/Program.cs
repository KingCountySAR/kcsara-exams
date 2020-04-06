using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using SarData.Logging;

namespace Kcsara.Exams
{
  public class Program
  {
    public static void Main(string[] args)
    {
      CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
      .ConfigureLogging((context, logBuilder) =>
      {
        logBuilder.AddSarDataLogging(context.Configuration["local_files"] ?? context.HostingEnvironment.ContentRootPath, "exams");
      })
      .ConfigureAppConfiguration((context, config) =>
      {
        config.AddConfigFiles(context.HostingEnvironment.EnvironmentName);
      })
      .ConfigureWebHostDefaults(webBuilder =>
      {
        webBuilder.UseStartup<Startup>();
      });
  }
}
