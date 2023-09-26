using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureBlobService
{
    public interface IFileService
    {
        public Task<List<Azure.Response<BlobContentInfo>>> UploadFiles(List<IFormFile> files);

        public Task<string> GetBlobAndSaveToLocalPath(string blobName);

        public Task<bool> CreateContainerAndUploadFile(string containerName, string folderName, string fileName, Stream fileStream);

        public Task<string> ConvertJsonToExcelAndDownload(string blobName);


        public  Task<string> CreateSubfolderAsync(string containerName, string folderPath, string subfolderName);
    }
}
