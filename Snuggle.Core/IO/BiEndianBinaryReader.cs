﻿using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Snuggle.Core.IO;

public class BiEndianBinaryReader : BinaryReader {
    public BiEndianBinaryReader(Stream input, bool isBigEndian = false, bool leaveOpen = false) : base(input, Encoding.UTF8, leaveOpen) {
        IsBigEndian = isBigEndian;
        Encoding = Encoding.UTF8;
    }

    public bool IsBigEndian { get; set; }

    public Encoding Encoding { get; }

    protected bool ShouldInvertEndianness => BitConverter.IsLittleEndian ? IsBigEndian : !IsBigEndian;
    public long Unconsumed => BaseStream.Length - BaseStream.Position;

    public static BiEndianBinaryReader FromArray(byte[] array, bool isBigEndian = false) {
        var ms = new MemoryStream(array) { Position = 0 };
        return new BiEndianBinaryReader(ms, isBigEndian);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Align(int alignment = 4) {
        if (BaseStream.Position % alignment == 0) {
            return;
        }

        var delta = (int) (alignment - BaseStream.Position % alignment);
        if (BaseStream.Position + delta > BaseStream.Length) {
            return;
        }

        BaseStream.Seek(delta, SeekOrigin.Current);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override decimal ReadDecimal() {
        Span<byte> span = stackalloc byte[16];
        Read(span);

        var lo = BinaryPrimitives.ReadInt32LittleEndian(span);
        var mid = BinaryPrimitives.ReadInt32LittleEndian(span[4..]);
        var hi = BinaryPrimitives.ReadInt32LittleEndian(span[8..]);
        var flags = BinaryPrimitives.ReadInt32LittleEndian(span[12..]);
        if (ShouldInvertEndianness) {
            lo = BinaryPrimitives.ReverseEndianness(lo);
            mid = BinaryPrimitives.ReverseEndianness(mid);
            hi = BinaryPrimitives.ReverseEndianness(hi);
            flags = BinaryPrimitives.ReverseEndianness(flags);
        }

        return new decimal(new[] { lo, mid, hi, flags });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override double ReadDouble() {
        Span<byte> span = stackalloc byte[8];
        Read(span);

        var value = BinaryPrimitives.ReadInt64LittleEndian(span);
        if (ShouldInvertEndianness) {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        return BitConverter.Int64BitsToDouble(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override float ReadSingle() {
        Span<byte> span = stackalloc byte[4];
        Read(span);

        var value = BinaryPrimitives.ReadInt32LittleEndian(span);
        if (ShouldInvertEndianness) {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        return BitConverter.Int32BitsToSingle(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override Half ReadHalf() {
        Span<byte> span = stackalloc byte[2];
        Read(span);

        var value = BinaryPrimitives.ReadInt16LittleEndian(span);
        if (ShouldInvertEndianness) {
            value = BinaryPrimitives.ReverseEndianness(value);
        }

        return BitConverter.Int16BitsToHalf(value);
    }

    public override short ReadInt16() {
        Span<byte> span = stackalloc byte[2];
        Read(span);

        var value = BinaryPrimitives.ReadInt16LittleEndian(span);
        return ShouldInvertEndianness ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    public override int ReadInt32() {
        Span<byte> span = stackalloc byte[4];
        Read(span);

        var value = BinaryPrimitives.ReadInt32LittleEndian(span);
        return ShouldInvertEndianness ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    public override long ReadInt64() {
        Span<byte> span = stackalloc byte[8];
        Read(span);

        var value = BinaryPrimitives.ReadInt64LittleEndian(span);
        return ShouldInvertEndianness ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    public override ushort ReadUInt16() {
        Span<byte> span = stackalloc byte[2];
        Read(span);

        var value = BinaryPrimitives.ReadUInt16LittleEndian(span);
        return ShouldInvertEndianness ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    public override uint ReadUInt32() {
        Span<byte> span = stackalloc byte[4];
        Read(span);

        var value = BinaryPrimitives.ReadUInt32LittleEndian(span);
        return ShouldInvertEndianness ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    public override ulong ReadUInt64() {
        Span<byte> span = stackalloc byte[8];
        Read(span);

        var value = BinaryPrimitives.ReadUInt64LittleEndian(span);
        return ShouldInvertEndianness ? BinaryPrimitives.ReverseEndianness(value) : value;
    }

    public string ReadString32(int align = 4) {
        var length = ReadInt32();
        if (length < 0) {
            throw new InvalidDataException();
        }

        Span<byte> span = new byte[length];
        Read(span);
        if (align > 1) {
            Align(align);
        }

        return Encoding.GetString(span);
    }

    public string ReadNullString(int maxLength = 0) {
        var sb = new StringBuilder();
        byte b;
        while ((b = ReadByte()) != 0) {
            sb.Append((char) b);

            if (maxLength > 0 && sb.Length >= maxLength) {
                break;
            }
        }

        return sb.ToString();
    }

    public Span<byte> ReadArray(int count) {
        Span<byte> span = new byte[count];
        Read(span);
        if (ShouldInvertEndianness) {
            throw new NotSupportedException("Cannot invert endianness of arrays");
        }

        return span;
    }

    public override bool ReadBoolean() {
        try {
            return ReadByte() == 1;
        } catch {
            return false;
        }
    }

    public Span<T> ReadArray<T>(int count) where T : struct {
        Span<T> span = new T[count];
        Read(MemoryMarshal.AsBytes(span));
        if (ShouldInvertEndianness) {
            throw new NotSupportedException("Cannot invert endianness of arrays");
        }

        return span;
    }

    public Memory<byte> ReadMemory(long count) {
        Memory<byte> memory = new byte[count];
        Read(memory.Span);
        return memory;
    }

    public Memory<T> ReadMemory<T>(long count) where T : struct {
        Memory<T> memory = new T[count];
        Read(MemoryMarshal.AsBytes(memory.Span));
        if (ShouldInvertEndianness) {
            throw new NotSupportedException("Cannot invert endianness of arrays");
        }

        return memory;
    }

    public T ReadStruct<T>() where T : struct {
        Span<T> span = new T[1];
        Read(MemoryMarshal.AsBytes(span));
        if (ShouldInvertEndianness) {
            throw new NotSupportedException("Cannot invert endianness of structs");
        }

        return span[0];
    }
}
