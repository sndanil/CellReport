using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace CellReport.WinForms
{
    internal static class ToolbarIcons
    {
        private const string ResourcePrefix = "CellReport.WinForms.Assets.Icons.";

        private static readonly Assembly Assembly = typeof(ToolbarIcons).Assembly;
        private static readonly Dictionary<string, Bitmap> BitmapCache =
            new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);

        private static Icon _appIcon;

        public static Bitmap ArrowLeft => GetBitmap("arrow_left.png");
        public static Bitmap ArrowRight => GetBitmap("arrow_right.png");
        public static Bitmap Bookmark => GetBitmap("bookmark.png");
        public static Bitmap DocumentArrowLeftright => GetBitmap("document_arrow_leftright.png");
        public static Bitmap Excel2013_16 => GetBitmap("excel2013-16.png");
        public static Bitmap Find => GetBitmap("find.png");
        public static Bitmap NavBlueLeft => GetBitmap("nav_blue_left.png");
        public static Bitmap NavBlueRight => GetBitmap("nav_blue_right.png");
        public static Bitmap Pdf2616_32 => GetBitmap("pdf-2616_32.png");
        public static Bitmap Print => GetBitmap("print.png");
        public static Bitmap PrintPreview => GetBitmap("print_preview.png");
        public static Bitmap PrintSetup => GetBitmap("print_setup.png");
        public static Bitmap Save => GetBitmap("save.png");
        public static Bitmap ShowColumns16x16 => GetBitmap("show_columns_16x16.png");
        public static Bitmap ShowGrid16x16 => GetBitmap("show_grid_16x16.png");

        public static Icon App => _appIcon ?? (_appIcon = LoadIcon("app.ico"));

        private static Bitmap GetBitmap(string fileName)
        {
            if (!BitmapCache.TryGetValue(fileName, out var bitmap))
            {
                using (var stream = OpenStream(fileName))
                    bitmap = new Bitmap(stream);
                BitmapCache[fileName] = bitmap;
            }

            return bitmap;
        }

        private static Icon LoadIcon(string fileName)
        {
            using (var stream = OpenStream(fileName))
                return new Icon(stream);
        }

        private static Stream OpenStream(string fileName)
        {
            var resourceName = ResourcePrefix + fileName;
            var stream = Assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                throw new InvalidOperationException($"Embedded resource not found: {resourceName}");
            return stream;
        }
    }
}
