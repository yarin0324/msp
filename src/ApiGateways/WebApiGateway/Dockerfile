FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ApiGateways/WebApiGateway/WebApiGateway.csproj", "src/ApiGateways/WebApiGateway/"]
RUN dotnet restore "src/ApiGateways/WebApiGateway/WebApiGateway.csproj"
COPY . .
WORKDIR "/src/src/ApiGateways/WebApiGateway"
RUN dotnet build "WebApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApiGateway.dll"]
