using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OnTopReplica.ProfileManagement {
    public sealed class ProfileManagerForm : Form {
        readonly ProfileRepository _repository=new ProfileRepository();
        readonly ReplicaProcessManager _processManager=new ReplicaProcessManager();
        readonly Timer _timer=new Timer();
        readonly string _requestedProfile;
        readonly bool _autoApplyOnShown;

        ComboBox _profiles;
        DataGridView _grid;
        Label _summary;
        CheckBox _autoRecover;
        ProfileDefinition _activeProfile;
        IList<ProfileDefinition> _loaded=new List<ProfileDefinition>();

        public ProfileManagerForm(string requestedProfile,bool autoApply) {
            _requestedProfile=requestedProfile;
            _autoApplyOnShown=autoApply;
            Text="OnTopReplica Profiles (Read-Only)";
            StartPosition=FormStartPosition.CenterScreen;
            Size=new Size(1050,620);
            MinimumSize=new Size(760,420);
            BuildUi();
            _timer.Interval=2000;
            _timer.Tick+=delegate { Reconcile(); };
            Load+=delegate { _repository.EnsureExamples(); ReloadProfiles(_requestedProfile); };
            Shown+=delegate { if (_autoApplyOnShown) ApplySelected(); };
            FormClosed+=delegate { _timer.Stop(); _processManager.Dispose(); };
        }

        void BuildUi() {
            var top=new FlowLayoutPanel { Dock=DockStyle.Top, Height=42, Padding=new Padding(6), WrapContents=false };
            top.Controls.Add(new Label { Text="Profile:", AutoSize=true, Margin=new Padding(0,7,4,0) });
            _profiles=new ComboBox { Width=220, DropDownStyle=ComboBoxStyle.DropDownList, DisplayMember="Name" };
            top.Controls.Add(_profiles);

            var reload=new Button { Text="Reload", AutoSize=true };
            reload.Click+=delegate { ReloadProfiles(GetSelectedName()); };
            top.Controls.Add(reload);

            var open=new Button { Text="Profiles folder", AutoSize=true };
            open.Click+=delegate { Process.Start("explorer.exe",_repository.FolderPath); };
            top.Controls.Add(open);

            var apply=new Button { Text="Apply", AutoSize=true };
            apply.Click+=delegate { ApplySelected(); };
            top.Controls.Add(apply);

            var stop=new Button { Text="Stop replicas", AutoSize=true };
            stop.Click+=delegate { _timer.Stop(); _activeProfile=null; _processManager.StopAll(); _grid.Rows.Clear(); _summary.Text="Managed replicas stopped."; };
            top.Controls.Add(stop);

            _autoRecover=new CheckBox { Text="Auto-recover restarted clients", AutoSize=true, Checked=true, Margin=new Padding(12,7,0,0) };
            _autoRecover.CheckedChanged+=delegate { _timer.Enabled=_autoRecover.Checked && _activeProfile!=null; };
            top.Controls.Add(_autoRecover);

            _grid=new DataGridView {
                Dock=DockStyle.Fill, ReadOnly=true, AllowUserToAddRows=false, AllowUserToDeleteRows=false,
                RowHeadersVisible=false, AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode=DataGridViewSelectionMode.FullRowSelect
            };
            _grid.Columns.Add("Character","Character");
            _grid.Columns.Add("Role","Role");
            _grid.Columns.Add("Slot","Slot");
            _grid.Columns.Add("Window","Matched window");
            _grid.Columns.Add("Status","Status");

            var bottom=new Panel { Dock=DockStyle.Bottom, Height=34 };
            _summary=new Label { Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleLeft, Padding=new Padding(8,0,0,0) };
            bottom.Controls.Add(_summary);

            Controls.Add(_grid);
            Controls.Add(bottom);
            Controls.Add(top);
        }

        void ReloadProfiles(string preserve) {
            _timer.Stop();
            _activeProfile=null;
            _processManager.StopAll();
            _loaded=_repository.LoadAll();
            _profiles.DataSource=null;
            _profiles.DataSource=_loaded.ToList();
            _profiles.DisplayMember="Name";
            string target=!string.IsNullOrWhiteSpace(preserve)?preserve:_requestedProfile;
            if (!string.IsNullOrWhiteSpace(target)) {
                for(int i=0;i<_profiles.Items.Count;i++) {
                    var p=_profiles.Items[i] as ProfileDefinition;
                    if (p!=null && string.Equals(p.Name,target,StringComparison.OrdinalIgnoreCase)) { _profiles.SelectedIndex=i; break; }
                }
            }
            _grid.Rows.Clear();
            _summary.Text=_loaded.Count+" profile(s) loaded from "+_repository.FolderPath;
        }

        void ApplySelected() {
            var p=_profiles.SelectedItem as ProfileDefinition;
            if (p==null) return;
            _processManager.StopAll();
            _activeProfile=p;
            Reconcile();
            _timer.Enabled=_autoRecover.Checked;
        }

        void Reconcile() {
            if (_activeProfile==null) return;
            try { UpdateGrid(_processManager.Reconcile(_activeProfile,Handle)); }
            catch(Exception ex) { _summary.Text="Reconcile error: "+ex.Message; Log.WriteException("Profile reconcile failed",ex); }
        }

        void UpdateGrid(IList<BindingRuntimeStatus> rows) {
            _grid.Rows.Clear();
            foreach(var s in rows)
                _grid.Rows.Add(s.Character,s.Role,s.Slot,s.WindowTitle,s.Status);
            int ready=rows.Count(r=>r.Status!=null && (r.Status.StartsWith("Ready") || r.Status=="Ignored by role"));
            _summary.Text=_activeProfile.Name+": "+ready+"/"+rows.Count+" bindings resolved.";
        }

        string GetSelectedName() {
            var p=_profiles.SelectedItem as ProfileDefinition;
            return p==null?null:p.Name;
        }
    }
}
