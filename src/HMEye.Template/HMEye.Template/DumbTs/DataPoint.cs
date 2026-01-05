using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HMEye.DumbTs;

public abstract class DataPointBase
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	[Required]
	[MaxLength(100)]
	public string SeriesName { get; set; } = string.Empty;

	[Required]
	[Column(TypeName = "timestamp")]
	public DateTime Timestamp { get; set; }

	[MaxLength(100)]
	public string? Source { get; set; }

	public DataPointType DataType { get; set; }
}

/// <summary>
/// All numeric types are converted to double.
/// Mapping to decimal to ensure sufficient space and precision for all types.
/// </summary>
public class NumericDataPoint : DataPointBase
{
	[Required]
	[Column(TypeName = "decimal(28,8)")]
	public double Value { get; set; }

	[MaxLength(20)]
	public string? Unit { get; set; }

	[MaxLength(50)]
	public string OriginalType { get; set; } = string.Empty;
}

public class BooleanDataPoint : DataPointBase
{
	[Required]
	public bool Value { get; set; }
}

public class TextDataPoint : DataPointBase
{
	[Required]
	[MaxLength(500)]
	public string Value { get; set; } = string.Empty;
}

public enum DataPointType
{
	Numeric = 0,
	Boolean = 1,
	Text = 2,
}