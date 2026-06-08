# PhotoCopyHub

Huong dan nay giup may dev khac clone repo va chay ung dung voi Supabase PostgreSQL hien tai. Khong commit password hay connection string that vao repo.

## Yeu cau

- .NET 8 SDK
- Git
- Visual Studio 2022 hoac terminal PowerShell
- Supabase PostgreSQL connection string do nguoi quan ly du an cung cap

## Cau hinh Supabase cho may dev moi

Ung dung doc connection string tu bien moi truong `PHOTOCOPYHUB_POSTGRES_CONNECTION`. Gia tri mau:

```powershell
Host=aws-...pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true;Application Name=DTBWebPhotocopyHub
```

Chay script setup:

```powershell
..\setup-dev-env.ps1 -ConnectionString "<SUPABASE_CONNECTION_STRING>"
```

Neu khong truyen `-ConnectionString`, script se hoi nhap connection string trong PowerShell. Script se:

- Kiem tra connection string khong rong.
- Chan placeholder nhu `[YOUR-PASSWORD]` hoac `<password>`.
- Yeu cau `Database=postgres`.
- Yeu cau host la Supabase hoac Supabase pooler.
- Luu bien moi truong user-level `PHOTOCOPYHUB_POSTGRES_CONNECTION`.
- Chi in connection string da che password.

Sau khi setup, mo terminal moi hoac restart Visual Studio de process moi doc duoc bien moi truong.

## Restore, build, run

```powershell
dotnet restore .\WebPhotocopyHub.sln
dotnet build .\WebPhotocopyHub.sln --no-restore
dotnet run --project .\WebPhotocopyHub_Web\WebPhotocopyHub_Web.csproj --launch-profile Start
```

Ung dung mac dinh chay tai:

- `https://localhost:7250`
- `http://localhost:5250`

Health check:

- `https://localhost:7250/healthz/live`
- `https://localhost:7250/healthz/ready`

## Kiem tra nhanh env var

```powershell
[Environment]::GetEnvironmentVariable('PHOTOCOPYHUB_POSTGRES_CONNECTION', 'User')
```

Neu app bao loi connection string, kiem tra lai password Supabase, host pooler, SSL mode, va dam bao terminal/Visual Studio da duoc restart sau khi set env var.
