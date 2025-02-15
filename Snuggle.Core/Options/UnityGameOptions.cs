﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Snuggle.Core.Meta;
using Snuggle.Core.Options.Game;

namespace Snuggle.Core.Options;

public record UnityGameOptions {
    public const int LatestVersion = 1;

    static UnityGameOptions() => Default = new UnityGameOptions().Migrate();

    public static UnityGameOptions Default { get; }
    public int Version { get; set; } = LatestVersion;
    public Dictionary<UnityGame, JsonElement> StorageMap { get; set; } = new();

    public bool TryGetOptionsObject<T>(UnityGame game, [MaybeNullWhen(false)] out T options) where T : IUnityGameOptions, new() {
        options = default;
        if (!StorageMap.TryGetValue(game, out var anonymousOptionsObject)) {
            return false;
        }

        if (anonymousOptionsObject.ValueKind != JsonValueKind.Object) {
            return false;
        }

        options = anonymousOptionsObject.Deserialize<T>(SnuggleCoreOptions.JsonOptions);
        if (options == null) {
            return false;
        }

        options = (T) options.Migrate();
        return true;
    }

    public void SetOptions(UnityGame game, object options) {
        StorageMap[game] = JsonSerializer.SerializeToElement(options, options.GetType(), SnuggleCoreOptions.JsonOptions);
    }

    public void MigrateOptions<T>(UnityGame game) where T : IUnityGameOptions, new() {
        SetOptions(game, !TryGetOptionsObject<T>(game, out var options) ? new T() : options.Migrate());
    }

    public UnityGameOptions Migrate() {
        MigrateOptions<UniteOptions>(UnityGame.PokemonUnite);
        return this;
    }
}
