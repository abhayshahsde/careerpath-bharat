# ?? Complete Step-by-Step Azure Deployment Guide for CareerPath Bharat

This guide walks you through hosting the entire solution on **Microsoft Azure** (Azure SQL Free Tier + Azure App Service for .NET 8 Backend + Azure Static Web Apps for Next.js Frontend) using the Azure Portal in your browser.

---

## ??? Azure Architecture

1. **Database**: Azure SQL Database (Serverless — **Free 32 GB Lifetime Offer**)
2. **Backend API**: Azure App Service (.NET 8 Linux Web App — **Free F1 Plan**)
3. **Frontend**: Azure Static Web Apps (Next.js 14 — **Free Plan**)

**Total Cost: ?0 / month**

---

## Step 1: Create Free Azure SQL Database (32 GB Free)

1. Log in to the [Azure Portal](https://portal.azure.com).
2. In the top search bar, type **SQL databases** and click **SQL databases**.
3. Click **+ Create**.
4. Fill in the **Basics** tab:
   - **Subscription**: Select your active Azure subscription.
   - **Resource Group**: Click *Create new* ? Name it `CareerPath-RG`.
   - **Database name**: `CareerPathProd`
   - **Server**: Click *Create new*:
     - **Server name**: `careerpath-server-unique` (e.g. `careerpath-sql-prod`)
     - **Location**: `Central India` (or `South India` / `Southeast Asia`)
     - **Authentication method**: *Use SQL authentication*
     - **Server admin login**: `careeradmin`
     - **Password**: Set a strong password (e.g. `Bharat@Career2026!`)
     - Click **OK**.
   - **Want to use SQL elastic pool?**: `No`
   - **Workload environment**: `Development`
   - **Compute + storage**: Click *Configure database*:
     - Select **General Purpose (Serverless)**.
     - Look for the banner: **"Apply 32 GB free offer"** and check it.
     - Enable **Auto-pause delay** (e.g., 1 hour) so compute is never wasted.
     - Click **Apply**.
5. Click **Next: Networking**:
   - **Connectivity method**: `Public endpoint`
   - **Allow Azure services and resources to access this server**: `Yes` (Important!)
   - **Add current client IP address**: `Yes`
6. Click **Review + create** ? Click **Create**.
7. Once deployment finishes, click **Go to resource** ? In the left menu, click **Connection strings** ? Copy the **ADO.NET (SQL authentication)** connection string. It will look like:
   ```text
   Server=tcp:careerpath-server-unique.database.windows.net,1433;Initial Catalog=CareerPathProd;Persist Security Info=False;User ID=careeradmin;Password={your_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   ```
   *(Replace `{your_password}` with your actual admin password).*

---

## Step 2: Deploy Backend Web API (.NET 8 App Service)

1. In the Azure Portal search bar, type **App Services** and click **App Services**.
2. Click **+ Create** ? **Web App**.
3. Fill in the **Basics** tab:
   - **Resource Group**: `CareerPath-RG`
   - **Name**: `careerpath-api-bharat` (This creates `https://careerpath-api-bharat.azurewebsites.net`)
   - **Publish**: `Code`
   - **Runtime stack**: `.NET 8 (LTS)`
   - **Operating System**: `Linux`
   - **Region**: `Central India`
   - **Pricing Plan**: Click *Explore pricing plans* ? Select **Free F1 (1 GB RAM, 60 CPU minutes/day - Free)** or **Basic B1**.
4. Click **Review + create** ? Click **Create**.
5. Once created, click **Go to resource**:
   - In the left sidebar under **Settings**, click **Environment variables** (or **Configuration**).
   - Under **App settings**, click **+ Add**:
     - `ASPNETCORE_ENVIRONMENT` = `Production`
     - `Jwt__Key` = `your-super-secret-jwt-key-min-32-chars-long`
     - `Jwt__Issuer` = `CareerPathBharat`
     - `Jwt__Audience` = `CareerPathBharatClients`
     - `Cors__AllowedOrigins__0` = `https://careerpath-bharat.azurestaticapps.net`
     - `Gemini__ApiKey` = `[Optional: Your Google Gemini API Key]`
   - Under **Connection strings**, click **+ Add**:
     - **Name**: `DefaultConnection`
     - **Value**: Paste the ADO.NET connection string from Step 1.
     - **Type**: `SQLAzure`
   - Click **Apply** at the bottom to save.

6. **Publishing the code from VS Code or GitHub**:
   - In your App Service left menu under **Deployment**, click **Deployment Center**.
   - Select **GitHub** as the source, link your repository `CareerPath-Bharat`, branch `main`.
   - Azure will automatically generate a GitHub Actions workflow and build/publish your .NET 8 API!
   - On first startup, the backend automatically runs all 26 SQL migrations on your Azure SQL Database!

---

## Step 3: Deploy Frontend (Azure Static Web Apps)

1. In the Azure Portal search bar, type **Static Web Apps** and click **Static Web Apps**.
2. Click **+ Create**.
3. Fill in the **Basics** tab:
   - **Resource Group**: `CareerPath-RG`
   - **Name**: `careerpath-bharat-frontend`
   - **Plan type**: **Free** ($0/month)
   - **Region**: `Central India` or `East Asia`
   - **Source**: Select **GitHub** ? Click **Sign in with GitHub**.
   - **Organization**: Your GitHub account.
   - **Repository**: `CareerPath-Bharat`
   - **Branch**: `main`
   - **Build Presets**: Select `Next.js`
   - **App location**: `frontend`
   - **Api location**: Leave empty
   - **Output location**: `.next`
4. Click **Review + create** ? Click **Create**.
5. Once created, click **Go to resource**:
   - Under **Settings**, click **Configuration**.
   - Click **+ Add** and set:
     - **Name**: `NEXT_PUBLIC_API_URL`
     - **Value**: `https://careerpath-api-bharat.azurewebsites.net` (Your App Service URL from Step 2)
     - **Name**: `NEXT_PUBLIC_RAZORPAY_KEY_ID`
     - **Value**: `rzp_test_careerpathbharat`
   - Click **Save**.

---

## Step 4: Verification Checklist

Once Azure finishes the automated deployment:
1. Open your Static Web App URL: `https://careerpath-bharat-XXXXX.azurestaticapps.net`
2. Test registering a user (`/auth/register`) ? Data is saved to Azure SQL Database.
3. Test Career Catalog, Roadmap Builder, Comparison Matrix, and AI Chatbot.
