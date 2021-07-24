﻿using System;
using Equilibrium.IO;
using Equilibrium.Models;
using Equilibrium.Models.Serialization;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Equilibrium.Implementations {
    [PublicAPI]
    public class SerializedObject : IEquatable<SerializedObject> {
        public SerializedObject(BiEndianBinaryReader reader, UnityObjectInfo info, SerializedFile serializedFile) {
            SerializedFile = serializedFile;
            PathId = info.PathId;
            ClassId = info.ClassId;
        }

        public override string ToString() => ClassId.ToString("G");

        public override bool Equals(object? obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            return obj is SerializedObject unityObject && Equals(unityObject);
        }

        public bool Equals(SerializedObject? other) {
            if (ReferenceEquals(null, other)) {
                return false;
            }

            if (ReferenceEquals(this, other)) {
                return true;
            }

            return PathId == other.PathId && ClassId == other.ClassId;
        }

        public ulong PathId { get; init; }
        public ClassId ClassId { get; init; }

        [JsonIgnore]
        public SerializedFile SerializedFile { get; init; }

        [JsonIgnore]
        public virtual bool ShouldDeserialize { get; set; }

        public virtual void Deserialize(BiEndianBinaryReader reader) {
            ShouldDeserialize = false;
        }

        public void Deserialize() {
            using var reader = BiEndianBinaryReader.FromArray(SerializedFile.OpenFile(PathId), SerializedFile.Header.IsBigEndian);
            Deserialize(reader);
        }

        public override int GetHashCode() => PathId.GetHashCode();
    }
}
