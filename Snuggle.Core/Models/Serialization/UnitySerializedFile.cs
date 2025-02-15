﻿using System.IO;
using Snuggle.Core.IO;
using Snuggle.Core.Meta;
using Snuggle.Core.Options;

namespace Snuggle.Core.Models.Serialization;

public record UnitySerializedFile(
    int HeaderSize,
    long Size,
    UnitySerializedFileVersion FileVersion,
    long Offset,
    bool IsBigEndian,
    ulong LargeAddressableFlags,
    string EngineVersion,
    UnityPlatform Platform,
    bool TypeTreeEnabled) {
    public UnityVersion? Version { get; } = UnityVersion.ParseSafe(EngineVersion);

    public static UnitySerializedFile FromReader(BiEndianBinaryReader reader, SnuggleCoreOptions options) {
        var headerSize = reader.ReadInt32();
        long size = reader.ReadInt32();
        var version = (UnitySerializedFileVersion) reader.ReadUInt32();
        long offset = reader.ReadInt32();
        var laf = 0ul;

        if (version < UnitySerializedFileVersion.HeaderContentAtFront) {
            reader.BaseStream.Seek(size - headerSize, SeekOrigin.Begin);
        }

        var isBigEndian = reader.ReadBoolean();
        reader.Align();

        if (version >= UnitySerializedFileVersion.LargeFiles) {
            headerSize = reader.ReadInt32();
            size = reader.ReadInt64();
            offset = reader.ReadInt64();
            laf = reader.ReadUInt64();
        }

        reader.IsBigEndian = isBigEndian;

        var engineVersion = string.Empty;
        if (version >= UnitySerializedFileVersion.UnityVersion) {
            engineVersion = reader.ReadNullString();
        }

        var targetPlatform = UnityPlatform.Unknown;
        if (version >= UnitySerializedFileVersion.TargetPlatform) {
            targetPlatform = (UnityPlatform) reader.ReadInt32();
        }

        var typeTreeEnabled = true;
        if (version >= UnitySerializedFileVersion.TypeTreeEnabledSwitch) {
            typeTreeEnabled = reader.ReadBoolean();
        }

        return new UnitySerializedFile(
            headerSize,
            size,
            version,
            offset,
            isBigEndian,
            laf,
            engineVersion,
            targetPlatform,
            typeTreeEnabled);
    }
}
