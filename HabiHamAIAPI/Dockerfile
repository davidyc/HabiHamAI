FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY HabiHamAIAPI.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 10000

CMD ["sh", "-c", "dotnet HabiHamAIAPI.dll --urls http://0.0.0.0:${PORT:-10000}"]
