# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY SupportBoi.csproj .
COPY lib/ lib/
RUN dotnet restore -r linux-x64

COPY . .
RUN dotnet publish -c Release -r linux-x64 --self-contained -o /app --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0
WORKDIR /app

COPY --from=build /app .

ENTRYPOINT ["./supportboi"]
