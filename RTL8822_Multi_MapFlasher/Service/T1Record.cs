using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapFlasher
{
    [Table("t1_test_result", Schema = "public")]
    public class T1Record
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }
        [Column("result")]
        public bool? Result { get; set; }
        [Column("date_time")]
        public DateTime? DateTime { get; set; }
        [Column("duration")]
        public decimal? Duration { get; set; }
        [Column("current_none")]
        public decimal? CurrentNone { get; set; }
        [Column("current_a")]
        public decimal? CurrentA { get; set; }
        [Column("current_b")]
        public decimal? CurrentB { get; set; }
        [Column("current_ab")]
        public decimal? CurrentAB { get; set; }
        [Column("current_bt")]
        public decimal? CurrentBt { get; set; }
        [MaxLength(5)]
        [Column("test_line")]
        public string TestLine { get; set; }
        [MaxLength(10)]
        [Column("test_station")]
        public string TestSation { get; set; }
        [MaxLength(20)]
        [Column("test_step")]
        public string TestStep { get; set; }
        [MaxLength(10)]
        [Column("emp_id")]
        public string EmpId { get; set; }
        [Column("comment")]
        public string Comment { get; set; }

        [ForeignKey("IOT03Record")]
        [Column("sn_ref_id")]
        public int SnRefId { get; set; }
        public IOT03Record IOT03Record { get; set; }
    }
}
