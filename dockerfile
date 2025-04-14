# Stage 1: Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /src

# restore dependencies
COPY ["src/HTTPServer/HTTPServer.csproj", "HTTPServer/"]
RUN dotnet restore "HTTPServer/HTTPServer.csproj"

# build the project
COPY ["src/HTTPServer/", "HTTPServer/"]
RUN dotnet build "HTTPServer/HTTPServer.csproj" -c Release -o /app/build

# Stage 2: Publish Stage
FROM build-env AS publish
WORKDIR /src/HTTPServer
RUN dotnet publish "HTTPServer.csproj" -c Release -o /app/publish

# Stage 3: Run Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV ASPNETCORE_HTTP_PORT=4321
EXPOSE 4321
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HTTPServer.dll"]