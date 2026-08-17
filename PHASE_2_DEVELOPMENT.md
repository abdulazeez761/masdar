# MASDAR (مصدر) — Phase 2 Development Submission

**Event**: JSYP 2026 Hackathon  
**Track**: Education & Digital Transformation ("How to Make Jordan Better")  
**Project**: MASDAR — University Knowledge Hub  
**Status**: Phase 2 (Development) Completed & Live Deployment Ready  

---

## 1. Executive Summary

MASDAR (مصدر) is a community-powered university knowledge platform designed to solve the problem of scattered, unverified, and ephemeral academic study materials across Jordanian universities. 

By combining **course-structured knowledge repositories**, **community quality signals & bounties**, **guided learning roadmaps**, **career relevance connections**, and a **course-aware AI tutor powered by Gemini RAG**, MASDAR turns static study files into an interactive, durable learning ecosystem.

---

## 2. Phase 2 Requirement Compliance Matrix

The table below maps every requirement from the **JSYP 2026 Hackathon Phase 2 Specification** (`JSYP_2026_Hackathon.pdf` & `jsyp.pdf`) directly to the implemented software solution:

| Feature / Requirement | JSYP PDF Specification | Implementation in MASDAR 2.0 |
| :--- | :--- | :--- |
| **University Knowledge Hub** | Select University → Major → Course to discover materials by type (exams, notes, summaries, etc.). | Implemented in `CoursesController` & `ResourcesController`. Filter by university (JUST, UJ, Yarmouk, PSUT, HU, GJU, BAU), major, and active courses. |
| **Community Contributions** | Upload PDF/DOCX materials with metadata (course, instructor, semester, tags, type). | Implemented in `ContributionsController` & `LocalFileStorageService`. Includes file signature validation, anti-virus/MIME verification, and SHA-256 deduplication. |
| **Quality Control & Rating** | Helpful votes, 1–5 star ratings, and "Exam Representative" badges. | Implemented in `ResourceService`. Community ratings and votes drive quality ranking (`Quality` sort algorithm). |
| **Course-Aware AI Tutor** | RAG assistant answering course-specific questions using uploaded materials with citations. | Implemented in `CourseTutorService`, `GeminiGroundedRagService`, and `GeminiApiClient`. Uses vector embeddings, cosine similarity, and inline source citations `[1]`, `[2]`. |
| **Real-World & Career Relevance** | Answer "Why am I learning this?" by connecting courses to real-world uses, skills, and careers. | Implemented in `CoursesController` (Career tab). Displays technical foundations, practical problem solving, portfolio projects, and industry career paths. |
| **Opportunities & Newsfeed** | Platform for hackathons, IEEE competitions, ICPC qualifiers, and workshops. | Implemented in `OpportunityService` & `CoursesController`. Allows students to RSVP and track upcoming events. |
| **Smart Learning Roadmap** | Guided course mindmap with topic difficulty (Easy, Medium, Hard), exam weights, and completion tracking. | Implemented in `CourseService` (Roadmap tab). Calculates student course-readiness percentage in real time. |
| **Gamification & Leaderboard** | Reputation points, request bounties, and contributor rankings. | Implemented in `CommunityService` & `CommunityController`. Users earn +15 pts for publishing, spend/earn bounties on requests, and compete on the leaderboard. |
| **Administration & Audit** | Dashboard for monitoring users, verification, system activity, and moderation. | Implemented in `Areas/Admin`. Protected admin dashboard with user management, resource verification, opportunity publishing, and durable audit logs. |
| **Production & Live Hosting** | Cross-platform execution, containerization, and live cloud deployment. | Implemented via `Dockerfile`, `render.yaml`, multi-database support (PostgreSQL, SQLite, SQL Server), and automatic schema initialization. |

---

## 3. Technology Architecture & Stack

- **Framework**: ASP.NET Core 8 MVC (C# .NET 8)
- **Frontend**: Responsive Razor Views, Vanilla CSS Design System, Light/Dark Mode, Arabic/English i18n
- **Database Layer**: Entity Framework Core 8 with multi-provider support:
  - **PostgreSQL** (Npgsql) for live cloud hosting (Render)
  - **SQL Server** for enterprise environments
  - **SQLite** for zero-config local testing
- **Authentication & Security**: ASP.NET Core Identity, cookie auth, role/ownership authorization, Anti-Forgery tokens, private validated file storage.
- **AI & RAG Engine**: Gemini API (`gemini-3.6-flash` + `gemini-embedding-001`), chunking via PdfPig & OpenXML, cosine similarity retrieval.
- **Hosting & DevOps**: Docker multi-stage container, Render Blueprint (`render.yaml`).

---

## 4. How to Run & Verify Phase 2 Locally

### Build the Application
```bash
dotnet build src/Masdar.Web/Masdar.Web.csproj
```

### Run Locally
```bash
dotnet run --project src/Masdar.Web/Masdar.Web.csproj
```
The application will launch on `https://localhost:7084` or `http://localhost:5292`. On initial boot, the database is created and seeded with universities, courses, roadmap milestones, sample resources, and initial admin/student accounts.

### Default Credentials
- **Admin**: `admin@masdar.local` / `Admin123!`
- **Student**: `user@masdar.local` / `User123!`

---

## 5. Live Hosting & Deployment (Render)

MASDAR 2.0 is fully configured for hosting on **Render**:
1. Push code to GitHub/GitLab.
2. Connect repo to **Render Blueprint** (`render.yaml`).
3. Render automatically provisions the Web Service (Docker container) and PostgreSQL Database.
4. Set environment variable `GEMINI_API_KEY` for live AI Tutor answers.
