Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;  // for DllImport, Marshal
using System.ComponentModel;  // for Win32Exception
using Microsoft.Win32.SafeHandles;

    using LPSECURITY_ATTRIBUTES = System.IntPtr;
    using LPOVERLAPPED = System.IntPtr;
    using HANDLE = System.IntPtr;
    using DWORD = System.UInt32;
    using LPCTSTR = System.String;


// Copied and adapted from
// https://github.com/DKorablin/DeviceIoControl/blob/master/DeviceIoControl/Native/StorageAPI.cs
  /// <summary>Storage IOCTL structures</summary>
  public struct StorageAPI
  {
    // pjh: was originally in Constant.cs.
    public const Int32 BUFFER_SIZE = 512;

    /// <summary>Indicates the properties of a storage device or adapter to retrieve as the input buffer passed to the <see cref="T:Constant.IOCTL_STORAGE.QUERY_PROPERTY"/> control code.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct STORAGE_PROPERTY_QUERY
    {
      /// <summary>Enumerates the possible values of the PropertyId member of the <see cref="T:STORAGE_PROPERTY_QUERY"/> structure passed as input to the <see cref="T:Constant.IOCTL_STORAGE.QUERY_PROPERTY"/> request to retrieve the properties of a storage device or adapter.</summary>
      public enum STORAGE_PROPERTY_ID : int
      {
        /// <summary>Indicates that the caller is querying for the device descriptor.</summary>
        StorageDeviceProperty = 0,
        /// <summary>Indicates that the caller is querying for the adapter descriptor.</summary>
        StorageAdapterProperty = 1,
        /// <summary>Indicates that the caller is querying for the device identifiers provided with the SCSI vital product data pages.</summary>
        StorageDeviceIdProperty = 2,
        /// <summary>Indicates that the caller is querying for the unique device identifiers.</summary>
        StorageDeviceUniqueIdProperty = 3,
        /// <summary>Indicates that the caller is querying for the write cache property.</summary>
        StorageDeviceWriteCacheProperty = 4,
        /// <summary>Indicates that the caller is querying for the miniport driver descriptor.</summary>
        StorageMiniportProperty = 5,
        /// <summary>Indicates that the caller is querying for the access alignment descriptor.</summary>
        StorageAccessAlignmentProperty = 6,
        /// <summary>Indicates that the caller is querying for the seek penalty descriptor.</summary>
        StorageDeviceSeekPenaltyProperty = 7,
        /// <summary>Indicates that the caller is querying for the trim descriptor.</summary>
        StorageDeviceTrimProperty = 8,
        /// <summary>Indicates that the caller is querying for the write aggregation property.</summary>
        StorageDeviceWriteAggregationProperty = 9,
        /// <summary>This value is reserved.</summary>
        StorageDeviceDeviceTelemetryProperty = 0xA,
        /// <summary>Indicates that the caller is querying for the logical block provisioning descriptor, usually to detect whether the storage system uses thin provisioning.</summary>
        StorageDeviceLBProvisioningProperty = 0xB,
        /// <summary>Indicates that the caller is querying for the power optical disk drive descriptor.</summary>
        StorageDeviceZeroPowerProperty = 0xC,
        /// <summary>Indicates that the caller is querying for the write offload descriptor.</summary>
        StorageDeviceCopyOffloadProperty = 0xD,
        /// <summary>Indicates that the caller is querying for the device resiliency descriptor.</summary>
        StorageDeviceResiliencyProperty = 0xE,
      }
      /// <summary>Types of queries</summary>
      public enum STORAGE_QUERY_TYPE : int
      {
        /// <summary>Retrieves the descriptor</summary>
        PropertyStandardQuery = 0,
        /// <summary>Used to test whether the descriptor is supported</summary>
        PropertyExistsQuery = 1,
        /// <summary>Used to retrieve a mask of writeable fields in the descriptor</summary>
        PropertyMaskQuery = 2,
        /// <summary>use to validate the value</summary>
        PropertyQueryMaxDefined = 3,
      }

      /// <summary>Indicates whether the caller is requesting a device descriptor, an adapter descriptor, a write cache property, a device unique ID (DUID), or the device identifiers provided in the device's SCSI vital product data (VPD) page.</summary>
      public STORAGE_PROPERTY_ID PropertyId;
      /// <summary>Contains flags indicating the type of query to be performed as enumerated by the <see cref="T:STORAGE_QUERY_TYPE"/> enumeration.</summary>
      public STORAGE_QUERY_TYPE QueryType;
      /// <summary>Contains an array of bytes that can be used to retrieve additional parameters for specific queries.</summary>
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
      public Byte[] AdditionalParameters;
    }

    /// <summary>Used with the <see cref="T:Constant.IOCTL_STORAGE.QUERY_PROPERTY"/> control code request to retrieve the device ID descriptor data for a device.</summary>
    /// <remarks>The device ID descriptor consists of an array of device IDs taken from the SCSI-3 vital product data (VPD) page 0x83 that was retrieved during discovery.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct STORAGE_DEVICE_ID_DESCRIPTOR
    {
      /// <summary>Contains the size of this structure, in bytes. The value of this member will change as members are added to the structure.</summary>
      public UInt32 Version;
      /// <summary>Specifies the total size of the data returned, in bytes. This may include data that follows this structure.</summary>
      public UInt32 Size;
      /// <summary>Contains the number of identifiers reported by the device in the Identifiers array.</summary>
      public UInt32 NumberOfIdentifiers;
      /// <summary>Contains a variable-length array of identification descriptors (STORAGE_IDENTIFIER).</summary>
      // pjh: BUFFER_SIZE (512 bytes) is assumed to be sufficient here.
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = BUFFER_SIZE)]
      public Byte[] Identifiers;
    }

    // Adapted from C:\Program Files (x86)\Windows Kits\10\Include\10.0.17763.0\um\winioctl.h
    //   WORD -> UInt16
    //   BYTE[1] -> Byte[]
    [StructLayout(LayoutKind.Sequential)]
    public struct STORAGE_IDENTIFIER
    {
      // These enums are copied from
      // C:\Program Files (x86)\Windows Kits\10\Include\10.0.17763.0\um\winioctl.h
      public enum STORAGE_IDENTIFIER_CODE_SET : int
      {
        StorageIdCodeSetReserved = 0,
        StorageIdCodeSetBinary = 1,
        StorageIdCodeSetAscii = 2,
        StorageIdCodeSetUtf8 = 3
      }
      public enum STORAGE_IDENTIFIER_TYPE : int
      {
        StorageIdTypeVendorSpecific = 0,
        StorageIdTypeVendorId = 1,
        StorageIdTypeEUI64 = 2,
        StorageIdTypeFCPHName = 3,
        StorageIdTypePortRelative = 4,
        StorageIdTypeTargetPortGroup = 5,
        StorageIdTypeLogicalUnitGroup = 6,
        StorageIdTypeMD5LogicalUnitIdentifier = 7,
        StorageIdTypeScsiNameString = 8
      }
      public enum STORAGE_ASSOCIATION_TYPE : int
      {
        StorageIdAssocDevice = 0,
        StorageIdAssocPort = 1,
        StorageIdAssocTarget = 2
      }

      public STORAGE_IDENTIFIER_CODE_SET CodeSet;
      public STORAGE_IDENTIFIER_TYPE Type;
      public UInt16 IdentifierSize;
      public UInt16 NextOffset;
      //
      // Add new fields here since existing code depends on
      // the above layout not changing.
      //
      public STORAGE_ASSOCIATION_TYPE Association;
      //
      // The identifier is a variable length array of bytes.
      //
      // pjh: The final variable-length array 'Identifiers' field is declared
      // so that it matches STORAGE_DEVICE_ID_DESCRIPTOR just above. In
      // particular the "MarshalAs(UnmanagedType.ByValArray)" annotation is
      // critical. Without the "UnmanagedType.ByValArray" annotation, calling
      // Marshal.PtrToStructure on this memory will lead to "Unhandled
      // Exception: System.AccessViolationException: Attempted to read or
      // write protected memory. Without the SizeConst annotation, calling
      // Marshal.Copy from the Identifier array will lead to
      // "System.ArgumentOutOfRangeException: Requested range extends past the
      // end of the array" because the runtime assumes a default length of 1
      // for the managed Byte[].
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = BUFFER_SIZE)]
      public Byte[] Identifier;
    }
  }

// Copied and adapted from
// https://github.com/DKorablin/DeviceIoControl/blob/master/DeviceIoControl/Native/WinAPI.cs
  /// <summary>Native structures</summary>
  public struct WinAPI
	{
    /// <summary>Define the method codes for how buffers are passed for I/O and FS controls</summary>
    public enum METHOD : byte
    {
      /// <summary>Specifies the buffered I/O method, which is typically used for transferring small amounts of data per request.</summary>
      BUFFERED = 0,
      /// <summary>Specifies the direct I/O method, which is typically used for reading or writing large amounts of data, using DMA or PIO, that must be transferred quickly.</summary>
      IN_DIRECT = 1,
      /// <summary>Specifies the direct I/O method, which is typically used for reading or writing large amounts of data, using DMA or PIO, that must be transferred quickly.</summary>
      OUT_DIRECT = 2,
      /// <summary>
      /// Specifies neither buffered nor direct I/O.
      /// The I/O manager does not provide any system buffers or MDLs.
      /// The IRP supplies the user-mode virtual addresses of the input and output buffers that were specified to DeviceIoControl or IoBuildDeviceIoControlRequest, without validating or mapping them.
      /// </summary>
      NEITHER = 3,
    }
    [Flags]
		public enum FILE_ACCESS_FLAGS : uint
		{
			/// <summary>Read</summary>
			GENERIC_READ = 0x80000000,
			/// <summary>Write</summary>
			GENERIC_WRITE = 0x40000000,
			/// <summary>Execute</summary>
			GENERIC_EXECUTE = 0x20000000,
			/// <summary>All</summary>
			GENERIC_ALL = 0x10000000,
		}
		/// <summary>Share</summary>
		[Flags]
		public enum FILE_SHARE : uint
		{
			/// <summary>
			/// Enables subsequent open operations on a file or device to request read access.
			/// Otherwise, other processes cannot open the file or device if they request read access.
			/// </summary>
			READ = 0x00000001,
			/// <summary>
			/// Enables subsequent open operations on a file or device to request write access.
			/// Otherwise, other processes cannot open the file or device if they request write access.
			/// </summary>
			WRITE = 0x00000002,
			/// <summary>
			/// Enables subsequent open operations on a file or device to request delete access.
			/// Otherwise, other processes cannot open the file or device if they request delete access.
			/// If this flag is not specified, but the file or device has been opened for delete access, the function fails.
			/// </summary>
			DELETE = 0x00000004,
		}
    /// <summary>Disposition</summary>
    public enum CreateDisposition : uint
    {
      /// <summary>Create new</summary>
      CREATE_NEW = 1,
      /// <summary>Create always</summary>
      CREATE_ALWAYS = 2,
      /// <summary>Open exising</summary>
      OPEN_EXISTING = 3,
      /// <summary>Open always</summary>
      OPEN_ALWAYS = 4,
      /// <summary>Truncate existing</summary>
      TRUNCATE_EXISTING = 5,
    }
  }

//namespace GetGcePdName
//{
  //class Program
  public class GetGcePdName
  {
    private const bool DEBUG = false;
    static void WriteDebugLine(string line)
    {
      if (DEBUG)
      {
        //Console.WriteLine(line);
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

    public static string Get_GcePdName(string physicalDrive)
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
          //Console.WriteLine(name);
          return name;
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
      return null;
    }

    static void Main(string[] args)
    {
      long driveNumber = -1;
      GetArgs(args, ref driveNumber);
      // https://stackoverflow.com/a/18074777/1230197 suggests that
      // string should work for LPCTSTR.
      string physicalDrive = @"\\.\PHYSICALDRIVE" + driveNumber;
    
      Get_GcePdName(physicalDrive);
    }
  }
//}
"@

