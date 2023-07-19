FROM mcr.microsoft.com/dotnet/sdk:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["csharp/aws-restapi/aws-restapi.csproj", "aws-restapi/"]
RUN dotnet restore "aws-restapi/aws-restapi.csproj"
COPY ./csharp/ .
WORKDIR "/src/aws-restapi"
RUN dotnet build "aws-restapi.csproj" -c Debug -o /app/build

FROM build AS publish
RUN dotnet publish "aws-restapi.csproj" -c Debug -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:80;https://0.0.0.0:443
RUN dotnet dev-certs https
CMD ["dotnet", "aws-restapi.dll"]
#CMD ["sleep", "10000"]
