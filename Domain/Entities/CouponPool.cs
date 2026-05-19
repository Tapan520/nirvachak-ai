namespace Nirvachak_AI.Domain.Entities;

public class CouponPool
{
    public int Id { get; set; }
    public int RewardConfigId { get; set; }
    public RewardConfig? RewardConfig { get; set; }

    public string CouponCode { get; set; } = string.Empty;

    public bool IsIssued { get; set; } = false;
    public int? IssuedToVoterId { get; set; }
    public Voter? IssuedToVoter { get; set; }
    public DateTime? IssuedAt { get; set; }

    public bool IsRedeemed { get; set; } = false;
    public DateTime? RedeemedAt { get; set; }
}
