using FluentMigrator;

namespace aws_migration.migrations;

[Migration(20230713000000)]
public class AddSpotPrices : Migration {
    public override void Up()
    {
        Create.Table("SpotPriceQueries").WithDescription("list of dates and hours we want to sample for spot prices")
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("Date").AsDate().NotNullable()
            .WithColumn("Hour").AsInt64().NotNullable()
            .WithColumn("QueryType").AsString().NotNullable();
        Create.Table("SpotPricesRaw").WithDescription("json serialized spot price data")
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("CreatedAt").AsDateTime2().WithColumnDescription("when db record was created").NotNullable()
            .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("Updated").AsDateTime2().WithColumnDescription("when db record was updated").NotNullable()
            .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("SpotPriceJson").AsString().NotNullable();
        Create.Table("SpotPrices").WithDescription("structured spot price data")
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("CreatedAt").AsDateTime2().WithColumnDescription("when db record was created").NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("Updated").AsDateTime2().WithColumnDescription("when db record was updated").NotNullable()
                .WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("AvailabilityZone").AsString().NotNullable()
            .WithColumn("InstanceType").AsString().NotNullable()
            .WithColumn("ProductDescription").AsString().NotNullable()
            .WithColumn("Price").AsCurrency().NotNullable()
            .WithColumn("Timestamp").AsDateTime2().WithColumnDescription("time stamp for aws spot price").NotNullable();
    }

    public override void Down()
    {
        Delete.Table("SpotQueries");
        Delete.Table("SpotPrices");
        Delete.Table("SpotPricesRaw");
    }
}