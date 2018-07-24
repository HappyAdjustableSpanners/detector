namespace Detector.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class adduseridtobrand : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Brands", "userId", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Brands", "userId");
        }
    }
}
