using System;
using System.IO;
using System.IO.Compression;

namespace Edi.Captcha;

internal sealed class CaptchaImage : IDisposable
{
    public int Width { get; }
    public int Height { get; }

    // RGBA pixel data: [y * Width * 4 + x * 4 + channel]
    private readonly byte[] _pixels;

    public CaptchaImage(int width, int height)
    {
        Width = width;
        Height = height;
        _pixels = new byte[width * height * 4];
    }

    public void Fill(byte r, byte g, byte b)
    {
        for (var i = 0; i < _pixels.Length; i += 4)
        {
            _pixels[i] = r;
            _pixels[i + 1] = g;
            _pixels[i + 2] = b;
            _pixels[i + 3] = 255;
        }
    }

    public void SetPixel(int x, int y, byte r, byte g, byte b, byte a = 255)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;
        var offset = (y * Width + x) * 4;
        if (a == 255)
        {
            _pixels[offset] = r;
            _pixels[offset + 1] = g;
            _pixels[offset + 2] = b;
            _pixels[offset + 3] = 255;
        }
        else if (a > 0)
        {
            // Alpha blending
            float sa = a / 255f;
            float da = 1f - sa;
            _pixels[offset] = (byte)(_pixels[offset] * da + r * sa);
            _pixels[offset + 1] = (byte)(_pixels[offset + 1] * da + g * sa);
            _pixels[offset + 2] = (byte)(_pixels[offset + 2] * da + b * sa);
            _pixels[offset + 3] = 255;
        }
    }

    public void DrawLine(float x0, float y0, float x1, float y1, byte r, byte g, byte b, float thickness = 1f)
    {
        // Bresenham's line with thickness approximation
        var dx = Math.Abs(x1 - x0);
        var dy = Math.Abs(y1 - y0);
        var steps = (int)Math.Max(dx, dy) + 1;

        for (var i = 0; i <= steps; i++)
        {
            var t = steps == 0 ? 0f : (float)i / steps;
            var px = x0 + (x1 - x0) * t;
            var py = y0 + (y1 - y0) * t;

            if (thickness <= 1.5f)
            {
                SetPixel((int)px, (int)py, r, g, b);
            }
            else
            {
                var half = (int)(thickness / 2);
                for (var ox = -half; ox <= half; ox++)
                {
                    for (var oy = -half; oy <= half; oy++)
                    {
                        SetPixel((int)px + ox, (int)py + oy, r, g, b);
                    }
                }
            }
        }
    }

    public void DrawCharacter(char c, int destX, int destY, int scale, byte r, byte g, byte b,
        CaptchaFontStyle fontStyle, float rotationDegrees)
    {
        var glyph = CaptchaFont.GetGlyph(c);
        var scaledW = CaptchaFont.GlyphWidth * scale;
        var scaledH = CaptchaFont.GlyphHeight * scale;
        var cx = scaledW / 2f;
        var cy = scaledH / 2f;

        var rad = rotationDegrees * MathF.PI / 180f;
        var cos = MathF.Cos(rad);
        var sin = MathF.Sin(rad);

        // Italic shear factor
        float shear = fontStyle == CaptchaFontStyle.Italic ? 0.2f : 0f;

        // Bold: draw each pixel slightly wider
        var boldExtra = fontStyle == CaptchaFontStyle.Bold ? 1 : 0;

        // Determine bounding box of rotated character to iterate over
        var boundPadding = (int)(Math.Max(scaledW, scaledH) * 0.5f) + 2;
        var minX = -boundPadding;
        var minY = -boundPadding;
        var maxX = scaledW + boundPadding;
        var maxY = scaledH + boundPadding;

        for (var dy = minY; dy < maxY; dy++)
        {
            for (var dx = minX; dx < maxX; dx++)
            {
                // Inverse rotation to find source pixel
                var rx = dx - cx;
                var ry = dy - cy;
                var srcX = rx * cos + ry * sin + cx;
                var srcY = -rx * sin + ry * cos + cy;

                // Apply inverse italic shear
                srcX -= srcY * shear;

                // Map to glyph coordinates
                var glyphX = (int)(srcX / scale);
                var glyphY = (int)(srcY / scale);

                var isSet = CaptchaFont.IsPixelSet(glyph, glyphX, glyphY);

                // Bold: also check adjacent pixel
                if (!isSet && boldExtra > 0 && glyphX > 0)
                {
                    isSet = CaptchaFont.IsPixelSet(glyph, glyphX - 1, glyphY);
                }

                if (isSet)
                {
                    var finalX = destX + dx;
                    var finalY = destY + dy;
                    SetPixel(finalX, finalY, r, g, b);
                }
            }
        }
    }

    public int MeasureCharWidth(char c, int scale, CaptchaFontStyle fontStyle)
    {
        var width = CaptchaFont.GlyphWidth * scale;
        if (fontStyle == CaptchaFontStyle.Bold)
        {
            width += scale;
        }

        // Account for italic shear: horizontal offset proportional to glyph height.
        // This mirrors the shear used in DrawCharacter (srcX -= srcY * shear).
        float shear = 0f;
        if (fontStyle == CaptchaFontStyle.Italic)
        {
            // Use the same shear factor as in DrawCharacter for italic rendering.
            shear = 0.3f;
        }

        if (shear != 0f)
        {
            // Maximum additional width is shear * glyph height * scale.
            var italicExtra = (int)Math.Ceiling(Math.Abs(shear) * CaptchaFont.GlyphHeight * scale);
            width += italicExtra;
        }

        return width;
    }

    public int MeasureCharHeight(int scale)
    {
        return CaptchaFont.GlyphHeight * scale;
    }

    #region PNG Encoder

    public byte[] EncodePng()
    {
        using var ms = new MemoryStream();

        // PNG signature
        ms.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        // IHDR chunk
        WriteChunk(ms, "IHDR"u8, writer =>
        {
            WriteBigEndianInt32(writer, Width);
            WriteBigEndianInt32(writer, Height);
            writer.WriteByte(8);  // bit depth
            writer.WriteByte(6);  // color type: RGBA
            writer.WriteByte(0);  // compression method
            writer.WriteByte(0);  // filter method
            writer.WriteByte(0);  // interlace method
        });

        // IDAT chunk (compressed image data)
        var rawData = BuildRawImageData();
        var compressedData = DeflateCompress(rawData);
        WriteChunk(ms, "IDAT"u8, writer =>
        {
            writer.Write(compressedData);
        });

        // IEND chunk
        WriteChunk(ms, "IEND"u8, _ => { });

        return ms.ToArray();
    }

    private byte[] BuildRawImageData()
    {
        // Each row: 1 filter byte (0 = None) + Width * 4 RGBA bytes
        var rowSize = 1 + Width * 4;
        var data = new byte[rowSize * Height];

        for (var y = 0; y < Height; y++)
        {
            var rowOffset = y * rowSize;
            data[rowOffset] = 0; // filter: None

            var srcOffset = y * Width * 4;
            Buffer.BlockCopy(_pixels, srcOffset, data, rowOffset + 1, Width * 4);
        }

        return data;
    }

    private static byte[] DeflateCompress(byte[] data)
    {
        using var output = new MemoryStream();
        // zlib header (RFC 1950): CMF=0x78 (deflate, window=32768), FLG=0x01
        output.WriteByte(0x78);
        output.WriteByte(0x01);

        using (var deflate = new DeflateStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            deflate.Write(data, 0, data.Length);
        }

        // Adler-32 checksum (required by zlib)
        var adler = ComputeAdler32(data);
        output.WriteByte((byte)(adler >> 24));
        output.WriteByte((byte)(adler >> 16));
        output.WriteByte((byte)(adler >> 8));
        output.WriteByte((byte)adler);

        return output.ToArray();
    }

    private static uint ComputeAdler32(byte[] data)
    {
        uint a = 1, b = 0;
        const uint mod = 65521;
        foreach (var t in data)
        {
            a = (a + t) % mod;
            b = (b + a) % mod;
        }
        return (b << 16) | a;
    }

    private delegate void ChunkWriter(MemoryStream stream);

    private static void WriteChunk(MemoryStream output, ReadOnlySpan<byte> chunkType, ChunkWriter writeData)
    {
        using var dataStream = new MemoryStream();
        writeData(dataStream);
        var data = dataStream.ToArray();

        // Length (4 bytes, big-endian)
        WriteBigEndianInt32(output, data.Length);

        // Type (4 bytes)
        output.Write(chunkType);

        // Data
        if (data.Length > 0)
        {
            output.Write(data, 0, data.Length);
        }

        // CRC32 over type + data
        var crc = ComputeCrc32(chunkType, data);
        WriteBigEndianUInt32(output, crc);
    }

    private static void WriteBigEndianInt32(MemoryStream stream, int value)
    {
        stream.WriteByte((byte)(value >> 24));
        stream.WriteByte((byte)(value >> 16));
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
    }

    private static void WriteBigEndianUInt32(MemoryStream stream, uint value)
    {
        stream.WriteByte((byte)(value >> 24));
        stream.WriteByte((byte)(value >> 16));
        stream.WriteByte((byte)(value >> 8));
        stream.WriteByte((byte)value);
    }

    #region CRC32

    private static readonly uint[] Crc32Table = GenerateCrc32Table();

    private static uint[] GenerateCrc32Table()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var crc = i;
            for (var j = 0; j < 8; j++)
            {
                crc = (crc & 1) != 0 ? 0xEDB88320 ^ (crc >> 1) : crc >> 1;
            }
            table[i] = crc;
        }
        return table;
    }

    private static uint ComputeCrc32(ReadOnlySpan<byte> type, byte[] data)
    {
        var crc = 0xFFFFFFFF;
        foreach (var b in type)
        {
            crc = Crc32Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }
        foreach (var b in data)
        {
            crc = Crc32Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }
        return crc ^ 0xFFFFFFFF;
    }

    #endregion

    #endregion

    public void Dispose()
    {
        // No unmanaged resources, but implements IDisposable for using pattern compatibility
    }
}
