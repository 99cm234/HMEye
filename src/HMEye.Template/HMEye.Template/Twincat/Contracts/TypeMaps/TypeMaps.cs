namespace HMEye.Twincat.Contracts.TypeMaps;

public partial class TypeMaps
{
	public static readonly IReadOnlyDictionary<string, Type> Map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
{
    // Bool / bits
    { "BOOL", typeof(bool) },
	{ "BIT", typeof(bool) },

    // Integer types
    { "SINT", typeof(sbyte) },
	{ "USINT", typeof(byte) },
	{ "BYTE", typeof(byte) },
	{ "INT", typeof(short) },
	{ "UINT", typeof(ushort) },
	{ "WORD", typeof(ushort) },
	{ "DINT", typeof(int) },
	{ "UDINT", typeof(uint) },
	{ "DWORD", typeof(uint) },
	{ "LINT", typeof(long) },
	{ "ULINT", typeof(ulong) },
	{ "LWORD", typeof(ulong) },

    // Floating point
    { "REAL", typeof(float) },
	{ "LREAL", typeof(double) },

    // Strings
    { "STRING", typeof(string) },
	{ "WSTRING", typeof(string) },

    // Time spans / durations
    { "TIME", typeof(TimeSpan) },
	{ "T", typeof(TimeSpan) },
	{ "LTIME", typeof(TimeSpan) },

    // Time of day
    { "TIME_OF_DAY", typeof(TimeOnly) },
	{ "TOD", typeof(TimeOnly) },
	{ "LTIME_OF_DAY", typeof(TimeOnly) },
	{ "LTOD", typeof(TimeOnly) },

    // Dates
    { "DATE", typeof(DateOnly) },
	{ "D", typeof(DateOnly) },
	{ "LDATE", typeof(DateOnly) },

    // Date+time
    { "DATE_AND_TIME", typeof(DateTime) },
	{ "DT", typeof(DateTime) },
	{ "LDATE_AND_TIME", typeof(DateTime) },
	{ "LDT", typeof(DateTime) },
};

}
