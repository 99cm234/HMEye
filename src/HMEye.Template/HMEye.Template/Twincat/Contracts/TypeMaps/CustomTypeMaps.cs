using HMEye.TwincatServices.Contracts.Models;

namespace HMEye.TwincatServices.Contracts.TypeMaps;

public class CustomTypeMaps
{
    public static readonly IReadOnlyDictionary<string, Type> Map = new Dictionary<string, Type>
    {
        { "ST_LrealDataPoint", typeof(DoubleDataPoint) },
    };
}

