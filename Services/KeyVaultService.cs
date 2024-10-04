using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace MIBServiceFunctionApp.Services
{
    public class KeyVaultService
    {
        private readonly SecretClient mSecretClient;

        public KeyVaultService(IConfiguration configuration)
        {
            // Read Key Vault URL from configuration
            string keyVaultUri = configuration["KeyVaultUri"];

            if (string.IsNullOrEmpty(keyVaultUri))
            {
                throw new InvalidOperationException("Key Vault URI is not configured.");
            }

            // Use DefaultAzureCredential to authenticate
            mSecretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
        }

        public async Task<string> GetSecret(string secretName)
        {
            try
            {
                var secret = await mSecretClient.GetSecretAsync(secretName);
                return secret.Value.Value;
            }
            catch (Exception ex)
            {
                // Handle exception (e.g., secret not found, network issues)
                throw new InvalidOperationException($"Could not retrieve secret '{secretName}' from Key Vault", ex);
            }
        }
    }
}
