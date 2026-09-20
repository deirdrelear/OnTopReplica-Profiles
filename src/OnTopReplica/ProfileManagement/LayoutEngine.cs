using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OnTopReplica.ProfileManagement {
    internal static class LayoutEngine {
        public static Point ComputePosition(ReplicaDefinition replica, int slot) {
            if (replica == null || replica.Layout == null) throw new ArgumentException("Replica layout is missing.");
            if (replica.Output == null || replica.Output.Width <= 0 || replica.Output.Height <= 0) throw new ArgumentException("Replica output size is invalid.");
            if (slot < 1) throw new ArgumentOutOfRangeException("slot");

            Screen screen = ResolveScreen(replica.Layout);
            Rectangle bounds = replica.Layout.UseWorkingArea ? screen.WorkingArea : screen.Bounds;
            int columns = Math.Max(1, replica.Layout.Columns);
            int index = slot - 1;
            int column = index % columns;
            int row = index / columns;

            return new Point(
                bounds.Left + replica.Layout.OriginX + column * (replica.Output.Width + replica.Layout.GapX),
                bounds.Top + replica.Layout.OriginY + row * (replica.Output.Height + replica.Layout.GapY)
            );
        }

        static Screen ResolveScreen(GridLayoutDefinition layout) {
            Screen[] screens = Screen.AllScreens;
            if (!string.IsNullOrWhiteSpace(layout.MonitorDeviceName)) {
                Screen named = screens.FirstOrDefault(s => string.Equals(s.DeviceName, layout.MonitorDeviceName, StringComparison.OrdinalIgnoreCase));
                if (named != null) return named;
            }
            if (layout.MonitorIndex >= 0 && layout.MonitorIndex < screens.Length) return screens[layout.MonitorIndex];
            return Screen.PrimaryScreen ?? screens[0];
        }
    }
}
