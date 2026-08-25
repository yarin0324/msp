FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5271
ENV ASPNETCORE_URLS=http://+:5271

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/InventoryService/InventoryService.WebApi/InventoryService.WebApi.csproj", "src/Services/InventoryService/InventoryService.WebApi/"]
COPY ["src/Services/InventoryService/InventoryService.Domain/InventoryService.Domain.csproj", "src/Services/InventoryService/InventoryService.Domain/"]
COPY ["src/Services/InventoryService/InventoryService.Application/InventoryService.Application.csproj", "src/Services/InventoryService/InventoryService.Application/"]
COPY ["src/Services/InventoryService/InventoryService.Infrastructure/InventoryService.Infrastructure.csproj", "src/Services/InventoryService/InventoryService.Infrastructure/"]
COPY ["src/Services/Common/Common.csproj", "src/Services/Common/"]

RUN dotnet restore "src/Services/InventoryService/InventoryService.WebApi/InventoryService.WebApi.csproj"
COPY . .
WORKDIR "/src/src/Services/InventoryService/InventoryService.WebApi"
RUN dotnet build "InventoryService.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InventoryService.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InventoryService.WebApi.dll"]
