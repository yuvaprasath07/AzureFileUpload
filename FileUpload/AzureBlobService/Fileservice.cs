using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;


namespace AzureBlobService
{
    public class Fileservice : IFileService
    {
        private readonly BlobServiceClient _blobServiceClient;
 
        public Fileservice(BlobServiceClient blobServiceClient)
        {

            _blobServiceClient = blobServiceClient;
        }

        public async Task<List<Azure.Response<BlobContentInfo>>> UploadFiles(List<IFormFile> files)
        {
            if (files.Count <= 0)
                return null;
            var azureResponse = new List<Azure.Response<BlobContentInfo>>();
            foreach (var file in files)
            {
                string fileName = file.FileName;
                using (var memoryStream = new MemoryStream())
                {
                    file.CopyTo(memoryStream);
                    memoryStream.Position = 0;
                    var blobContainer = _blobServiceClient.GetBlobContainerClient("filecontainer");
                    var client = await blobContainer.UploadBlobAsync(fileName, memoryStream, default);
                    azureResponse.Add(client);
                }
            };
            return azureResponse;
        }


        public async Task<string> GetBlobAndSaveToLocalPath(string blobName)
        {
            string localDirectory = @"\\192.168.0.5\vaf\task";
            string localPath = Path.Combine(localDirectory, blobName);

            var blobContainer = _blobServiceClient.GetBlobContainerClient("filecontainer");
            var blobClient = blobContainer.GetBlobClient(blobName);


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


        public async Task<bool> CreateContainerAndUploadFile(string containerName, string folderName, string fileName, Stream fileStream)
        {
            try
            {
                var blobContainer = _blobServiceClient.GetBlobContainerClient(containerName);
               

                string blobName = string.IsNullOrEmpty(folderName) ? fileName : $"{folderName}/{fileName}";
                await blobContainer.CreateIfNotExistsAsync();
                var blobClient = blobContainer.GetBlobClient(blobName);
                await blobClient.UploadAsync(fileStream, overwrite: true);

                return true;
            }
            catch
            {
                return false;
            }
        }


        public async Task<string> ConvertJsonToExcelAndDownload(string filename)
        {
            var blobContainer = _blobServiceClient.GetBlobContainerClient("filecontainer");
            var blobClient = blobContainer.GetBlobClient(filename);
       

            try
            {
                if (!await blobClient.ExistsAsync())
                {
                    return null;
                }

                BlobDownloadInfo blobDownloadInfo = await blobClient.DownloadAsync();

                using (StreamReader reader = new StreamReader(blobDownloadInfo.Content))
                {
                    string content = await reader.ReadToEndAsync();

                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                        JArray dataArray = JArray.Parse(content);

                        if (dataArray.Count > 0)
                        {
                            var firstObject = dataArray[0];
                            var properties = firstObject.Children<JProperty>();
                            string[] fixedHeaders = properties.Select(p => p.Name).ToArray();

                            for (int i = 0; i < fixedHeaders.Length; i++)
                            {
                                worksheet.Cells[1, i + 1].Value = fixedHeaders[i];
                            }
                        }

                        int rowIndex = 2;
                        foreach (var dataObject in dataArray)
                        {
                            int columnIndex = 1;
                            foreach (var property in dataObject)
                            {
                                worksheet.Cells[rowIndex, columnIndex].Value = property;
                                columnIndex++;
                            }
                            rowIndex++;
                        }

                        byte[] excelBytes = package.GetAsByteArray();

                        string tempFileName = Path.GetTempFileName() + ".xlsx";
                        File.WriteAllBytes(tempFileName, excelBytes);

                        return tempFileName;
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public async Task<string> CreateSubfolderAsync(string containerName, string folderPath, string subfolderName)
        {
            try
            {
               
                BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                string subfolderPath = folderPath + "/" + subfolderName;
                BlockBlobClient blobClient = containerClient.GetBlockBlobClient(subfolderPath + "/placeholder.txt");
                using (Stream stream = new MemoryStream())
                {
                    await blobClient.UploadAsync(stream);
                }

                return ($"Subfolder '{subfolderName}' created in folder '{folderPath}' of container '{containerName}'.");
            }
            catch (Exception ex)
            {
                return ($"Error creating subfolder: {ex.Message}");
            }
        }


    }
  

}