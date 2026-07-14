FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /app

# Copy csproj and restore
COPY *.sln ./
COPY src/KayCareLIS.Core/*.csproj ./src/KayCareLIS.Core/
COPY src/KayCareLIS.Infrastructure/*.csproj ./src/KayCareLIS.Infrastructure/
COPY src/KayCareLIS.API/*.csproj ./src/KayCareLIS.API/
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish src/KayCareLIS.API/KayCareLIS.API.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /app/out .
ENV ASPNETCORE_URLS=http://+:5211
EXPOSE 5211
EXPOSE 2575
ENTRYPOINT ["dotnet", "KayCareLIS.API.dll"]
