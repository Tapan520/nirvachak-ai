namespace Nirvachak_AI.Domain.Entities;

public class RewardConfig
{
    public int Id { get; set; }
    public int ConstituencyId { get; set; }
    public Constituency? Constituency { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PartnerBrand { get; set; }
    public string CouponCodePrefix { get; set; } = "NIRV";
    public DateTime ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CouponPool> Coupons { get; set; } = new List<CouponPool>();
}
