using System.Text;
using System.Text.Json;
using BUTR.ModListServer.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

namespace BUTR.ModListServer.Controllers
{
    [ApiController]
    [Route("")]
    public class ModListAPIController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IDistributedCache _cache;

        public ModListAPIController(ILogger<ModListAPIController> logger, IDistributedCache cache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [HttpGet("raw/{id:guid}")]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK, "application/json")]
        [ProducesResponseType(typeof(void), StatusCodes.Status500InternalServerError, "application/problem+json")]
        public async Task<IActionResult> RawAsync(string id, CancellationToken ct) =>
            await _cache.GetStringAsync(id, ct) is { } json ? Ok(json) : NotFound();

        [HttpGet("butrloader/{id:guid}")]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK, "application/json")]
        [ProducesResponseType(typeof(void), StatusCodes.Status500InternalServerError, "application/problem+json")]
        public async Task<IActionResult> BUTRLoaderAsync(string id, CancellationToken ct)
        {
            if (await _cache.GetStringAsync(id, ct) is not { } json || JsonSerializer.Deserialize<ModList>(json) is not { } modList)
                return NotFound();

            var sb = new StringBuilder();
            foreach (var module in modList.Modules)
                sb.AppendLine($"{module.Id}: {module.Version}");
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/plain", "MyList.bmlist");
        }
    }
}