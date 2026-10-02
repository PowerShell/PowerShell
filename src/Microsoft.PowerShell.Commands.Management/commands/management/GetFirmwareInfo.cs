/** 
  * MIT License.
  * Copyright (C) Microsoft
*/

/* Libs */
using System;
using System.Runtime.InteropServices;
using System.Management.Automation;
using System.Management.Infrastructure;
using System.IO;

/** A Structure for Datas in OutputType in
  MainStructure */
public struct FirmwareInfo
{
  public int BiosVersion;  /* Firmware Version */
  public int DataBIOS;  /* Release Data Firmware */
  public bool isUefi;  /* Is a UEFI? */
  public bool secureBootActive; /* Secure Boot Actived? */
  public int SMBIOSVersion;  /* Version of SMBIOS */
  public string NameCpuACPI; /* Example: Intel Core i3-1115G4 */
  public int CoresCpuACPI;  /* Example: 2 Cores */
  public int ThreadsCpuACPI; /* Example: 4 Threads */
  public int ArchCpuACPI; /* Example: 0x8664 for x86_64 */
  public int AcpiVersion; /* What is the ACPI Version? */
  public string OwnerBios; /* Example: Samsung */
}

namespace Microsoft.PowerShell.Commands
{
  /// <summary>
  /// Gets the system firmware information.
  /// </summary>
  [Cmdlet(VerbsCommon.Get, "FirmwareInfo")]
  [OutputType(typeof(FirmwareInfo))]
  public class GetFirmwareInfoCommand : Cmdlet
  {
    /// <summary>
    /// Executes the cmdlet logic.
    /// </summary>
    protected override void ProcessRecord()
    {
      // Is Windows?
      if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      {
         /* Var For uses */
         var info = new FirmwareInfo();
         /* Open a Session */
         using (var session = CimSession.Create(null))
         {
           /** Executes a Query for Get the
             Data Properties */
           var object_res = session.QueryInstances(
             "root\\cimv2",
             "WQL",
             "SELECT * FROM Win32_BIOS"
           );
           /** Loop for:
             Data in FirmwareInfo 
           */
           foreach (var instance in object_res)
           {
              /* Data_Bios = CimInstanceProperties */ 
              info.OwnerBios = instance.CimInstanceProperties["Manufacturer"]?.Value?.ToString();
              info.BiosVersion = Convert.ToInt32(instance.CimInstanceProperties["Version"]?.Value ?? 0);
              info.DataBIOS = Convert.ToInt32(instance.CimInstanceProperties["ReleaseDate"]?.Value ?? 0);
              info.SMBIOSVersion = Convert.ToInt32(instance.CimInstanceProperties["SMBIOSBIOSVersion"]?.Value ?? 0);
           }
           /** Second Query:
             Win32_Processor */
           var obj = session.QueryInstances(
             "root\\cimv2",
             "WQL",
             "SELECT * FROM Win32_Processor"
           );
           /* Loop for Get the CPU Data */
           foreach (var instance in obj)
           {
             /* Data_Processor = CimInstanceProperties */
             info.NameCpuACPI = instance.CimInstanceProperties["Name"]?.Value?.ToString();
             info.CoresCpuACPI = Convert.ToInt32(instance.CimInstanceProperties["NumberOfCores"]?.Value ?? 0);
             info.ThreadsCpuACPI = Convert.ToInt32(instance.CimInstanceProperties["NumberOfLogicalProcessors"]?.Value ?? 0);
           }
           /* Third Query: SecureBoot Namespace */
           var sbObj = session.QueryInstances(
             "root\\Microsoft\\Windows\\SecureBoot",
             "WQL",
             "SELECT * FROM MS_SecureBoot"
           );
           /* Loop for update the secureBootActive */   
           foreach (var instance in sbObj)
           {
             info.secureBootActive = Convert.ToBoolean(instance.CimInstanceProperties["SecureBootEnabled"]?.Value ?? false);
           }
        }
        WriteObject(info);
      }
      /* Is Linux? */
      else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
      {
        /* Verify if DMI path exists */
        if (!Directory.Exists("/sys/class/dmi/id/"))
        {
          return;
        }
        var info = new FirmwareInfo();
        /* Read BIOS and Manufacturer Info */
        if (File.Exists("/sys/class/dmi/id/sys_vendor"))
        {
          info.OwnerBios = File.ReadAllText("/sys/class/dmi/id/sys_vendor").Trim();
        }
        if (File.Exists("/sys/class/dmi/id/bios_version"))
        {
          string vStr = File.ReadAllText("/sys/class/dmi/id/bios_version").Trim();
          int.TryParse(new string(Array.FindAll(vStr.ToCharArray(), Char.IsDigit)), out info.BiosVersion);
        }
        if (File.Exists("/sys/class/dmi/id/bios_date"))
        {
          string dStr = File.ReadAllText("/sys/class/dmi/id/bios_date").Trim();
          int.TryParse(new string(Array.FindAll(dStr.ToCharArray(), Char.IsDigit)), out info.DataBIOS);
        }
        if (File.Exists("/sys/class/dmi/id/bios_version"))
        {
          string smbStr = File.ReadAllText("/sys/class/dmi/id/bios_version").Trim();
          int.TryParse(new string(Array.FindAll(smbStr.ToCharArray(), Char.IsDigit)), out info.SMBIOSVersion);
        }
        /* Check UEFI and SecureBoot */
        if (Directory.Exists("/sys/firmware/efi"))
        {
          info.isUefi = true;
          string secureBootPath = "/sys/firmware/efi/vars/SecureBoot-8be4df61-93ca-11d2-aa0d-00e098032b8c/data";
          if (File.Exists(secureBootPath))
          {
            byte[] sbData = File.ReadAllBytes(secureBootPath);
            info.secureBootActive = (sbData.Length > 0 && sbData[sbData.Length - 1] == 1);
          }
        }
        /* Populate CPU details from /proc/cpuinfo */
        if (File.Exists("/proc/cpuinfo"))
        {
          string[] cpuLines = File.ReadAllLines("/proc/cpuinfo");
          int coreCount = 0;
          int threadCount = 0;
          foreach (var line in cpuLines)
          {
            if (line.StartsWith("model name"))
            {
              int colonIdx = line.IndexOf(':');
              if (colonIdx > 0 && string.IsNullOrEmpty(info.NameCpuACPI))
              {
                info.NameCpuACPI = line.Substring(colonIdx + 1).Trim();
              }
            }
            if (line.StartsWith("processor"))
            {
              threadCount++;
            }
            if (line.StartsWith("cpu cores"))
            {
              int colonIdx = line.IndexOf(':');
              if (colonIdx > 0)
              {
                int.TryParse(line.Substring(colonIdx + 1).Trim(), out coreCount);
              }
            }
          }
          info.ThreadsCpuACPI = threadCount > 0 ? threadCount : 1;
          info.CoresCpuACPI = coreCount > 0 ? coreCount : threadCount;
        }
        /* Set architecture flag */
        info.ArchCpuACPI = RuntimeInformation.ProcessArchitecture == Architecture.X64 ? 0x8664 : 0;
        WriteObject(info);
      }
      /* Is MacOS? */
      else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      {
        var info = new FirmwareInfo();  
        /* Apple defaults */
        info.OwnerBios = "Apple Inc.";
        info.isUefi = true;
        info.CoresCpuACPI = Environment.ProcessorCount;
        info.ThreadsCpuACPI = Environment.ProcessorCount;
        info.NameCpuACPI = "Apple Silicon / Intel Mac Processor";
        info.ArchCpuACPI = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? 0xfeed : 0x8664;
        WriteObject(info);
      }
      /* Unknown OS? Return */
      else
      {
        return;
      }
    }
  }
}