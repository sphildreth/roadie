using Microsoft.EntityFrameworkCore;

namespace Roadie.Library.Data.Context.Implementation
{
    /// <summary>
    /// File based Context using FileContextCore
    /// <seealso cref="https://github.com/morrisjdev/FileContextCore"/>
    /// </summary>
    public sealed class FileRoadieDbContext : LinqDbContextBase
    {
        public FileRoadieDbContext(DbContextOptions options)
            : base(options)
        {
        }
    }
}