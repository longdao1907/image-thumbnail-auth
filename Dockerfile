# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj & restore (tối ưu cache)
COPY ["AuthAPI/AuthAPI.csproj", "AuthAPI/"]
RUN dotnet restore "AuthAPI/AuthAPI.csproj"

# Copy source còn lại và build
WORKDIR "/src/AuthAPI"
COPY AuthAPI/ .
RUN dotnet build "AuthAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "AuthAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Lấy binary cloud-sql-proxy từ image chính thức
FROM gcr.io/cloud-sql-connectors/cloud-sql-proxy:2.11.4 AS cloudsql


# Final image
FROM base AS final
WORKDIR /app
# Copy app published
COPY --from=publish /app/publish .

# Copy cloud-sql-proxy binary
COPY --from=cloudsql /cloud-sql-proxy /usr/local/bin/cloud-sql-proxy

# Copy entrypoint to set ASPNETCORE_URLS with $PORT
COPY entrypoint.sh ./entrypoint.sh

# set execution permission to run script
RUN chmod +x /app/entrypoint.sh
# Execute with non-root user 
USER app
ENTRYPOINT ["./entrypoint.sh"]