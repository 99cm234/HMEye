using Microsoft.EntityFrameworkCore;

namespace HMEye.DumbTs;

public class DumbTsDbContext : DbContext
{
	public DumbTsDbContext(DbContextOptions<DumbTsDbContext> options)
		: base(options) { }

	public DbSet<NumericDataPoint> NumericDataPoints { get; set; }
	public DbSet<BooleanDataPoint> BooleanDataPoints { get; set; }
	public DbSet<TextDataPoint> TextDataPoints { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<NumericDataPoint>(entity =>
		{
			entity.HasIndex(p => new { p.SeriesName, p.Timestamp })
				  .IsDescending(false, true);

			entity.Property(p => p.Timestamp)
				  .HasConversion<long>();

			entity.Property(p => p.DataType)
				  .HasConversion<string>();

			entity.Property(p => p.OriginalType)
				  .HasMaxLength(50);
		});

		modelBuilder.Entity<BooleanDataPoint>(entity =>
		{
			entity.HasIndex(p => new { p.SeriesName, p.Timestamp })
				  .IsDescending(false, true);

			entity.Property(p => p.Timestamp)
				  .HasConversion<long>();

			entity.Property(p => p.DataType)
				  .HasConversion<string>();
		});

		modelBuilder.Entity<TextDataPoint>(entity =>
		{
			entity.HasIndex(p => new { p.SeriesName, p.Timestamp })
				  .IsDescending(false, true);

			entity.Property(p => p.Timestamp)
				  .HasConversion<long>();

			entity.Property(p => p.DataType)
				  .HasConversion<string>();
		});
	}
}