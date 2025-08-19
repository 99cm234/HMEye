namespace HMEye.TwincatServices;

public class TwincatCustomTypeMaps
{
    public static readonly IReadOnlyDictionary<string, Type> Map = new Dictionary<string, Type>
    {
        { "ST_LrealDataPoint", typeof(DoubleDataPoint) },
    };
}
