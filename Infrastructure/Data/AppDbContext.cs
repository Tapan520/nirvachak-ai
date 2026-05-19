using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Nirvachak_AI.Domain.Entities;

namespace Nirvachak_AI.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Constituency> Constituencies => Set<Constituency>();
    public DbSet<Voter> Voters => Set<Voter>();
    public DbSet<Booth> Booths => Set<Booth>();
    public DbSet<DoorToDoorVisit> DoorToDoorVisits => Set<DoorToDoorVisit>();
    public DbSet<Volunteer> Volunteers => Set<Volunteer>();
    public DbSet<CampaignEvent> CampaignEvents => Set<CampaignEvent>();
    public DbSet<Grievance> Grievances => Set<Grievance>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Ward> Wards => Set<Ward>();
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<SurveyResponse> SurveyResponses => Set<SurveyResponse>();
    public DbSet<Announcement> Announcements => Set<Announcement>();
    public DbSet<AnnouncementAcknowledgement> AnnouncementAcknowledgements => Set<AnnouncementAcknowledgement>();

    // ── Voter Self-Survey Module ──────────────────────────────────
    public DbSet<VoterProfile> VoterProfiles => Set<VoterProfile>();
    public DbSet<VoterConsent> VoterConsents => Set<VoterConsent>();
    public DbSet<RewardConfig> RewardConfigs => Set<RewardConfig>();
    public DbSet<CouponPool> CouponPools => Set<CouponPool>();
    public DbSet<SurveyCompletion> SurveyCompletions => Set<SurveyCompletion>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Voter>().HasIndex(v => v.VoterId);
        builder.Entity<Voter>().HasIndex(v => new { v.ConstituencyId, v.BoothNumber });
        builder.Entity<Voter>().HasIndex(v => v.Name);

        builder.Entity<Expense>()
            .Property(e => e.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Entity<AnnouncementAcknowledgement>()
            .HasIndex(a => new { a.AnnouncementId, a.UserId })
            .IsUnique();

        builder.Entity<AppUser>()
            .HasOne(u => u.Constituency)
            .WithMany()
            .HasForeignKey(u => u.ConstituencyId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Voter Self-Survey Module ──────────────────────────────
        builder.Entity<VoterProfile>()
            .HasIndex(v => v.VoterId).IsUnique();

        builder.Entity<VoterConsent>()
            .HasIndex(v => v.VoterId).IsUnique();

        builder.Entity<SurveyCompletion>()
            .HasIndex(v => v.VoterId).IsUnique();

        builder.Entity<CouponPool>()
            .HasIndex(c => c.CouponCode).IsUnique();

        builder.Entity<CouponPool>()
            .HasOne(c => c.RewardConfig)
            .WithMany(r => r.Coupons)
            .HasForeignKey(c => c.RewardConfigId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CouponPool>()
            .HasOne(c => c.IssuedToVoter)
            .WithMany()
            .HasForeignKey(c => c.IssuedToVoterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<SurveyCompletion>()
            .HasOne(s => s.Coupon)
            .WithMany()
            .HasForeignKey(s => s.CouponId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
