using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Svg.Skia;
using Svg.Skia;

namespace BnbnavNetClient.Helpers;

public static class DrawingContextExtensions
{
    static readonly Dictionary<string, SKSvg> SvgCache = new();
    
    public static void DrawSvgUrl(this DrawingContext context, string url, Rect rect, double angle = 0)
    {
        SKSvg? svg = null;

        if (SvgCache.TryGetValue(url, out var outSvg))
        {
            svg = outSvg;
        }
        else
        {
            var assetLoader = AvaloniaLocator.Current.GetService<IAssetLoader>()!;
            var uri = new Uri(url);
            if (assetLoader.Exists(uri))
            {
                var asset = assetLoader.Open(uri);

                svg = new SKSvg();
                svg.Load(asset);
                if (svg.Picture is null)
                    return;
                SvgCache.Add(url, svg);
            }
        }

        if (svg is null)
            return;

        var sourceSize = new Size(svg.Picture!.CullRect.Width, svg.Picture.CullRect.Height);
        var scaleMatrix = Matrix.CreateScale(
            rect.Width / sourceSize.Width,
            rect.Height / sourceSize.Height);
        var translateMatrix = Matrix.CreateTranslation(
            rect.X * sourceSize.Width / rect.Width,
            rect.Y * sourceSize.Height / rect.Height);
        var rotateMatrix = Matrix.CreateRotation(MathHelper.ToRad(angle));
        var preRotateMatrix = Matrix.CreateTranslation(-sourceSize.Width / 2, -sourceSize.Width / 2);

        using (context.PushClip(rect))
        using (context.PushPreTransform(translateMatrix * scaleMatrix))
        using (context.PushTransformContainer())
        using (context.PushPostTransform(preRotateMatrix))
        using (context.PushPostTransform(rotateMatrix))
        using (context.PushPostTransform(preRotateMatrix.Invert()))
            context.Custom(new SvgCustomDrawOperation(rect, svg));
    }
}