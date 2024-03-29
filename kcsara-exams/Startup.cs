using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using Kcsara.Exams.Certificates;
using Kcsara.Exams.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SarData;
using SarData.Common.Apis;
using SarData.Common.Apis.Database;
using SarData.Server;
using SarData.Server.Apis;
using SarData.Server.Apis.Health;
using Serilog;

namespace Kcsara.Exams
{
  public class Startup
  {
    private readonly IWebHostEnvironment env;

    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
      Configuration = configuration;
      this.env = env;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      try
      {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        services.AddAuthentication(options =>
        {
          options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
          options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddOpenIdConnect(options =>
        {
          options.Authority = Configuration["auth:authority"];
          options.RequireHttpsMetadata = !env.IsDevelopment();
          options.ClientId = Configuration["auth:frontend:client_id"];
          options.ClientSecret = Configuration["auth:frontend:client_secret"];
          foreach (var scope in Configuration["auth:frontend:scope"]?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? new string[0])
          {
            options.Scope.Add(scope);
          }
          options.ResponseType = "code";
          options.GetClaimsFromUserInfoEndpoint = true;
          options.ClaimActions.MapUniqueJsonKey("memberId", "memberId");
          options.SaveTokens = true;
        });
        services.AddControllersWithViews();
        services.AddRazorPages().AddJsonOptions(options => options.JsonSerializerOptions.Setup());

        services.AddSingleton<ITokenClient, DefaultTokenClient>();

        var healthChecksBuilder = services.AddHealthChecks();
        services.AddMessagingApi(Configuration, healthChecksBuilder);

        services.AddTableStorage(Configuration);
        services.AddSingleton<CertificateStore>();
        var quizStore = QuizStore.init(Configuration["local_files"]);
        if (quizStore.Quizzes.Count == 0)
        {
          Log.Logger.Error("exams.json not found. No quizzes will be available");
        }
        services.AddSingleton(quizStore);
      }
      catch (Exception e)
      {
        Log.Logger.Error(e, "Failed to setup app");
      }
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      app.UseSarHealthChecks<Startup>();

      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }
     // app.UseHttpsRedirection();
      app.UseStaticFiles();

      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllerRoute(
                  name: "default",
                  pattern: "{controller=Home}/{action=Index}/{id?}");
        endpoints.MapRazorPages();
        endpoints.MapGet("/.well-known/acme-challenge/{name}", async context =>
        {
          string folder = Path.GetFullPath(Path.Combine(Configuration["local_files"] ?? ".", "letsencrypt", ".well-known", "acme-challenge"));
          string name = Path.GetFullPath(Path.Combine(folder, context.Request.RouteValues["name"].ToString()));
          // Don't trust the user supplied file not to jump out of the directory. Make sure it exists in the acme-challenge directory.
          string requestedPath = Directory.GetFiles(folder).FirstOrDefault(f => f.Equals(name));
          context.Response.StatusCode = requestedPath == null ? 404 : 200;
          await context.Response.WriteAsync(requestedPath == null ? "Not here" : await File.ReadAllTextAsync(requestedPath));
        });
      });
    }
  }
}
