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