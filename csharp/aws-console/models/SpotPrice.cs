using System;

public class SpotPrice
{
    public long Id { get; set; }
    // public DateTime CreatedAt { get; set; }
    // public DateTime Updated { get; set; }
    public DateTime Timestamp { get; set; }
    public string AvailabilityZone { get; set; }
    public string InstanceType { get; set; }
    public string ProductDescription { get; set; }
    public decimal Price { get; set; }
}