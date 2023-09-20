﻿using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;

namespace AzureBlobService
{
    public class Fileservice: IFileService
    {
        BlobServiceClient _blobClient;
        BlobContainerClient _containerClient;
        string azureConnectionString = "DefaultEndpointsProtocol=https;AccountName=001files;AccountKey=C4x415dewVYLPRID15XW7IbBZ38BzGbm7gtaKyJSoLrdmz5RbCpkHLJgav7xF1hoXt2qwnYbOGaH+AStEwLubA==;EndpointSuffix=core.windows.net";
        public Fileservice()
        {
            _blobClient = new BlobServiceClient(azureConnectionString);
            _containerClient = _blobClient.GetBlobContainerClient("filecontainer");
        }
        public async Task<List<Azure.Response<BlobContentInfo>>> UploadFiles(List<IFormFile> files)
        {
            var azureResponse = new List<Azure.Response<BlobContentInfo>>();
            foreach (var file in files)
            {
                string fileName = file.FileName;
                using (var memoryStream = new MemoryStream())
                {
                    file.CopyTo(memoryStream);
                    memoryStream.Position = 0;
                    var client = await _containerClient.UploadBlobAsync(fileName, memoryStream, default);
                    azureResponse.Add(client);
                }
            };
            return azureResponse;
        }
        public async Task<string> GetBlobAndSaveToLocalPath(string blobName)
        {
            string localDirectory = @"\\192.168.0.5\vaf\task"; 
            string localPath = Path.Combine(localDirectory, blobName);

            BlobClient blobClient = _containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync())
            {
                return null;
            }

            BlobDownloadInfo blobDownloadInfo = await blobClient.DownloadAsync();

           
            Directory.CreateDirectory(localDirectory);

            using (FileStream fs = File.OpenWrite(localPath))
            {
                await blobDownloadInfo.Content.CopyToAsync(fs);
            }

            return localPath;
        }



    }
}