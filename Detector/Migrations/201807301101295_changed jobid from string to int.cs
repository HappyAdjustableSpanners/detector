namespace Detector.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class changedjobidfromstringtoint : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.BrandJobs", "jobId", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.BrandJobs", "jobId", c => c.String());
        }
    }
}
