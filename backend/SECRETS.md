# CareerPath Bharat — Development Environment

## Secrets (Never Commit These!)

All secrets go in environment variables or user secrets, not in files.

To set user secrets for the API project:
```bash
cd apps/api/CareerPath.Api
dotnet user-secrets set "Jwt:Key" "your-32+-character-secret-key-here"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;..."
```

## Environment Variables for Production

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `Jwt__Key` | JWT signing key (min 32 chars) |
| `Jwt__Issuer` | JWT issuer |
| `Jwt__Audience` | JWT audience |
