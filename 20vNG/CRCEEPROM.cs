using System;
using System.Collections.Generic;
using System.Linq;
//using System.ServiceModel;


namespace CrcNamespace
{
    //[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    //public abstract class SingletonBase : MarshalByRefObject { }

    //[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    //public abstract class InstanceBase : MarshalByRefObject { }

    public class SingletonBase : MarshalByRefObject { }
    public class CRCEEPROM : SingletonBase
    {
        //internal static IContextLog log = ContextLogManager.GetLogger("EEPROM");
        // This object should never be used outside this class
        //internal eepromCache EEPROM_Cache = new eepromCache();

        #region CRC Table
        private readonly ushort[] _crc_table = {
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
        #endregion

        //#region CCReadEEPROM
        //public byte[] CCReadEEPROM(BLTBUS bus, byte addr, EEPROMSize deviceSize, ushort offset, ushort length, bool USECRC16)
        //{
        //    //log.DebugFormat("CCReadEEPROM --- bus: {0}, addr: {1}, devSize: {2}, offset: {3}, length: {4}", bus, addr, (int)deviceSize, offset, length);
        //    ushort temp_size;
        //    ushort temp_offset;
        //    CheckSizeAndDoCRCSizing(deviceSize, offset, length, USECRC16, out temp_offset, out temp_size);

        //    byte[] temp_data = _CCReadEEPROM(bus, addr, deviceSize, temp_offset, temp_size);

        //    if (USECRC16)
        //    {
        //        //verify the checksum
        //        byte[] line = new byte[14];
        //        for (int i = 0; i < temp_size; i += 16)
        //        {
        //            Array.Copy(temp_data, i, line, 0, 14);
        //            byte[] crc = CalculateCRC(line);

        //            if (temp_data[i + 14] != crc[0] || temp_data[i + 15] != crc[1])
        //            {
        //                throw new BadChecksumException();
        //            }
        //        }

        //        byte[] bdata = new byte[length];

        //        Array.Copy(temp_data, offset - temp_offset, bdata, 0, length);

        //        return bdata;
        //    }
        //    return temp_data;
        //}

        //private byte[] _CCReadEEPROM(BLTBUS bus, byte addr, EEPROMSize deviceSize, ushort offset, ushort length)
        //{
        //    //log.DebugFormat("_CCReadEEPROM --- bus: {0}, addr: {1}, devSize: {2}, offset: {3}, length: {4}", bus, addr, (int)deviceSize, offset, length);
        //    eepromCache.eepromCacheRecord cacheRecord = EEPROM_Cache.getCacheRecord(bus, addr, deviceSize);
        //    bool eepromReadReady = false;
        //    byte[] outData = new byte[length];
        //    ISMBUSCommon smbus = ServiceLocator.Get<ISMBUSCommon>();
        //    for (int i = 0; i < length; i++)
        //    {
        //        ushort cur_offset = (ushort)(offset + i);
        //        if (cacheRecord.cacheValid[cur_offset] == false)
        //        {
        //            if (eepromReadReady == false)
        //            {
        //                try
        //                {
        //                    //check number of bytes for addressing
        //                    if ((int)deviceSize > 0x100)
        //                    {
        //                        //log.DebugFormat("ByteDataWrite --- bus: {0}, addr: {1}, offset: {2}", bus, addr, cur_offset);
        //                        smbus.ByteDataWrite(bus, addr, (byte)(cur_offset >> 8), (byte)(cur_offset & 0xff));
        //                    }
        //                    else
        //                    {
        //                        //log.DebugFormat("ByteWrite --- bus: {0}, addr: {1}, offset: {2}", bus, addr, cur_offset);
        //                        smbus.ByteWrite(bus, addr, (byte)(cur_offset & 0xff));
        //                    }
        //                }
        //                catch (Exception e)
        //                {
        //                    throw new DummyWriteFailedException("", e);
        //                }
        //                eepromReadReady = true;
        //            }

        //            try
        //            {
        //                smbus.ByteRead(bus, addr, out outData[i]);
        //                cacheRecord.cacheData[cur_offset] = outData[i];
        //                cacheRecord.cacheValid[cur_offset] = true;
        //            }
        //            catch (Exception e)
        //            {
        //                throw new RdWrIoctlException("", e);
        //            }

        //        }
        //        else
        //        {
        //            outData[i] = cacheRecord.cacheData[cur_offset];
        //        }
        //    }
        //    return outData;
        //}
        //#endregion

        //#region CCWriteEEPROM
        //public void CCWriteEEPROM(BLTBUS bus, byte addr, EEPROMSize deviceSize, ushort offset, ushort length, byte[] bdata, bool USECRC16)
        //{
        //    if (addr == 0xA0 && CCPricelineNegotiator.GetCCType() == CSState.CC3)
        //    {
        //        throw new WritePreventedException();
        //    }

        //    ushort temp_size;
        //    ushort temp_offset;
        //    CheckSizeAndDoCRCSizing(deviceSize, offset, length, USECRC16, out temp_offset, out temp_size);

        //    byte[] temp_data;

        //    if (USECRC16)
        //    {
        //        temp_data = _CCReadEEPROM(bus, addr, deviceSize, temp_offset, temp_size);

        //        for (int i = 0; i < length; i++)
        //        {
        //            temp_data[i + offset - temp_offset] = bdata[i];
        //        }

        //        byte[] line = new byte[14];
        //        for (int i = 0; i < temp_size; i += 16)
        //        {
        //            Array.Copy(temp_data, i, line, 0, 14);
        //            byte[] crc = CalculateCRC(line);

        //            temp_data[i + 14] = crc[0];
        //            temp_data[i + 15] = crc[1];
        //        }
        //    }
        //    else
        //    {
        //        temp_data = bdata;
        //    }

        //    _CCWriteEEPROM(bus, addr, deviceSize, temp_offset, temp_data);
        //}

        //private void _CCWriteEEPROM(BLTBUS bus, byte addr, EEPROMSize deviceSize, ushort offset, byte[] data)
        //{
        //    eepromCache.eepromCacheRecord cacheRecord = EEPROM_Cache.getCacheRecord(bus, addr, deviceSize);
        //    ISMBUSCommon smbus = ServiceLocator.Get<ISMBUSCommon>();
        //    try
        //    {
        //        for (int i = 0; i < data.Length; i++)
        //        {
        //            ushort cur_offset = (ushort)(offset + i);
        //            cacheRecord.cacheData[cur_offset] = data[i];
        //            cacheRecord.cacheValid[cur_offset] = false;
        //            if ((int)deviceSize > 0x100)
        //            {
        //                smbus.WordDataWrite(bus, addr, (byte)((cur_offset >> 8) & 0xff), (byte)(cur_offset & 0xff), data[i]);
        //            }
        //            else
        //            {
        //                smbus.ByteDataWrite(bus, addr, (byte)(cur_offset & 0xff), data[i]);
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        throw new RdWrIoctlException("", e);
        //    }
        //}
        //#endregion

        //#region CheckSizeAndDoCRCSizing
        //private void CheckSizeAndDoCRCSizing(EEPROMSize deviceSize, ushort offset, ushort size, bool USECRC16, out ushort temp_offset, out ushort temp_size)
        //{
        //    if (offset + size > (int)deviceSize)
        //    {
        //        throw new PageSizeExceededException();
        //    }

        //    //rounding values to 16 byte aligned values to check CRCs
        //    if (USECRC16)
        //    {
        //        temp_offset = (ushort)(offset - (offset % 16));

        //        ushort end_offset = (ushort)(offset + size);

        //        if ((end_offset) % 16 != 0)
        //        {
        //            //round to 16 byte aligned
        //            end_offset = (ushort)((end_offset + 16) / 16 * 16);
        //        }
        //        temp_size = (ushort)(end_offset - temp_offset);
        //    }
        //    else
        //    {
        //        temp_offset = offset;
        //        temp_size = size;
        //    }
        //}
        //#endregion

        #region CalculateCRC
        /// <summary>
        /// This function takes a 14 byte line to or from the eeprom, and calculates the crc
        /// </summary>
        /// <param name="line">the first 14 bytes of a 16 byte aligned eeprom value</param>
        /// <returns>checksum bytes, byte[0] == crc at line[14], byte[1] == crc at line[15]</returns>

        public byte[] CalculateCRC(byte[] line)
        {
            if (line.Length != 14)
            {
                throw new Exception("EEPROM: CRC Length is incorrect");
            }

            byte[] ret_crc = new byte[2];

            ushort crc = line.Aggregate<byte, ushort>(0xffff, (current, t) => (ushort) ((current >> 8) ^ _crc_table[((current ^ t) & 0xff)]));

            ret_crc[0] = (byte)(crc & 0xff);
            ret_crc[1] = (byte)((crc >> 8) & 0xff);
            return ret_crc;
        }
        #endregion

        //#region eepromCache
        //public class eepromCache
        //{
        //    private readonly List<eepromCacheRecord> eepromCacheRecords = new List<eepromCacheRecord>();

        //    public eepromCacheRecord getCacheRecord(BLTBUS bus, byte addr, EEPROMSize deviceSize)
        //    {
        //        foreach (eepromCacheRecord record in eepromCacheRecords)
        //        {
        //            if ((record.eepromBus == bus) && (record.eepromAddr == addr) && (record.eepromSize == ((int)deviceSize)))
        //            {
        //                return record;
        //            }
        //        }

        //        eepromCacheRecord newRecord = new eepromCacheRecord(bus, addr, deviceSize);
        //        eepromCacheRecords.Add(newRecord);

        //        return newRecord;
        //    }

        //    public class eepromCacheRecord
        //    {
        //        public BLTBUS eepromBus { get; set; }
        //        public byte eepromAddr { get; set; }
        //        public int eepromSize { get; set; }
        //        public byte[] cacheData;
        //        public bool[] cacheValid;

        //        public eepromCacheRecord(BLTBUS bus, byte addr, EEPROMSize deviceSize)
        //        {
        //            eepromBus = bus;
        //            eepromAddr = addr;
        //            eepromSize = (int)deviceSize;
        //            cacheData = new byte[(int)deviceSize];
        //            cacheValid = new bool[(int)deviceSize];
        //            for (int i = 0; i < cacheData.Length; i++)
        //            {
        //                cacheData[i] = 0xff;
        //                cacheValid[i] = false;
        //            }
        //            //theLog.Warn("Finished creating EEPROM Record");
        //        }
        //    }
        //}
        //#endregion
    }
}
