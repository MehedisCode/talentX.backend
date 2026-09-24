# Configuration & Environment Variables

The TalentX Backend supports a unified configuration strategy supporting **`.env` local files**, **`appsettings.json`**, and **Cloud/CI environment variables**.

---

## 📄 `.env` Configuration File

Create a `.env` file in `talentx2026q3.backend/` (modeled after `.env.example`):

```bash
# Server Port & Hosting
PORT=5035
ASPNETCORE_ENVIRONMENT=Development
ENABLE_SWAGGER=true

# CORS Configuration (Comma-separated allowed origins)
CORS_ALLOWED_ORIGINS="http://localhost:3000,http://127.0.0.1:3000"

# PostgreSQL Database Configuration
DB_HOST=localhost
DB_PORT=5432
DB_NAME=talentx
DB_USER_NAME=postgres
DB_PASSWORD=your_password_here

# JWT Security Settings
JWT_SECRET=your_jwt_secret_min_32_characters_long_12345
JWT_ISSUER=TalentX
JWT_AUDIENCE=TalentX.Client
JWT_ACCESS_TOKEN_MINUTES=15
JWT_REFRESH_TOKEN_EXPIRY_DAYS=7

# SMTP Email Configuration (MailKit)
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=your_email@domain.com
SMTP_PASS=your_app_password
SENDER_EMAIL=noreply@talentx.ai
SENDER_NAME=TalentX Support

# Google OAuth 2.0 Client Keys
GOOGLE_CLIENT_ID=your_google_client_id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your_google_client_secret

# Gemini AI Configuration
GEMINI_API_KEY=your_gemini_api_key_here
GEMINI_MODEL_NAME=gemini-3.1-flash-lite
GEMINI_BASE_URL=https://generativelanguage.googleapis.com/

# AWS S3 Configuration
AWS_ACCESS_KEY_ID=your_aws_access_key_id
AWS_SECRET_ACCESS_KEY=your_aws_secret_access_key
AWS_REGION=ap-southeast-1

# File Storage Configuration
FILE_STORAGE_PROVIDER=S3
FILE_STORAGE_LOCAL_BASE_PATH=uploads/resumes
FILE_STORAGE_S3_BUCKET_NAME=talentx-s3-staging
FILE_STORAGE_S3_REGION=ap-southeast-1
FILE_STORAGE_S3_FOLDER_PREFIX=resumes

# Company Reviews Configuration (optional, default 1)
# Days after an online assessment's deadline before the talent can review the company.
# Also settable as CompanyReview:AssessmentReviewDelayDays in appsettings.
COMPANY_REVIEW_ASSESSMENT_DELAY_DAYS=1
```

---

## 🔍 Precedence & Loading Order

1. **System Environment Variables** (Deployment / Docker / Kubernetes)
2. **`.env` File Variables** (Loaded recursively at startup via `LoadDotEnv` in `Program.cs`)
3. **`appsettings.{Environment}.json`**
4. **`appsettings.json`**
