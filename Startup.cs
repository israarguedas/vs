using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MIBServiceFunctionApp.Services;
using Azure.Search.Documents;
using Azure;
using System;
using Microsoft.AspNetCore.Http.Features;

[assembly: FunctionsStartup(typeof(MIBServiceFunctionApp.Startup))]

namespace MIBServiceFunctionApp
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var configuration = builder.GetContext().Configuration;
            // Increase multipart body length limit
            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // 50 MB
            });

            // Register KeyVaultService
            builder.Services.AddSingleton<KeyVaultService>();

            // Configure SearchClient
            builder.Services.AddSingleton(provider =>
            {
                var searchServiceEndpoint = configuration["SearchServiceEndpoint"];
                var searchServiceApiKey = configuration["SearchServiceApiKey"];
                var searchIndexName = configuration["SearchIndexName"];

                var searchClient = new SearchClient(new Uri(searchServiceEndpoint), searchIndexName, new AzureKeyCredential(searchServiceApiKey));
                return searchClient;
            });

            // Register SearchService
            builder.Services.AddSingleton<SearchService>();

            // Register BlobServiceClient using the connection string from the configuration
            builder.Services.AddSingleton(provider =>
            {
                var storageConnectionString = configuration["StorageAccountConnectionString"];
                return new BlobServiceClient(storageConnectionString);
            });

            // Register StorageService
            builder.Services.AddSingleton<IStorageService, StorageService>(provider =>
            {
                var storageConnectionString = configuration["StorageAccountConnectionString"];
                return new StorageService(storageConnectionString);
            });

            // Register SearchService
            builder.Services.AddSingleton<StorageService>();

            builder.Services.AddSingleton<ParseMIB>();         
        }
    }
}
