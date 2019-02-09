﻿// To test and debug this cmdlet:
//   - Build the solution (see below if you get errors for
//     System.Management.Automation).
//   - In powershell use a nested powershell to:
//     - Import-Module -Verbose -Force -Name C:\...\GceTools\GetGcePdName\bin\Debug\GetGcePdName.dll
//     - Get-Module  # verify Get-GcePdName is exported from GetGcePdName module
//     - Get-GcePdName -?
//   - Close the nested powershell and reopen a new one to re-build; Visual
//     Studio will not overwrite the .dll while the powershell that imported it
//     is still running.

namespace GetGcePdName
{
  using System;  // for InvalidOperationException
  using System.ComponentModel;  // for Win32Exception
  // To get System.Management.Automation, right-click on References in the
  // Solution Explorer and choose Manage NuGet Packages. On the Browse tab search
  // for "powershell reference" and install the official Microsoft
  // "Microsoft.Powershell.5.ReferenceAssemblies" package (the one with the most
  // downloads). See:
  //   https://blogs.msdn.microsoft.com/powershell/2015/12/11/powershell-sdk-reference-assemblies-available-via-nuget-org/
  // and perhaps:
  //   https://github.com/PowerShell/PowerShell/issues/2284#issuecomment-247655190
  using System.Management.Automation;

  #region GetGcePdNameCommand

  // https://docs.microsoft.com/en-us/powershell/developer/cmdlet/cmdlet-class-declaration
  [Cmdlet(VerbsCommon.Get, "GcePdName")]
  public class GetGcePdNameCommand : Cmdlet
  {
    // Best resource for developing a cmdlet in C#:
    //   https://docs.microsoft.com/en-us/powershell/developer/cmdlet/tutorials-for-writing-cmdlets.
    // Going through the examples in order until you've learned everything you
    // need to know is recommended.

    #region Parameters
    /// <summary>
    /// List of physical disk numbers.
    /// </summary>
    // The type is string to match the DeviceId property from Get-PhysicalDisk
    // (Microsoft.Management.Infrastructure.CimInstance#root/microsoft/windows/storage/MSFT_PhysicalDisk).
    // The Parameter declaration here supports specifying the list of disk
    // numbers in three ways:
    //   Get-GcePdName 1,3,5
    //   @(1,3,5) | Get-GcePdName
    //   Get-PhysicalDisk | Get-GcePdName
    private string[] deviceIds;
    [Parameter(
      Position = 0,
      ValueFromPipeline = true,
      ValueFromPipelineByPropertyName = true)]
    [ValidateNotNullOrEmpty]
    public string[] DeviceId
    {
      get { return this.deviceIds; }
      set { this.deviceIds = value; }
    }
    #endregion Parameters

    #region Cmdlet Overrides
    protected override void ProcessRecord()
    {
      if (this.deviceIds == null)
      {
        var ex = new InvalidOperationException("No device IDs specified");
        WriteError(new ErrorRecord(ex, ex.ToString(),
          ErrorCategory.InvalidOperation, ""));
        return;
      }
      for (int i = 0; i < this.deviceIds.Length; ++i)
      {
        try
        {
          string name = GceTools.GcePdLib.Get_GcePdName(this.deviceIds[i]);
          WriteObject(name);
        }
        catch (Win32Exception ex)
        {
          WriteError(new ErrorRecord(ex, ex.ToString(),
            ErrorCategory.ReadError, this.deviceIds[i]));
          continue;
        }      
      }
    }
    #endregion Cmdlet Overrides
  }
  #endregion GetGcePdNameCommand
}
