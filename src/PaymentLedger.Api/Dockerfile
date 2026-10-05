FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/PaymentLedger.Domain/PaymentLedger.Domain.csproj src/PaymentLedger.Domain/
COPY src/PaymentLedger.Application/PaymentLedger.Application.csproj src/PaymentLedger.Application/
COPY src/PaymentLedger.Infrastructure/PaymentLedger.Infrastructure.csproj src/PaymentLedger.Infrastructure/
COPY src/PaymentLedger.Api/PaymentLedger.Api.csproj src/PaymentLedger.Api/
RUN dotnet restore src/PaymentLedger.Api/PaymentLedger.Api.csproj

COPY src/ src/
RUN dotnet publish src/PaymentLedger.Api/PaymentLedger.Api.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "PaymentLedger.Api.dll"]
