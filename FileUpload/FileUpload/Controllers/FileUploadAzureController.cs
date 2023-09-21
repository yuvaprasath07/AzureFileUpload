﻿using Azure.Storage.Blobs;
using AzureBlobService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FileUpload.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileUploadAzureController : ControllerBase
    {
        IFileService _service;
        public FileUploadAzureController(IFileService service)
        {
            _service = service;
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

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFiles(List<IFormFile> files, string containerName)
        {
            try
            {
                if (string.IsNullOrEmpty(containerName))
                {
                    return BadRequest("Container name is required.");
                }

                foreach (var file in files)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        file.CopyTo(memoryStream);
                        memoryStream.Position = 0;

                        bool uploadResult = await _service.CreateContainerAndUploadFile(containerName, file.FileName, memoryStream);

                        if (!uploadResult)
                        {
                            return BadRequest($"Failed to upload {file.FileName} to container {containerName}");
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

    }
}
