using System;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Intel.STHI.ApseCore.Common;
using Intel.STHI.ApseCore.Common.Enums;
using Intel.STHI.ApseCore.Common.Fusion;
using Sttd;
using System.Diagnostics;
using MCP2210;
using itufflogger;
using static Intel.STHI.CommonHelperClasses.Interops;
using System.Runtime.InteropServices;
using CrcNamespace;
using FT;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using static FT.pdb1;
using TotalPhase;
using System.Text;

namespace _ATX1
{
    class Program
    {
        [DllImport("kernel32", SetLastError = true)]
        private static extern bool GetSystemTime(out SYSTEMTIME systemTime);
        struct SYSTEMTIME { internal short wYear; internal short wMonth; internal short wDayOfWeek; internal short wDay; internal short wHour; internal short wMinute; internal short wSecond; internal short wMilliseconds; }

        enum BLT_OFFSET : byte { MANID = 0x00, ASM = 0x10, SN = 0x20, MFDATE = 0x30, INDATE = 0x37, CYCCNT = 0x40, GBLCNT = 0x47, FW1 = 0x50, FW2 = 0x54, FW3 = 0x58, BID = 0x60, HW = 0x69, FW4 = 0x70, FW5 = 0x71, CUST = 0x72, };
        public const int BLT_RFDBOFFSET = 0x0D;//13
        public const int BLT_CRCOFFSET = 0x0E;//14
        public const int BLT_SIZE = 0x80;//256

        static byte[] BLT_WR = Enumerable.Repeat((byte)0xFF, BLT_SIZE).ToArray();
        static byte[] BLT_RD = Enumerable.Repeat((byte)0x00, BLT_SIZE).ToArray();

        private static Logger obj;
        private static int Bin = 10639999; //default to unknown failure.
        private const int PassBin = 01000000;
        private const string ErrMsg = "ErrorMessage"; //use this to add final error messages to ITUFF.

        // initialize Aardvark
        const int AARDVARK_PORT_NUMBER = 0;
        const int AARDVARK_BIT_RATE = 100;
        const int AARDVARK_BUS_TIMEOUT = (AARDVARK_BIT_RATE / 2) + 50;
        static int handle = 0; // Aardvark handle

        static void Main(string[] args)
        {
            bool[] gpio_en = new bool[9] { false, false, false, false, true, false, false, false, false };
            Log.Info("Console Argument is : " + args[0]);
            switch (args[0].ToLower())
            {

                case "readblt":
                    try
                    {
                        obj = new Logger();
                        obj.StartLot("C:\\STHI\\ATX1\\ITUFFTemplate.xml");
                        obj.LoadBinList("C:\\STHI\\ATX1\\binlist.xml");
                        obj.StartDut();

                        obj.StartTest("Read BLT Test");
                        readBLTTest();
                        obj.EndTest(true);

                        obj.SetDutResult(PassBin);
                        obj.EndDut();
                        obj.EndLot();
                        Log.Info("Success : Reading BLT for ATX1 Fab B completed.");

                    }
                    catch (Exception ex)
                    {
                        obj.SetDutResult(10630101);
                        obj.EndDut();
                        obj.EndLot();
                        Log.Error("Disaster : Test failed. " + ex.Message);
                        //retval = 1;
                        throw new Exception($"Read blt failed with {ex.Message}");
                    }

                    break;

                case "writeblt":
                    try
                    {
                        obj = new Logger();
                        obj.StartLot("C:\\STHI\\ATX1\\ITUFFTemplate.xml");
                        obj.LoadBinList("C:\\STHI\\ATX1\\binlist.xml");
                        obj.StartDut();

                        obj.StartTest("Write BLT Test");
                        writeBLTTest();
                        obj.EndTest(true);

                        obj.SetDutResult(PassBin);
                        obj.EndDut();
                        obj.EndLot();
                        Log.Info("Success : Writing BLT for ATX1 Fab B completed.");
                    }
                    catch (Exception ex)
                    {
                        obj.SetDutResult(10630102);
                        obj.EndDut();
                        obj.EndLot();
                        Log.Error("Disaster : Test failed. " + ex.Message);
                        throw new Exception($"Write blt failed with {ex.Message}");
                    }

                    break;

                case "vidpid":
                    obj = new Logger();
                    obj.StartLot("C:\\STHI\\ATX1\\ITUFFTemplate.xml");
                    obj.LoadBinList("C:\\STHI\\ATX1\\binlist.xml");
                    obj.StartDut();

                    obj.StartTest("Program VID/PID Test");
                    Log.Info("Starting VID/PID programming for the MPDU ATX1 Fab B");

                    // Checking whether the board has POR VID/PID. if not, program it
                    uint mcp2210_VID = 0x04D8; // VID for Microchip Technology Inc. 0x04D8
                    uint mcp2210_PID = 0x00DE; // PID for MCP2210 0X00DE

                    IUsbToSpiDevice device = new UsbToSpiDevice();
                    device.Connect();

                    bool isConnected = false; // Connection status variable for MCP2210

                    DevIO UsbSpi = new DevIO(mcp2210_VID, mcp2210_PID);
                    bool porVidPid = true;

                    isConnected = UsbSpi.Settings.GetConnectionStatus();

                    if (!isConnected)
                    {
                        UsbSpi = new DevIO(0x8087, 0x0BE1); // todo: change this after confirm the value
                        isConnected = UsbSpi.Settings.GetConnectionStatus();
                        porVidPid = false;
                        Log.Info("Done : Board has correct POR VID/PID. Wont be re-programmed.");

                        obj.EndTest(true);
                        obj.SetDutResult(PassBin);
                        obj.EndDut();
                        obj.EndLot();

                        if (!isConnected)
                            throw new Exception("Device not connected with either 048d/00de or 8087/0BE1."); //same as above
                    }

                    try
                    {
                        if (porVidPid)
                        {
                            Log.Info("Board needs VID/PID programming ...");

                            INonVolatileRam vram = device.NonVolatileRam;

                            Log.Info("Programming POR Intel VID/PID : 8087/0BE1"); // todo: confirm the VID/PID value for ATX1
                            UsbKeyPowerSettings keypower1 = new UsbKeyPowerSettings();

                            keypower1.RemoteWakeUpCapable = false;
                            keypower1.RequestedCurrent = 100;//mA
                            keypower1.HostPowered = true;
                            keypower1.VID = 0x8087;
                            keypower1.PID = 0x0BE1; // same as above
                            vram.WriteUsbSettings(keypower1);
                            Log.Info("Done programming VID/PID.");
                            Thread.Sleep(1000);

                            FT.pdb1.restartUSB(FT.pdb1.findMPDUPort());//automated for finding the usb port
                            obj.EndTest(true);

                            obj.SetDutResult(PassBin);
                            obj.EndDut();
                            obj.EndLot();
                        }

                    }
                    catch (Exception ex)
                    {
                        Log.Error("Disaster : VID/PID programming failed.");
                        Bin = 10630103;
                        Log.Error(ex.Message);
                        obj.SetDutResult(Bin);
                        obj.EndDut();
                        obj.EndLot();
                        FT.pdb1.copytoHost();
                        throw new Exception($"VID/PID programming failed with {ex.Message}");
                    }


                    break;

                case "pson":
                    obj = new Logger();
                    obj.StartLot("C:\\STHI\\ATX1\\ITUFFTemplate.xml");
                    obj.LoadBinList("C:\\STHI\\ATX1\\binlist.xml");
                    obj.StartDut();

                    obj.StartTest("PSON Test");

                    Log.Info("Starting DVM test for GPIO controlled PS_ON Signal -> Disabled");
                    gpio_en = new bool[9] { false, false, true, false, false, false, false, false, false };
                    FT.pdb1.setGPIO(gpio_en);
                    Thread.Sleep(3000);
                    FT.pdb1.setGPIO_nvram(gpio_en);
                    Thread.Sleep(3000);

                    FT.pdb1.SetUpWCF();

                    Log.Info("Reading 20V output to verify PS_ON is disabled");
                    try
                    {
                        FT.pdb1.read_20V(0.0);
                    }
                    catch (Exception ex)
                    {
                        FT.pdb1.powerOffCRPS();
                        Bin = 10630202;
                        Log.Error("Disaster : Disable PS_ON test failed.");
                        Log.Error(ex.Message);
                        obj.SetDutResult(Bin);
                        obj.EndDut();
                        obj.EndLot();
                        FT.pdb1.copytoHost();
                        throw new Exception($"PSON Disable test failed with {ex.Message}");
                    }

                    Log.Info("Starting DVM test for GPIO controlled PS_ON Signal -> Enabled");
                    gpio_en = new bool[9] { false, false, false, false, true, false, false, false, false };
                    FT.pdb1.setGPIO(gpio_en);
                    FT.pdb1.setGPIO_nvram(gpio_en);

                    Thread.Sleep(5000);

                    Log.Info("Reading 20V output to verify PS_ON is enabled");
                    try
                    {
                        FT.pdb1.read_20V(10.0);
                        Log.Info("sukses : PS_ON Enable/Disable test completed.");
                        obj.EndTest(true);

                        obj.SetDutResult(PassBin);
                        obj.EndDut();
                        obj.EndLot();
                    }
                    catch (Exception ex)
                    {
                        FT.pdb1.powerOffCRPS();
                        Bin = 10630201;
                        Log.Error("Disaster : Enable PS_ON test failed.");
                        Log.Error(ex.Message);
                        obj.SetDutResult(Bin);
                        obj.EndDut();
                        obj.EndLot();
                        FT.pdb1.copytoHost();
                        throw new Exception($"PSON Enable test failed with {ex.Message}");
                    }

                    break;

                case "20v_test":
                    obj = new Logger();
                    obj.StartLot("C:\\STHI\\ATX1\\ITUFFTemplate.xml");
                    obj.LoadBinList("C:\\STHI\\ATX1\\binlist.xml");
                    obj.StartDut();

                    obj.StartTest("20V Test");

                    Log.Info("Starting DVM test for GPIO-based 20V connectors - disabled");
                    gpio_en = new bool[9] { true, false, true, false, true, false, false, false, false };
                    FT.pdb1.setGPIO(gpio_en);
                    Thread.Sleep(5000);

                    FT.pdb1.SetUpWCF();

                    Log.Info("Reading 20V output to verify output is disabled");
                    try
                    {
                        FT.pdb1.read_20V(0.0);
                    }
                    catch (Exception ex)
                    {
                        FT.pdb1.powerOffCRPS();
                        Bin = 10630203;
                        Log.Error("Disaster: Disable 20v power rail using GPIO failed.");
                        Log.Error(ex.Message);
                        obj.SetDutResult(Bin);
                        obj.EndDut();
                        obj.EndLot();
                        FT.pdb1.copytoHost();
                        throw new Exception($"20V Disable test failed with {ex.Message}");
                    }

                    Log.Info("Starting DVM test for GPIO-based 20V connectors - enabled");
                    gpio_en = new bool[9] { false, false, false, false, true, false, false, false, false };
                    FT.pdb1.setGPIO(gpio_en);
                    Thread.Sleep(5000);

                    Log.Info("Reading 20V output to verify output is enabled");
                    try
                    {
                        FT.pdb1.read_20V(10.0);
                        Log.Info("sukses2 : 20V Enable/Disable test completed.");
                        obj.EndTest(true);

                        obj.SetDutResult(PassBin);
                        obj.EndDut();
                        obj.EndLot();
                    }
                    catch (Exception ex)
                    {
                        FT.pdb1.powerOffCRPS();
                        Bin = 10630204;
                        Log.Error("Disaster: Enable 20v power rail using GPIO failed.");
                        Log.Error(ex.Message);
                        obj.SetDutResult(Bin);
                        obj.EndDut();
                        obj.EndLot();
                        FT.pdb1.copytoHost();
                        throw new Exception($"20V Enable test failed with {ex.Message}");
                    }

                    break;

                case "pwrok_aux":
                    obj = new Logger();
                    obj.StartLot("C:\\STHI\\ATX1\\ITUFFTemplate.xml");
                    obj.LoadBinList("C:\\STHI\\ATX1\\binlist.xml");
                    obj.StartDut();

                    obj.StartTest("Power OK and 12V Aux Test");

                    Log.Info("Starting DVM test for 12V Aux and PWR_OK Signals >> Connected to 2x2 Connectors");

                    string[] DIOchannel_12V = new string[6] { DO_Channel.DO_0, DO_Channel.DO_1, DO_Channel.DO_2, DO_Channel.DO_3, DO_Channel.DO_4,
                DO_Channel.DO_5};

                    DIOState[] ADMMapping = new DIOState[6] { DIOState.LOW, DIOState.LOW, DIOState.LOW,
                                                      DIOState.LOW,DIOState.LOW,DIOState.LOW};

                    var auxChannels = new string[3] { "0", "1", "4" }; // 0,1 = 12V | 4 = PWR_OK

                    FT.pdb1.SetUpWCF();

                    for (int j = 0; j < 6; j++)
                    {
                        Log.Info("Setting DIO " + j + " to " + ADMMapping[j].ToString());
                        STDIO.SetDIO(DIOchannel_12V[j], (byte)ADMMapping[j]);
                    }
                    Thread.Sleep(3000);

                    try
                    {
                        double expected = 0;
                        double tolerance = 0;

                        for (int index = 0; index < auxChannels.Length; index++)
                        {
                            var DVM = STDIO.ReadCalculatedDVM(auxChannels[index].ToString());
                            Log.Info("DVM for Channel " + $"{auxChannels[index]}" + " is : " + DVM);
                            Log.Info("\n");

                            if (index == 2) // PWR_OK
                            {
                                expected = 3.1;
                                tolerance = 0.3;

                                if (DVM > expected + tolerance || DVM < expected - tolerance)
                                {
                                    throw new Exception($"DVM for channel 4 failed." + " Expected : " + expected + " Current -> " + DVM);
                                }
                            }
                            else // 12V Aux
                            {
                                expected = 12.0;
                                tolerance = 1.2;

                                if (DVM > expected + tolerance || DVM < expected - tolerance)
                                {
                                    throw new Exception($"DVM for channel {index} failed." + " Expected : " + expected + " Current -> " + DVM);
                                }

                            }
                        }
                        Log.Info("sukses3 : Power_OK and 12 Aux voltage rail test completed.");
                        obj.EndTest(true);

                        obj.SetDutResult(PassBin);
                        obj.EndDut();
                        obj.EndLot();
                    }
                    catch (Exception ex)
                    {
                        powerOffCRPS();
                        Bin = 10630207;
                        Log.Info("\n");
                        Log.Error("Disaster : 12V AUX and/or PWROK test failed");
                        Log.Error(ex.Message);
                        obj.SetDutResult(Bin);
                        obj.EndDut();
                        obj.EndLot();
                        copytoHost();
                        throw new Exception($"{ex.Message}");
                    }


                    break;

                case "20v_12v":
                    obj = new Logger();
                    obj.StartLot("C:\\STHI\\ATX1\\ITUFFTemplate.xml");
                    obj.LoadBinList("C:\\STHI\\ATX1\\binlist.xml");
                    obj.StartDut();

                    obj.StartTest("12V and 20V Test");

                    Log.Info("Starting DVM test for MPDU 12VO ,12V CPU and 20V connectors");
                    DIOchannel_12V = new string[6] { DO_Channel.DO_0, DO_Channel.DO_1, DO_Channel.DO_2, DO_Channel.DO_3, DO_Channel.DO_4,
                DO_Channel.DO_5};
                    DIOState[] FanMapping = new DIOState[6] { DIOState.HIGH, DIOState.HIGH, DIOState.HIGH,
                                                      DIOState.HIGH,DIOState.HIGH,DIOState.HIGH};

                    string[] fanChannels = new string[10] { "0", "1", "2", "3", "4", "5", "6", "7", "9", "10" };
                    FT.pdb1.SetUpWCF();

                    for (int j = 0; j < 6; j++)
                    {
                        Log.Info("Setting DIO " + j + " to " + FanMapping[j].ToString());
                        FT.pdb1.STDIO.SetDIO(DIOchannel_12V[j], (byte)FanMapping[j]);
                    }
                    Thread.Sleep(3000);

                    //Set range and fail out of range values
                    var expectedVolt = 6.0; // voltage divider -> So 12V = 6.0V
                    var toleranceV = 0.5;
                    try
                    {
                        for (int k = 0; k < fanChannels.Length; k++)
                        {
                            var DVM = 0.00;

                            if (k > 7)
                            {
                                DVM = STDIO.ReadCalculatedDVM(fanChannels[k]);
                                Log.Info("DVM for Channel " + $"{fanChannels[k]}" + " is : " + DVM);

                                if (DVM > 10 + toleranceV || DVM < 10 - toleranceV)
                                {
                                    throw new Exception($"DVM for channel {fanChannels[k]} failed." + " Expected : " + 10 + " Current -> " + DVM);
                                }
                            }
                            else
                            {
                                DVM = STDIO.ReadCalculatedDVM(fanChannels[k]);
                                Log.Info("DVM for Channel " + $"{fanChannels[k]}" + " is : " + DVM);

                                if (DVM > expectedVolt + toleranceV || DVM < expectedVolt - toleranceV)
                                {
                                    throw new Exception($"DVM for channel {fanChannels[k]} failed." + " Expected : " + expectedVolt + " Current -> " + DVM);
                                }
                            }
                        }

                        Log.Info("sukses4 : 20V and 12V power rails tests completed.");
                        obj.EndTest(true);

                        obj.SetDutResult(PassBin);
                        obj.EndDut();
                        obj.EndLot();
                    }

                    catch (Exception ex)
                    {
                        powerOffCRPS();
                        Bin = 10630208;
                        Log.Error("Disaster : 12V CPU/12VO/20V connected to BPD Fan Header failed");
                        Log.Error(ex.Message);
                        obj.SetDutResult(Bin);
                        obj.EndDut();
                        obj.EndLot();
                        copytoHost();
                        throw new Exception($"20V and 12V power rails test failed with {ex.Message}");
                    }


                    break;

                case "ext_fan_tach":
                    obj = new Logger();
                    obj.StartLot("C:\\STHI\\ATX1\\ITUFFTemplate.xml");
                    obj.LoadBinList("C:\\STHI\\ATX1\\binlist.xml");
                    obj.StartDut();

                    obj.StartTest("External Fan Tach Test");
                    Log.Info("Starting external Fan Tach reading test ...");
                    try
                    {
                        Process cmd = new Process();
                        cmd.StartInfo.FileName = "C:\\STHI\\ATX1\\ReadTach\\gpio1_sample.exe";
                        cmd.StartInfo.Arguments = @" count 10";
                        cmd.StartInfo.UseShellExecute = false;
                        cmd.StartInfo.RedirectStandardOutput = true;
                        cmd.StartInfo.RedirectStandardError = true;
                        cmd.StartInfo.RedirectStandardInput = true;
                        cmd.Start();
                        cmd.WaitForExit();

                        Log.Info("printing output from gpio1_sample.exe...");
                        while (!cmd.StandardOutput.EndOfStream)
                        {
                            string data = cmd.StandardOutput.ReadLine();
                            Log.Info(data);
                            // TODO:need to process line with Hz
                            if (data.Contains("Frequency"))
                            {
                                //Log.Info("Found the line with frequency : " + data);
                                //Log.Info("Frequency is : " + data.Substring(11));

                                string pattern = @"-?\d+\.?\d*";
                                MatchCollection matches = Regex.Matches(data, pattern);

                                foreach (Match match in matches)
                                {
                                    if (double.TryParse(match.Value, out double number))
                                    {
                                        //Log.Info(number.ToString());
                                        if (number < 367)
                                        {
                                            throw new Exception("Fan frequency is less than 367 Hz.");
                                        }
                                        else
                                        {
                                            Log.Info("External fan frequency is : " + number.ToString() + " Hz.");
                                        }
                                    }
                                }
                                break;
                            }
                        }
                        Log.Info("sukses5 : Ext fan tach reading test completed.");
                        obj.EndTest(true);

                        obj.SetDutResult(PassBin);
                        obj.EndDut();
                        obj.EndLot();
                    }
                    catch (Exception ex)
                    {
                        powerOffCRPS();
                        Bin = 10630209;
                        Log.Error("Disaster : External Fan Tach Reading failed.");
                        Log.Error(ex.Message);
                        obj.SetDutResult(Bin);
                        obj.EndDut();
                        obj.EndLot();
                        copytoHost();
                        throw new Exception($"External fan tach reading test failed with {ex.Message}");

                    }

                    break;

                case "gpio_aux":
                    obj = new Logger();
                    obj.StartLot("C:\\STHI\\ATX1\\ITUFFTemplate.xml");
                    obj.LoadBinList("C:\\STHI\\ATX1\\binlist.xml");
                    obj.StartDut();

                    obj.StartTest("GPIO AUX Test");
                    Log.Info("Starting DVM test for GPIO-based 12v AUX power rails - disabled");
                    gpio_en = new bool[9] { false, true, false, true, true, false, false, false, false };
                    FT.pdb1.setGPIO(gpio_en);
                    Thread.Sleep(5000);

                    FT.pdb1.SetUpWCF();

                    Log.Info("Reading 12V Aux output to verify output is disabled");
                    try
                    {
                        FT.pdb1.read_12vAUX(0.0);
                    }
                    catch (Exception ex)
                    {
                        FT.pdb1.powerOffCRPS();
                        Bin = 10630206;
                        Log.Error("Disaster: Disable 12v aux power rail using GPIO failed.");
                        Log.Error(ex.Message);
                        obj.SetDutResult(Bin);
                        obj.EndDut();
                        obj.EndLot();
                        FT.pdb1.copytoHost();
                        throw new Exception($"12v Aux Disable test failed with {ex.Message}");
                    }

                    Log.Info("Starting DVM test for GPIO-based 12v AUX power rails - enabled");
                    gpio_en = new bool[9] { false, false, false, false, true, false, false, false, false };
                    FT.pdb1.setGPIO(gpio_en);
                    Thread.Sleep(5000);

                    Log.Info("Reading 12V Aux output to verify output is enabled");
                    try
                    {
                        FT.pdb1.read_12vAUX(12.0);
                        Log.Info("sukses6 : 12V Aux disable/enable test completed.");
                        obj.EndTest(true);

                        obj.SetDutResult(PassBin);
                        obj.EndDut();
                        obj.EndLot();
                    }
                    catch (Exception ex)
                    {
                        FT.pdb1.powerOffCRPS();
                        Bin = 10630205;
                        Log.Error("Disaster: Enable 12V Aux power rail using GPIO failed.");
                        Log.Error(ex.Message);
                        obj.SetDutResult(Bin);
                        obj.EndDut();
                        obj.EndLot();
                        FT.pdb1.copytoHost();
                        throw new Exception($"12V Aux disable/enable test failed with {ex.Message}");
                    }

                    break;

                case "pmbus":
                    obj = new Logger();
                    obj.StartLot("C:\\STHI\\ATX1\\ITUFFTemplate.xml");
                    obj.LoadBinList("C:\\STHI\\ATX1\\binlist.xml");
                    obj.StartDut();

                    obj.StartTest("PMBUS Communication Test");

                    Log.Info("Starting PMBUS communication test - Write and Read");

                    try
                    {
                        // Aardvark handle initialization
                        Initialize_Aardvark();

                        // Aardvark I2C settings
                        setupI2C();

                        // do master write
                        writePMBUS();

                        // master read
                        string result = readPMBUS();
                        string reference_data = "PSSF162205A"; //PSSF162205A

                        if (result.Equals(reference_data))
                        {
                            Log.Info("sukses7 : PMBUS test completed.");

                            AardvarkApi.aa_close(handle);
                            obj.EndTest(true);

                            obj.SetDutResult(PassBin);
                            obj.EndDut();
                            obj.EndLot();
                        }
                        else 
                        {
                            throw new Exception($"PMBUS read output -> Expected : {reference_data} -- Current {result}. Differennt version/model of CRPS is being used.");                       
                        
                        }
                    }
                    catch (Exception ex) 
                    {
                        AardvarkApi.aa_close(handle);
                        FT.pdb1.powerOffCRPS();
                        Bin = 10630210;
                        Log.Error("Disaster: PMBUS Test Failed.");
                        Log.Error(ex.Message);
                        obj.SetDutResult(Bin);
                        obj.EndDut();
                        obj.EndLot();
                        FT.pdb1.copytoHost();
                        throw new Exception($"PMBUS test failed with {ex.Message}");

                    }                   


                    break;

                case "default_config":
                    Log.Info("Reset GPIO to POR config with - GPIO 4 - High and 20V/12V power rails are enabled");
                    gpio_en = new bool[9] { false, false, false, false, true, false, false, false, false };
                    
                    FT.pdb1.setGPIO_nvram(gpio_en);//nvram
                    Thread.Sleep(3000);

                    break;

                case "atx1_ft":

                    try
                    {
                        Log.Info("########################################################################################");
                        Log.Info($"Starting FT for MPDU ATX1 Fab B ...");

                        Bin = pdb1.pdb1ft();

                        if (Bin != PassBin)
                        {
                            throw new Exception($"Test failed with Bin : {Bin}");
                        }
                        Log.Info("#########################################################################################");
                        Log.Info("\n");
                        Log.Info("\n");
                        Thread.Sleep(1000);// sleep for 3s 

                    }
                    catch (Exception ex)
                    {
                        Log.Error("Disaster : Test failed. " + ex.Message);
                        Log.Info("#########################################################################################");
                    }

                    break;

                case "loop_ft":

                    int loop = 0;
                    bool failedResult = false;

                    while (loop < 1 && failedResult == false)
                    {
                        loop += 1;

                        try
                        {
                            Log.Info("########################################################################################");
                            //Log.Info($"Starting FT loop for MPDU ATX1 Fab B ...  Loop = {loop}");
                            Log.Info($"Starting FT for ATX1 Fab B ...");

                            Bin = pdb1.pdb1ft();

                            if (Bin != PassBin)
                            {
                                throw new Exception($"Test failed with Bin : {Bin}");
                            }
                            Log.Info("#########################################################################################");
                            Log.Info("\n");
                            Log.Info("\n");
                            Log.Info("\n");
                            Thread.Sleep(3000);// sleep for 3s 

                        }
                        catch (Exception ex)
                        {
                            failedResult = true;
                            Log.Error("Disaster : Test failed. " + ex.Message);
                            Log.Info("#########################################################################################");
                        }

                    }

                    if (!failedResult)
                    {
                        Log.Info($"Bagus - FT for MPDU ATX1 Fab B Completed");
                    }


                    break;


                case "fan_tach":
                    try
                    {
                        Process cmd = new Process();
                        cmd.StartInfo.FileName = "C:\\STHI\\ATX1\\ReadTach\\gpio1_sample.exe";
                        cmd.StartInfo.Arguments = @" count 10";
                        cmd.StartInfo.UseShellExecute = false;
                        cmd.StartInfo.RedirectStandardOutput = true;
                        cmd.StartInfo.RedirectStandardError = true;
                        cmd.StartInfo.RedirectStandardInput = true;
                        cmd.Start();
                        cmd.WaitForExit();

                        Log.Info("printing output from gpio1_sample.exe...");
                        while (!cmd.StandardOutput.EndOfStream)
                        {
                            string data = cmd.StandardOutput.ReadLine();
                            Log.Info(data);
                            // TODO:need to process line with Hz
                            if (data.Contains("Frequency")) 
                            {
                                Log.Info("Found the line with frequency : " + data);
                                Log.Info("Frequency is : " +  data.Substring(11));

                                string pattern = @"-?\d+\.?\d*";
                                MatchCollection matches = Regex.Matches(data, pattern);

                                foreach (Match match in matches)
                                {
                                    if (double.TryParse(match.Value, out double number))
                                    {
                                        Log.Info(number.ToString());
                                    }
                                }
                                break;
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Log.Error("Disaster : Failed to read fan RPM");
                        Log.Error(ex.Message);                                          
                    }

                    break;

                case "pwrok":

                    Log.Info("Starting GPIO 5 - PWROK Read Test");
                    try
                    {
                        string vals = FT.pdb1.gpioVal();
                        Log.Info($"gpio vals : {Convert.ToInt32(vals, 2)}");
                        int decVal = Convert.ToInt32(vals, 2);

                        if (decVal == 48)
                        {
                            Log.Info("GPIO 5 - PWROK is HIGH as expected");
                        }
                        else
                        {
                            throw new Exception("GPIO 5 - PWROK is expected to be HIGH but it is LOW");
                        }

                    }
                    catch (Exception ex)
                    {                    
                        Log.Error("Disaster : GPIO 5 - PWROK read test failed");
                        Log.Error(ex.Message);
                    }

                    break;

                case "findmpdu":
                    try
                    {
                        Process cmd = new Process();
                        cmd.StartInfo.FileName = "C:\\STHI\\ATX1\\RestartUsbPort\\RestartUsbPort.exe";
                        cmd.StartInfo.Arguments = @" -l";
                        cmd.StartInfo.UseShellExecute = false;
                        cmd.StartInfo.RedirectStandardOutput = true;
                        cmd.StartInfo.RedirectStandardError = true;
                        cmd.StartInfo.RedirectStandardInput = true;
                        cmd.Start();
                        cmd.WaitForExit();

                        Log.Info("printing output from Restart USB Port...");
                        string output = null;
                        while (!cmd.StandardOutput.EndOfStream)
                        {
                            string currentLine = cmd.StandardOutput.ReadLine();
                            //Log.Info(currentLine);
                            output += currentLine;                           

                        }
                        string targetDeviceId = @"USB\VID_04D8&PID_00DE"; // default Microchip VID/PID
                        string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (string line in lines) 
                        {
                            if (line.Contains(targetDeviceId)) 
                            {
                                Log.Info($"{line}");
                                Log.Info("found the line");
                                int index = line.IndexOf(targetDeviceId);
                                Log.Info($"index : {index}");

                                string lineOut = line.Substring(index, index + 150);
                                string startMarker = "Location  :";
                                string endMarker = "DriverKey";
                                int start = lineOut.IndexOf(startMarker);
                                if (start == -1) Log.Error (string.Empty);

                                start += startMarker.Length;
                                int end = lineOut.IndexOf(endMarker, start);
                                if (end == -1) end = lineOut.Length;

                                 Log.Info(lineOut.Substring(start, end - start).Trim());
                                //Log.Info(line.Substring(349,800));

                            }                           
                        
                        
                        }

                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Failed to run the restartUSB. " + ex.Message);
                    }

                    break;

                default:
                    Log.Info("Entered test is not available. Available tests are readblt, writeblt, atx1_ft, set-gpio," +
                        "set-default-gpio, vidpid, loop_ft, fan_tach, pwrok,findmpdu and read-gpio");
                    throw new Exception("Test not present");
            }

        }        
       

        static void Initialize_Aardvark()
        {
            ushort[] ports = new ushort[16];
            uint[] uniqueIds = new uint[16];
            int numElem = 16;
            int i;

            // Find all the attached devices
            int count = AardvarkApi.aa_find_devices_ext(numElem, ports,
                                                        numElem, uniqueIds);

            Log.Info($"{count} device(s) found:");
            if (count > numElem) count = numElem;

            // Print the information on each device
            for (i = 0; i < count; ++i)
            {
                // Determine if the device is in-use
                string status = "(avail) ";
                if ((ports[i] & AardvarkApi.AA_PORT_NOT_FREE) != 0)
                {
                    ports[i] &= unchecked((ushort)~AardvarkApi.AA_PORT_NOT_FREE);
                    status = "(in-use)";
                }

                // Display device port number, in-use status, and serial number
                uint id = unchecked((uint)uniqueIds[i]);
                Log.Info(string.Format("    port={0,-3} {1} ({2:d4}-{3:d6})",
                                  ports[i], status, id / 1000000,
                                  id % 1000000));
            }
            try
            {
                // Open the device
                handle = AardvarkApi.aa_open(AARDVARK_PORT_NUMBER);
                if (handle <= 0)
                {
                    throw new Exception("Aardvark initialization failed");
                }

                Log.Info(string.Format("Aardvark initialized : {0}", handle));
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("Unable to open Aardvark device on port {0}",
                                      ports[0]));
                Log.Error(string.Format("error: {0}",
                                      AardvarkApi.aa_status_string(handle)));

                AardvarkApi.aa_close(handle);
            }

        }

        static void setupI2C()
        {
            try
            {
                // Ensure that the I2C subsystem is enabled
                AardvarkApi.aa_configure(handle, AardvarkConfig.AA_CONFIG_SPI_I2C);
                AardvarkApi.aa_i2c_pullup(handle, AardvarkApi.AA_I2C_PULLUP_BOTH);

                // Enable the Aardvark adapter's power pins.
                // This command is only effective on v2.0 hardware or greater.
                // The power pins on the v1.02 hardware are not enabled by default.
                AardvarkApi.aa_target_power(handle, AardvarkApi.AA_TARGET_POWER_BOTH);
                //Console.WriteLine("Target power enabled");

                // Setup the bitrate
                int bitrate = AardvarkApi.aa_i2c_bitrate(handle, AARDVARK_BIT_RATE);
                //Console.WriteLine("Bitrate set to {0} kHz", bitrate);

                AardvarkApi.aa_i2c_bus_timeout(handle, AARDVARK_BUS_TIMEOUT);
            }
            catch (Exception ex)
            {
                Log.Info(string.Format("setup error: {0}",
                                      AardvarkApi.aa_status_string(handle)));

                AardvarkApi.aa_close(handle);

            }
        }

        static string readPMBUS()
        {
            // Read CRPS PMBus Interface
            ushort numRead = 0;
            byte[] dataIn = new byte[32];

            try
            {
                int numBytes = AardvarkApi.aa_i2c_read_ext(handle, 0x58, AardvarkI2cFlags.AA_I2C_NO_FLAGS, 32, dataIn, ref numRead);

                Log.Info($"Total bytes read from CRPS is : {numRead}");
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("setup error: {0}",
                                      AardvarkApi.aa_status_string(handle)));

                AardvarkApi.aa_close(handle);
            }

            //Calculate checksum for the data
            byte checksum = 0;
            foreach (byte b in dataIn)
            {
                checksum = (byte) ((checksum+b) & 0xFF);            
            }
            Log.Info($"checksum is : {checksum}");

            // Dump the data to the screen
            Log.Info("Data read from ATX1:");
            for (int i = 0; i < dataIn.Length; ++i)
            {
                //if ((i & 0x0f) == 0) 
                //    _log.AddLog(string.Format("\n{0:x4}:  ", i));

                Log.Info(string.Format("{0:x2} ", dataIn[i] & 0xff));

                //if (((i + 1) & 0x07) == 0) 
                //    _log.AddLog(" ");
            }
            Log.Info("\n");

            string result = ConvertLinear11BlockToAscii(dataIn);

            Log.Info($"Decoded String: {result}");

            return result;
        }

        /// <summary>
        /// Converts a PMBus Block Read byte array to an ASCII string.
        /// </summary>
        /// <param name="data">The raw byte array (Length byte + Data + Padding)</param>
        /// <returns>The decoded ASCII string</returns>
        public static string ConvertLinear11BlockToAscii(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            // The first byte (data[0]) defines how many bytes of actual ASCII follow
            int length = data[0];

            // Safety check: ensure the array is actually as long as the header claims
            // (Length + 1 because the header byte itself isn't part of the string)
            int actualLength = Math.Min(length, data.Length - 1);

            // Extract the string starting from index 1
            return Encoding.ASCII.GetString(data, 1, actualLength);
        }

        static void writePMBUS() 
        {
            byte[] data_out = new byte[1] { 0x9A };


            try 
            {
                int write = AardvarkApi.aa_i2c_write(handle, 0x58,
                 AardvarkI2cFlags.AA_I2C_NO_FLAGS, 1, data_out);
                Log.Info($"Write operation returned  : {write}");
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("Write error: {0}",
                                      AardvarkApi.aa_status_string(handle)));

                AardvarkApi.aa_close(handle);
            }
        }

        public static void readBLTTest()
        {
            //obj.StartTest(MethodBase.GetCurrentMethod().Name);
            IUsbToSpiDevice device = new UsbToSpiDevice();
            device.Connect();

            try
            {
                Log.Info("Starting BLT read test for MPDU ATX1 Fab B");

                BLT_RD = BLT.BLT_Access.ReadBLT(device.EEPROM);
                Log.Info("+++++++++++++++++++++++++++++++++++++++++++");
                Log.Info("Done read BLT");
                Log.Info("+++++++++++++++++++++++++++++++++++++++++++");
                Log.Info("Displaying eeprom BLT content ....");
                Log.Info(" ");
                // Display the BLT content @ CMD
                BltShow();

                //Thread.Sleep(10000);
                // Verify the eeprom content
                for (int i = 0; i < BLT_SIZE; i++)
                {
                    LoadConfig();// load back the content from .ini file
                    BltInit(); // re-create the BLT_WR array contents

                    if (BLT_RD[i] != BLT_WR[i])
                    {

                        Log.Error("Mismatch at eeprom offset: " + i + " " + "Current Content : " + BLT_WR[i].ToString("X") + " vs " + "Expected Content : " + BLT_RD[i].ToString("X"));
                        Log.Error("BLT content mismatch!");
                        throw new Exception("BLT content mismatch!!!");
                    }
                }

                Log.Info("BLT content matches the ATX1_BLT.ini file.");

                Log.Info("+++++++++++++++++++++++++++++++++++++++++++");
                device.Disconnect();
                //obj.EndTest(true);
            }
            catch (Exception ex)
            {
                Bin = 10630202;
                device.Disconnect();
                Log.Error(ex.ToString());
                throw new Exception("BLT read failed");
            }

        }
        static string manid, asmno, snumb, mdate, idate, cycnt, gycnt, fwar1, fwar2, fwar3, brdid, hwrev, fwar4, fwar5, cust;
        static void BltShow()
        {
            manid = asmno = snumb = mdate = idate = cycnt = gycnt = fwar1 = fwar2 = fwar3 = brdid = hwrev = fwar4 = fwar5 = cust = "";

            manid = CharArrayToString((int)(BLT_OFFSET.MANID), 13);
            asmno = CharArrayToString((int)(BLT_OFFSET.ASM), 13);
            snumb = CharArrayToString((int)(BLT_OFFSET.SN), 13);

            mdate = CharArrayToString((int)(BLT_OFFSET.MFDATE), 6);
            idate = CharArrayToString((int)(BLT_OFFSET.INDATE), 6);

            cycnt = CharArrayToDecString((int)(BLT_OFFSET.CYCCNT), 3);
            gycnt = CharArrayToDecString((int)(BLT_OFFSET.GBLCNT), 3);

            fwar1 = CharArrayToString((int)(BLT_OFFSET.FW1), 4);
            fwar2 = CharArrayToString((int)(BLT_OFFSET.FW2), 4);
            fwar3 = CharArrayToString((int)(BLT_OFFSET.FW3), 4);

            brdid = CharArrayToString((int)(BLT_OFFSET.BID), 4);
            hwrev = CharArrayToString((int)(BLT_OFFSET.HW), 4);


            fwar4 = CharArrayToString((int)(BLT_OFFSET.FW4), 1);
            fwar5 = CharArrayToString((int)(BLT_OFFSET.FW5), 1);

            cust = CharArrayToString((int)(BLT_OFFSET.CUST), 11);

            ShowBltContent();
        }

        static string CharArrayToString(int off, int len)
        {
            string str = string.Empty;

            for (int i = 0; i < len; i++)
            {
                try
                {
                    if (BLT_RD[i + off] == 0xFF)
                        str += "";
                    else
                        str += Convert.ToChar(BLT_RD[i + off]).ToString();
                }
#pragma warning disable 0168
                catch (Exception ex)
#pragma warning restore 0168
                {
                    str = string.Empty;
                }
            }

            return str;
        }

        static string CharArrayToDecString(int off, int len)
        {
            string str = string.Empty;

            int dec = BLT_RD[off];
            for (int i = 1; i < len; i++)
            {
                dec = (dec << 8) | BLT_RD[i + off];
            }

            if (dec >= 0xFFFFFF)
            {
                str = string.Empty;
            }
            else
            {
                str = dec.ToString();
            }

            return str;
        }

        static void ShowBltContent()
        {
            Log.Info("Manufacturing ID : " + manid);
            Log.Info("Assembly No : " + asmno);
            Log.Info("Serial No : " + snumb);
            Log.Info("Manufacturing Date : " + mdate);
            Log.Info("Install Date : " + idate);
            Log.Info("Cycle Counter : " + cycnt);
            Log.Info("Global Counter : " + gycnt);
            Log.Info("FW ver1 : " + fwar1);
            Log.Info("FW ver2 : " + fwar2);
            Log.Info("FW ver3 : " + fwar3);
            Log.Info("Board ID : " + brdid);
            Log.Info("Hardware Rev : " + hwrev);
            Log.Info("FW ver4 : " + fwar4);
            Log.Info("FW ver5 : " + fwar5);
            Log.Info("Custom : " + cust);
        }

        static void LoadConfig()
        {
            manid = ReadConfigFile("Manufacturer_ID");
            asmno = ReadConfigFile("Assembly_No");
            snumb = ReadConfigFile("Serial_No");

            brdid = ReadConfigFile("Board_ID");
            hwrev = ReadConfigFile("HW_Revision");

            mdate = GetDate();
            idate = GetDate();
            //mdate = ReadConfigFile("Manufc_Date");
            //idate = ReadConfigFile("Install_Date");

            fwar1 = ReadConfigFile("FW_Revision_1");
            fwar2 = ReadConfigFile("FW_Revision_2");
            fwar3 = ReadConfigFile("FW_Revision_3");
            fwar4 = ReadConfigFile("FW_Revision_4");
            fwar5 = ReadConfigFile("FW_Revision_5");
            cust = ReadConfigFile("Custom");

            cycnt = ReadConfigFile("Cycle_Counter");
            gycnt = ReadConfigFile("Global_Counter");
        }

        static string ReadConfigFile(string param)
        {
            string ret = "0";
            try
            {
                var lines = File.ReadLines(@"C:\STHI\ATX1\ATX1_BLT.ini");
                foreach (string value in lines)
                {
                    if (value.Trim().ToUpper().Contains(param.Trim().ToUpper()))
                    {
                        ret = value.Split('=')[1];
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("error: " + ex);
            }
            return ret;
        }

        static string GetDate()
        {
            SYSTEMTIME st;
            GetSystemTime(out st);
            string YY = st.wYear.ToString();
            YY = YY.Substring(2, 2);
            string MM = st.wMonth.ToString();
            MM = (MM.Length < 2) ? ("0" + MM) : MM;
            string DD = st.wDay.ToString();
            DD = (DD.Length < 2) ? ("0" + DD) : DD;
            string str = YY + MM + DD;
            return str;
        }
        public static void writeBLTTest()
        {
            //obj.StartTest(MethodBase.GetCurrentMethod().Name);
            IUsbToSpiDevice device = new UsbToSpiDevice();
            device.Connect();

            Log.Info("Loading BLT info from the .ini file...");
            LoadConfig(); // read .ini file
            Log.Info("Preparing data to be written to the eeprom...");
            BltInit();
            Log.Info("Showing BLT content that going to be written to eeprom");

            BltShowWR();

            try
            {
                Log.Info("Starting BLT write test for MPDU ATX1 Fab B");
                for (byte i = 0x00; i < BLT_SIZE; i += 0x01)
                    BLT.BLT_Access.WriteBLT(device.EEPROM, i, BLT_WR[i]);
                Log.Info("+++++++++++++++++++++++++++++++++++++++++++++");
            }
            catch (Exception ex)
            {
                Bin = 10630201;
                device.Disconnect();
                Log.Error(ex.ToString());
                throw new Exception("BLT write failed");
            }

            device.Disconnect();
            //obj.EndTest(true);
        }

        static void BltInit()
        {
            PutStringToCharArray(manid, (int)(BLT_OFFSET.MANID));

            for (int i = 0; i < asmno.Length; i++)
            {
                if (!(asmno.Substring(i, 1).All(Char.IsLetter)))
                {
                    asmno = asmno.Substring(i - 1);
                    break;
                }
            }
            PutStringToCharArray(asmno, (int)(BLT_OFFSET.ASM));
            PutStringToCharArray(snumb, (int)(BLT_OFFSET.SN));

            PutStringToCharArray(mdate, (int)(BLT_OFFSET.MFDATE));
            PutStringToCharArray(idate, (int)(BLT_OFFSET.MFDATE + 7));

            StringToHexArray(cycnt, (int)(BLT_OFFSET.CYCCNT));
            StringToHexArray(gycnt, (int)(BLT_OFFSET.GBLCNT));// .CYCCNT + 7));

            FirmwareVersion(fwar1, (int)(BLT_OFFSET.FW1));
            FirmwareVersion(fwar2, (int)(BLT_OFFSET.FW1 + 4));
            FirmwareVersion(fwar3, (int)(BLT_OFFSET.FW1 + 8));
            PutStringToCharArray(fwar4, (int)(BLT_OFFSET.FW4));
            PutStringToCharArray(fwar5, (int)(BLT_OFFSET.FW4 + 1));
            PutStringToCharArray(cust, (int)(BLT_OFFSET.FW4 + 2));

            PutStringToCharArray(brdid, (int)(BLT_OFFSET.BID));
            HardwareRev(hwrev, (int)(BLT_OFFSET.BID + 0xd));//0xd


            BLT_WR[(byte)(BLT_OFFSET.MANID + BLT_RFDBOFFSET)] = 0x01;
            BLT_WR[(byte)(BLT_OFFSET.ASM + BLT_RFDBOFFSET)] = 0x01;
            BLT_WR[(byte)(BLT_OFFSET.SN + BLT_RFDBOFFSET)] = 0x01;
            BLT_WR[(byte)(BLT_OFFSET.MFDATE + BLT_RFDBOFFSET)] = 0x31;
            BLT_WR[(byte)(BLT_OFFSET.CYCCNT + BLT_RFDBOFFSET)] = 0x10;
            BLT_WR[(byte)(BLT_OFFSET.FW1 + BLT_RFDBOFFSET)] = 0x01;
            BLT_WR[(byte)(BLT_OFFSET.BID + BLT_RFDBOFFSET)] = 0x01;
            BLT_WR[(byte)(BLT_OFFSET.FW4 + BLT_RFDBOFFSET)] = 0x01;

            for (int i = 0; i < BLT_SIZE; i += 0x10)
                CRC_APPEND_CHECKSUM((ulong)(i + BLT_CRCOFFSET));
        }
        static void PutStringToCharArray(string str, int off)
        {
            int len = str.Length;
            char[] tmp = str.ToCharArray();

            byte temp = 0;
            for (int i = 0; i < len; i++)
            {
                temp = BLT_WR[i + off];
                try
                {
                    BLT_WR[i + off] = (byte)tmp[i];
                }
#pragma warning disable 0168
                catch (Exception ex)
#pragma warning restore 0168
                {
                    BLT_WR[i + off] = temp;
                }
            }
        }

        static void StringToHexArray(string num, int off)
        {
            int len = 3;
            string hex = string.Format("{0:X6}", Convert.ToUInt32(num));

            for (int i = 0; i < len; i++)
            {
                try
                {
                    BLT_WR[i + off] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                }
#pragma warning disable 0168
                catch (Exception ex)
#pragma warning restore 0168
                {
                    BLT_WR[i + off] = 0x00;
                }
            }
        }

        static void FirmwareVersion(string str, int off)
        {
            int len = str.Length;
            char[] tmp;

            if (str.Contains('.'))
            {
                string[] s0 = str.Split('.');
                string s1 = "00" + s0[0];
                string s2 = "00" + s0[1];
                string s3 = s1.Substring(s1.Length - 2) + s2.Substring(s2.Length - 2);
                tmp = s3.ToCharArray();
            }
            else
            {
                tmp = str.ToCharArray();
            }

            byte temp = 0;
            for (int i = 0; i < len; i++)
            {
                temp = BLT_WR[i + off];
                try
                {
                    BLT_WR[i + off] = (byte)tmp[i];
                }
#pragma warning disable 0168
                catch (Exception ex)
#pragma warning restore 0168
                {
                    BLT_WR[i + off] = temp;
                }
            }
        }

        static void HardwareRev(string str, int off)
        {
            int len = str.Length;
            char[] tmp = str.ToCharArray();

            byte temp = 0;
            for (int i = 0; i < len; i++)
            {
                temp = BLT_WR[i + off - len];
                try
                {
                    BLT_WR[i + off - len] = (byte)tmp[i];
                }
#pragma warning disable 0168
                catch (Exception ex)
#pragma warning restore 0168
                {
                    BLT_WR[i + off - len] = temp;
                }
            }
        }

        static void BltShowWR()
        {
            manid = asmno = snumb = mdate = idate = cycnt = gycnt = fwar1 = fwar2 = fwar3 = brdid = hwrev = fwar4 = fwar5 = cust = "";

            manid = CharArrayToStringWR((int)(BLT_OFFSET.MANID), 13);
            asmno = CharArrayToStringWR((int)(BLT_OFFSET.ASM), 13);
            snumb = CharArrayToStringWR((int)(BLT_OFFSET.SN), 13);

            mdate = CharArrayToStringWR((int)(BLT_OFFSET.MFDATE), 6);
            idate = CharArrayToStringWR((int)(BLT_OFFSET.INDATE), 6);

            cycnt = CharArrayToDecStringWR((int)(BLT_OFFSET.CYCCNT), 3);
            gycnt = CharArrayToDecStringWR((int)(BLT_OFFSET.GBLCNT), 3);

            fwar1 = CharArrayToStringWR((int)(BLT_OFFSET.FW1), 4);
            fwar2 = CharArrayToStringWR((int)(BLT_OFFSET.FW2), 4);
            fwar3 = CharArrayToStringWR((int)(BLT_OFFSET.FW3), 4);

            brdid = CharArrayToStringWR((int)(BLT_OFFSET.BID), 4);
            hwrev = CharArrayToStringWR((int)(BLT_OFFSET.HW), 4);


            fwar4 = CharArrayToStringWR((int)(BLT_OFFSET.FW4), 4);
            fwar5 = CharArrayToStringWR((int)(BLT_OFFSET.FW5), 4);

            cust = CharArrayToString((int)(BLT_OFFSET.CUST), 4);

            ShowBltContent();
        }

        static string CharArrayToStringWR(int off, int len)
        {
            string str = string.Empty;

            for (int i = 0; i < len; i++)
            {
                try
                {
                    if (BLT_WR[i + off] == 0xFF)
                        str += "";
                    else
                        str += Convert.ToChar(BLT_WR[i + off]).ToString();
                }
#pragma warning disable 0168
                catch (Exception ex)
#pragma warning restore 0168
                {
                    str = string.Empty;
                }
            }

            return str;
        }

        static string CharArrayToDecStringWR(int off, int len)
        {
            string str = string.Empty;

            int dec = BLT_WR[off];
            for (int i = 1; i < len; i++)
            {
                dec = (dec << 8) | BLT_WR[i + off];
            }

            if (dec >= 0xFFFFFF)
            {
                str = string.Empty;
            }
            else
            {
                str = dec.ToString();
            }

            return str;
        }

        static void CRC_APPEND_CHECKSUM(ulong length)
        {
            ulong crc = CRC_CHECKSUM(length);
            BLT_WR[length + 1] = (byte)(crc & 0xff);
            BLT_WR[length] = (byte)((crc >> 8) & 0xff);
        }

        public static ushort CRC_VALUE(ulong crcval, ulong newchar)
        {
            return (ushort)((crcval >> 8) ^ CRC_TABLE[(crcval ^ newchar) & 0x00ff]);
        }

        public static ushort CRC_CHECKSUM(ulong length)
        {
            ushort crc = 0;
            byte[] data = new byte[0xE];
            ulong j = 0;
            bool apse = true;
            //if (apse) Console.WriteLine();
            for (ulong i = (length - 0x0E); i < length; i++)
            {
                crc = CRC_VALUE(crc, BLT_WR[i]);
                //Console.WriteLine(length.ToString("x") + "\t" + i.ToString("x"));
                if (apse)
                {
                    data[j] = BLT_WR[i];
                    //Console.Write(data[j].ToString("X2") + " ");
                    j++;
                }
            }
            if (apse)
            {
                //Console.WriteLine();
                byte[] val = (new CRCEEPROM()).CalculateCRC(data);
                //Console.WriteLine("CRC value is : " + val[0].ToString("X2") + " " + val[1].ToString("X2"));
                crc = (ushort)(((ulong)val[0]) << 8);
                crc += (ushort)val[1];
            }
            return crc;
        }

        public const int CRC_INIT = 0xFFFF;

        public static readonly ushort[] CRC_TABLE = new ushort[256]
        {
            0x0000, 0x1189, 0x2312, 0x329b, 0x4624, 0x57ad, 0x6536, 0x74bf,
            0x8c48, 0x9dc1, 0xaf5a, 0xbed3, 0xca6c, 0xdbe5, 0xe97e, 0xf8f7,
            0x1081, 0x0108, 0x3393, 0x221a, 0x56a5, 0x472c, 0x75b7, 0x643e,
            0x9cc9, 0x8d40, 0xbfdb, 0xae52, 0xdaed, 0xcb64, 0xf9ff, 0xe876,
            0x2102, 0x308b, 0x0210, 0x1399, 0x6726, 0x76af, 0x4434, 0x55bd,
            0xad4a, 0xbcc3, 0x8e58, 0x9fd1, 0xeb6e, 0xfae7, 0xc87c, 0xd9f5,
            0x3183, 0x200a, 0x1291, 0x0318, 0x77a7, 0x662e, 0x54b5, 0x453c,
            0xbdcb, 0xac42, 0x9ed9, 0x8f50, 0xfbef, 0xea66, 0xd8fd, 0xc974,
            0x4204, 0x538d, 0x6116, 0x709f, 0x0420, 0x15a9, 0x2732, 0x36bb,
            0xce4c, 0xdfc5, 0xed5e, 0xfcd7, 0x8868, 0x99e1, 0xab7a, 0xbaf3,
            0x5285, 0x430c, 0x7197, 0x601e, 0x14a1, 0x0528, 0x37b3, 0x263a,
            0xdecd, 0xcf44, 0xfddf, 0xec56, 0x98e9, 0x8960, 0xbbfb, 0xaa72,
            0x6306, 0x728f, 0x4014, 0x519d, 0x2522, 0x34ab, 0x0630, 0x17b9,
            0xef4e, 0xfec7, 0xcc5c, 0xddd5, 0xa96a, 0xb8e3, 0x8a78, 0x9bf1,
            0x7387, 0x620e, 0x5095, 0x411c, 0x35a3, 0x242a, 0x16b1, 0x0738,
            0xffcf, 0xee46, 0xdcdd, 0xcd54, 0xb9eb, 0xa862, 0x9af9, 0x8b70,
            0x8408, 0x9581, 0xa71a, 0xb693, 0xc22c, 0xd3a5, 0xe13e, 0xf0b7,
            0x0840, 0x19c9, 0x2b52, 0x3adb, 0x4e64, 0x5fed, 0x6d76, 0x7cff,
            0x9489, 0x8500, 0xb79b, 0xa612, 0xd2ad, 0xc324, 0xf1bf, 0xe036,
            0x18c1, 0x0948, 0x3bd3, 0x2a5a, 0x5ee5, 0x4f6c, 0x7df7, 0x6c7e,
            0xa50a, 0xb483, 0x8618, 0x9791, 0xe32e, 0xf2a7, 0xc03c, 0xd1b5,
            0x2942, 0x38cb, 0x0a50, 0x1bd9, 0x6f66, 0x7eef, 0x4c74, 0x5dfd,
            0xb58b, 0xa402, 0x9699, 0x8710, 0xf3af, 0xe226, 0xd0bd, 0xc134,
            0x39c3, 0x284a, 0x1ad1, 0x0b58, 0x7fe7, 0x6e6e, 0x5cf5, 0x4d7c,
            0xc60c, 0xd785, 0xe51e, 0xf497, 0x8028, 0x91a1, 0xa33a, 0xb2b3,
            0x4a44, 0x5bcd, 0x6956, 0x78df, 0x0c60, 0x1de9, 0x2f72, 0x3efb,
            0xd68d, 0xc704, 0xf59f, 0xe416, 0x90a9, 0x8120, 0xb3bb, 0xa232,
            0x5ac5, 0x4b4c, 0x79d7, 0x685e, 0x1ce1, 0x0d68, 0x3ff3, 0x2e7a,
            0xe70e, 0xf687, 0xc41c, 0xd595, 0xa12a, 0xb0a3, 0x8238, 0x93b1,
            0x6b46, 0x7acf, 0x4854, 0x59dd, 0x2d62, 0x3ceb, 0x0e70, 0x1ff9,
            0xf78f, 0xe606, 0xd49d, 0xc514, 0xb1ab, 0xa022, 0x92b9, 0x8330,
            0x7bc7, 0x6a4e, 0x58d5, 0x495c, 0x3de3, 0x2c6a, 0x1ef1, 0x0f78
        };

    }
}
