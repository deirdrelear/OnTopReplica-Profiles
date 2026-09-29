using System;
using System.Drawing;
using System.Windows.Forms;

namespace OnTopReplica.ProfileManagement {
    internal sealed class RegionPickerForm : Form {
        readonly ThumbnailPanel _panel;
        public ThumbnailRegion SelectedRegion { get; private set; }

        public RegionPickerForm(WindowHandle source) {
            if (source==null) throw new ArgumentNullException("source");
            Text="Select source region - drag with left mouse button";
            StartPosition=FormStartPosition.CenterScreen;
            ClientSize=new Size(1100,700);
            MinimumSize=new Size(640,420);

            _panel=new ThumbnailPanel { Dock=DockStyle.Fill };
            Controls.Add(_panel);

            var instructions=new Label {
                Dock=DockStyle.Top,
                Height=30,
                Text="Drag a rectangle over the source window. The dialog closes when the region is selected.",
                TextAlign=ContentAlignment.MiddleCenter
            };
            Controls.Add(instructions);
            instructions.BringToFront();

            Shown+=delegate {
                _panel.SetThumbnailHandle(source,null);
                _panel.DrawMouseRegions=true;
            };
            _panel.RegionDrawn+=delegate(object sender, ThumbnailRegion region) {
                SelectedRegion=region;
                DialogResult=DialogResult.OK;
                Close();
            };
            FormClosed+=delegate { _panel.UnsetThumbnail(); };
        }
    }
}
