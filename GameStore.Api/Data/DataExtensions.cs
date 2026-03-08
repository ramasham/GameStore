using GameStore.Api.Models;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Data;

public static class DataExtensions
{
    public static void MigrateDb(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GameStoreContext>();
        dbContext.Database.Migrate();
    }

    public static void SeedGamesFromCoverFolder(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GameStoreContext>();

        var coversDirectory = Path.Combine(app.Environment.ContentRootPath, "uploads", "games");
        Directory.CreateDirectory(coversDirectory);

        var supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".webp"
        };

        var platformerGenreId = dbContext.Genres
            .AsNoTracking()
            .Where(genre => genre.Name == "Platformer")
            .Select(genre => genre.Id)
            .FirstOrDefault();

        if (platformerGenreId == 0)
        {
            return;
        }

        var existingNameKeys = dbContext.Games
            .AsNoTracking()
            .Select(game => NormalizeKey(game.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var importCandidates = Directory.EnumerateFiles(coversDirectory)
            .Where(path => supportedExtensions.Contains(Path.GetExtension(path)))
            .Select(path =>
            {
                var baseName = Path.GetFileNameWithoutExtension(path);
                return new { BaseName = baseName, Key = NormalizeKey(baseName) };
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Key))
            .GroupBy(item => item.Key)
            .Select(group => group.First())
            .ToList();

        var gamesToAdd = new List<Game>();
        for (var index = 0; index < importCandidates.Count; index++)
        {
            var item = importCandidates[index];
            if (existingNameKeys.Contains(item.Key))
            {
                continue;
            }

            gamesToAdd.Add(new Game
            {
                Name = ToDisplayName(item.BaseName),
                GenreId = platformerGenreId,
                Price = Math.Round(14.99m + (index % 8) * 1.25m, 2),
                ReleaseDate = new DateOnly(1990, 1, 1)
            });

            existingNameKeys.Add(item.Key);
        }

        if (gamesToAdd.Count == 0)
        {
            return;
        }

        dbContext.Games.AddRange(gamesToAdd);
        dbContext.SaveChanges();
    }

    public static void AddGameStoreDb(this WebApplicationBuilder builder)
    {
        var connString = builder.Configuration.GetConnectionString("GameStore");

        builder.Services.AddSqlite<GameStoreContext>(
            connString,
            optionsAction: options => options.UseSeeding((context, _) =>
            {
                SeedGenres(context);
                SeedClassicGames(context);
            })
        );
    }

    private static void SeedGenres(DbContext context)
    {
        var existingGenres = context.Set<Genre>()
            .AsNoTracking()
            .Select(genre => genre.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var requiredGenres = new[] { "Fighting", "RPG", "Platformer", "Racing", "Sports" };
        var genresToAdd = requiredGenres
            .Where(genreName => !existingGenres.Contains(genreName))
            .Select(genreName => new Genre { Name = genreName })
            .ToArray();

        if (genresToAdd.Length == 0)
        {
            return;
        }

        context.Set<Genre>().AddRange(genresToAdd);
        context.SaveChanges();
    }

    private static void SeedClassicGames(DbContext context)
    {
        var genresByName = context.Set<Genre>()
            .AsNoTracking()
            .ToDictionary(genre => genre.Name, genre => genre.Id, StringComparer.OrdinalIgnoreCase);

        if (genresByName.Count == 0)
        {
            return;
        }

        var existingGameNames = context.Set<Game>()
            .AsNoTracking()
            .Select(game => game.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var gamesToAdd = new List<Game>();

        for (var i = 0; i < ClassicCatalog.Length; i++)
        {
            var seed = ClassicCatalog[i];

            if (existingGameNames.Contains(seed.Name))
            {
                continue;
            }

            if (!genresByName.TryGetValue(seed.GenreName, out var genreId))
            {
                continue;
            }

            gamesToAdd.Add(new Game
            {
                Name = seed.Name,
                GenreId = genreId,
                Price = GetSeedPrice(seed.GenreName, i),
                ReleaseDate = seed.ReleaseDate
            });
        }

        if (gamesToAdd.Count == 0)
        {
            return;
        }

        context.Set<Game>().AddRange(gamesToAdd);
        context.SaveChanges();
    }

    private static decimal GetSeedPrice(string genreName, int index)
    {
        var basePrice = genreName switch
        {
            "RPG" => 29.99m,
            "Fighting" => 24.99m,
            "Racing" => 22.99m,
            "Platformer" => 19.99m,
            "Sports" => 18.99m,
            _ => 19.99m
        };

        return Math.Round(basePrice + (index % 6) * 1.75m, 2);
    }

    private static string NormalizeKey(string value)
    {
        var chars = value
            .ToLowerInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray();

        return new string(chars);
    }

    private static string ToDisplayName(string value)
    {
        var chars = value
            .Select(character => char.IsLetterOrDigit(character) ? character : ' ')
            .ToArray();

        var normalizedSpaces = string.Join(
            ' ',
            new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries)
        );

        if (string.IsNullOrWhiteSpace(normalizedSpaces))
        {
            return "Unknown Game";
        }

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalizedSpaces.ToLowerInvariant());
    }

    private sealed record SeedGameDefinition(string Name, string GenreName, DateOnly ReleaseDate);

    private static readonly SeedGameDefinition[] ClassicCatalog =
    [
        // Platformer
        new SeedGameDefinition("Super Mario Bros.", "Platformer", new DateOnly(1985, 9, 13)),
        new SeedGameDefinition("Super Mario Bros. 2", "Platformer", new DateOnly(1988, 10, 9)),
        new SeedGameDefinition("Super Mario Bros. 3", "Platformer", new DateOnly(1988, 10, 23)),
        new SeedGameDefinition("Super Mario World", "Platformer", new DateOnly(1990, 11, 21)),
        new SeedGameDefinition("Super Mario World 2: Yoshi's Island", "Platformer", new DateOnly(1995, 8, 5)),
        new SeedGameDefinition("Donkey Kong", "Platformer", new DateOnly(1981, 7, 9)),
        new SeedGameDefinition("Donkey Kong Country", "Platformer", new DateOnly(1994, 11, 21)),
        new SeedGameDefinition("Donkey Kong Country 2", "Platformer", new DateOnly(1995, 11, 21)),
        new SeedGameDefinition("Donkey Kong Country 3", "Platformer", new DateOnly(1996, 11, 23)),
        new SeedGameDefinition("Sonic the Hedgehog", "Platformer", new DateOnly(1991, 6, 23)),
        new SeedGameDefinition("Sonic the Hedgehog 2", "Platformer", new DateOnly(1992, 11, 21)),
        new SeedGameDefinition("Sonic 3 & Knuckles", "Platformer", new DateOnly(1994, 10, 18)),
        new SeedGameDefinition("Mega Man 2", "Platformer", new DateOnly(1988, 12, 24)),
        new SeedGameDefinition("Mega Man X", "Platformer", new DateOnly(1993, 12, 17)),
        new SeedGameDefinition("Castlevania", "Platformer", new DateOnly(1986, 9, 26)),
        new SeedGameDefinition("Castlevania: Symphony of the Night", "Platformer", new DateOnly(1997, 3, 20)),
        new SeedGameDefinition("Prince of Persia", "Platformer", new DateOnly(1989, 10, 3)),
        new SeedGameDefinition("Rayman", "Platformer", new DateOnly(1995, 9, 1)),
        new SeedGameDefinition("Earthworm Jim", "Platformer", new DateOnly(1994, 6, 9)),
        new SeedGameDefinition("Contra", "Platformer", new DateOnly(1987, 2, 20)),
        new SeedGameDefinition("Battletoads", "Platformer", new DateOnly(1991, 6, 1)),
        new SeedGameDefinition("Ghosts 'n Goblins", "Platformer", new DateOnly(1985, 9, 19)),
        new SeedGameDefinition("Jackal", "Platformer", new DateOnly(1988, 9, 26)),
        new SeedGameDefinition("Kirby's Adventure", "Platformer", new DateOnly(1993, 3, 23)),
        new SeedGameDefinition("Life Force", "Platformer", new DateOnly(1987, 7, 1)),
        new SeedGameDefinition("Mega Man 6", "Platformer", new DateOnly(1993, 11, 5)),
        new SeedGameDefinition("Rocket Knight Adventures", "Platformer", new DateOnly(1993, 10, 1)),
        new SeedGameDefinition("Shadow Dancer: The Secret of Shinobi", "Platformer", new DateOnly(1989, 12, 1)),
        new SeedGameDefinition("Snake Rattle 'n' Roll", "Platformer", new DateOnly(1990, 4, 1)),

        // RPG
        new SeedGameDefinition("Final Fantasy", "RPG", new DateOnly(1987, 12, 18)),
        new SeedGameDefinition("Final Fantasy IV", "RPG", new DateOnly(1991, 7, 19)),
        new SeedGameDefinition("Final Fantasy VI", "RPG", new DateOnly(1994, 4, 2)),
        new SeedGameDefinition("Final Fantasy VII", "RPG", new DateOnly(1997, 1, 31)),
        new SeedGameDefinition("Chrono Trigger", "RPG", new DateOnly(1995, 3, 11)),
        new SeedGameDefinition("Secret of Mana", "RPG", new DateOnly(1993, 8, 6)),
        new SeedGameDefinition("EarthBound", "RPG", new DateOnly(1994, 8, 27)),
        new SeedGameDefinition("Dragon Quest III", "RPG", new DateOnly(1988, 2, 10)),
        new SeedGameDefinition("Dragon Quest V", "RPG", new DateOnly(1992, 9, 27)),
        new SeedGameDefinition("Phantasy Star IV", "RPG", new DateOnly(1993, 12, 17)),
        new SeedGameDefinition("Pokemon Red", "RPG", new DateOnly(1996, 2, 27)),
        new SeedGameDefinition("Pokemon Gold", "RPG", new DateOnly(1999, 11, 21)),
        new SeedGameDefinition("The Legend of Zelda", "RPG", new DateOnly(1986, 2, 21)),
        new SeedGameDefinition("Zelda II: The Adventure of Link", "RPG", new DateOnly(1987, 1, 14)),
        new SeedGameDefinition("The Legend of Zelda: A Link to the Past", "RPG", new DateOnly(1991, 11, 21)),
        new SeedGameDefinition("The Legend of Zelda: Link's Awakening", "RPG", new DateOnly(1993, 6, 6)),
        new SeedGameDefinition("The Legend of Zelda: Ocarina of Time", "RPG", new DateOnly(1998, 11, 21)),
        new SeedGameDefinition("The Legend of Zelda: Majora's Mask", "RPG", new DateOnly(2000, 4, 27)),
        new SeedGameDefinition("Super Mario RPG", "RPG", new DateOnly(1996, 3, 9)),
        new SeedGameDefinition("Baldur's Gate", "RPG", new DateOnly(1998, 12, 21)),
        new SeedGameDefinition("StarTropics", "RPG", new DateOnly(1990, 12, 1)),

        // Fighting
        new SeedGameDefinition("Street Fighter II", "Fighting", new DateOnly(1991, 2, 6)),
        new SeedGameDefinition("Street Fighter Alpha 2", "Fighting", new DateOnly(1996, 2, 6)),
        new SeedGameDefinition("Mortal Kombat", "Fighting", new DateOnly(1992, 10, 8)),
        new SeedGameDefinition("Mortal Kombat II", "Fighting", new DateOnly(1993, 10, 25)),
        new SeedGameDefinition("Tekken 3", "Fighting", new DateOnly(1997, 3, 20)),
        new SeedGameDefinition("Virtua Fighter 2", "Fighting", new DateOnly(1994, 11, 1)),
        new SeedGameDefinition("The King of Fighters '98", "Fighting", new DateOnly(1998, 7, 23)),
        new SeedGameDefinition("Samurai Shodown II", "Fighting", new DateOnly(1994, 10, 28)),
        new SeedGameDefinition("Soulcalibur", "Fighting", new DateOnly(1998, 7, 30)),
        new SeedGameDefinition("Marvel vs. Capcom 2", "Fighting", new DateOnly(2000, 3, 30)),
        new SeedGameDefinition("Killer Instinct", "Fighting", new DateOnly(1994, 10, 28)),
        new SeedGameDefinition("Guilty Gear", "Fighting", new DateOnly(1998, 5, 14)),
        new SeedGameDefinition("Fatal Fury Special", "Fighting", new DateOnly(1993, 9, 16)),
        new SeedGameDefinition("Darkstalkers 3", "Fighting", new DateOnly(1997, 12, 18)),
        new SeedGameDefinition("Super Smash Bros.", "Fighting", new DateOnly(1999, 1, 21)),
        new SeedGameDefinition("Super Street Fighter II Turbo", "Fighting", new DateOnly(1994, 2, 23)),
        new SeedGameDefinition("Dead or Alive 2", "Fighting", new DateOnly(1999, 10, 6)),
        new SeedGameDefinition("ClayFighter", "Fighting", new DateOnly(1993, 11, 19)),
        new SeedGameDefinition("Battle Arena Toshinden", "Fighting", new DateOnly(1995, 1, 1)),
        new SeedGameDefinition("Bloody Roar 2", "Fighting", new DateOnly(1999, 3, 1)),

        // Racing
        new SeedGameDefinition("Out Run", "Racing", new DateOnly(1986, 9, 1)),
        new SeedGameDefinition("Pole Position", "Racing", new DateOnly(1982, 9, 1)),
        new SeedGameDefinition("F-Zero", "Racing", new DateOnly(1990, 11, 21)),
        new SeedGameDefinition("Super Mario Kart", "Racing", new DateOnly(1992, 8, 27)),
        new SeedGameDefinition("Mario Kart 64", "Racing", new DateOnly(1996, 12, 14)),
        new SeedGameDefinition("Diddy Kong Racing", "Racing", new DateOnly(1997, 11, 14)),
        new SeedGameDefinition("Top Gear", "Racing", new DateOnly(1992, 3, 27)),
        new SeedGameDefinition("Road Rash", "Racing", new DateOnly(1991, 9, 1)),
        new SeedGameDefinition("Micro Machines", "Racing", new DateOnly(1991, 1, 1)),
        new SeedGameDefinition("Daytona USA", "Racing", new DateOnly(1994, 4, 1)),
        new SeedGameDefinition("Ridge Racer", "Racing", new DateOnly(1993, 10, 1)),
        new SeedGameDefinition("Need for Speed III: Hot Pursuit", "Racing", new DateOnly(1998, 5, 1)),
        new SeedGameDefinition("Gran Turismo", "Racing", new DateOnly(1997, 12, 23)),
        new SeedGameDefinition("Gran Turismo 2", "Racing", new DateOnly(1999, 12, 11)),
        new SeedGameDefinition("Sega Rally Championship", "Racing", new DateOnly(1994, 11, 24)),
        new SeedGameDefinition("Cruis'n USA", "Racing", new DateOnly(1994, 12, 1)),
        new SeedGameDefinition("Wave Race 64", "Racing", new DateOnly(1996, 9, 27)),
        new SeedGameDefinition("Wipeout XL", "Racing", new DateOnly(1996, 8, 30)),
        new SeedGameDefinition("Micro Machines 2: Turbo Tournament", "Racing", new DateOnly(1994, 11, 1)),
        new SeedGameDefinition("Rock n' Roll Racing", "Racing", new DateOnly(1993, 6, 4)),
        new SeedGameDefinition("Excitebike", "Racing", new DateOnly(1984, 11, 30)),

        // Sports
        new SeedGameDefinition("NBA Jam", "Sports", new DateOnly(1993, 6, 4)),
        new SeedGameDefinition("International Superstar Soccer Deluxe", "Sports", new DateOnly(1995, 11, 1)),
        new SeedGameDefinition("FIFA International Soccer", "Sports", new DateOnly(1993, 12, 15)),
        new SeedGameDefinition("FIFA 98: Road to World Cup", "Sports", new DateOnly(1997, 10, 31)),
        new SeedGameDefinition("Tony Hawk's Pro Skater", "Sports", new DateOnly(1999, 9, 29)),
        new SeedGameDefinition("WWF WrestleMania 2000", "Sports", new DateOnly(1999, 11, 12)),
        new SeedGameDefinition("Virtua Tennis", "Sports", new DateOnly(1999, 7, 29)),
        new SeedGameDefinition("NHL 94", "Sports", new DateOnly(1993, 11, 15)),
        new SeedGameDefinition("Madden NFL 94", "Sports", new DateOnly(1993, 10, 1)),
        new SeedGameDefinition("Tecmo Bowl", "Sports", new DateOnly(1987, 11, 13)),
        new SeedGameDefinition("Super Punch-Out!!", "Sports", new DateOnly(1994, 9, 14)),
        new SeedGameDefinition("NBA Live 95", "Sports", new DateOnly(1994, 10, 1)),
        new SeedGameDefinition("Ken Griffey Jr. Presents Major League Baseball", "Sports", new DateOnly(1994, 5, 1)),
        new SeedGameDefinition("Windjammers", "Sports", new DateOnly(1994, 2, 17)),
        new SeedGameDefinition("California Games", "Sports", new DateOnly(1987, 1, 1)),
        new SeedGameDefinition("Track & Field", "Sports", new DateOnly(1983, 11, 1)),
        new SeedGameDefinition("SSX", "Sports", new DateOnly(2000, 10, 26)),
        new SeedGameDefinition("Neo Turf Masters", "Sports", new DateOnly(1996, 1, 1)),
        new SeedGameDefinition("Mario Tennis", "Sports", new DateOnly(2000, 7, 21)),
        new SeedGameDefinition("NFL Blitz", "Sports", new DateOnly(1997, 10, 1))
    ];
}
