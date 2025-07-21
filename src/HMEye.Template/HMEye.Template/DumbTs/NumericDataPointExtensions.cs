namespace HMEye.DumbTs;

/// <summary>
/// Helper service for DumbTs Logger to provide safe type casting of queried <see cref="NumericDataPoint"/> data. 
/// Numeric data is all returned as <see cref="double"/>, but original type is tracked
/// and can be used to cast back to source type if desired.
/// </summary>
public static class NumericDataPointExtensions
{
	public static T SafeCast<T>(this NumericDataPoint point) where T : struct, IConvertible
	{
		if (!IsNumericType<T>())
			throw new ArgumentException("T must be a numeric type.");

		double value = point.Value;
		string originalType = point.OriginalType;

		// Handle common TwinCAT 3 types
		if (typeof(T) == typeof(sbyte) && originalType == "SByte") // SINT
			return (T)(object)checked((sbyte)value);
		if (typeof(T) == typeof(short) && originalType == "Int16") // INT
			return (T)(object)checked((short)value);
		if (typeof(T) == typeof(int) && originalType == "Int32") // DINT
			return (T)(object)checked((int)value);
		if (typeof(T) == typeof(long) && originalType == "Int64") // LINT
			return (T)(object)checked((long)value);
		if (typeof(T) == typeof(byte) && originalType == "Byte") // USINT
			return (T)(object)checked((byte)value);
		if (typeof(T) == typeof(ushort) && originalType == "UInt16") // UINT
			return (T)(object)checked((ushort)value);
		if (typeof(T) == typeof(uint) && originalType == "UInt32") // UDINT
			return (T)(object)checked((uint)value);
		if (typeof(T) == typeof(ulong) && originalType == "UInt64") // ULINT
			return (T)(object)checked((ulong)value);
		if (typeof(T) == typeof(float) && originalType == "Single") // REAL
			return (T)(object)(float)value;
		if (typeof(T) == typeof(double) && originalType == "Double") // LREAL
			return (T)(object)value;

		// Default casting with range checks
		if (typeof(T) == typeof(short) && (value < short.MinValue || value > short.MaxValue))
			throw new InvalidCastException($"Value {value} is out of range for short.");
		if (typeof(T) == typeof(int) && (value < int.MinValue || value > int.MaxValue))
			throw new InvalidCastException($"Value {value} is out of range for int.");
		if (typeof(T) == typeof(float) && (value < float.MinValue || value > float.MaxValue))
			throw new InvalidCastException($"Value {value} is out of range for float.");

		return (T)Convert.ChangeType(value, typeof(T));
	}

	private static bool IsNumericType<T>()
	{
		var type = typeof(T);
		return type.IsPrimitive && type != typeof(char) && type != typeof(bool);
	}
}