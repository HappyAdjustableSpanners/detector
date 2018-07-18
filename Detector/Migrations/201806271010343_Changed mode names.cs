namespace Detector.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Changedmodenames : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.DetectObjects", newName: "Brands");
            AlterColumn("dbo.Brands", "Name", c => c.String(nullable: false, maxLength: 50));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Brands", "Name", c => c.String());
            RenameTable(name: "dbo.Brands", newName: "DetectObjects");
        }
    }
}
