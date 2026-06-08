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
using _ATX1;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;

namespace FT
{
    public class pdb1
    {        
        public static IFusionSTDIO STDIO;
        const string endPointNameStdio = @"FusionSTDIO";
        private static Logger obj;

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool GetSystemTime(out SYSTEMTIME systemTime);

        public static void SetUpWCF(string ip = "localhost")
        {
            string endPointBase = @"net.tcp://" + ip + ":8990/";
            EndpointAddress endpointAddressStdio = new EndpointAddress(endPointBase + endPointNameStdio);
            ChannelFactory<IFusionSTDIO> channelFactoryStdio = new ChannelFactory<IFusionSTDIO>(WCFBinding.GetBinding(), endpointAddressStdio);
            STDIO = channelFactoryStdio.CreateChannel();

            Log.Info("Creating STDIO WCF interface");
            Log.Info("\n");

            if (STDIO == null)
            {
                Log.Error("Could not create " + endPointNameStdio + " object.");
                throw new Exception("Could not create " + endPointNameStdio + " object.");
            }
            Thread.Sleep(100);

            // Clear previous then load map files
            STDIO.ClearDVMMap();
            Thread.Sleep(500);
            STDIO.ClearDIOMap();
            Thread.Sleep(500);

            Log.Info("Cleared old DIO/DVM map files");

            STDIO.LoadDVMMapFile(@"C:\STHI\ATX1\DVM.xml");
            Thread.Sleep(2000);
            STDIO.LoadDIOMapFile(@"C:\STHI\ATX1\DIO2.xml");
            Thread.Sleep(2000);

            STDIO.SetInitialDIOState();
            Thread.Sleep(1000);
        }

        public static int copytoFCC()
        {
            try
            {
                Process cmd = new Process();
                cmd.StartInfo.FileName = "robocopy";
                cmd.StartInfo.Arguments = @" \\10.0.0.100\STHI \\10.0.0.1\STHI 01.scan";
                cmd.StartInfo.UseShellExecute = false;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.RedirectStandardError = true;
                cmd.StartInfo.RedirectStandardInput = true;
                //cmd.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);
                cmd.Start();
                cmd.WaitForExit();

                Log.Info("printing output from Robocopy...");
                while (!cmd.StandardOutput.EndOfStream)
                {
                    Log.Info(cmd.StandardOutput.ReadLine());
                }
                return 0;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to copy scan file to FCC. " + ex.Message);
                return 9;
            }
        }

        public static int copytoHost()
        {
            try
            {
                // Map network drives with credentials first
                string password = "P@ssw0rd";
                string domain = "STTD-APSE-40";
                string username = "Administrator";
                Process netUse = new Process();
                netUse.StartInfo.FileName = "net";
                netUse.StartInfo.Arguments = $@" use \\10.0.0.100\STHI {password} /user:{domain}\{username}";
                netUse.StartInfo.UseShellExecute = false;
                netUse.StartInfo.CreateNoWindow = true;
                netUse.Start();
                netUse.WaitForExit();

                Process cmd = new Process();
                cmd.StartInfo.FileName = "robocopy";
                cmd.StartInfo.Arguments = @" \\10.0.0.1\STHI\DataLog \\10.0.0.100\STHI\DataLog ituff.itf";
                cmd.StartInfo.UseShellExecute = false;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.RedirectStandardError = true;
                cmd.StartInfo.RedirectStandardInput = true;
                //cmd.OutputDataReceived += (s, e) => Console.WriteLine(e.Data);
                cmd.Start();
                cmd.WaitForExit();

                Log.Info("printing output from Robocopy...");
                while (!cmd.StandardOutput.EndOfStream)
                {
                    Log.Info(cmd.StandardOutput.ReadLine());
                }
                return 0;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to copy ituff file to Host. " + ex.Message);
                return 9;
            }
        }

        static string SN = "";
        static string AA = "";
        public static int AddSN(string scanFilePath)
        {
            string path = scanFilePath;

            // Read a text file line by line.  
            string[] lines = File.ReadAllLines(path);
            //var SN = "";
            //var AA = "";

            Log.Info("\n");
            Log.Info("Scan file info : ");

            foreach (string line in lines)
            {
                //Extract SN and AA number

                if (line.Contains("SERIAL"))
                {
                    SN = line;
                    Log.Info("\n");
                    Log.Info("===========================");
                    Log.Info(SN);

                }

                if (line.Contains("PRODUCT"))
                {
                    AA = line;
                    Log.Info(AA);
                    Log.Info("============================");
                }

            }
            Log.Info("\n");
            return 0;
        }


        static void lineChanger(string newText, string fileName, int line_to_edit)
        {
            string[] arrLine = File.ReadAllLines(fileName);
            arrLine[line_to_edit - 1] = newText;
            File.WriteAllLines(fileName, arrLine);
        }
        public static int UpdateINI(string SN, string AA)
        {
            string path = @"C:\STHI\ATX1\ATX1_BLT.ini";

            int pos1 = SN.IndexOf("=");

            int pos2 = AA.IndexOf("=");

            string newSN = SN.Remove(0, pos1+1);
            string newAA = AA.Remove(0, pos2+1);


            string editSN = "Serial_No=" + newSN;

            string editAA = "Assembly_No=" + newAA;

             

            Log.Info("Updating SN in INI file ....");
            lineChanger(editSN,path,4);
            Thread.Sleep(1000);

            Log.Info("Updating AA in INI file ....");
            lineChanger(editAA, path, 3);

            return 0;

        }


        public static class DO_Channel // DIO Output Channels
        {
            public const string DO_0 = "0";
            public const string DO_1 = "1";
            public const string DO_2 = "2";
            public const string DO_3 = "3";
            public const string DO_4 = "4";
            public const string DO_5 = "5";
        }
        
        public static void setGPIO(bool[] gpio)
        {
            Log.Info("Writing GPIO settings of MCP 2210");
            //Variables
            uint mcp2210_VID = 0x8087; // VID Intel
            uint mcp2210_PID = 0x0BE1; // todo : need to check the assigned PID for this card

            IUsbToSpiDevice device = new UsbToSpiDevice();
            device.Connect();

            bool isConnected = false; // Connection status variable for MCP2210

            DevIO UsbSpi = new DevIO(mcp2210_VID, mcp2210_PID);

            isConnected = UsbSpi.Settings.GetConnectionStatus();

            if (!isConnected)
            {
                throw new Exception("Device not connected with 8087/0BE1.");
            }

            // change default output of current RAM (volatile)
            try
            {
                configure_GPIO(gpio);
            }
            catch (Exception ex)
            {
                device.Disconnect();
                Log.Error($"Unable to set GPIO. Error: {ex.Message}");
                throw new Exception("Failed to set GPIO");
            }

            device.Disconnect();
            Log.Info("Setting GPIO configuration -> Completed.");
        }

        static void configure_GPIO(bool[] gpio) // VRAM only
        {
            Log.Info("Setting  GPIO default output of MCP 2210");
            IUsbToSpiDevice device = new UsbToSpiDevice();
            device.Connect();

            //Variables
            uint mcp2210_VID = 0x8087; // VID for Microchip Technology Inc. 0x04D8
            uint mcp2210_PID = 0x0BE1; // todo : same as above

            bool isConnected = false; // Connection status variable for MCP2210

            DevIO UsbSpi = new DevIO(mcp2210_VID, mcp2210_PID);

            isConnected = UsbSpi.Settings.GetConnectionStatus();

            if (!isConnected)
            {
                throw new Exception("Device not connected with 8087/0BE1.");
            }

            IVolatileRam vram = device.VolatileRam;
            
            // set the chip configuration
            ChipSettings chipSettings = new ChipSettings();
            chipSettings.InterruptBitMode = DedicatedFunction.CountFallingEdges;
            chipSettings.RemoteWakeUpEnabled = true;
            chipSettings.SpiBusReleaseEnable = true;
            chipSettings.AccessControl = NramChipAccessControl.NotProtected;
            
            chipSettings.PinDirections = new PinDirection[] {
                    PinDirection.Output,
                    PinDirection.Output,
                    PinDirection.Output,
                    PinDirection.Output,
                    PinDirection.Output,
                    PinDirection.Input,
                    PinDirection.Input,
                    PinDirection.Input,
                    PinDirection.Input
                };

            chipSettings.PinModes = new PinMode[] {
                    PinMode.GPIO,
                    PinMode.GPIO,
                    PinMode.GPIO,
                    PinMode.GPIO,
                    PinMode.GPIO,
                    PinMode.GPIO,
                    PinMode.DedicatedFunction, //Special Function - Fan Tach - GPIO 6
                    PinMode.GPIO,
                    PinMode.GPIO
                };

            // change default output
            chipSettings.DefaultOutput = gpio;

            vram.ConfigureChip(chipSettings);

            Thread.Sleep(3000);
        }

        public static void setGPIO_nvram(bool[] gpio)
        {
            Log.Info("Writing GPIO settings of NVRAM of MCP 2210");
            //Variables
            uint mcp2210_VID = 0x8087; // VID for Microchip Technology Inc. 0x04D8
            uint mcp2210_PID = 0x0BE1; // same as above

            IUsbToSpiDevice device = new UsbToSpiDevice();
            device.Connect();

            bool isConnected = false; // Connection status variable for MCP2210

            DevIO UsbSpi = new DevIO(mcp2210_VID, mcp2210_PID);

            isConnected = UsbSpi.Settings.GetConnectionStatus();

            if (!isConnected)
            {
                throw new Exception("Device not connected with 8087/0BE1.");
            }

            // change default output of NV RAM 
            try
            {
                configure_GPIO_NVRAM(gpio);
            }
            catch (Exception ex)
            {
                device.Disconnect();
                Log.Error($"Unable to set GPIO. Error: {ex.Message}");
                throw new Exception("Failed to set GPIO");
            }

            device.Disconnect();
            Log.Info("Setting NVRAM GPIO configuration -> Completed.");
        }

        static void configure_GPIO_NVRAM(bool[] gpio) 
        {
            Log.Info("Writing GPIO settings of MCP 2210 to NVRAM");

            IUsbToSpiDevice device = new UsbToSpiDevice();
            device.Connect();

            //Variables
            uint mcp2210_VID = 0x8087; // VID for Intel
            uint mcp2210_PID = 0x0BE1; // check value

            bool isConnected = false; // Connection status variable for MCP2210

            DevIO UsbSpi = new DevIO(mcp2210_VID, mcp2210_PID);

            isConnected = UsbSpi.Settings.GetConnectionStatus();

            if (!isConnected)
            {
                throw new Exception("Device not connected with 8087/0BE1.");
            }

            // change default output of Non - volatile RAM
            try
            {
                INonVolatileRam vram = device.NonVolatileRam;                

                // set the chip configuration
                ChipSettings chipSettings = new ChipSettings();
                chipSettings.InterruptBitMode = DedicatedFunction.CountFallingEdges;
                chipSettings.RemoteWakeUpEnabled = true;
                chipSettings.SpiBusReleaseEnable = true;//false 
                chipSettings.AccessControl = NramChipAccessControl.NotProtected;

                vram.ManufacterName = "Intel";
                vram.ProductName = "MPDU_20V";

                chipSettings.PinDirections = new PinDirection[] {
                    PinDirection.Output,
                    PinDirection.Output,
                    PinDirection.Output,
                    PinDirection.Output,
                    PinDirection.Output,
                    PinDirection.Input,
                    PinDirection.Input,
                    PinDirection.Input,
                    PinDirection.Input
                };

                chipSettings.PinModes = new PinMode[] {
                    PinMode.GPIO,
                    PinMode.GPIO,
                    PinMode.GPIO,
                    PinMode.GPIO,
                    PinMode.GPIO,
                    PinMode.GPIO,
                    PinMode.DedicatedFunction, //Special Function - Fan Tach - GPIO 6
                    PinMode.GPIO,
                    PinMode.GPIO
                };
                
                // change default output
                chipSettings.DefaultOutput = gpio;

                vram.ConfigureChip(chipSettings);
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                device.Disconnect();
                Log.Error($"Unable to set GPIO at the end. Error: {ex.Message}");
                throw new Exception("Failed to set GPIO at the end");
            }

            device.Disconnect();
            Log.Info("Setting GPIO configuration -> Completed.");
        }

        public static string gpioVal() 
        {
            Log.Info("Reading GPIO pin values of MCP 2210 ...");

            IUsbToSpiDevice device = new UsbToSpiDevice();
            device.Connect();

            //Variables
            uint mcp2210_VID = 0x8087; // VID for Microchip Technology Inc. 0x04D8
            uint mcp2210_PID = 0x0BE1; // check the value

            bool isConnected = false; // Connection status variable for MCP2210

            DevIO UsbSpi = new DevIO(mcp2210_VID, mcp2210_PID);

            isConnected = UsbSpi.Settings.GetConnectionStatus();

            if (!isConnected)
            {
                throw new Exception("Device not connected with 8087/0BE1.");
            }

            var pinVal = "";
            try
            {
                Log.Info("GPIO Pin Values : ");
                pinVal = Convert.ToString(UsbSpi.Functions.GetGpioPinVal(), 2);
                Log.Info(pinVal + "\n");

            }
            catch (Exception ex)
            {
                device.Disconnect();
                Log.Error($"Unable to read GPIO values. Error: {ex.Message}");
                throw new Exception("Failed to read GPIO vals");
            }            

            device.Disconnect();
            return pinVal;
        }

        static int powerOnCRPS()
        {
            try
            {
                Log.Info("Powering On CRPS...");
                Process cmd = new Process();
                cmd.StartInfo.FileName = @"curl.exe";
                cmd.StartInfo.Arguments = @" http://192.168.1.10/out_ctrl.csp?port=1&ctrl_kind=1" + " -u" + " admin:admin";
                Log.Info($"Arguments ON : {cmd.StartInfo.Arguments}");
                cmd.StartInfo.UseShellExecute = false;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.RedirectStandardError = true;
                cmd.StartInfo.RedirectStandardInput = true;
                cmd.Start();
                cmd.WaitForExit();

                Log.Info("printing output from CRPS power on operation...");
                while (!cmd.StandardOutput.EndOfStream)
                {
                    Log.Info(cmd.StandardOutput.ReadLine());
                }
                return 0;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to power on CRPS !!!. " + ex.Message);
                return 9;
            }
        }

        public static int powerOffCRPS()
        {
            try
            {
                Log.Info("Powering OFF CRPS...");
                Process cmd = new Process();
                cmd.StartInfo.FileName = @"curl.exe";
                cmd.StartInfo.Arguments = @" http://192.168.1.10/out_ctrl.csp?port=1&ctrl_kind=2" + " -u" + " admin:admin";
                Log.Info($"Arguments OFF : {cmd.StartInfo.FileName}");
                cmd.StartInfo.UseShellExecute = false;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.RedirectStandardError = true;
                cmd.StartInfo.RedirectStandardInput = true;
                cmd.Start();
                cmd.WaitForExit();

                Log.Info("printing output from CRPS power off operation...");
                while (!cmd.StandardOutput.EndOfStream)
                {
                    Log.Info(cmd.StandardOutput.ReadLine());
                }
                return 0;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to power off CRPS !!!. " + ex.Message);
                return 9;
            }
        }

        public static void read_20V(double expected_voltage) 
        {
            var toleranceV = 0.5;
            var expectedVolt = expected_voltage;

            DIOState[] FanMapping = new DIOState[6] { DIOState.HIGH, DIOState.HIGH, DIOState.HIGH,
                                                      DIOState.HIGH,DIOState.HIGH,DIOState.HIGH};


            string[] fanChannels =new string[2] { "9", "10" };

            string[] DIOchannel_12V = new string[6] { DO_Channel.DO_0, DO_Channel.DO_1, DO_Channel.DO_2, DO_Channel.DO_3, DO_Channel.DO_4,
                DO_Channel.DO_5};

            for (int j = 0; j < 6; j++)
            {
                Log.Info("Setting DIO " + j + " to " + FanMapping[j].ToString());
                STDIO.SetDIO(DIOchannel_12V[j], (byte)FanMapping[j]);
            }
            Thread.Sleep(3000);

            try
            {
                for (int k = 0; k < fanChannels.Length; k++)
                {
                    var DVM = 0.00;

                    DVM = STDIO.ReadCalculatedDVM(fanChannels[k]);

                    Log.Info("DVM for Channel " + $"{fanChannels[k]}" + " is : " + DVM);

                    if (DVM > expectedVolt + toleranceV || DVM < expectedVolt - toleranceV)
                    {
                        throw new Exception($"DVM for channel {fanChannels[k]} failed." + " Expected : " + expectedVolt + " Current -> " + DVM);
                    }

                }

            }
            catch (Exception ex)
            {
                Log.Info("Disaster : Reading 20V failed!");
                Log.Error(ex.Message);
                throw new Exception(ex.Message);
            }

        }

        public static void read_12vAUX(double expVoltage) 
        {
            var toleranceV = 0.5;
            var expectedVolt = expVoltage;

            string[] DIOchannel_12V = new string[6] { DO_Channel.DO_0, DO_Channel.DO_1, DO_Channel.DO_2, DO_Channel.DO_3, DO_Channel.DO_4,
                DO_Channel.DO_5};

            DIOState[] ADMMapping = new DIOState[6] { DIOState.LOW, DIOState.LOW, DIOState.LOW,
                                                      DIOState.LOW,DIOState.LOW,DIOState.LOW};

            var auxChannels = new string[2] { "0", "1" }; // 0,1 = 12V

            for (int j = 0; j < 6; j++)
            {
                Log.Info("Setting DIO " + j + " to " + ADMMapping[j].ToString());
                STDIO.SetDIO(DIOchannel_12V[j], (byte)ADMMapping[j]);
            }
            Thread.Sleep(3000);

            try
            {
                for (int k = 0; k < auxChannels.Length; k++)
                {
                    var DVM = 0.00;

                    DVM = STDIO.ReadCalculatedDVM(auxChannels[k]);

                    Log.Info("DVM for Channel " + $"{auxChannels[k]}" + " is : " + DVM);

                    if (DVM > expectedVolt + toleranceV || DVM < expectedVolt - toleranceV)
                    {
                        throw new Exception($"DVM for channel {auxChannels[k]} failed." + " Expected : " + expectedVolt + " Current -> " + DVM);
                    }

                }

            }
            catch (Exception ex)
            {
                Log.Info("Disaster : Reading 12V AUX failed!");
                Log.Error(ex.Message);
                throw new Exception(ex.Message);
            }

        }

        public static string findMPDUPort() 
        {
            string port = "";
            try
            {
                Process cmd = new Process();
                cmd.StartInfo.FileName = "RestartUsbPort.exe";
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
                string targetDeviceId = @"USB\VID_04D8&PID_00DE"; // default Microchip devices VID/PID
                string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                bool found = false;

                foreach (string line in lines)
                {
                    if (line.Contains(targetDeviceId))
                    {
                        //Log.Info($"{line}");
                       // Log.Info("found the line");
                        int index = line.IndexOf(targetDeviceId);
                       // Log.Info($"index : {index}");

                        string lineOut = line.Substring(index, index + 150);
                        string startMarker = "Location  :";
                        string endMarker = "DriverKey";
                        int start = lineOut.IndexOf(startMarker);
                        if (start == -1) Log.Error(string.Empty);

                        start += startMarker.Length;
                        int end = lineOut.IndexOf(endMarker, start);
                        if (end == -1) end = lineOut.Length;

                        //Log.Info(lineOut.Substring(start, end - start).Trim());
                        port = lineOut.Substring(start, end - start).Trim();
                        //Log.Info(line.Substring(349,800));
                        found = true;
                    }
                }

                if (found)
                {
                    Log.Info("MPDU ATX1 card is found to be connected at USB port : " + port);
                    return port;
                }
                else 
                {
                    Log.Error("Cant find the usb port of the connected MPDU ATX1 card. Make sure the card is connected.");
                    throw new Exception("Cant find the usb port of the connected MPDU ATX1 card. Make sure the card is connected.");
                }

            }
            catch (Exception ex)
            {
                Log.Error($"Failed to find usb port of the MPDU ATX1 card. " + ex.Message);
                return "failed";                
            }
            
        }

        public static string restartUSB(string port)
        {
            try
            {
                Process cmd = new Process();
                cmd.StartInfo.FileName = "RestartUsbPort.exe";
                cmd.StartInfo.Arguments = $" {port}"; //Port_#0004.Hub_#0004-NG Port_#0002.Hub_#0001-OLD
                cmd.StartInfo.UseShellExecute = false;
                cmd.StartInfo.RedirectStandardOutput = true;
                cmd.StartInfo.RedirectStandardError = true;
                cmd.StartInfo.RedirectStandardInput = true;
                cmd.Start();
                cmd.WaitForExit();

                Log.Info("printing output from Restart USB Port...");
                string output =  null;
                while (!cmd.StandardOutput.EndOfStream)
                {
                    Log.Info(cmd.StandardOutput.ReadLine());
                    output += cmd.StandardOutput.ReadLine();
                }
                return output;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to run the restartUSB. " + ex.Message);
                return "9";
            }
        }

        public static int pdb1ft()
        {
            copytoFCC();//copy updated 01.scan file from Host           

            Thread.Sleep(5000);           

            obj = new Logger();
            obj.StartLot("C:\\STHI\\NG20V\\ITUFFTemplate.xml");
            obj.LoadBinList("C:\\STHI\\NG20V\\binlist.xml");
            obj.StartDut();

            AddSN(@"C:\STHI\01.scan");

            UpdateINI(SN, AA);// grab sn/aa from 01.scan and update the BLT.ini

            SetUpWCF();

            int Bin = 0;// ituff bin

            #region VID/PID prog
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
                UsbSpi = new DevIO(0x8087, 0x0BD3); // todo: check the pid
                isConnected = UsbSpi.Settings.GetConnectionStatus();
                porVidPid = false;
                Log.Info("Board has correct POR VID/PID. Wont be re-programmed.");

                if (!isConnected)
                    throw new Exception("Device not connected with either 048d/00de or 8087/."); //update
            }

            try
            {
                if (porVidPid)
                {
                    Log.Info("Board needs VID/PID programming ...");

                    INonVolatileRam vram = device.NonVolatileRam;

                    Log.Info("Programming POR Intel VID/PID : 8087/0BD3"); //check
                    UsbKeyPowerSettings keypower1 = new UsbKeyPowerSettings();

                    keypower1.RemoteWakeUpCapable = false;
                    keypower1.RequestedCurrent = 100;//mA
                    keypower1.HostPowered = true;
                    keypower1.VID = 0x8087;
                    keypower1.PID = 0x0BD3; //change later
                    vram.WriteUsbSettings(keypower1);
                    Log.Info("Done programming VID/PID.");
                    Thread.Sleep(1000);

                    restartUSB(findMPDUPort());//automated for finding the usb port

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
                copytoHost();
                return Bin;
            }

            #endregion VID/PID prog

            powerOnCRPS();

            Thread.Sleep(3000);

            //Declare the GPIO array with PS_ON pulled to HIGH and all the outputs voltage rails enabled
            bool[] gpio_en = new bool[9] { false, false, false, false, true, false, false, false, false };


            //writeBLT test
            obj.StartTest("Write BLT Test");
            Log.Info("Staring write blt test");

            try
            {
                _ATX1.Program.writeBLTTest();
            }
            catch (Exception ex)
            {
                powerOffCRPS();
                Bin = 10630102;
                Log.Error("Disaster: Write BLT test failed.");
                Log.Error(ex.Message);
                obj.SetDutResult(Bin);
                obj.EndDut();
                obj.EndLot();
                copytoHost();
                return Bin;
            }

            Log.Info("Done : Write BLT Test");
            obj.EndTest(true);
            Log.Info("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");

            //readBLT test            
            obj.StartTest("Read BLT Test");
            Log.Info("Staring read blt test");

            try
            {
                _ATX1.Program.readBLTTest();
            }
            catch (Exception ex)
            {
                powerOffCRPS();
                Bin = 10630101;
                Log.Error("Disaster : Read BLT test failed.");
                Log.Error(ex.Message);
                obj.SetDutResult(Bin);
                obj.EndDut();
                obj.EndLot();
                copytoHost();
                return Bin;
            }

            Log.Info("Done : Read BLT Test");
            obj.EndTest(true);
            Log.Info("======================================================================");

            #region PS_ON Test
            obj.StartTest("GPIO test - enable or disable PS_ON");
            Log.Info("Starting DVM test for GPIO controlled PS_ON Signal -> Disabled");
            gpio_en = new bool[9] { true, false, true, false, false, false, false, false, false };
            setGPIO(gpio_en);
            Thread.Sleep(5000);

            Log.Info("Reading 20V output to verify PS_ON is disabled");
            try
            {
                read_20V(0.0);
            }
            catch (Exception ex)
            {
                powerOffCRPS();
                Bin = 10630202;
                Log.Error("Disaster : Disable PS_ON test failed.");
                Log.Error(ex.Message);
                obj.SetDutResult(Bin);
                obj.EndDut();
                obj.EndLot();
                copytoHost();
                return Bin;
            }

            obj.StartTest("GPIO test - enable or disable PS_ON");
            Log.Info("Starting DVM test for GPIO controlled PS_ON Signal -> Enabled");
            gpio_en = new bool[9] { false, false, false, false, true, false, false, false, false };
            setGPIO(gpio_en);
            Thread.Sleep(5000);

            Log.Info("Reading 20V output to verify PS_ON is enabled");
            try
            {
                read_20V(10.0);
            }
            catch (Exception ex)
            {
                powerOffCRPS();
                Bin = 10630201;
                Log.Error("Disaster : Enable PS_ON test failed.");
                Log.Error(ex.Message);
                obj.SetDutResult(Bin);
                obj.EndDut();
                obj.EndLot();
                copytoHost();
                return Bin;
            }

            #endregion PS_ON Test

            #region 20v_test
            obj.StartTest("GPIO test - enable or disable 20V");
            // Setting GPIO to read 20V output
            Log.Info("Starting DVM test for GPIO-based 20V connectors - disabled");
            gpio_en = new bool[9] { true, false, true, false, true, false, false, false, false };
            setGPIO(gpio_en);
            Thread.Sleep(5000);

            Log.Info("Reading 20V output to verify output is disabled");
            try
            {
                read_20V(0.0);
            }
            catch (Exception ex)
            {
                powerOffCRPS();
                Bin = 10630203;
                Log.Error("Disaster: Disable 20v power rail using GPIO failed.");
                Log.Error(ex.Message);
                obj.SetDutResult(Bin);
                obj.EndDut();
                obj.EndLot();
                copytoHost();
                return Bin;
            }

            Log.Info("Starting DVM test for GPIO-based 20V connectors - enabled");
            gpio_en = new bool[9] { false, false, false, false, true, false, false, false, false };
            setGPIO(gpio_en);
            Thread.Sleep(5000);

            Log.Info("Reading 20V output to verify output is enabled");
            try
            {
                read_20V(10.0);
            }
            catch (Exception ex)
            {
                powerOffCRPS();
                Bin = 10630204;
                Log.Error("Disaster: Enable 20v power rail using GPIO failed.");
                Log.Error(ex.Message);
                obj.SetDutResult(Bin);
                obj.EndDut();
                obj.EndLot();
                copytoHost();
                return Bin;
            }

            obj.EndTest(true);
            #endregion 20v_test  

            #region 12V_AUX_test
            obj.StartTest("GPIO test - enable or disable 20V");
            // Setting GPIO to read 20V output
            Log.Info("Starting DVM test for GPIO-based 12V AUX connectors - disabled");
            gpio_en = new bool[9] { true, true, true, true, true, false, false, false, false };
            setGPIO(gpio_en);
            Thread.Sleep(5000);

            Log.Info("Reading 12V AUX output to verify output is disabled");
            try
            {
                read_12vAUX(0.0);
            }
            catch (Exception ex)
            {
                powerOffCRPS();
                Bin = 10630206;
                Log.Error("Disaster: Disable 12V AUX using GPIO failed.");
                Log.Error(ex.Message);
                obj.SetDutResult(Bin);
                obj.EndDut();
                obj.EndLot();
                copytoHost();
                return Bin;
            }

            Log.Info("Starting DVM test for GPIO-based 12V AUX connectors - enabled");
            gpio_en = new bool[9] { false, false, false, false, true, false, false, false, false };
            setGPIO(gpio_en);
            Thread.Sleep(5000);

            Log.Info("Reading 12V AUX output to verify output is enabled");
            try
            {
                read_12vAUX(12.0);
            }
            catch (Exception ex)
            {
                powerOffCRPS();
                Bin = 10630205;
                Log.Error("Disaster: Enable 12V AUX using GPIO failed..");
                Log.Error(ex.Message);
                obj.SetDutResult(Bin);
                obj.EndDut();
                obj.EndLot();
                copytoHost();
                return Bin;
            }

            obj.EndTest(true);
            #endregion 12V_AUX_test

            obj.StartTest("12V Aux and PWR_OK Signals Test");
            Log.Info("Starting DVM test for 12V Aux and PWR_OK Signals >> Connected to 2x2 Connectors");

            string[] DIOchannel_12V = new string[6] { DO_Channel.DO_0, DO_Channel.DO_1, DO_Channel.DO_2, DO_Channel.DO_3, DO_Channel.DO_4,
                DO_Channel.DO_5};

            DIOState[] ADMMapping = new DIOState[6] { DIOState.LOW, DIOState.LOW, DIOState.LOW,
                                                      DIOState.LOW,DIOState.LOW,DIOState.LOW};

            var auxChannels = new string[3] { "0", "1", "4" }; // 0,1 = 12V | 4 = PWR_OK

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
                            throw new Exception($"DVM for channel {index} failed." + " Expected : " + expected + " Current -> " + DVM);
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
                return Bin;
            }
            obj.EndTest(true);


            obj.StartTest("12VO ,12V CPU and 20V connectors Test");
            // 12VO ,12V CPU and 20V connectors
            Log.Info("Starting DVM test for MPDU 12VO ,12V CPU and 20V connectors");

            DIOState[] FanMapping = new DIOState[6] { DIOState.HIGH, DIOState.HIGH, DIOState.HIGH,
                                                      DIOState.HIGH,DIOState.HIGH,DIOState.HIGH};

            string[] fanChannels = new string[10] { "0", "1", "2", "3", "4", "5", "6", "7", "9", "10" };

            for (int j = 0; j < 6; j++)
            {
                Log.Info("Setting DIO " + j + " to " + FanMapping[j].ToString());
                STDIO.SetDIO(DIOchannel_12V[j], (byte)FanMapping[j]);
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
                return Bin;
            }
            obj.EndTest(true);

            Log.Info("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");


            #region Fan Tach Read Test
            obj.StartTest("Fan Tach Read Test");
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
                                if (number < 367) // todo : check value for POR fan
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
                return Bin;

            }
            obj.EndTest(true);
            #endregion Fan Tach Read Test
            Log.Info("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");

            #region Read GPIO PWROK Test
            obj.StartTest("GPIO 5 PWROK Read Test");

            Log.Info("Starting GPIO 5 - PWROK Read Test");

            try
            {
                string vals = gpioVal();
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
                powerOffCRPS();
                Bin = 10630210;
                Log.Error("Disaster : GPIO 5 - PWROK read test failed");
                Log.Error(ex.Message);
                obj.SetDutResult(Bin);
                obj.EndDut();
                obj.EndLot();
                copytoHost();
                return Bin;
            }
            obj.EndTest(true);

            Log.Info("\n");
            #endregion Read GPIO PWROK Test


            #region Default output is set to 000010000
            Log.Info("Reset GPIO to turn on upon CRPS is connected - GPIO 4 is High");
            gpio_en = new bool[9] { false, false, false, false, true, false, false, false, false };
            configure_GPIO(gpio_en);
            Thread.Sleep(1000);
            configure_GPIO_NVRAM(gpio_en);//nvram
            Thread.Sleep(3000);

            #endregion
            Log.Info("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%");

            Bin = 01000000;
            
            Log.Info($"Bagus : FT for MPDU ATX1 Fab B Completed with Bin : {Bin} ");
            obj.SetDutResult(Bin);
            obj.EndDut();
            obj.EndLot();
            copytoHost();
            powerOffCRPS();
            Log.Info("%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%");
            return Bin;

        }

    }
}
