using SDL2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

static partial class Engine
{
    public static string GetAssetPath(string path)
    {
        return Path.Combine("Assets", path);
    }

    public static void CopyToBaseDirectory(string file)
    {
        if (!File.Exists(file)) return;
        var targetBase = Directory.GetCurrentDirectory();
        string sourceBase = targetBase;

        while (true)
        {
            // Figure out if we're in the project's base directory:
            if (File.Exists(Path.Combine(sourceBase, "Game.sln")) &&
                Directory.Exists(Path.Combine(sourceBase, "Assets")))
            {
                break;
            }

            // Try again in the parent directory, stopping when we run out of places to look:
            DirectoryInfo parent = Directory.GetParent(sourceBase);
            if (parent == null)
            {
                break;
            }
            else
            {
                sourceBase = parent.FullName;
            }
        }

        // Figure out if we're in the project's base directory:
        if (!File.Exists(Path.Combine(sourceBase, "Game.sln")) &&
            !Directory.Exists(Path.Combine(sourceBase, "Assets"))) throw new FileNotFoundException("Can't find base folder");
        
        var lines = File.ReadAllLines(file);
        var targetPath = Path.Combine(sourceBase, file);

        var sw = File.CreateText(targetPath);
        foreach (var l in lines)
        {
            sw.WriteLine(l);
        }

        sw.Close();

        Debug.WriteLine("Copied to " + targetPath);
    }

    public static void ReloadAssets()
    {
        // ======================================================================================
        // Copy assets and libraries into the working directory
        // ======================================================================================

        string targetBase = Directory.GetCurrentDirectory();
        string sourceBase = targetBase;

        while (true)
        {
            // Figure out if we're in the project's base directory:
            if (File.Exists(Path.Combine(sourceBase, "Game.sln")) &&
                Directory.Exists(Path.Combine(sourceBase, "Assets")))
            {
                MirrorDirectory(Path.Combine(sourceBase, "Assets"), Path.Combine(targetBase, "Assets"), true);
                break;
            }

            // Try again in the parent directory, stopping when we run out of places to look:
            DirectoryInfo parent = Directory.GetParent(sourceBase);
            if (parent == null)
            {
                break;
            }
            else
            {
                sourceBase = parent.FullName;
            }
        }
    }

    /// <summary>
    /// Loads a texture from the Assets directory. Supports the following formats: BMP, GIF, JPEG, PNG, SVG, TGA, TIFF, WEBP.
    /// </summary>
    /// <param name="path">The path to the texture file, relative to the Assets directory.</param>
    public static Texture LoadTexture(string path)
    {
        IntPtr handle = SDL_image.IMG_LoadTexture(Renderer, GetAssetPath(path));
        if (handle == IntPtr.Zero)
        {
            throw new Exception("Failed to load texture.");
        }

        uint format;
        int access, width, height;
        SDL.SDL_QueryTexture(handle, out format, out access, out width, out height);

        return new Texture(handle, width, height);
    }

    /// <summary>
    /// Loads a resizable texture from the Assets directory. Supports the following formats: BMP, GIF, JPEG, PNG, SVG, TGA, TIFF, WEBP.
    /// See the documentation for an explanation of what these parameters _actually_ mean.
    /// </summary>
    /// <param name="path">The path to the texture file, relative to the Assets directory.</param>
    /// <param name="leftOffset">The resize offset from the left of the texture (in pixels).</param>
    /// <param name="rightOffset">The resize offset from the right of the texture (in pixels).</param>
    /// <param name="topOffset">The resize offset from the top of the texture (in pixels).</param>
    /// <param name="bottomOffset">The resize offset from the bottom of the texture (in pixels).</param>
    public static ResizableTexture LoadResizableTexture(string path, int leftOffset, int rightOffset, int topOffset, int bottomOffset)
    {
        IntPtr handle = SDL_image.IMG_LoadTexture(Renderer, GetAssetPath(path));
        if (handle == IntPtr.Zero)
        {
            throw new Exception("Failed to load texture.");
        }

        uint format;
        int access, width, height;
        SDL.SDL_QueryTexture(handle, out format, out access, out width, out height);

        // Convert the relative offsets (from the edges) into absolute offsets (from the origin):
        rightOffset = width - rightOffset - 1;
        bottomOffset = height - bottomOffset - 1;

        if (leftOffset < 0 || rightOffset >= width || topOffset < 0 || bottomOffset >= height || leftOffset > rightOffset || topOffset > bottomOffset)
        {
            throw new Exception("Invalid offset parameter.");
        }

        return new ResizableTexture(handle, width, height, leftOffset, rightOffset, topOffset, bottomOffset);
    }

    /// <summary>
    /// Loads a font from the Assets directory for a single text Size. Supports the following formats: TTF, FON.
    /// </summary>
    /// <param name="path">The path to the font file, relative to the Assets directory.</param>
    /// <param name="pointSize">The Size of the text that will be rendered by this font (in points).</param>
    public static Font LoadFont(string path, int pointSize)
    {
        IntPtr handle = SDL_ttf.TTF_OpenFont(GetAssetPath(path), pointSize);
        if (handle == IntPtr.Zero)
        {
            throw new Exception("Failed to load font.");
        }

        return new Font(handle, pointSize);
    }

    /// <summary>
    /// Loads a sound file from the Assets directory. Supports the following formats: WAV, OGG.
    /// </summary>
    /// <param name="path">The path to the sound file, relative to the Assets directory.</param>
    public static Sound LoadSound(string path)
    {
        IntPtr handle = SDL_mixer.Mix_LoadWAV(GetAssetPath(path));
        if (handle == IntPtr.Zero)
        {
            throw new Exception("Failed to load sound.");
        }

        return new Sound(handle);
    }

    /// <summary>
    /// Loads a music file from the Assets directory. Supports the following formats: WAV, OGG, MP3, FLAC.
    /// </summary>
    /// <param name="path">The path to the music file, relative to the Assets directory.</param>
    public static Music LoadMusic(string path)
    {
        IntPtr handle = SDL_mixer.Mix_LoadMUS(GetAssetPath(path));
        if (handle == IntPtr.Zero)
        {
            throw new Exception("Failed to load music.");
        }

        return new Music(handle);
    }
}

/// <summary>
/// A handle to a texture. These should only be created by calling LoadTexture().
/// </summary>
class Texture
{
    public readonly IntPtr Handle;
    public readonly int Width;
    public readonly int Height;
    public readonly Vector2 Size;

    public Texture(IntPtr handle, int width, int height)
    {
        Handle = handle;
        Width = width;
        Height = height;
        Size = new Vector2(width, height);
    }
}

/// <summary>
/// A handle to a resizable texture. These should only be created by calling LoadResizableTexture().
/// </summary>
class ResizableTexture
{
    public readonly IntPtr Handle;
    public readonly int Width;
    public readonly int Height;
    public readonly int LeftOffset;
    public readonly int RightOffset;
    public readonly int TopOffset;
    public readonly int BottomOffset;

    public ResizableTexture(IntPtr handle, int width, int height, int leftOffset, int rightOffset, int topOffset, int bottomOffset)
    {
        Handle = handle;
        Width = width;
        Height = height;
        LeftOffset = leftOffset;
        RightOffset = rightOffset;
        TopOffset = topOffset;
        BottomOffset = bottomOffset;
    }
}

/// <summary>
/// A handle to a font. These should only be created by calling LoadFont().
/// </summary>
class Font
{
    public readonly IntPtr Handle;

    public readonly int PointSize;

    public Font(IntPtr handle, int pointSize)
    {
        Handle = handle;
        PointSize = pointSize;
    }
}

/// <summary>
/// A handle to a sound file. These should only be created by calling LoadSound().
/// </summary>
class Sound
{
    public readonly IntPtr Handle;

    public Sound(IntPtr handle)
    {
        Handle = handle;
    }
}

/// <summary>
/// A handle to a music file. These should only be created by calling LoadMusic().
/// </summary>
class Music
{
    public readonly IntPtr Handle;

    public Music(IntPtr handle)
    {
        Handle = handle;
    }
}
