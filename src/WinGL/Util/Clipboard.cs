﻿using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using WinGL.Win32;
using WinGL.Win32.User32;

namespace WinGL.Util
{
    public static class Clipboard
    {
        // https://stackoverflow.com/questions/44205260/net-core-copy-to-clipboard
        // https://github.com/CopyText/TextCopy/blob/main/src/TextCopy/WindowsClipboard.cs

        const uint cfUnicodeText = 13;

        public static void SetText(string text)
        {
            OpenClipboard();

            WinUser.EmptyClipboard();
            IntPtr hGlobal = IntPtr.Zero;
            try
            {
                var bytes = (text.Length + 1) * 2;
                hGlobal = Marshal.AllocHGlobal(bytes);

                if (hGlobal == IntPtr.Zero)
                {
                    ThrowWin32();
                }

                IntPtr target = WinApi.GlobalLock(hGlobal);

                if (target == IntPtr.Zero)
                {
                    ThrowWin32();
                }

                try
                {
                    Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                }
                finally
                {
                    WinApi.GlobalUnlock(target);
                }

                if (WinUser.SetClipboardData(cfUnicodeText, hGlobal) == IntPtr.Zero)
                {
                    ThrowWin32();
                }

                hGlobal = IntPtr.Zero;
            }
            finally
            {
                if (hGlobal != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(hGlobal);
                }

                WinUser.CloseClipboard();
            }
        }

        public static void OpenClipboard()
        {
            var num = 10;
            while (true)
            {
                if (WinUser.OpenClipboard(IntPtr.Zero))
                {
                    break;
                }

                if (--num == 0)
                {
                    ThrowWin32();
                }

                Thread.Sleep(100);
            }
        }

        static void ThrowWin32()
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
}