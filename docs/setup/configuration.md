# Configuration & Environment Reference

The TalentX backend uses a modern **12-Factor App** configuration system where non-sensitive structure lives in `appsettings.json` and all environment-specific values and secrets are supplied via **Environment Variables** (loaded automatically from a `.env` file in development, or injected by Docker/Cloud in production).

---

## 🏛️ Configuration Strategy: `appsettings.json` vs `.env` / Environment Variables

### 1. `appsettings.json` (Base Structure & Non-Sensitive Defaults)
- **Role**: Defines default settings and structural templates.
- **Git Status**: **Committed to source control**.
- **What goes here**: Log levels (`Default: Information`), default lifetimes (`AccessTokenMinutes: 15`), default host headers (`*`).
- **Rule**: **NEVER** put real passwords, secrets, or production connection strings in `appsettings.json`.

### 2. `.env` File (Local Development)
- **Role**: Local configuration and secrets for developers.
- **Git Status**: **Ignored by git** (`.gitignore`).
- **How it works**: At startup, [`Program.cs`](file:///Users/jubair/Developer/asthait/talentx/talentx2026q3.backend/src/TalentX.Api/Program.cs#L8-L29) automatically discovers and parses `talentx2026q3.backend/.env` and sets process-level environment variables.
- **Setup**: Copy `.env.example` to `.env` and fill in your local values.

### 3. Production Environment (Docker / Kubernetes / Azure / AWS)
- **Role**: Production secrets and hosting environment variables.
- **How it works**: The cloud platform / container orchestrator (e.g. Docker Compose, Kubernetes ConfigMaps/Secrets, Azure App Service Environment Variables) directly injects the exact same environment variable keys (`DB_HOST`, `DB_PASSWORD`, `JWT_SECRET`, `SMTP_PASS`, etc.).
- **Rule**: You do **not** need to modify `appsettings.json` for production — environment variables take highest precedence in ASP.NET Core.

---

## 📋 Full `.env` Variable Reference

Below is the complete list of environment variables supported by the backend (matching [`.env.example`](file:///Users/jubair/Developer/asthait/talentx/talentx2026q3.backend/.env.example)):

```dotenv
# ==============================================================================
# Server & Hosting Configuration
# ==============================================================================
PORT=5035
ASPNETCORE_ENVIRONMENT=Development
ENABLE_SWAGGER=true

# Dynamic CORS: Comma-separated list of allowed frontend URLs
CORS_ALLOWED_ORIGINS="http://localhost:3000,http://127.0.0.1:3000,https://talentx.ai,https://app.talentx.ai"

# ==============================================================================
# PostgreSQL Database Connection
# ==============================================================================
DB_HOST=localhost
DB_PORT=5432
DB_NAME=talentx
DB_USER_NAME=postgres
DB_PASSWORD=your_strong_password

# ==============================================================================
# JWT Authentication & Session Tokens
# ==============================================================================
JWT_SECRET=your_production_jwt_secret_min_32_characters_long_super_secure
JWT_ISSUER=TalentX
JWT_AUDIENCE=TalentX.Client

# ==============================================================================
# SMTP Email Dispatcher (Gmail / SendGrid / Mailtrap)
# ==============================================================================
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=your_email@asthait.com
SMTP_PASS=your_16_character_app_password
SENDER_EMAIL=your_email@asthait.com
SENDER_NAME=TalentX Support

# ==============================================================================
# Google OAuth 2.0 Integration
# ==============================================================================
GOOGLE_CLIENT_ID=your_google_client_id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your_google_client_secret

# ==============================================================================
# AI / Gemini Service Integration (Resume Extraction Pipeline)
# ==============================================================================
GEMINI_API_KEY=your_gemini_api_key_here
GEMINI_MODEL_NAME=gemini-3.1-flash-lite
GEMINI_BASE_URL=https://generativelanguage.googleapis.com/
```

---

## 🔄 How ASP.NET Core Loads Configuration (Hierarchy)

When an endpoint or service reads configuration in .NET:

```
1. Environment Variables (Highest priority: .env in Dev, Container ENV in Prod)
          ▲
          │ (overrides)
2. appsettings.Production.json / appsettings.Development.json
          ▲
          │ (overrides)
3. appsettings.json (Base defaults)
```

This guarantees that local development is frictionless via `.env`, while production deployments remain 100% secure and cloud-native without modifying code or config files.
