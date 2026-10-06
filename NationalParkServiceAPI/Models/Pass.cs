namespace NationalParkServiceAPI.Models;

public class Pass
{
    public int PassId { get; set; }
    public int? PassTypeId { get; set; }
    public PassType? PassType { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int? Active { get; set; }
}
