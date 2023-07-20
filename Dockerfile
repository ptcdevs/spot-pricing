FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["csharp/aws-restapi/aws-restapi.csproj", "aws-restapi/"]
RUN dotnet restore "aws-restapi/aws-restapi.csproj"
COPY ./csharp/ .
WORKDIR "/src/aws-restapi"

FROM build as devbuild
WORKDIR "/src/aws-restapi"
RUN dotnet build "aws-restapi.csproj" -c Debug -o /app/build

FROM build as prodbuild
WORKDIR "/src/aws-restapi"
RUN dotnet build "aws-restapi.csproj" -c Release -o /app/build

FROM devbuild AS development
EXPOSE 3000
EXPOSE 3001
ENV ASPNETCORE_URLS=http://0.0.0.0:3000;https://0.0.0.0:3001
ENV ASPNETCORE_ENVIRONMENT=Development
RUN dotnet dev-certs https
CMD ["dotnet", "run", "-c", "Debug"]

FROM build AS publish
RUN dotnet publish "aws-restapi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS release
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
#CMD ["dotnet", "aws-restapi.dll"]
CMD ["sleep", "10000"]
