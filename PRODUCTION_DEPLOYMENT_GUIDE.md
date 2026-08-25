# ?? CareerPath Bharat — Complete Project Documentation & Production Hosting Guide

---

## 1. Project Overview & Architecture

**CareerPath Bharat** is an AI-powered, bilingual (Hindi & English) career guidance and roadmap builder platform tailored specifically for Indian students (Classes 9–12, Diploma, and Undergraduates).

```
   +--------------------------------------------------------+
   ¦             Next.js 14 Web Frontend                   ¦
   ¦  • Tailwind CSS (Royal Sapphire palette)               ¦
   ¦  • Bilingual localization (en / hi)                    ¦
   ¦  • Client-side JWT auth + Razorpay Checkout            ¦
   +--------------------------------------------------------+
                               ¦ HTTPS REST API
                               ?
   +--------------------------------------------------------+
   ¦             ASP.NET Core 8 Web API                     ¦
   ¦  • Clean Architecture + CQRS (MediatR)                 ¦
   ¦  • JWT Bearer Authentication & Admin RBAC              ¦
   ¦  • Dapper Micro-ORM + SQL Server / Azure SQL / Neon    ¦
   ¦  • Google Gemini 1.5 Flash + Local Fallback Engine     ¦
   +--------------------------------------------------------+
                               ¦ Raw SQL Queries
                               ?
   +--------------------------------------------------------+
   ¦             Cloud SQL Database Engine                  ¦
   ¦  • 26 Idempotent Database Migrations                   ¦
   ¦  • Schemas: identity, catalog, student, billing, etc.  ¦
   +--------------------------------------------------------+
```

---

## 2. Exact URLs To Replace For Production

When moving from `localhost` to live production, you only need to update **3 URLs**:

| Configuration Location | Development URL (Current) | Production URL (Live Target) | Description |
|---|---|---|---|
| **Frontend `.env.production` / Vercel Env** (`NEXT_PUBLIC_API_URL`) | `http://localhost:5073` | `https://your-api-name.onrender.com` | Tells frontend where to send API requests |
| **Backend CORS / Render Env** (`Cors__AllowedOrigins__0`) | `http://localhost:3000` | `https://your-app-name.vercel.app` | Allows frontend origin in backend CORS policy |
| **Frontend Razorpay Key** (`NEXT_PUBLIC_RAZORPAY_KEY_ID`) | `rzp_test_careerpathbharat` | `rzp_live_your_actual_key` (or test key) | Razorpay public payment key identifier |

---

## 3. Step-by-Step Production Deployment Guide (100% Free)

### Step 1: Initialize Git and Push to GitHub

1. Open your terminal in the root folder: `e:\Abhay\Projects\VibeCoding\CareerPath\careerpath-bharat`
2. Run the following commands:
   ```bash
   git init
   git add .
   git commit -m "Initial commit: Production ready CareerPath Bharat platform"
   ```
3. Create a **New Repository** on [GitHub.com](https://github.com/new) named `CareerPath-Bharat` (set it to Private or Public).
4. Push your codebase:
   ```bash
   git remote add origin https://github.com/YOUR_GITHUB_USERNAME/CareerPath-Bharat.git
   git branch -M main
   git push -u origin main
   ```

---

### Step 2: Set Up a Free Cloud Database

You can use **Azure SQL Database (Free Tier)**, **Aiven Cloud SQL**, or **Neon / Supabase**:

#### Option A: Azure SQL Database Free Tier (Recommended for Microsoft SQL Server)
1. Sign up on [Azure Portal](https://portal.azure.com).
2. Search for **Azure SQL** ? Click **Create**.
3. Choose **Free 32 GB offer**.
4. Set Server Admin Login (e.g. `careeradmin`) and Password.
5. In **Networking**, enable `"Allow Azure services and resources to access this server"`.
6. Copy the ADO.NET Connection String:
   ```text
   Server=tcp:your-server.database.windows.net,1433;Initial Catalog=CareerPathProd;Persist Security Info=False;User ID=careeradmin;Password=YourStrongPassword!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   ```

---

### Step 3: Deploy Backend API on Render (100% Free)

1. Sign up on [Render.com](https://render.com) using your GitHub account.
2. Click **New +** ? **Web Service**.
3. Connect your GitHub repository: `CareerPath-Bharat`.
4. Fill in the deployment form:
   - **Name**: `careerpath-bharat-api`
   - **Region**: Singapore (Closest to India)
   - **Branch**: `main`
   - **Root Directory**: `backend`
   - **Runtime**: `Docker`
   - **Instance Type**: **Free ($0/month)**
5. Expand **Environment Variables** and add:
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `ConnectionStrings__DefaultConnection` = `[Paste Your Cloud DB Connection String from Step 2]`
   - `Jwt__Key` = `a-very-secure-random-key-with-at-least-32-characters-12345`
   - `Jwt__Issuer` = `CareerPathBharat`
   - `Jwt__Audience` = `CareerPathBharatClients`
   - `Cors__AllowedOrigins__0` = `https://careerpath-bharat.vercel.app`
   - `Gemini__ApiKey` = `[Your Google Gemini API Key - Optional]`
6. Click **Create Web Service**.
7. Once deployed, Render will give you your live API URL:
   ?? **`https://careerpath-bharat-api.onrender.com`**

---

### Step 4: Deploy Frontend Web App on Vercel (100% Free)

1. Sign up on [Vercel.com](https://vercel.com) using your GitHub account.
2. Click **Add New...** ? **Project**.
3. Select your `CareerPath-Bharat` repository.
4. Configure the build settings:
   - **Framework Preset**: `Next.js`
   - **Root Directory**: Click `Edit` and select `frontend`
5. Expand **Environment Variables** and add:
   - `NEXT_PUBLIC_API_URL` = `https://careerpath-bharat-api.onrender.com` *(From Step 3)*
   - `NEXT_PUBLIC_RAZORPAY_KEY_ID` = `rzp_test_careerpathbharat`
6. Click **Deploy**.
7. In ~60 seconds, your site will be live at:
   ?? **`https://careerpath-bharat.vercel.app`**

---

## 4. Verification & Smoke Test Checklist

Once both services are live:
- [ ] Visit `https://careerpath-bharat.vercel.app` — verify Home page loads in English and Hindi.
- [ ] Sign up as a new student (`/auth/register`) and complete Stream onboarding (`/onboarding`).
- [ ] Open Career Catalog (`/careers`), compare 3 careers (`/careers/compare`), and view salary charts.
- [ ] Create a personalized Roadmap (`/me/roadmaps`) and click **Print / Export PDF**.
- [ ] Chat with AI Counselor (`/ai/counselor`) and verify response generation.
- [ ] Visit Subscription page (`/subscribe`), apply coupon `BHARAT50`, and test Razorpay modal.
- [ ] Log in with Admin credentials and view Live Metrics in Super Admin Hub (`/admin`).
