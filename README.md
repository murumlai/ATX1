# ATX1 FabB — Functional Test Software

Console-based functional test application for the **MPDU ATX1 Fab B** board.  
Targets **.NET Framework 4.7.2** and is part of the Intel STHI manufacturing test flow.

---

## Solution Structure

| Project | Framework | Description |
|---------|-----------|-------------|
| `20vNG/ATX1.csproj` | .NET 4.7.2 | Main console test executable |
| `BLT/BLT.csproj` | .NET 3.5 | Board Level Tracking (EEPROM read/write) |
| `MCP2210/MCP2210.csproj` | .NET 3.5 | USB-to-SPI driver wrapper for MCP2210 |

---

## Hardware Dependencies

| Hardware | Purpose |
|----------|---------|
| **MCP2210** | USB-to-SPI bridge — used for BLT EEPROM access and VID/PID programming |
| **Aardvark I2C/SPI** | PMBus communication with CRPS power supply |
| **FT232 / WCF STDIO** | GPIO control, DVM voltage measurements, DIO state control |
| **CRPS** | Common Redundant Power Supply — 20V/12V power rails under test |

---

## Required File Paths

The following paths must exist on the test station before running:

```
C:\STHI\ATX1\ITUFFTemplate.xml              — ITUFF logging template
C:\STHI\ATX1\binlist.xml                    — Bin code definitions
C:\STHI\ATX1\ATX1_BLT.ini                  — BLT configuration (SN, AA, FW revisions, etc.)
C:\STHI\ATX1\DVM.xml                        — DVM channel map
C:\STHI\ATX1\DIO2.xml                       — DIO channel map
C:\STHI\ATX1\ReadTach\gpio1_sample.exe      — Fan tach reader executable
C:\STHI\ATX1\RestartUsbPort\RestartUsbPort.exe — USB port restart utility
```

---

## Usage

```
ATX1.exe <test_name>
```

### Available Test Commands

| Command | Description |
|---------|-------------|
| `readblt` | Read BLT EEPROM and verify against `ATX1_BLT.ini` |
| `writeblt` | Write BLT EEPROM from `ATX1_BLT.ini` |
| `vidpid` | Program MCP2210 USB VID/PID to Intel values (`8087/0BE1`) |
| `pson` | Test GPIO-controlled PS_ON signal (enable/disable 20V output) |
| `20v_test` | Test GPIO-controlled 20V power rail (enable/disable) |
| `pwrok_aux` | DVM test for 12V Aux and PWR_OK signals on 2x2 connectors |
| `20v_12v` | DVM test for 12V CPU, 12VO, and 20V connectors via BPD fan header |
| `ext_fan_tach` | Read external fan tach frequency (min threshold: 367 Hz) |
| `gpio_aux` | Test GPIO-controlled 12V Aux power rail (enable/disable) |
| `pmbus` | PMBus I2C communication test with CRPS (read model string) |
| `default_config` | Reset GPIO NVRAM to POR state (GPIO4 High, rails enabled) |
| `atx1_ft` | Full functional test sequence for MPDU ATX1 Fab B |
| `loop_ft` | Run full functional test in a loop |
| `fan_tach` | Read and log fan tach frequency (debug, no pass/fail) |
| `pwrok` | Read and verify GPIO5 PWR_OK signal is HIGH |
| `findmpdu` | Locate the MPDU ATX1 Fab B USB device port via `RestartUsbPort.exe` |

---

## BLT (Board Level Tracking)

The BLT is a 128-byte EEPROM on the board organized as 8 rows of 16 bytes.  
Each row contains 14 bytes of data and a 2-byte **CRC-16/CCITT** checksum.

Key fields stored in the BLT:

| Field | BLT Offset |
|-------|-----------|
| Manufacturer ID | `0x00` |
| Assembly No | `0x10` |
| Serial No | `0x20` |
| Manufacturing Date | `0x30` |
| Install Date | `0x37` |
| Cycle Counter | `0x40` |
| Global Counter | `0x47` |
| FW Revisions 1–3 | `0x50–0x5B` |
| Board ID | `0x60` |
| HW Revision | `0x69` |
| FW Revisions 4–5 | `0x70–0x71` |
| Custom | `0x72` |

---

## Pass / Fail Bins

| Bin | Meaning |
|-----|---------|
| `1000000` | PASS |
| `10639999` | Unknown failure (default) |
| `106301xx` | BLT / VID-PID failures |
| `106302xx` | Power rail / GPIO / PMBus failures |

---

## Dependencies (External DLLs)

| Library | Purpose |
|---------|---------|
| `ApseCore.dll` | Intel APSE core framework |
| `ApseCore.Common.dll` | Common APSE types and enums |
| `ApseCore.InternalCommon.dll` | Internal APSE helpers |
| `itufflogger.exe` | ITUFF test result logging |
| `Log.exe` | Console/file logging |
| `HidSharp.dll` | HID communication for MCP2210 |
| `aardvark.dll` | Total Phase Aardvark I2C/SPI API |
| `MCP2210DLL-M-dotNet4.dll` | MCP2210 .NET driver |

---

## Build

Open `ATX1.sln` in **Visual Studio 2026+** and build in `Release|x64`.

```
msbuild ATX1.sln /p:Configuration=Release /p:Platform=x64
```

---

## Network / Station Setup

| Address | Role |
|---------|------|
| `10.0.0.1` | Test station (DUT controller) |
| `10.0.0.100` | Host / FCC server |
| `localhost:8990` | WCF STDIO service endpoint |
