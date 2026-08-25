FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5198
ENV ASPNETCORE_URLS=http://+:5198

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/Payment.API/Payment.API.csproj", "src/Services/Payment.API/"]
RUN dotnet restore "src/Services/Payment.API/Payment.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Payment.API"
RUN dotnet build "Payment.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Payment.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Payment.API.dll"]
