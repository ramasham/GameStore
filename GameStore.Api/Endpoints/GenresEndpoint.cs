using GameStore.Api.Data;
using GameStore.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Endpoints;

public static class GenresEndpoint
{
    public static void MapGenreEndpoint(this WebApplication app) {
        var group = app.MapGroup("/genres");

        group.MapGet("/", async (GameStoreContext dbContext) =>
            await dbContext.Genres
                .Select(genre => new GenreDTO(genre.Id, genre.Name))
                .AsNoTracking()
                .ToListAsync()
        );
    }
}
