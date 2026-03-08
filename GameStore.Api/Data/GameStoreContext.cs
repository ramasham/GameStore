using System;
using GameStore.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Api.Data;

////DbContext is the ORM gateway
//this class is the bridge between C# objects and database
//tables (Entity framework core)

//this is a constructor
//what is options
// - Which database? (SQL Server? PostgreSQL? SQLite?)
// - What connection string (IP/user/password/db name)?
// - Logging? Lazy loading? etc.

//DbContext is the EF Core class that:
// - knows how to connect to the database
// - tracks entities you load or add
// - translates queries into SQL
// - sends inserts/updates/deletes when you call SaveChanges() or SaveChangesAsync()
public class GameStoreContext(DbContextOptions<GameStoreContext> options)
    : DbContext(options)
{
    //The line looks like “table”, but it’s actually a door (an API)
    //to a table, not the table itself.
    //That line does help EF Core know that Game is part of the model,
    //It tells EF Core:
    // - “Game is an entity type I want this context to manage.”
    // - “When I want to query/add/update/delete Game entities, I’ll use db.Games.”
    
    //So Games is not the table itself — it is the C# entry point to the mapped table.
    //so if you want to do database operations for Game, you usually start through db.Games.
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Genre> Genres => Set<Genre>();
}

//conclusion
/*
    this code mean 
    - you have C# classes like Game and Genre
    - you want EF Core to treat them as entities
    - you want those entities to be stored/retrieved from the database
    - so you add them to your DbContext

    Why inside DbContext?
    - Because DbContext is the place where EF Core knows:
        - which entity types belong to this database session
        - how to access them
        - how to map them to database tables
*/
