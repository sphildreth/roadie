using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Roadie.Library.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Roadie.Library.Data
{
    public static class DbContextFactory
    {
        public static IRoadieDbContext Create(IRoadieSettings configuration)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MySQLRoadieDbContext>();
            optionsBuilder.UseMySql(configuration.ConnectionString, mySqlOptions =>
            {
                mySqlOptions.ServerVersion(new Version(5, 5), ServerType.MariaDb);
                mySqlOptions.EnableRetryOnFailure(
                    10,
                    TimeSpan.FromSeconds(30),
                    null);
            });
            return new MySQLRoadieDbContext(optionsBuilder.Options);
        }
    }
}
