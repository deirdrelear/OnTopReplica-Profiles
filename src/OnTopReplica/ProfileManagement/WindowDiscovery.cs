using System;
using System.Collections.Generic;
using System.Linq;
using OnTopReplica.WindowSeekers;

namespace OnTopReplica.ProfileManagement {
    internal sealed class WindowDiscovery {
        public IList<WindowHandle> GetCandidateWindows(ProfileDefinition profile, IntPtr ownerHandle) {
            var seeker = new TaskWindowSeeker { OwnerHandle = ownerHandle, SkipNotVisibleWindows = true };
            seeker.Refresh();
            IEnumerable<WindowHandle> windows = seeker.Windows;
            if (profile != null && !string.IsNullOrWhiteSpace(profile.WindowTitleFilter)) {
                string filter = profile.WindowTitleFilter.Trim();
                windows = windows.Where(w => w.Title != null && w.Title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            return windows.OrderBy(w => w.Title, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
