using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace OnTopReplica.ProfileManagement {
    [DataContract]
    public sealed class ProfileDefinition {
        public ProfileDefinition() {
            Name = "New profile";
            WindowTitleFilter = "EVE";
            Roles = new List<RoleDefinition>();
            Bindings = new List<CharacterBinding>();
        }

        [DataMember(Name="name", Order=1)] public string Name { get; set; }
        [DataMember(Name="windowTitleFilter", Order=2, EmitDefaultValue=false)] public string WindowTitleFilter { get; set; }
        [DataMember(Name="roles", Order=3)] public List<RoleDefinition> Roles { get; set; }
        [DataMember(Name="bindings", Order=4)] public List<CharacterBinding> Bindings { get; set; }
    }

    [DataContract]
    public sealed class RoleDefinition {
        public RoleDefinition() { Name = "Role"; Replicas = new List<ReplicaDefinition>(); }
        [DataMember(Name="name", Order=1)] public string Name { get; set; }
        [DataMember(Name="replicas", Order=2)] public List<ReplicaDefinition> Replicas { get; set; }
    }

    [DataContract]
    public sealed class CharacterBinding {
        public CharacterBinding() { Enabled = true; Slot = 1; }
        [DataMember(Name="character", Order=1)] public string Character { get; set; }
        [DataMember(Name="windowTitleContains", Order=2, EmitDefaultValue=false)] public string WindowTitleContains { get; set; }
        [DataMember(Name="role", Order=3)] public string Role { get; set; }
        [DataMember(Name="slot", Order=4)] public int Slot { get; set; }
        [DataMember(Name="enabled", Order=5)] public bool Enabled { get; set; }
    }

    [DataContract]
    public sealed class ReplicaDefinition {
        public ReplicaDefinition() {
            Name = "Replica";
            Source = new RectangleDefinition();
            Output = new SizeDefinition();
            Layout = new GridLayoutDefinition();
            ClickThrough = true;
            Borderless = true;
            Opacity = 255;
        }

        [DataMember(Name="name", Order=1)] public string Name { get; set; }
        [DataMember(Name="source", Order=2)] public RectangleDefinition Source { get; set; }
        [DataMember(Name="output", Order=3)] public SizeDefinition Output { get; set; }
        [DataMember(Name="layout", Order=4)] public GridLayoutDefinition Layout { get; set; }
        [DataMember(Name="clickThrough", Order=5)] public bool ClickThrough { get; set; }
        [DataMember(Name="borderless", Order=6)] public bool Borderless { get; set; }
        [DataMember(Name="opacity", Order=7)] public int Opacity { get; set; }
    }

    [DataContract]
    public sealed class RectangleDefinition {
        [DataMember(Name="x", Order=1)] public int X { get; set; }
        [DataMember(Name="y", Order=2)] public int Y { get; set; }
        [DataMember(Name="width", Order=3)] public int Width { get; set; }
        [DataMember(Name="height", Order=4)] public int Height { get; set; }
    }

    [DataContract]
    public sealed class SizeDefinition {
        [DataMember(Name="width", Order=1)] public int Width { get; set; }
        [DataMember(Name="height", Order=2)] public int Height { get; set; }
    }

    [DataContract]
    public sealed class GridLayoutDefinition {
        public GridLayoutDefinition() { MonitorIndex=0; Columns=1; UseWorkingArea=false; }
        [DataMember(Name="monitorIndex", Order=1)] public int MonitorIndex { get; set; }
        [DataMember(Name="monitorDeviceName", Order=2, EmitDefaultValue=false)] public string MonitorDeviceName { get; set; }
        [DataMember(Name="originX", Order=3)] public int OriginX { get; set; }
        [DataMember(Name="originY", Order=4)] public int OriginY { get; set; }
        [DataMember(Name="columns", Order=5)] public int Columns { get; set; }
        [DataMember(Name="gapX", Order=6)] public int GapX { get; set; }
        [DataMember(Name="gapY", Order=7)] public int GapY { get; set; }
        [DataMember(Name="useWorkingArea", Order=8)] public bool UseWorkingArea { get; set; }
    }

    public sealed class BindingRuntimeStatus {
        public string Character { get; set; }
        public string Role { get; set; }
        public int Slot { get; set; }
        public string WindowTitle { get; set; }
        public string Status { get; set; }
    }

    internal sealed class DesiredReplica {
        public string Key { get; set; }
        public string Character { get; set; }
        public IntPtr SourceHandle { get; set; }
        public string Arguments { get; set; }
        public string Signature { get; set; }
    }
}
