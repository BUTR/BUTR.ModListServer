using BUTR.ModListServer.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

using System.Text.Json;

namespace BUTR.ModListServer.Controllers
{
    [Route("")]
    public class AvatarController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IDistributedCache _cache;

        public AvatarController(ILogger<AvatarController> logger, IDistributedCache cache)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        [HttpGet]
        [Route("avatar/{id:guid}.png")]
        [ProducesResponseType(typeof(void), StatusCodes.Status200OK, "text/plain")]
        [ProducesResponseType(typeof(void), StatusCodes.Status500InternalServerError, "application/problem+json")]
        public async Task<IActionResult> IndexAsync(Guid id, CancellationToken ct)
        {
            if (await _cache.GetAsync($"avatar_{id}", ct) is not { } data)
                return NotFound();

            return File(data, "text/plain");
        }
    }
}