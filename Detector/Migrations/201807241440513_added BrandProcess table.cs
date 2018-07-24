namespace Detector.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class addedBrandProcesstable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.BrandProcesses",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        brandId = c.Int(nullable: false),
                        procId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.id);
            
            DropColumn("dbo.BrandJobs", "procId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.BrandJobs", "procId", c => c.Int(nullable: false));
            DropTable("dbo.BrandProcesses");
        }
    }
}
