using System.Runtime.InteropServices;

namespace HCXT.App.StandardService
{
    /// <summary>
    /// 动态设置窗体图标
    /// WuLei 2011-10-12
    /// </summary>
    class ModifyWindowIcon
    {
// ReSharper disable InconsistentNaming
        private const int SM_CXICON = 11;
        private const int SM_CYICON = 12;
        private const int SM_CXSMICON = 49;
        private const int SM_CYSMICON = 50;
        //private const int LR_DEFAULTCOLOR = 0x0;
        //private const int LR_MONOCHROME = 0x1;
        //private const int LR_COLOR = 0x2;
        //private const int LR_COPYRETURNORG = 0x4;
        //private const int LR_COPYDELETEORG = 0x8;
        private const int LR_LOADFROMFILE = 0x10;
        //private const int LR_LOADTRANSPARENT = 0x20;
        //private const int LR_DEFAULTSIZE = 0x40;
        //private const int LR_VGACOLOR = 0x80;
        //private const int LR_LOADMAP3DCOLORS = 0x1000;
        //private const int LR_CREATEDIBSECTION = 0x2000;
        //private const int LR_COPYFROMRESOURCE = 0x4000;
        //private const int LR_SHARED = 0x8000;
        private const int IMAGE_ICON = 1;
        private const int WM_SETICON = 0x80;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;
        private const int GW_OWNER = 4;
// ReSharper restore InconsistentNaming
        [DllImport("user32.dll")]
        private static extern int LoadImageA(int hInst, string lpsz, int uType, int cxDesired, int cyDesired, int fuLoad);
        [DllImport("user32.dll")]
        private static extern int SendMessageA(int hwnd, int wMsg, int wParam, int lParam);
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
        [DllImport("user32.dll")]
        private static extern int GetWindow(int hwnd, int wCmd);


        /// <summary>
        /// 动态设置窗体图标
        /// </summary>
        /// <param name="hwnd">窗体句柄</param>
        /// <param name="sIconFileName">图标文件名</param>
        /// <param name="bSetAsAppIcon">默认为true</param>
        public static void SetIcon(int hwnd, string sIconFileName, bool bSetAsAppIcon)
        {
            int lhWndTop = 0;
            int cx;
            int cy;
            int hIconLarge;
            int hIconSmall;

            if (bSetAsAppIcon)
            {
                int lhWnd = hwnd;
                lhWndTop = lhWnd;
                while (lhWnd != 0)
                {
                    lhWnd = GetWindow(lhWnd, GW_OWNER);
                    if (lhWnd != 0)
                        lhWndTop = lhWnd;
                }
            }
            int hInstance = 0;//App.hInstance

            cx = GetSystemMetrics(SM_CXICON);
            cy = GetSystemMetrics(SM_CYICON);
            hIconLarge = LoadImageA(hInstance, sIconFileName, IMAGE_ICON, cx, cy, LR_LOADFROMFILE);
            if (bSetAsAppIcon)
            {
                SendMessageA(lhWndTop, WM_SETICON, ICON_BIG, hIconLarge);
            }
            SendMessageA(hwnd, WM_SETICON, ICON_BIG, hIconLarge);

            cx = GetSystemMetrics(SM_CXSMICON);
            cy = GetSystemMetrics(SM_CYSMICON);
            hIconSmall = LoadImageA(hInstance, sIconFileName, IMAGE_ICON, cx, cy, LR_LOADFROMFILE);

            if (bSetAsAppIcon)
            {
                SendMessageA(lhWndTop, WM_SETICON, ICON_SMALL, hIconSmall);
            }
            SendMessageA(hwnd, WM_SETICON, ICON_SMALL, hIconSmall);
        }
    }
}
