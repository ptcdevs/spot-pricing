using FluentMigrator;

namespace aws_migration.migrations;

[Migration(20230713000000)]
public class AddSpotPrices : Migration {
    public override void Up()
    {
        Create.Table("SpotPrices").WithDescription("structured spot price data")
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("Timestamp").AsDateTime2().NotNullable()
            .WithColumnDescription("time stamp for aws spot price")
            .WithColumn("AvailabilityZone").AsString().NotNullable()
            .WithColumn("InstanceType").AsString().NotNullable()
            .WithColumn("ProductDescription").AsString().NotNullable()
            .WithColumn("Price").AsDecimal().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("SpotPrices");
    }
}