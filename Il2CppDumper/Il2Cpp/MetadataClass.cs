using System;

namespace Il2CppDumper
{
    public class Il2CppGlobalMetadataHeader
    {
        public uint sanity;
        public int version;
        public uint stringLiteralOffset; // string data for managed code
        public int stringLiteralSize;
        public uint stringLiteralDataOffset;
        public int stringLiteralDataSize;
        public uint stringOffset; // string data for metadata
        public int stringSize;
        public uint eventsOffset; // Il2CppEventDefinition
        public int eventsSize;
        public uint propertiesOffset; // Il2CppPropertyDefinition
        public int propertiesSize;
        public uint methodsOffset; // Il2CppMethodDefinition
        public int methodsSize;
        public uint parameterDefaultValuesOffset; // Il2CppParameterDefaultValue
        public int parameterDefaultValuesSize;
        public uint fieldDefaultValuesOffset; // Il2CppFieldDefaultValue
        public int fieldDefaultValuesSize;
        public uint fieldAndParameterDefaultValueDataOffset; // uint8_t
        public int fieldAndParameterDefaultValueDataSize;
        public int fieldMarshaledSizesOffset; // Il2CppFieldMarshaledSize
        public int fieldMarshaledSizesSize;
        public uint parametersOffset; // Il2CppParameterDefinition
        public int parametersSize;
        public uint fieldsOffset; // Il2CppFieldDefinition
        public int fieldsSize;
        public uint genericParametersOffset; // Il2CppGenericParameter
        public int genericParametersSize;
        public uint genericParameterConstraintsOffset; // TypeIndex
        public int genericParameterConstraintsSize;
        public uint genericContainersOffset; // Il2CppGenericContainer
        public int genericContainersSize;
        public uint nestedTypesOffset; // TypeDefinitionIndex
        public int nestedTypesSize;
        public uint interfacesOffset; // TypeIndex
        public int interfacesSize;
        public uint vtableMethodsOffset; // EncodedMethodIndex
        public int vtableMethodsSize;
        public int interfaceOffsetsOffset; // Il2CppInterfaceOffsetPair
        public int interfaceOffsetsSize;
        public uint typeDefinitionsOffset; // Il2CppTypeDefinition
        public int typeDefinitionsSize;
        public uint rgctxEntriesOffset; // Il2CppRGCTXDefinition
        public int rgctxEntriesCount;
        public uint imagesOffset; // Il2CppImageDefinition
        public int imagesSize;
        public uint assembliesOffset; // Il2CppAssemblyDefinition
        public int assembliesSize;
        public uint metadataUsageListsOffset; // Il2CppMetadataUsageList
        public int metadataUsageListsCount;
        public uint metadataUsagePairsOffset; // Il2CppMetadataUsagePair
        public int metadataUsagePairsCount;
        public uint fieldRefsOffset; // Il2CppFieldRef
        public int fieldRefsSize;
        public int referencedAssembliesOffset; // int32_t
        public int referencedAssembliesSize;
        public uint attributesInfoOffset; // Il2CppCustomAttributeTypeRange
        public int attributesInfoCount;
        public uint attributeTypesOffset; // TypeIndex
        public int attributeTypesCount;
        public int unresolvedVirtualCallParameterTypesOffset; // TypeIndex
        public int unresolvedVirtualCallParameterTypesSize;
        public int unresolvedVirtualCallParameterRangesOffset; // Il2CppRange
        public int unresolvedVirtualCallParameterRangesSize;
        public int windowsRuntimeTypeNamesOffset; // Il2CppWindowsRuntimeTypeNamePair
        public int windowsRuntimeTypeNamesSize;
    }


    /* 2 bytes shrink */
    public class Il2CppAssemblyDefinition
    {   
        public int imageIndex;
        public short customAttributeIndex;
        public int referencedAssemblyStart;
        public int referencedAssemblyCount;
        public Il2CppAssemblyNameDefinition aname;
    }

    public class Il2CppAssemblyNameDefinition
    {
        public uint nameIndex;
        public uint cultureIndex;
        public int hashValueIndex;
        public uint publicKeyIndex;
        public uint hash_alg;
        public int hash_len;
        public uint flags;
        public int major;
        public int minor;
        public int build;
        public int revision;
        [ArrayLength(Length = 8)]
        public byte[] public_key_token;
    }


     /* 20 bytes*/
    public class Il2CppImageDefinition
    {
        public uint nameIndex;
        public int assemblyIndex;
        public int typeStart;
        public uint typeCount;
        public int entryPointIndex;

    }


    /* 80 bytes*/
    public class Il2CppTypeDefinition
    {
        public uint nameIndex;
        public uint namespaceIndex;
        public int byvalTypeIndex;
        public int byrefTypeIndex;
        public int declaringTypeIndex;
        public int parentIndex;
        public int elementTypeIndex; // we can probably remove this one. Only used for enums
        public uint flags;
        public int fieldStart;
        public int methodStart;
        public int vtableStart;
        public short customAttributeIndex;
        public short rgctxStartIndex;
        public short rgctxCount;
        public short genericContainerIndex;
        public short eventStart;
        public short propertyStart;
        public short nestedTypesStart;
        public short interfacesStart;
        public short interfaceOffsetsStart;
        public ushort method_count;
        public ushort property_count;
        public ushort field_count;
        public ushort event_count;
        public ushort nested_type_count;
        public ushort vtable_count;
        public ushort interfaces_count;
        public ushort interface_offsets_count;

        // bitfield to portably encode boolean values as single bits
        // 01 - valuetype;
        // 02 - enumtype;
        // 03 - has_finalize;
        // 04 - has_cctor;
        // 05 - is_blittable;
        // 06 - is_import_or_windows_runtime;
        // 07-10 - One of nine possible PackingSize values (0, 1, 2, 4, 8, 16, 32, 64, or 128)
        // 11 - PackingSize is default
        // 12 - ClassSize is default
        // 13-16 - One of nine possible PackingSize values (0, 1, 2, 4, 8, 16, 32, 64, or 128) - the specified packing size (even for explicit layouts)
        public ushort bitfield;
        public bool IsValueType => (bitfield & 0x1) == 1;
        public bool IsEnum => ((bitfield >> 1) & 0x1) == 1;
    }

    /* 42 bytes */
    public class Il2CppMethodDefinition
    {
        public uint nameIndex;
        public int methodIndex;
        public int returnType;
        public int parameterStart;
        public uint token;
        public ushort declaringType;
        public short customAttributeIndex;
        public short genericContainerIndex;
        public ushort invokerIndex;
        public short delegateWrapperIndex;
        public short rgctxStartIndex;
        public ushort rgctxCount;
        public ushort flags;
        public ushort iflags;
        public ushort slot;
        public ushort parameterCount;
    }


    /* 10 bytes*/
    public class Il2CppParameterDefinition
    {
        public uint nameIndex;
        public short customAttributeIndex;
        public int typeIndex;
    }


    /* 10 bytes */
    public class Il2CppFieldDefinition
    {
        public uint nameIndex;
        public int typeIndex;
        public short customAttributeIndex;
    }


    public class Il2CppFieldDefaultValue
    {
        public int fieldIndex;
        public int typeIndex;
        public int dataIndex;
    }


    /* 12 bytes*/
    public class Il2CppPropertyDefinition
    {
        public uint nameIndex;
        public short get;
        public short set;
        public ushort attrs;
        public ushort customAttributeIndex;
    }

    /* 4 bytes*/
    public class Il2CppCustomAttributeTypeRange
    {
        public ushort start;
        public ushort count;
    }

    public class Il2CppMetadataUsageList
    {
        public uint start;
        public ushort count;
    }


    public class Il2CppMetadataUsagePair
    {
        public uint destinationIndex;
        public uint encodedSourceIndex;
    }


    public class Il2CppStringLiteral
    {
        public uint length;
        public int dataIndex;
    }

    public class Il2CppParameterDefaultValue
    {
        public int parameterIndex;
        public int typeIndex;
        public int dataIndex;
    }

    /* 16 bytes*/
    public class Il2CppEventDefinition
    {
        public uint nameIndex;
        public int typeIndex;
        public short add;
        public short remove;
        public short raise;
        public short customAttributeIndex;
    }


    /* 12 bytes*/
    public class Il2CppGenericContainer
    {
        /* index of the generic type definition or the generic method definition corresponding to this container */
        public int ownerIndex; // either index into Il2CppClass metadata array or Il2CppMethodDefinition array
        public int genericParameterStart;
        /* If true, we're a generic method, otherwise a generic type definition. */
        public short type_argc;
        /* Our type parameters. */
        public short is_method;
    }


    /* 6 bytes*/
    public class Il2CppFieldRef
    {
        public int typeIndex;
        public short fieldIndex; // local offset into type fields
    }


    /* 14 bytes*/
    public class Il2CppGenericParameter
    {
        public uint nameIndex;  /* Type or method this parameter was defined in. */
        public ushort ownerIndex;
        public short constraintsStart;
        public short constraintsCount;
        public ushort num;
        public ushort flags;
    }


    public enum Il2CppRGCTXDataType
    {
        IL2CPP_RGCTX_DATA_INVALID,
        IL2CPP_RGCTX_DATA_TYPE,
        IL2CPP_RGCTX_DATA_CLASS,
        IL2CPP_RGCTX_DATA_METHOD,
        IL2CPP_RGCTX_DATA_ARRAY,
        IL2CPP_RGCTX_DATA_CONSTRAINED,
    }

    public class Il2CppRGCTXDefinitionData
    {
        public int rgctxDataDummy;
        public int methodIndex => rgctxDataDummy;
        public int typeIndex => rgctxDataDummy;
    }

    public class Il2CppRGCTXDefinition
    {
        public Il2CppRGCTXDataType type => (Il2CppRGCTXDataType)type_pre29;
        public int type_pre29;
        public Il2CppRGCTXDefinitionData data;
    }

    public enum Il2CppMetadataUsage
    {
        kIl2CppMetadataUsageInvalid,
        kIl2CppMetadataUsageTypeInfo,
        kIl2CppMetadataUsageIl2CppType,
        kIl2CppMetadataUsageMethodDef,
        kIl2CppMetadataUsageFieldInfo,
        kIl2CppMetadataUsageStringLiteral,
        kIl2CppMetadataUsageMethodRef,
    };

}
