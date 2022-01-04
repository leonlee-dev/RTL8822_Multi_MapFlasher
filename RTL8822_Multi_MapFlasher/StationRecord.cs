using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapFlasher
{
    public class StationRecord
    {
        public decimal currentNone;
        public decimal currentA;
        public decimal currentB;
        public decimal currentAB;
        public decimal currentBt;
        public bool isEfuseWritten;
        public bool result;

        public StationRecord()
        {
            currentNone = 0;
            currentA = 0;
            currentB = 0;
            currentAB = 0;
            currentBt = 0;
            isEfuseWritten = false;
            result = false;
        }
    }
}
