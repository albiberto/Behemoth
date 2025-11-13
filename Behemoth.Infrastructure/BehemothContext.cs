using Behemoth.Domain;
using Microsoft.EntityFrameworkCore;

namespace Behemoth.Infrastructure;

public class BehemothContext(DbContextOptions<BehemothContext> options) : DbContext(options)
{
    public DbSet<Profile> Profiles { get; set; }
}