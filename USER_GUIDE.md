# MASDAR User, Administrator, and Configuration Guide

This guide explains how to run and use MASDAR, which configuration values are required, and whether any API tokens are needed. For the full architecture and deployment article, see [README.md](README.md).

## 1. What is required?

| Item | Required? | Purpose |
| --- | --- | --- |
| .NET 8 SDK | Yes | Builds and runs the ASP.NET Core MVC application |
| SQL Server or SQL Server LocalDB | Yes | Stores users, courses, resources, progress, requests, opportunities, and audit records |
| `ConnectionStrings:DefaultConnection` | Yes | Tells EF Core which SQL Server database to use |
| `dotnet-ef` 8.x CLI | Optional | Needed only when applying or creating migrations manually |
| Gemini API key | Yes for tutor | Powers semantic embeddings and grounded answer generation; the rest of MASDAR works without it |
| Cloud-storage token | No | Development/MVP uploads use private local file storage |
| Email-provider token | No | Email confirmation is disabled in the current MVP |

The application will not start without a database connection string. Browsing and resource workflows do not require an external token, but the AI tutor requires a Gemini API key.

## 2. Quick start

From the repository root:

```powershell
dotnet restore .\Masdar.sln
dotnet run --project .\src\Masdar.Web\Masdar.Web.csproj
```

Open the URL printed by `dotnet run`.

The Development environment automatically applies committed migrations and runs the idempotent demonstration-data seeder when these settings are enabled in `appsettings.Development.json`.

## 3. Development accounts

| Role | Email | Password |
| --- | --- | --- |
| Administrator | `admin@masdar.local` | `Admin123!` |
| Student | `user@masdar.local` | `User123!` |

These credentials are only for local development. Change them before sharing a deployed environment.

## 4. Configuration reference

### Required database connection

Development defaults to SQL Server LocalDB:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=MasdarMvp;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

For a hosted SQL Server, supply the value as an environment variable or secret:

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=tcp:YOUR_SERVER,1433;Initial Catalog=Masdar;User ID=YOUR_USER;Password=YOUR_PASSWORD;Encrypt=True;TrustServerCertificate=False"
```

Do not commit a production connection string.

### Database and seed settings

| Key | Development default | Meaning |
| --- | --- | --- |
| `Database:ApplyMigrations` | `true` | Applies committed EF Core migrations during startup |
| `SeedData:Enabled` | `true` | Creates demonstration roles, users, courses, resources, and opportunities |
| `SeedData:AdminEmail` | `admin@masdar.local` | Development administrator email |
| `SeedData:AdminPassword` | `Admin123!` | Development administrator password |
| `SeedData:UserEmail` | `user@masdar.local` | Development student email |
| `SeedData:UserPassword` | `User123!` | Development student password |

For production, keep automatic migrations and development seeding disabled. Apply migrations as a controlled deployment step and provision the first administrator securely.

### Private file storage

| Key | Default | Meaning |
| --- | --- | --- |
| `FileStorage:RootPath` | `App_Data/uploads` | Private storage directory, relative to the web project |
| `FileStorage:MaximumBytes` | `10485760` | Maximum upload size: 10 MB |

Only PDF and DOCX files are accepted. MASDAR validates file extension, declared MIME type, binary signature, maximum size, safe storage path, SHA-256 hash, and DOCX archive structure. Uploaded files are not exposed directly from `wwwroot`; published, validated downloads pass through an application-controlled action.

## 5. Tokens and external services

### Course tutor / RAG

Set a Gemini API key using .NET user secrets; never commit it to configuration:

```powershell
dotnet user-secrets set "Rag:ApiKey" "<your-gemini-api-key>" --project .\src\Masdar.Web\Masdar.Web.csproj
```

The tutor reads published and validated PDF/DOCX files from the active course, chunks their text, retrieves relevant passages with Gemini embeddings, and sends only those passages to Gemini for a grounded answer. `GEMINI_API_KEY` or `Rag__ApiKey` can be used in hosted environments. Without a key, the tutor shows a configuration status while the rest of the application remains available.

Use only non-sensitive demonstration material with the Gemini free tier. A production deployment should complete a privacy review and use provider terms suitable for real student data.

### Cloud storage

No Azure Storage, Amazon S3, or other cloud-storage token is required for local use. If private cloud storage is added later, put credentials in a managed secret store or use workload/managed identity; do not place access keys in `appsettings.json`.

### Email

No email-provider token is required in the MVP. If account confirmation or password-reset delivery is enabled later, configure the chosen provider through environment variables or a secret manager.

## 6. Student guide

### Choose the active course

1. Sign in as a student.
2. Open **Courses** or **Explore resources**.
3. Choose a university and major if needed.
4. Select a course and choose **Make active**.
5. Confirm that the course code/name appears in the top bar and sidebar.

The active course controls the dashboard, progress, roadmap, resource recommendations, and tutor context. The tutor does not ask the student to choose a subject again.

### Explore resources

1. Open **Explore resources**.
2. Select one or more courses to search across.
3. Use the **Make active** control beside a selected-course chip to set the tutor course.
4. Filter by resource type, saved status, or search text.
5. Sort by quality, helpful votes, or newest.
6. Preview a resource, download it, save it, rate it, mark it helpful, or ask the tutor about it.

### Use the course workspace

The course page contains:

- **Overview:** exam focus, next milestones, tutor entry, and top resources.
- **Materials:** trusted resources for the course.
- **Roadmap:** ordered topics, difficulty, exam weight, and personal completion state.
- **Career relevance:** practical applications and relevant opportunities.

Opportunity registration remains available from the Career relevance section.

### Ask the course tutor

1. Make a course active.
2. Open **AI tutor** from the sidebar, top-level course CTA, or resource details.
3. Ask directly about the course. Subject selection is automatic.
4. Use suggestions such as a simple explanation, worked example, quiz, misconception check, or study plan.
5. Review the cited MASDAR resources shown with the answer.

Questions unrelated to the active course are redirected back to the current course context. If the required evidence is not available, the tutor says so instead of pretending to know.

### Contribute a resource

1. Open **Contribute**.
2. Select a PDF or DOCX no larger than 10 MB.
3. Choose the course/type and add a clear title, description, tags, and optional tutor context.
4. Confirm that you have permission to share the file.
5. Publish it for community review.

Contributing awards reputation. Uploading against an open community request can fulfill that request and award its bounty.

### Community

Students can create resource requests, reserve a reputation bounty, vote on important requests, fulfill open requests, and view the contributor leaderboard.

### Arabic interface

Use the **ع / EN** control in the top bar to switch between English and Arabic. The choice is saved in the browser. Arabic mode changes the interface direction to RTL and translates navigation, forms, common actions, course/resource labels, tutor controls, and administration terminology. University, course, user, and resource names remain as entered in the database.

## 7. Administrator guide

Sign in with an Admin account and choose **Administration** from the account menu.

The Admin dashboard provides platform KPIs, recent trends, moderation workload, resource quality, top courses/contributors, recent registrations, and quick actions. Management sections include:

- users, activation, and roles;
- universities and majors;
- courses and academic context;
- resource moderation, verification, representative status, and archiving;
- opportunities and publication state; and
- the administrative audit log.

All Admin operations are protected by the `Admin` role and server-side authorization. State-changing forms require antiforgery validation.

## 8. Production configuration checklist

- Set `ASPNETCORE_ENVIRONMENT=Production`.
- Supply `ConnectionStrings__DefaultConnection` through the hosting platform's secret configuration.
- Keep `SeedData__Enabled=false`.
- Keep `Database__ApplyMigrations=false` and apply migrations in the release pipeline.
- Use HTTPS and persist ASP.NET Core Data Protection keys outside an ephemeral instance.
- Replace local upload storage with durable private storage before horizontal scaling.
- Back up SQL Server and uploaded files.
- Configure centralized logs and health monitoring.
- Replace development credentials and provision administrators securely.

## 9. Troubleshooting

### LocalDB cannot create an automatic instance

Confirm SQL Server LocalDB is installed and that `MSSQLLocalDB` is available:

```powershell
SqlLocalDB info
SqlLocalDB start MSSQLLocalDB
```

Alternatively, point `DefaultConnection` to SQL Server Developer/Express.

### Database schema or seed data is missing

Development startup normally applies the committed migration automatically. For manual migration commands, install the matching EF Core 8 CLI once if it is not already available:

```powershell
dotnet tool install --global dotnet-ef --version 8.*
```

```powershell
dotnet ef database update --project .\src\Masdar.Web\Masdar.Web.csproj --startup-project .\src\Masdar.Web\Masdar.Web.csproj
dotnet run --project .\src\Masdar.Web\Masdar.Web.csproj
```

Verify that the environment is Development and `SeedData:Enabled` is true.

### The tutor has no active course

Open **Courses**, choose a course, and select **Make active**. The top-bar course pill should update immediately after the redirect.

### A file is rejected

Verify that it is a real PDF or DOCX, its extension matches its content, and it is no larger than 10 MB. Renaming another file type to `.pdf` or `.docx` will not pass validation.

### Arabic is not retained

Allow site storage in the browser. The language preference uses the `masdar-language` local-storage key.
