using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MIBServiceFunctionApp.Services
{
    public class SearchService
    {
        private readonly SearchClient mSearchClient;
        private readonly IConfiguration mConfiguration; 

        public SearchService(SearchClient searchClient, IConfiguration config)
        {
            mSearchClient = searchClient;
            mConfiguration = config;
        }

        public async Task<IList<SearchResult>> SearchMIBsAsync(string searchText)
        {
            // Define the search options
            var options = new SearchOptions
            {
                Size = Convert.ToInt16(mConfiguration["SearchRecordCount"]) // Limit to top 50 results
            };

            // Execute the search
            var searchResults = await mSearchClient.SearchAsync<SearchDocument>(searchText, options);

            // Process the search results
            List<SearchResult> results = new List<SearchResult>();
            await foreach (var result in searchResults.Value.GetResultsAsync())
            {
                results.Add(new SearchResult
                {
                    MibName = result.Document["metadata_storage_name"]?.ToString(), // Adjust the key name based on your index definition
                    // Add other relevant properties
                });
            }

            return results;
        }

        public async Task<bool> SearchExactMIBsAsync(string searchText)
        {
            var searchOptions = new SearchOptions
            {
                Filter = $"metadata_storage_name eq '{searchText}'", // Exact match using eq operator
                Size = 1 // We only need one result to confirm existence
            };

            // Execute the search
            var searchResults = await mSearchClient.SearchAsync<SearchDocument>(searchText, searchOptions);

            // Process the search results
            List<SearchResult> results = new List<SearchResult>();
            await foreach (var result in searchResults.Value.GetResultsAsync())
            {
                var dsad = result.Document["metadata_storage_name"]?.ToString();
                results.Add(new SearchResult
                {
                    MibName = result.Document["metadata_storage_name"]?.ToString(), // Adjust the key name based on your index definition
                    // Add other relevant properties
                });
            }

            return results.Count > 0 ? true : false;
        }       
    }

    public class SearchResult
    {
        public string MibName { get; set; }
        // Add other properties if necessary
    }
}