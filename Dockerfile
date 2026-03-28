FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["PhotoCopyHub.sln", "."]
COPY ["Directory.Build.props", "."]
COPY ["global.json", "."]
COPY ["NuGet.Config", "."]
COPY ["src/PhotoCopyHub.Domain/PhotoCopyHub.Domain.csproj", "src/PhotoCopyHub.Domain/"]
COPY ["src/PhotoCopyHub.Application/PhotoCopyHub.Application.csproj", "src/PhotoCopyHub.Application/"]
COPY ["src/PhotoCopyHub.Infrastructure/PhotoCopyHub.Infrastructure.csproj", "src/PhotoCopyHub.Infrastructure/"]
COPY ["src/PhotoCopyHub.Web/PhotoCopyHub.Web.csproj", "src/PhotoCopyHub.Web/"]

RUN dotnet restore "src/PhotoCopyHub.Web/PhotoCopyHub.Web.csproj"

COPY . .
RUN dotnet publish "src/PhotoCopyHub.Web/PhotoCopyHub.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PhotoCopyHub.Web.dll"]
