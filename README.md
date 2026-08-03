# KayCare LIS — Laboratory & Radiology Information System

KayCare LIS is an advanced diagnostic software platform integrating a Laboratory Information System (LIS) and Radiology Information System (RIS) into a unified clinical command center. Designed for hospital diagnostic units, reference laboratories, and imaging centers, KayCare LIS streamlines sample processing, automates analyzer data ingestion, handles Temporal PACS imaging storage, and features embedded AI assistance.

---

## Key Features & Capabilities

- **Diagnostic Command Center**: Centralized dashboard offering real-time visibility into pending lab requisitions, processing queues, radiology scans, and turnaround time (TAT) metrics.
- **Laboratory Information System (LIS)**: Full lifecycle sample tracking (Ordered -> Sample Received -> Resulted -> Signed Off),panic-value critical result alerts with automated chimes, and mandatory call-logging audit trails.
- **Radiology & PACS Archival**: Stateless image storage uploading scans directly to cloud object storage (AWS S3) via temporal pre-signed URLs, coupled with an integrated DICOM scan workstation viewer.
- **Google Gemini AI Assistant**: Integrated AI capabilities including Vision AI radiology report drafting, automated ICD-10 billing code suggestions, patient-friendly diagnostic summaries, and real-time HL7 payload auto-repair.
- **Security & HIPAA Compliance**: Authenticated PHI field-level encryption at rest (AES-256 GCM) and automated 15-minute workstation idle session locking.

---

## Technical Stack

### Backend & API Architecture
- **Framework**: ASP.NET Core 8.0 Web API
- **Language**: C# 12
- **Data Access**: Entity Framework Core 8.0
- **Database**: PostgreSQL 15+
- **Cloud Storage**: Amazon S3 (for PACS images & diagnostic documents)
- **AI Integration**: Google Gemini API SDK

### Frontend Application
- **Framework**: React 19
- **Build Tool**: Vite
- **Language**: TypeScript
- **Styling**: Tailwind CSS
- **State & Routing**: React Router v6

---

## Repository Structure

```
kaycare-lis/
├── app_overview.md                    # Detailed functional product overview & screenshot guide
├── roadmap.md                         # Product development roadmap and feature status
├── KayCare_Suite_Product_Overview.pdf # Executive overview document
├── frontend/                          # React + TypeScript SPA application
├── infrastructure/                    # Container scripts and infrastructure configs
├── src/
│   ├── KayCareLIS.API/                # REST Web API endpoints, middleware, controllers
│   ├── KayCareLIS.Core/               # Domain logic, interfaces, DTOs, entity definitions
│   └── KayCareLIS.Infrastructure/     # EF Core DbContext, migrations, S3 & AI service adapters
├── tests/                             # Test projects and test data
├── tools/                             # Helper tools and setup scripts
└── KayCareLIS.sln                     # .NET Solution File
```

---

## Prerequisites

Before starting development, verify your environment meets the following requirements:

- **.NET 8.0 SDK** or higher
- **Node.js** (v18.0.0 or higher) and **npm** (v9.0.0 or higher)
- **PostgreSQL Database** (v15.0 or higher)
- **AWS S3 Credentials** (optional for local image testing)

---

## Getting Started

### 1. Database & Environment Configuration
Configure your database connection in `src/KayCareLIS.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=kaycare_lis;Username=postgres;Password=yourpassword"
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY"
  }
}
```

### 2. Backend API Execution

1. Restore dependencies:
   ```bash
   dotnet restore
   ```

2. Apply EF Core database migrations:
   ```bash
   dotnet ef database update --project src/KayCareLIS.Infrastructure --startup-project src/KayCareLIS.API
   ```

3. Launch the API server:
   ```bash
   dotnet run --project src/KayCareLIS.API
   ```

### 3. Frontend Application Execution

1. Navigate to the `frontend` directory:
   ```bash
   cd frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Start the development server:
   ```bash
   npm run dev
   ```
   Open `http://localhost:5173` in your browser.

---

## Testing

Run backend tests using the .NET CLI:

```bash
dotnet test tests/
```

Run frontend static checks and build verification:

```bash
cd frontend
npm run build
```

---

## Related Documentation

- [app_overview.md](file:///c:/Users/asnah/Desktop/KayCare%20Suite/kaycare-lis/app_overview.md) — Product Feature Overview and Interface Visuals
- [roadmap.md](file:///c:/Users/asnah/Desktop/KayCare%20Suite/kaycare-lis/roadmap.md) — Platform Features & Milestones
- [frontend/README.md](file:///c:/Users/asnah/Desktop/KayCare%20Suite/kaycare-lis/frontend/README.md) — Frontend Setup Guide
