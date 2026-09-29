using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using OnTopReplica.Native;
using OnTopReplica.Properties;
using OnTopReplica.StartupOptions;
using OnTopReplica.WindowSeekers;
using WindowsFormsAero.Dwm;
using WindowsFormsAero.TaskDialog;

namespace OnTopReplica {

    partial class MainForm : AspectRatioForm {

        //GUI elements
        ThumbnailPanel _thumbnailPanel;

        //Managers
        readonly MessagePumpManager _msgPumpManager = new MessagePumpManager();
        WindowListMenuManager _windowListManager;
        public FullscreenFormManager FullscreenManager { get; private set; }

        Options _startupOptions;

        public MainForm(Options startupOptions) {
            _startupOptions = startupOptions;

            FullscreenManager = new FullscreenFormManager(this);
            _quickRegionDrawingHandler = new ThumbnailPanel.RegionDrawnHandler(HandleQuickRegionDrawn);
            
            //WinForms init pass
            InitializeComponent();
            
            //Store default values
            DefaultNonClickTransparencyKey = this.TransparencyKey;
            DefaultBorderStyle = this.FormBorderStyle;

            //Load saved auto hide border setting
            _autoHideBorder = Settings.Default.AutoHideBorder;

            //Thumbnail panel
            _thumbnailPanel = new ThumbnailPanel {
                Location = Point.Empty,
                Dock = DockStyle.Fill
            };
            _thumbnailPanel.CloneClick += new EventHandler<CloneClickEventArgs>(Thumbnail_CloneClick);
            if (Program.IsManagedReplica) {
                // Keep mouse events inside our own window so a borderless managed replica
                // can be dragged. CloneClick itself is a read-only no-op in this build.
                _thumbnailPanel.ReportThumbnailClicks = true;
                _thumbnailPanel.MouseDown += ManagedReplica_MouseDown;
                _thumbnailPanel.MouseMove += ManagedReplica_MouseMove;
                _thumbnailPanel.MouseUp += ManagedReplica_MouseUp;
            }
            Controls.Add(_thumbnailPanel);

            //Set native renderer on context menus
            Asztal.Szótár.NativeToolStripRenderer.SetToolStripRenderer(
                menuContext, menuWindows, menuOpacity, menuResize, menuFullscreenContext
            );

            //Set to Key event preview
            this.KeyPreview = true;

            Log.Write("Main form constructed");
        }


        bool _managedReplicaDragging;
        Point _managedReplicaDragCursor;
        Point _managedReplicaDragOrigin;

        void ManagedReplica_MouseDown(object sender, MouseEventArgs e) {
            if (!Program.IsManagedReplica || ClickThroughEnabled || e.Button != MouseButtons.Left)
                return;
            _managedReplicaDragging = true;
            _managedReplicaDragCursor = Cursor.Position;
            _managedReplicaDragOrigin = Location;
            Capture = true;
        }

        void ManagedReplica_MouseMove(object sender, MouseEventArgs e) {
            if (!_managedReplicaDragging || !Program.IsManagedReplica || ClickThroughEnabled)
                return;
            Point now = Cursor.Position;
            Location = new Point(
                _managedReplicaDragOrigin.X + now.X - _managedReplicaDragCursor.X,
                _managedReplicaDragOrigin.Y + now.Y - _managedReplicaDragCursor.Y
            );
        }

        void ManagedReplica_MouseUp(object sender, MouseEventArgs e) {
            if (e.Button != MouseButtons.Left)
                return;
            _managedReplicaDragging = false;
            Capture = false;
        }

        #region Event override

        protected override void OnHandleCreated(EventArgs e){
  	        base.OnHandleCreated(e);

            //Window init
            KeepAspectRatio = false;
            GlassMargins = new Padding(-1);

            //Managers
            _msgPumpManager.Initialize(this);
            _windowListManager = new WindowListMenuManager(this, menuWindows);
            _windowListManager.ParentMenus = new System.Windows.Forms.ContextMenuStrip[] {
                menuContext, menuFullscreenContext
            };

            //Platform specific form initialization
            Program.Platform.PostHandleFormInit(this);
        }

        protected override void OnShown(EventArgs e) {
            Log.Write("Main form shown");
            base.OnShown(e);

            //Apply startup options
            _startupOptions.Apply(this);
        }

        protected override void OnClosing(CancelEventArgs e) {
            Log.Write("Main form closing");
            base.OnClosing(e);

            _msgPumpManager.Dispose();
            Program.Platform.CloseForm(this);
        }

        protected override void OnClosed(EventArgs e) {
            Log.Write("Main form closed");
            base.OnClosed(e);
        }

        protected override void OnMove(EventArgs e) {
            base.OnMove(e);

            AdjustSidePanelLocation();
        }

        protected override void OnResizeEnd(EventArgs e) {
            base.OnResizeEnd(e);

            RefreshScreenLock();
        }

        protected override void OnResizing(EventArgs e) {
            //Update aspect ratio from thumbnail while resizing (but do not refresh, resizing does that anyway)
            if (_thumbnailPanel.IsShowingThumbnail) {
                SetAspectRatio(_thumbnailPanel.ThumbnailPixelSize, false);
            }
        }

        protected override void OnActivated(EventArgs e) {
            base.OnActivated(e);

            Program.Platform.RestoreForm(this);
        }

        protected override void OnDeactivate(EventArgs e) {
            base.OnDeactivate(e);

            //HACK: sometimes, even if TopMost is true, the window loses its "always on top" status.
            //  This is a fix attempt that probably won't work...
            if (!FullscreenManager.IsFullscreen) { //fullscreen mode doesn't use TopMost
                TopMost = false;
                TopMost = true;
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e) {
            base.OnMouseWheel(e);

            // Check if Alt key is pressed
            if ((ModifierKeys & Keys.Alt) != Keys.Alt)
                return;

            if (!FullscreenManager.IsFullscreen) {
                if (_thumbnailPanel.IsShowingThumbnail) {
                    SetAspectRatio(_thumbnailPanel.ThumbnailPixelSize, false);
                }

                // Check if Shift key is also pressed for larger increments
                int change = (int)(e.Delta / 6.0); //assumes a mouse wheel "tick" is in the 80-120 range
                if ((ModifierKeys & Keys.Shift) == Keys.Shift) {
                    change *= 3; // Larger increment when Shift is pressed
                }
                
                AdjustSize(change);

                RefreshScreenLock();
            }
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e) {
            base.OnMouseDoubleClick(e);

            if (Program.IsManagedReplica)
                return;

            //This is handled by the WM_NCLBUTTONDBLCLK msg handler usually (because the GlassForm translates
            //clicks on client to clicks on caption). But if fullscreen mode disables GlassForm dragging, we need
            //this auxiliary handler to switch mode.
            FullscreenManager.Toggle();
        }

        protected override void OnMouseClick(MouseEventArgs e) {
            base.OnMouseClick(e);

            //Same story as above (OnMouseDoubleClick)
            if (e.Button == System.Windows.Forms.MouseButtons.Right) {
                OpenContextMenu(null);
            }
        }

        private ThumbnailPanel.RegionDrawnHandler _quickRegionDrawingHandler;

        protected override void WndProc(ref Message m) {
            if (_msgPumpManager != null) {
                if (_msgPumpManager.PumpMessage(ref m)) {
                    return;
                }
            }

            switch (m.Msg) {
                case WM.NCRBUTTONUP:
                    //Open context menu if right button clicked on caption (i.e. all of the window area because of glass)
                    if (m.WParam.ToInt32() == HT.CAPTION) {
                        OpenContextMenu(null);

                        m.Result = IntPtr.Zero;
                        return;
                    }
                    break;

                case WM.NCLBUTTONDOWN:
                    if ((ModifierKeys & Keys.Control) == Keys.Control &&
                        ThumbnailPanel.IsShowingThumbnail &&
                        !ThumbnailPanel.DrawMouseRegions) {

                        ThumbnailPanel.EnableMouseRegionsDrawingWithMouseDown();
                        ThumbnailPanel.RegionDrawn += _quickRegionDrawingHandler;

                        m.Result = IntPtr.Zero;
                        return;
                    }
                    break;

                case WM.NCLBUTTONDBLCLK:
                    if (Program.IsManagedReplica) {
                        m.Result = IntPtr.Zero;
                        return;
                    }
                    //Toggle fullscreen mode if double click on caption (whole glass area)
                    if (m.WParam.ToInt32() == HT.CAPTION) {
                        FullscreenManager.Toggle();

                        m.Result = IntPtr.Zero;
                        return;
                    }
                    break;

                case WM.NCHITTEST:
                    //Managed profile replicas are borderless but draggable. Treat their
                    // entire client surface as a caption; fixed size is enforced separately.
                    if (Program.IsManagedReplica && _startupOptions.FixedSize && !ClickThroughEnabled) {
                        m.Result = (IntPtr)HT.CAPTION;
                        return;
                    }

                    //Make transparent to hit-testing if in click through mode
                    if (ClickThroughEnabled) {
                        m.Result = (IntPtr)HT.TRANSPARENT;

                        RefreshClickThroughComeBack();
                        return;
                    }
                    break;
            }

            base.WndProc(ref m);
        }

        private void HandleQuickRegionDrawn(object sender, ThumbnailRegion region) {
            //Reset region drawing state
            ThumbnailPanel.DrawMouseRegions = false;
            ThumbnailPanel.RegionDrawn -= _quickRegionDrawingHandler;

            SelectedThumbnailRegion = region;
        }

        #endregion

        #region Keyboard event handling

        protected override void OnKeyUp(KeyEventArgs e) {
            base.OnKeyUp(e);

            //ALT
            if (e.Modifiers == Keys.Alt) {
                if (e.KeyCode == Keys.Enter) {
                    e.Handled = true;
                    FullscreenManager.Toggle();
                }

                else if (e.KeyCode == Keys.D1 || e.KeyCode == Keys.NumPad1) {
                    FitToThumbnail(0.25);
                }

                else if (e.KeyCode == Keys.D2 || e.KeyCode == Keys.NumPad2) {
                    FitToThumbnail(0.5);
                }

                else if (e.KeyCode == Keys.D3 || e.KeyCode == Keys.NumPad3 ||
                         e.KeyCode == Keys.D0 || e.KeyCode == Keys.NumPad0) {
                    FitToThumbnail(1.0);
                }

                else if (e.KeyCode == Keys.D4 || e.KeyCode == Keys.NumPad4) {
                    FitToThumbnail(2.0);
                }
            }

            //F11 Fullscreen switch
            else if (e.KeyCode == Keys.F11) {
                e.Handled = true;
                FullscreenManager.Toggle();
            }

            //ESCAPE
            else if (e.KeyCode == Keys.Escape) {
                //Disable click-through
                if (ClickThroughEnabled) {
                    ClickThroughEnabled = false;
                }
                //Toggle fullscreen
                else if (FullscreenManager.IsFullscreen) {
                    FullscreenManager.SwitchBack();
                }
                //Disable click forwarding
                else if (ClickForwardingEnabled) {
                    ClickForwardingEnabled = false;
                }
            }
        }

        #endregion

        #region Thumbnail operation

        /// <summary>
        /// Sets a new thumbnail.
        /// </summary>
        /// <param name="handle">Handle to the window to clone.</param>
        /// <param name="region">Region of the window to clone or null.</param>
        public void SetThumbnail(WindowHandle handle, ThumbnailRegion region) {
            try {
                Log.Write("Cloning window HWND {0} of class {1}", handle.Handle, handle.Class);

                CurrentThumbnailWindowHandle = handle;
                _thumbnailPanel.SetThumbnailHandle(handle, region);

                //Set aspect ratio (this will resize the form), do not refresh if in fullscreen
                SetAspectRatio(_thumbnailPanel.ThumbnailPixelSize, !FullscreenManager.IsFullscreen);

                //Auto hide border if enabled
                if (IsAutoHideBorderEnabled) {
                    IsChromeVisible = false;
                }
            }
            catch (Exception ex) {
                Log.WriteException("Unable to set new thumbnail", ex);

                ThumbnailError(ex, false, Strings.ErrorUnableToCreateThumbnail);
                _thumbnailPanel.UnsetThumbnail();
            }
        }

        /// <summary>
        /// Disables the cloned thumbnail.
        /// </summary>
        public void UnsetThumbnail() {
            //Unset handle
            CurrentThumbnailWindowHandle = null;
            _thumbnailPanel.UnsetThumbnail();

            //Disable aspect ratio
            KeepAspectRatio = false;

            //Restore border if auto hide was enabled
            if (IsAutoHideBorderEnabled) {
                IsChromeVisible = true;
            }
        }

        /// <summary>
        /// Gets or sets the region displayed of the current thumbnail.
        /// </summary>
        public ThumbnailRegion SelectedThumbnailRegion {
            get {
                if (!_thumbnailPanel.IsShowingThumbnail || !_thumbnailPanel.ConstrainToRegion)
                    return null;

                return _thumbnailPanel.SelectedRegion;
            }
            set {
                if (!_thumbnailPanel.IsShowingThumbnail)
                    return;

                _thumbnailPanel.SelectedRegion = value;
                _thumbnailPanel.ConstrainToRegion = true;

                SetAspectRatio(_thumbnailPanel.ThumbnailPixelSize, true);

                FixPositionAndSize();
            }
        }

        const int FixMargin = 10;

        /// <summary>
        /// Fixes the form's position and size, ensuring it is fully displayed in the current screen.
        /// </summary>
        internal void FixPositionAndSize() {
            var screen = Screen.FromControl(this);

            if (Width > screen.WorkingArea.Width) {
                Width = screen.WorkingArea.Width - FixMargin;
            }
            if (Height > screen.WorkingArea.Height) {
                Height = screen.WorkingArea.Height - FixMargin;
            }
            if (Location.X < screen.WorkingArea.Left) {
                Location = new Point(screen.WorkingArea.Left, Location.Y);
            }
            if (Location.Y < screen.WorkingArea.Top) {
                Location = new Point(Location.X, screen.WorkingArea.Top);
            }
            if (Location.X + Width > screen.WorkingArea.Right) {
                Location = new Point(screen.WorkingArea.Right - Width - FixMargin, Location.Y);
            }
            if (Location.Y + Height > screen.WorkingArea.Bottom) {
                Location = new Point(Location.X, screen.WorkingArea.Bottom - Height - FixMargin);
            }
        }

        private void ThumbnailError(Exception ex, bool suppress, string title) {
            if (!suppress) {
                ShowErrorDialog(title, Strings.ErrorGenericThumbnailHandleError, ex.Message);
            }

            UnsetThumbnail();
        }

        /// <summary>Automatically sizes the window in order to accomodate the thumbnail p times.</summary>
        /// <param name="p">Scale of the thumbnail to consider.</param>
        private void FitToThumbnail(double p) {
            try {
                Size originalSize = _thumbnailPanel.ThumbnailPixelSize;
                Size fittedSize = new Size((int)(originalSize.Width * p), (int)(originalSize.Height * p));
                ClientSize = fittedSize;
                RefreshScreenLock();
            }
            catch (Exception ex) {
                ThumbnailError(ex, false, Strings.ErrorUnableToFit);
            }
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Gets the form's thumbnail panel.
        /// </summary>
        public ThumbnailPanel ThumbnailPanel {
            get {
                return _thumbnailPanel;
            }
        }

        /// <summary>
        /// Gets the form's message pump manager.
        /// </summary>
        public MessagePumpManager MessagePumpManager {
            get {
                return _msgPumpManager;
            }
        }

        /// <summary>
        /// Gets the form's window list drop down menu.
        /// </summary>
        public ContextMenuStrip MenuWindows {
            get {
                return menuWindows;
            }
        }

        /// <summary>
        /// Retrieves the window handle of the currently cloned thumbnail.
        /// </summary>
        public WindowHandle CurrentThumbnailWindowHandle {
            get;
            private set;
        }

        #endregion
        
    }
}
