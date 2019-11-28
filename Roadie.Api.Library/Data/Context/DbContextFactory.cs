using FileContextCore;
using Microsoft.EntityFrameworkCore;
using Roadie.Library.Configuration;
using Roadie.Library.Data.Context.Implementation;
using System;
using System.IO;

namespace Roadie.Library.Data.Context
{
    public static class DbContextFactory
    {
        public static IRoadieDbContext Create(IRoadieSettings configuration)
        {
            switch (configuration.DbContextToUse)
            {
                case DbContexts.SQLite:
                    var sqlLiteOptionsBuilder = new DbContextOptionsBuilder<SQLiteRoadieDbContext>();
                    var databaseName = Path.Combine(configuration.FileDatabaseOptions.DatabaseFolder, $"{ configuration.FileDatabaseOptions.DatabaseName }.db");
                    sqlLiteOptionsBuilder.UseSqlite($"Filename={databaseName}");
                    return new SQLiteRoadieDbContext(sqlLiteOptionsBuilder.Options);

                case DbContexts.File:
                    var fileOptionsBuilder = new DbContextOptionsBuilder<MySQLRoadieDbContext>();
                    fileOptionsBuilder.UseFileContextDatabase(configuration.FileDatabaseOptions.DatabaseFormat.ToString().ToLower(),
                                                              databaseName: configuration.FileDatabaseOptions.DatabaseName,
                                                              location: configuration.FileDatabaseOptions.DatabaseFolder);
                    return new FileRoadieDbContext(fileOptionsBuilder.Options);

                case DbContexts.MySQL:
                    var mysqlOptionsBuilder = new DbContextOptionsBuilder<MySQLRoadieDbContext>();
                    mysqlOptionsBuilder.UseMySql(configuration.ConnectionString, mySqlOptions =>
                    {
                        mySqlOptions.EnableRetryOnFailure(
                            10,
                            TimeSpan.FromSeconds(30),
                            null);
                    });
                    return new MySQLRoadieDbContext(mysqlOptionsBuilder.Options);

                default:
                    throw new NotImplementedException("Unknown DbContext Type");
            }
        }
    }
}