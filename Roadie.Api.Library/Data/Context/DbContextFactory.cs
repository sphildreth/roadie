using FileContextCore;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context.Implementation;
using System;

namespace Roadie.Library.Data.Context
{
    public static class DbContextFactory
    {
        public static IRoadieDbContext Create(IRoadieSettings configuration)
        {
            switch (configuration.DbContextToUse)
            {
                case DbContexts.File:
                    var fileOptionsBuilder = new DbContextOptionsBuilder<MySQLRoadieDbContext>();
                    fileOptionsBuilder.UseFileContextDatabase(configuration.FileDatabaseOptions.DatabaseFormat.ToString().ToLower(),
                                                              databaseName: configuration.FileDatabaseOptions.DatabaseName,
                                                              location: configuration.FileDatabaseOptions.DatabaseFolder);
                    return new FileRoadieDbContext(fileOptionsBuilder.Options);

                default:
                    var mysqlOptionsBuilder = new DbContextOptionsBuilder<MySQLRoadieDbContext>();
                    mysqlOptionsBuilder.UseMySql(configuration.ConnectionString, mySqlOptions =>
                    {
                        mySqlOptions.ServerVersion(new Version(5, 5), ServerType.MariaDb);
                        mySqlOptions.EnableRetryOnFailure(
                            10,
                            TimeSpan.FromSeconds(30),
                            null);
                    });
                    return new MySQLRoadieDbContext(mysqlOptionsBuilder.Options);
            }
        }
    }
}