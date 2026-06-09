/*=========================================================================
| Aardvark Interface Library - CORRECTED VERSION
|--------------------------------------------------------------------------
| Copyright (c) 2003-2024 Total Phase, Inc.
| All rights reserved.
| www.totalphase.com
|
| MODIFICATIONS:
| - Fixed P/Invoke declarations to use correct EntryPoint with stdcall
|   name decoration (e.g., "net_aa_find_devices_ext@16")
| - All original functionality and constants preserved
| - Compatible with 32-bit aardvark.dll (must compile as x86)
|
| CRITICAL: Your project must be compiled for x86 platform target!
|
| Original license terms apply (see below)
|--------------------------------------------------------------------------
| Redistribution and use of this file in source and binary forms, with
| or without modification, are permitted provided that the following
| conditions are met:
|
| - Redistributions of source code must retain the above copyright
|   notice, this list of conditions, and the following disclaimer.
|
| - Redistributions in binary form must reproduce the above copyright
|   notice, this list of conditions, and the following disclaimer in the
|   documentation and other materials provided with the distribution.
|
| - This file must only be used to interface with Total Phase products.
|   The names of Total Phase and its contributors must not be used to
|   endorse or promote products derived from this software.
|
| THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
| "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING BUT NOT
| LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
| FOR A PARTICULAR PURPOSE, ARE DISCLAIMED.  IN NO EVENT WILL THE
| COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
| INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING
| BUT NOT LIMITED TO PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
| LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
| CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
| LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
| ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
| POSSIBILITY OF SUCH DAMAGE.
 ========================================================================*/

using System;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitleAttribute("Aardvark .NET binding (Corrected)")]
[assembly: AssemblyDescriptionAttribute(".NET binding for Aardvark with fixed P/Invoke")]
[assembly: AssemblyCompanyAttribute("Total Phase, Inc.")]
[assembly: AssemblyProductAttribute("Aardvark")]
[assembly: AssemblyCopyrightAttribute("Total Phase, Inc. 2024")]

namespace TotalPhase {

public enum AardvarkStatus : int {
    /* General codes (0 to -99) */
    AA_OK                       =    0,
    AA_UNABLE_TO_LOAD_LIBRARY   =   -1,
    AA_UNABLE_TO_LOAD_DRIVER    =   -2,
    AA_UNABLE_TO_LOAD_FUNCTION  =   -3,
    AA_INCOMPATIBLE_LIBRARY     =   -4,
    AA_INCOMPATIBLE_DEVICE      =   -5,
    AA_COMMUNICATION_ERROR      =   -6,
    AA_UNABLE_TO_OPEN           =   -7,
    AA_UNABLE_TO_CLOSE          =   -8,
    AA_INVALID_HANDLE           =   -9,
    AA_CONFIG_ERROR             =  -10,

    /* I2C codes (-100 to -199) */
    AA_I2C_NOT_AVAILABLE        = -100,
    AA_I2C_NOT_ENABLED          = -101,
    AA_I2C_READ_ERROR           = -102,
    AA_I2C_WRITE_ERROR          = -103,
    AA_I2C_SLAVE_BAD_CONFIG     = -104,
    AA_I2C_SLAVE_READ_ERROR     = -105,
    AA_I2C_SLAVE_TIMEOUT        = -106,
    AA_I2C_DROPPED_EXCESS_BYTES = -107,
    AA_I2C_BUS_ALREADY_FREE     = -108,

    /* SPI codes (-200 to -299) */
    AA_SPI_NOT_AVAILABLE        = -200,
    AA_SPI_NOT_ENABLED          = -201,
    AA_SPI_WRITE_ERROR          = -202,
    AA_SPI_SLAVE_READ_ERROR     = -203,
    AA_SPI_SLAVE_TIMEOUT        = -204,
    AA_SPI_DROPPED_EXCESS_BYTES = -205,

    /* GPIO codes (-400 to -499) */
    AA_GPIO_NOT_AVAILABLE       = -400
}

public enum AardvarkConfig : int {
    AA_CONFIG_GPIO_ONLY = 0x00,
    AA_CONFIG_SPI_GPIO  = 0x01,
    AA_CONFIG_GPIO_I2C  = 0x02,
    AA_CONFIG_SPI_I2C   = 0x03,
    AA_CONFIG_QUERY     = 0x80
}

public enum AardvarkI2cFlags : int {
    AA_I2C_NO_FLAGS          = 0x00,
    AA_I2C_10_BIT_ADDR       = 0x01,
    AA_I2C_COMBINED_FMT      = 0x02,
    AA_I2C_NO_STOP           = 0x04,
    AA_I2C_SIZED_READ        = 0x10,
    AA_I2C_SIZED_READ_EXTRA1 = 0x20
}

public enum AardvarkI2cStatus : int {
    AA_I2C_STATUS_OK            = 0,
    AA_I2C_STATUS_BUS_ERROR     = 1,
    AA_I2C_STATUS_SLA_ACK       = 2,
    AA_I2C_STATUS_SLA_NACK      = 3,
    AA_I2C_STATUS_DATA_NACK     = 4,
    AA_I2C_STATUS_ARB_LOST      = 5,
    AA_I2C_STATUS_BUS_LOCKED    = 6,
    AA_I2C_STATUS_LAST_DATA_ACK = 7
}

public enum AardvarkSpiPolarity : int {
    AA_SPI_POL_RISING_FALLING = 0,
    AA_SPI_POL_FALLING_RISING = 1
}

public enum AardvarkSpiPhase : int {
    AA_SPI_PHASE_SAMPLE_SETUP = 0,
    AA_SPI_PHASE_SETUP_SAMPLE = 1
}

public enum AardvarkSpiBitorder : int {
    AA_SPI_BITORDER_MSB = 0,
    AA_SPI_BITORDER_LSB = 1
}

public enum AardvarkSpiSSPolarity : int {
    AA_SPI_SS_ACTIVE_LOW  = 0,
    AA_SPI_SS_ACTIVE_HIGH = 1
}

public enum AardvarkGpioBits : int {
    AA_GPIO_SCL  = 0x01,
    AA_GPIO_SDA  = 0x02,
    AA_GPIO_MISO = 0x04,
    AA_GPIO_SCK  = 0x08,
    AA_GPIO_MOSI = 0x10,
    AA_GPIO_SS   = 0x20
}


public class AardvarkApi {

/*=========================================================================
| HELPER FUNCTIONS / CLASSES
 ========================================================================*/
static long tp_min(long x, long y) { return x < y ? x : y; }

private class GCContext {
    GCHandle[] handles;
    int index;
    public GCContext () {
        handles = new GCHandle[16];
        index   = 0;
    }
    public void add (GCHandle gch) {
        handles[index] = gch;
        index++;
    }
    public void free () {
        while (index != 0) {
            index--;
            handles[index].Free();
        }
    }
}

/*=========================================================================
| VERSION
 ========================================================================*/
[DllImport ("aardvark")]
private static extern int aa_c_version ();

public const int AA_API_VERSION    = 0x0600;   // v6.00
public const int AA_REQ_SW_VERSION = 0x050a;   // v5.10

private static short AA_SW_VERSION;
private static short AA_REQ_API_VERSION;
private static bool  AA_LIBRARY_LOADED;

static AardvarkApi () {
    AA_SW_VERSION      = (short)(aa_c_version() & 0xffff);
    AA_REQ_API_VERSION = (short)((aa_c_version() >> 16) & 0xffff);
    AA_LIBRARY_LOADED  = 
        ((AA_SW_VERSION >= AA_REQ_SW_VERSION) &&
         (AA_API_VERSION >= AA_REQ_API_VERSION));
}

/*=========================================================================
| GENERAL TYPE DEFINITIONS
 ========================================================================*/
[StructLayout(LayoutKind.Sequential)]
public struct AardvarkVersion {
    /* Software, firmware, and hardware versions. */
    public ushort software;
    public ushort firmware;
    public ushort hardware;

    /* Firmware requires that software must be >= this version. */
    public ushort sw_req_by_fw;

    /* Software requires that firmware must be >= this version. */
    public ushort fw_req_by_sw;

    /* Software requires that the API interface must be >= this version. */
    public ushort api_req_by_sw;
}

[StructLayout(LayoutKind.Sequential)]
public struct AardvarkExt {
    /* Version matrix */
    public AardvarkVersion version;

    /* Features of this device. */
    public int             features;
}


/*=========================================================================
| GENERAL API
 ========================================================================*/
public const ushort AA_PORT_NOT_FREE = 0x8000;
public static int aa_find_devices (
    int       num_devices,
    ushort[]  devices
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    int devices_num_devices = (int)tp_min(num_devices, devices.Length);
    return net_aa_find_devices(devices_num_devices, devices);
}

public static int aa_find_devices_ext (
    int       num_devices,
    ushort[]  devices,
    int       num_ids,
    uint[]    unique_ids
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    int devices_num_devices = (int)tp_min(num_devices, devices.Length);
    int unique_ids_num_ids = (int)tp_min(num_ids, unique_ids.Length);
    return net_aa_find_devices_ext(devices_num_devices, devices, unique_ids_num_ids, unique_ids);
}

public static int aa_open (
    int  port_number
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_open(port_number);
}

public static int aa_open_ext (
    int              port_number,
    ref AardvarkExt  aa_ext
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_open_ext(port_number, ref aa_ext);
}

public static int aa_close (
    int  aardvark
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_close(aardvark);
}

public static int aa_port (
    int  aardvark
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_port(aardvark);
}

public const int AA_FEATURE_SPI = 0x00000001;
public const int AA_FEATURE_I2C = 0x00000002;
public const int AA_FEATURE_GPIO = 0x00000008;
public static int aa_features (
    int  aardvark
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_features(aardvark);
}

public static uint aa_unique_id (
    int  aardvark
)
{
    if (!AA_LIBRARY_LOADED) return 0;
    return net_aa_unique_id(aardvark);
}

public static string aa_status_string (
    int  status
)
{
    if (!AA_LIBRARY_LOADED) return null;
    return Marshal.PtrToStringAnsi(net_aa_status_string(status));
}

public const int AA_LOG_STDOUT = 1;
public const int AA_LOG_STDERR = 2;
public static int aa_log (
    int  aardvark,
    int  level,
    int  handle
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_log(aardvark, level, handle);
}

public static int aa_version (
    int                  aardvark,
    ref AardvarkVersion  version
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_version(aardvark, ref version);
}

public const int AA_CONFIG_SPI_MASK = 0x00000001;
public const int AA_CONFIG_I2C_MASK = 0x00000002;
public static int aa_configure (
    int             aardvark,
    AardvarkConfig  config
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_configure(aardvark, config);
}

public const byte AA_TARGET_POWER_NONE = 0x00;
public const byte AA_TARGET_POWER_BOTH = 0x03;
public const byte AA_TARGET_POWER_QUERY = 0x80;
public static int aa_target_power (
    int   aardvark,
    byte  power_mask
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_target_power(aardvark, power_mask);
}

public static uint aa_sleep_ms (
    uint  milliseconds
)
{
    if (!AA_LIBRARY_LOADED) return 0;
    return net_aa_sleep_ms(milliseconds);
}


/*=========================================================================
| ASYNC MESSAGE POLLING
 ========================================================================*/
public const int AA_ASYNC_NO_DATA = 0x00000000;
public const int AA_ASYNC_I2C_READ = 0x00000001;
public const int AA_ASYNC_I2C_WRITE = 0x00000002;
public const int AA_ASYNC_SPI = 0x00000004;
public static int aa_async_poll (
    int  aardvark,
    int  timeout
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_async_poll(aardvark, timeout);
}


/*=========================================================================
| I2C API
 ========================================================================*/
public static int aa_i2c_free_bus (
    int  aardvark
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_i2c_free_bus(aardvark);
}

public static int aa_i2c_bitrate (
    int  aardvark,
    int  bitrate_khz
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_i2c_bitrate(aardvark, bitrate_khz);
}

public static int aa_i2c_bus_timeout (
    int     aardvark,
    ushort  timeout_ms
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_i2c_bus_timeout(aardvark, timeout_ms);
}

public static int aa_i2c_read (
    int               aardvark,
    ushort            slave_addr,
    AardvarkI2cFlags  flags,
    ushort            num_bytes,
    byte[]            data_in
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    ushort data_in_num_bytes = (ushort)tp_min(num_bytes, data_in.Length);
    return net_aa_i2c_read(aardvark, slave_addr, flags, data_in_num_bytes, data_in);
}

public static int aa_i2c_read_ext (
    int               aardvark,
    ushort            slave_addr,
    AardvarkI2cFlags  flags,
    ushort            num_bytes,
    byte[]            data_in,
    ref ushort        num_read
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    ushort data_in_num_bytes = (ushort)tp_min(num_bytes, data_in.Length);
    return net_aa_i2c_read_ext(aardvark, slave_addr, flags, data_in_num_bytes, data_in, ref num_read);
}

public static int aa_i2c_write (
    int               aardvark,
    ushort            slave_addr,
    AardvarkI2cFlags  flags,
    ushort            num_bytes,
    byte[]            data_out
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    ushort data_out_num_bytes = (ushort)tp_min(num_bytes, data_out.Length);
    return net_aa_i2c_write(aardvark, slave_addr, flags, data_out_num_bytes, data_out);
}

public static int aa_i2c_write_ext (
    int               aardvark,
    ushort            slave_addr,
    AardvarkI2cFlags  flags,
    ushort            num_bytes,
    byte[]            data_out,
    ref ushort        num_written
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    ushort data_out_num_bytes = (ushort)tp_min(num_bytes, data_out.Length);
    return net_aa_i2c_write_ext(aardvark, slave_addr, flags, data_out_num_bytes, data_out, ref num_written);
}

public static int aa_i2c_write_read (
    int               aardvark,
    ushort            slave_addr,
    AardvarkI2cFlags  flags,
    ushort            out_num_bytes,
    byte[]            out_data,
    ref ushort        num_written,
    ushort            in_num_bytes,
    byte[]            in_data,
    ref ushort        num_read
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    ushort out_data_out_num_bytes = (ushort)tp_min(out_num_bytes, out_data.Length);
    ushort in_data_in_num_bytes = (ushort)tp_min(in_num_bytes, in_data.Length);
    return net_aa_i2c_write_read(aardvark, slave_addr, flags, out_data_out_num_bytes, out_data, ref num_written, in_data_in_num_bytes, in_data, ref num_read);
}

public static int aa_i2c_slave_enable (
    int     aardvark,
    byte    addr,
    ushort  maxTxBytes,
    ushort  maxRxBytes
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_i2c_slave_enable(aardvark, addr, maxTxBytes, maxRxBytes);
}

public static int aa_i2c_slave_disable (
    int  aardvark
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_i2c_slave_disable(aardvark);
}

public static int aa_i2c_slave_set_response (
    int     aardvark,
    byte    num_bytes,
    byte[]  data_out
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    byte data_out_num_bytes = (byte)tp_min(num_bytes, data_out.Length);
    return net_aa_i2c_slave_set_response(aardvark, data_out_num_bytes, data_out);
}

public static int aa_i2c_slave_write_stats (
    int  aardvark
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_i2c_slave_write_stats(aardvark);
}

public static int aa_i2c_slave_read (
    int       aardvark,
    ref byte  addr,
    ushort    num_bytes,
    byte[]    data_in
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    ushort data_in_num_bytes = (ushort)tp_min(num_bytes, data_in.Length);
    return net_aa_i2c_slave_read(aardvark, ref addr, data_in_num_bytes, data_in);
}

public static int aa_i2c_slave_write_stats_ext (
    int         aardvark,
    ref ushort  num_written
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_i2c_slave_write_stats_ext(aardvark, ref num_written);
}

public static int aa_i2c_slave_read_ext (
    int         aardvark,
    ref byte    addr,
    ushort      num_bytes,
    byte[]      data_in,
    ref ushort  num_read
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    ushort data_in_num_bytes = (ushort)tp_min(num_bytes, data_in.Length);
    return net_aa_i2c_slave_read_ext(aardvark, ref addr, data_in_num_bytes, data_in, ref num_read);
}

public const byte AA_I2C_PULLUP_NONE = 0x00;
public const byte AA_I2C_PULLUP_BOTH = 0x03;
public const byte AA_I2C_PULLUP_QUERY = 0x80;
public static int aa_i2c_pullup (
    int   aardvark,
    byte  pullup_mask
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_i2c_pullup(aardvark, pullup_mask);
}


/*=========================================================================
| SPI API
 ========================================================================*/
public static int aa_spi_bitrate (
    int  aardvark,
    int  bitrate_khz
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_spi_bitrate(aardvark, bitrate_khz);
}

public static int aa_spi_configure (
    int                  aardvark,
    AardvarkSpiPolarity  polarity,
    AardvarkSpiPhase     phase,
    AardvarkSpiBitorder  bitorder
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_spi_configure(aardvark, polarity, phase, bitorder);
}

public static int aa_spi_write (
    int     aardvark,
    ushort  out_num_bytes,
    byte[]  data_out,
    ushort  in_num_bytes,
    byte[]  data_in
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    ushort data_out_out_num_bytes = (ushort)tp_min(out_num_bytes, data_out.Length);
    ushort data_in_in_num_bytes = (ushort)tp_min(in_num_bytes, data_in.Length);
    return net_aa_spi_write(aardvark, data_out_out_num_bytes, data_out, data_in_in_num_bytes, data_in);
}

public static int aa_spi_slave_enable (
    int  aardvark
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_spi_slave_enable(aardvark);
}

public static int aa_spi_slave_disable (
    int  aardvark
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_spi_slave_disable(aardvark);
}

public static int aa_spi_slave_set_response (
    int     aardvark,
    byte    num_bytes,
    byte[]  data_out
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    byte data_out_num_bytes = (byte)tp_min(num_bytes, data_out.Length);
    return net_aa_spi_slave_set_response(aardvark, data_out_num_bytes, data_out);
}

public static int aa_spi_slave_read (
    int     aardvark,
    ushort  num_bytes,
    byte[]  data_in
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    ushort data_in_num_bytes = (ushort)tp_min(num_bytes, data_in.Length);
    return net_aa_spi_slave_read(aardvark, data_in_num_bytes, data_in);
}

public static int aa_spi_master_ss_polarity (
    int                    aardvark,
    AardvarkSpiSSPolarity  polarity
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_spi_master_ss_polarity(aardvark, polarity);
}


/*=========================================================================
| GPIO API
 ========================================================================*/
public const byte AA_GPIO_DIR_INPUT = 0x00;
public const byte AA_GPIO_DIR_OUTPUT = 0x01;
public static int aa_gpio_direction (
    int   aardvark,
    byte  direction_mask
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_gpio_direction(aardvark, direction_mask);
}

public const byte AA_GPIO_PULLUP_OFF = 0;
public const byte AA_GPIO_PULLUP_ON = 1;
public static int aa_gpio_pullup (
    int   aardvark,
    byte  pullup_mask
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_gpio_pullup(aardvark, pullup_mask);
}

public static int aa_gpio_get (
    int  aardvark
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_gpio_get(aardvark);
}

public static int aa_gpio_set (
    int   aardvark,
    byte  value
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_gpio_set(aardvark, value);
}

public static int aa_gpio_change (
    int     aardvark,
    ushort  timeout
)
{
    if (!AA_LIBRARY_LOADED) return (int)AardvarkStatus.AA_INCOMPATIBLE_LIBRARY;
    return net_aa_gpio_change(aardvark, timeout);
}


/*=========================================================================
| NATIVE DLL BINDINGS - CORRECTED WITH ENTRYPOINT
 ========================================================================*/
[DllImport ("aardvark", EntryPoint = "net_aa_find_devices@8", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_find_devices (int num_devices, [Out] ushort[] devices);

[DllImport ("aardvark", EntryPoint = "net_aa_find_devices_ext@16", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_find_devices_ext (int num_devices, [Out] ushort[] devices, int num_ids, [Out] uint[] unique_ids);

[DllImport ("aardvark", EntryPoint = "net_aa_open@4", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_open (int port_number);

[DllImport ("aardvark", EntryPoint = "net_aa_open_ext@8", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_open_ext (int port_number, ref AardvarkExt aa_ext);

[DllImport ("aardvark", EntryPoint = "net_aa_close@4", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_close (int aardvark);

[DllImport ("aardvark", EntryPoint = "net_aa_port@4", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_port (int aardvark);

[DllImport ("aardvark", EntryPoint = "net_aa_features@4", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_features (int aardvark);

[DllImport ("aardvark", EntryPoint = "net_aa_unique_id@4", CallingConvention = CallingConvention.StdCall)]
private static extern uint net_aa_unique_id (int aardvark);

[DllImport ("aardvark", EntryPoint = "net_aa_status_string@4", CallingConvention = CallingConvention.StdCall)]
private static extern IntPtr net_aa_status_string (int status);

[DllImport ("aardvark", EntryPoint = "net_aa_log@12", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_log (int aardvark, int level, int handle);

[DllImport ("aardvark", EntryPoint = "net_aa_version@8", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_version (int aardvark, ref AardvarkVersion version);

[DllImport ("aardvark", EntryPoint = "net_aa_configure@8", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_configure (int aardvark, AardvarkConfig config);

[DllImport ("aardvark", EntryPoint = "net_aa_target_power@8", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_target_power (int aardvark, byte power_mask);

[DllImport ("aardvark", EntryPoint = "net_aa_sleep_ms@4", CallingConvention = CallingConvention.StdCall)]
private static extern uint net_aa_sleep_ms (uint milliseconds);

[DllImport ("aardvark", EntryPoint = "net_aa_async_poll@8", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_async_poll (int aardvark, int timeout);

[DllImport ("aardvark", EntryPoint = "net_aa_i2c_free_bus@4", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_i2c_free_bus (int aardvark);

[DllImport ("aardvark", EntryPoint = "net_aa_i2c_bitrate@8", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_i2c_bitrate (int aardvark, int bitrate_khz);

[DllImport ("aardvark", EntryPoint = "net_aa_i2c_bus_timeout@8", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_i2c_bus_timeout (int aardvark, ushort timeout_ms);

[DllImport ("aardvark", EntryPoint = "net_aa_i2c_read@20", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_i2c_read (int aardvark, ushort slave_addr, AardvarkI2cFlags flags, ushort num_bytes, [Out] byte[] data_in);

[DllImport ("aardvark", EntryPoint = "net_aa_i2c_read_ext@24", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_i2c_read_ext (int aardvark, ushort slave_addr, AardvarkI2cFlags flags, ushort num_bytes, [Out] byte[] data_in, ref ushort num_read);

[DllImport ("aardvark", EntryPoint = "net_aa_i2c_write@20", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_i2c_write (int aardvark, ushort slave_addr, AardvarkI2cFlags flags, ushort num_bytes, [In] byte[] data_out);

[DllImport ("aardvark", EntryPoint = "net_aa_i2c_write_ext@24", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_i2c_write_ext (int aardvark, ushort slave_addr, AardvarkI2cFlags flags, ushort num_bytes, [In] byte[] data_out, ref ushort num_written);

[DllImport ("aardvark", EntryPoint = "net_aa_i2c_write_read@36", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_i2c_write_read (int aardvark, ushort slave_addr, AardvarkI2cFlags flags, ushort out_num_bytes, [In] byte[] out_data, ref ushort num_written, ushort in_num_bytes, [Out] byte[] in_data, ref ushort num_read);

[DllImport ("aardvark", EntryPoint = "net_aa_i2c_slave_enable@16", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_i2c_slave_enable (int aardvark, byte addr, ushort maxTxBytes, ushort maxRxBytes);

[DllImport ("aardvark", EntryPoint = "net_aa_i2c_slave_disable@4", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_i2c_slave_disable (int aardvark);

[DllImport ("aardvark", EntryPoint = "net_aa_i2c_slave_set_response@12", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_i2c_slave_set_response (int aardvark, byte num_bytes, [In] byte[] data_out);

[DllImport ("aardvark", EntryPoint = "net_aa_i2c_slave_write_stats@4", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_i2c_slave_write_stats (int aardvark);

[DllImport ("aardvark", EntryPoint = "net_aa_i2c_slave_read@16", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_i2c_slave_read (int aardvark, ref byte addr, ushort num_bytes, [Out] byte[] data_in);

[DllImport ("aardvark", EntryPoint = "net_aa_i2c_slave_write_stats_ext@8", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_i2c_slave_write_stats_ext (int aardvark, ref ushort num_written);

[DllImport ("aardvark", EntryPoint = "net_aa_i2c_slave_read_ext@20", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_i2c_slave_read_ext (int aardvark, ref byte addr, ushort num_bytes, [Out] byte[] data_in, ref ushort num_read);

[DllImport ("aardvark", EntryPoint = "net_aa_i2c_pullup@8", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_i2c_pullup (int aardvark, byte pullup_mask);

[DllImport ("aardvark", EntryPoint = "net_aa_spi_bitrate@8", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_spi_bitrate (int aardvark, int bitrate_khz);

[DllImport ("aardvark", EntryPoint = "net_aa_spi_configure@16", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_spi_configure (int aardvark, AardvarkSpiPolarity polarity, AardvarkSpiPhase phase, AardvarkSpiBitorder bitorder);

[DllImport ("aardvark", EntryPoint = "net_aa_spi_write@20", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_spi_write (int aardvark, ushort out_num_bytes, [In] byte[] data_out, ushort in_num_bytes, [Out] byte[] data_in);

[DllImport ("aardvark", EntryPoint = "net_aa_spi_slave_enable@4", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_spi_slave_enable (int aardvark);

[DllImport ("aardvark", EntryPoint = "net_aa_spi_slave_disable@4", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_spi_slave_disable (int aardvark);

[DllImport ("aardvark", EntryPoint = "net_aa_spi_slave_set_response@12", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_spi_slave_set_response (int aardvark, byte num_bytes, [In] byte[] data_out);

[DllImport ("aardvark", EntryPoint = "net_aa_spi_slave_read@12", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_spi_slave_read (int aardvark, ushort num_bytes, [Out] byte[] data_in);

[DllImport ("aardvark", EntryPoint = "net_aa_spi_master_ss_polarity@8", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_spi_master_ss_polarity (int aardvark, AardvarkSpiSSPolarity polarity);

[DllImport ("aardvark", EntryPoint = "net_aa_gpio_direction@8", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_gpio_direction (int aardvark, byte direction_mask);

[DllImport ("aardvark", EntryPoint = "net_aa_gpio_pullup@8", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_gpio_pullup (int aardvark, byte pullup_mask);

[DllImport ("aardvark", EntryPoint = "net_aa_gpio_get@4", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_gpio_get (int aardvark);

[DllImport ("aardvark", EntryPoint = "net_aa_gpio_set@8", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_gpio_set (int aardvark, byte value);

[DllImport ("aardvark", EntryPoint = "net_aa_gpio_change@8", CallingConvention = CallingConvention.StdCall)]
private static extern int net_aa_gpio_change (int aardvark, ushort timeout);


} // class AardvarkApi

} // namespace TotalPhase
