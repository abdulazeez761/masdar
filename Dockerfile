FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Masdar.sln", "./"]
COPY ["src/Masdar.Web/Masdar.Web.csproj", "src/Masdar.Web/"]

# Restore dependencies
RUN dotnet restore "src/Masdar.Web/Masdar.Web.csproj"

# Copy full source
COPY . .

# Build and publish
WORKDIR "/src/src/Masdar.Web"
RUN dotnet publish "Masdar.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Environment setup
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Masdar.Web.dll"]
