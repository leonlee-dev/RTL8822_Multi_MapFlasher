using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapFlasher
{
    //public enum UsedState
    //{
    //    NOTUSED,
    //    USED,
    //    USING
    //}

    [Table("iot03_record", Schema = "public")]
    public class IOT03Record
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("sn_id")]
        public int SnId { get; set; }
        [MaxLength(20)]
        [Index(IsUnique = true)]
        [Column("sn")]
        public string Sn { get; set; }
        [MaxLength(12)]
        [Index("IX_UNIQUE_WL_MAC", IsUnique = true)]
        [Index("IX_UNIQUE_COMBO_MAC", Order = 0, IsUnique = true)]
        [Column("wl_mac")]
        public string WlMac { get; set; }
        [MaxLength(12)]
        [Index("IX_UNIQUE_BT_MAC", IsUnique = true)]
        [Index("IX_UNIQUE_COMBO_MAC", Order = 1, IsUnique = true)]
        [Column("bt_mac")]
        public string BtMac { get; set; }
        [Column("is_written")]
        public bool? IsWritten { get; set; }
        //[Column("used_state")]
        //public UsedState UsedState { get; set; } // 0 - not used, 1 - used, 2 - using

        public ICollection<T1Record> T1Records { get; set; }
        public ICollection<T2Record> T2Records { get; set; }
    }
}
