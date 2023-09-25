using Azure.Storage.Blobs;
using AzureBlobService;
using Microsoft.AspNetCore.Mvc;

namespace FileUpload.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadAzureController : ControllerBase
    {
        IFileService _service;
        private readonly BlobServiceClient _blobServiceClient;

        public FileUploadAzureController(IFileService service, BlobServiceClient blobServiceClient)
        {
            _service = service;
            _blobServiceClient = blobServiceClient;
        }

        [HttpPost]
        public async Task<IActionResult> UploadBlobs(List<IFormFile> files)
        {
            var response = await _service.UploadFiles(files);
            if (response == null)
            {
                return BadRequest();
            }
            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBlobs(string filename)
        {
            var response = await _service.GetBlobAndSaveToLocalPath(filename);
            return Ok(response);
        }

        

        [HttpPost("FolderCreate")]
        public async Task<IActionResult> UploadFiles([FromForm] List<IFormFile> files, [FromQuery] string containerName, [FromQuery] string folderName)
        {
            try
            {
                if (string.IsNullOrEmpty(containerName))
                {
                    return BadRequest("Container name is required.");
                }

                foreach (var file in files)
                {
                    if (file.Length == 0)
                    {
                        continue;
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        file.CopyTo(memoryStream);
                        memoryStream.Position = 0;

                        bool uploadResult = await _service.CreateContainerAndUploadFile(containerName, folderName, file.FileName, memoryStream);

                        if (!uploadResult)
                        {
                            return BadRequest($"Failed to upload {file.FileName} to container {containerName}/{folderName}");
                        }
                    }
                }

                return Ok("Files uploaded successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("download/{filename}")]
        public async Task<IActionResult> DownloadBlobAsExcel(string filename)
        {
            string filePath = await _service.ConvertJsonToExcelAndDownload(filename);   

            if (filePath == null)
            {
                return NotFound(); // Or handle the error appropriately
            }
            return PhysicalFile(filePath, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "studentdata.xlsx");
        }
    }
}

