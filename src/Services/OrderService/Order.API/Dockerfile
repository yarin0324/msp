FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5125
ENV ASPNETCORE_URLS=http://+:5125

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/OrderService/Order.API/OrderService.WebApi.csproj", "src/Services/OrderService/Order.API/"]
COPY ["src/Services/OrderService/Order.Core/OrderService.Domain.csproj", "src/Services/OrderService/Order.Core/"]
COPY ["src/Services/OrderService/Order.UseCase/OrderService.Application.csproj", "src/Services/OrderService/Order.UseCase/"]
COPY ["src/Services/OrderService/Order.Infrastructure/OrderService.Infrastructure.csproj", "src/Services/OrderService/Order.Infrastructure/"]
COPY ["src/Services/Common/Common.csproj", "src/Services/Common/"]

RUN dotnet restore "src/Services/OrderService/Order.API/OrderService.WebApi.csproj"
COPY . .
WORKDIR "/src/src/Services/OrderService/Order.API"
RUN dotnet build "OrderService.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OrderService.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderService.WebApi.dll"]
