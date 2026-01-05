using TcEventLoggerAdsProxyLib;

namespace HMEye.Twincat.Plc.EventLogService
{
	public class EventLogEvent
	{
		public Guid EventClass { get; set; }
		public uint EventId { get; set; }
		public string? EventText { get; set; }
		public long FileTimeCleared { get; set; }
		public long FileTimeConfirmed { get; set; }
		public long FileTimeRaised { get; set; }
		public bool IsActive { get; set; }
		public bool IsRaised { get; set; }
		public bool IsConfirmed { get; set; }
		public bool IsCleared { get; set; }
		public string? JsonAttribute { get; set; }
		public SeverityLevelEnum SeverityLevel { get; set; }
		public string? SourceName { get; set; }
		public DateTime TimeCleared { get; set; }
		public DateTime TimeConfirmed { get; set; }
		public DateTime TimeRaised { get; set; }
		public bool WithConfirmation { get; set; }
	}
}