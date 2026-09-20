using System;
using System.Collections.Generic;
using System.Text;
using OnTopReplica.Native;
using System.Drawing;
using System.Windows.Forms;

namespace OnTopReplica {
	public static class Win32Helper {

        #region Read-only build

        /// <summary>
        /// Click forwarding is intentionally unavailable in the read-only profile build.
        /// Kept as a compatibility stub for old call sites; it never sends input.
        /// </summary>
        public static void InjectFakeMouseClick(IntPtr window, CloneClickEventArgs clickArgs) {
            Log.Write("Blocked click-forwarding request for HWND {0}", window);
        }

        #endregion

        /// <summary>Returns the child control of a window corresponding to a screen location.</summary>
		/// <param name="parent">Parent window to explore.</param>
		/// <param name="scrClickLocation">Child control location in screen coordinates.</param>
		private static IntPtr GetRealChildControlFromPoint(IntPtr parent, NPoint scrClickLocation) {
			IntPtr curr = parent, child = IntPtr.Zero;
			do {
                child = WindowManagerMethods.RealChildWindowFromPoint(curr,
                    WindowManagerMethods.ScreenToClient(curr, scrClickLocation));

				if (child == IntPtr.Zero || child == curr)
					break;

				//Update for next loop
				curr = child;
			}
			while (true);

			//Safety check, shouldn't happen
			if (curr == IntPtr.Zero)
				curr = parent;

			return curr;
		}

        /// <summary>
        /// Gets a handle to the window that currently is in the foreground.
        /// </summary>
        /// <returns>May return null if call fails or no valid window selected.</returns>
        public static WindowHandle GetCurrentForegroundWindow() {
            IntPtr handle = WindowManagerMethods.GetForegroundWindow();
            if (handle == IntPtr.Zero)
                return null;

            return new WindowHandle(handle);
        }

	}
}
