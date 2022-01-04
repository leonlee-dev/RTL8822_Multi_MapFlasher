namespace MapFlasher.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<MapFlasher.IOT03DbContext>
    {
        public Configuration()
        {
            CodeGenerator = new ExtendedMigrationCodeGenerator();
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(MapFlasher.IOT03DbContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method
            //  to avoid creating duplicate seed data.
        }
    }
}
