using FluentMigrator;

namespace aws_migration.migrations;

[Migration(20230716214800)]
public class AddOnDemandPricingTables : Migration {
    public override void Up()
    {
        Create.Table("OnDemandCsvFiles").WithDescription("raw imported csv filenames and headers")
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("CreatedAt").AsDateTime2().NotNullable()
            .WithColumn("Filename").AsString().NotNullable()
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