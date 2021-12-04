﻿using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Snuggle.Core.Implementations;
using Snuggle.Handlers;

namespace Snuggle.Converters;

public class SpriteToBitmapConverter : MarkupExtension, IValueConverter {
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) => value is not Sprite sprite ? null : new TaskCompletionNotifier<BitmapSource?>(sprite, ConvertSprite(sprite, Dispatcher.CurrentDispatcher));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException($"{nameof(Texture2DToBitmapConverter)} only supports converting to BitmapSource");

    private static async Task<BitmapSource?> ConvertSprite(Sprite sprite, Dispatcher dispatcher) {
        return await SnuggleCore.Instance.WorkerAction(
            "DecodeTexture",
            _ => {
                sprite.Deserialize(SnuggleCore.Instance.Settings.ObjectOptions);
                var memory = SnuggleSpriteFile.ConvertSprite(sprite, SnuggleCore.Instance.Settings.ObjectOptions, SnuggleCore.Instance.Settings.ExportOptions.UseDirectTex, out var textureFormat);
                return memory.Length == 0 ? null : dispatcher.Invoke(() => new RGBABitmapSource(memory, (int) sprite.Rect.W, (int) sprite.Rect.H, textureFormat));
            },
            true);
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
