using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TalebElm.Domain.Entities;
using TalebElm.Infrastructure.Persistence;
using TalebElm.Infrastructure.Persistence.Repositories;
using TalebElm.Infrastructure.Repositories;     
using Xunit;

namespace TalebElm.Tests.UnitTests;

public class UserRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public UserRepositoryTests()
    {

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private AppDbContext NewContext() => new AppDbContext(_options);

    private static User NewUser(string name) => new User
    {
        Name = name,
        Email = $"{name}@test.com",
        PasswordHash = "hash",
        JoinedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task GetByIdAsync_ReturnsUser_WhenExists()
    {
        var user = NewUser("ahmed");
        await using (var seed = NewContext())
        {
            seed.Users.Add(user);
            await seed.SaveChangesAsync();
        }

        await using var context = NewContext();
        var result = await new UserRepository(context).GetByIdAsync(user.Id);

        Assert.NotNull(result);
        Assert.Equal("ahmed", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenMissing()
    {
        await using var context = NewContext();

        var result = await new UserRepository(context).GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllUsers()
    {
        await using (var seed = NewContext())
        {
            seed.Users.AddRange(NewUser("a"), NewUser("b"), NewUser("c"));
            await seed.SaveChangesAsync();
        }

        await using var context = NewContext();
        var result = await new UserRepository(context).GetAllAsync();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task AddAsync_DoesNotPersist_UntilSave()
    {
        var user = NewUser("sara");

        await using var context = NewContext();
        await new UserRepository(context).AddAsync(user);


        await using (var other = NewContext())
            Assert.Equal(0, await other.Users.CountAsync());

        await context.SaveChangesAsync();


        await using (var other = NewContext())
            Assert.Equal(1, await other.Users.CountAsync());
    }
}