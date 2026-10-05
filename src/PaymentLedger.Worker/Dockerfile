FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/PaymentLedger.Domain/PaymentLedger.Domain.csproj src/PaymentLedger.Domain/
COPY src/PaymentLedger.Application/PaymentLedger.Application.csproj src/PaymentLedger.Application/
COPY src/PaymentLedger.Infrastructure/PaymentLedger.Infrastructure.csproj src/PaymentLedger.Infrastructure/
COPY src/PaymentLedger.Worker/PaymentLedger.Worker.csproj src/PaymentLedger.Worker/
RUN dotnet restore src/PaymentLedger.Worker/PaymentLedger.Worker.csproj

COPY src/ src/
RUN dotnet publish src/PaymentLedger.Worker/PaymentLedger.Worker.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "PaymentLedger.Worker.dll"]
