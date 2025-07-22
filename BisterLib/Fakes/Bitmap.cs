using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace System.Drawing
{
    internal class Bitmap
    {
        public Bitmap()
        { }

        public void Save(MemoryStream stream, ImageFormat format)
        {

        }
    }
}

namespace System.Drawing.Imaging
{ 

    internal sealed partial class ImageFormat
    {
        public ImageFormat(System.Guid guid) { }
        public static System.Drawing.Imaging.ImageFormat Bmp { get; }
        public static System.Drawing.Imaging.ImageFormat Emf { get; }
        public static System.Drawing.Imaging.ImageFormat Exif { get; }
        public static System.Drawing.Imaging.ImageFormat Gif { get; }
        public System.Guid Guid { get; }
        public static System.Drawing.Imaging.ImageFormat Icon { get; }
        public static System.Drawing.Imaging.ImageFormat Jpeg { get; }
        public static System.Drawing.Imaging.ImageFormat MemoryBmp { get; }
        public static System.Drawing.Imaging.ImageFormat Png { get; }
        public static System.Drawing.Imaging.ImageFormat Tiff { get; }
        public static System.Drawing.Imaging.ImageFormat Wmf { get; }
    }
}
