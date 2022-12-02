using System.Text.Json;

using BUTR.ModListServer.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace BUTR.ModListServer.Controllers
{
    [ApiController]
    [Route("/")]
    public class ModListUploadController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IDistributedCache _cache;

        public ModListUploadController(ILogger<ModListUploadController> logger, IDistributedCache cache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
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
            return Ok(guid);
        }
    }
}