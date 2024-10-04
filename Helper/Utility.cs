using Newtonsoft.Json;
using System.Collections;
using System.IO;
using System.Threading.Tasks;

namespace MIBServiceFunctionApp
{
    public static class Utility
    {
        public static string GetOriginalFileName(string fileName)
        {
            // If the file name matches the chunked pattern, return the original MIB file name
            var chunkPattern = "_cg_";
            if (fileName.Contains(chunkPattern))
            {
                // Remove the chunk pattern and anything after it to get the original file name
                return fileName.Substring(0, fileName.IndexOf(chunkPattern)) + ".mib";
            }
            return Path.ChangeExtension(fileName, ".mib"); 
        }

        public static async Task<string> GetMibContentAsync(Stream stream, ParseMIB mParseMIB)
        {
            string fileContent = string.Empty;
            string jsonResult = string.Empty;
            // Read the file content as a string if stream is not null or empty
            using (var reader = new StreamReader(stream))
            {
                fileContent = await reader.ReadToEndAsync();
            }

            // If the file content is not empty, process it
            if (!string.IsNullOrEmpty(fileContent))
            {
                // Process the file content using the external function
                IDictionary processedContent = mParseMIB.ParseMIBFileContents(fileContent);

                // Convert the processed content to a JSON object
                jsonResult = JsonConvert.SerializeObject(processedContent, Formatting.Indented);
            }
            return jsonResult;
        }       
    }
}
