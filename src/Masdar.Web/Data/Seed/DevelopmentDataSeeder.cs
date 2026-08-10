using System.Text;
using Masdar.Web.Domain.Entities;
using Masdar.Web.Domain.Enums;
using Masdar.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Masdar.Web.Data.Seed;

public static class DevelopmentDataSeeder
{
    public const string AdminRole = "Admin";
    public const string UserRole = "User";

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        var environment = services.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            return;
        }

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        await EnsureRolesAsync(roleManager);
        var academics = await SeedAcademicsAsync(dbContext, cancellationToken);
        var users = await SeedUsersAsync(dbContext, userManager, configuration, academics, cancellationToken);
        await SeedUserSelectionsAsync(dbContext, users.Student, academics.Courses, cancellationToken);
        var resources = await SeedResourcesAsync(dbContext, fileStorage, users, academics.Courses, cancellationToken);
        await SeedResourceSignalsAsync(dbContext, resources, users, cancellationToken);
        await SeedRoadmapsAsync(dbContext, users.Student, academics.Courses, cancellationToken);
        await SeedRequestsAsync(dbContext, users, academics.Courses, cancellationToken);
        await SeedOpportunitiesAsync(dbContext, users.Student, cancellationToken);
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in new[] { AdminRole, UserRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                EnsureIdentitySucceeded(result, $"create the {role} role");
            }
        }
    }

    private static async Task<AcademicSeedResult> SeedAcademicsAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var universitySeeds = new[]
        {
            new UniversitySeed(1, "Jordan University of Science and Technology", "Irbid, Jordan"),
            new UniversitySeed(2, "University of Jordan", "Amman, Jordan"),
            new UniversitySeed(3, "Yarmouk University", "Irbid, Jordan"),
            new UniversitySeed(4, "Princess Sumaya University for Technology", "Amman, Jordan"),
            new UniversitySeed(5, "The Hashemite University", "Zarqa, Jordan"),
            new UniversitySeed(6, "German Jordanian University", "Amman, Jordan"),
            new UniversitySeed(7, "Al-Balqa Applied University", "Salt, Jordan")
        };
        var universities = new Dictionary<int, University>();
        foreach (var seed in universitySeeds)
        {
            var normalized = Normalize(seed.Name);
            var entity = await dbContext.Universities.SingleOrDefaultAsync(x => x.NormalizedName == normalized, cancellationToken);
            if (entity is null)
            {
                entity = new University { Name = seed.Name, NormalizedName = normalized, Location = seed.Location };
                dbContext.Universities.Add(entity);
            }

            universities[seed.SeedId] = entity;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var majorSeeds = new[]
        {
            new MajorSeed(1, 1, "Computer Science"), new MajorSeed(2, 1, "Software Engineering"),
            new MajorSeed(3, 2, "Data Science"), new MajorSeed(4, 3, "Information Technology"),
            new MajorSeed(5, 1, "Artificial Intelligence"), new MajorSeed(6, 2, "Computer Engineering"),
            new MajorSeed(7, 2, "Business Information Systems"), new MajorSeed(8, 3, "Cybersecurity"),
            new MajorSeed(9, 4, "Computer Science"), new MajorSeed(10, 4, "Software Engineering"),
            new MajorSeed(11, 5, "Computer Science"), new MajorSeed(12, 6, "Design and Media Informatics"),
            new MajorSeed(13, 7, "Computer Science"), new MajorSeed(14, 5, "Cybersecurity")
        };
        var majors = new Dictionary<int, Major>();
        foreach (var seed in majorSeeds)
        {
            var university = universities[seed.UniversitySeedId];
            var normalized = Normalize(seed.Name);
            var entity = await dbContext.Majors.SingleOrDefaultAsync(
                x => x.UniversityId == university.Id && x.NormalizedName == normalized,
                cancellationToken);
            if (entity is null)
            {
                entity = new Major
                {
                    UniversityId = university.Id,
                    Name = seed.Name,
                    NormalizedName = normalized
                };
                dbContext.Majors.Add(entity);
            }

            majors[seed.SeedId] = entity;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var courseSeeds = new[]
        {
            new CourseSeed(1, 1, "MATH 201", "Calculus II", "Dr. Ahmed", 2026, 5, 18),
            new CourseSeed(2, 1, "CS 211", "Data Structures and Algorithms", "Dr. Laila", 2026, 5, 23),
            new CourseSeed(3, 1, "CS 342", "Operating Systems", "Dr. Bashar", 2026, 5, 28),
            new CourseSeed(4, 2, "SE 312", "Software Architecture", "Dr. Omar", 2026, 6, 2),
            new CourseSeed(5, 3, "DATA 240", "Applied Statistics", "Dr. Huda", 2026, 5, 25),
            new CourseSeed(6, 4, "CIS 220", "Database Systems", "Dr. Khaled", 2026, 5, 29),
            new CourseSeed(7, 5, "AI 301", "Machine Learning", "Dr. Noor", 2026, 5, 30),
            new CourseSeed(8, 6, "CPE 321", "Computer Networks", "Dr. Sami", 2026, 6, 4),
            new CourseSeed(9, 7, "BIS 230", "Business Analytics", "Dr. Reem", 2026, 6, 6),
            new CourseSeed(10, 8, "CYB 310", "Network Security", "Dr. Fadi", 2026, 5, 27),
            new CourseSeed(11, 9, "CS 210", "Algorithm Design", "Dr. Dana", 2026, 5, 31),
            new CourseSeed(12, 9, "CS 330", "Web Application Development", "Dr. Tareq", 2026, 6, 8),
            new CourseSeed(13, 10, "SE 260", "Software Testing", "Dr. Majd", 2026, 6, 1),
            new CourseSeed(14, 11, "CS 205", "Discrete Mathematics", "Dr. Hanan", 2026, 5, 26),
            new CourseSeed(15, 12, "UX 220", "Interaction Design", "Dr. Rania", 2026, 6, 10),
            new CourseSeed(16, 13, "CS 101", "Programming Fundamentals", "Dr. Ayman", 2026, 5, 24),
            new CourseSeed(17, 14, "CYB 240", "Ethical Hacking", "Dr. Qais", 2026, 6, 3),
            new CourseSeed(18, 1, "MATH 101", "Calculus I", "Dr. Dima", 2026, 5, 21),
            new CourseSeed(19, 2, "SE 401", "DevOps and Cloud Systems", "Dr. Laith", 2026, 6, 11),
            new CourseSeed(20, 5, "AI 220", "Data Mining", "Dr. Saba", 2026, 6, 5)
        };
        var courses = new Dictionary<int, Course>();
        foreach (var seed in courseSeeds)
        {
            var major = majors[seed.MajorSeedId];
            var entity = await dbContext.Courses.SingleOrDefaultAsync(
                x => x.MajorId == major.Id && x.Code == seed.Code && x.Semester == "Spring 2026",
                cancellationToken);
            if (entity is null)
            {
                entity = new Course
                {
                    MajorId = major.Id,
                    Code = seed.Code,
                    Name = seed.Name,
                    Instructor = seed.Instructor,
                    Semester = "Spring 2026",
                    ExamDateUtc = new DateTime(seed.Year, seed.Month, seed.Day, 9, 0, 0, DateTimeKind.Utc),
                    Description = $"Course workspace for {seed.Name}, including materials, roadmap milestones, requests, and career links."
                };
                dbContext.Courses.Add(entity);
            }

            courses[seed.SeedId] = entity;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new AcademicSeedResult(universities, majors, courses);
    }

    private static async Task<UserSeedResult> SeedUsersAsync(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        AcademicSeedResult academics,
        CancellationToken cancellationToken)
    {
        var adminEmail = configuration["SeedData:AdminEmail"] ?? configuration["SeedAdmin:Email"] ?? configuration["Seed:Admin:Email"] ?? "admin@masdar.local";
        var userEmail = configuration["SeedData:UserEmail"] ?? configuration["SeedUser:Email"] ?? configuration["Seed:User:Email"] ?? "user@masdar.local";
        var adminPassword = configuration["SeedData:AdminPassword"] ?? configuration["SeedAdmin:Password"] ?? configuration["Seed:Admin:Password"] ?? "Masdar.Admin.Dev2026!";
        var userPassword = configuration["SeedData:UserPassword"] ?? configuration["SeedUser:Password"] ?? configuration["Seed:User:Password"] ?? "Masdar.User.Dev2026!";

        var admin = await EnsureLoginUserAsync(
            userManager,
            adminEmail,
            adminPassword,
            "Masdar Administrator",
            academics.Universities[1].Id,
            academics.Majors[1].Id,
            1000,
            AdminRole);
        var student = await EnsureLoginUserAsync(
            userManager,
            userEmail,
            userPassword,
            "Student Ahmed",
            academics.Universities[1].Id,
            academics.Majors[1].Id,
            150,
            UserRole);

        var people = new Dictionary<string, ApplicationUser>(StringComparer.OrdinalIgnoreCase)
        {
            [admin.DisplayName] = admin,
            [student.DisplayName] = student
        };
        var contributorSeeds = new[]
        {
            new PersonSeed("Salma Nabulsi", 2, 860), new PersonSeed("Rasha Al-Sayed", 1, 740),
            new PersonSeed("Hasan Khatib", 1, 615), new PersonSeed("Zainab Radi", 1, 390),
            new PersonSeed("Lina Masri", 3, 590), new PersonSeed("Omar Darwish", 8, 520),
            new PersonSeed("Leen Abbadi", 9, 465), new PersonSeed("Yousef Naser", 6, 355),
            new PersonSeed("Raneem Jaber", 4, 670), new PersonSeed("Farah Saleh", 12, 548),
            new PersonSeed("Samer Taha", 5, 705), new PersonSeed("Mariam Haddad", 7, 310),
            new PersonSeed("Qais Hamdan", 14, 815), new PersonSeed("Ahmad Sweidan", 1, 280),
            new PersonSeed("Mohammad H.", 1, 260), new PersonSeed("Masdar Curation", 1, 500),
            new PersonSeed("Omar K.", 1, 240), new PersonSeed("Dana M.", 2, 230)
        };
        foreach (var seed in contributorSeeds)
        {
            people[seed.DisplayName] = await EnsureSeedPersonAsync(dbContext, seed, academics, cancellationToken);
        }

        var requesterNames = new[]
        {
            "Lina M.", "Khaled A.", "Aya S.", "Yousef N.", "Mariam H.", "Samer T.", "Raneem J.",
            "Omar D.", "Leen A.", "Basil K.", "Farah S.", "Nour Q.", "Mahmoud R.", "Hala Z."
        };
        for (var index = 0; index < requesterNames.Length; index++)
        {
            var majorSeedId = ((index + 1) % academics.Majors.Count) + 1;
            var seed = new PersonSeed(requesterNames[index], majorSeedId, 220);
            people[seed.DisplayName] = await EnsureSeedPersonAsync(dbContext, seed, academics, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        foreach (var person in people.Values.DistinctBy(x => x.Id))
        {
            var key = $"initial-grant:{person.Id}";
            if (!await dbContext.ReputationTransactions.AnyAsync(x => x.IdempotencyKey == key, cancellationToken))
            {
                dbContext.ReputationTransactions.Add(new ReputationTransaction
                {
                    UserId = person.Id,
                    Type = ReputationTransactionType.InitialGrant,
                    Points = person.ReputationBalance,
                    BalanceAfter = person.ReputationBalance,
                    Reason = "Development seed reputation balance.",
                    IdempotencyKey = key,
                    RelatedEntityType = nameof(ApplicationUser),
                    RelatedEntityId = person.Id
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new UserSeedResult(admin, student, people);
    }

    private static async Task<ApplicationUser> EnsureLoginUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string displayName,
        int universityId,
        int majorId,
        int reputation,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                UniversityId = universityId,
                MajorId = majorId,
                ReputationBalance = reputation,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            EnsureIdentitySucceeded(await userManager.CreateAsync(user, password), $"create {email}");
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            EnsureIdentitySucceeded(await userManager.AddToRoleAsync(user, role), $"assign {email} to {role}");
        }

        return user;
    }

    private static async Task<ApplicationUser> EnsureSeedPersonAsync(
        ApplicationDbContext dbContext,
        PersonSeed seed,
        AcademicSeedResult academics,
        CancellationToken cancellationToken)
    {
        var email = $"seed.{Slug(seed.DisplayName)}@masdar.local";
        var normalizedEmail = Normalize(email);
        var existing = await dbContext.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var major = academics.Majors[seed.MajorSeedId];
        var person = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = email,
            NormalizedUserName = normalizedEmail,
            Email = email,
            NormalizedEmail = normalizedEmail,
            EmailConfirmed = false,
            DisplayName = seed.DisplayName,
            UniversityId = major.UniversityId,
            MajorId = major.Id,
            ReputationBalance = seed.Reputation,
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        dbContext.Users.Add(person);
        await dbContext.SaveChangesAsync(cancellationToken);
        return person;
    }

    private static async Task SeedUserSelectionsAsync(
        ApplicationDbContext dbContext,
        ApplicationUser student,
        IReadOnlyDictionary<int, Course> courses,
        CancellationToken cancellationToken)
    {
        foreach (var seedCourseId in new[] { 1, 2 })
        {
            var courseId = courses[seedCourseId].Id;
            if (!await dbContext.UserCourses.AnyAsync(x => x.UserId == student.Id && x.CourseId == courseId, cancellationToken))
            {
                dbContext.UserCourses.Add(new UserCourse
                {
                    UserId = student.Id,
                    CourseId = courseId,
                    IsActive = seedCourseId == 1,
                    SelectedAtUtc = DateTime.UtcNow,
                    LastAccessedAtUtc = DateTime.UtcNow
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<StudyResource>> SeedResourcesAsync(
        ApplicationDbContext dbContext,
        IFileStorageService fileStorage,
        UserSeedResult users,
        IReadOnlyDictionary<int, Course> courses,
        CancellationToken cancellationToken)
    {
        var seeds = new List<ResourceSeed>
        {
            new(1, "Midterm Exam 2025 - Solved", ResourceType.PreviousExam, "Spring 2025", "Rasha Al-Sayed", true, true, ["midterm", "integration", "series"]),
            new(1, "Integration Techniques - Visual Summary", ResourceType.LectureSummary, "Spring 2026", "Ahmad Sweidan", true, false, ["integration", "by-parts", "trig-sub"]),
            new(1, "Infinite Series Quick Reference", ResourceType.CheatSheet, "Spring 2026", "Hasan Khatib", true, false, ["series", "convergence", "taylor"]),
            new(1, "Week 8-10 Student Notes", ResourceType.StudentNote, "Spring 2026", "Zainab Radi", false, false, ["applications", "volumes", "arc-length"]),
            new(2, "AVL Trees and Rotations - Quiz Bank", ResourceType.QuizBank, "Spring 2026", "Salma Nabulsi", true, true, ["avl", "trees", "rotations"]),
            new(2, "Graph Algorithms Assignment 3", ResourceType.Assignment, "Spring 2026", "Mohammad H.", true, false, ["graphs", "bfs", "dfs"]),
            new(2, "How Dijkstra Powers GPS Routing", ResourceType.RecommendedVideo, "Spring 2026", "Masdar Curation", true, false, ["dijkstra", "gps", "career"]),
            new(2, "Interactive Binary Search Tree Visualizer", ResourceType.ExternalResource, "Spring 2026", "Masdar Curation", true, false, ["bst", "visualizer", "practice"]),
            new(3, "Process Scheduling Summary", ResourceType.LectureSummary, "Spring 2026", "Omar K.", true, false, ["scheduling", "processes"]),
            new(4, "Architecture Patterns Cheat Sheet", ResourceType.CheatSheet, "Spring 2026", "Dana M.", false, false, ["mvc", "layers", "patterns"])
        };
        var topics = CourseTopics();
        var uploaders = new[]
        {
            "Salma Nabulsi", "Rasha Al-Sayed", "Hasan Khatib", "Lina Masri", "Omar Darwish",
            "Leen Abbadi", "Raneem Jaber", "Farah Saleh", "Samer Taha", "Mariam Haddad"
        };
        var allTypes = Enum.GetValues<ResourceType>();
        foreach (var pair in courses.OrderBy(x => x.Key))
        {
            var existingTypes = seeds.Where(x => x.CourseSeedId == pair.Key).Select(x => x.Type).ToHashSet();
            var courseTopics = topics[pair.Key];
            var offset = 0;
            foreach (var type in allTypes.Where(type => !existingTypes.Contains(type)))
            {
                var topic = courseTopics[offset++ % courseTopics.Length];
                seeds.Add(new ResourceSeed(
                    pair.Key,
                    GeneratedResourceTitle(pair.Value, type, topic),
                    type,
                    offset % 3 == 0 ? "Fall 2025" : "Spring 2026",
                    uploaders[(pair.Key + offset) % uploaders.Length],
                    offset % 5 != 1,
                    type == ResourceType.PreviousExam && offset % 2 == 0,
                    [topic.Split(' ')[0], topic.Split(' ')[^1], offset % 2 == 0 ? "exam-ready" : "practice"]));
            }
        }

        var seededResources = new List<StudyResource>();
        foreach (var seed in seeds)
        {
            var course = courses[seed.CourseSeedId];
            var resource = await dbContext.StudyResources
                .Include(x => x.ResourceTags)
                .SingleOrDefaultAsync(x => x.CourseId == course.Id && x.Title == seed.Title, cancellationToken);
            if (resource is null)
            {
                var file = CreateSeedPdf(seed.Title);
                var stored = await fileStorage.SaveValidatedAsync(file, cancellationToken);
                var now = DateTime.UtcNow.AddDays(-(seed.CourseSeedId + (int)seed.Type));
                resource = new StudyResource
                {
                    CourseId = course.Id,
                    UploaderId = users.ByDisplayName[seed.Uploader].Id,
                    Title = seed.Title,
                    Type = seed.Type,
                    Semester = seed.Semester,
                    Description = $"A structured {DisplayResourceType(seed.Type).ToLowerInvariant()} for {course.Code}. It covers {string.Join(", ", seed.Tags)} with worked examples, review guidance, and exam-ready next steps.",
                    TutorContext = $"Use this resource when answering questions about {string.Join(", ", seed.Tags)} in {course.Code}.",
                    PublicationStatus = ResourcePublicationStatus.Published,
                    VerificationStatus = seed.Verified ? ResourceVerificationStatus.Verified : ResourceVerificationStatus.Unverified,
                    IsRepresentative = seed.Representative,
                    StorageKey = stored.StorageKey,
                    OriginalFileName = stored.OriginalFileName,
                    ContentType = stored.ContentType,
                    FileSizeBytes = stored.SizeBytes,
                    Sha256 = stored.Sha256,
                    FileScanStatus = FileScanStatus.Validated,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    PublishedAtUtc = now
                };
                dbContext.StudyResources.Add(resource);
                foreach (var tagName in seed.Tags.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var normalized = Normalize(tagName);
                    var tag = dbContext.Tags.Local.FirstOrDefault(x => x.NormalizedName == normalized)
                        ?? await dbContext.Tags.SingleOrDefaultAsync(x => x.NormalizedName == normalized, cancellationToken);
                    if (tag is null)
                    {
                        tag = new Tag { Name = tagName, NormalizedName = normalized };
                        dbContext.Tags.Add(tag);
                    }

                    resource.ResourceTags.Add(new ResourceTag { Resource = resource, Tag = tag });
                }
            }

            seededResources.Add(resource);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return seededResources;
    }

    private static async Task SeedResourceSignalsAsync(
        ApplicationDbContext dbContext,
        IReadOnlyList<StudyResource> resources,
        UserSeedResult users,
        CancellationToken cancellationToken)
    {
        var people = users.ByDisplayName.Values.DistinctBy(x => x.Id).ToArray();
        var resourceIds = resources.Select(x => x.Id).ToArray();
        var ratingKeys = (await dbContext.ResourceRatings
                .Where(x => resourceIds.Contains(x.ResourceId))
                .Select(x => new { x.ResourceId, x.UserId })
                .ToListAsync(cancellationToken))
            .Select(x => $"{x.ResourceId}:{x.UserId}").ToHashSet();
        var helpfulKeys = (await dbContext.ResourceHelpfulVotes
                .Where(x => resourceIds.Contains(x.ResourceId))
                .Select(x => new { x.ResourceId, x.UserId })
                .ToListAsync(cancellationToken))
            .Select(x => $"{x.ResourceId}:{x.UserId}").ToHashSet();

        for (var resourceIndex = 0; resourceIndex < resources.Count; resourceIndex++)
        {
            var resource = resources[resourceIndex];
            for (var index = 0; index < Math.Min(5, people.Length); index++)
            {
                var person = people[(resourceIndex + index) % people.Length];
                var key = $"{resource.Id}:{person.Id}";
                if (ratingKeys.Add(key))
                {
                    dbContext.ResourceRatings.Add(new ResourceRating
                    {
                        ResourceId = resource.Id,
                        UserId = person.Id,
                        Value = index == 3 ? 4 : 5,
                        CreatedAtUtc = resource.CreatedAtUtc.AddHours(index),
                        UpdatedAtUtc = resource.CreatedAtUtc.AddHours(index)
                    });
                }

                if (index < 4 && helpfulKeys.Add(key))
                {
                    dbContext.ResourceHelpfulVotes.Add(new ResourceHelpfulVote
                    {
                        ResourceId = resource.Id,
                        UserId = person.Id,
                        CreatedAtUtc = resource.CreatedAtUtc.AddHours(index)
                    });
                }
            }

            if (resourceIndex % 11 == 1 && !await dbContext.SavedResources.AnyAsync(
                    x => x.UserId == users.Student.Id && x.ResourceId == resource.Id,
                    cancellationToken))
            {
                dbContext.SavedResources.Add(new SavedResource { UserId = users.Student.Id, ResourceId = resource.Id });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRoadmapsAsync(
        ApplicationDbContext dbContext,
        ApplicationUser student,
        IReadOnlyDictionary<int, Course> courses,
        CancellationToken cancellationToken)
    {
        var topics = CourseTopics();
        foreach (var pair in courses)
        {
            if (await dbContext.RoadmapNodes.AnyAsync(x => x.CourseId == pair.Value.Id, cancellationToken))
            {
                continue;
            }

            IReadOnlyList<RoadmapSeed> nodes = pair.Key switch
            {
                1 =>
                [
                    new("Limits and continuity", "Core definitions, L'Hopital's rule, and continuity tests.", RoadmapDifficulty.Easy, 20),
                    new("Integration techniques", "By parts, trigonometric integrals, substitutions, and partial fractions.", RoadmapDifficulty.Medium, 35),
                    new("Applications of integration", "Areas, volumes, arc length, and physical applications.", RoadmapDifficulty.Medium, 20),
                    new("Infinite and Taylor series", "Convergence tests, power series, and approximation.", RoadmapDifficulty.Hard, 25)
                ],
                2 =>
                [
                    new("Complexity and linear structures", "Big-O, arrays, linked lists, stacks, and queues.", RoadmapDifficulty.Easy, 20),
                    new("Trees and balancing", "BST operations, AVL rotations, heaps, and priority queues.", RoadmapDifficulty.Medium, 40),
                    new("Graphs and shortest paths", "Representations, traversals, Dijkstra, and MST algorithms.", RoadmapDifficulty.Hard, 40)
                ],
                _ => topics[pair.Key].Select((topic, index) => new RoadmapSeed(
                    TitleCase(topic),
                    $"Understand the core ideas, solve guided examples, then practice exam-style questions on {topic}.",
                    new[] { RoadmapDifficulty.Easy, RoadmapDifficulty.Medium, RoadmapDifficulty.Medium, RoadmapDifficulty.Hard }[index],
                    new[] { 15, 25, 30, 30 }[index])).ToArray()
            };

            for (var index = 0; index < nodes.Count; index++)
            {
                var seed = nodes[index];
                dbContext.RoadmapNodes.Add(new RoadmapNode
                {
                    CourseId = pair.Value.Id,
                    Order = index + 1,
                    Title = seed.Title,
                    Description = seed.Description,
                    Difficulty = seed.Difficulty,
                    ExamWeightPercent = seed.Weight
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var firstNode = await dbContext.RoadmapNodes
            .Where(x => x.CourseId == courses[1].Id)
            .OrderBy(x => x.Order)
            .FirstAsync(cancellationToken);
        if (!await dbContext.UserRoadmapProgress.AnyAsync(
                x => x.UserId == student.Id && x.RoadmapNodeId == firstNode.Id,
                cancellationToken))
        {
            dbContext.UserRoadmapProgress.Add(new UserRoadmapProgress
            {
                UserId = student.Id,
                RoadmapNodeId = firstNode.Id,
                IsCompleted = true,
                CompletedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedRequestsAsync(
        ApplicationDbContext dbContext,
        UserSeedResult users,
        IReadOnlyDictionary<int, Course> courses,
        CancellationToken cancellationToken)
    {
        var seeds = new[]
        {
            new RequestSeed(1, ResourceType.PreviousExam, "Second exam 2024 with solutions", "Lina M.", 18, 60),
            new RequestSeed(1, ResourceType.QuizBank, "Practice questions on infinite series", "Khaled A.", 12, 40),
            new RequestSeed(2, ResourceType.CheatSheet, "Graph algorithms one-page summary", "Aya S.", 21, 80),
            new RequestSeed(3, ResourceType.LectureSummary, "Deadlocks and synchronization summary", "Yousef N.", 16, 40),
            new RequestSeed(6, ResourceType.PreviousExam, "Database final exam from 2025", "Mariam H.", 29, 80),
            new RequestSeed(7, ResourceType.Assignment, "Solved gradient descent assignment", "Samer T.", 24, 60),
            new RequestSeed(8, ResourceType.QuizBank, "Subnetting practice with answer key", "Raneem J.", 35, 80),
            new RequestSeed(10, ResourceType.CheatSheet, "Common cryptography algorithms comparison", "Omar D.", 14, 40),
            new RequestSeed(12, ResourceType.StudentNote, "React state management lecture notes", "Leen A.", 11, 20),
            new RequestSeed(13, ResourceType.PreviousExam, "Software testing midterm with rubric", "Basil K.", 19, 60),
            new RequestSeed(15, ResourceType.ExternalResource, "Accessible prototyping checklist", "Farah S.", 17, 40),
            new RequestSeed(17, ResourceType.RecommendedVideo, "Beginner-friendly web exploitation lab", "Nour Q.", 31, 80),
            new RequestSeed(19, ResourceType.LectureSummary, "CI/CD pipeline summary and diagram", "Mahmoud R.", 23, 60),
            new RequestSeed(20, ResourceType.QuizBank, "Association rules practice questions", "Hala Z.", 13, 40)
        };
        var voters = users.ByDisplayName.Values.DistinctBy(x => x.Id).ToArray();
        foreach (var seed in seeds)
        {
            var courseId = courses[seed.CourseSeedId].Id;
            var request = await dbContext.ResourceRequests.SingleOrDefaultAsync(
                x => x.CourseId == courseId && x.Title == seed.Title,
                cancellationToken);
            var requester = users.ByDisplayName[seed.Requester];
            if (request is null)
            {
                request = new ResourceRequest
                {
                    CourseId = courseId,
                    RequesterId = requester.Id,
                    DesiredResourceType = seed.Type,
                    Title = seed.Title,
                    Description = $"Community request for {seed.Title.ToLowerInvariant()}.",
                    BountyPoints = seed.Bounty,
                    Status = ResourceRequestStatus.Open,
                    CreatedAtUtc = DateTime.UtcNow.AddDays(-seed.CourseSeedId),
                    UpdatedAtUtc = DateTime.UtcNow.AddDays(-seed.CourseSeedId)
                };
                dbContext.ResourceRequests.Add(request);
                requester.ReputationBalance -= seed.Bounty;
                await dbContext.SaveChangesAsync(cancellationToken);
                dbContext.ReputationTransactions.Add(new ReputationTransaction
                {
                    UserId = requester.Id,
                    Type = ReputationTransactionType.RequestBountyReserved,
                    Points = -seed.Bounty,
                    BalanceAfter = requester.ReputationBalance,
                    Reason = "Reserved a development seed request bounty.",
                    IdempotencyKey = $"request-bounty-reserved:{request.Id}",
                    RelatedEntityType = nameof(ResourceRequest),
                    RelatedEntityId = request.Id.ToString()
                });
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var currentVoterIds = (await dbContext.ResourceRequestVotes
                    .Where(x => x.ResourceRequestId == request.Id)
                    .Select(x => x.UserId)
                    .ToListAsync(cancellationToken))
                .ToHashSet();
            foreach (var voter in voters.Where(x => x.Id != requester.Id && !currentVoterIds.Contains(x.Id)).Take(Math.Max(0, seed.Votes - currentVoterIds.Count)))
            {
                dbContext.ResourceRequestVotes.Add(new ResourceRequestVote
                {
                    ResourceRequestId = request.Id,
                    UserId = voter.Id,
                    CreatedAtUtc = request.CreatedAtUtc.AddMinutes(currentVoterIds.Count + 1)
                });
                currentVoterIds.Add(voter.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedOpportunitiesAsync(
        ApplicationDbContext dbContext,
        ApplicationUser student,
        CancellationToken cancellationToken)
    {
        var seeds = new[]
        {
            new OpportunitySeed(OpportunityType.Hackathon, "IEEE JUST Hackathon 2026", 4, 18, "Build sustainable education technology with a university team."),
            new OpportunitySeed(OpportunityType.Competition, "National ICPC Qualifier", 5, 2, "Form a team and solve algorithmic programming challenges."),
            new OpportunitySeed(OpportunityType.Workshop, "Orange Backend Academy", 6, 12, "Hands-on workshop on APIs, PostgreSQL, and cloud systems."),
            new OpportunitySeed(OpportunityType.Internship, "ProgressSoft Summer Internship", 6, 21, "A structured internship in software engineering, QA, and financial technology."),
            new OpportunitySeed(OpportunityType.Meetup, "Amman AI Community Night", 7, 4, "Student-friendly talks about applied machine learning and responsible AI."),
            new OpportunitySeed(OpportunityType.Bootcamp, "Zain Cybersecurity Bootcamp", 7, 16, "Practice threat modeling, web security, network defense, and incident response."),
            new OpportunitySeed(OpportunityType.Competition, "Jordan Data Challenge", 8, 8, "Analyze a public dataset and present an evidence-backed recommendation."),
            new OpportunitySeed(OpportunityType.Workshop, "Design Systems for Developers", 8, 20, "Build accessible interfaces using reusable tokens and component patterns."),
            new OpportunitySeed(OpportunityType.Scholarship, "AWS Cloud Practitioner Path", 9, 3, "A sponsored certification path covering cloud foundations and architecture."),
            new OpportunitySeed(OpportunityType.Volunteer, "Code for Schools Mentorship", 9, 12, "Mentor school students through a four-week introductory programming track."),
            new OpportunitySeed(OpportunityType.Talk, "Careers in Product Engineering", 10, 1, "Engineers and product designers discuss portfolios, interviews, and first roles."),
            new OpportunitySeed(OpportunityType.Hackathon, "NASA Space Apps Amman", 10, 10, "Solve an open science challenge using software, data, design, and teamwork.")
        };
        for (var index = 0; index < seeds.Length; index++)
        {
            var seed = seeds[index];
            var opportunity = await dbContext.Opportunities.SingleOrDefaultAsync(x => x.Title == seed.Title, cancellationToken);
            if (opportunity is null)
            {
                var starts = new DateTime(2026, seed.Month, seed.Day, 9, 0, 0, DateTimeKind.Utc);
                opportunity = new Opportunity
                {
                    Type = seed.Type,
                    Title = seed.Title,
                    Description = seed.Description,
                    Location = "Amman, Jordan",
                    StartsAtUtc = starts,
                    EndsAtUtc = starts.AddHours(seed.Type == OpportunityType.Internship ? 8 * 7 : 8),
                    RegistrationDeadlineUtc = starts.AddDays(-1),
                    Capacity = seed.Type is OpportunityType.Workshop or OpportunityType.Bootcamp ? 80 : null,
                    IsPublished = true,
                    IsActive = true
                };
                dbContext.Opportunities.Add(opportunity);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            if (index is 3 or 7 && !await dbContext.OpportunityRegistrations.AnyAsync(
                    x => x.OpportunityId == opportunity.Id && x.UserId == student.Id,
                    cancellationToken))
            {
                dbContext.OpportunityRegistrations.Add(new OpportunityRegistration
                {
                    OpportunityId = opportunity.Id,
                    UserId = student.Id,
                    Status = OpportunityRegistrationStatus.Registered,
                    RegisteredAtUtc = opportunity.StartsAtUtc.AddDays(-30),
                    UpdatedAtUtc = opportunity.StartsAtUtc.AddDays(-30)
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Dictionary<int, string[]> CourseTopics() => new()
    {
        [1] = ["integration techniques", "applications of integration", "infinite series", "Taylor series"],
        [2] = ["complexity analysis", "balanced trees", "graph traversal", "shortest paths"],
        [3] = ["process scheduling", "synchronization", "virtual memory", "file systems"],
        [4] = ["architecture patterns", "quality attributes", "microservices", "event-driven systems"],
        [5] = ["probability distributions", "hypothesis testing", "regression", "confidence intervals"],
        [6] = ["relational modeling", "SQL queries", "normalization", "transactions"],
        [7] = ["linear regression", "gradient descent", "classification", "model evaluation"],
        [8] = ["network layers", "subnetting", "routing protocols", "transport reliability"],
        [9] = ["data storytelling", "dashboard metrics", "forecasting", "decision models"],
        [10] = ["cryptography", "network attacks", "access control", "incident response"],
        [11] = ["divide and conquer", "dynamic programming", "greedy methods", "complexity proofs"],
        [12] = ["semantic HTML", "responsive CSS", "JavaScript state", "web APIs"],
        [13] = ["unit testing", "test design", "integration testing", "quality automation"],
        [14] = ["logic and proofs", "sets and relations", "combinatorics", "graph theory"],
        [15] = ["user research", "information architecture", "interaction patterns", "usability testing"],
        [16] = ["variables and control flow", "functions", "arrays", "object-oriented basics"],
        [17] = ["reconnaissance", "web exploitation", "privilege escalation", "reporting findings"],
        [18] = ["limits", "derivatives", "curve analysis", "optimization"],
        [19] = ["version control", "continuous integration", "containers", "cloud deployment"],
        [20] = ["data preparation", "association rules", "clustering", "feature selection"]
    };

    private static string GeneratedResourceTitle(Course course, ResourceType type, string topic) => type switch
    {
        ResourceType.PreviousExam => $"{course.Code} Final Exam 2025 - Fully Solved",
        ResourceType.Assignment => $"{TitleCase(topic)} Practice Assignment + Solutions",
        ResourceType.QuizBank => $"{TitleCase(topic)} Quiz Bank - 30 Questions",
        ResourceType.LectureSummary => $"{course.Name} Complete Lecture Summary",
        ResourceType.CheatSheet => $"{TitleCase(topic)} One-Page Cheat Sheet",
        ResourceType.StudentNote => $"{course.Code} Annotated Student Notes - Weeks 5-9",
        ResourceType.RecommendedVideo => $"{TitleCase(topic)} Explained Visually",
        _ => $"{course.Name} Interactive Practice Lab"
    };

    private static IFormFile CreateSeedPdf(string title)
    {
        var bytes = Encoding.UTF8.GetBytes($"%PDF-1.4\n% MASDAR development seed\n% {title}\n%%EOF\n");
        var stream = new MemoryStream(bytes, writable: false);
        return new FormFile(stream, 0, bytes.Length, "file", $"{Slug(title)}.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }

    private static string DisplayResourceType(ResourceType type) => type switch
    {
        ResourceType.PreviousExam => "Previous Exam", ResourceType.QuizBank => "Quiz Bank",
        ResourceType.LectureSummary => "Lecture Summary", ResourceType.CheatSheet => "Cheat Sheet",
        ResourceType.StudentNote => "Student Note", ResourceType.RecommendedVideo => "Recommended Video",
        ResourceType.ExternalResource => "External Resource", _ => "Assignment"
    };

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private static string Slug(string value)
    {
        var slug = new string(value.ToLowerInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '.').ToArray());
        while (slug.Contains("..", StringComparison.Ordinal)) slug = slug.Replace("..", ".", StringComparison.Ordinal);
        return slug.Trim('.');
    }

    private static string TitleCase(string value) => string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Select(word => char.ToUpperInvariant(word[0]) + word[1..]));

    private static void EnsureIdentitySucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Unable to {operation}: {string.Join("; ", result.Errors.Select(x => x.Description))}");
        }
    }

    private sealed record UniversitySeed(int SeedId, string Name, string Location);
    private sealed record MajorSeed(int SeedId, int UniversitySeedId, string Name);
    private sealed record CourseSeed(int SeedId, int MajorSeedId, string Code, string Name, string Instructor, int Year, int Month, int Day);
    private sealed record PersonSeed(string DisplayName, int MajorSeedId, int Reputation);
    private sealed record ResourceSeed(int CourseSeedId, string Title, ResourceType Type, string Semester, string Uploader, bool Verified, bool Representative, string[] Tags);
    private sealed record RoadmapSeed(string Title, string Description, RoadmapDifficulty Difficulty, int Weight);
    private sealed record RequestSeed(int CourseSeedId, ResourceType Type, string Title, string Requester, int Votes, int Bounty);
    private sealed record OpportunitySeed(OpportunityType Type, string Title, int Month, int Day, string Description);
    private sealed record AcademicSeedResult(
        IReadOnlyDictionary<int, University> Universities,
        IReadOnlyDictionary<int, Major> Majors,
        IReadOnlyDictionary<int, Course> Courses);
    private sealed record UserSeedResult(
        ApplicationUser Admin,
        ApplicationUser Student,
        IReadOnlyDictionary<string, ApplicationUser> ByDisplayName);
}
