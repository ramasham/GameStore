using GameStore.Api.Data;
using GameStore.Api.Endpoints;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

//AddValidation() -> built-in feature that turns on automatic
//request validation for Minimal APIs
builder.Services.AddValidation();
builder.AddGameStoreDb();

var app = builder.Build();

app.MigrateDb();
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "uploads", "games"));
app.SeedGamesFromCoverFolder();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.ContentRootPath, "uploads")),
    RequestPath = "/uploads"
});
app.MapGamesEndpoints();
app.MapGenreEndpoint();

app.Run();
