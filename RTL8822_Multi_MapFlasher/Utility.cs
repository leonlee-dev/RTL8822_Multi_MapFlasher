using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapFlasher
{
    public class Utility
    {
        public static string GetMacAddrWithColon(byte[] mac)
        {
            if (mac.Length != 6)
                throw new Exception("pls take 6 bytes!");

            string macAddr = "";
            for (int i = 0; i < mac.Length; i++)
            {
                macAddr += mac[i].ToString("X2") + ((i != mac.Length - 1) ? ":" : "");
            }
            return macAddr;
        }

        public static string GetMacAddr(byte[] mac)
        {
            if (mac.Length != 6)
                throw new Exception("pls take 6 bytes!");

            string macAddr = "";
            for (int i = 0; i < mac.Length; i++)
            {
                macAddr += mac[i].ToString("X2");
            }
            return macAddr;
        }

        public static byte[] GetMacAddrWithColon(string mac)
        {
            string[] sMac = mac.Split(':');
            byte[] bMac = new byte[sMac.Length];
            for (int i = 0; i < bMac.Length; i++)
            {
                bMac[i] = Convert.ToByte(sMac[i], 16);
            }
            return bMac;
        }

        public static byte[] GetMacAddr(string mac)
        {
            if (mac.Length < 12)
                throw new Exception("pls take 12 characters!");

            byte[] bMac = new byte[6];
            for (int i = 0; i < bMac.Length; i++)
            {
                bMac[i] = Convert.ToByte(mac.Substring(i * 2, 2), 16);
            }
            return bMac;
        }

        public static byte[] CalcNextMac(byte[] mac)
        {
            if (mac.Length != 6)
                throw new Exception("pls take 6 bytes!");

            bool carry = false;
            mac[mac.Length - 1] = (byte)(mac[mac.Length - 1] + 1);
            for (int i = mac.Length - 1; i >= 0; i--)
            {
                if (carry)
                    mac[i] = (byte)(mac[i] + 1);
                if (mac[i] == 0x00)
                    carry = true;
                else
                {
                    carry = false;
                    break;
                }
            }
            return mac;
        }

        public static string CalcNextMacWithColon(string mac)
        {
            byte[] bMac = GetMacAddrWithColon(mac);
            return GetMacAddrWithColon(CalcNextMac(bMac));
        }
    }
}
