// Copied and adapted from
// https://github.com/DKorablin/DeviceIoControl/blob/master/DeviceIoControl/Native/StorageAPI.cs

using System;
using System.Runtime.InteropServices;

namespace AlphaOmega.Debug.Native
{
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
}