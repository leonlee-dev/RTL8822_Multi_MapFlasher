using MapFlasher;
using RTKModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static MapFlasher.INA219DataParser;

namespace MapFlasher
{
    public class CurrentINA219Diagnosis
    {
        public const string pyCmd = "python /home/pi/test/ina219.pyc";
        public const string pyCmd_Cfg = pyCmd + " 9";
        public const string strBTDataKeyWordHead = "@@INA219";
        public const string strBTDataKeyWordTail = "912ANI@@";
        public static readonly Regex currentRegex = new Regex(strBTDataKeyWordHead + ",([0-9a-zA-Z]{4},){6}" + strBTDataKeyWordTail);

        private double upperLimit;
        private double lowerLimit;

        public CurrentINA219Diagnosis(double upperLimit, double lowerLimit)
        {
            this.upperLimit = upperLimit;
            this.lowerLimit = lowerLimit;
        }

        public bool TestResult(string data, out INA219Data ina219Data)
        {
            string rawData = data.Replace(strBTDataKeyWordHead, "").Replace(strBTDataKeyWordTail, "");
            ina219Data = INA219DataParser.GetINA219Data(rawData);
            if (ina219Data.Current_mA >= lowerLimit && ina219Data.Current_mA <= upperLimit)
                return true;
            return false;
        }

        public static string GetFormatResult(INA219Data data, string headerMsg, string tailMsg)
        {
            string[] hMsg = headerMsg.Split(',');
            string[] tMsg = tailMsg.Split(',');

            if (hMsg.Length < 4 || tMsg.Length < 4)
                throw new Exception("Message of header and tailer must contain 4 sub-message with delimiter:','");

            return hMsg[0] + "Shunt Voltage = " + data.ShuntVoltage_mV.ToString("F03") + "mV" + tMsg[0] + "\r\n"
                 + hMsg[1] + "Bus Voltage = " + data.BusVoltage_V.ToString("F03") + "V" + tMsg[1] + "\r\n"
                 + hMsg[2] + "Power = " + data.Power_mW.ToString("F03") + "mW" + tMsg[2] + "\r\n"
                 + hMsg[3] + "Current = " + data.Current_mA.ToString("F03") + "mA" + tMsg[3] + "\r\n";
        }
    }
}
