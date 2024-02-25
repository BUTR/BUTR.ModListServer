using BUTR.ModListServer.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

using System.Text.Json;

namespace BUTR.ModListServer.Controllers;

[Route("")]
public class ModListController : Controller
{
    private readonly ILogger _logger;
    private readonly IDistributedCache _cache;

    public ModListController(ILogger<ModListController> logger, IDistributedCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    [HttpGet]
    [Route("{id:guid}")]
    public async Task<IActionResult> IndexAsync(string id, CancellationToken ct)
    {
        if (await _cache.GetStringAsync(id, ct) is not { } json || JsonSerializer.Deserialize<ModList>(json) is not { } modList)
            return NotFound();

        return View(new ModListModel { Id = id, ModList = modList });
    }
}