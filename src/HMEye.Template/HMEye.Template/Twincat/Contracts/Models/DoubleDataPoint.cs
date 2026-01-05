using System.Runtime.InteropServices;

namespace HMEye.Twincat.Contracts.Models;

[StructLayout(LayoutKind.Sequential, Pack = 0)]
public struct DoubleDataPoint
{
	public long Timestamp;
	public double Value;

	public DateTime DateTime
	{
		get => DateTime.FromFileTimeUtc(Timestamp);
		set => Timestamp = value.ToFileTimeUtc();
	}
}