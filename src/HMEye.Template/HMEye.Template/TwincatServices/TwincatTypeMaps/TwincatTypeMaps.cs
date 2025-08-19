namespace HMEye.TwincatServices;

public partial class TwincatTypeMaps
{
    public static readonly IReadOnlyDictionary<string, Type> Map = new Dictionary<string, Type>
    {
        { "BOOL", typeof(bool) },
        { "BYTE", typeof(byte) },
        { "SINT", typeof(sbyte) },
        { "USINT", typeof(byte) },
        { "WORD", typeof(ushort) },
        { "INT", typeof(short) },
        { "UINT", typeof(ushort) },
        { "DWORD", typeof(uint) },
        { "DINT", typeof(int) },
        { "UDINT", typeof(uint) },
        { "LWORD", typeof(ulong) },
        { "LINT", typeof(long) },
        { "ULINT", typeof(ulong) },
        { "REAL", typeof(float) },
        { "LREAL", typeof(double) },
        { "STRING", typeof(string) },
        { "WSTRING", typeof(string) },
        { "BIT", typeof(bool) },

        // Time / duration
        { "TIME", typeof(TimeSpan) },
        { "T", typeof(TimeSpan) },
        { "LTIME", typeof(TimeSpan) },

        // Time of day
        { "TIME_OF_DAY", typeof(TimeOnly) },
        { "TOD", typeof(TimeOnly) },
        { "LTIME_OF_DAY", typeof(TimeOnly) },
        { "LTOD", typeof(TimeOnly) },

        // Date only
        { "DATE", typeof(DateOnly) },
        { "D", typeof(DateOnly) },
        { "LDATE", typeof(DateOnly) },

        // Date + time
        { "DATE_AND_TIME", typeof(DateTime) },
        { "DT", typeof(DateTime) },
        { "LDATE_AND_TIME", typeof(DateTime) },
        { "LDT", typeof(DateTime) },
	};
}
