using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace HCXT.App.StandardService
{
    /// <summary>
    /// ���崦��API������װ��
    /// </summary>
    public class ConsoleWin32Helper
    {
        #region ���ùرհ�ť
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, IntPtr bRevert);

        [DllImport("user32.dll", EntryPoint = "RemoveMenu")]
        static extern IntPtr RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);

        /// <summary>
        /// ���ùرհ�ť
        /// </summary>
        /// <param name="title">����̨����</param>
        public static int DisableCloseButton(string title)
        {
            //�߳�˯�ߣ�ȷ��closebtn���ܹ�����FindWindow��������ʱ��Findʧ�ܡ���
            Thread.Sleep(100);

            IntPtr windowHandle = FindWindow(null, title);
            if (windowHandle != IntPtr.Zero)
            {
                IntPtr closeMenu = GetSystemMenu(windowHandle, IntPtr.Zero);
                if(closeMenu != IntPtr.Zero)
                {
// ReSharper disable InconsistentNaming
                    const uint SC_CLOSE = 0xF060;
                    RemoveMenu(closeMenu, SC_CLOSE, 0x0);
// ReSharper restore InconsistentNaming
                }
            }
            return windowHandle.ToInt32();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        public static bool IsExistsConsole(string title)
        {
            IntPtr windowHandle = FindWindow(null, title);
            if (windowHandle.Equals(IntPtr.Zero)) return false;

            return true;
        }
        #endregion
    }
}
