using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MCP2210;
using Sttd;

namespace BLT
{
    public class BLT_Access
    {
        public static byte[] ReadBLT(IEepromMemory eeprom)
        {
            Console.WriteLine("*** Reading BLT EEPROM: ");
            byte[] dataIn = Enumerable.Repeat((byte)0x00, 0x80).ToArray(); //FF = 256 || 80=128
            try
            {
                byte addr = 0x00;

                for (byte i = 0x00; i < 0x80; i++)
                {
                    dataIn[i] = eeprom.ReadAddress(i);

                    if ((i & 0x0f) == 0)
                    {
                        Console.Write("\n{0:X4}:  ", addr + i);
                        //Log.Info($"\n{addr + i:X4}:  ");
                    }

                    Console.Write("{0:X2} ", dataIn[i]);
                    //Log.Info($"{dataIn[i]:X2} ");


                    if (((i + 1) & 0x07) == 0)
                    {
                        Console.Write(" ");
                        //Log.Info(" ");
                    }

                }
                Console.WriteLine();
                Log.Info("\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw new Exception("Read BLT failed. Recheck cable connection or power to the board.");
            }

            return dataIn;

        }

        public static void WriteBLT(IEepromMemory eeprom,byte address, byte data)
        {
            byte reply;
            
            try
            {
                reply = eeprom.WriteAddress(address, data);
                Console.WriteLine($"BLT write completed successfully with reply : {reply:X2} ");
                Log.Info($"BLT write completed successfully with reply : {reply:X2} ");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Log.Error(ex.ToString());
                throw new Exception($"Write BLT failed for addr : {address}. Recheck cable connection or power to the board.");
            }            
        }

        public static void ConfigureMCP(IVolatileRam nvram)
        {
            //read current USB SPI setting to verify the password protected nature           
            ChipSettings readSettings = nvram.ReadChipConfiguration(); // read curent setting

            Console.WriteLine($"Access Control is: {readSettings.AccessControl}");


            // set new chip setting with no password protection
            ChipSettings chipSettings = new ChipSettings();
            chipSettings.InterruptBitMode = DedicatedFunction.NoInterruptCounting;
            chipSettings.RemoteWakeUpEnabled = true;
            chipSettings.SpiBusReleaseEnable = true;

            // these are never used by the volatile RAM
            chipSettings.AccessControl = NramChipAccessControl.NotProtected;

            chipSettings.PinDirections = new PinDirection[] {
                    PinDirection.Output,
                    PinDirection.Input,
                    PinDirection.Output,
                    PinDirection.Output,
                    PinDirection.Input,
                    PinDirection.Input,
                    PinDirection.Output,
                    PinDirection.Output,
                    PinDirection.Output
                };

            chipSettings.PinModes = new PinMode[] {
                    PinMode.GPIO,
                    PinMode.GPIO,
                    PinMode.GPIO,
                    PinMode.GPIO,
                    PinMode.GPIO,
                    PinMode.GPIO,
                    PinMode.GPIO,
                    PinMode.GPIO,
                    PinMode.GPIO
                };

            chipSettings.DefaultOutput = new bool[] {
                    true,
                    true,
                    false,
                    false,
                    false,
                    false,
                    false,
                    true,
                    true
                };

            try
            {
                nvram.ConfigureChip(chipSettings);
            }
            catch (Exception ex)
            {
                nvram.ConfigureChip(readSettings); // revert to previous programming
                Console.WriteLine(ex);

            }
        }
    }
}
