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
$modulePath = ".\GceTools\GetGcePdName\GetGcePdName.dll"
Unblock-File $modulePath
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
PS > Get-GcePdName
Name       DeviceId
----       --------
mypd-2     2
mypd-1     1
pds-2019-0 0

PS > $targetPd = "mypd-1"
PS > (Get-GcePdName | Where-Object Name -eq $targetPd).DeviceId
1

PS > Get-GcePdName 0
Name       DeviceId
----       --------
pds-2019-0 0

PS > Get-GcePdName 1,2
Name   DeviceId
----   --------
mypd-1 1
mypd-2 2

PS > @(2,0) | Get-GcePdName
Name       DeviceId
----       --------
mypd-2     2
pds-2019-0 0

PS > Get-PhysicalDisk | Select-Object DeviceId,FriendlyName,Size | Format-Table

DeviceId FriendlyName                 Size
-------- ------------                 ----
1        Google PersistentDisk 53687091200
0        Google PersistentDisk 53687091200
2        Google PersistentDisk 26843545600

PS > Get-PhysicalDisk | Where-Object DeviceId -eq 1 | Get-GcePdName
Name   DeviceId
----   --------
mypd-1 1
```
