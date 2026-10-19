using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

namespace AlibreCatalogManager.Generation
{
    /// <summary>
    /// Draws a placeholder add-on icon — a rounded colour tile with initials — and
    /// writes it as a classic ICO with 32-bit BMP (DIB) entries at 16/24/32/48 px.
    /// No PNG-compressed entries, so any icon loader (LoadImage, System.Drawing.Icon)
    /// can read every size.
    /// </summary>
    public static class IconWriter
    {
        private static readonly int[] Sizes = { 16, 24, 32, 48 };

        public static void Write(string path, string initials, Color color)
        {
            var images = new List<byte[]>();
            foreach (var size in Sizes)
                using (var bitmap = Draw(size, initials, color))
                    images.Add(ToDib(bitmap));

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                // ICONDIR
                writer.Write((short) 0);
                writer.Write((short) 1);
                writer.Write((short) Sizes.Length);

                // ICONDIRENTRY per image
                var offset = 6 + 16 * Sizes.Length;
                for (var i = 0; i < Sizes.Length; i++)
                {
                    writer.Write((byte) Sizes[i]);
                    writer.Write((byte) Sizes[i]);
                    writer.Write((byte) 0);
                    writer.Write((byte) 0);
                    writer.Write((short) 1);
                    writer.Write((short) 32);
                    writer.Write(images[i].Length);
                    writer.Write(offset);
                    offset += images[i].Length;
                }

                foreach (var image in images) writer.Write(image);
                writer.Flush();
                File.WriteAllBytes(path, stream.ToArray());
            }

            // Self-check: System.Drawing must be able to read it back.
            using (new Icon(path)) { }
        }

        private static Bitmap Draw(int size, string initials, Color color)
        {
            var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);

                var s = size / 32f;
                var rect = new RectangleF(0.5f * s, 0.5f * s, size - s, size - s);
                var d = 2 * Math.Max(2f, 6 * s);
                using (var tile = new GraphicsPath())
                using (var brush = new LinearGradientBrush(rect, Lighten(color, 0.25f), Darken(color, 0.25f), 90f))
                {
                    tile.AddArc(rect.X, rect.Y, d, d, 180, 90);
                    tile.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
                    tile.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
                    tile.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
                    tile.CloseFigure();
                    g.FillPath(brush, tile);
                }

                // One letter at 16 px: two are unreadable at that size.
                var text = size <= 16 && initials.Length > 1 ? initials.Substring(0, 1) : initials;
                var emSize = size * (text.Length > 1 ? 0.42f : 0.6f);
                using (var font = new Font("Segoe UI", emSize, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                    g.DrawString(text, font, Brushes.White, new RectangleF(0, 0, size, size + s), format);
            }

            return bitmap;
        }

        private static Color Lighten(Color c, float amount) =>
            Color.FromArgb(255, Mix(c.R, 255, amount), Mix(c.G, 255, amount), Mix(c.B, 255, amount));

        private static Color Darken(Color c, float amount) =>
            Color.FromArgb(255, Mix(c.R, 0, amount), Mix(c.G, 0, amount), Mix(c.B, 0, amount));

        private static int Mix(int from, int to, float amount) => (int) Math.Round(from + (to - from) * amount);

        /// <summary>BITMAPINFOHEADER + bottom-up BGRA pixels + 1 bpp AND mask.</summary>
        private static byte[] ToDib(Bitmap bitmap)
        {
            int w = bitmap.Width, h = bitmap.Height;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(40);
                writer.Write(w);
                writer.Write(h * 2); // height covers the XOR image plus the AND mask
                writer.Write((short) 1);
                writer.Write((short) 32);
                for (var i = 0; i < 6; i++) writer.Write(0);

                for (var y = h - 1; y >= 0; y--)
                for (var x = 0; x < w; x++)
                {
                    var c = bitmap.GetPixel(x, y);
                    writer.Write(c.B);
                    writer.Write(c.G);
                    writer.Write(c.R);
                    writer.Write(c.A);
                }

                // The alpha channel carries transparency; the mask marks fully clear pixels.
                var rowBytes = (w + 31) / 32 * 4;
                for (var y = h - 1; y >= 0; y--)
                {
                    var row = new byte[rowBytes];
                    for (var x = 0; x < w; x++)
                        if (bitmap.GetPixel(x, y).A == 0)
                            row[x / 8] |= (byte) (0x80 >> (x % 8));
                    writer.Write(row);
                }

                writer.Flush();
                return stream.ToArray();
            }
        }
    }
}
