#define KEYWORDTRIGGERTEST
//#define OFFLINE
using RTKModule;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MapFlasher
{
    public partial class Form1 : Form
    {
        public enum TestStage
        {
            READY,
            INIT,
            DEINIT,
            TEST_WL_CURRENT,
            TEST_BT_CURRENT,
            WRITE_MAC,
            FLASHING,
            CHECKING,
            FAIL,
            SUCCESS
        }

        public delegate void StartTestHandler();
        public delegate void EnableTimerHandler(int dutID, bool enable);
        public delegate void UpdateRichTextBoxHandler(int dutID, string appendText, bool bEndOfLine);
        public delegate void ClearRichTextBoxHandler(int dutID);
        public delegate void UpdateProgressBarHandler(int dutID, int percentProgress);
        public delegate void UpdateTestStageHandler(int dutID, TestStage stage);

        public static Form1 mainForm;

        public IOT03DbService iOT03DbService;

        public readonly string saveLogDir = Application.StartupPath + "\\Log";
        public readonly string configPath = Application.StartupPath + "\\Setup\\config.txt";
        public readonly string testLimitPath = Application.StartupPath + "\\Setup\\test_limit.txt";
        public readonly string adbPath = Application.StartupPath + "\\platform-tools\\adb.exe";
        public readonly string cmdLogDir = Application.StartupPath + "\\Cmd_Log";
        public const int OPENCOMPORTID = 1; // open comport if application starts and close it if application stops
        public const string WAITBUTTONTRIGGEREDCMD = "/home/pi/test/keypad_detect.sh &";
        public const string TRIGGERKETWORD = "tsc54542235"; // trigger application to test

        public TestStation[] station = new TestStation[SystemConfig.STATIONCOUNT];

        public ILog[] cmdLog = new Log[SystemConfig.STATIONCOUNT];

        public RichTextBox[] richTextBoxControls;
        public ProgressBar[] progressBarControls;
        public TextBox[] uiStatusControls;
        public Label[] timerLabels;
        public int[] timeCounters;
        public System.Windows.Forms.Timer[] timerControls;
        public bool[] bStartTrigger;

        public int curDutID = 1;

        public const string DATETIMEFORMAT = "[yyyy-MM-dd HH:mm:ss.fff] ";

        private readonly string swVersion;

        public void EnableTimerControl(int dutID, bool enable)
        {
            if (this.InvokeRequired)
            {
                EnableTimerHandler handler = new EnableTimerHandler(EnableTimerControl);
                this.Invoke(handler, dutID, enable);
                return;
            }

            timerControls[dutID - 1].Enabled = enable;
        }

        public void UpdateRichTextBox(int dutID, string appendText, bool bEndOfLine = true)
        {
            if (this.InvokeRequired)
            {
                UpdateRichTextBoxHandler handler = new UpdateRichTextBoxHandler(UpdateRichTextBox);
                this.Invoke(handler, dutID, appendText, bEndOfLine);
                return;
            }

            richTextBoxControls[dutID - 1].Text += appendText + (bEndOfLine ? "\r\n" : "");
            richTextBoxControls[dutID - 1].SelectionStart = richTextBoxControls[dutID - 1].Text.Length - 1;
            richTextBoxControls[dutID - 1].ScrollToCaret();
        }

        public void CleanRichTextBox(int dutID)
        {
            if (this.InvokeRequired)
            {
                ClearRichTextBoxHandler handler = new ClearRichTextBoxHandler(CleanRichTextBox);
                this.Invoke(handler, dutID);
                return;
            }

            richTextBoxControls[dutID - 1].Text = "";
        }

        public void UpdateProgressBar(int dutID, int percentProgress)
        {
            if (this.InvokeRequired)
            {
                UpdateProgressBarHandler handler = new UpdateProgressBarHandler(UpdateProgressBar);
                this.Invoke(handler, dutID, percentProgress);
                return;
            }

            progressBarControls[dutID - 1].Value = percentProgress;
        }

        public void UpdateTestStage(int dutID, TestStage testStage)
        {
            if (this.InvokeRequired)
            {
                UpdateTestStageHandler handler = new UpdateTestStageHandler(UpdateTestStage);
                this.Invoke(handler, dutID, testStage);
                return;
            }

            string strTestStage = "";
            switch (testStage)
            {
                case TestStage.READY:
                    uiStatusControls[dutID - 1].BackColor = Color.Green;
                    strTestStage = "Ready";
                    break;
                case TestStage.INIT:
                    uiStatusControls[dutID - 1].BackColor = Color.Orange;
                    strTestStage = "Init";
                    break;
                case TestStage.DEINIT:
                    uiStatusControls[dutID - 1].BackColor = Color.Orange;
                    strTestStage = "Deinit";
                    break;
                case TestStage.TEST_WL_CURRENT:
                    uiStatusControls[dutID - 1].BackColor = Color.Purple;
                    strTestStage = "Test WL";
                    break;
                case TestStage.TEST_BT_CURRENT:
                    uiStatusControls[dutID - 1].BackColor = Color.Purple;
                    strTestStage = "Test BT";
                    break;
                case TestStage.WRITE_MAC:
                    uiStatusControls[dutID - 1].BackColor = Color.Yellow;
                    strTestStage = "Write Mac";
                    break;
                case TestStage.FLASHING:
                    uiStatusControls[dutID - 1].BackColor = Color.Yellow;
                    strTestStage = "Flashing";
                    break;
                case TestStage.CHECKING:
                    uiStatusControls[dutID - 1].BackColor = Color.DarkRed;
                    strTestStage = "Checking";
                    break;
                case TestStage.FAIL:
                    uiStatusControls[dutID - 1].BackColor = Color.Red;
                    strTestStage = "Fail";
                    break;
                case TestStage.SUCCESS:
                    uiStatusControls[dutID - 1].BackColor = Color.Blue;
                    strTestStage = "Pass";
                    break;
            }

            uiStatusControls[dutID - 1].Text = strTestStage;
        }

        public static Form1 GetMainForm()
        {
            return mainForm;
        }

        public Control GetFormControl(string controlName)
        {
            foreach (Control c in Controls)
            {
                if (c.Name == controlName)
                    return c;
            }
            return null;
        }

        public Form1()
        {
            InitializeComponent();
            swVersion = typeof(Form1).Assembly.FullName.Split(',')[0];
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SystemConfig.Load();

            mainForm = this;
            mainForm.Text = swVersion;

            richTextBoxControls = new RichTextBox[]
            {
                richTextBox1, richTextBox2, richTextBox3, richTextBox4
            };

            progressBarControls = new ProgressBar[]
            {
                progressBar1, progressBar2, progressBar3, progressBar4
            };

            uiStatusControls = new TextBox[]
            {
                textBox2, textBox4, textBox5, textBox6
            };

            timerLabels = new Label[]
            {
                label29, label30, label31, label32
            };

            timeCounters = new int[]
            {
                0 , 0 , 0 , 0
            };

            timerControls = new System.Windows.Forms.Timer[]
            {
                timer1, timer2, timer3, timer4
            };

            bStartTrigger = new bool[]
            {
                false, false, false, false
            };

            for (int i = 0; i < station.Length; i++)
                station[i] = new TestStation(i + 1);

            for (int i = 0; i < uiStatusControls.Length; i++)
                UpdateTestStage(station[i].id, TestStage.READY);

            for (int i = 0; i < timerLabels.Length; i++)
                timerLabels[i].Text = "0s";

            for (int i = 0; i < timerControls.Length; i++)
            {
                timerControls[i].Interval = 1000;
                timerControls[i].Enabled = false;
            }

            string wlMapPath = Application.StartupPath + "\\Map\\wl.map";
            string btMapPath = Application.StartupPath + "\\Map\\bt.map";
            if (File.Exists(wlMapPath))
                textBox1.Text = wlMapPath;
            if (File.Exists(btMapPath))
                textBox8.Text = btMapPath;
#if false
            try
            {
                for(int i = 0; i < STATIONCOUNT; i++)
                {
                    station[i].dutComPort = new ComPort(connectionInterfaces[i], 115200, 8, StopBits.One, Parity.None);
                    station[i].dutComPort.ReceiveSerialMessageEvent += ProcessSerialReceive;
                    station[i].dutComPort.Open();
                }

                station[OPENCOMPORTID - 1].dutCommand = new RtwCommand(RtwProxyCreator.CreatProxy(station[OPENCOMPORTID - 1].dutComPort));
                string stringReturn;
                while (true)
                {
                    stringReturn = "";
                    if (station[OPENCOMPORTID - 1].dutCommand.WaitFor("\n", TestTask.userHeaderRegex, ref stringReturn, 1000, 1))
                        break;
                    Thread.Sleep(2000);
                }
                // ready to receive button trigger from jig
                station[OPENCOMPORTID - 1].dutComPort.SendRtwCommand(WAITBUTTONTRIGGEREDCMD);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Close();
            }
#endif      
            for (int i = 0; i < SystemConfig.STATIONCOUNT; i++)
            {
                if ((SystemConfig.Platform == "1319" || SystemConfig.Platform == "Bananapi" || SystemConfig.Platform == "Raspi")
                  && SystemConfig.ConnectionInterfaces[i].StartsWith("COM"))
                {
                    int baudRate = 115200;
                    if (SystemConfig.Platform == "1319")
                        baudRate = 460800;
                    try
                    {
                        ComPort comport = new ComPort(SystemConfig.ConnectionInterfaces[i], baudRate, 8, StopBits.One, Parity.None);
                        comport.ReceiveSerialMessageEvent += ProcessSerialReceive;
                        comport.Open();

                        station[i].dutComPort = comport;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        this.Close();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Pls check the platform or interface connection!");
                    this.Close();
                    return;
                }

                // login bananapi
                station[i].dutCommand = new RtwCommand(RtwProxyCreator.CreatProxy(station[i].dutComPort));
                string returnString = "";
                if (station[i].dutCommand.WaitFor("\n", TestTask.userHeaderRegex, ref returnString, 2000, 1))
                {
                    // have login
                }
                else if (station[i].dutCommand.WaitFor("root", "Password:", 3000, 1)
                      && station[i].dutCommand.WaitFor("bananapi", TestTask.userHeaderRegex, ref returnString, 3000, 1))
                {
                    // login successfully
                }
                else
                {
                    MessageBox.Show("Login fail!\r\nPls restart the application.");
                    this.Close();
                    return;
                }
            }

            textBox7.Text = curDutID.ToString();
#if !OFFLINE
            // get db user and password from local setting
            string localConnectionString = ConfigurationManager.ConnectionStrings["IOT03DbContext"].ToString();
            string user = localConnectionString.Split(';').Where(p => p.Contains("User Id")).First().Split('=')[1];
            string password = localConnectionString.Split(';').Where(p => p.Contains("Password")).First().Split('=')[1];
            // db service
            iOT03DbService = new IOT03DbService(SystemConfig.DbIp, user, password);
#endif
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            textBox3.Focus();
        }

        public void InterfaceClose(int dutID)
        {
            if (SystemConfig.ConnectionInterfaces[dutID - 1].StartsWith("COM"))
            {
                if (station[dutID - 1].dutComPort != null)
                {
                    station[dutID - 1].dutComPort.Close();
                    station[dutID - 1].dutComPort = null;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
#if false
            for (int curDutID = 1; curDutID <= 4; curDutID++)
            {
                // get S/N,MAC from database

                station[curDutID - 1].InitStation(curDutID);
                string sn = station[curDutID - 1].sn = curDutID.ToString();

                // mac
                string curWLMacAddr = "";
                string endWLMacAddr = "";
                string curBTMacAddr = "";
                string endBTMacAddr = "";
                lock (configPath)
                {
                    string[] config = File.ReadAllLines(configPath);
                    switch (curDutID)
                    {
                        case 1:
                            curWLMacAddr = config.Where(p => p.Split('=')[0] == "WL_MAC_1").First().Split('=')[1].Trim();
                            curBTMacAddr = config.Where(p => p.Split('=')[0] == "BT_MAC_1").First().Split('=')[1].Trim();
                            break;
                        case 2:
                            curWLMacAddr = config.Where(p => p.Split('=')[0] == "WL_MAC_2").First().Split('=')[1].Trim();
                            curBTMacAddr = config.Where(p => p.Split('=')[0] == "BT_MAC_2").First().Split('=')[1].Trim();
                            break;
                        case 3:
                            curWLMacAddr = config.Where(p => p.Split('=')[0] == "WL_MAC_3").First().Split('=')[1].Trim();
                            curBTMacAddr = config.Where(p => p.Split('=')[0] == "BT_MAC_3").First().Split('=')[1].Trim();
                            break;
                        case 4:
                            curWLMacAddr = config.Where(p => p.Split('=')[0] == "WL_MAC_4").First().Split('=')[1].Trim();
                            curBTMacAddr = config.Where(p => p.Split('=')[0] == "BT_MAC_4").First().Split('=')[1].Trim();
                            break;
                    }
                    endWLMacAddr = config.Where(p => p.Split('=')[0] == "WL_EMAC").First().Split('=')[1].Trim();
                    endBTMacAddr = config.Where(p => p.Split('=')[0] == "BT_EMAC").First().Split('=')[1].Trim();
                }

                if (curWLMacAddr.Equals(endWLMacAddr) || curBTMacAddr.Equals(endBTMacAddr))
                {
                    MessageBox.Show("Pls reassign the new mac address");
                    return;
                }

                station[curDutID - 1].wlMac = Utility.GetMacAddr(curWLMacAddr);
                station[curDutID - 1].btMac = Utility.GetMacAddr(curBTMacAddr);

                switch (station[curDutID - 1].id)
                {
                    case 1:
                        label4.Text = sn;
                        label5.Text = curWLMacAddr;
                        label25.Text = curBTMacAddr;
                        break;
                    case 2:
                        label9.Text = sn;
                        label7.Text = curWLMacAddr;
                        label26.Text = curBTMacAddr;
                        break;
                    case 3:
                        label13.Text = sn;
                        label11.Text = curWLMacAddr;
                        label27.Text = curBTMacAddr;
                        break;
                    case 4:
                        label17.Text = sn;
                        label15.Text = curWLMacAddr;
                        label28.Text = curBTMacAddr;
                        break;
                }

                // init rtw command
                cmdLog[curDutID - 1] = new Log();
                RtwLogHandledInterceptor rtwLogHandledInterceptor = new RtwLogHandledInterceptor(cmdLog[curDutID - 1]);
                // get proxy processor instance and set
                //if (connectionInterface == "ADB") 
                ////rtwProxyProcessor[curDutID - 1] = RtwProxyCreator.CreatProxy(adb[curDutID - 1], rtwLogHandledInterceptor);
                //else
                if (connectionInterfaces[curDutID - 1].StartsWith("COM"))
                    station[curDutID - 1].dutCommand = new RtwCommand(RtwProxyCreator.CreatProxy(station[curDutID - 1].dutComPort, rtwLogHandledInterceptor));

                Task task = new Task(TestLoop, station[curDutID - 1]);
                task.Start();

                // start timer from 0s
                timeCounters[curDutID - 1] = 0;
                timerLabels[curDutID - 1].Text = timeCounters[curDutID - 1] + "s";
                timerControls[curDutID - 1].Enabled = true;

                CleanRichTextBox(curDutID);
            }
#else
            try
            {
                if (string.IsNullOrEmpty(textBox1.Text.Trim()) || string.IsNullOrEmpty(textBox8.Text.Trim()))
                {
                    MessageBox.Show("Pls select the map for flash!");
                    return;
                }

                string sn = textBox3.Text.Trim();
                string strCurDutID = textBox7.Text.Trim();

                if (string.IsNullOrEmpty(sn))
                {
                    MessageBox.Show("Pls type the SN!");
                    return;
                }

                if (string.IsNullOrEmpty(strCurDutID))
                {
                    MessageBox.Show("Pls type the DUT ID!");
                    return;
                }

                curDutID = int.Parse(strCurDutID);

                if (curDutID > SystemConfig.STATIONCOUNT)
                {
                    MessageBox.Show("Pls type the DUT ID(1-" + SystemConfig.STATIONCOUNT + ")!");
                    return;
                }

                if (!CheckSn(sn))
                    return;

                // check if the comport of the station is being used
                if (station[curDutID - 1].isUsed)
                {
                    MessageBox.Show("DUT " + curDutID + " is being used...");
                    return;
                }

                station[curDutID - 1].InitStation(curDutID);
                station[curDutID - 1].sn = sn;

#if OFFLINE
                // mac
                string curWLMacAddr = "";
                string endWLMacAddr = "";
                string curBTMacAddr = "";
                string endBTMacAddr = "";
                lock (configPath)
                {
                    string[] config = File.ReadAllLines(configPath);
                    switch (curDutID)
                    {
                        case 1:
                            curWLMacAddr = config.Where(p => p.Split('=')[0] == "WL_MAC_1").First().Split('=')[1].Trim();
                            curBTMacAddr = config.Where(p => p.Split('=')[0] == "BT_MAC_1").First().Split('=')[1].Trim();
                            break;
                        case 2:
                            curWLMacAddr = config.Where(p => p.Split('=')[0] == "WL_MAC_2").First().Split('=')[1].Trim();
                            curBTMacAddr = config.Where(p => p.Split('=')[0] == "BT_MAC_2").First().Split('=')[1].Trim();
                            break;
                        case 3:
                            curWLMacAddr = config.Where(p => p.Split('=')[0] == "WL_MAC_3").First().Split('=')[1].Trim();
                            curBTMacAddr = config.Where(p => p.Split('=')[0] == "BT_MAC_3").First().Split('=')[1].Trim();
                            break;
                        case 4:
                            curWLMacAddr = config.Where(p => p.Split('=')[0] == "WL_MAC_4").First().Split('=')[1].Trim();
                            curBTMacAddr = config.Where(p => p.Split('=')[0] == "BT_MAC_4").First().Split('=')[1].Trim();
                            break;
                    }
                    endWLMacAddr = config.Where(p => p.Split('=')[0] == "WL_EMAC").First().Split('=')[1].Trim();
                    endBTMacAddr = config.Where(p => p.Split('=')[0] == "BT_EMAC").First().Split('=')[1].Trim();
                }

                if (curWLMacAddr.Equals(endWLMacAddr) || curBTMacAddr.Equals(endBTMacAddr))
                {
                    MessageBox.Show("Pls reassign the new mac address");
                    return;
                }

                switch (station[curDutID - 1].id)
                {
                    case 1:
                        label4.Text = sn;
                        label5.Text = curWLMacAddr;
                        label25.Text = curBTMacAddr;
                        break;
                    case 2:
                        label9.Text = sn;
                        label7.Text = curWLMacAddr;
                        label26.Text = curBTMacAddr;
                        break;
                    case 3:
                        label13.Text = sn;
                        label11.Text = curWLMacAddr;
                        label27.Text = curBTMacAddr;
                        break;
                    case 4:
                        label17.Text = sn;
                        label15.Text = curWLMacAddr;
                        label28.Text = curBTMacAddr;
                        break;
                }

                station[curDutID - 1].wlMac = Utility.GetMacAddrWithColon(curWLMacAddr);
                station[curDutID - 1].btMac = Utility.GetMacAddrWithColon(curBTMacAddr);
#else               
                string[] mac = iOT03DbService.GetMac(station[curDutID - 1].sn);
                if (mac != null && mac.Length < 2 && (string.IsNullOrEmpty(mac[0]) || string.IsNullOrEmpty(mac[1])))
                {
                    MessageBox.Show("Pls check the dedicated MAC in db!");
                    return;
                }

                string curWLMacAddr = mac[0];
                string curBTMacAddr = mac[1];
                switch (station[curDutID - 1].id)
                {
                    case 1:
                        label4.Text = sn;
                        label5.Text = curWLMacAddr;
                        label25.Text = curBTMacAddr;
                        break;
                    case 2:
                        label9.Text = sn;
                        label7.Text = curWLMacAddr;
                        label26.Text = curBTMacAddr;
                        break;
                    case 3:
                        label13.Text = sn;
                        label11.Text = curWLMacAddr;
                        label27.Text = curBTMacAddr;
                        break;
                    case 4:
                        label17.Text = sn;
                        label15.Text = curWLMacAddr;
                        label28.Text = curBTMacAddr;
                        break;
                }

                station[curDutID - 1].wlMac = Utility.GetMacAddr(curWLMacAddr);
                station[curDutID - 1].btMac = Utility.GetMacAddr(curBTMacAddr);
#endif           
                // init rtw command
                cmdLog[curDutID - 1] = new Log();
                RtwLogHandledInterceptor rtwLogHandledInterceptor = new RtwLogHandledInterceptor(cmdLog[curDutID - 1]);
                // get proxy processor instance and set
                //if (connectionInterface == "ADB") 
                ////rtwProxyProcessor[curDutID - 1] = RtwProxyCreator.CreatProxy(adb[curDutID - 1], rtwLogHandledInterceptor);
                //else
                if (SystemConfig.ConnectionInterfaces[curDutID - 1].StartsWith("COM"))
                    station[curDutID - 1].dutCommand = new RtwCommand(RtwProxyCreator.CreatProxy(station[curDutID - 1].dutComPort, rtwLogHandledInterceptor));

#if KEYWORDTRIGGERTEST
                // ready to receive button trigger from jig
                station[OPENCOMPORTID - 1].dutCommand.rtwProxyProcessor.Send(WAITBUTTONTRIGGEREDCMD);
#endif

                station[curDutID - 1].isUsed = true;
#if !KEYWORDTRIGGERTEST
                station[curDutID - 1].start = DateTime.Now;
#endif
                Task task = new Task(TestLoop, station[curDutID - 1]);
                task.Start();

                // start timer from 0s
                timeCounters[curDutID - 1] = 0;
                timerLabels[curDutID - 1].Text = timeCounters[curDutID - 1] + "s";
                timerControls[curDutID - 1].Enabled = true;

                CleanRichTextBox(curDutID);
                // update dut ID
                textBox7.Text = (curDutID = (++curDutID) % (SystemConfig.STATIONCOUNT + 1) == 0 ? 1 : curDutID).ToString();
            }
            finally
            {
                textBox3.Clear();
                textBox3.Focus();
            }
#endif
            }

        private void button2_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Text (*.map)|*.map";
            openFileDialog1.FileName = "";
            openFileDialog1.InitialDirectory = Application.StartupPath + "\\Map";
            openFileDialog1.Multiselect = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            openFileDialog2.Filter = "Text (*.map)|*.map";
            openFileDialog2.FileName = "";
            openFileDialog2.InitialDirectory = Application.StartupPath + "\\Map";
            openFileDialog2.Multiselect = false;

            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                textBox8.Text = openFileDialog2.FileName;
            }
        }

        public bool CheckSn(string sn)
        {
            if (string.IsNullOrEmpty(sn))
            {
                MessageBox.Show("The S/N shouldn't left blank!");
                return false;
            }

            for (int i = 0; i < station.Length; i++)
            {
                if (station[i].sn == sn)
                {
                    MessageBox.Show("The S/N is being used!");
                    return false;
                }
            }
#if !OFFLINE
            if (!iOT03DbService.CheckModuleSnIsExist(sn))
            {
                MessageBox.Show("The S/N isn't in server!\r\nPls check.");
                return false;
            }

            if (iOT03DbService.CheckModuleIsWritten(sn))
            {
                MessageBox.Show("This module had been written!\r\nPls go to next station.");
                return false;
            }
#endif
            return true;
        }

        public void TestLoop(object obj)
        {
            if (!(obj is TestStation))
                return;

            TestStation station = obj as TestStation;
#if KEYWORDTRIGGERTEST
            // wait key pad triggered
            while (!bStartTrigger[station.id - 1])
            {
                Application.DoEvents();
                Thread.Sleep(100);
            }
            // start test
            station.start = DateTime.Now;
#endif
            ILog testLog = new Log();
            TestTask testTask = new TestTask(station, testLog);
            TestItems testItems = new TestItems();
            testItems.AddTasks(testTask);

            List<Func<TestStatus, TestResult>> testFuncs = testItems.testFuncList;

            if (testFuncs != null && testFuncs.Count <= 0)
                return;

            //init
            UpdateProgressBar(station.id, 0);

            while (true)
            {
                Thread.Sleep(50);

                station.testStatus = TestStatus.INIT;

                if(station.testNum != testFuncs.Count - 1)
                {
                    station.finalTestResult = testFuncs[station.testNum](station.testStatus);
                    if (station.finalTestResult == TestResult.FAILURE)
                    {
                        station.testStatus = TestStatus.END;
                        testFuncs[station.testNum](station.testStatus);
                        goto LAST;
                    }
                }
                else
                    testFuncs[station.testNum](station.testStatus);

                station.testStatus = TestStatus.START;

                if (station.testNum != testFuncs.Count - 1)
                {
                    station.finalTestResult = testFuncs[station.testNum](station.testStatus);
                    if (station.finalTestResult == TestResult.SUCCESS)
                    {
                        station.testStatus = TestStatus.END;
                        testFuncs[station.testNum](station.testStatus);
                    }
                    else
                    {
                        station.testStatus = TestStatus.END;
                        testFuncs[station.testNum](station.testStatus);
                        goto LAST;
                    }
                }
                else
                    testFuncs[station.testNum](station.testStatus);

                station.testNum++;
                LAST:
                if(station.testNum != testFuncs.Count && station.finalTestResult == TestResult.FAILURE)
                {
                    station.testNum = testFuncs.Count - 1;
                }

                UpdateProgressBar(station.id, station.testNum * 100 / testFuncs.Count);
                if (station.testNum < testFuncs.Count) // end ??
                    continue;
                else
                    break;
            }
            Done(station, testLog);
        }

        public void Done(TestStation station, ILog log)
        {
            UpdateRichTextBox(station.id, "Done.");
            if (station.finalTestResult == TestResult.SUCCESS)
            {
#if OFFLINE
                // update next mac
                string curWLMacAddr = Utility.GetMacAddrWithColon(station.wlMac);
                string curBTMacAddr = Utility.GetMacAddrWithColon(station.btMac);
                lock (configPath)
                {
                    string[] lines = File.ReadAllLines(configPath);
                    string[] wlAddr = lines.Where(line => line.StartsWith("WL_SMAC") || line.StartsWith("WL_EMAC")).ToArray();
                    string startWLMacAddr = wlAddr[0].Split('=')[1];
                    string endWLMacAddr = wlAddr[1].Split('=')[1];
                    string[] btAddr = lines.Where(line => line.StartsWith("BT_SMAC") || line.StartsWith("BT_EMAC")).ToArray();
                    string startBTMacAddr = btAddr[0].Split('=')[1];
                    string endBTMacAddr = btAddr[1].Split('=')[1];
                    // write back to config
                    int index = -1;
                    switch (station.id)
                    {
                        case 1:
                            index = Array.FindIndex(lines, s => s.StartsWith("WL_MAC_1"));
                            lines[index] = lines[index].Replace("WL_MAC_1=" + curWLMacAddr, "WL_MAC_1=" + startWLMacAddr);
                            index = Array.FindIndex(lines, s => s.StartsWith("BT_MAC_1"));
                            lines[index] = lines[index].Replace("BT_MAC_1=" + curBTMacAddr, "BT_MAC_1=" + startBTMacAddr);
                            break;
                        case 2:
                            index = Array.FindIndex(lines, s => s.StartsWith("WL_MAC_2"));
                            lines[index] = lines[index].Replace("WL_MAC_2=" + curWLMacAddr, "WL_MAC_2=" + startWLMacAddr);
                            index = Array.FindIndex(lines, s => s.StartsWith("BT_MAC_2"));
                            lines[index] = lines[index].Replace("BT_MAC_2=" + curBTMacAddr, "BT_MAC_2=" + startBTMacAddr);
                            break;
                        case 3:
                            index = Array.FindIndex(lines, s => s.StartsWith("WL_MAC_3"));
                            lines[index] = lines[index].Replace("WL_MAC_3=" + curWLMacAddr, "WL_MAC_3=" + startWLMacAddr);
                            index = Array.FindIndex(lines, s => s.StartsWith("BT_MAC_3"));
                            lines[index] = lines[index].Replace("BT_MAC_3=" + curBTMacAddr, "BT_MAC_3=" + startBTMacAddr);
                            break;
                        case 4:
                            index = Array.FindIndex(lines, s => s.StartsWith("WL_MAC_4"));
                            lines[index] = lines[index].Replace("WL_MAC_4=" + curWLMacAddr, "WL_MAC_4=" + startWLMacAddr);
                            index = Array.FindIndex(lines, s => s.StartsWith("BT_MAC_4"));
                            lines[index] = lines[index].Replace("BT_MAC_4=" + curBTMacAddr, "BT_MAC_4=" + startBTMacAddr);
                            break;
                    }

                    string nextMacAddr = "";
                    if (!startWLMacAddr.Equals(endWLMacAddr))
                    {
                        nextMacAddr = Utility.CalcNextMacWithColon(startWLMacAddr);
                        index = Array.FindIndex(lines, s => s.StartsWith("WL_SMAC"));
                        lines[index] = lines[index].Replace("WL_SMAC=" + startWLMacAddr, "WL_SMAC=" + nextMacAddr);
                    }

                    if (!startBTMacAddr.Equals(endBTMacAddr))
                    {
                        nextMacAddr = Utility.CalcNextMacWithColon(startBTMacAddr);
                        index = Array.FindIndex(lines, s => s.StartsWith("BT_SMAC"));
                        lines[index] = lines[index].Replace("BT_SMAC=" + startBTMacAddr, "BT_SMAC=" + nextMacAddr);
                    }

                    File.WriteAllLines(configPath, lines);
                }
#endif
                station.record.result = true;
                log.WriteLine(DateTime.Now.ToString(DATETIMEFORMAT) + "Result:PASS");
                UpdateTestStage(station.id, TestStage.SUCCESS);
            }
            else
            {
                station.record.result = false;
                log.WriteLine(DateTime.Now.ToString(DATETIMEFORMAT) + "Result:FAIL");
                UpdateTestStage(station.id, TestStage.FAIL);
            }

            station.end = DateTime.Now;
            log.WriteLine("\r\n======================================================");
            log.WriteLine("Time:" + station.end.Subtract(station.start).TotalSeconds.ToString("F2") + "s");
            log.WriteLine("DUT ID:" + station.id);

            string dir = cmdLogDir + "\\DUT" + station.id;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            cmdLog[station.id - 1].Save(dir + "\\" + station.sn + "_" + station.end.ToString("yyyyMMdd_HHmmss") + ".txt");

            dir = saveLogDir + "\\DUT" + station.id;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            log.Save(dir + "\\" + station.sn + "_" + station.end.ToString("yyyyMMdd_HHmmss") + ".txt");
#if !OFFLINE
            // access database
            try
            {
                T1Record t1Record = new T1Record();
                t1Record.DateTime = new DateTime(station.start.Year, station.start.Month, station.start.Day, station.start.Hour, station.start.Minute, station.start.Second);
                t1Record.Duration = Decimal.Round(Convert.ToDecimal(station.end.Subtract(station.start).TotalSeconds), 2, MidpointRounding.AwayFromZero);
                t1Record.Result = station.record.result;
                t1Record.CurrentNone = station.record.currentNone;
                t1Record.CurrentA = station.record.currentA;
                t1Record.CurrentB = station.record.currentB;
                t1Record.CurrentAB = station.record.currentAB;
                t1Record.CurrentBt = station.record.currentBt;
                t1Record.SnRefId = iOT03DbService.GetSnId(station.sn);
                iOT03DbService.UpdateIOT03WrittenState(station.sn, station.record.isEfuseWritten);
                iOT03DbService.InsertT1Record(t1Record);
            }
            catch(Exception ex)
            {
                UpdateRichTextBox(station.id, ex.Message);
            }
#endif
            // if station id is OPENCOMPORTID, not close the interface
#if KEYWORDTRIGGERTEST
            for(int i = 0; i < SystemConfig.STATIONCOUNT; i++)
                bStartTrigger[i] = false;
#endif
            EnableTimerControl(station.id, false);
            station.isUsed = false;
            station.sn = string.Empty;
        }

        private void ProcessSerialReceive(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = sender as SerialPort;
            string data = sp.ReadExisting();
            if (string.IsNullOrEmpty(data))
                return;

            int dutID = -1;
            for (int i = 0; i < station.Length; i++)
            {
                if (station[i].dutComPort != null && sp.PortName.Equals(station[i].dutComPort.GetPortName()))
                {
                    dutID = i + 1;
                    break;
                }
            }

            if (dutID == -1)
                return;
#if KEYWORDTRIGGERTEST
            if (dutID == OPENCOMPORTID)
            {
                if (data.Contains(TRIGGERKETWORD))
                {
                    for (int i = 0; i < bStartTrigger.Length; i++)
                        bStartTrigger[i] = true;
                    return;
                }
            }
#endif
            if (station[dutID - 1].dutCommand != null)
            {
                IProxyProcessor rtwProxyProcessor = station[dutID - 1].dutCommand.rtwProxyProcessor;
                if (rtwProxyProcessor != null)
                    rtwProxyProcessor.Receive(data);
            }       
        }

        private void ProcessExit(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            for (int i = 0; i < station.Length; i++)
            {
                if (station[i].isUsed)
                {
                    MessageBox.Show("Pls wait for testing completely!");
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            for (int i = 0; i < station.Length; i++)
            {
                InterfaceClose(station[i].id);
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            System.Windows.Forms.Timer timer = sender as System.Windows.Forms.Timer;
            int index = GetTimerControlsIndex(timer);
            timerLabels[index].Text = ++timeCounters[index] + "s";
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            System.Windows.Forms.Timer timer = sender as System.Windows.Forms.Timer;
            int index = GetTimerControlsIndex(timer);
            timerLabels[index].Text = ++timeCounters[index] + "s";
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            System.Windows.Forms.Timer timer = sender as System.Windows.Forms.Timer;
            int index = GetTimerControlsIndex(timer);
            timerLabels[index].Text = ++timeCounters[index] + "s";
        }

        private void timer4_Tick(object sender, EventArgs e)
        {
            System.Windows.Forms.Timer timer = sender as System.Windows.Forms.Timer;
            int index = GetTimerControlsIndex(timer);
            timerLabels[index].Text = ++timeCounters[index] + "s";
        }

        public int GetTimerControlsIndex(System.Windows.Forms.Timer timer)
        {
            int index = 0;
            foreach (System.Windows.Forms.Timer t in timerControls)
            {
                if (timerControls[index].GetHashCode() == timer.GetHashCode()) break;
                index++;
            }
            return index;
        }

        private void textBox7_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                if (int.TryParse(textBox7.Text, out curDutID))
                {
                    textBox3.Clear();
                    textBox3.Focus();
                }    
                return;
            }

            if (!(e.KeyChar >= '0' && e.KeyChar <= '9' || e.KeyChar == (char)Keys.Back))
            {
                e.Handled = true;
                return;
            }
            e.Handled = false;
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if(textBox3.Text.Length >= 13)
            {
                button1.PerformClick();
                //textBox7.Focus();
            }
        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {
            if(textBox7.Text.Length >= 1)
            {
                //button1.PerformClick();
                //textBox3.Clear();
                //textBox7.Clear();
                //textBox3.Focus();
            }
        }
    }
}
