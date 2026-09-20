using System;

namespace OnTopReplica.ProfileManagement {
    internal static class ProfileManagerMode {
        public static bool IsRequested(string[] args) {
            if (args == null) return false;
            foreach (string arg in args) {
                if (string.Equals(arg, "--profiles", StringComparison.OrdinalIgnoreCase) ||
                    arg.StartsWith("--applyProfile=", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public static string GetRequestedProfile(string[] args) {
            if (args == null) return null;
            foreach (string arg in args) {
                const string prefix="--applyProfile=";
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return arg.Substring(prefix.Length).Trim().Trim('"');
            }
            return null;
        }

        public static bool ShouldAutoApply(string[] args) {
            return !string.IsNullOrWhiteSpace(GetRequestedProfile(args));
        }
    }
}
