using System;
using System.Runtime.InteropServices;  // for DllImport, Marshal
using System.ComponentModel;  // for Win32Exception

// https://github.com/DKorablin/DeviceIoControl
using AlphaOmega.Debug;
using AlphaOmega.Debug.Native;

namespace GetGcePdName
{
  using Microsoft.Win32.SafeHandles;

  using LPSECURITY_ATTRIBUTES = IntPtr;
  using LPOVERLAPPED = IntPtr;
  using HANDLE = IntPtr;
  using DWORD = UInt32;
  using LPCTSTR = String;

  class Program
  {
    const bool DEBUG = false;
    static void WriteDebugLine(string line)
    {
      if (DEBUG)
      {
        Console.WriteLine(line);
      }
    }
    // https://www.pinvoke.net/default.aspx/kernel32.deviceiocontrol
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
    static uint CTL_CODE(uint DeviceType, uint Function,
      uint Method, uint Access)
    {
      return (DeviceType << 16) | (Access << 14) | (Function) << 2 | Method;
    }
    // static readonly: https://stackoverflow.com/a/16143924/1230197
    static readonly uint METHOD_BUFFERED = 0;
    static readonly uint FILE_ANY_ACCESS = 0;
    static readonly uint FILE_DEVICE_MASS_STORAGE = 0x0000002d;
    static readonly uint IOCTL_STORAGE_BASE = FILE_DEVICE_MASS_STORAGE;
    static readonly uint IOCTL_STORAGE_QUERY_PROPERTY = CTL_CODE(
        IOCTL_STORAGE_BASE, 0x0500, METHOD_BUFFERED, FILE_ANY_ACCESS);

    static void GetArgs(string[] args, ref long driveNumber)
    {
      if (args.Length != 1)
      {
        WriteDebugLine("Usage: GetGcePdName.exe <physical drive number>");
        Environment.Exit(1);
      }
      driveNumber = Convert.ToInt64(args[0]);
      if (driveNumber < 0)
      {
        WriteDebugLine("Please enter a positive drive number");
        Environment.Exit(1);
      }
    }

    static void GetGcePdName(string physicalDrive)
    {
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
        var e = new Win32Exception(Marshal.GetLastWin32Error());
        WriteDebugLine(String.Format("Error: {0}", e.ToString()));
        WriteDebugLine("Please use a valid physical drive number (e.g. " +
          "(Get-PhysicalDisk).DeviceId)");
        Environment.Exit(1);
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
        throw new Win32Exception();
      // rsize will be something like 524 and written will be something like 88;
      // some code examples fail if these two values are not equal, but since
      // our ioctl returns variable-length structs/arrays we do not care.

      uint numIdentifiers = result.NumberOfIdentifiers;
      WriteDebugLine(String.Format("numIdentifiers: {0}", numIdentifiers));

      int identifierBufferStart = 0;
      for (int i = 0; i < numIdentifiers; ++i)
      {
        // Example:
        // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal.copy?view=netframework-4.7.2#System_Runtime_InteropServices_Marshal_Copy_System_Byte___System_Int32_System_IntPtr_System_Int32_.
        // We don't know exactly how large this identifier is until we
        // marshal the struct below. "Constant.BUFFER_SIZE" is used by
        // STORAGE_DEVICE_ID_DESCRIPTOR for the combined size of all
        // the identifiers so it's an upper bound on the size of this
        // one.
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

          // TODO(pjh): name always seems to have "Google  " prefix - strip
          // this here?
          string name = System.Text.Encoding.ASCII.GetString(
            storageIdentifier.Identifier, 0, storageIdentifier.IdentifierSize);
          Console.WriteLine(name);
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
    }

    static void Main(string[] args)
    {
      long driveNumber = -1;
      GetArgs(args, ref driveNumber);
      // https://stackoverflow.com/a/18074777/1230197 suggests that
      // string should work for LPCTSTR.
      // TODO(pjh): take disk number as argument!
      string physicalDrive = @"\\.\PHYSICALDRIVE" + driveNumber;

      GetGcePdName(physicalDrive);
    }
  }
}