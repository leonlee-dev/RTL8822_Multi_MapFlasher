using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapFlasher
{
    public class INA219DataParser
    {
        public class INA219Data
        {
            public double ShuntVoltage_mV;
            public double BusVoltage_V;
            public double Power_mW;
            public double Current_mA;
        }

        public static INA219Data GetINA219Data(string strRawData)
        {
            if (strRawData.Trim().Length == 0)
            {
                return null;
            }

            ushort[] INA219RegVal = new ushort[6] { 0, 0, 0, 0, 0, 0 };

            //parse raw data string
            string[] rawData = strRawData.Split(',');
            int i;
            for (i = 1; i <= 6; i++)
            {
                INA219RegVal[i - 1] = Convert.ToUInt16(rawData[i], 16);
            }

            INA219Data data = new INA219Data()
            {
                ShuntVoltage_mV = (short)INA219RegVal[1] * 0.01,
                BusVoltage_V = ((INA219RegVal[2] >> 3) * 4.0) / 1000.0,
                Power_mW = INA219RegVal[3] * 1.0,    //1mW per bit
                Current_mA = (short)INA219RegVal[4] * 0.05 //50uA per bit
            };

            return data;
        }
    }
}
