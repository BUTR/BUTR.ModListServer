﻿using BUTR.ModListServer.Options;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IO;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using System.Runtime.InteropServices;

namespace BUTR.ModListServer.Controllers;

[ApiController]
[Route("")]
public class AvatarUploadController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly ModListUploadOptions _options;
    private readonly IDistributedCache _cache;
    private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;

    public AvatarUploadController(ILogger<AvatarUploadController> logger, IOptionsSnapshot<ModListUploadOptions> options, IDistributedCache cache, RecyclableMemoryStreamManager recyclableMemoryStreamManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _recyclableMemoryStreamManager = recyclableMemoryStreamManager ?? throw new ArgumentNullException(nameof(recyclableMemoryStreamManager));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    [HttpPost("avatar/upload")]
    [RequestSizeLimit(1024 * 200)]
    [Consumes("application/octet-stream")]
    [ProducesResponseType(typeof(void), StatusCodes.Status200OK, "text/plain")]
    [ProducesResponseType(typeof(void), StatusCodes.Status500InternalServerError, "application/problem+json")]
    public async Task<IActionResult> UploadAsync(CancellationToken ct)
    {
        if (await Image.LoadAsync(HttpContext.Request.Body, ct) is not Image<Rgba32> image || !image.DangerousTryGetSinglePixelMemory(out var memory))
            return BadRequest();

        var converted = Image.LoadPixelData<Bgra32>(MemoryMarshal.Cast<Rgba32, byte>(memory.Span), image.Width, image.Height);
        await using var stream = _recyclableMemoryStreamManager.GetStream();
        await converted.SaveAsWebpAsync(stream, cancellationToken: ct);

        var id = Guid.NewGuid().ToString();
        await _cache.SetAsync($"avatar_{id}", stream.ToArray(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        }, ct);
        return Ok($"{_options.BaseUri}/avatar/{id}.webp");
    }
}