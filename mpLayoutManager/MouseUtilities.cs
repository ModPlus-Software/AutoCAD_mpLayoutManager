namespace mpLayoutManager
{
    using System;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Media;

    public class MouseUtilities
    {
        [DllImport("user32.dll", CharSet = CharSet.None, ExactSpelling = false)]
        private static extern bool GetCursorPos(ref Win32Point pt);

        public static Point GetMousePosition(Visual relativeTo)
        {
            var win32Point = new Win32Point();
            GetCursorPos(ref win32Point);
            var point = relativeTo.PointFromScreen(new Point(win32Point.X, win32Point.Y));
            return point;
        }

        [DllImport("user32.dll", CharSet = CharSet.None, ExactSpelling = false)]
        private static extern bool ScreenToClient(IntPtr hwnd, ref Win32Point pt);

        private struct Win32Point
        {
            public int X;

            public int Y;
        }
    }
}