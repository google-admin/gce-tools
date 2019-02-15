using System;
using System.Runtime.InteropServices;  // for DllImport, Marshal
using System.ComponentModel;  // for Win32Exception

// https://github.com/DKorablin/DeviceIoControl
using AlphaOmega.Debug;
using AlphaOmega.Debug.Native;

namespace GceTools
{
  using Microsoft.Win32.SafeHandles;
  using System.Collections.Generic;
  using System.Linq;
  // Make sure that the Visual Studio project "references" System.Management:
  // right-click on References in the Solution Explorer, Add Reference, search
  // for System.Management and check the box.
  // https://stackoverflow.com/a/11660206/1230197
  using System.Management;  // for WqlObjectQuery

  using LPSECURITY_ATTRIBUTES = System.IntPtr;
  using LPOVERLAPPED = System.IntPtr;
  using HANDLE = System.IntPtr;
  using DWORD = System.UInt32;
  using LPCTSTR = System.String;

  public class GcePdLib
  {
    // Append the drive number to this string for use with the CreateFile API.
    private const string PHYSICALDRIVE = @"\\.\PHYSICALDRIVE";
    // The SCSI query that we execute below returns a string for the disk name
    // that includes this prefix plus the PD name that we care about.
    private const string GOOGLEPREFIX = "Google  ";

    private const bool DEBUG = false;
    private static void WriteDebugLine(string line)
    {
#pragma warning disable CS0162 // Unreachable code detected
      if (DEBUG)
      {
        Console.WriteLine(line);
      }
#pragma warning restore CS0162 // Unreachable code detected
    }
    // https://www.pinvoke.net/default.aspx/kernel32.createfile
    // https://codereview.stackexchange.com/q/23264
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern SafeFileHandle CreateFile(
        LPCTSTR lpFileName,
        DWORD dwDesiredAccess,
        DWORD dwShareMode,
        LPSECURITY_ATTRIBUTES lpSecurityAttributes,
        DWORD dwCreationDisposition,
        DWORD dwFlagsAndAttributes,
        HANDLE hTemplateFile
        );

    // https://www.pinvoke.net/default.aspx/kernel32.deviceiocontrol
    // https://codereview.stackexchange.com/q/23264
    // https://stackoverflow.com/a/17354960/1230197
    // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        DWORD dwIoControlCode,
        ref StorageAPI.STORAGE_PROPERTY_QUERY lpInBuffer,
        DWORD nInBufferSize,
        out StorageAPI.STORAGE_DEVICE_ID_DESCRIPTOR lpOutBuffer,
        int nOutBufferSize,
        ref DWORD lpBytesReturned,
        LPOVERLAPPED lpOverlapped
        );

    // Copied from C:\Program Files (x86)\Windows Kits\10\Include\10.0.17763.0\um\winioctl.h
    private static uint CTL_CODE(uint DeviceType, uint Function,
      uint Method, uint Access)
    {
      return (DeviceType << 16) | (Access << 14) | (Function) << 2 | Method;
    }
    // static readonly: https://stackoverflow.com/a/16143924/1230197
    private static readonly uint METHOD_BUFFERED = 0;
    private static readonly uint FILE_ANY_ACCESS = 0;
    private static readonly uint FILE_DEVICE_MASS_STORAGE = 0x0000002d;
    private static readonly uint IOCTL_STORAGE_BASE = FILE_DEVICE_MASS_STORAGE;
    private static readonly uint IOCTL_STORAGE_QUERY_PROPERTY = CTL_CODE(
        IOCTL_STORAGE_BASE, 0x0500, METHOD_BUFFERED, FILE_ANY_ACCESS);

    // deviceId is the physical disk number, e.g. from Get-PhysicalDisk. If the
    // device is determined to be a GCE PD then its name will be returned.
    //
    // Throws:
    //   System.ComponentModel.Win32Exception: if we could not open a handle for
    //     the device (probably because deviceId is invalid)
    //   InvalidOperationException: if the device is not a GCE PD.
    public static string Get_GcePdName(string deviceId)
    {
      string physicalDrive = PHYSICALDRIVE + deviceId;
      var hDevice = CreateFile(physicalDrive,
          ((uint)WinAPI.FILE_ACCESS_FLAGS.GENERIC_READ |
           (uint)WinAPI.FILE_ACCESS_FLAGS.GENERIC_WRITE),
          (uint)WinAPI.FILE_SHARE.READ,
          IntPtr.Zero,
          (uint)WinAPI.CreateDisposition.OPEN_EXISTING,
          0,
          IntPtr.Zero
          );
      if (hDevice.IsInvalid)
      {
        var e = new Win32Exception(Marshal.GetLastWin32Error(),
          String.Format("CreateFile({0}) failed. Is the drive number valid?",
          physicalDrive));
        WriteDebugLine(String.Format("Error: {0}", e.ToString()));
        WriteDebugLine("Please use a valid physical drive number (e.g. " +
          "(Get-PhysicalDisk).DeviceId)");
        throw e;
      }

      // https://stackoverflow.com/a/17354960/1230197
      var query = new StorageAPI.STORAGE_PROPERTY_QUERY
      {
        PropertyId = StorageAPI.STORAGE_PROPERTY_QUERY.STORAGE_PROPERTY_ID.StorageDeviceIdProperty,  // page 83
        QueryType = StorageAPI.STORAGE_PROPERTY_QUERY.STORAGE_QUERY_TYPE.PropertyStandardQuery
      };
      var qsize = (uint)Marshal.SizeOf(query);
      // https://stackoverflow.com/a/2069456/1230197
      var result = default(StorageAPI.STORAGE_DEVICE_ID_DESCRIPTOR);
      var rsize = Marshal.SizeOf(result);
      uint written = 0;
      bool ok = DeviceIoControl(hDevice, IOCTL_STORAGE_QUERY_PROPERTY,
                   ref query, qsize, out result, rsize, ref written,
                   IntPtr.Zero);
      if (!ok)
      {
        var e = new Win32Exception(Marshal.GetLastWin32Error(),
        String.Format("DeviceIoControl({0}) failed", physicalDrive));
        WriteDebugLine(String.Format("Error: {0}", e.ToString()));
        hDevice.Close();
        throw e;
      }

      uint numIdentifiers = result.NumberOfIdentifiers;
      WriteDebugLine(String.Format("numIdentifiers: {0}", numIdentifiers));

      int identifierBufferStart = 0;
      for (int i = 0; i < numIdentifiers; ++i)
      {
        // Example:
        // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal.copy?view=netframework-4.7.2#System_Runtime_InteropServices_Marshal_Copy_System_Byte___System_Int32_System_IntPtr_System_Int32_.
        // We don't know exactly how large this identifier is until we marshal
        // the struct below. "BUFFER_SIZE" is used by
        // STORAGE_DEVICE_ID_DESCRIPTOR for the combined size of all the
        // identifiers so it's an upper bound on the size of this one.
        IntPtr storageIdentifierBuffer =
          Marshal.AllocHGlobal(StorageAPI.BUFFER_SIZE);
        int identifiersBufferLeft =
          StorageAPI.BUFFER_SIZE - identifierBufferStart;
        WriteDebugLine(
          String.Format("getting storageIdentifier {0} from memory [{1}, {2})",
            i, identifierBufferStart,
            identifierBufferStart + identifiersBufferLeft));
        Marshal.Copy(result.Identifiers, identifierBufferStart,
            storageIdentifierBuffer, identifiersBufferLeft);

        var storageIdentifier =
          Marshal.PtrToStructure<StorageAPI.STORAGE_IDENTIFIER>(
            storageIdentifierBuffer);

        WriteDebugLine(String.Format("storageIdentifier type: {0} ({1})",
            storageIdentifier.Type, (int)storageIdentifier.Type));
        WriteDebugLine(String.Format("storageIdentifier association: {0} ({1})",
            storageIdentifier.Association, (int)storageIdentifier.Association));
        WriteDebugLine(String.Format("storageIdentifier size: {0}",
          (int)storageIdentifier.IdentifierSize));

        if (storageIdentifier.Type == StorageAPI.STORAGE_IDENTIFIER.STORAGE_IDENTIFIER_TYPE.StorageIdTypeVendorId &&
            storageIdentifier.Association == StorageAPI.STORAGE_IDENTIFIER.STORAGE_ASSOCIATION_TYPE.StorageIdAssocDevice)
        {
          IntPtr identifierData =
            Marshal.AllocHGlobal(storageIdentifier.IdentifierSize + 1);
          Marshal.Copy(storageIdentifier.Identifier, 0,
            identifierData, storageIdentifier.IdentifierSize);

          // Make sure to close the file handle before returning, or subsequent
          // CreateFile calls for this disk or others may fail.
          hDevice.Close();

          // Empirically the SCSI identifier for GCE PDs always begins with
          // "Google". Physical disk objects passed to this function may
          // represent storage devices other than PDs though - for example,
          // Docker containers that are running have a "Msft Virtual Disk"
          // associated with them (why this is considered a PhysicalDisk, I have
          // no idea...). Therefore we check for the presence of the Google
          // prefix and throw an exception if it's not found.
          //
          // TODO(pjh): make this more robust? Not sure if the Google prefix is
          // guaranteed or subject to change in the future.
          string fullName = System.Text.Encoding.ASCII.GetString(
            storageIdentifier.Identifier, 0, storageIdentifier.IdentifierSize);
          if (!fullName.StartsWith(GOOGLEPREFIX))
          {
            var e = new InvalidOperationException(
              String.Format("deviceId {0} maps to {1} which is not a GCE PD",
                deviceId, fullName));
            WriteDebugLine(String.Format("{0}", e.ToString()));
            throw e;
          }
          return fullName.Substring(GOOGLEPREFIX.Length);
        }

        // To get the start of the next identifier we need to advance
        // by the length of the STORAGE_IDENTIFIER struct as well as
        // the size of its variable-length data array. We subtract
        // the size of the "byte[] Identifiers" member because it's
        // included in the size of the data array.
        //
        // TODO(pjh): figure out how to make this more robust.
        // Marshal.SizeOf(storageIdentifier) returns bonkers
        // values when we set the SizeConst MarshalAs attribute (which
        // we need to do in order to copy from the byte[] above). I
        // couldn't figure out how to correctly calculate this value
        // using Marshal.SizeOf, but it's 20 bytes - this value is
        // fixed (for this platform at least) by the definition of
        // STORAGE_IDENTIFIER in winioctl.h.
        int advanceBy = 20 - sizeof(byte) + storageIdentifier.IdentifierSize;
        WriteDebugLine(
          String.Format("advanceBy = {0} - {1} + {2} = {3}",
            20, sizeof(byte), storageIdentifier.IdentifierSize, advanceBy));
        identifierBufferStart += advanceBy;
        WriteDebugLine(String.Format("will read next identifier starting at {0}",
          identifierBufferStart));
        WriteDebugLine("");
        Marshal.FreeHGlobal(storageIdentifierBuffer);
      }
      hDevice.Close();
      return null;
    }

    // Returns a list of the deviceIds of all of the physical disks attached
    // to the system. This is equivalent to running
    // `(Get-PhysicalDisk).deviceId` in PowerShell.
    public static string[] GetAllPhysicalDeviceIds()
    {
      // Adapted from https://stackoverflow.com/a/39869074/1230197
      List<String> physicalDrives;

      var query = new WqlObjectQuery("SELECT * FROM Win32_DiskDrive");
      using (var searcher = new ManagementObjectSearcher(query))
      {
        physicalDrives = searcher.Get()
                         .OfType<ManagementObject>()
                         .Select(o => o.Properties["DeviceID"].Value.ToString())
                         .ToList();
      }

      string[] stripped = new string[physicalDrives.Count];
      int i = 0;
      foreach (String drive in physicalDrives)
      {
        // Strip off '\\.\PHYSICALDRIVE' prefix
        stripped[i] = drive.Substring(PHYSICALDRIVE.Length);
        ++i;
      }
      return stripped;
    }
  }
}
