using RTKModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapFlasher
{
    public class TestStation : IMpStation
    {
        public int id;
        public string sn;
        public int testNum;
        public TestResult finalTestResult;
        public TestStatus testStatus;
        public DateTime start;
        public DateTime end;
        public byte[] wlMac;
        public byte[] btMac;
        public ComPort dutComPort;
        public RtwCommand dutCommand;
        public bool isUsed;
        public StationRecord record;

        public TestStation(int dutID)
        {
            InitStation(dutID);
        }

        public void InitStation(int dutID)
        {
            id = dutID;
            sn = "";
            testNum = 0;
            finalTestResult = TestResult.FAILURE;
            testStatus = TestStatus.INIT;
            wlMac = null;
            btMac = null;
            dutCommand = null;
            isUsed = false;
            record = new StationRecord();
        }
    }
}
