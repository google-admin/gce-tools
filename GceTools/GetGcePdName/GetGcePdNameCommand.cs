// To test and debug this cmdlet:
//   - Build the solution (see below if you get errors for
//     System.Management.Automation).
//   - Import-Module -Verbose -Force -Name C:\...\GceTools\GetGcePdName\bin\Debug\GetGcePdName.dll
//   - Get-Module  # verify Get-GcePdName is exported from GetGcePdName module
//   - Get-GcePdName -?

namespace GetGcePdName
{
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
    ///// <summary>
    ///// The physical disk numbers to get the PD name of.
    ///// </summary>
    //// The type is string to match the DeviceId property from Get-PhysicalDisk
    //// (type 
    //// Microsoft.Management.Infrastructure.CimInstance#root/microsoft/windows/storage/MSFT_PhysicalDisk).
    //private string[] physicalDiskNumbers;

    //// TODO(pjh): do better input validation:
    //// https://docs.microsoft.com/en-us/powershell/developer/cmdlet/how-to-validate-parameter-input
    //[Parameter(Position = 0)]
    //[ValidateNotNullOrEmpty]
    //public string[] PhysicalDiskNumber
    //{
    //  get { return this.physicalDiskNumbers; }
    //  set { this.physicalDiskNumbers = value; }
    //}

    /// <summary>
    /// List of DeviceId properties from Get-PhysicalDisk.
    /// </summary>
    // The type is string to match the DeviceId property from Get-PhysicalDisk
    // (type 
    // Microsoft.Management.Infrastructure.CimInstance#root/microsoft/windows/storage/MSFT_PhysicalDisk).
    private string[] deviceIds;
    [Parameter(
      Position = 0,
      //ValueFromPipeline = true,
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
      //// If no process names are passed to the cmdlet, get all
      //// processes.
      //if (this.physicalDiskNumbers == null)
      //{
      //  WriteObject("physicalDiskNumbers is null");
      //}
      //else
      //{
      //  foreach (string diskNum in this.physicalDiskNumbers)
      //  {
      //    WriteObject(string.Format("physicalDiskNumber {0}", diskNum), true);
      //  }
      //}

      if (this.deviceIds == null)
      {
        WriteObject("deviceIds is null");
      }
      else
      {
        foreach (string id in this.deviceIds)
        {
          WriteObject(string.Format("deviceId {0}", id), true);
        }
      }
    }
    #endregion Cmdlet Overrides
  }
  #endregion GetGcePdNameCommand
}
