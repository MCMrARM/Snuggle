﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Equilibrium.IO;
using Equilibrium.Meta;
using Equilibrium.Models;
using Equilibrium.Models.Bundle;
using JetBrains.Annotations;

namespace Equilibrium {
    [PublicAPI]
    public class AssetCollection : IDisposable {
        public List<Bundle> Bundles { get; } = new();
        public Dictionary<string, SerializedFile> Files { get; } = new(StringComparer.InvariantCultureIgnoreCase);
        public Dictionary<string, (object Tag, IFileHandler Handler)> ResourceStreams { get; } = new(StringComparer.InvariantCultureIgnoreCase);
        public Dictionary<string, (object Tag, IFileHandler Handler)> Resources { get; } = new(StringComparer.InvariantCultureIgnoreCase);

        public void Dispose() {
            Reset();
            GC.SuppressFinalize(this);
        }

        public void Reset() {
            foreach (var bundle in Bundles) {
                bundle.Dispose();
            }

            Bundles.Clear();
            Files.Clear();
            ResourceStreams.Clear();
            Resources.Clear();

            GC.Collect();
        }

        public void LoadBundle(Bundle bundle) {
            var handler = new BundleStreamHandler(bundle);
            foreach (var block in bundle.Container.Blocks) {
                if (block.Flags.HasFlag(UnityBundleBlockFlags.SerializedFile)) {
                    LoadSerializedFile(bundle.OpenFile(block), block, handler, false, bundle.Header.Version, bundle.Options);
                } else {
                    var ext = Path.GetExtension(block.Path)[1..].ToLower();
                    switch (ext) {
                        case "ress":
                            ResourceStreams[block.Path] = (block, handler);
                            break;
                        case "resource":
                            Resources[block.Path] = (block, handler);
                            break;
                        default:
                            // ??
                            continue;
                    }
                }
            }

            Bundles.Add(bundle);
        }

        public void LoadBundle(Stream dataStream, object tag, IFileHandler handler, EquilibriumOptions? options, bool leaveOpen = false) => LoadBundle(new Bundle(dataStream, tag, handler, options ?? EquilibriumOptions.Default, leaveOpen));

        public void LoadBundle(string path, EquilibriumOptions? options, bool leaveOpen = false) => LoadBundle(File.OpenRead(path), path, FileStreamHandler.Instance.Value, options, leaveOpen);

        public void LoadBundleSequence(Stream dataStream, object tag, IFileHandler handler, EquilibriumOptions? options = null, int align = 1, bool leaveOpen = false) {
            var bundles = Bundle.OpenBundleSequence(dataStream, tag, handler, align, options, leaveOpen);
            foreach (var bundle in bundles) {
                LoadBundle(bundle);
            }
        }

        public void LoadBundleSequence(string path, EquilibriumOptions? options = null, int align = 1) => LoadBundleSequence(File.OpenRead(path), path, MultiStreamHandler.FileInstance.Value, options, align);

        public void LoadSerializedFile(Stream dataStream, object tag, IFileHandler handler, bool leaveOpen = false, UnityVersion? fallbackVersion = null, EquilibriumOptions? options = null) {
            var path = tag switch {
                UnityBundleBlock block => block.Path,
                string str => Path.GetFileName(str),
                _ => throw new InvalidOperationException(),
            };

            if (Files.ContainsKey(path)) {
                if (!leaveOpen) {
                    dataStream.Dispose();
                }

                return;
            }

            var file = new SerializedFile(dataStream, tag, handler, options ?? EquilibriumOptions.Default, true) { Assets = this, Name = path };
            if (file.Version == UnityVersion.MinValue &&
                fallbackVersion != null &&
                fallbackVersion != UnityVersion.MinValue) {
                file.Version = fallbackVersion.Value;
            }

            foreach (var objectInfo in file.ObjectInfos) {
                try {
                    options?.Reporter?.SetStatus($"Processing {objectInfo.PathId} ({objectInfo.ClassId:G})");
                    file.Objects[objectInfo.PathId] = ObjectFactory.GetInstance(dataStream, objectInfo, file);
                } catch (Exception e) {
                    Debug.WriteLine($"Failed to decode {objectInfo.PathId} (type {objectInfo.ClassId}) on file {file.Name}.");
                    Debug.WriteLine(e);
                    file.Objects[objectInfo.PathId] = ObjectFactory.GetInstance(dataStream, objectInfo, file, ClassId.Object);
                }
            }

            Files[path] = file;

            if (!leaveOpen) {
                dataStream.Dispose();
            }
        }

        public void LoadSerializedFile(string path, EquilibriumOptions? options) => LoadSerializedFile(File.OpenRead(path), path, FileStreamHandler.Instance.Value, false, null, options);

        public void LoadFile(string path, EquilibriumOptions? options = null) => LoadFile(File.OpenRead(path), path, MultiStreamHandler.FileInstance.Value, options);

        private void LoadFile(Stream dataStream, object tag, IFileHandler handler, EquilibriumOptions? options = null, int align = 1, bool leaveOpen = false) {
            if (SerializedFile.IsSerializedFile(dataStream)) {
                LoadSerializedFile(dataStream, tag, handler, leaveOpen, null, options);
            } else if (Bundle.IsBundleFile(dataStream)) {
                LoadBundleSequence(dataStream, tag, handler, options, align, leaveOpen);
            } else {
                if (tag is not string path) {
                    throw new InvalidOperationException();
                }

                path = Path.GetFileName(path);
                var ext = Path.GetExtension(path)[1..].ToLower();
                switch (ext) {
                    case "ress":
                        ResourceStreams[path] = (tag, handler);
                        break;
                    case "resource":
                        Resources[path] = (tag, handler);
                        break;
                    default:
                        return;
                }
            }
        }
    }
}
