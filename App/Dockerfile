FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["App/Reservator.csproj", "App/"]
RUN dotnet restore "App/Reservator.csproj"
COPY . .
WORKDIR "/src/App"
RUN dotnet build "Reservator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Reservator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Reservator.dll"]
