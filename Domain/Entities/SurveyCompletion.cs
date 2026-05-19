namespace Nirvachak_AI.Domain.Entities;

public class SurveyCompletion
{
    public int Id { get; set; }
    public int VoterId { get; set; }
    public Voter? Voter { get; set; }
    public int ConstituencyId { get; set; }

    public int? CouponId { get; set; }
    public CouponPool? Coupon { get; set; }

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
}
