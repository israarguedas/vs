using MIBServiceFunctionApp.Models;
using MIBServiceFunctionApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MIBServiceFunctionApp
{
    public class MIBFunction
    {
        private readonly SearchService mSearchService;
        private readonly IStorageService mStorageService;
        private readonly KeyVaultService mKvService;
        private readonly ParseMIB mParseMIB;

        public MIBFunction(SearchService searchService, IStorageService storageService, KeyVaultService kvService, ParseMIB parseMIB)
        {
            mSearchService = searchService;
            mStorageService = storageService;
            mKvService = kvService;
            mParseMIB = parseMIB;
        }

        [FunctionName("SearchMIBs")]
        public async Task<IActionResult> SearchMIBs(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "mib/search")] HttpRequest req, ILogger log)
        {
            try
            {
                string query = req.Query["query"];

                // Call the search service to get the MIB results
                var searchResults = await mSearchService.SearchMIBsAsync(query);

                // Transform the results into the desired format
                var finalResults = searchResults.Select(result =>
                {
                    // Extract the original file name without chunking
                    var originalFileName = Utility.GetOriginalFileName(result.MibName);

                    // Return both the name and the exact file name
                    return new
                    {
                        Name = Path.GetFileNameWithoutExtension(originalFileName),
                        FileName = originalFileName
                    };
                }).Distinct(); // Ensure uniqueness if required

                // Return the final results in the expected format
                return new OkObjectResult(finalResults);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while searching for MIB files.");
                return ErrorHandler.HandleException(ex);
            }
        }
        public class FileUploadRequest
        {
            public string FileName { get; set; }
            public string FileContent { get; set; } // Base64-encoded content
        }

        [FunctionName("UploadMIB")]
        public async Task<IActionResult> UploadMIB(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "mib/upload")] HttpRequest req,
            ILogger log)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var fileUploadRequest = JsonConvert.DeserializeObject<FileUploadRequest>(requestBody);
                string fileContent = string.Empty;

                if (fileUploadRequest == null || string.IsNullOrEmpty(fileUploadRequest.FileName) || string.IsNullOrEmpty(fileUploadRequest.FileContent))
                {
                    return new BadRequestObjectResult(new ErrorResponse
                    {
                        ErrorCode = "400",
                        ErrorMessage = "FileName or FileContent is missing in the request."
                    });
                }

                // Decode the Base64 file content
                byte[] byteArray = Convert.FromBase64String(fileUploadRequest.FileContent);
                // Create a MemoryStream from the byte array
                using (MemoryStream stream = new MemoryStream(byteArray))
                {

                    // Optionally reset the position to the beginning of the stream
                    stream.Position = 0;
                    var blobName = fileUploadRequest.FileName;

                    // Check if the MIB file already exists in Blob Storage
                    var containerClient = mStorageService.GetBlobContainerClient();
                    var blobClient = containerClient.GetBlobClient(blobName);

                    if (await blobClient.ExistsAsync())
                    {
                        fileContent = await Utility.GetMibContentAsync(stream, mParseMIB);
                        if (fileContent != null)
                            return new OkObjectResult(fileContent);
                    }
                    await mStorageService.UploadMibAsync(blobName, stream);

                    // If the file size > 50Kb, chunk and upload
                    if (byteArray.Length > 50 * 1024) // 50Kb = 50 * 1024 bytes
                    {
                        // Split the file into chunks and upload each chunk
                        const int chunkSize = 50 * 1024; // 50Kb chunk size
                        int chunkNumber = 0;
                        stream.Position = 0;
                        byte[] buffer = new byte[chunkSize];

                        while (stream.Position < stream.Length)
                        {
                            int bytesRead = await stream.ReadAsync(buffer, 0, chunkSize);

                            var chunkFileName = $"{Path.GetFileNameWithoutExtension(fileUploadRequest.FileName)}_cg_{chunkNumber}.txt";

                            await using (var chunkStream = new MemoryStream(buffer, 0, bytesRead))
                            {
                                await mStorageService.UploadMibAsync(chunkFileName, chunkStream);
                            }

                            chunkNumber++;
                        }
                    }
                    // Optionally reset the position to the beginning of the stream
                    stream.Position = 0;
                    fileContent = await Utility.GetMibContentAsync(stream, mParseMIB);
                }
                return new OkObjectResult(fileContent);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while uploading the MIB file.");
                return ErrorHandler.HandleException(ex);
            }
        }


        [FunctionName("DownloadMIB")]
        public async Task<IActionResult> DownloadMIB(
    [HttpTrigger(AuthorizationLevel.Function, "get", Route = "mib/json/{fileName}")] HttpRequest req, string fileName, ILogger log)
        {
            try
            {
                // Download the MIB file from Blob Storage
                string fileNameWithText = fileName + ".txt";
                string fileNameWithMib = fileName + ".mib";
                string fileContent = string.Empty;

                // Try downloading the .txt file first
                var stream = await mStorageService.DownloadMibAsync(fileNameWithText);

                if (stream == null)
                {
                    // If .txt file doesn't exist or stream is empty, try downloading the .mib file
                    stream = await mStorageService.DownloadMibAsync(fileNameWithMib);

                    // Check again if the stream is null or empty
                    if (stream == null)
                    {
                        return new OkObjectResult($"MIB - {fileNameWithMib} referenced as import module for {fileNameWithMib} was not found.");
                    }
                }

                fileContent = await Utility.GetMibContentAsync(stream,mParseMIB);
                if (fileContent != null)
                    return new OkObjectResult(fileContent);

                // Return if the file content is still empty
                return new OkObjectResult($"MIB - {fileNameWithMib} referenced as import module for {fileNameWithMib} was not found.");
            }
            catch (JsonException jsonEx)
            {
                log.LogError(jsonEx, "An error occurred while converting the processed content to JSON.");
                return new BadRequestObjectResult(new ErrorResponse
                {
                    ErrorCode = "400",
                    ErrorMessage = "Invalid JSON format."
                });
            }
            catch (Exception ex)
            {
                log.LogError(ex, "An error occurred while downloading the MIB file.");
                return ErrorHandler.HandleException(ex);
            }
        }
    }
}


//Error sample

//if (string.IsNullOrEmpty(name))
//{
//    return new BadRequestObjectResult(new ErrorResponse
//    {
//        ErrorCode = "400",
//        ErrorMessage = "The MIB name must be provided."
//    });
//}

//logging sample
//log.LogError(ex, "An error occurred while processing the request.");
