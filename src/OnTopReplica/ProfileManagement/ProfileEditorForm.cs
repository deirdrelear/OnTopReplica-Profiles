using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OnTopReplica.ProfileManagement {
    internal sealed class ProfileEditorForm : Form {
        readonly ProfileRepository _repository;
        readonly WindowDiscovery _discovery=new WindowDiscovery();
        readonly ProfileDefinition _profile;

        TextBox _name;
        TextBox _filter;
        TabControl _tabs;
        DataGridView _bindings;
        ListBox _roles;
        ListBox _replicas;
        TextBox _roleName;
        PropertyGrid _replicaProperties;
        Label _status;

        public ProfileDefinition Profile { get { return _profile; } }

        public ProfileEditorForm(ProfileRepository repository, ProfileDefinition profile) {
            _repository=repository;
            _profile=profile??CreateDefaultProfile();
            Text="OnTopReplica Profile Editor";
            StartPosition=FormStartPosition.CenterParent;
            Size=new Size(1180,760);
            MinimumSize=new Size(900,600);
            BuildUi();
            LoadFromProfile();
        }

        static ProfileDefinition CreateDefaultProfile() {
            var p=new ProfileDefinition { Name="New Profile", WindowTitleFilter="EVE" };
            p.Roles.Add(new RoleDefinition { Name="Default" });
            return p;
        }

        void BuildUi() {
            var header=new TableLayoutPanel { Dock=DockStyle.Top, Height=72, ColumnCount=4, Padding=new Padding(8) };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            header.Controls.Add(new Label { Text="Profile name:", AutoSize=true, Anchor=AnchorStyles.Left },0,0);
            _name=new TextBox { Dock=DockStyle.Fill };
            header.Controls.Add(_name,1,0);
            header.Controls.Add(new Label { Text="Window title filter:", AutoSize=true, Anchor=AnchorStyles.Left },2,0);
            _filter=new TextBox { Dock=DockStyle.Fill };
            header.Controls.Add(_filter,3,0);

            var actions=new FlowLayoutPanel { Dock=DockStyle.Bottom, Height=42, FlowDirection=FlowDirection.RightToLeft, Padding=new Padding(6) };
            var save=new Button { Text="Save", AutoSize=true };
            save.Click+=delegate { SaveProfile(); };
            var cancel=new Button { Text="Close", AutoSize=true };
            cancel.Click+=delegate { Close(); };
            actions.Controls.Add(save);
            actions.Controls.Add(cancel);

            _status=new Label { Dock=DockStyle.Bottom, Height=26, TextAlign=ContentAlignment.MiddleLeft, Padding=new Padding(8,0,0,0) };

            _tabs=new TabControl { Dock=DockStyle.Fill };
            _tabs.TabPages.Add(BuildBindingsTab());
            _tabs.TabPages.Add(BuildRolesTab());

            Controls.Add(_tabs);
            Controls.Add(_status);
            Controls.Add(actions);
            Controls.Add(header);
        }

        TabPage BuildBindingsTab() {
            var tab=new TabPage("Characters / roles");
            var buttons=new FlowLayoutPanel { Dock=DockStyle.Top, Height=40, Padding=new Padding(4) };

            var detect=new Button { Text="Detect EVE windows", AutoSize=true };
            detect.Click+=delegate { DetectWindows(); };
            var add=new Button { Text="Add binding", AutoSize=true };
            add.Click+=delegate { AddBindingRow(null); };
            var remove=new Button { Text="Remove selected", AutoSize=true };
            remove.Click+=delegate {
                foreach(DataGridViewRow row in _bindings.SelectedRows) if (!row.IsNewRow) _bindings.Rows.Remove(row);
            };
            buttons.Controls.Add(detect); buttons.Controls.Add(add); buttons.Controls.Add(remove);

            _bindings=new DataGridView {
                Dock=DockStyle.Fill,
                AllowUserToAddRows=false,
                AllowUserToDeleteRows=true,
                RowHeadersVisible=false,
                AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode=DataGridViewSelectionMode.FullRowSelect
            };
            _bindings.Columns.Add("Character","Character");
            _bindings.Columns.Add("Match","Window title contains");
            var roleCol=new DataGridViewComboBoxColumn { Name="Role", HeaderText="Role", FlatStyle=FlatStyle.Flat };
            _bindings.Columns.Add(roleCol);
            var slot=new DataGridViewTextBoxColumn { Name="Slot", HeaderText="Slot" };
            _bindings.Columns.Add(slot);
            var enabled=new DataGridViewCheckBoxColumn { Name="Enabled", HeaderText="Enabled" };
            _bindings.Columns.Add(enabled);

            tab.Controls.Add(_bindings);
            tab.Controls.Add(buttons);
            return tab;
        }

        TabPage BuildRolesTab() {
            var tab=new TabPage("Roles / replicas");
            var split=new SplitContainer { Dock=DockStyle.Fill, SplitterDistance=280 };

            var rolePanel=new Panel { Dock=DockStyle.Fill };
            var roleButtons=new FlowLayoutPanel { Dock=DockStyle.Top, Height=40, Padding=new Padding(4) };
            var addRole=new Button { Text="Add role", AutoSize=true };
            addRole.Click+=delegate {
                var role=new RoleDefinition { Name="Role"+(_profile.Roles.Count+1) };
                _profile.Roles.Add(role); RefreshRoleList(role);
            };
            var removeRole=new Button { Text="Remove role", AutoSize=true };
            removeRole.Click+=delegate { RemoveSelectedRole(); };
            roleButtons.Controls.Add(addRole); roleButtons.Controls.Add(removeRole);

            _roles=new ListBox { Dock=DockStyle.Fill };
            _roles.SelectedIndexChanged+=delegate { RoleSelectionChanged(); };
            _roleName=new TextBox { Dock=DockStyle.Bottom };
            _roleName.TextChanged+=delegate {
                var r=_roles.SelectedItem as RoleDefinition;
                if (r!=null && r.Name!=_roleName.Text) { r.Name=_roleName.Text; RefreshBindingRoleChoices(); _roles.Refresh(); }
            };
            rolePanel.Controls.Add(_roles); rolePanel.Controls.Add(_roleName); rolePanel.Controls.Add(roleButtons);

            var right=new SplitContainer { Dock=DockStyle.Fill, Orientation=Orientation.Horizontal, SplitterDistance=220 };
            var repPanel=new Panel { Dock=DockStyle.Fill };
            var repButtons=new FlowLayoutPanel { Dock=DockStyle.Top, Height=40, Padding=new Padding(4) };
            var addRep=new Button { Text="Add replica", AutoSize=true };
            addRep.Click+=delegate { AddReplica(); };
            var removeRep=new Button { Text="Remove replica", AutoSize=true };
            removeRep.Click+=delegate { RemoveReplica(); };
            var pick=new Button { Text="Pick source region", AutoSize=true };
            pick.Click+=delegate { PickRegion(); };
            repButtons.Controls.Add(addRep); repButtons.Controls.Add(removeRep); repButtons.Controls.Add(pick);

            _replicas=new ListBox { Dock=DockStyle.Fill };
            _replicas.SelectedIndexChanged+=delegate { _replicaProperties.SelectedObject=_replicas.SelectedItem; };
            repPanel.Controls.Add(_replicas); repPanel.Controls.Add(repButtons);

            _replicaProperties=new PropertyGrid { Dock=DockStyle.Fill, HelpVisible=true, ToolbarVisible=true };
            _replicaProperties.PropertyValueChanged+=delegate { _replicas.Refresh(); };

            right.Panel1.Controls.Add(repPanel);
            right.Panel2.Controls.Add(_replicaProperties);
            split.Panel1.Controls.Add(rolePanel);
            split.Panel2.Controls.Add(right);
            tab.Controls.Add(split);
            return tab;
        }

        void LoadFromProfile() {
            _name.Text=_profile.Name;
            _filter.Text=_profile.WindowTitleFilter;
            RefreshRoleList(_profile.Roles.FirstOrDefault());
            _bindings.Rows.Clear();
            foreach(var b in _profile.Bindings) AddBindingRow(b);
            RefreshBindingRoleChoices();
            _status.Text="Edit roles, bindings and replica properties. Use Pick source region to crop visually.";
        }

        void RefreshRoleList(RoleDefinition select) {
            _roles.DataSource=null;
            _roles.DataSource=_profile.Roles.ToList();
            if (select!=null) _roles.SelectedItem=select;
            RefreshBindingRoleChoices();
        }

        void RoleSelectionChanged() {
            var role=_roles.SelectedItem as RoleDefinition;
            _roleName.Text=role==null?"":role.Name;
            _replicas.DataSource=null;
            if (role!=null) _replicas.DataSource=role.Replicas.ToList();
            _replicaProperties.SelectedObject=_replicas.SelectedItem;
        }

        void RefreshBindingRoleChoices() {
            if (_bindings==null) return;
            var col=_bindings.Columns["Role"] as DataGridViewComboBoxColumn;
            if (col==null) return;
            var values=_profile.Roles.Select(r=>r.Name).ToList();
            col.DataSource=values;
        }

        void AddBindingRow(CharacterBinding b) {
            string role=b==null?(_profile.Roles.FirstOrDefault()==null?"":_profile.Roles.First().Name):b.Role;
            int i=_bindings.Rows.Add(
                b==null?"":b.Character,
                b==null?"":b.WindowTitleContains,
                role,
                b==null?1:b.Slot,
                b==null || b.Enabled);
            if (b!=null) _bindings.Rows[i].Tag=b;
        }

        void DetectWindows() {
            SyncProfileFromUi(false);
            IList<WindowHandle> windows=_discovery.GetCandidateWindows(_profile,Handle);
            int added=0;
            foreach(var w in windows) {
                string character=GuessCharacter(w.Title);
                if (string.IsNullOrWhiteSpace(character)) continue;
                bool exists=_bindings.Rows.Cast<DataGridViewRow>().Any(r =>
                    string.Equals(Convert.ToString(r.Cells["Character"].Value),character,StringComparison.OrdinalIgnoreCase));
                if (exists) continue;
                AddBindingRow(new CharacterBinding {
                    Character=character,
                    WindowTitleContains=character,
                    Role=_profile.Roles.FirstOrDefault()==null?"":_profile.Roles.First().Name,
                    Slot=_bindings.Rows.Count+1,
                    Enabled=true
                });
                added++;
            }
            _status.Text="Detected "+windows.Count+" candidate window(s), added "+added+" new binding(s).";
        }

        static string GuessCharacter(string title) {
            if (string.IsNullOrWhiteSpace(title)) return "";
            string t=title.Trim();
            if (t.StartsWith("EVE - ",StringComparison.OrdinalIgnoreCase)) return t.Substring(6).Trim();
            if (t.EndsWith(" - EVE",StringComparison.OrdinalIgnoreCase)) return t.Substring(0,t.Length-6).Trim();
            return t;
        }

        void AddReplica() {
            var role=_roles.SelectedItem as RoleDefinition;
            if (role==null) return;
            var r=new ReplicaDefinition {
                Name="Replica"+(role.Replicas.Count+1),
                Source=new RectangleDefinition { X=0,Y=0,Width=400,Height=400 },
                Output=new SizeDefinition { Width=300,Height=300 },
                Layout=new GridLayoutDefinition { MonitorIndex=0, Columns=6, GapX=2, GapY=2 },
                ClickThrough=false,
                Borderless=true,
                Opacity=255
            };
            role.Replicas.Add(r);
            RoleSelectionChanged();
            _replicas.SelectedItem=r;
        }

        void RemoveReplica() {
            var role=_roles.SelectedItem as RoleDefinition;
            var replica=_replicas.SelectedItem as ReplicaDefinition;
            if (role==null || replica==null) return;
            role.Replicas.Remove(replica);
            RoleSelectionChanged();
        }

        void RemoveSelectedRole() {
            var role=_roles.SelectedItem as RoleDefinition;
            if (role==null) return;
            if (_profile.Bindings.Any(b=>string.Equals(b.Role,role.Name,StringComparison.OrdinalIgnoreCase))) {
                MessageBox.Show(this,"This role is still assigned to one or more characters.","Role in use",MessageBoxButtons.OK,MessageBoxIcon.Warning);
                return;
            }
            _profile.Roles.Remove(role);
            RefreshRoleList(_profile.Roles.FirstOrDefault());
        }

        void PickRegion() {
            SyncProfileFromUi(false);
            var role=_roles.SelectedItem as RoleDefinition;
            var replica=_replicas.SelectedItem as ReplicaDefinition;
            if (role==null || replica==null) return;

            CharacterBinding binding=_profile.Bindings.FirstOrDefault(b => b.Enabled &&
                string.Equals(b.Role,role.Name,StringComparison.OrdinalIgnoreCase));
            if (binding==null) {
                MessageBox.Show(this,"Assign at least one running character to this role first.","No source character");
                return;
            }

            IList<WindowHandle> windows=_discovery.GetCandidateWindows(_profile,Handle);
            string needle=string.IsNullOrWhiteSpace(binding.WindowTitleContains)?binding.Character:binding.WindowTitleContains;
            WindowHandle source=windows.FirstOrDefault(w=>w.Title.IndexOf(needle,StringComparison.OrdinalIgnoreCase)>=0);
            if (source==null) {
                MessageBox.Show(this,"No currently running source window matched '"+needle+"'.","Window not found");
                return;
            }

            using(var picker=new RegionPickerForm(source)) {
                if (picker.ShowDialog(this)==DialogResult.OK && picker.SelectedRegion!=null) {
                    Rectangle b=picker.SelectedRegion.Bounds;
                    replica.Source.X=b.X; replica.Source.Y=b.Y; replica.Source.Width=b.Width; replica.Source.Height=b.Height;
                    _replicaProperties.Refresh();
                    _status.Text="Source region updated from "+source.Title+".";
                }
            }
        }

        void SyncProfileFromUi(bool validateRows) {
            _profile.Name=_name.Text.Trim();
            _profile.WindowTitleFilter=_filter.Text.Trim();
            var list=new List<CharacterBinding>();
            foreach(DataGridViewRow row in _bindings.Rows) {
                string character=Convert.ToString(row.Cells["Character"].Value).Trim();
                if (string.IsNullOrWhiteSpace(character)) continue;
                int slot;
                if (!Int32.TryParse(Convert.ToString(row.Cells["Slot"].Value),out slot)) slot=1;
                bool enabled=row.Cells["Enabled"].Value==null || Convert.ToBoolean(row.Cells["Enabled"].Value);
                var old=row.Tag as CharacterBinding;
                list.Add(new CharacterBinding {
                    Character=character,
                    WindowTitleContains=Convert.ToString(row.Cells["Match"].Value).Trim(),
                    Role=Convert.ToString(row.Cells["Role"].Value),
                    Slot=Math.Max(1,slot),
                    Enabled=enabled,
                    Placements=old!=null && old.Placements!=null ? old.Placements : new List<ReplicaPlacementDefinition>()
                });
            }
            _profile.Bindings=list;
        }

        void SaveProfile() {
            try {
                SyncProfileFromUi(true);
                _repository.Save(_profile);
                _status.Text="Saved "+_profile.Name+".";
                DialogResult=DialogResult.OK;
            }
            catch(Exception ex) {
                MessageBox.Show(this,ex.Message,"Unable to save profile",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }
    }
}
