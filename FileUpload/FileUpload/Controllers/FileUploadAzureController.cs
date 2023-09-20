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
            return Ok(response);
        }
        [HttpGet]
        public async Task<IActionResult> GetAllBlobs(string filename)
        {

            var response = await _service.GetBlobAndSaveToLocalPath(filename);
            return Ok(response);
        }
    }
}
