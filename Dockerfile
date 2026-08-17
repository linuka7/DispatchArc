FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/DispatchArc.Domain/DispatchArc.Domain.csproj src/DispatchArc.Domain/
COPY src/DispatchArc.Application/DispatchArc.Application.csproj src/DispatchArc.Application/
COPY src/DispatchArc.Infrastructure/DispatchArc.Infrastructure.csproj src/DispatchArc.Infrastructure/
COPY src/DispatchArc.Api/DispatchArc.Api.csproj src/DispatchArc.Api/

RUN dotnet restore src/DispatchArc.Api/DispatchArc.Api.csproj

COPY src/ src/

RUN dotnet publish src/DispatchArc.Api/DispatchArc.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

COPY --from=build /app/publish .

USER app

CMD ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet DispatchArc.Api.dll"]