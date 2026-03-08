namespace GameStore.Api.DTOs;

public record GameDetailsDTO(
    int Id,
    string Name,
    int GenreId,
    decimal Price,
    DateOnly ReleaseDate,
    string? ImageUrl
);
