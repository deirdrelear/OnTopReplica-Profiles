using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace OnTopReplica.ProfileManagement {
    internal sealed class ProfileRepository {
        readonly string _folder;

        public ProfileRepository() {
            _folder = Path.Combine(AppPaths.PrivateRoamingFolderPath, "Profiles");
        }

        public string FolderPath { get { return _folder; } }

        public IList<ProfileDefinition> LoadAll() {
            EnsureFolder();
            var result = new List<ProfileDefinition>();
            foreach (string file in Directory.GetFiles(_folder, "*.json").OrderBy(p => p, StringComparer.OrdinalIgnoreCase)) {
                try {
                    using (var stream = File.OpenRead(file)) {
                        var serializer = new DataContractJsonSerializer(typeof(ProfileDefinition));
                        var profile = serializer.ReadObject(stream) as ProfileDefinition;
                        if (profile != null) {
                            Normalize(profile);
                            Validate(profile, file);
                            result.Add(profile);
                        }
                    }
                }
                catch (Exception ex) {
                    Log.WriteException("Unable to load profile " + file, ex);
                }
            }
            return result;
        }

        public void EnsureExamples() {
            EnsureFolder();
            string path = Path.Combine(_folder, "Mining.example.json");
            if (!File.Exists(path))
                File.WriteAllText(path, MiningExample, new UTF8Encoding(false));
        }

        void EnsureFolder() {
            if (!Directory.Exists(_folder))
                Directory.CreateDirectory(_folder);
        }

        static void Normalize(ProfileDefinition p) {
            if (p.Roles == null) p.Roles = new List<RoleDefinition>();
            if (p.Bindings == null) p.Bindings = new List<CharacterBinding>();
            foreach (var role in p.Roles) {
                if (role.Replicas == null) role.Replicas = new List<ReplicaDefinition>();
            }
        }

        static void Validate(ProfileDefinition p, string source) {
            if (string.IsNullOrWhiteSpace(p.Name))
                throw new InvalidDataException("Profile name is empty: " + source);

            var roleNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var role in p.Roles) {
                if (role == null || string.IsNullOrWhiteSpace(role.Name))
                    throw new InvalidDataException("Role name is empty in " + source);
                if (!roleNames.Add(role.Name))
                    throw new InvalidDataException("Duplicate role '" + role.Name + "' in " + source);
                foreach (var replica in role.Replicas) {
                    if (replica.Source == null || replica.Source.Width <= 0 || replica.Source.Height <= 0)
                        throw new InvalidDataException("Invalid source region in role " + role.Name);
                    if (replica.Output == null || replica.Output.Width <= 0 || replica.Output.Height <= 0)
                        throw new InvalidDataException("Invalid output size in role " + role.Name);
                    if (replica.Layout == null)
                        throw new InvalidDataException("Missing layout in role " + role.Name);
                }
            }

            var slots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var b in p.Bindings.Where(x => x != null && x.Enabled)) {
                if (string.IsNullOrWhiteSpace(b.Character))
                    throw new InvalidDataException("Binding character is empty");
                if (string.IsNullOrWhiteSpace(b.Role) || !roleNames.Contains(b.Role))
                    throw new InvalidDataException("Unknown role '" + b.Role + "' for " + b.Character);
                if (b.Slot < 1)
                    throw new InvalidDataException("Slot must be >= 1 for " + b.Character);
                string k=b.Role+"|"+b.Slot;
                if (!slots.Add(k))
                    throw new InvalidDataException("Duplicate slot " + b.Slot + " in role " + b.Role);
            }
        }

        const string MiningExample = @"{
  ""name"": ""Mining"",
  ""windowTitleFilter"": ""EVE"",
  ""roles"": [
    {
      ""name"": ""Barge"",
      ""replicas"": [
        {
          ""name"": ""Local"",
          ""source"": { ""x"": 0, ""y"": 0, ""width"": 360, ""height"": 720 },
          ""output"": { ""width"": 240, ""height"": 480 },
          ""layout"": { ""monitorIndex"": 1, ""originX"": 0, ""originY"": 0, ""columns"": 9, ""gapX"": 2, ""gapY"": 2, ""useWorkingArea"": false },
          ""clickThrough"": true,
          ""borderless"": true,
          ""opacity"": 255
        }
      ]
    },
    { ""name"": ""Booster"", ""replicas"": [] },
    {
      ""name"": ""Scout"",
      ""replicas"": [
        {
          ""name"": ""Local"",
          ""source"": { ""x"": 0, ""y"": 0, ""width"": 360, ""height"": 720 },
          ""output"": { ""width"": 300, ""height"": 600 },
          ""layout"": { ""monitorIndex"": 1, ""originX"": 0, ""originY"": 970, ""columns"": 2, ""gapX"": 4, ""gapY"": 4, ""useWorkingArea"": false },
          ""clickThrough"": true,
          ""borderless"": true,
          ""opacity"": 255
        }
      ]
    }
  ],
  ""bindings"": [
    { ""character"": ""Barge01"", ""role"": ""Barge"", ""slot"": 1, ""enabled"": true },
    { ""character"": ""Booster01"", ""role"": ""Booster"", ""slot"": 1, ""enabled"": true },
    { ""character"": ""Scout01"", ""role"": ""Scout"", ""slot"": 1, ""enabled"": true },
    { ""character"": ""Scout02"", ""role"": ""Scout"", ""slot"": 2, ""enabled"": true }
  ]
}";
    }
}
