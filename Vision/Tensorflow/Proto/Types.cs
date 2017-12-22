// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: types.proto
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Vision.Tensorflow.Proto {

  /// <summary>Holder for reflection information generated from types.proto</summary>
  public static partial class TypesReflection {

    #region Descriptor
    /// <summary>File descriptor for types.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static TypesReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "Cgt0eXBlcy5wcm90bxIKdGVuc29yZmxvdyqqBgoIRGF0YVR5cGUSDgoKRFRf",
            "SU5WQUxJRBAAEgwKCERUX0ZMT0FUEAESDQoJRFRfRE9VQkxFEAISDAoIRFRf",
            "SU5UMzIQAxIMCghEVF9VSU5UOBAEEgwKCERUX0lOVDE2EAUSCwoHRFRfSU5U",
            "OBAGEg0KCURUX1NUUklORxAHEhAKDERUX0NPTVBMRVg2NBAIEgwKCERUX0lO",
            "VDY0EAkSCwoHRFRfQk9PTBAKEgwKCERUX1FJTlQ4EAsSDQoJRFRfUVVJTlQ4",
            "EAwSDQoJRFRfUUlOVDMyEA0SDwoLRFRfQkZMT0FUMTYQDhINCglEVF9RSU5U",
            "MTYQDxIOCgpEVF9RVUlOVDE2EBASDQoJRFRfVUlOVDE2EBESEQoNRFRfQ09N",
            "UExFWDEyOBASEgsKB0RUX0hBTEYQExIPCgtEVF9SRVNPVVJDRRAUEg4KCkRU",
            "X1ZBUklBTlQQFRINCglEVF9VSU5UMzIQFhINCglEVF9VSU5UNjQQFxIQCgxE",
            "VF9GTE9BVF9SRUYQZRIRCg1EVF9ET1VCTEVfUkVGEGYSEAoMRFRfSU5UMzJf",
            "UkVGEGcSEAoMRFRfVUlOVDhfUkVGEGgSEAoMRFRfSU5UMTZfUkVGEGkSDwoL",
            "RFRfSU5UOF9SRUYQahIRCg1EVF9TVFJJTkdfUkVGEGsSFAoQRFRfQ09NUExF",
            "WDY0X1JFRhBsEhAKDERUX0lOVDY0X1JFRhBtEg8KC0RUX0JPT0xfUkVGEG4S",
            "EAoMRFRfUUlOVDhfUkVGEG8SEQoNRFRfUVVJTlQ4X1JFRhBwEhEKDURUX1FJ",
            "TlQzMl9SRUYQcRITCg9EVF9CRkxPQVQxNl9SRUYQchIRCg1EVF9RSU5UMTZf",
            "UkVGEHMSEgoORFRfUVVJTlQxNl9SRUYQdBIRCg1EVF9VSU5UMTZfUkVGEHUS",
            "FQoRRFRfQ09NUExFWDEyOF9SRUYQdhIPCgtEVF9IQUxGX1JFRhB3EhMKD0RU",
            "X1JFU09VUkNFX1JFRhB4EhIKDkRUX1ZBUklBTlRfUkVGEHkSEQoNRFRfVUlO",
            "VDMyX1JFRhB6EhEKDURUX1VJTlQ2NF9SRUYQe0IsChhvcmcudGVuc29yZmxv",
            "dy5mcmFtZXdvcmtCC1R5cGVzUHJvdG9zUAH4AQFiBnByb3RvMw=="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { },
          new pbr::GeneratedClrTypeInfo(new[] {typeof(global::Vision.Tensorflow.Proto.DataType), }, null));
    }
    #endregion

  }
  #region Enums
  /// <summary>
  /// LINT.IfChange
  /// </summary>
  public enum DataType {
    /// <summary>
    /// Not a legal value for DataType.  Used to indicate a DataType field
    /// has not been set.
    /// </summary>
    [pbr::OriginalName("DT_INVALID")] DtInvalid = 0,
    /// <summary>
    /// Data types that all computation devices are expected to be
    /// capable to support.
    /// </summary>
    [pbr::OriginalName("DT_FLOAT")] DtFloat = 1,
    [pbr::OriginalName("DT_DOUBLE")] DtDouble = 2,
    [pbr::OriginalName("DT_INT32")] DtInt32 = 3,
    [pbr::OriginalName("DT_UINT8")] DtUint8 = 4,
    [pbr::OriginalName("DT_INT16")] DtInt16 = 5,
    [pbr::OriginalName("DT_INT8")] DtInt8 = 6,
    [pbr::OriginalName("DT_STRING")] DtString = 7,
    /// <summary>
    /// Single-precision complex
    /// </summary>
    [pbr::OriginalName("DT_COMPLEX64")] DtComplex64 = 8,
    [pbr::OriginalName("DT_INT64")] DtInt64 = 9,
    [pbr::OriginalName("DT_BOOL")] DtBool = 10,
    /// <summary>
    /// Quantized int8
    /// </summary>
    [pbr::OriginalName("DT_QINT8")] DtQint8 = 11,
    /// <summary>
    /// Quantized uint8
    /// </summary>
    [pbr::OriginalName("DT_QUINT8")] DtQuint8 = 12,
    /// <summary>
    /// Quantized int32
    /// </summary>
    [pbr::OriginalName("DT_QINT32")] DtQint32 = 13,
    /// <summary>
    /// Float32 truncated to 16 bits.  Only for cast ops.
    /// </summary>
    [pbr::OriginalName("DT_BFLOAT16")] DtBfloat16 = 14,
    /// <summary>
    /// Quantized int16
    /// </summary>
    [pbr::OriginalName("DT_QINT16")] DtQint16 = 15,
    /// <summary>
    /// Quantized uint16
    /// </summary>
    [pbr::OriginalName("DT_QUINT16")] DtQuint16 = 16,
    [pbr::OriginalName("DT_UINT16")] DtUint16 = 17,
    /// <summary>
    /// Double-precision complex
    /// </summary>
    [pbr::OriginalName("DT_COMPLEX128")] DtComplex128 = 18,
    [pbr::OriginalName("DT_HALF")] DtHalf = 19,
    [pbr::OriginalName("DT_RESOURCE")] DtResource = 20,
    /// <summary>
    /// Arbitrary C++ data types
    /// </summary>
    [pbr::OriginalName("DT_VARIANT")] DtVariant = 21,
    [pbr::OriginalName("DT_UINT32")] DtUint32 = 22,
    [pbr::OriginalName("DT_UINT64")] DtUint64 = 23,
    /// <summary>
    /// Do not use!  These are only for parameters.  Every enum above
    /// should have a corresponding value below (verified by types_test).
    /// </summary>
    [pbr::OriginalName("DT_FLOAT_REF")] DtFloatRef = 101,
    [pbr::OriginalName("DT_DOUBLE_REF")] DtDoubleRef = 102,
    [pbr::OriginalName("DT_INT32_REF")] DtInt32Ref = 103,
    [pbr::OriginalName("DT_UINT8_REF")] DtUint8Ref = 104,
    [pbr::OriginalName("DT_INT16_REF")] DtInt16Ref = 105,
    [pbr::OriginalName("DT_INT8_REF")] DtInt8Ref = 106,
    [pbr::OriginalName("DT_STRING_REF")] DtStringRef = 107,
    [pbr::OriginalName("DT_COMPLEX64_REF")] DtComplex64Ref = 108,
    [pbr::OriginalName("DT_INT64_REF")] DtInt64Ref = 109,
    [pbr::OriginalName("DT_BOOL_REF")] DtBoolRef = 110,
    [pbr::OriginalName("DT_QINT8_REF")] DtQint8Ref = 111,
    [pbr::OriginalName("DT_QUINT8_REF")] DtQuint8Ref = 112,
    [pbr::OriginalName("DT_QINT32_REF")] DtQint32Ref = 113,
    [pbr::OriginalName("DT_BFLOAT16_REF")] DtBfloat16Ref = 114,
    [pbr::OriginalName("DT_QINT16_REF")] DtQint16Ref = 115,
    [pbr::OriginalName("DT_QUINT16_REF")] DtQuint16Ref = 116,
    [pbr::OriginalName("DT_UINT16_REF")] DtUint16Ref = 117,
    [pbr::OriginalName("DT_COMPLEX128_REF")] DtComplex128Ref = 118,
    [pbr::OriginalName("DT_HALF_REF")] DtHalfRef = 119,
    [pbr::OriginalName("DT_RESOURCE_REF")] DtResourceRef = 120,
    [pbr::OriginalName("DT_VARIANT_REF")] DtVariantRef = 121,
    [pbr::OriginalName("DT_UINT32_REF")] DtUint32Ref = 122,
    [pbr::OriginalName("DT_UINT64_REF")] DtUint64Ref = 123,
  }

  #endregion

}

#endregion Designer generated code
