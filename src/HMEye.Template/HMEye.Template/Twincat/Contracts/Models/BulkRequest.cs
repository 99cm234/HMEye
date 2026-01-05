namespace HMEye.TwincatServices.Models;

public class BulkRequest
{
    public List<BulkItem> Items { get; set; } = new();
}
