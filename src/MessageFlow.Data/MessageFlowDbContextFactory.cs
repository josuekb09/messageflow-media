using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MessageFlow.Data;

public sealed class MessageFlowDbContextFactory : IDesignTimeDbContextFactory<MessageFlowDbContext>
{
    public MessageFlowDbContext CreateDbContext(string[] args)
    {
        var databasePath = MessageFlowDatabase.DefaultDatabasePath;
        MessageFlowDatabase.EnsureDatabaseDirectory(databasePath);

        var options = new DbContextOptionsBuilder<MessageFlowDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        return new MessageFlowDbContext(options);
    }
}
