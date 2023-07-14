FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["aws-restapi/aws-restapi.csproj", "aws-restapi/"]
RUN dotnet restore "aws-restapi/aws-restapi.csproj"
COPY . .
WORKDIR "/src/aws-restapi"
RUN dotnet build "aws-restapi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "aws-restapi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "aws-restapi.dll"]
