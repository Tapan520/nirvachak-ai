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

    // ── Competitor Intelligence ──────────────────────────────────
    public DbSet<CompetitorActivity> CompetitorActivities => Set<CompetitorActivity>();

    // ── Influencer Network ───────────────────────────────────────
    public DbSet<Influencer> Influencers => Set<Influencer>();

    // ── Phone Banking ────────────────────────────────────────────
    public DbSet<PhoneCallLog> PhoneCallLogs => Set<PhoneCallLog>();

    // ── Voter Self-Survey Module ──────────────────────────────────
    public DbSet<VoterProfile> VoterProfiles => Set<VoterProfile>();
    public DbSet<VoterConsent> VoterConsents => Set<VoterConsent>();
    public DbSet<RewardConfig> RewardConfigs => Set<RewardConfig>();
    public DbSet<CouponPool> CouponPools => Set<CouponPool>();
    public DbSet<SurveyCompletion> SurveyCompletions => Set<SurveyCompletion>();

    // ── Election Enhancement Modules ─────────────────────────────
    public DbSet<PannaPramukh> PannaPramukhs => Set<PannaPramukh>();
    public DbSet<TransportVehicle> TransportVehicles => Set<TransportVehicle>();
    public DbSet<VoterTransportRequest> VoterTransportRequests => Set<VoterTransportRequest>();
    public DbSet<FieldReport> FieldReports => Set<FieldReport>();
    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();
    public DbSet<MessageBroadcast> MessageBroadcasts => Set<MessageBroadcast>();
    public DbSet<VoterTag> VoterTags => Set<VoterTag>();
    public DbSet<BoothShiftAssignment> BoothShiftAssignments => Set<BoothShiftAssignment>();
    public DbSet<RapidResponseItem> RapidResponseItems => Set<RapidResponseItem>();
    public DbSet<BudgetPlan> BudgetPlans => Set<BudgetPlan>();
    public DbSet<ElectionResult> ElectionResults => Set<ElectionResult>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Voter>().HasIndex(v => v.VoterId);
        builder.Entity<Voter>().HasIndex(v => new { v.ConstituencyId, v.BoothNumber });
        builder.Entity<Voter>().HasIndex(v => v.Name);

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

        // ── Election Enhancement Modules ─────────────────────────
        builder.Entity<VoterTag>()
            .HasIndex(t => new { t.VoterId, t.Tag })
            .IsUnique();

        builder.Entity<BoothShiftAssignment>()
            .HasOne(b => b.Volunteer)
            .WithMany()
            .HasForeignKey(b => b.VolunteerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<VoterTransportRequest>()
            .HasOne(r => r.Voter)
            .WithMany()
            .HasForeignKey(r => r.VoterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<VoterTransportRequest>()
            .HasOne(r => r.Vehicle)
            .WithMany()
            .HasForeignKey(r => r.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<MessageBroadcast>()
            .HasOne(b => b.Template)
            .WithMany()
            .HasForeignKey(b => b.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Expense>()
            .Property(e => e.Amount)
            .HasColumnType("decimal(18,2)");

        builder.Entity<BudgetPlan>()
            .Property(b => b.PlannedAmount)
            .HasColumnType("decimal(18,2)");
    }
}
