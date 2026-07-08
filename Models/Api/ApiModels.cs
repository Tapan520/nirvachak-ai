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
string? MobileNumber,
int BoothNumber, string? WardNumber,
string? AgeBracket, string? CasteCategory, string? Religion,
string? Education, string? Occupation, string? MonthlyIncomeBracket,
List<string> PrimaryConcerns, string? PreferredLanguage,
bool ConsentThirdParty, bool ConsentCampaign, bool ConsentWhatsApp,
bool ConsentScheme, bool ConsentAnalytics,
DateTime? ProfileUpdatedAt);

public record UpdateVoterSurveyRequest(
string? MobileNumber,
string? AgeBracket, string? CasteCategory, string? Religion,
string? Education, string? Occupation, string? MonthlyIncomeBracket,
List<string> PrimaryConcerns, string? PreferredLanguage,
bool ConsentThirdParty, bool ConsentCampaign, bool ConsentWhatsApp,
bool ConsentScheme, bool ConsentAnalytics);

// ── Predictive Analytics ─────────────────────────────────────────
public record BoothPredictionResponse(
    int BoothNumber, string BoothName,
    int TotalVoters, int FavourVoters, int AgainstVoters, int FloatingVoters,
    int ContactedVoters, int RecentVisits, double ContactRate,
    double PredictedTurnoutPercent, double PredictedSupportPercent,
    int EstimatedFavourVotes,
    string TurnoutRisk, string SupportConfidence,
    List<string> StrategyAlerts);

public record PredictionSummaryResponse(
    int TotalVoters, int TotalContacted, int TotalFavour, int TotalFloating,
    double PredictedOverallTurnout, double PredictedOverallSupport,
    int EstimatedTotalFavourVotes,
    int AtRiskBoothCount, int WeakSupportBoothCount,
    List<BoothPredictionResponse> BoothPredictions);

// ── Phone Banking ─────────────────────────────────────────────────
public record PhoneBankingStatsResponse(
    int TotalCallsToday, int TalkedCount, int NoAnswerCount, int CallBackCount,
    List<PhoneCallItem> RecentCalls, List<PendingCallVoter> PendingVoters);

public record PhoneCallItem(
    int Id, int VoterId, string VoterName, string? Phone,
    DateTime CalledAt, string Outcome, int DurationSeconds,
    string? Notes, string? SentimentAfterCall);

public record PendingCallVoter(
    int Id, string Name, string Phone,
    int BoothNumber, string? WardNumber, string Sentiment);

public record LogPhoneCallRequest(
    int VoterId, string Outcome, int DurationSeconds,
    string? Notes, string? SentimentAfterCall);

// ── Influencers ───────────────────────────────────────────────────
public record InfluencerListItem(
    int Id, string Name, string? MobileNumber, string? Category, string? Community,
    int? EstimatedFollowers, string? Ward, int? BoothNumber,
    string Alignment, string? Notes, DateTime? LastMetAt, string? LastMeetingOutcome);

public record CreateInfluencerRequest(
    string Name, string? MobileNumber, string? Category, string? Community,
    int? EstimatedFollowers, string? Ward, int? BoothNumber,
    string Alignment, string? Notes);

public record UpdateInfluencerMeetingRequest(
    string Alignment, string? OutcomeNotes, string? Notes);

// ── Competitor Tracker ────────────────────────────────────────────
public record CompetitorActivityItem(
    int Id, string CompetitorName, string? PartyName,
    string ActivityTitle, string ActivityType,
    string? Location, string? Ward, int? BoothNumber,
    DateTime ActivityDate, int? EstimatedCrowd,
    string? Notes, string ThreatLevel);

public record CreateCompetitorActivityRequest(
    string CompetitorName, string? PartyName,
    string ActivityTitle, string ActivityType,
    string? Location, string? Ward, int? BoothNumber,
    DateTime ActivityDate, int? EstimatedCrowd,
    string? Notes, string ThreatLevel);

// ── Booth Shifts ──────────────────────────────────────────────────
public record BoothShiftItem(
    int Id, int VolunteerId, string VolunteerName, string VolunteerPhone,
    int BoothNumber, DateTime ShiftStart, DateTime ShiftEnd,
    string Role, bool IsConfirmed, string? Notes);

public record CreateBoothShiftRequest(
    int VolunteerId, int BoothNumber,
    DateTime ShiftStart, DateTime ShiftEnd,
    string Role, string? Notes);

// ── Budget ────────────────────────────────────────────────────────
public record BudgetItem(
    int Id, string Category, decimal PlannedAmount, decimal SpentAmount,
    decimal Remaining, double UtilisationPercent, string? Notes);

public record CreateBudgetItemRequest(
    string Category, decimal PlannedAmount, string? Notes);

// ── Broadcast / Messaging ─────────────────────────────────────────
public record MessageTemplateItem(
    int Id, string Title, string Body, string Language, string Category, DateTime CreatedAt);

public record CreateMessageTemplateRequest(
    string Title, string Body, string Language, string Category);

public record BroadcastItem(
    int Id, int TemplateId, string TemplateTitle,
    string? TargetDescription, int TotalTargeted, int SentCount,
    string Status, DateTime? ScheduledAt, DateTime? SentAt,
    string CreatedByName, DateTime CreatedAt);

public record CreateBroadcastRequest(
    int TemplateId, string? TargetDescription, DateTime? ScheduledAt);

// ── Field Reports ─────────────────────────────────────────────────
public record FieldReportItem(
    int Id, string WorkerName, DateTime ReportDate,
    int ContactsMade, int FavourContacts, int FloatingContacts, int AgainstContacts,
    int IssuesLogged, string? Highlights, string? Challenges,
    string? PlannedForTomorrow, string Status);

public record CreateFieldReportRequest(
    int ContactsMade, int FavourContacts, int FloatingContacts, int AgainstContacts,
    int IssuesLogged, string? Highlights, string? Challenges, string? PlannedForTomorrow);

// ── Panna Pramukh ─────────────────────────────────────────────────
public record PannaPramukhItem(
    int Id, string Name, string Phone, int BoothNumber, string PannaNumber,
    int TotalVotersAssigned, int VotersContacted, double ContactPercent,
    bool IsActive, string? Notes);

public record CreatePannaPramukhRequest(
    string Name, string Phone, string? Email, string? Address,
    int BoothNumber, string PannaNumber, int TotalVotersAssigned, string? Notes);

public record UpdatePannaContactRequest(int Id, int VotersContacted);

// ── Rapid Response ────────────────────────────────────────────────
public record RapidResponseListItem(
    int Id, string Title, string Description, string? Source,
    string? AffectedWards, string? AssignedToName,
    string? ResponseText, string Status, string ThreatLevel,
    DateTime DetectedAt, DateTime? ResolvedAt);

public record CreateRapidResponseRequest(
    string Title, string Description, string? Source,
    string? AffectedWards, string ThreatLevel, string? ResponseText);

public record UpdateRapidResponseRequest(string Status, string? ResponseText);

// ── Reports (Expense Summary) ─────────────────────────────────────
public record ExpenseReportResponse(
    decimal TotalAmount, decimal EcBudgetLimit, int EcBudgetPercent,
    List<CategoryTotal> CategoryTotals, List<ExpenseListItem> Expenses);

public record CategoryTotal(string Category, decimal Amount, double Percent);

// ── Election Results ──────────────────────────────────────────────
public record ElectionResultItem(
    int Id, int BoothNumber, int RoundNumber,
    int CandidateVotes, int? Competitor1Votes, string? Competitor1Name,
    int? Competitor2Votes, string? Competitor2Name,
    int? TotalVotesCast, bool IsFinal, DateTime EnteredAt);

public record ElectionResultSummary(
    int TotalCandidateVotes, int TotalCompetitor1Votes, int TotalCompetitor2Votes,
    string? Competitor1Name, string? Competitor2Name,
    bool IsLeading, int LeadMargin,
    List<ElectionResultItem> Results);

public record CreateElectionResultRequest(
    int BoothNumber, int RoundNumber, int CandidateVotes,
    int? Competitor1Votes, string? Competitor1Name,
    int? Competitor2Votes, string? Competitor2Name,
    int? TotalVotesCast, bool IsFinal);

// ── Transport ─────────────────────────────────────────────────────
public record TransportVehicleItem(
    int Id, string DriverName, string DriverPhone,
    string? VehicleNumber, string? VehicleType,
    int Capacity, int BoothNumber, bool IsAvailable, string? Notes);

public record TransportRequestItem(
    int Id, int VoterId, string VoterName, string? VoterPhone,
    int? VehicleId, string? DriverName, string? VehicleNumber,
    string Status, string? PickupAddress, DateTime RequestedAt);

public record CreateTransportRequestRequest(
    int VoterId, string? PickupAddress, string? PickupNotes, int? VehicleId);

public record CreateTransportVehicleRequest(
    string DriverName, string DriverPhone, string? VehicleNumber,
    string? VehicleType, int Capacity, int BoothNumber, string? Notes);

// ── Voter Slips ───────────────────────────────────────────────────
public record VoterSlipItem(
    int Id, string VoterId, string Name, string? NameLocal,
    int BoothNumber, string? WardNumber, string? PannaNumber,
    int SerialNumber, int Age, string Gender, string Address);

// ── Admin (User List) ─────────────────────────────────────────────
public record AdminUserItem(
    string Id, string FullName, string? Email, string Role,
    string? ConstituencyName, string? AssignedWard, bool IsActive);

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
