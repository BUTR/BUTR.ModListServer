using Aragas.Extensions.Options.FluentValidation.Extensions;

using BUTR.ModListServer.Options;

using Community.Microsoft.Extensions.Caching.PostgreSql;

using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IO;
using Microsoft.OpenApi.Models;

using System.IO.Compression;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

namespace BUTR.ModListServer
{
    public class Program
    {
        private const string ConnectionStringsSectionName = "ConnectionStrings";
        private const string ModListUploadSectionName = "ModListUpload";

        private static JsonSerializerOptions Configure(JsonSerializerOptions opt)
        {
            opt.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            opt.PropertyNameCaseInsensitive = true;
            opt.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
            opt.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            return opt;
        }

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionStringSection = builder.Configuration.GetSection(ConnectionStringsSectionName);
            var modListUploadSection = builder.Configuration.GetSection(ModListUploadSectionName);

            builder.Services.AddValidatedOptions<ConnectionStringsOptions, ConnectionStringsOptionsValidator>(connectionStringSection);
            builder.Services.AddValidatedOptions<ModListUploadOptions, ModListUploadOptionsValidator>(modListUploadSection);

            /*
            builder.Services.AddDistributedMemoryCache();
            */
            builder.Services.AddDistributedPostgreSqlCache(options =>
            {
                var opts = connectionStringSection.Get<ConnectionStringsOptions>();

                options.ConnectionString = opts.Main;
                options.SchemaName = "modlist";
                options.TableName = "modlist_cache_entry";
                options.CreateInfrastructure = true;
            });

            builder.Services.AddControllersWithViews().AddJsonOptions(opt => Configure(opt.JsonSerializerOptions));
            builder.Services.AddRouting(options =>
            {
                options.LowercaseUrls = true;
            });
            builder.Services.AddResponseCompression(options =>
            {
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });
            builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Fastest;
            });
            builder.Services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.SmallestSize;
            });

            builder.Services.AddSingleton<RecyclableMemoryStreamManager>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(opt =>
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

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=ModList}/{action=IndexAsync}/{id?}");

            app.Run();
        }
    }
}