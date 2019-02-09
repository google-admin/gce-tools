# Tools for Windows VMs in GCE

## `GetGcePdName`

This project contains a PowerShell cmdlet that can get the name of GCE
persistent disks attached to a Windows VM. It is implemented as a binary C#
PowerShell module which invokes a Windows device IOCTL to get the PD name via a
SCSI query. You can load this project in Visual Studio to build the dll
yourself, or use the prebuilt dll in this repository if your target VM does not
have Visual Studio installed.

### Building the PowerShell module

*   Open `GceTools.sln` in Visual Studio and ensure that the GetGcePdName
    project is active.

*   Build the solution (Build -> Build Solution from the menu, or Ctrl+Shift+B).
    This should generate a `bin\Debug\GetGcePdName.dll` file that contains both
    code for getting the GCE PD name via Windows APIs and the PowerShell
    wrapper.

### Importing the GetGcePdName PowerShell module

In a PowerShell running with Administrator privileges, first set the path to the
dll for the PowerShell module. To use the prebuilt dll, you must first
["unblock"](https://stackoverflow.com/a/13804692/1230197) the file since it came
from a remote source. From the root of this repository run:

```
Unblock-File ".\GceTools\GetGcePdName\GetGcePdName.dll"
$modulePath = ".\GceTools\GetGcePdName\GetGcePdName.dll"
```

Or if you built the GetGcePdName code yourself, then use:

```
$modulePath = ".\GceTools\GetGcePdName\\bin\Debug\GetGcePdName.dll"
```

Then run these commands to import the module and check its contents:

```
Import-Module -Verbose -Name $modulePath
Get-Module GetGcePdName
Get-GcePdName -?
```

TODO(pjh): add instructions for installing the module system-wide.

### Running the Get-GcePdName cmdlet

**`Get-GcePdName` must be run with Administrator privilege.**

`Get-GcePdName` identifies disks by `DeviceId` property that Windows assigns to
physical disks. These ids can either be specified directly as command arguments,
or `MSFT_PhysicalDisk` objects from the `Get-PhysicalDisk` cmdlet can be piped
to `Get-GcePdName`.

```
PS > Get-GcePdName 0
pds-2019-0

PS > Get-GcePdName 1,2
mypd-1
mypd-2

PS > Get-PhysicalDisk | Select-Object DeviceId,FriendlyName,Size | Format-Table

DeviceId FriendlyName                 Size
-------- ------------                 ----
1        Google PersistentDisk 53687091200
0        Google PersistentDisk 53687091200
2        Google PersistentDisk 26843545600

PS > Get-PhysicalDisk | Get-GcePdName
mypd-1
pds-2019-0
mypd-2

PS > Get-PhysicalDisk | Where-Object DeviceId -eq 1 | Get-GcePdName
mypd-1

# Fetching the DeviceId from a target PD name currently requires a few steps:
PS > $target = 'target-pd-name'
PS > $deviceIds = $((Get-PhysicalDisk | Sort-Object DeviceId).DeviceId)
PS > $names = $($deviceIds | Get-GcePdName)
PS > $map = for ($i = 0; $i -lt $deviceIds.Count; $i++) {
  [PSCustomObject]@{
    DeviceId = $deviceIds[$i]
    Name = $names[$i]
  }
}
PS > ($map | Where-Object Name -eq $target).DeviceId
```

TODO(pjh): update Get-GcePdName to return this custom object directly! Perhaps a
hash table from name to id makes most sense.
