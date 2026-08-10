using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Masdar.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Opportunities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    RegistrationUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StartsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RegistrationDeadlineUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Capacity = table.Column<int>(type: "int", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Opportunities", x => x.Id);
                    table.CheckConstraint("CK_Opportunities_Capacity", "[Capacity] IS NULL OR [Capacity] > 0");
                    table.CheckConstraint("CK_Opportunities_Dates", "[EndsAtUtc] IS NULL OR [EndsAtUtc] >= [StartsAtUtc]");
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Universities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LogoStorageKey = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Universities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Majors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UniversityId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Majors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Majors_Universities_UniversityId",
                        column: x => x.UniversityId,
                        principalTable: "Universities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ReputationBalance = table.Column<int>(type: "int", nullable: false),
                    UniversityId = table.Column<int>(type: "int", nullable: true),
                    MajorId = table.Column<int>(type: "int", nullable: true),
                    ProfileImageKey = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.CheckConstraint("CK_AspNetUsers_ReputationBalance", "[ReputationBalance] >= 0");
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Majors_MajorId",
                        column: x => x.MajorId,
                        principalTable: "Majors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Universities_UniversityId",
                        column: x => x.UniversityId,
                        principalTable: "Universities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MajorId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Instructor = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ExamDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_Majors_MajorId",
                        column: x => x.MajorId,
                        principalTable: "Majors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OpportunityRegistrations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OpportunityId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RegisteredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OpportunityRegistrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OpportunityRegistrations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OpportunityRegistrations_Opportunities_OpportunityId",
                        column: x => x.OpportunityId,
                        principalTable: "Opportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReputationTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    RelatedEntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReputationTransactions", x => x.Id);
                    table.CheckConstraint("CK_ReputationTransactions_BalanceAfter", "[BalanceAfter] >= 0");
                    table.ForeignKey(
                        name: "FK_ReputationTransactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RoadmapNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    Difficulty = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExamWeightPercent = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoadmapNodes", x => x.Id);
                    table.CheckConstraint("CK_RoadmapNodes_ExamWeight", "[ExamWeightPercent] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_RoadmapNodes_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyResources",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    UploaderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Semester = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 6000, nullable: false),
                    TutorContext = table.Column<string>(type: "nvarchar(max)", maxLength: 6000, nullable: true),
                    PublicationStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    VerificationStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IsRepresentative = table.Column<bool>(type: "bit", nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    FileScanStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ExternalUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyResources", x => x.Id);
                    table.CheckConstraint("CK_StudyResources_FileSize", "[FileSizeBytes] > 0 AND [FileSizeBytes] <= 10485760");
                    table.ForeignKey(
                        name: "FK_StudyResources_AspNetUsers_UploaderId",
                        column: x => x.UploaderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudyResources_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserCourses",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SelectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAccessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCourses", x => new { x.UserId, x.CourseId });
                    table.ForeignKey(
                        name: "FK_UserCourses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCourses_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoadmapProgress",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoadmapNodeId = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoadmapProgress", x => new { x.UserId, x.RoadmapNodeId });
                    table.ForeignKey(
                        name: "FK_UserRoadmapProgress_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoadmapProgress_RoadmapNodes_RoadmapNodeId",
                        column: x => x.RoadmapNodeId,
                        principalTable: "RoadmapNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceHelpfulVotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceHelpfulVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceHelpfulVotes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceHelpfulVotes_StudyResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "StudyResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceRatings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceRatings", x => x.Id);
                    table.CheckConstraint("CK_ResourceRatings_Value", "[Value] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_ResourceRatings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceRatings_StudyResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "StudyResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    RequesterId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DesiredResourceType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    BountyPoints = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    FulfilledByResourceId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FulfilledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceRequests", x => x.Id);
                    table.CheckConstraint("CK_ResourceRequests_Bounty", "[BountyPoints] IN (20, 40, 60, 80)");
                    table.ForeignKey(
                        name: "FK_ResourceRequests_AspNetUsers_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceRequests_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceRequests_StudyResources_FulfilledByResourceId",
                        column: x => x.FulfilledByResourceId,
                        principalTable: "StudyResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResourceTags",
                columns: table => new
                {
                    ResourceId = table.Column<long>(type: "bigint", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceTags", x => new { x.ResourceId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ResourceTags_StudyResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "StudyResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedResources",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ResourceId = table.Column<long>(type: "bigint", nullable: false),
                    SavedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedResources", x => new { x.UserId, x.ResourceId });
                    table.ForeignKey(
                        name: "FK_SavedResources_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SavedResources_StudyResources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "StudyResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceRequestVotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ResourceRequestId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceRequestVotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceRequestVotes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceRequestVotes_ResourceRequests_ResourceRequestId",
                        column: x => x.ResourceRequestId,
                        principalTable: "ResourceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_ActorUserId_CreatedAtUtc",
                table: "ActivityLogs",
                columns: new[] { "ActorUserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_EntityType_EntityId_CreatedAtUtc",
                table: "ActivityLogs",
                columns: new[] { "EntityType", "EntityId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_MajorId",
                table: "AspNetUsers",
                column: "MajorId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_UniversityId_MajorId",
                table: "AspNetUsers",
                columns: new[] { "UniversityId", "MajorId" });

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_IsActive_Name",
                table: "Courses",
                columns: new[] { "IsActive", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_MajorId_Code_Semester",
                table: "Courses",
                columns: new[] { "MajorId", "Code", "Semester" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Majors_UniversityId_IsActive",
                table: "Majors",
                columns: new[] { "UniversityId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Majors_UniversityId_NormalizedName",
                table: "Majors",
                columns: new[] { "UniversityId", "NormalizedName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Opportunities_IsPublished_IsActive_StartsAtUtc",
                table: "Opportunities",
                columns: new[] { "IsPublished", "IsActive", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityRegistrations_OpportunityId_UserId",
                table: "OpportunityRegistrations",
                columns: new[] { "OpportunityId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OpportunityRegistrations_UserId_Status",
                table: "OpportunityRegistrations",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReputationTransactions_IdempotencyKey",
                table: "ReputationTransactions",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReputationTransactions_UserId_CreatedAtUtc",
                table: "ReputationTransactions",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceHelpfulVotes_ResourceId_UserId",
                table: "ResourceHelpfulVotes",
                columns: new[] { "ResourceId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceHelpfulVotes_UserId",
                table: "ResourceHelpfulVotes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRatings_ResourceId_UserId",
                table: "ResourceRatings",
                columns: new[] { "ResourceId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRatings_UserId",
                table: "ResourceRatings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequests_CourseId",
                table: "ResourceRequests",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequests_FulfilledByResourceId",
                table: "ResourceRequests",
                column: "FulfilledByResourceId",
                filter: "[FulfilledByResourceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequests_RequesterId",
                table: "ResourceRequests",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequests_Status_CourseId_CreatedAtUtc",
                table: "ResourceRequests",
                columns: new[] { "Status", "CourseId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequestVotes_ResourceRequestId_UserId",
                table: "ResourceRequestVotes",
                columns: new[] { "ResourceRequestId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceRequestVotes_UserId",
                table: "ResourceRequestVotes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceTags_TagId",
                table: "ResourceTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_RoadmapNodes_CourseId_Order",
                table: "RoadmapNodes",
                columns: new[] { "CourseId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SavedResources_ResourceId",
                table: "SavedResources",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedResources_UserId_SavedAtUtc",
                table: "SavedResources",
                columns: new[] { "UserId", "SavedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyResources_CourseId_VerificationStatus_IsRepresentative",
                table: "StudyResources",
                columns: new[] { "CourseId", "VerificationStatus", "IsRepresentative" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyResources_PublicationStatus_CourseId_Type_CreatedAtUtc",
                table: "StudyResources",
                columns: new[] { "PublicationStatus", "CourseId", "Type", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyResources_Sha256",
                table: "StudyResources",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_StudyResources_UploaderId",
                table: "StudyResources",
                column: "UploaderId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_NormalizedName",
                table: "Tags",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Universities_IsActive",
                table: "Universities",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Universities_NormalizedName",
                table: "Universities",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserCourses_CourseId",
                table: "UserCourses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCourses_UserId_IsActive",
                table: "UserCourses",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoadmapProgress_RoadmapNodeId",
                table: "UserRoadmapProgress",
                column: "RoadmapNodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "OpportunityRegistrations");

            migrationBuilder.DropTable(
                name: "ReputationTransactions");

            migrationBuilder.DropTable(
                name: "ResourceHelpfulVotes");

            migrationBuilder.DropTable(
                name: "ResourceRatings");

            migrationBuilder.DropTable(
                name: "ResourceRequestVotes");

            migrationBuilder.DropTable(
                name: "ResourceTags");

            migrationBuilder.DropTable(
                name: "SavedResources");

            migrationBuilder.DropTable(
                name: "UserCourses");

            migrationBuilder.DropTable(
                name: "UserRoadmapProgress");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Opportunities");

            migrationBuilder.DropTable(
                name: "ResourceRequests");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "RoadmapNodes");

            migrationBuilder.DropTable(
                name: "StudyResources");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Majors");

            migrationBuilder.DropTable(
                name: "Universities");
        }
    }
}
