using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace aframe.Views
{
    public interface IOverlay
    {
        bool OverlayVisible { get; set; }

        int ZOrder { get; }
    }

    public static class OverlayExtensions
    {
        public static Window ToWindow(
            this IOverlay overlay)
            => overlay as Window;

        public static bool SetOverlayVisible(
            this IOverlay overlay,
            ref bool overlayVisible,
            bool newValue,
            double opacity = 1.0d)
        {
            if (overlayVisible != newValue)
            {
                overlayVisible = newValue;
                if (overlayVisible)
                {
                    overlay.ShowOverlay(opacity);
                }
                else
                {
                    overlay.HideOverlay();
                }

                return true;
            }

            return false;
        }

        public static void InitializeOverlayVisible(
            this IOverlay overlay,
            ref bool overlayVisible,
            bool newValue,
            double opacity = 1.0d)
        {
            overlayVisible = newValue;

            if (overlayVisible)
            {
                overlay.ShowOverlay(opacity);
            }
            else
            {
                overlay.HideOverlay();
            }
        }

        public static bool ShowOverlay(
            this IOverlay overlay,
            double opacity = 1.0d)
        {
            var r = false;

            if (overlay is Window w)
            {
                if (w.Opacity <= 0)
                {
                    w.Opacity = opacity;
                    r = true;
                }
            }

            return r;
        }

        public static void HideOverlay(
            this IOverlay overlay)
        {
            if (overlay is Window w)
            {
                w.Opacity = 0;
            }
        }

        public static void ToNonActive(
            this Window window)
        {
            window.SourceInitialized += (s, e) =>
            {
                // Get this window's handle
                var hwnd = new WindowInteropHelper(window).Handle;

                // Change the extended window style to include WS_EX_TRANSPARENT
                var extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);

                NativeMethods.SetWindowLong(
                    hwnd,
                    NativeMethods.GWL_EXSTYLE,
                    extendedStyle | NativeMethods.WS_EX_NOACTIVATE);
            };
        }

        public static void ToNotTransparent(
            this Window window)
        {
            // Get this window's handle
            IntPtr hwnd = new WindowInteropHelper(window).Handle;

            // Change the extended window style to include WS_EX_TRANSPARENT
            int extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);

            NativeMethods.SetWindowLong(
                hwnd,
                NativeMethods.GWL_EXSTYLE,
                extendedStyle & ~NativeMethods.WS_EX_TRANSPARENT);
        }

        public static void ToTransparent(
            this Window window)
        {
            // Get this window's handle
            var hwnd = new WindowInteropHelper(window).Handle;

            // Change the extended window style to include WS_EX_TRANSPARENT
            var extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);

            NativeMethods.SetWindowLong(
                hwnd,
                NativeMethods.GWL_EXSTYLE,
                extendedStyle | NativeMethods.WS_EX_TRANSPARENT);
        }

        #region ZOrder Corrector

        private static readonly DispatcherTimer ZOrderCorrector = new DispatcherTimer(DispatcherPriority.ContextIdle)
        {
            Interval = TimeSpan.FromSeconds(1.5)
        };

        private static readonly List<IOverlay> ToCorrectOverlays = new List<IOverlay>(64);

        public static void SubscribeZOrderCorrector(
            this IOverlay overlay)
        {
            lock (ZOrderCorrector)
            {
                if (!ZOrderCorrector.IsEnabled)
                {
                    ZOrderCorrector.Tick -= ZOrderCorrectorOnTick;
                    ZOrderCorrector.Tick += ZOrderCorrectorOnTick;
                }

                if (overlay is Window window)
                {
                    window.Closing += (x, y) =>
                    {
                        if (x is IOverlay o)
                        {
                            o.UnsubscribeZOrderCorrector();
                        }
                    };
                }

                if (!ToCorrectOverlays.Contains(overlay))
                {
                    ToCorrectOverlays.Add(overlay);
                }

                if (ToCorrectOverlays.Any() &&
                    !ZOrderCorrector.IsEnabled)
                {
                    ZOrderCorrector.Start();
                }
            }
        }

        public static void UnsubscribeZOrderCorrector(
            this IOverlay overlay)
        {
            lock (ZOrderCorrector)
            {
                ToCorrectOverlays.Remove(overlay);

                if (!ToCorrectOverlays.Any())
                {
                    ZOrderCorrector.Stop();
                }
            }
        }

        private static void ZOrderCorrectorOnTick(
            object sender,
            EventArgs e)
        {
            lock (ZOrderCorrector)
            {
                if (!ToCorrectOverlays.Any())
                {
                    ZOrderCorrector.Stop();
                    return;
                }

                var targets = ToCorrectOverlays.OrderBy(x => x.ZOrder);
                foreach (var overlay in ToCorrectOverlays)
                {
                    Thread.Yield();

                    if (overlay == null)
                    {
                        continue;
                    }

                    if (overlay is Window window &&
                        window.IsLoaded &&
                        !window.Topmost)
                    {
                        if (!overlay.IsOverlaysGameWindow())
                        {
                            overlay.EnsureTopMost();
                        }
                    }
                }
            }
        }

        public static IntPtr GetHandle(
            this IOverlay overlay) =>
            new WindowInteropHelper(overlay as Window).Handle;

        /// <summary>
        /// FFXIVより前面にいるか？
        /// </summary>
        /// <param name="overlay"></param>
        /// <returns></returns>
        private static bool IsOverlaysGameWindow(
            this IOverlay overlay)
        {
            var gameHandle = GetGameWindowHandle();
            var handle = overlay.GetHandle();

            if (gameHandle == IntPtr.Zero)
            {
                return false;
            }

            while (handle != IntPtr.Zero)
            {
                // Overlayウィンドウよりも前面側にFF14のウィンドウがあった
                if (handle == gameHandle)
                {
                    return false;
                }

                handle = NativeMethods.GetWindow(handle, NativeMethods.GW_HWNDPREV);
            }

            // 前面側にOverlayが存在する、もしくはFF14が起動していない
            return true;
        }

        /// <summary>
        /// Windowを最前面に持ってくる
        /// </summary>
        /// <param name="overlay"></param>
        public static void EnsureTopMost(
            this IOverlay overlay)
        {
            NativeMethods.SetWindowPos(
                overlay.GetHandle(),
                NativeMethods.HWND_TOPMOST,
                0, 0, 0, 0,
                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOACTIVATE);
        }

        private static readonly object gameProcLocker = new object();
        private static Process gameProc;
        private static DateTime lastTry;
        private static TimeSpan tryInterval = new TimeSpan(0, 0, 15);

        private static IntPtr GetGameWindowHandle()
        {
            lock (gameProcLocker)
            {
                try
                {
                    // プロセスがすでに終了してるならプロセス情報をクリア
                    if (gameProc != null && gameProc.HasExited)
                    {
                        gameProc = null;
                    }

                    // プロセス情報がなく、tryIntervalよりも時間が経っているときは新たに取得を試みる
                    if (gameProc == null && DateTime.Now - lastTry > tryInterval)
                    {
                        gameProc = Process.GetProcessesByName("ffxiv").FirstOrDefault();
                        if (gameProc == null)
                        {
                            gameProc = Process.GetProcessesByName("ffxiv_dx11").FirstOrDefault();
                        }

                        lastTry = DateTime.Now;
                    }

                    if (gameProc != null)
                    {
                        return gameProc.MainWindowHandle;
                    }
                }
                catch (System.ComponentModel.Win32Exception) { }

                return IntPtr.Zero;
            }
        }

        #endregion ZOrder Corrector
    }
}
