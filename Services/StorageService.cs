using Azure.Storage.Blobs;
using System.IO;
using System.Threading.Tasks;

namespace MIBServiceFunctionApp.Services
{
    public interface IStorageService
    {
        Task UploadMibAsync(string blobName, Stream fileStream);
        Task<Stream> DownloadMibAsync(string blobName);
        BlobContainerClient GetBlobContainerClient();
    }

    public class StorageService : IStorageService
    {
        private readonly BlobServiceClient mBlobServiceClient;
        private readonly string mContainerName = "mibs";

        public StorageService(string connectionString)
        {
            mBlobServiceClient = new BlobServiceClient(connectionString);
        }

        public BlobContainerClient GetBlobContainerClient()
        {
            return mBlobServiceClient.GetBlobContainerClient(mContainerName);
        }

        public async Task UploadMibAsync(string blobName, Stream fileStream)
        {
            var containerClient = GetBlobContainerClient();
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(fileStream, overwrite: true);
        }

        public async Task<Stream> DownloadMibAsync(string blobName)
        {
            var containerClient = GetBlobContainerClient();
            var blobClient = containerClient.GetBlobClient(blobName);
            // Check if the blob exists
            bool exists = await blobClient.ExistsAsync();
            if (exists)
            {
                var response = await blobClient.DownloadAsync();
                return response.Value.Content;
            }
            else
            {
                return null;
            }
        }
    }
}
