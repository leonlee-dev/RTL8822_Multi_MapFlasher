#define WIFIONLY
#define BTONLY
using RTKModule;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static MapFlasher.Form1;

namespace MapFlasher
{
    public class TestTask
    {
        private EfuseMap wlMap;
        private EfuseMap btMap;
        public IMpStation mpStation;
        public ILog log;

        public static readonly Regex userLoginRegex = new Regex(@"\w+\s*login\s*:\s*");
        public static readonly Regex rootPasswdRegex = new Regex(@"password\s*for\s*pi\s*:\s*");
        public static readonly Regex userHeaderRegex = new Regex(@"\w+\s*@\s*\w+\s*:\s*\S+\s*[#$]{1}");
        public static readonly Regex currentRegex = new Regex(@"@@INA219,([0-9a-zA-Z]{4},){6}912ANI@@");
        private const int DAC = 70;
        private const int BTTGPOWER = 6;

        private static object mapLock = new object();

        public TestTask(IMpStation station, ILog log)
        {
            this.mpStation = station;
            this.log = log;
        }

        private static string GetNow()
        {
            return DateTime.Now.ToString(Form1.DATETIMEFORMAT);
        }

        private static int GetWifiCurrentTestWithFreq(int stationID)
        {
            switch (stationID)
            {
                case 1:
                    return 5510;
                case 2:
                    return 5550;
                case 3:
                    return 5590;
                case 4:
                    return 5630;
                default:
                    return 5510;
            }
        }

        public TestResult Init(TestStatus status)
        {
            Form1 mainForm = Form1.GetMainForm();

            TestStation station = mpStation as TestStation;
            
            if (status == TestStatus.INIT)
            {
                Console.WriteLine("station id:" + station.id);
                mainForm.UpdateTestStage(station.id, TestStage.INIT);
                log.WriteLine(GetNow() + (station.testNum + 1) + ".Init DUT...");

                mainForm.UpdateRichTextBox(station.id, (station.testNum + 1) + ".Init DUT...");

                try
                {
                    string wlMapPath = ((TextBox)mainForm.GetFormControl("textBox1")).Text;
                    string btMapPath = ((TextBox)mainForm.GetFormControl("textBox8")).Text;
                    if (!wlMapPath.EndsWith(".map"))
                    {
                        log.WriteLine(GetNow() + "Not get any wifi sample map:" + wlMapPath + "!");
                        MessageBox.Show("Pls select the wifi map file!");
                        return TestResult.FAILURE;
                    }

                    if (!btMapPath.EndsWith(".map"))
                    {
                        log.WriteLine(GetNow() + "Not get any bt sample map:" + btMapPath + "!");
                        MessageBox.Show("Pls select the bt map file!");
                        return TestResult.FAILURE;
                    }

                    // load map
                    lock (mapLock)
                    {
                        wlMap = new EfuseMap(EfuseMap.WLMAPSIZE);
                        wlMap.LoadMap(wlMapPath);

                        btMap = new EfuseMap(EfuseMap.BTMAPSIZE);
                        btMap.LoadMap(btMapPath);
                    }

                    return TestResult.SUCCESS;
                }
                catch (Exception ex)
                {
                    log.WriteLine(GetNow() + ex.Message);
                    mainForm.UpdateRichTextBox(station.id, ex.Message);
                }

                return TestResult.FAILURE;
            }

            if (status == TestStatus.END)
            {
                return TestResult.SUCCESS;
            }

            IProxyProcessor proxyProcessor = station.dutCommand.rtwProxyProcessor;

            //// login bananapi
            //string returnString = "";
            //if (station.dutCommand.WaitFor("\n", userHeaderRegex, ref returnString, 2000, 1))
            //{
            //    // have login
            //}
            //else if (station.dutCommand.WaitFor("root", "Password:", 2000, 1)
            //      && station.dutCommand.WaitFor("bananapi", userHeaderRegex, ref returnString, 3000, 1))
            //{
            //    // login successfully
            //}
            //else
            //{
            //    log.WriteLine(GetNow() + "Login fail!");
            //    mainForm.UpdateRichTextBox(station.id, "Login fail!");
            //    return TestResult.FAILURE;
            //}

            string wifiDriv = SystemConfig.DrivFile;
            string wifiDrivDirPath = SystemConfig.DrivDir;
            if (!wifiDrivDirPath.EndsWith("/"))
                wifiDrivDirPath += "/";

            // remove module
            proxyProcessor.Send("rmmod " + wifiDriv, 400);
            proxyProcessor.Send("gpio mode 29 out", 100);
            proxyProcessor.Send("gpio write 29 1", 200);
            station.dutCommand.WaitFor(CurrentINA219Diagnosis.pyCmd_Cfg, "Config INA219 OK", 1000);
            Thread.Sleep(400);
            // power on module
            //if (station.dutCommand.WaitFor("gpio mode 29 out", userHeaderRegex, ref returnString, 1000)
            // && station.dutCommand.WaitFor("gpio write 29 1", userHeaderRegex, ref returnString, 1000))
            //{
            //    Thread.Sleep(100);
            //    if (!station.dutCommand.WaitFor(pyCmd_Cfg, "Config INA219 OK", 1000))
            //        return TestResult.FAILURE;
            //    Thread.Sleep(400);
            //}
            //else
            //{
            //    log.WriteLine(GetNow() + "Power on module fail!");
            //    mainForm.UpdateRichTextBox(station.id, "Power on module fail!");
            //    return TestResult.FAILURE;
            //}

            // insert module
            proxyProcessor.Send("insmod " + wifiDrivDirPath + wifiDriv + " rtw_powertracking_type=0 rtw_RFE_type=4", 3000);

            // make sure wlan0 interface in ifconfig information
            proxyProcessor.Send("ifconfig wlan0 up", 100);
            /*
            // read Wifi MAC from efuse map
            string wlMapMac = station.dutCommand.GetMacFromWLRealMap();
            string btMapMac = "";
            // reset BT
            if (station.dutCommand.WaitFor("echo 0 > /sys/class/rfkill/rfkill0/state", userHeaderRegex, ref returnString, 1000)
             && station.dutCommand.WaitFor("echo 1 > /sys/class/rfkill/rfkill0/state", userHeaderRegex, ref returnString, 1000))
            {
                // read BT MAC from efuse map
                btMapMac = station.dutCommand.GetMacFromBTRealMap();
            }
            log.WriteLine(GetNow() + "MAC of Wifi real map:" + wlMapMac);
            log.WriteLine(GetNow() + "MAC of BT real map:" + btMapMac);
            */
            return TestResult.SUCCESS;
        }

        public TestResult DeInit(TestStatus status)
        {
            Form1 mainForm = Form1.GetMainForm();

            TestStation station = mpStation as TestStation;

            if (status == TestStatus.INIT)
            {
                mainForm.UpdateTestStage(station.id, TestStage.DEINIT);
                log.WriteLine(GetNow() + (station.testNum + 1) + ".Deinit DUT...");

                mainForm.UpdateRichTextBox(station.id, (station.testNum + 1) + ".Deinit DUT...");

                return TestResult.FAILURE;
            }

            if (status == TestStatus.END)
            {
                return TestResult.SUCCESS;
            }

            IProxyProcessor proxyProcessor = station.dutCommand.rtwProxyProcessor;
            proxyProcessor.Send("killall rtk_hciattach", 500);
            proxyProcessor.Send("gpio write 29 0", 500);

            return TestResult.SUCCESS;
        }

        public TestResult TestWLCurrent(TestStatus status)
        {
            Form1 mainForm = Form1.GetMainForm();

            TestStation station = mpStation as TestStation;

            if (status == TestStatus.INIT)
            {
                mainForm.UpdateTestStage(station.id, TestStage.TEST_WL_CURRENT);
                log.WriteLine(GetNow() + (station.testNum + 1) + ".Test WL Current...");

                mainForm.UpdateRichTextBox(station.id, (station.testNum + 1) + ".Test WL Current...");

                return TestResult.SUCCESS;
            }

            if (status == TestStatus.END)
            {
                return TestResult.SUCCESS;
            }

            int i = 0;
            int retryCount = 3;
            string returnString = "";

            CurrentINA219Diagnosis diagnosis;

            if (station.dutCommand.StartMp())
            {
                Thread.Sleep(500);
                log.WriteLine(GetNow() + "Test None...");
                mainForm.UpdateRichTextBox(station.id, "Test None...");

                diagnosis = new CurrentINA219Diagnosis(Convert.ToDouble(SystemConfig.CurrentNoneUpper), Convert.ToDouble(SystemConfig.CurrentNoneLower));

                while (i < retryCount)
                {
                    // no power
                    if (station.dutCommand.WaitFor(CurrentINA219Diagnosis.pyCmd, currentRegex, ref returnString, 1000))
                    {
                        INA219DataParser.INA219Data data;
                        bool currentOk = diagnosis.TestResult(returnString, out data);

                        string headerMsg = string.Join(",", new string[] { GetNow(), GetNow(), GetNow(), GetNow() });
                        string tailMsg = string.Join(",", new string[] { "", "", "", "(" + SystemConfig.CurrentNoneLower + "," + SystemConfig.CurrentNoneUpper + ")" + (currentOk ? " ---> PASS" : " ---> FAIL") });
                        log.WriteLine(GetNow() + "----------------------------------------");
                        log.Write(CurrentINA219Diagnosis.GetFormatResult(data, headerMsg, tailMsg));
                        log.WriteLine(GetNow() + "----------------------------------------");
                        mainForm.UpdateRichTextBox(station.id, returnString);

                        station.record.currentNone = Decimal.Round(Convert.ToDecimal(data.Current_mA), 2, MidpointRounding.AwayFromZero);

                        if (currentOk) break;
                    }
                    else
                    {
                        log.WriteLine(GetNow() + "Command '" + CurrentINA219Diagnosis.pyCmd + "' fail!");
                        mainForm.UpdateRichTextBox(station.id, "Command '" + CurrentINA219Diagnosis.pyCmd + "' fail!");
                        return TestResult.FAILURE;
                    }
                    Thread.Sleep(100);
                    i++;
                }
                
                if(i == retryCount)
                    return TestResult.FAILURE;

                Thread.Sleep(100);

                // test antenna A
                RtwTx tx = new RtwTx()
                {
                    freq = GetWifiCurrentTestWithFreq(station.id),
                    bw = BW.B_40MHZ,
                    antPath = ANT_PATH.PATH_A,
                    rateID = RATE_ID.HTMCS7,
                    txMode = TX_MODE.PACKET_TX
                };

                if (station.dutCommand.StartSwTxCommand(tx, false)
                 && station.dutCommand.SendTxPowerCommand(tx.antPath, DAC))
                {
                    Thread.Sleep(500);
                    log.WriteLine(GetNow() + "Test A...");
                    log.WriteLine(GetNow() + "Freq:" + tx.freq + " BW:" + Wifi.bwDic[tx.bw] + " Rate:" + Wifi.rateIdDic[tx.rateID] + " Power DAC:" + DAC);
                    mainForm.UpdateRichTextBox(station.id, "Test A...");

                    try
                    {
                        diagnosis = new CurrentINA219Diagnosis(Convert.ToDouble(SystemConfig.CurrentAUpper), Convert.ToDouble(SystemConfig.CurrentALower));
                        i = 0;
                        while(i < retryCount)
                        {
                            // test current
                            if (station.dutCommand.WaitFor(CurrentINA219Diagnosis.pyCmd, currentRegex, ref returnString, 1000))
                            {
                                INA219DataParser.INA219Data data;
                                bool currentOk = diagnosis.TestResult(returnString, out data);

                                string headerMsg = string.Join(",", new string[] { GetNow(), GetNow(), GetNow(), GetNow() });
                                string tailMsg = string.Join(",", new string[] { "", "", "", "(" + SystemConfig.CurrentALower + "," + SystemConfig.CurrentAUpper + ")" + (currentOk ? " ---> PASS" : " ---> FAIL") });
                                log.WriteLine(GetNow() + "----------------------------------------");
                                log.Write(CurrentINA219Diagnosis.GetFormatResult(data, headerMsg, tailMsg));
                                log.WriteLine(GetNow() + "----------------------------------------");
                                mainForm.UpdateRichTextBox(station.id, returnString);

                                station.record.currentA = Decimal.Round(Convert.ToDecimal(data.Current_mA), 2, MidpointRounding.AwayFromZero);

                                if (currentOk) break;
                            }
                            else
                            {
                                log.WriteLine(GetNow() + "Command '" + CurrentINA219Diagnosis.pyCmd + "' fail!");
                                mainForm.UpdateRichTextBox(station.id, "Command '" + CurrentINA219Diagnosis.pyCmd + "' fail!");
                                return TestResult.FAILURE;
                            }
                            Thread.Sleep(100);
                            i++;
                        }

                        if( i == retryCount)
                            return TestResult.FAILURE;
                    }
                    finally
                    {
                        Thread.Sleep(100);
                        station.dutCommand.StopSwTxCommand();
                    }
                }
                else
                {
                    log.WriteLine(GetNow() + "Send Tx fail!");
                    mainForm.UpdateRichTextBox(station.id, "Send Tx fail!");
                    return TestResult.FAILURE;
                }

                // test antenna B
                tx = new RtwTx()
                {
                    freq = GetWifiCurrentTestWithFreq(station.id),
                    bw = BW.B_40MHZ,
                    antPath = ANT_PATH.PATH_B,
                    rateID = RATE_ID.HTMCS7,
                    txMode = TX_MODE.PACKET_TX
                };

                if (station.dutCommand.StartSwTxCommand(tx, false)
                 && station.dutCommand.SendTxPowerCommand(tx.antPath, DAC))
                {
                    Thread.Sleep(500);
                    log.WriteLine(GetNow() + "Test B...");
                    log.WriteLine(GetNow() + "Freq:" + tx.freq + " BW:" + Wifi.bwDic[tx.bw] + " Rate:" + Wifi.rateIdDic[tx.rateID] + " Power DAC:" + DAC);
                    mainForm.UpdateRichTextBox(station.id, "Test B...");

                    try
                    {
                        diagnosis = new CurrentINA219Diagnosis(Convert.ToDouble(SystemConfig.CurrentBUpper), Convert.ToDouble(SystemConfig.CurrentBLower));
                        i = 0;
                        while (i < retryCount)
                        {
                            // test current
                            if (station.dutCommand.WaitFor(CurrentINA219Diagnosis.pyCmd, currentRegex, ref returnString, 1000))
                            {
                                INA219DataParser.INA219Data data;
                                bool currentOk = diagnosis.TestResult(returnString, out data);

                                string headerMsg = string.Join(",", new string[] { GetNow(), GetNow(), GetNow(), GetNow() });
                                string tailMsg = string.Join(",", new string[] { "", "", "", "(" + SystemConfig.CurrentBLower + "," + SystemConfig.CurrentBUpper + ")" + (currentOk ? " ---> PASS" : " ---> FAIL") });
                                log.WriteLine(GetNow() + "----------------------------------------");
                                log.Write(CurrentINA219Diagnosis.GetFormatResult(data, headerMsg, tailMsg));
                                log.WriteLine(GetNow() + "----------------------------------------");
                                mainForm.UpdateRichTextBox(station.id, returnString);

                                station.record.currentB = Decimal.Round(Convert.ToDecimal(data.Current_mA), 2, MidpointRounding.AwayFromZero);

                                if (currentOk) break;
                            }
                            else
                            {
                                log.WriteLine(GetNow() + "Command '" + CurrentINA219Diagnosis.pyCmd + "' fail!");
                                mainForm.UpdateRichTextBox(station.id, "Command '" + CurrentINA219Diagnosis.pyCmd + "' fail!");
                                return TestResult.FAILURE;
                            }
                            Thread.Sleep(100);
                            i++;
                        }
                         
                        if(i == retryCount)
                            return TestResult.FAILURE;
                    }
                    finally
                    {
                        Thread.Sleep(100);
                        station.dutCommand.StopSwTxCommand();
                    }
                }
                else
                {
                    log.WriteLine(GetNow() + "Send Tx fail!");
                    mainForm.UpdateRichTextBox(station.id, "Send Tx fail!");
                    return TestResult.FAILURE;
                }

                // test antenna A + B
                tx = new RtwTx()
                {
                    freq = GetWifiCurrentTestWithFreq(station.id),
                    bw = BW.B_40MHZ,
                    antPath = ANT_PATH.PATH_AB,
                    rateID = RATE_ID.HTMCS7,
                    txMode = TX_MODE.PACKET_TX
                };

                if (station.dutCommand.StartSwTxCommand(tx, false)
                 && station.dutCommand.SendTxPowerCommand(tx.antPath, DAC, DAC))
                {
                    Thread.Sleep(500);
                    log.WriteLine(GetNow() + "Test A+B...");
                    log.WriteLine(GetNow() + "Freq:" + tx.freq + " BW:" + Wifi.bwDic[tx.bw] + " Rate:" + Wifi.rateIdDic[tx.rateID] + " Power DAC:" + DAC);
                    mainForm.UpdateRichTextBox(station.id, "Test A+B...");

                    try
                    {
                        diagnosis = new CurrentINA219Diagnosis(Convert.ToDouble(SystemConfig.CurrentABUpper), Convert.ToDouble(SystemConfig.CurrentABLower));
                        i = 0;
                        while (i < retryCount)
                        {
                            // test current
                            if (station.dutCommand.WaitFor(CurrentINA219Diagnosis.pyCmd, currentRegex, ref returnString, 1000))
                            {
                                INA219DataParser.INA219Data data;
                                bool currentOk = diagnosis.TestResult(returnString, out data);

                                string headerMsg = string.Join(",", new string[] { GetNow(), GetNow(), GetNow(), GetNow() });
                                string tailMsg = string.Join(",", new string[] { "", "", "", "(" + SystemConfig.CurrentABLower + "," + SystemConfig.CurrentABUpper + ")" + (currentOk ? " ---> PASS" : " ---> FAIL") });
                                log.WriteLine(GetNow() + "----------------------------------------");
                                log.Write(CurrentINA219Diagnosis.GetFormatResult(data, headerMsg, tailMsg));
                                log.WriteLine(GetNow() + "----------------------------------------");
                                mainForm.UpdateRichTextBox(station.id, returnString);

                                station.record.currentAB = Decimal.Round(Convert.ToDecimal(data.Current_mA), 2, MidpointRounding.AwayFromZero);

                                if (currentOk) break;
                            }
                            else
                            {
                                log.WriteLine(GetNow() + "Command '" + CurrentINA219Diagnosis.pyCmd + "' fail!");
                                mainForm.UpdateRichTextBox(station.id, "Command '" + CurrentINA219Diagnosis.pyCmd + "' fail!");
                                return TestResult.FAILURE;
                            }
                            Thread.Sleep(100);
                            i++;
                        }

                        if (i == retryCount)
                            return TestResult.FAILURE;
                    }
                    finally
                    {
                        Thread.Sleep(100);
                        station.dutCommand.StopSwTxCommand();
                    }
                }
                else
                {
                    log.WriteLine(GetNow() + "Send Tx fail!");
                    mainForm.UpdateRichTextBox(station.id, "Send Tx fail!");
                    return TestResult.FAILURE;
                }

                //if (!station.dutCommand.StopMp())
                //{
                //    log.WriteLine(GetNow() + "Can't stop MP mode!");
                //    mainForm.UpdateRichTextBox(station.id, "Can't stop MP mode!");
                //}
            }
            else
            {
                log.WriteLine(GetNow() + "Can't start MP mode!");
                mainForm.UpdateRichTextBox(station.id, "Can't start MP mode!");
                return TestResult.FAILURE;
            }

            return TestResult.SUCCESS;
        }

        public TestResult TestBTCurrent(TestStatus status)
        {
            Form1 mainForm = Form1.GetMainForm();

            TestStation station = mpStation as TestStation;

            if (status == TestStatus.INIT)
            {
                mainForm.UpdateTestStage(station.id, TestStage.TEST_BT_CURRENT);
                log.WriteLine(GetNow() + (station.testNum + 1) + ".Test BT Current...");

                mainForm.UpdateRichTextBox(station.id, (station.testNum + 1) + ".Test BT Current...");

                return TestResult.SUCCESS;
            }

            if (status == TestStatus.END)
            {
                return TestResult.SUCCESS;
            }

            IProxyProcessor proxyProcessor = station.dutCommand.rtwProxyProcessor;

            int i = 0;
            int retryCount = 3;
            string returnString = "";

            CurrentINA219Diagnosis diagnosis;

            string wifiDriv = SystemConfig.DrivFile;
            string wifiDrivDirPath = SystemConfig.DrivDir;
            if (!wifiDrivDirPath.EndsWith("/"))
                wifiDrivDirPath += "/";

            // disable BT normal mode
            if (station.dutCommand.WaitFor("killall rtk_hciattach", userHeaderRegex, ref returnString, 3000)
             && station.dutCommand.WaitFor("killall bluetoothd", userHeaderRegex, ref returnString, 3000)
             && station.dutCommand.WaitFor("echo 0 > /sys/class/rfkill/rfkill0/state", userHeaderRegex, ref returnString, 2000)
             && station.dutCommand.WaitFor("echo 1 > /sys/class/rfkill/rfkill0/state", userHeaderRegex, ref returnString, 2000))
            {
                log.WriteLine(GetNow() + "Enable BT MP mode...");
                mainForm.UpdateRichTextBox(station.id, "Enable BT MP mode...");

                try
                {
                    if (station.dutCommand.WaitFor("rtlbtmp", "Bluetooth MP Test Tool Starting", 1500)
                     && station.dutCommand.WaitFor("enable uart:/dev/ttyS1", "enable[Success:0]", 3000))
                    {
                        log.WriteLine(GetNow() + "Enter tty console.");
                        mainForm.UpdateRichTextBox(station.id, "Enter tty console.");

                        // reset BT
                        proxyProcessor.Send("bt_mp_Exec 0", 100);

                        // disable power tracking
                        proxyProcessor.Send("bt_mp_SetParam 18,0,0", 100);
                        proxyProcessor.Send("bt_mp_Exec 42", 100);

                        // set ANT S0 (non-shared)
                        proxyProcessor.Send("bt_mp_SetParam 18,0", 100);
                        proxyProcessor.Send("bt_mp_Exec 40", 100);

                        // set Tx Gain K
                        proxyProcessor.Send("bt_mp_SetParam 18,1,0x06", 100);
                        proxyProcessor.Send("bt_mp_Exec 45", 100);

                        // set Tx Flatness K
                        proxyProcessor.Send("bt_mp_SetParam 18,1,0,0x32", 100); // 0x0032 
                        proxyProcessor.Send("bt_mp_Exec 46", 100);

                        // set Tx power
                        proxyProcessor.Send("bt_mp_SetParam 18,0x33,0x3a,0x3a,0x33,0x33", 100); // target 4dBm
                        proxyProcessor.Send("bt_mp_Exec 51", 100);

                        //Send Tx Packet
                        //1-CH; 2-Packet Type; 3-Payload type; 4-Tx packet count (0); 6-WhiteningCoeffValue(0x7f); 7-TxGainIndex(0); 11-HitTarget
                        if (station.dutCommand.WaitFor("bt_mp_SetParam 1," + (37 + station.id * 2) + ";2,0x05;3,0x07;4,0x00;6,0x7F;7,0x0;11,0x0000009e8b33", "bt_mp_SetParam[Success:0]", 2000)
                         && station.dutCommand.WaitFor("bt_mp_Exec 30", "bt_mp_Exec[Success:0]", 2000))
                        {
                            // leave BT mode because need to evaluate the current
                            proxyProcessor.Send("quit", 500);

                            log.WriteLine(GetNow() + "Test BT(Non-shared)...");
                            log.WriteLine(GetNow() + "CH:" + (37 + station.id * 2) + " GainK:0x06 FlatnessK:0x32 Target Power:" + BTTGPOWER);
                            mainForm.UpdateRichTextBox(station.id, "Test BT(Non-shared)...");

                            diagnosis = new CurrentINA219Diagnosis(Convert.ToDouble(SystemConfig.CurrentBtUpper), Convert.ToDouble(SystemConfig.CurrentBtLower));

                            while (i < retryCount)
                            {
                                // test current
                                if (station.dutCommand.WaitFor(CurrentINA219Diagnosis.pyCmd, currentRegex, ref returnString, 1000))
                                {
                                    INA219DataParser.INA219Data data;
                                    bool currentOk = diagnosis.TestResult(returnString, out data);

                                    string headerMsg = string.Join(",", new string[] { GetNow(), GetNow(), GetNow(), GetNow() });
                                    string tailMsg = string.Join(",", new string[] { "", "", "", "(" + SystemConfig.CurrentBtLower + "," + SystemConfig.CurrentBtUpper + ")" + (currentOk ? " ---> PASS" : " ---> FAIL") });
                                    log.WriteLine(GetNow() + "----------------------------------------");
                                    log.Write(CurrentINA219Diagnosis.GetFormatResult(data, headerMsg, tailMsg));
                                    log.WriteLine(GetNow() + "----------------------------------------");
                                    mainForm.UpdateRichTextBox(station.id, returnString);

                                    station.record.currentBt = Decimal.Round(Convert.ToDecimal(data.Current_mA), 2, MidpointRounding.AwayFromZero);

                                    if (currentOk) break;
                                }
                                else
                                {
                                    log.WriteLine(GetNow() + "Command '" + CurrentINA219Diagnosis.pyCmd + "' fail!");
                                    mainForm.UpdateRichTextBox(station.id, "Command '" + CurrentINA219Diagnosis.pyCmd + "' fail!");
                                    return TestResult.FAILURE;
                                }
                                Thread.Sleep(100);
                                i++;
                            }

                            if (i == retryCount)
                                return TestResult.FAILURE;

                            // stop Tx Packet by entering bt mode to disable it
                            if (station.dutCommand.WaitFor("rtlbtmp", "Bluetooth MP Test Tool Starting", 1500)
                             && station.dutCommand.WaitFor("enable uart:/dev/ttyS1", "enable[Success:0]", 3000))
                            {
                                log.WriteLine(GetNow() + "Disable BT mode...");
                                mainForm.UpdateRichTextBox(station.id, "Disable BT mode...");

                                if (!station.dutCommand.WaitFor("disable", "disable[Success:0]", 2000))
                                {
                                    log.WriteLine(GetNow() + "Can't disable BT mode!");
                                    mainForm.UpdateRichTextBox(station.id, "Can't disable BT mode!");
                                }
                            }
                            else
                            {
                                log.WriteLine(GetNow() + "Can't enable BT MP mode!");
                                mainForm.UpdateRichTextBox(station.id, "Can't enable BT MP mode!");
                            }
                        }
                        else
                        {
                            log.WriteLine(GetNow() + "Send Tx Packet Fail!");
                            mainForm.UpdateRichTextBox(station.id, "Send Tx Packet Fail!");
                            return TestResult.FAILURE;
                        }
                    }
                    else
                    {
                        log.WriteLine(GetNow() + "Can't enable BT MP mode!");
                        mainForm.UpdateRichTextBox(station.id, "Can't enable BT MP mode!");
                        return TestResult.FAILURE;
                    }
                }
                finally
                {
                    // leave BT mode
                    proxyProcessor.Send("quit", 500);
                }      
            }
            else
            {
                log.WriteLine(GetNow() + "Can't disable BT normal mode!");
                mainForm.UpdateRichTextBox(station.id, "Can't disable BT normal mode!");
                return TestResult.FAILURE;
            }

            return TestResult.SUCCESS;
        }

        public TestResult WriteMac(TestStatus status)
        {
            Form1 mainForm = Form1.GetMainForm();

            TestStation station = mpStation as TestStation;

            if (status == TestStatus.INIT)
            {
                mainForm.UpdateTestStage(station.id, TestStage.WRITE_MAC);
                log.WriteLine(GetNow() + (station.testNum + 1) + ".Write MAC...");

                mainForm.UpdateRichTextBox(station.id, (station.testNum + 1) + ".Write MAC...");

                return TestResult.SUCCESS;
            }

            if (status == TestStatus.END)
            {
                return TestResult.SUCCESS;
            }

            try
            {
                for (int i = 0; i < station.wlMac.Length; i++)
                {
                    wlMap.WriteMap(0x16A + i, station.wlMac[i]);
                }

                for (int i = 0; i < station.btMac.Length; i++)
                {
                    btMap.WriteMap(0x30 + station.btMac.Length - i - 1, station.btMac[i]);
                }

                string strWLMac = Utility.GetMacAddr(station.wlMac);
                string strBTMac = Utility.GetMacAddr(station.btMac);

                log.WriteLine(GetNow() + "WL MAC address:" + strWLMac);
                log.WriteLine(GetNow() + "BT MAC address:" + strBTMac);
                //mainForm.UpdateRichTextBox(station.id, "WL MAC address:" + strWLMac);
                //mainForm.UpdateRichTextBox(station.id, "BT MAC address:" + strBTMac);
            }
            catch (Exception ex)
            {
                log.WriteLine(ex.Message);
                mainForm.UpdateRichTextBox(station.id, ex.Message);
                return TestResult.FAILURE;
            }

            return TestResult.SUCCESS;
        }

        public TestResult WriteFakeMap(TestStatus status)
        {
            Form1 mainForm = Form1.GetMainForm();
            TestStation station = mpStation as TestStation;

            if (status == TestStatus.INIT)
            {
                mainForm.UpdateTestStage(station.id, TestStage.FLASHING);
                log.WriteLine(GetNow() + (station.testNum + 1) + ".Write Fake Map...");

                mainForm.UpdateRichTextBox(station.id, (station.testNum + 1) + ".Write Fake Map...");

                return TestResult.SUCCESS;
            }

            if (status == TestStatus.END)
            {
                return TestResult.SUCCESS;
            }

            //if (station.dutCommand.StartMp())
            //{
                // write all data to fake map
                int d0, d1;
#if WIFIONLY
                d0 = wlMap.efuse.GetLength(0);
                d1 = wlMap.efuse.GetLength(1);
                for (int i = 0; i < d0; i++)
                {
                    byte[] b = new byte[d1];
                    for (int j = 0; j < d1; j++)
                    {
                        b[j] = wlMap.efuse[i, j];
                    }
                    if (!station.dutCommand.WriteWLFakeMap(0x00 + d1 * i, b))
                    {
                        log.WriteLine(GetNow() + "Write wl fake error!");
                        mainForm.UpdateRichTextBox(station.id, "Write wl fake error!");
                        return TestResult.FAILURE;
                    }
                }
#endif
#if BTONLY
                d0 = btMap.efuse.GetLength(0);
                d1 = btMap.efuse.GetLength(1);
                for (int i = 0; i < d0; i++)
                {
                    byte[] b = new byte[d1];
                    for (int j = 0; j < d1; j++)
                    {
                        b[j] = btMap.efuse[i, j];
                    }
                    if (!station.dutCommand.WriteBTFakeMap(0x00 + d1 * i, b))
                    {
                        log.WriteLine(GetNow() + "Write bt fake error!");
                        mainForm.UpdateRichTextBox(station.id, "Write bt fake error!");
                        return TestResult.FAILURE;
                    }
                }
#endif
                /*
                //fake map init command
                rtwpriv wlan0 efuse_set btwfake,14,E11BFFFF5B01
                rtwpriv wlan0 efuse_set btwfake,30,4062B6000002
                //rtwpriv wlan0 efuse_set btwfake,7a,0FC0
                rtwpriv wlan0 efuse_set btwfake,1CE,777E
                //rtwpriv wlan0 efuse_set btwfake,1F4,A6800B00
                rtwpriv wlan0 efuse_set btwfake,278,00
                rtwpriv wlan0 efuse_set btwfake,280,383F3F3838
                rtwpriv wlan0 efuse_set btwfake,28c,6c
                */

                //int addr1 = 0x14, addr2 = 0x30, addr3 = 0x1CE, addr4 = 0x278, addr5 = 0x280, addr6 = 0x28C;
                //byte[] data1 = new byte[6];
                //byte[] data2 = new byte[6];
                //byte[] data3 = new byte[2];
                //byte data4;
                //byte[] data5 = new byte[5];
                //byte data6;

                //for(int i = 0; i < data1.Length; i++)
                //{
                //    data1[i] = btMap.ReadMap(addr1 + i);
                //}

                //for (int i = 0; i < data2.Length; i++)
                //{
                //    data2[i] = btMap.ReadMap(addr2 + i);
                //}

                //for (int i = 0; i < data3.Length; i++)
                //{
                //    data3[i] = btMap.ReadMap(addr3 + i);
                //}

                //data4 = btMap.ReadMap(addr4);

                //for (int i = 0; i < data5.Length; i++)
                //{
                //    data5[i] = btMap.ReadMap(addr5 + i);
                //}

                //data6 = btMap.ReadMap(addr6);

                //if (!(station.dutCommand.WriteBTFakeMap(addr1, data1)
                //   && station.dutCommand.WriteBTFakeMap(addr2, data2)
                //   && station.dutCommand.WriteBTFakeMap(addr3, data3)
                //   && station.dutCommand.WriteBTFakeMap(addr4, data4)
                //   && station.dutCommand.WriteBTFakeMap(addr5, data5)
                //   && station.dutCommand.WriteBTFakeMap(addr6, data6)))
                //{
                //    log.WriteLine(GetNow() + "Write bt fake error!");
                //    mainForm.UpdateRichTextBox(station.id, "Write bt fake error!");
                //    return TestResult.FAILURE;
                //}
            //}
            //else
            //{
            //    log.WriteLine(GetNow() + "Can't start MP mode!");
            //    mainForm.UpdateRichTextBox(station.id, "Can't start MP mode!");
            //    return TestResult.FAILURE;
            //}

            return TestResult.SUCCESS;
        }

        public TestResult WriteEfuse(TestStatus status)
        {
            Form1 mainForm = Form1.GetMainForm();
            TestStation station = mpStation as TestStation;

            if (status == TestStatus.INIT)
            {
                mainForm.UpdateTestStage(station.id, TestStage.FLASHING);
                log.WriteLine(GetNow() + (station.testNum + 1) + ".Flashing...");

                mainForm.UpdateRichTextBox(station.id, (station.testNum + 1) + ".Flashing...");

                return TestResult.SUCCESS;
            }

            if (status == TestStatus.END)
            {
                return TestResult.SUCCESS;
            }

            // enable BT normal mode for writing BT efuse
            //string returnString = "";
            //if (station.dutCommand.WaitFor("killall rtk_hciattach", userHeaderRegex, ref returnString, 2000)
            // && station.dutCommand.WaitFor("echo 0 > /sys/class/rfkill/rfkill0/state", userHeaderRegex, ref returnString, 1000)
            // && station.dutCommand.WaitFor("echo 1 > /sys/class/rfkill/rfkill0/state", userHeaderRegex, ref returnString, 1000)
            // && station.dutCommand.WaitFor("rtk_hciattach -n -s 115200 /dev/ttyS1 rtk_h5 &", "Realtek Bluetooth :Device setup complete", 10000, 1))
            //{
            //    station.dutCommand.WaitFor("\n", userHeaderRegex, ref returnString, 1000);

                // write fake map to efuse
#if WIFIONLY
                if (!station.dutCommand.WriteToWLEfuse())
                {
                    log.WriteLine(GetNow() + "Write wl efuse error!");
                    mainForm.UpdateRichTextBox(station.id, "Write wl efuse error!");
                    return TestResult.FAILURE;
                }
#endif
#if BTONLY
                if (!station.dutCommand.WriteToBTEfuse())
                {
                    log.WriteLine(GetNow() + "Write bt efuse error!");
                    mainForm.UpdateRichTextBox(station.id, "Write bt efuse error!");
                    return TestResult.FAILURE;
                }
#endif
                station.record.isEfuseWritten = true;
            //}
            //else
            //{
            //    log.WriteLine(GetNow() + "Can't BT normal mode!");
            //    mainForm.UpdateRichTextBox(station.id, "Can't BT normal mode!");
            //    return TestResult.FAILURE;
            //}

            if (!station.dutCommand.StopMp())
            {
                log.WriteLine(GetNow() + "Can't stop MP mode!");
                mainForm.UpdateRichTextBox(station.id, "Can't stop MP mode!");
            }

            return TestResult.SUCCESS;
        }

        public TestResult CheckEfuse(TestStatus status)
        {
            Form1 mainForm = Form1.GetMainForm();
            TestStation station = mpStation as TestStation;

            if (status == TestStatus.INIT)
            {
                mainForm.UpdateTestStage(station.id, TestStage.CHECKING);
                log.WriteLine(GetNow() + (station.testNum + 1) + ".Checking...");

                mainForm.UpdateRichTextBox(station.id, (station.testNum + 1) + ".Checking...");

                return TestResult.SUCCESS;
            }

            if (status == TestStatus.END)
            {
                return TestResult.SUCCESS;
            }

            string wlRealRawData = station.dutCommand.ReadFromWLEfuse();
            string btRealRawData = station.dutCommand.ReadFromBTEfuse();
            log.WriteLine(GetNow() + "Show Wifi & BT real map:");
            log.WriteLine(wlRealRawData + "\r\n" + btRealRawData);

            EfuseMap wlTempMap = new EfuseMap(EfuseMap.WLMAPSIZE);
            EfuseMap btTempMap = new EfuseMap(EfuseMap.BTMAPSIZE);

            //0x000	FF FF FF FF FF FF FF FF 	FF FF FF FF FF FF FF FF
            Regex efuseMapRegex = new Regex("0x\\w{2,3}\\s+(\\s*\\w{2}\\s*){16}");
            Regex base16Regex = new Regex("\\b\\w{2}\\b");

            int p = 0;
#if WIFIONLY
            foreach (Match m1 in efuseMapRegex.Matches(wlRealRawData))
            {
                foreach (Match m2 in base16Regex.Matches(m1.Value))
                {
                    wlTempMap.WriteMap(p, Convert.ToByte(m2.Value, 16));
                    p++;
                }
            }

            for (int i = 0; i < EfuseMap.WLMAPSIZE; i++)
            {
                if (wlTempMap.ReadMap(i) != wlMap.ReadMap(i))
                {
                    log.WriteLine(GetNow() + "Check Wifi Map Fail!");
                    mainForm.UpdateRichTextBox(station.id, "Check Wifi Map Fail!");
                    return TestResult.FAILURE;
                }
            }
#endif
#if BTONLY
            p = 0;
            foreach (Match m1 in efuseMapRegex.Matches(btRealRawData))
            {
                foreach (Match m2 in base16Regex.Matches(m1.Value))
                {
                    btTempMap.WriteMap(p, Convert.ToByte(m2.Value, 16));
                    p++;
                }
            }

            for (int i = 0; i < EfuseMap.BTMAPSIZE; i++)
            {
                if (btTempMap.ReadMap(i) != btMap.ReadMap(i))
                {
                    log.WriteLine(GetNow() + "Check BT Map Fail!");
                    mainForm.UpdateRichTextBox(station.id, "Check BT Map Fail!");
                    return TestResult.FAILURE;
                }
            }
#endif
            //int addr1 = 0x14, addr2 = 0x30, addr3 = 0x1CE, addr4 = 0x278, addr5 = 0x280, addr6 = 0x28C;
            //bool check = true;
            //for (int i = 0; i < 6; i++)
            //{
            //    if(btTempMap.ReadMap(addr1 + i) != btTempMap.ReadMap(addr1 + i))
            //        check = false;
            //}

            //for (int i = 0; i < 6; i++)
            //{
            //    if (btTempMap.ReadMap(addr2 + i) != btTempMap.ReadMap(addr2 + i))
            //        check = false;
            //}

            //for (int i = 0; i < 2; i++)
            //{
            //    if (btTempMap.ReadMap(addr3 + i) != btTempMap.ReadMap(addr3 + i))
            //        check = false;
            //}

            //if (btTempMap.ReadMap(addr4) != btTempMap.ReadMap(addr4))
            //    check = false;

            //for (int i = 0; i < 5; i++)
            //{
            //    if (btTempMap.ReadMap(addr5 + i) != btTempMap.ReadMap(addr5 + i))
            //        check = false;
            //}

            //if (btTempMap.ReadMap(addr6) != btTempMap.ReadMap(addr6))
            //    check = false;

            //if (!check)
            //{
            //    log.WriteLine(GetNow() + "Check BT Map Fail!");
            //    mainForm.UpdateRichTextBox(station.id, "Check BT Map Fail!");
            //    return TestResult.FAILURE;
            //}
            return TestResult.SUCCESS;
        }
    }
}
