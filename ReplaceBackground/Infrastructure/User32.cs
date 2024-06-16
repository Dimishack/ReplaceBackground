using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ReplaceBackground.Infrastructure
{
    internal class User32
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo
            (int uAction, int uParam, string lpvParam, int fuWinIni);

        public static void SetWallpaper(string path, int style, int title)
        {
            using (var key = Registry.CurrentUser.OpenSubKey("Control Panel\\Desktop", true))
            {
                key.SetValue("WallpaperStyle", style.ToString());
                key.SetValue("TileWallpaper", title.ToString());
                SystemParametersInfo(20, 0, path, 0x01 | 0x02);
            }
        }
    }
}
