namespace Nirvachak_AI.Models.Api;

// ── Auth ──────────────────────────────────────────────────────────
public record LoginRequest(string Email, string Password);
public record LoginResponse(
    string Token, DateTime ExpiresAt, string FullName,
    string Role, int? ConstituencyId, string UserId);

// ── Dashboard ─────────────────────────────────────────────────────
public record DashboardStatsResponse(
    int TotalVoters, int FavourVoters, int AgainstVoters,
    int NeutralVoters, int UnknownVoters, int TotalBooths,
    int OpenGrievances, int TotalVolunteers, int TotalVoted, double TurnoutPercent);

// ── Voters ────────────────────────────────────────────────────────
public record VoterListItem(
    int Id, string VoterId, string Name, string? NameLocal,
    int Age, string Gender, string? MobileNumber,
    int BoothNumber, string? WardNumber, string? PannaNumber,
    int SerialNumber, string Sentiment, string ElectionDayStatus, string Address);

public record VoterDetailResponse(
    int Id, string VoterId, string Name, string? NameLocal,
    string? FatherHusbandName, int Age, string Gender, string? MobileNumber,
    string Address, int BoothNumber, string? WardNumber, string? PannaNumber,
    int SerialNumber, string Sentiment, string ElectionDayStatus,
    string? Notes, DateTime ImportedAt, DateTime? LastContactedAt,
    List<VisitHistoryItem> Visits);

public record VisitHistoryItem(
    int Id, string WorkerName, DateTime VisitedAt,
    string Status, string Sentiment, string? Notes);

public record UpdateSentimentRequest(int VoterId, string Sentiment);
public record LogVisitRequest(int VoterId, string VisitStatus, string Sentiment, string? Notes);

// ── Booths ────────────────────────────────────────────────────────
public record BoothResponse(
    int Id, int BoothNumber, string BoothName, string Address,
    string? WardNumber, int TotalVoters, int MaleVoters, int FemaleVoters,
    int VotedCount, double TurnoutPercent,
    string? AssignedAgentName, string? AssignedAgentPhone);

// ── Election Day ──────────────────────────────────────────────────
public record MarkVotedRequest(int VoterId);
public record LiveTurnoutResponse(
    int TotalVoters, int TotalVoted, double OverallPercent,
    List<BoothTurnoutItem> Booths);
public record BoothTurnoutItem(
    int BoothNumber, string BoothName,
    int TotalVoters, int VotedCount, double TurnoutPercent);

// ── Grievances ────────────────────────────────────────────────────
public record GrievanceListItem(
    int Id, string Title, string Status, string Priority,
    string? ReportedBy, string? ReporterPhone,
    string? Ward, string? Location, DateTime ReportedAt);

public record GrievanceDetailResponse(
    int Id, string Title, string Description,
    string Status, string Priority,
    string? ReportedBy, string? ReporterPhone,
    string? Ward, string? Location, int? BoothNumber,
    string? AssignedToName, string? ResolutionNotes,
    DateTime ReportedAt, DateTime? ResolvedAt);

public record CreateGrievanceRequest(
    string Title, string Description,
    string? ReportedBy, string? ReporterPhone,
    string Priority, string? Ward, string? Location);

public record UpdateGrievanceStatusRequest(string Status, string? ResolutionNotes);

// ── Volunteers ────────────────────────────────────────────────────
public record VolunteerListItem(
    int Id, string Name, string Phone, string Task,
    string? AssignedArea, string? AssignedBoothNumbers, bool IsActive);

public record CreateVolunteerRequest(
    string Name, string Phone, string? Email, string? Address,
    string Task, string? AssignedArea, string? AssignedBoothNumbers, string? Notes);

// ── Campaign Events ───────────────────────────────────────────────
public record CampaignEventListItem(
    int Id, string Title, string EventType, string Location,
    DateTime ScheduledAt, int? ExpectedAttendance, int? ActualAttendance,
    string? OrganizedByName, bool IsCompleted, string? TargetWards, string? Description);

public record CreateCampaignEventRequest(
    string Title, string EventType, string Location,
    DateTime ScheduledAt, int? ExpectedAttendance,
    string? Description, string? TargetWards, string? TargetBoothNumbers);

// ── Surveys ───────────────────────────────────────────────────────
public record SurveyListItem(
    int Id, string Title, string? Description,
    string Category, bool IsActive, int ResponseCount, DateTime CreatedAt);

public record SubmitSurveyResponseRequest(
    string? RespondentName, string? RespondentPhone,
    string? Ward, int? BoothNumber, int Rating, string? Feedback);

// ── Expenses ──────────────────────────────────────────────────────
public record ExpenseListItem(
    int Id, string Description, string Category,
    decimal Amount, DateTime ExpenseDate,
    string? PayeeName, string? VoucherNumber,
    bool IsECCompliant, string? ApprovedByName);

public record CreateExpenseRequest(
    string Description, string Category,
    decimal Amount, DateTime ExpenseDate,
    string? PayeeName, string? VoucherNumber, string? Notes);

// ── Analytics ─────────────────────────────────────────────────────
public record AnalyticsResponse(
    SentimentBreakdown Sentiment, GenderBreakdown Gender,
    List<AgeGroupItem> AgeGroups, List<BoothAnalyticsItem> BoothBreakdown);

public record SentimentBreakdown(int Favour, int Against, int Neutral, int Floating, int Unknown);
public record GenderBreakdown(int Male, int Female, int Other);
public record AgeGroupItem(string Label, int Count);
public record BoothAnalyticsItem(
    int BoothNumber, int Total,
    int Favour, int Against, int Neutral, int Unknown, int Floating);

// ── Voter Consent / Survey Analytics ─────────────────────────────
public record VoterConsentStatsResponse(
    int TotalVoters, int CompletedCount, int PendingCount, double CompletionRate,
    int CouponsIssued, int CouponsRedeemed,
    int ConsentThirdParty, int ConsentCampaign, int ConsentWhatsApp,
    int ConsentScheme, int ConsentAnalytics,
    List<int> AvailableBooths, List<string> AvailableWards,
    List<BoothSurveyCount> CompletionByBooth);

public record BoothSurveyCount(int BoothNumber, int Count);

public record SurveyCompletedVoter(
    int Id, string Name, string VoterEpic, string? MobileNumber,
    int BoothNumber, string? WardNumber,
    DateTime CompletedAt, bool HasCoupon, string? CouponCode);

public record SurveyPendingVoter(
    int Id, string Name, string VoterEpic, string? MobileNumber,
    int BoothNumber, string? WardNumber);

public record VoterSurveyProfileResponse(
    int VoterId, string VoterName, string VoterEpic,
    int BoothNumber, string? WardNumber,
    string? AgeBracket, string? CasteCategory, string? Religion,
    string? Education, string? Occupation, string? MonthlyIncomeBracket,
    List<string> PrimaryConcerns, string? PreferredLanguage,
    bool ConsentThirdParty, bool ConsentCampaign, bool ConsentWhatsApp,
    bool ConsentScheme, bool ConsentAnalytics,
    DateTime? ProfileUpdatedAt);

public record UpdateVoterSurveyRequest(
    string? AgeBracket, string? CasteCategory, string? Religion,
    string? Education, string? Occupation, string? MonthlyIncomeBracket,
    List<string> PrimaryConcerns, string? PreferredLanguage,
    bool ConsentThirdParty, bool ConsentCampaign, bool ConsentWhatsApp,
    bool ConsentScheme, bool ConsentAnalytics);

// ── Generic ───────────────────────────────────────────────────────
public record ApiResult(bool Success, string? Message = null);
public record PagedResult<T>(List<T> Items, int Total, int Page, int PageSize, int TotalPages);

// ── Announcements ─────────────────────────────────────────────────
public record AnnouncementListItem(
    int Id, string Title, string Body,
    string Category, string CategoryLabel, string CategoryColor,
    string CreatedByName, string TargetRoles,
    bool IsPinned, bool RequiresAcknowledgement,
    bool IsAcknowledged, int AcknowledgementCount,
    DateTime? ExpiresAt, DateTime CreatedAt);

public record CreateAnnouncementRequest(
    string Title, string Body, string Category,
    string? TargetRoles, bool RequiresAcknowledgement, DateTime? ExpiresAt);
