using System.ComponentModel.DataAnnotations;

namespace GameStore.Api.DTOs;

//What is CreateGameDTO and why do we need it?
//Because when you create a game, the client should NOT send everything.
public record CreateGameDTO (
    [Required] [StringLength(100)] string Name,
    [Range(1, 50)] int GenreId,
    [Range(1, 100)] decimal Price,
    DateOnly ReleaseDate
);
