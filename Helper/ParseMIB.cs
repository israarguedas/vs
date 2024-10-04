using nsoftware.IPWorksSNMP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using MIBServiceFunctionApp.Services;
using System.IO;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;

namespace MIBServiceFunctionApp
{
    public class ParseMIB
    {
        private readonly IStorageService mStorageService;
        private readonly string _containerName;
        private List<dynamic> mibTreeObject;
        private nsoftware.IPWorksSNMP.MibBrowser mibBrowser = new nsoftware.IPWorksSNMP.MibBrowser();
        private readonly IConfiguration mConfiguration;

        public ParseMIB(IStorageService storageService, IConfiguration config)
        {
            mStorageService = storageService;
            mConfiguration = config;
        }
        /// <summary>
        /// Method that will take the given file contents and parse the MIB tree
        /// </summary>
        /// <param name="fileContent">Content of the file to be parsed</param>
        /// <returns>JSON format of the MIB tree</returns>
        public IDictionary ParseMIBFileContents(string fileContent)
        {
            mibBrowser.RuntimeLicense = mConfiguration["LicenceKey"];
            IDictionary result = new Hashtable();

            if (fileContent != null && fileContent.Length > 0)
            {
                mibTreeObject = new List<dynamic>();
                mibBrowser.Reset();
                mibBrowser.OnMibNode += FireOnMibNode;
                // Add this line right below the OnMibNode line
                mibBrowser.OnImportSymbols += FireOnImportSymbols;
                try
                {
                    mibBrowser.LoadMib(fileContent);
                    mibBrowser.ListSuccessors();
                    mibBrowser.ListChildren();
                    result["data"] = mibTreeObject;
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(ex.Message);
                }

            }
            return result;
        }

        private void FireOnMibNode(object sender, MibBrowserMibNodeEventArgs e)
        {
            dynamic nodeInfo = new ExpandoObject();
            nodeInfo.Access = e.NodeAccess;
            nodeInfo.Description = e.NodeDescription;
            nodeInfo.Index = e.NodeIndex;
            nodeInfo.Module = e.NodeModuleName;
            nodeInfo.Name = e.NodeLabel;
            nodeInfo.OID = e.NodeOid;
            nodeInfo.ParentName = e.NodeParentName;
            nodeInfo.Syntax = e.NodeSyntaxString;
            nodeInfo.Type = e.NodeTypeString;

            mibTreeObject.Add(nodeInfo);

            mibBrowser.SelectNode(nodeInfo.OID);
            mibBrowser.ListChildren();
        }

        private async void FireOnImportSymbols(object sender, MibBrowserImportSymbolsEventArgs e)
        {
            string missingModuleNameTxt = e.ModuleName + ".txt";
            string missingModuleNameMib = e.ModuleName + ".mib";
           
            var stream = await mStorageService.DownloadMibAsync(missingModuleNameMib);
            if(stream == null)
            {
                stream = await mStorageService.DownloadMibAsync(missingModuleNameTxt);
                if (stream !=null)
                {
                    RetrieveFileContentOrPathAsync(stream);
                }                
            }            
        }

        private async Task<bool> SearchFileInAzureSearchAsync(string missingModuleName)
        {
            // Get a reference to the blob
            // Check if the MIB file already exists in Blob Storage
            var containerClient = mStorageService.GetBlobContainerClient();
            BlobClient blobClient = containerClient.GetBlobClient(missingModuleName);

            // Check if the blob exists
            bool exists = await blobClient.ExistsAsync();

            // Return true if the blob exists, otherwise false
            return exists;

        }

        private async void RetrieveFileContentOrPathAsync(Stream stream)
        {            
            // Read the file content as a string
            string fileContent;
            using (var reader = new StreamReader(stream))
            {
                fileContent = await reader.ReadToEndAsync();
                mibBrowser.LoadMib(fileContent);
            }
        }

    }
}
