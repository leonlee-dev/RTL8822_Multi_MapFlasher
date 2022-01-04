namespace MapFlasher.Migrations
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity.Infrastructure.Annotations;
    using System.Data.Entity.Migrations;
    
    public partial class initCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "public.iot03_record",
                c => new
                    {
                        sn_id = c.Int(nullable: false, identity: true),
                        sn = c.String(maxLength: 20),
                        wl_mac = c.String(maxLength: 12),
                        bt_mac = c.String(maxLength: 12),
                        is_written = c.Boolean(defaultValue: false,
                            annotations: new Dictionary<string, AnnotationValues>
                            {
                                { 
                                    "DefaultBoolean",
                                    new AnnotationValues(oldValue: null, newValue: "False")
                                },
                            }),
                    })
                .PrimaryKey(t => t.sn_id)
                .Index(t => t.sn, unique: true)
                .Index(t => new { t.wl_mac, t.bt_mac }, unique: true, name: "IX_UNIQUE_COMBO_MAC")
                .Index(t => t.wl_mac, unique: true, name: "IX_UNIQUE_WL_MAC")
                .Index(t => t.bt_mac, unique: true, name: "IX_UNIQUE_BT_MAC");
            
            CreateTable(
                "public.t1_test_result",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        result = c.Boolean(defaultValue: false,
                            annotations: new Dictionary<string, AnnotationValues>
                            {
                                { 
                                    "DefaultBoolean",
                                    new AnnotationValues(oldValue: null, newValue: "False")
                                },
                            }),
                        date_time = c.DateTime(),
                        duration = c.Decimal(precision: 18, scale: 2),
                        current_none = c.Decimal(precision: 18, scale: 2),
                        current_a = c.Decimal(precision: 18, scale: 2),
                        current_b = c.Decimal(precision: 18, scale: 2),
                        current_ab = c.Decimal(precision: 18, scale: 2),
                        current_bt = c.Decimal(precision: 18, scale: 2),
                        test_line = c.String(maxLength: 5),
                        test_station = c.String(maxLength: 10),
                        test_step = c.String(maxLength: 20),
                        emp_id = c.String(maxLength: 10),
                        comment = c.String(),
                        sn_ref_id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.id)
                .ForeignKey("public.iot03_record", t => t.sn_ref_id, cascadeDelete: true)
                .Index(t => t.sn_ref_id);
            
            CreateTable(
                "public.t2_test_result",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        result = c.Boolean(defaultValue: false,
                            annotations: new Dictionary<string, AnnotationValues>
                            {
                                { 
                                    "DefaultBoolean",
                                    new AnnotationValues(oldValue: null, newValue: "False")
                                },
                            }),
                        date_time = c.DateTime(),
                        duration = c.Decimal(precision: 18, scale: 2),
                        wifi_tx_a = c.Boolean(),
                        wifi_rx_a = c.Boolean(),
                        wifi_tx_b = c.Boolean(),
                        wifi_rx_b = c.Boolean(),
                        bt_tx = c.Boolean(),
                        bt_rx = c.Boolean(),
                        test_line = c.String(maxLength: 5),
                        test_station = c.String(maxLength: 10),
                        test_step = c.String(maxLength: 20),
                        emp_id = c.String(maxLength: 10),
                        comment = c.String(),
                        sn_ref_id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.id)
                .ForeignKey("public.iot03_record", t => t.sn_ref_id, cascadeDelete: true)
                .Index(t => t.sn_ref_id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("public.t2_test_result", "sn_ref_id", "public.iot03_record");
            DropForeignKey("public.t1_test_result", "sn_ref_id", "public.iot03_record");
            DropIndex("public.t2_test_result", new[] { "sn_ref_id" });
            DropIndex("public.t1_test_result", new[] { "sn_ref_id" });
            DropIndex("public.iot03_record", "IX_UNIQUE_BT_MAC");
            DropIndex("public.iot03_record", "IX_UNIQUE_WL_MAC");
            DropIndex("public.iot03_record", "IX_UNIQUE_COMBO_MAC");
            DropIndex("public.iot03_record", new[] { "sn" });
            DropTable("public.t2_test_result",
                removedColumnAnnotations: new Dictionary<string, IDictionary<string, object>>
                {
                    {
                        "result",
                        new Dictionary<string, object>
                        {
                            { "DefaultBoolean", "False" },
                        }
                    },
                });
            DropTable("public.t1_test_result",
                removedColumnAnnotations: new Dictionary<string, IDictionary<string, object>>
                {
                    {
                        "result",
                        new Dictionary<string, object>
                        {
                            { "DefaultBoolean", "False" },
                        }
                    },
                });
            DropTable("public.iot03_record",
                removedColumnAnnotations: new Dictionary<string, IDictionary<string, object>>
                {
                    {
                        "is_written",
                        new Dictionary<string, object>
                        {
                            { "DefaultBoolean", "False" },
                        }
                    },
                });
        }
    }
}
