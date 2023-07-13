// See https://aka.ms/new-console-template for more information

using aws_migration.migrations;
using FluentMigrator.Runner;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();
var npgsqlConnectionStringBuilder = new NpgsqlConnectionStringBuilder()
{
    Host = config["postgres:host"],
    Port = int.Parse(config["postgres:port"] ?? string.Empty),
    Database = config["postgres:database"],
    Username = config["postgres:username"],
    Password = config["POSTGRESQL_PASSWORD"],
    SslMode = SslMode.VerifyCA,
    RootCertificate = "ptcdevs-psql-ca-certificate.crt",
};

var serviceProvider = new ServiceCollection()
    .AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddPostgres()
        // Set the connection string
        .WithGlobalConnectionString(npgsqlConnectionStringBuilder.ToString())
        // Define the assembly containing the migrations
        .ScanIn(typeof(AddLogTable).Assembly).For.Migrations())
    // Enable logging to console in the FluentMigrator way
    .AddLogging(lb => lb.AddFluentMigratorConsole())
    // Build the service provider
    .BuildServiceProvider(false);

using var scope = serviceProvider.CreateScope();
var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

// Execute the migrations
runner.MigrateUp();

Console.WriteLine("fin");