using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapFlasher
{
    public class IOT03DbContext : DbContext
    {
        public IOT03DbContext() : base("name=IOT03DbContext")
        {

        }

        public IOT03DbContext(string connectionString) : base(connectionString)
        {

        }

        public virtual DbSet<IOT03Record> IOT03Records { get; set; }
        public virtual DbSet<T1Record> T1Records { get; set; }
        public virtual DbSet<T2Record> T2Records { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Entity<IOT03Record>().Property(t => t.IsWritten).HasColumnAnnotation("DefaultBoolean", false);
            //modelBuilder.Entity<IOT03Record>().Property(t => t.UsedState).HasColumnAnnotation("DefaultUsedState", (int)UsedState.NOTUSED);
            modelBuilder.Entity<T1Record>().Property(t => t.Result).HasColumnAnnotation("DefaultBoolean", false);
            modelBuilder.Entity<T2Record>().Property(t => t.Result).HasColumnAnnotation("DefaultBoolean", false);           
        }
    }
}
