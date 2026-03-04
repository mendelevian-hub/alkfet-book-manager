FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# csak a backend forrás
COPY src/ ./src/

RUN dotnet restore ./src/DataAccessApi/DataAccessApi.csproj
RUN dotnet publish ./src/DataAccessApi/DataAccessApi.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","DataAccessApi.dll"]