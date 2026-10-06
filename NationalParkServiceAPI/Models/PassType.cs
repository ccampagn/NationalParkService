namespace NationalParkServiceAPI.Models;

public class PassType
{
    public int PassTypeId { get; set; }
    public string? PassTypeName { get; set; }
    public int? Cost { get; set; }
    public int ValidPeriod { get; set; }
    public string? Description { get; set; }
}
