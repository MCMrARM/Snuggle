﻿using Snuggle.Core.IO;
using Snuggle.Core.Meta;

namespace Snuggle.Core.Models.Objects.Graphics;

public record BoneWeight(float[] Weights, int[] Indices) {
    public static BoneWeight Default => new(new float[] { 0, 0, 0, 0 }, new[] { 0, 0, 0, 0 });

    public static BoneWeight FromReader(BiEndianBinaryReader reader, SerializedFile file) => new(reader.ReadArray<float>(4).ToArray(), reader.ReadArray<int>(4).ToArray());

    public void ToWriter(BiEndianBinaryWriter writer, SerializedFile serializedFile, UnityVersion targetVersion) {
        writer.WriteArray(Weights, false);
        writer.WriteArray(Indices, false);
    }
}
