# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Karry.Api/Karry.Api.csproj", "Karry.Api/"]
COPY ["Karry.Application/Karry.Application.csproj", "Karry.Application/"]
COPY ["Karry.Domain/Karry.Domain.csproj", "Karry.Domain/"]
COPY ["Karry.Infrastructure/Karry.Infrastructure.csproj", "Karry.Infrastructure/"]
COPY ["Karry.MathEngine.Client/Karry.MathEngine.Client.csproj", "Karry.MathEngine.Client/"]
COPY ["Directory.Build.props", "./"]
RUN dotnet restore "Karry.Api/Karry.Api.csproj"

COPY . .
RUN dotnet publish "Karry.Api/Karry.Api.csproj" -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5000
COPY --from=build /out .
ENTRYPOINT ["dotnet", "Karry.Api.dll"]
