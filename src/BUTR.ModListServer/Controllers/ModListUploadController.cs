using BUTR.ModListServer.Models;
using BUTR.ModListServer.Options;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IO;

using System.Text.Json;

namespace BUTR.ModListServer.Controllers
{
    [ApiController]
    [Route("/")]
    public class ModListUploadController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly ModListUploadOptions _options;
        private readonly IDistributedCache _cache;
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

        public ModListUploadController(ILogger<ModListUploadController> logger, IOptionsSnapshot<ModListUploadOptions> options, IDistributedCache cache, RecyclableMemoryStreamManager recyclableMemoryStreamManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _recyclableMemoryStreamManager = recyclableMemoryStreamManager ?? throw new ArgumentNullException(nameof(recyclableMemoryStreamManager));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        }

        [HttpPost("upload")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK, "text/plain")]
        [ProducesResponseType(typeof(void), StatusCodes.Status500InternalServerError, "application/problem+json")]
        public async Task<IActionResult> UploadAsync([FromBody] ModList modList, CancellationToken ct)
        {
            var guid = Guid.NewGuid().ToString();
            var json = JsonSerializer.Serialize(modList);
            await _cache.SetStringAsync(guid, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8)
            }, ct);
            return Ok($"{_options.BaseUri}/{guid}");
        }
        
        [HttpPost("upload_avatar")]
        [Consumes("text/plain")]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK, "text/plain")]
        [ProducesResponseType(typeof(void), StatusCodes.Status500InternalServerError, "application/problem+json")]
        public async Task<IActionResult> UploadAsync(CancellationToken ct)
        {
            if (HttpContext.Request.Body.Length > 1024 * 1024)
                return BadRequest();

            var image = await Image.LoadAsync<Bgra32>(HttpContext.Request.Body, ct);
            using var stream = _recyclableMemoryStreamManager.GetStream();
            await image.SaveAsPngAsync(stream, cancellationToken: ct);
            
            var guid = Guid.NewGuid().ToString();
            await _cache.SetAsync(guid, stream.ToArray(), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8)
            }, ct);
            return Ok($"{_options.BaseUri}/{guid}");
        }
    }
}