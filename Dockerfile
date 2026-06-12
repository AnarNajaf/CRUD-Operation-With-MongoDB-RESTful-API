FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY iTarlaMapBackend.csproj ./
RUN dotnet restore iTarlaMapBackend.csproj

COPY . .
RUN dotnet publish iTarlaMapBackend.csproj -c Release -o /app/dist --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/dist .

EXPOSE 8080

ENTRYPOINT ["dotnet", "iTarlaMapBackend.dll"]
