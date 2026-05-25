FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files for restore
COPY LaptopRequisition.sln ./
COPY LaptopRequisition.Domain/LaptopRequisition.Domain.csproj ./LaptopRequisition.Domain/
COPY LaptopRequisition.Application/LaptopRequisition.Application.csproj ./LaptopRequisition.Application/
COPY LaptopRequisition.Infrastructure/LaptopRequisition.Infrastructure.csproj ./LaptopRequisition.Infrastructure/
COPY LaptopRequisition.WebAPI/LaptopRequisition.WebAPI.csproj ./LaptopRequisition.WebAPI/

RUN dotnet restore

# Copy everything else and publish
COPY . .
RUN dotnet publish LaptopRequisition.WebAPI/LaptopRequisition.WebAPI.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}

ENTRYPOINT ["dotnet", "LaptopRequisition.WebAPI.dll"]
