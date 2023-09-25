using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OfficeOpenXml;


namespace AzureBlobService
{
    public class MyDataObject
    {
        public string Property1 { get; set; }
        public string Property2 { get; set; }
        // Add more properties as needed
    }
    public class Fileservice : IFileService
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


        public async Task<bool> CreateContainerAndUploadFile(string containerName, string fileName, Stream fileStream)
        {
            try
            {

                BlobContainerClient containerClient = _blobClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                await containerClient.UploadBlobAsync(fileName, fileStream);

                return true;
            }
            catch
            {
                return false;
            }
        }


        public async Task<bool> CreateContainerAndUploadFile(string containerName, string folderName, string fileName, Stream fileStream)
        {
            try
            {
                string blobName = string.IsNullOrEmpty(folderName) ? fileName : $"{folderName}/{fileName}";

                BlobContainerClient containerClient = _blobClient.GetBlobContainerClient(containerName);
                await containerClient.CreateIfNotExistsAsync();

                BlobClient blobClient = containerClient.GetBlobClient(blobName);
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
            BlobClient blobClient = _containerClient.GetBlobClient(filename);

            try
            {
                if (!await blobClient.ExistsAsync())
                {
                    return null; // Blob doesn't exist, return early
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

                        // Extract headers from the first JSON object in the array
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

    }
}