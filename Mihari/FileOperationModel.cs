using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mihari
{
    using System.ComponentModel;
    using System.Drawing;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Interop;

    public class FileOperationModel
    {
        public WatcherChangeTypes ChangeType { get; set; }
        public string FilePath { get; set; }
        public string DateTime { get; set; }

        public bool FileExists
        {
            get { return File.Exists(FilePath); }
        }

        public string Dirctory
        {
            get { return Path.GetDirectoryName(FilePath); }
        }

        public System.Windows.Media.ImageSource FileIcon
        {
            get
            {
                if (!File.Exists(FilePath)) return null;

                return Icon.ExtractAssociatedIcon(FilePath).ToImageSource();
            }
        }

        public bool IsMalicious
        {
            get
            {
                var keywords = Mihari.Properties.Settings.Default.MaliciousExes.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                return FilePath.Contains(true, keywords);
            }
        }
    }

    public static class IconExtension
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        public static System.Windows.Media.ImageSource ToImageSource(this Icon icon)
        {
            Bitmap bitmap = icon.ToBitmap();
            IntPtr hBitmap = bitmap.GetHbitmap();

            var wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());

            if (!DeleteObject(hBitmap))
            {
                throw new Win32Exception();
            }

            return wpfBitmap;
        }
    }
}
