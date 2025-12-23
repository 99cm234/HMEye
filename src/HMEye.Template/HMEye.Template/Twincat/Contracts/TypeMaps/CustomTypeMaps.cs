using HMEye.Twincat.Contracts.Models;

namespace HMEye.Twincat.Contracts.TypeMaps;

public class CustomTypeMaps
{
    public static readonly IReadOnlyDictionary<string, Type> Map = new Dictionary<string, Type>
    {
        { "ST_LrealDataPoint", typeof(DoubleDataPoint) },
    };
}
