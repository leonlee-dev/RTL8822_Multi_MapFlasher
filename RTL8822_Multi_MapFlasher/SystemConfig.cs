using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MapFlasher
{
    public class SystemConfig
    {
        private static readonly string configPath = Application.StartupPath + "\\Setup\\config.txt";
        private static readonly string testLimitPath = Application.StartupPath + "\\Setup\\test_limit.txt";

        public const int STATIONCOUNT = 4;
        public static string Platform { get; private set; }
        public static string[] ConnectionInterfaces { get; private set; } = new string[STATIONCOUNT];
        public static string DrivFile { get; private set; }
        public static string DrivDir { get; private set; }
        public static string DbIp { get; private set; }
        public static string FtpIp { get; private set; }

        public static decimal CurrentNoneUpper { get; private set; }
        public static decimal CurrentNoneLower { get; private set; }
        public static decimal CurrentAUpper { get; private set; }
        public static decimal CurrentALower { get; private set; }
        public static decimal CurrentBUpper { get; private set; }
        public static decimal CurrentBLower { get; private set; }
        public static decimal CurrentABUpper { get; private set; }
        public static decimal CurrentABLower { get; private set; }
        public static decimal CurrentBtUpper { get; private set; }
        public static decimal CurrentBtLower { get; private set; }

        public static void Load()
        {
            FileStream fs1, fs2;
            byte[] byteData1, byteData2;

            fs1 = File.Open(configPath, FileMode.Open);
            byteData1 = new byte[1024];
            IAsyncResult ar1 = fs1.BeginRead(byteData1, 0, byteData1.Length, delegate (IAsyncResult ar)
            {
                int byteReads = fs1.EndRead(ar);
                string str = Encoding.UTF8.GetString(byteData1, 0, byteReads);

                try
                {
                    // PLATFORM
                    int sIndex = str.IndexOf("PLATFORM", 0);
                    Platform = str.Substring(sIndex, str.IndexOf("\r\n", sIndex) - sIndex).Split('=')[1].Trim();
                    // CONNECTION_INTERFACE
                    for (int i = 0; i < ConnectionInterfaces.Length; i++)
                    {
                        sIndex = str.IndexOf("INTERFACE_" + (i + 1), 0);
                        ConnectionInterfaces[i] = str.Substring(sIndex, str.IndexOf("\r\n", sIndex) - sIndex).Split('=')[1].Trim();
                    }
                    // KO_FILE
                    sIndex = str.IndexOf("KO_FILE", 0);
                    DrivFile = str.Substring(sIndex, str.IndexOf("\r\n", sIndex) - sIndex).Split('=')[1].Trim();
                    // KO_DIR
                    sIndex = str.IndexOf("KO_DIR", 0);
                    DrivDir = str.Substring(sIndex, str.IndexOf("\r\n", sIndex) - sIndex).Split('=')[1].Trim();
                    // DB_SERVER
                    sIndex = str.IndexOf("DB_SERVER", 0);
                    DbIp = str.Substring(sIndex, str.IndexOf("\r\n", sIndex) - sIndex).Split('=')[1].Trim();
                    // FTP_SERVER
                    sIndex = str.IndexOf("FTP_SERVER", 0);
                    FtpIp = str.Substring(sIndex, str.IndexOf("\r\n", sIndex) - sIndex).Split('=')[1].Trim();
                }
                catch (IOException io)
                {
                    MessageBox.Show("Load config IOException:" + io.Message);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Load config Exception:" + ex.Message);
                }
                finally
                {
                    fs1.Close();
                }
            }, null);

            fs2 = File.Open(testLimitPath, FileMode.Open);
            byteData2 = new byte[1024];
            IAsyncResult ar2 = fs2.BeginRead(byteData2, 0, byteData2.Length, delegate (IAsyncResult ar)
            {
                int byteReads = fs2.EndRead(ar);
                string str = Encoding.UTF8.GetString(byteData2, 0, byteReads);

                try
                {
                    // CURRENT_NONE_UPPER
                    int sIndex = str.IndexOf("CURRENT_NONE_UPPER", 0);
                    CurrentNoneUpper = Convert.ToDecimal(str.Substring(sIndex, str.IndexOf("\r\n", sIndex) - sIndex).Split('=')[1].Trim());
                    // CURRENT_NONE_LOWER
                    sIndex = str.IndexOf("CURRENT_NONE_LOWER", 0);
                    CurrentNoneLower = Convert.ToDecimal(str.Substring(sIndex, str.IndexOf("\r\n", sIndex) - sIndex).Split('=')[1].Trim());
                    // CURRENT_A_UPPER
                    sIndex = str.IndexOf("CURRENT_A_UPPER", 0);
                    CurrentAUpper = Convert.ToDecimal(str.Substring(sIndex, str.IndexOf("\r\n", sIndex) - sIndex).Split('=')[1].Trim());
                    // CURRENT_A_LOWER
                    sIndex = str.IndexOf("CURRENT_A_LOWER", 0);
                    CurrentALower = Convert.ToDecimal(str.Substring(sIndex, str.IndexOf("\r\n", sIndex) - sIndex).Split('=')[1].Trim());
                    // CURRENT_B_UPPER
                    sIndex = str.IndexOf("CURRENT_B_UPPER", 0);
                    CurrentBUpper = Convert.ToDecimal(str.Substring(sIndex, str.IndexOf("\r\n", sIndex) - sIndex).Split('=')[1].Trim());
                    // CURRENT_B_LOWER
                    sIndex = str.IndexOf("CURRENT_B_LOWER", 0);
                    CurrentBLower = Convert.ToDecimal(str.Substring(sIndex, str.IndexOf("\r\n", sIndex) - sIndex).Split('=')[1].Trim());
                    // CURRENT_AB_UPPER
                    sIndex = str.IndexOf("CURRENT_AB_UPPER", 0);
                    CurrentABUpper = Convert.ToDecimal(str.Substring(sIndex, str.IndexOf("\r\n", sIndex) - sIndex).Split('=')[1].Trim());
                    // CURRENT_AB_LOWER
                    sIndex = str.IndexOf("CURRENT_AB_LOWER", 0);
                    CurrentABLower = Convert.ToDecimal(str.Substring(sIndex, str.IndexOf("\r\n", sIndex) - sIndex).Split('=')[1].Trim());
                    // CURRENT_BT_UPPER
                    sIndex = str.IndexOf("CURRENT_BT_UPPER", 0);
                    CurrentBtUpper = Convert.ToDecimal(str.Substring(sIndex, str.IndexOf("\r\n", sIndex) - sIndex).Split('=')[1].Trim());
                    // CURRENT_BT_LOWER
                    sIndex = str.IndexOf("CURRENT_BT_LOWER", 0);
                    CurrentBtLower = Convert.ToDecimal(str.Substring(sIndex, str.IndexOf("\r\n", sIndex) - sIndex).Split('=')[1].Trim());
                }
                catch (IOException io)
                {
                    MessageBox.Show("Load test_limit IOException:" + io.Message);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Load test_limit Exception:" + ex.Message);
                }
                finally
                {
                    fs2.Close();
                }
            }, null);

            while(!ar1.IsCompleted || !ar2.IsCompleted)
            {
                Thread.Sleep(10);
            }
        }
    }
}
