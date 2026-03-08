using GameStore.Api.Data;
using GameStore.Api.DTOs;
using GameStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Endpoints;

public static class GamesEndpoints
{
    private const string GetGameEndpointName = "GetGame";
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedImageExtensionsOrdered = [".png", ".jpg", ".jpeg", ".webp"];

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".webp"
    };

    public static void MapGamesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/games");

        group.MapGet("/", async (GameStoreContext dbContext, IWebHostEnvironment environment) =>
        {
            var games = await dbContext.Games
                .Include(game => game.Genre)
                .AsNoTracking()
                .ToListAsync();

            return games.Select(game => new GameDTO(
                game.Id,
                game.Name,
                game.Genre!.Name,
                game.Price,
                game.ReleaseDate,
                ResolveGameImageUrl(game, environment)
            )).ToList();
        });

        group.MapGet("/{id}", async (int id, GameStoreContext dbContext, IWebHostEnvironment environment) =>
        {
            var game = await dbContext.Games
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id);

            return game is null ? Results.NotFound() : Results.Ok(
                new GameDetailsDTO(
                    game.Id,
                    game.Name,
                    game.GenreId,
                    game.Price,
                    game.ReleaseDate,
                    ResolveGameImageUrl(game, environment)
                )
            );
        }).WithName(GetGameEndpointName);

        group.MapGet("/{id}/cover", async (int id, GameStoreContext dbContext, IWebHostEnvironment environment) =>
        {
            var game = await dbContext.Games
                .Include(item => item.Genre)
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id);

            if (game is null)
            {
                return Results.NotFound();
            }

            var resolvedImageUrl = ResolveGameImageUrl(game, environment);
            if (TryGetLocalUploadPath(resolvedImageUrl, environment, out var filePath))
            {
                return Results.File(filePath, GetContentType(filePath));
            }

            var svg = BuildGeneratedCoverSvg(game.Name, game.Genre?.Name ?? "Game", game.ReleaseDate);
            return Results.Text(svg, "image/svg+xml");
        });

        group.MapPost("/", async (CreateGameDTO newGame, GameStoreContext dbContext) =>
        {
            Game game = new()
            {
                Name = newGame.Name,
                GenreId = newGame.GenreId,
                Price = newGame.Price,
                ReleaseDate = newGame.ReleaseDate
            };

            dbContext.Games.Add(game);
            await dbContext.SaveChangesAsync();

            GameDetailsDTO gameDto = new(
                game.Id,
                game.Name,
                game.GenreId,
                game.Price,
                game.ReleaseDate,
                game.ImageUrl
            );

            return Results.CreatedAtRoute(GetGameEndpointName, new { id = gameDto.Id }, gameDto);
        });

        group.MapPut("/{id}", async (
            int id,
            UpdateGameDTO updatedGame,
            GameStoreContext dbContext) =>
        {
            var existingGame = await dbContext.Games.FindAsync(id);

            if (existingGame is null)
            {
                return Results.NotFound();
            }

            existingGame.Name = updatedGame.Name;
            existingGame.GenreId = updatedGame.GenreId;
            existingGame.Price = updatedGame.Price;
            existingGame.ReleaseDate = updatedGame.ReleaseDate;

            await dbContext.SaveChangesAsync();
            return Results.NoContent();
        });

        group.MapDelete("/{id}", async (int id, GameStoreContext dbContext) =>
        {
            await dbContext.Games
                .Where(game => game.Id == id)
                .ExecuteDeleteAsync();

            return Results.NoContent();
        });

        group.MapPost("/{id}/image", async (
            int id,
            IFormFile file,
            IWebHostEnvironment environment,
            GameStoreContext dbContext) =>
        {
            if (file.Length == 0)
            {
                return Results.BadRequest("Image file is empty.");
            }

            if (file.Length > MaxImageSizeBytes)
            {
                return Results.BadRequest("Image size must be 5MB or less.");
            }

            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(extension) || !AllowedImageExtensions.Contains(extension))
            {
                return Results.BadRequest("Only .png, .jpg, .jpeg, and .webp are allowed.");
            }

            var game = await dbContext.Games.FindAsync(id);
            if (game is null)
            {
                return Results.NotFound();
            }

            var imagesDirectory = GetImagesDirectory(environment);

            if (!string.IsNullOrWhiteSpace(game.ImageUrl))
            {
                var oldFileName = Path.GetFileName(game.ImageUrl);
                var oldFilePath = Path.Combine(imagesDirectory, oldFileName);

                if (File.Exists(oldFilePath))
                {
                    File.Delete(oldFilePath);
                }
            }

            var fileName = $"{id}-{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var filePath = Path.Combine(imagesDirectory, fileName);

            await using (var stream = File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            game.ImageUrl = $"/uploads/games/{fileName}";
            await dbContext.SaveChangesAsync();

            return Results.Ok(new { imageUrl = game.ImageUrl });
        }).DisableAntiforgery();
    }

    private static string GetImagesDirectory(IWebHostEnvironment environment)
    {
        var path = Path.Combine(environment.ContentRootPath, "uploads", "games");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string? ResolveGameImageUrl(Game game, IWebHostEnvironment environment)
    {
        if (!string.IsNullOrWhiteSpace(game.ImageUrl))
        {
            return game.ImageUrl;
        }

        var imagesDirectory = GetImagesDirectory(environment);
        var byId = FindImageByBaseName(imagesDirectory, game.Id.ToString());
        if (byId is not null)
        {
            return $"/uploads/games/{byId}";
        }

        var byName = FindImageByGameName(imagesDirectory, game.Name);
        if (byName is not null)
        {
            return $"/uploads/games/{byName}";
        }

        return null;
    }

    private static bool TryGetLocalUploadPath(string? imageUrl, IWebHostEnvironment environment, out string path)
    {
        path = string.Empty;

        if (string.IsNullOrWhiteSpace(imageUrl) ||
            !imageUrl.StartsWith("/uploads/games/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var fileName = Path.GetFileName(imageUrl);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var candidate = Path.Combine(GetImagesDirectory(environment), fileName);
        if (!File.Exists(candidate))
        {
            return false;
        }

        path = candidate;
        return true;
    }

    private static string GetContentType(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static string? FindImageByBaseName(string directory, string baseName)
    {
        foreach (var extension in AllowedImageExtensionsOrdered)
        {
            var fileName = $"{baseName}{extension}";
            var fullPath = Path.Combine(directory, fileName);
            if (File.Exists(fullPath))
            {
                return fileName;
            }
        }

        return null;
    }

    private static string? FindImageByGameName(string directory, string gameName)
    {
        var desiredKey = NormalizeMatchKey(gameName);
        if (string.IsNullOrWhiteSpace(desiredKey))
        {
            return null;
        }

        foreach (var filePath in Directory.EnumerateFiles(directory))
        {
            var extension = Path.GetExtension(filePath);
            if (!AllowedImageExtensions.Contains(extension))
            {
                continue;
            }

            var fileName = Path.GetFileName(filePath);
            var fileKey = NormalizeMatchKey(Path.GetFileNameWithoutExtension(fileName));

            if (string.Equals(fileKey, desiredKey, StringComparison.OrdinalIgnoreCase))
            {
                return fileName;
            }
        }

        return null;
    }

    private static string NormalizeMatchKey(string value)
    {
        var chars = value
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray();

        return new string(chars);
    }

    private static string BuildGeneratedCoverSvg(string title, string genre, DateOnly releaseDate)
    {
        var safeTitle = EscapeXml(title);
        var safeGenre = EscapeXml(genre);
        var year = releaseDate.Year;
        var shortTitle = safeTitle.Length > 36 ? $"{safeTitle[..36]}..." : safeTitle;

        return $"""
               <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1000 1200">
                 <defs>
                   <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
                     <stop offset="0%" stop-color="#6f5bff"/>
                     <stop offset="100%" stop-color="#1ca9ff"/>
                   </linearGradient>
                 </defs>
                 <rect width="1000" height="1200" fill="url(#bg)"/>
                 <circle cx="810" cy="180" r="200" fill="#ffffff" opacity="0.16"/>
                 <circle cx="190" cy="1040" r="180" fill="#ffffff" opacity="0.1"/>
                 <rect x="56" y="56" width="888" height="1088" rx="34" fill="rgba(7,11,20,0.26)"/>
                 <text x="96" y="868" fill="#fff" font-size="68" font-weight="700" font-family="Trebuchet MS, Segoe UI, sans-serif">{shortTitle}</text>
                 <text x="96" y="946" fill="#eef7ff" font-size="38" font-weight="700" font-family="Trebuchet MS, Segoe UI, sans-serif">{safeGenre}</text>
                 <text x="96" y="1000" fill="#d8efff" font-size="32" font-weight="600" font-family="Trebuchet MS, Segoe UI, sans-serif">Released {year}</text>
               </svg>
               """;
    }

    private static string EscapeXml(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&apos;", StringComparison.Ordinal);
    }
}
