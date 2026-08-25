FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5079
ENV ASPNETCORE_URLS=http://+:5079

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/BasketService/Basket.API/Basket.API.csproj", "src/Services/BasketService/Basket.API/"]
COPY ["src/Services/BasketService/Basket.Core/Basket.Core.csproj", "src/Services/BasketService/Basket.Core/"]
COPY ["src/Services/BasketService/Basket.Infrastructure/Basket.Infrastructure.csproj", "src/Services/BasketService/Basket.Infrastructure/"]
RUN dotnet restore "src/Services/BasketService/Basket.API/Basket.API.csproj"
COPY . .
WORKDIR "/src/src/Services/BasketService/Basket.API"
RUN dotnet build "Basket.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Basket.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Basket.API.dll"]
