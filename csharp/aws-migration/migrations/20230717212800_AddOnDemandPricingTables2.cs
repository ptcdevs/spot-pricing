using FluentMigrator;
using NpgsqlTypes;

namespace aws_migration.migrations;

[Migration(20230717212800)]
public class AddOnDemandPricingTables2 : Migration {
    public override void Up()
    {
        Create.Table("OnDemandPricing").WithDescription("on demand pricing data")
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            // .WithColumn("CreatedAt").AsCustom(NpgsqlDbType.TimestampTz.ToString()).NotNullable()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable()
            .WithColumn("Header").AsString().NotNullable().WithColumnDescription("Header line of csv file")
            .WithColumn("Url").AsString().NotNullable().WithColumnDescription("file download url");
        Create.Table("OnDemandCsvRows").WithDescription("raw imported csv rows")
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("OnDemandCsvFilesId").AsInt64().ForeignKey("OnDemandCsvFiles", "Id")
            .WithColumn("CreatedAt").AsDateTime2().NotNullable()
            .WithColumn("Row").AsString().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("OnDemandCsvRows");
        Delete.Table("OnDemandCsvFiles");
    }
}