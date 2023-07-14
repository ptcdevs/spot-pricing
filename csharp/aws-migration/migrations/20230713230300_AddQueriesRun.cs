using FluentMigrator;

namespace aws_migration.migrations;

[Migration(20230713230300)]
public class AddQueriesRun : Migration {
    public override void Up()
    {
        Create.Table("QueriesRun").WithDescription("list of timestamps and searches queried")
            .WithColumn("Id").AsInt64().PrimaryKey().Identity()
            .WithColumn("StartTime").AsDateTime2().NotNullable()
            .WithColumn("Search").AsString().NotNullable();
    }

    public override void Down()
    {
        Delete.Table("QueriesRun");
    }
}