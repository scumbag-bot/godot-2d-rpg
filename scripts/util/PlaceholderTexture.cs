using Godot;
using System;

namespace rpg_game.scripts.util;

public static class PlaceholderTexture
{
    public static ImageTexture RectColor(int w, int h, Color color)
    {
        var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
        img.Fill(color);
        return ImageTexture.CreateFromImage(img);
    }

    public static ImageTexture LetterSprite(string letter, Color bg, Color fg, int size = 64)
    {
        var img = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
        img.Fill(bg);
        // crude letter mark: a centered plus sign per first letter
        var c = size / 2;
        int t = Math.Max(2, size / 12);
        for (int y = -t; y <= t; y++)
            for (int x = -size / 3; x <= size / 3; x++)
                img.SetPixel(c + x, c + y, fg);
        for (int x = -t; x <= t; x++)
            for (int y = -size / 3; y <= size / 3; y++)
                img.SetPixel(c + x, c + y, fg);
        return ImageTexture.CreateFromImage(img);
    }
}
