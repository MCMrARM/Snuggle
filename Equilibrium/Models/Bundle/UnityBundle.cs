﻿using Equilibrium.IO;
using Equilibrium.Meta;
using JetBrains.Annotations;

namespace Equilibrium.Models.Bundle {
    [PublicAPI]
    public record UnityBundle(
        string Signature,
        int FormatVersion,
        string EngineVersion,
        string EngineRevision) {
        public UnityFormat Format { get; } = (UnityFormat) Signature[5];

        public UnityVersion? Version { get; } = UnityVersion.ParseSafe(EngineVersion);
        public UnityVersion? Revision { get; } = UnityVersion.ParseSafe(EngineRevision);

        public static UnityBundle FromReader(BiEndianBinaryReader reader) =>
            new(
                reader.ReadNullString(),
                reader.ReadInt32(),
                reader.ReadNullString(),
                reader.ReadNullString());
    }
}
