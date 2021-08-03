﻿using Equilibrium.IO;
using Equilibrium.Options;
using JetBrains.Annotations;

namespace Equilibrium.Interfaces {
    [PublicAPI]
    public interface ISerialized {
        public void Deserialize(BiEndianBinaryReader reader, ObjectDeserializationOptions options);
        public void Serialize(BiEndianBinaryWriter writer, AssetSerializationOptions options);
        public void Free();
    }
}
