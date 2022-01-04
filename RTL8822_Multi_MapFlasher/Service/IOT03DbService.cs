using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapFlasher
{
    public class IOT03DbService
    {
        private readonly string connectionString;

        public IOT03DbService(string ip, string user, string password)
        {
            this.connectionString = "Server=" + ip + ";port=5432;Database=IOT03;User Id=" + user + ";Password=" + password + ";";
            // 關閉檢查MigrationHistory Table
            Database.SetInitializer<IOT03DbContext>(null);
        }

        public bool CheckModuleSnIsExist(string sn)
        {
            try
            {
                using (var dbContext = new IOT03DbContext(connectionString))
                {
                    var _sn = (from d in dbContext.IOT03Records
                               where d.Sn == sn
                               select d.Sn).AsNoTracking().FirstOrDefault();
                    return _sn != null ? true : false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool CheckModuleIsWritten(string sn)
        {
            try
            {
                using (var dbContext = new IOT03DbContext(connectionString))
                {
                    var isWritten = (from d in dbContext.IOT03Records
                                     where d.Sn == sn && d.IsWritten == true
                                     select d.IsWritten).FirstOrDefault();
                    return isWritten != null && (bool)isWritten;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int GetSnId(string sn)
        {
            try
            {
                using (var dbContext = new IOT03DbContext(connectionString))
                {
                    var snId = (from d in dbContext.IOT03Records
                                where d.Sn == sn
                                select d.SnId).FirstOrDefault();
                    if (snId == 0)
                        return -1;
                    return snId;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string[] GetMac(string sn)
        {
            try
            {
                using (var dbContext = new IOT03DbContext(connectionString))
                {
                    //DateTime s = DateTime.Now;
                    //dbContext.Database.Log = Console.WriteLine;
                    var macs = (from d in dbContext.IOT03Records
                                where d.Sn == sn
                                select d).AsNoTracking().FirstOrDefault();
                    //Console.WriteLine(DateTime.Now.Subtract(s).TotalMilliseconds + " ms");
                    if (macs != null)
                    {
                        string[] macArray = new string[2];
                        macArray[0] = macs.WlMac.ToUpper();
                        macArray[1] = macs.BtMac.ToUpper();
                        return macArray;
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool GetT1Result(string sn)
        {
            try
            {

                using (var dbContext = new IOT03DbContext(connectionString))
                {
                    //dbContext.Database.Log = Console.WriteLine;
                    var result = (from d1 in dbContext.IOT03Records
                                  join d2 in dbContext.T1Records on d1.SnId equals d2.SnRefId
                                  where d1.Sn == sn
                                  orderby d2.DateTime descending
                                  select d2.Result).FirstOrDefault();
                    return result != null ? (bool)result : false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool GetT2Result(string sn)
        {
            try
            {
                using (var dbContext = new IOT03DbContext(connectionString))
                {
                    var result = (from d1 in dbContext.IOT03Records
                                  join d2 in dbContext.T2Records on d1.SnId equals d2.SnRefId
                                  where d1.Sn == sn
                                  orderby d2.DateTime descending
                                  select d2.Result).FirstOrDefault();
                    return result != null ? (bool)result : false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<T1Record> GetT1Record(string sn)
        {
            try
            {
                using (var dbContext = new IOT03DbContext(connectionString))
                {
                    // Eager Loading mode
                    //dbContext.Database.Log = Console.WriteLine;
                    dbContext.Configuration.LazyLoadingEnabled = false; // close default Lazy Loading mode
                    var query = dbContext.IOT03Records.Include(t => t.T1Records)
                                                      .Where(x => x.Sn == sn)
                                                      .Select(x => new { x.Sn, x.T1Records });
                    List<T1Record> t1Records = new List<T1Record>();
                    foreach (var iOT03Record in query)
                        foreach (var t1Record in iOT03Record.T1Records)
                            t1Records.Add(t1Record);
                    return t1Records;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<T2Record> GetT2Record(string sn)
        {
            try
            {
                using (var dbContext = new IOT03DbContext(connectionString))
                {
                    // Eager Loading mode
                    //dbContext.Database.Log = Console.WriteLine;
                    dbContext.Configuration.LazyLoadingEnabled = false; // close default Lazy Loading mode
                    var query = dbContext.IOT03Records.Include(t => t.T2Records)
                                                      .Where(x => x.Sn == sn)
                                                      .Select(x => new { x.Sn, x.T2Records });
                    List<T2Record> t2Records = new List<T2Record>();
                    foreach (var iOT03Record in query)
                        foreach (var t2Record in iOT03Record.T2Records)
                            t2Records.Add(t2Record);
                    return t2Records;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int InsertT1Record(T1Record record)
        {
            try
            {
                using (var dbContext = new IOT03DbContext(connectionString))
                {
                    dbContext.T1Records.Add(record);
                    return dbContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int InsertT2Record(T2Record record)
        {
            try
            {
                using (var dbContext = new IOT03DbContext(connectionString))
                {
                    dbContext.T2Records.Add(record);
                    return dbContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int UpdateIOT03WrittenState(string sn, bool isWritten)
        {
            try
            {
                using (var dbContext = new IOT03DbContext(connectionString))
                {
                    //dbContext.Database.Log = Console.WriteLine;
                    var result = (from d in dbContext.IOT03Records
                                  where d.Sn == sn
                                  select d).FirstOrDefault();

                    //Method 1
                    if (result != null)
                        result.IsWritten = isWritten;
                    /* Method 2
                    if (result != null)
                    {
                        result.IsWritten = isWritten;
                        dbContext.IOT03Records.Add(result);
                        dbContext.Entry(result).State = EntityState.Modified;
                    }
                    */
                    return dbContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int test(string sn, bool isWritten)
        {
            try
            {
                using (var dbContext = new IOT03DbContext(connectionString))
                {
                    dbContext.Database.Log = Console.WriteLine;

                    dbContext.IOT03Records.Where(p => p.Sn == sn).Load();

                    foreach (var result in dbContext.IOT03Records)
                    {
                        Console.WriteLine(result.Sn + " " + result.IsWritten + " " + dbContext.Entry(result).State);
                    }
                    Console.WriteLine("---local---");
                    foreach (var result in dbContext.IOT03Records.Local)
                    {
                        Console.WriteLine(result.Sn + " " + result.IsWritten + " " + dbContext.Entry(result).State);
                    }

                    IOT03Record iot03Recrrd = dbContext.IOT03Records.FirstOrDefault();
                    Console.WriteLine(">>>>>>>>>>>>>");
                    foreach (var result in dbContext.IOT03Records)
                    {
                        Console.WriteLine(result.Sn + " " + result.IsWritten + " " + dbContext.Entry(result).State);
                    }
                    Console.WriteLine("---local---");
                    foreach (var result in dbContext.IOT03Records.Local)
                    {
                        Console.WriteLine(result.Sn + " " + result.IsWritten + " " + dbContext.Entry(result).State);
                    }
                    Console.WriteLine(">>>>>>>>>>>>>");
                    iot03Recrrd.IsWritten = isWritten;
                    Console.WriteLine("<<<<<<<<<<<<<");
                    foreach (var result in dbContext.IOT03Records)
                    {
                        Console.WriteLine(result.Sn + " " + result.IsWritten + " " + dbContext.Entry(result).State);
                    }
                    Console.WriteLine("---local---");
                    foreach (var result in dbContext.IOT03Records.Local)
                    {
                        Console.WriteLine(result.Sn + " " + result.IsWritten + " " + dbContext.Entry(result).State);
                    }
                    Console.WriteLine("<<<<<<<<<<<<<");
                    return dbContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
