using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PhotoCopyHub.Infrastructure.Data;

namespace PhotoCopyHub.Tests;

public static class TestDbFactory
{
    public static (ApplicationDbContext Context, SqliteConnection Connection) CreateContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return (context, connection);
    }
}
