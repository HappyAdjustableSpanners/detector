namespace Detector.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class changedbrandjobidfrominttostring : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.BrandJobs", "jobId", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.BrandJobs", "jobId", c => c.Int(nullable: false));
        }
    }
}
