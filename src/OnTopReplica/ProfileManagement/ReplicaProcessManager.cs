using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace OnTopReplica.ProfileManagement {
    internal sealed class ReplicaProcessManager : IDisposable {
        sealed class ManagedReplica {
            public Process Process;
            public string Signature;
            public IntPtr SourceHandle;
        }

        readonly Dictionary<string, ManagedReplica> _running = new Dictionary<string, ManagedReplica>(StringComparer.OrdinalIgnoreCase);
        readonly WindowDiscovery _discovery = new WindowDiscovery();

        public IList<BindingRuntimeStatus> Reconcile(ProfileDefinition profile, IntPtr ownerHandle) {
            CleanupExited();
            var statuses = new List<BindingRuntimeStatus>();
            var desired = new Dictionary<string, DesiredReplica>(StringComparer.OrdinalIgnoreCase);
            IList<WindowHandle> candidates = _discovery.GetCandidateWindows(profile, ownerHandle);

            foreach (var binding in profile.Bindings.Where(b => b != null && b.Enabled)) {
                var status = new BindingRuntimeStatus { Character=binding.Character, Role=binding.Role, Slot=binding.Slot };
                statuses.Add(status);

                RoleDefinition role = profile.Roles.FirstOrDefault(r => string.Equals(r.Name, binding.Role, StringComparison.OrdinalIgnoreCase));
                if (role == null) { status.Status="ERROR: role not found"; continue; }

                string needle = string.IsNullOrWhiteSpace(binding.WindowTitleContains) ? binding.Character : binding.WindowTitleContains;
                List<WindowHandle> matches = candidates.Where(w => w.Title != null && w.Title.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                if (matches.Count != 1) {
                    status.Status = matches.Count == 0 ? "Window not found" : "AMBIGUOUS ("+matches.Count+")";
                    continue;
                }

                WindowHandle source=matches[0];
                status.WindowTitle=source.Title;
                if (role.Replicas.Count == 0) { status.Status="Ignored by role"; continue; }

                foreach (var replica in role.Replicas) {
                    DesiredReplica d=BuildDesiredReplica(binding, replica, source);
                    desired[d.Key]=d;
                }
                status.Status="Ready ("+role.Replicas.Count+" replica"+(role.Replicas.Count==1?"":"s")+")";
            }

            foreach (string key in _running.Keys.ToList()) {
                DesiredReplica wanted;
                ManagedReplica existing=_running[key];
                if (!desired.TryGetValue(key,out wanted) || existing.SourceHandle!=wanted.SourceHandle || existing.Signature!=wanted.Signature)
                    StopOne(key);
            }

            foreach (var pair in desired) {
                if (_running.ContainsKey(pair.Key)) continue;
                try {
                    Process p=StartReplica(pair.Value);
                    _running[pair.Key]=new ManagedReplica { Process=p, Signature=pair.Value.Signature, SourceHandle=pair.Value.SourceHandle };
                }
                catch(Exception ex) {
                    Log.WriteException("Unable to launch managed replica "+pair.Key,ex);
                    BindingRuntimeStatus s=statuses.FirstOrDefault(x=>string.Equals(x.Character,pair.Value.Character,StringComparison.OrdinalIgnoreCase));
                    if (s!=null) s.Status="ERROR launching replica: "+ex.Message;
                }
            }

            return statuses;
        }

        DesiredReplica BuildDesiredReplica(CharacterBinding binding, ReplicaDefinition replica, WindowHandle source) {
            Point pos=LayoutEngine.ComputePosition(replica,binding.Slot);
            int opacity=Math.Max(1,Math.Min(255,replica.Opacity));
            var a=new StringBuilder();
            a.Append("--managedReplica ");
            a.Append("--windowId=").Append(source.Handle.ToInt64()).Append(' ');
            a.Append("--region=").Append(replica.Source.X).Append(',').Append(replica.Source.Y).Append(',').Append(replica.Source.Width).Append(',').Append(replica.Source.Height).Append(' ');
            a.Append("--size=").Append(replica.Output.Width).Append(',').Append(replica.Output.Height).Append(' ');
            a.Append("--position=").Append(pos.X).Append(',').Append(pos.Y).Append(' ');
            a.Append("--opacity=").Append(opacity).Append(' ');
            if (replica.ClickThrough) a.Append("--clickThrough ");
            if (replica.Borderless) a.Append("--chromeOff ");

            string args=a.ToString().Trim();
            return new DesiredReplica {
                Key=(binding.Character??"")+"|"+(replica.Name??"Replica"),
                Character=binding.Character,
                SourceHandle=source.Handle,
                Arguments=args,
                Signature=source.Handle.ToInt64()+"|"+args
            };
        }

        static Process StartReplica(DesiredReplica r) {
            var psi=new ProcessStartInfo {
                FileName=Application.ExecutablePath,
                Arguments=r.Arguments,
                WorkingDirectory=Application.StartupPath,
                UseShellExecute=false,
                CreateNoWindow=true
            };
            Process p=Process.Start(psi);
            if (p==null) throw new InvalidOperationException("Process.Start returned null.");
            return p;
        }

        void CleanupExited() {
            foreach(string key in _running.Keys.ToList()) {
                bool exited;
                try { exited=_running[key].Process==null || _running[key].Process.HasExited; } catch { exited=true; }
                if (exited) StopOne(key);
            }
        }

        void StopOne(string key) {
            ManagedReplica r;
            if (!_running.TryGetValue(key,out r)) return;
            _running.Remove(key);
            try {
                if (r.Process!=null && !r.Process.HasExited) {
                    r.Process.CloseMainWindow();
                    if (!r.Process.WaitForExit(800)) r.Process.Kill();
                }
            } catch(Exception ex) { Log.WriteException("Unable to stop managed replica "+key,ex); }
            finally { if (r.Process!=null) r.Process.Dispose(); }
        }

        public void StopAll() { foreach(string key in _running.Keys.ToList()) StopOne(key); }
        public void Dispose() { StopAll(); }
    }
}
