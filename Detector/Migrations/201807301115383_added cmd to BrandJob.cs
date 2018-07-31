namespace Detector.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addedcmdtoBrandJob : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.BrandJobs", "cmd", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.BrandJobs", "cmd");
        }
    }
}
