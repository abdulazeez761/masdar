namespace Masdar.Web.Domain.Enums;

public enum RoadmapDifficulty
{
    Easy = 1,
    Medium = 2,
    Hard = 3
}

public enum ResourceRequestStatus
{
    Open = 1,
    Fulfilled = 2,
    Cancelled = 3,
    Expired = 4
}

public enum OpportunityType
{
    Hackathon = 1,
    Competition = 2,
    Workshop = 3,
    Internship = 4,
    Meetup = 5,
    Bootcamp = 6,
    Scholarship = 7,
    Volunteer = 8,
    Talk = 9,
    Other = 10
}

public enum OpportunityRegistrationStatus
{
    Registered = 1,
    Waitlisted = 2,
    Cancelled = 3,
    Attended = 4
}

public enum ReputationTransactionType
{
    InitialGrant = 1,
    ResourcePublished = 2,
    RequestBountyReserved = 3,
    RequestBountyAwarded = 4,
    RequestBountyRefunded = 5,
    AdministrativeAdjustment = 6
}
