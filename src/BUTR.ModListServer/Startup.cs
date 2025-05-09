﻿using Aragas.Extensions.Options.FluentValidation.Extensions;

using BUTR.ModListServer.Options;
using BUTR.ModListServer.Services;

using Community.Microsoft.Extensions.Caching.PostgreSql;

using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IO;
using Microsoft.OpenApi.Models;

using System.IO.Compression;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace BUTR.ModListServer;

public class Startup
{
    private const string ConnectionStringsSectionName = "ConnectionStrings";
    private const string ModListUploadSectionName = "ModListUpload";
    private const string UptimeKumaSectionName = "UptimeKuma";

    private static JsonSerializerOptions Configure(JsonSerializerOptions opt)
    {
        opt.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opt.PropertyNameCaseInsensitive = true;
        opt.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
        opt.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return opt;
    }

    private readonly string _appName;
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "ERROR";
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var assemblyName = typeof(Startup).Assembly.GetName();
        var userAgent = $"{assemblyName.Name ?? "ERROR"} v{assemblyName.Version?.ToString() ?? "ERROR"} (github.com/BUTR)";

        var connectionStringSection = _configuration.GetSection(ConnectionStringsSectionName);
        var modListUploadSection = _configuration.GetSection(ModListUploadSectionName);
        var uptimeKumaSection = _configuration.GetSection(UptimeKumaSectionName);

        services.AddValidatedOptions<ConnectionStringsOptions, ConnectionStringsOptionsValidator>().Bind(connectionStringSection);
        services.AddValidatedOptions<ModListUploadOptions, ModListUploadOptionsValidator>().Bind(modListUploadSection);
        services.Configure<UptimeKumaOptions>(uptimeKumaSection);

        services.AddHttpClient<IHealthCheckPublisher, UptimeKumaHealthCheckPublisher>().ConfigureHttpClient((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<UptimeKumaOptions>>().Value;

            if (Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var uri))
                client.BaseAddress = uri;
            client.DefaultRequestHeaders.Add("User-Agent", userAgent);
        });

        /*
        services.AddDistributedMemoryCache();
        */
        services.AddDistributedPostgreSqlCache(options =>
        {
            var opts = connectionStringSection.Get<ConnectionStringsOptions>()!;

            options.ConnectionString = opts.Main;
            options.SchemaName = "modlist";
            options.TableName = "modlist_cache_entry";
            options.CreateInfrastructure = true;
        });

        services.AddControllersWithViews().AddJsonOptions(opt => Configure(opt.JsonSerializerOptions));
        services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
        });
        services.AddResponseCompression(options =>
        {
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });
        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.SmallestSize;
        });

        services.AddSingleton<RecyclableMemoryStreamManager>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(opt =>
        {
            opt.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "BUTR's ModList Server API",
                Description = "BUTR's API that for managing Mod Lists",
            });

            opt.DescribeAllParametersInCamelCase();
            opt.SupportNonNullableReferenceTypes();

            var currentAssembly = Assembly.GetExecutingAssembly();
            var xmlFilePaths = currentAssembly.GetReferencedAssemblies()
                .Append(currentAssembly.GetName())
                .Select(x => Path.Combine(Path.GetDirectoryName(currentAssembly.Location)!, $"{x.Name}.xml"))
                .Where(File.Exists)
                .ToList();
            foreach (var xmlFilePath in xmlFilePaths)
                opt.IncludeXmlComments(xmlFilePath);
        });

        services.AddHealthChecks()
            .AddNpgSql(_configuration.GetConnectionString("Main")!);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseSwagger();
        app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", _appName));

        app.UseHealthChecks("/healthz");

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}