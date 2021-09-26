FROM mcr.microsoft.com/dotnet/sdk:5.0 AS final
WORKDIR /src
COPY ["Migrator/Migrator.csproj", "Migrator/"]
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef
RUN dotnet restore "Migrator/Migrator.csproj"
COPY . .
WORKDIR "/src/Migrator"
