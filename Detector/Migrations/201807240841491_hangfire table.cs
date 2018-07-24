namespace Detector.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class hangfiretable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.BrandJobs",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        brandId = c.Int(nullable: false),
                        jobId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.BrandJobs");
        }
    }
}
