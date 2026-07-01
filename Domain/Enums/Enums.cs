namespace Nirvachak_AI.Domain.Enums;

public enum ElectionType { MLA, Ward }
public enum VoterSentiment { Unknown, Favour, Against, Neutral, Floating }
public enum UserRole { Admin = 0, CampaignManager = 1, Candidate = 2, FieldWorker = 3, BoothAgent = 4, SuperAdmin = 5 }
public enum GrievanceStatus { Open, InProgress, Resolved, Closed }
public enum GrievancePriority { Low, Medium, High, Critical }
public enum ExpenseCategory { Publicity, Transport, Food, Communication, Printing, Miscellaneous }
public enum VisitStatus { NotVisited, Visited, NotAtHome, Refused }
public enum ElectionDayStatus { NotVoted, Voted, Absent }
public enum CampaignEventType { Rally, DoorToDoor, SmallMeeting, LargeMeeting, PhoneCall, Other }
public enum VolunteerTask { BoothManagement, VoterOutreach, DataEntry, Transport, Communication, Other }
public enum SurveyCategory { CandidateAwareness, LocalIssues, PartySupport, DevelopmentFeedback, GeneralOpinion }
public enum AnnouncementCategory { CampaignAnnouncement, CriticalAlert, ECComplianceNotice, DailyBriefing, Motivation, LiveDataNudge }
public enum CompetitorActivityType { Rally, RoadShow, DoorToDoor, SmallMeeting, Announcement, MediaCoverage, SocialMedia, Other }
public enum CompetitorThreatLevel { Low, Medium, High, Critical }
public enum InfluencerAlignment { Unknown, Favour, Against, Neutral, Floating }
public enum CallOutcome { NoAnswer, Talked, CallBack, Wrong, Refused }
