using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace Apollo_Technology_Engineer_Application
{
    // =========================================================================
    // MAIN APPLICATION WINDOW
    // =========================================================================
    public partial class Form1 : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();

        private KBWindowForm kbForm;
        private bool isSyncing = false;

        // Controls we need to update dynamically
        private Panel workAreaContainer;
        private Panel whiteBoard;
        private Panel titleBar;
        private Label titleText;
        private Panel controlBox;
        private Button btnMin, btnMax, btnClose;
        private MenuStrip menuStrip;
        private List<Button> actionButtons = new List<Button>();

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.StartPosition = FormStartPosition.CenterScreen;

            SetupTrueWin95Theme();

            // Subscribe to theme changes
            Win95Theme.OnThemeChanged += ApplyTheme;
            ApplyTheme(); // Apply initial
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            kbForm = new KBWindowForm(this);
            kbForm.Show();
            SyncKBWindowLocation();
        }

        private void SyncKBWindowLocation()
        {
            if (kbForm != null && !kbForm.IsDisposed && kbForm.WindowState == FormWindowState.Normal)
            {
                kbForm.Location = new Point(this.Location.X + this.Width + 10, this.Location.Y);
                kbForm.Height = this.Height;
            }
        }

        public void SetWindowState(FormWindowState state)
        {
            if (this.WindowState != state)
            {
                isSyncing = true;
                this.WindowState = state;
                isSyncing = false;
            }
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            SyncKBWindowLocation();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (!isSyncing && kbForm != null && !kbForm.IsDisposed)
            {
                kbForm.SetWindowState(this.WindowState);
            }
            SyncKBWindowLocation();
        }

        private void SetupTrueWin95Theme()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(800, 600);
            this.Font = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Regular);
            this.Padding = new Padding(4, 4, 4, 16);

            // Container
            workAreaContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

            // "Whiteboard" area
            whiteBoard = new Panel { Dock = DockStyle.Fill, BorderStyle = BorderStyle.Fixed3D };

            // Buttons
            Button btnDiagnostic = CreateMenuButton("Run System Health Check", new Point(20, 20));
            btnDiagnostic.Click += BtnHealthCheck_Click;

            Button btnInternetCheck = CreateMenuButton("Check internet access", new Point(20, 60));
            btnInternetCheck.Click += BtnInternetCheck_Click;

            Button btnEngage = CreateMenuButton("Engage Main Drive", new Point(20, 100));

            whiteBoard.Controls.Add(btnDiagnostic);
            whiteBoard.Controls.Add(btnInternetCheck);
            whiteBoard.Controls.Add(btnEngage);
            workAreaContainer.Controls.Add(whiteBoard);

            // Menu Strip
            menuStrip = new MenuStrip { RenderMode = ToolStripRenderMode.System, Dock = DockStyle.Top };

            menuStrip.Items.Add("File");

            ToolStripMenuItem optionsMenu = new ToolStripMenuItem("Options");
            ToolStripMenuItem themeItem = new ToolStripMenuItem("Toggle Dark Mode");
            themeItem.Click += (s, e) => Win95Theme.ToggleTheme();
            optionsMenu.DropDownItems.Add(themeItem);
            menuStrip.Items.Add(optionsMenu);

            ToolStripMenuItem helpMenu = new ToolStripMenuItem("Help");
            ToolStripMenuItem bugItem = new ToolStripMenuItem("Bug report");
            bugItem.Click += (s, e) => { Process.Start(new ProcessStartInfo { FileName = "https://forms.gle/rDxseGaWDFda5Cft7", UseShellExecute = true }); };
            ToolStripMenuItem updateItem = new ToolStripMenuItem("Check for updates");
            updateItem.Click += (s, e) => { MessageBox.Show("Apollo Technology Engineer App is up to date.", "Check for updates", MessageBoxButtons.OK, MessageBoxIcon.Information); };
            ToolStripMenuItem aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += (s, e) => { MessageBox.Show("Apollo Technology Engineer Application\nVersion 1.0\nBuild 1995.04.26\n\nCopyright © 2026 Apollo Technology.", "About", MessageBoxButtons.OK, MessageBoxIcon.Information); };

            helpMenu.DropDownItems.Add(bugItem);
            helpMenu.DropDownItems.Add(updateItem);
            helpMenu.DropDownItems.Add(new ToolStripSeparator());
            helpMenu.DropDownItems.Add(aboutItem);
            menuStrip.Items.Add(helpMenu);

            // Title Bar
            titleBar = new Panel { Height = 18, Dock = DockStyle.Top };
            titleBar.MouseDown += CustomTitleBar_MouseDown;

            titleText = new Label { Text = "Apollo Technology - Engineer Application", Font = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Bold), AutoSize = true, Location = new Point(2, 2) };
            titleText.MouseDown += CustomTitleBar_MouseDown;

            controlBox = new Panel { Size = new Size(54, 18), Dock = DockStyle.Right };

            btnMin = CreateTitleButton("0", new Point(2, 2));
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            btnMax = CreateTitleButton("1", new Point(18, 2));
            btnMax.Click += (s, e) => {
                this.WindowState = this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
                btnMax.Invalidate();
            };
            // Custom paint for Maximize button to toggle icon
            btnMax.Paint += (s, e) => {
                e.Graphics.Clear(Win95Theme.Background);
                string iconChar = this.WindowState == FormWindowState.Maximized ? "2" : "1";
                using (Font marlett = new Font("Marlett", 8.25f))
                    TextRenderer.DrawText(e.Graphics, iconChar, marlett, new Rectangle(0, 0, btnMax.Width, btnMax.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btnMax.Width, btnMax.Height), Border3DStyle.Raised);
            };

            btnClose = CreateTitleButton("r", new Point(36, 2));
            btnClose.Click += (s, e) => Application.Exit();

            controlBox.Controls.Add(btnMin); controlBox.Controls.Add(btnMax); controlBox.Controls.Add(btnClose);
            titleBar.Controls.Add(titleText); titleBar.Controls.Add(controlBox);

            this.Controls.Add(workAreaContainer);
            this.Controls.Add(menuStrip);
            this.Controls.Add(titleBar);
            this.MainMenuStrip = menuStrip;
        }

        private Button CreateMenuButton(string text, Point loc)
        {
            Button btn = new Button { Text = text, Location = loc, Size = new Size(160, 30), FlatStyle = FlatStyle.Standard };
            actionButtons.Add(btn);
            return btn;
        }

        private void ApplyTheme()
        {
            this.BackColor = Win95Theme.Background;
            workAreaContainer.BackColor = Win95Theme.Background;
            whiteBoard.BackColor = Win95Theme.WindowBackground;

            // Buttons
            foreach (var btn in actionButtons)
            {
                btn.BackColor = Win95Theme.ButtonFace;
                btn.ForeColor = Win95Theme.ButtonText;
            }

            // Menu
            menuStrip.BackColor = Win95Theme.Background;
            menuStrip.ForeColor = Win95Theme.Text;

            // Title Bar
            titleBar.BackColor = Win95Theme.TitleBar;
            titleText.ForeColor = Color.White;
            titleText.BackColor = Win95Theme.TitleBar;
            controlBox.BackColor = Win95Theme.TitleBar;

            // Title Buttons
            btnMin.Invalidate();
            btnMax.Invalidate();
            btnClose.Invalidate();

            this.Invalidate(); // Redraw borders
        }

        private void BtnHealthCheck_Click(object sender, EventArgs e)
        {
            string psCommand = "iwr https://apollotech.short.gy/health_check -OutFile heathcheck.ps1; & .\\heathcheck.ps1";
            RetroTerminalForm terminal = new RetroTerminalForm(psCommand);
            terminal.Show();
        }

        private void BtnInternetCheck_Click(object sender, EventArgs e)
        {
            string leftCommand = "ping google.com -n 25";
            string rightCommand = "ping 1.1.1.1 -n 25";
            DualRetroTerminalForm splitTerminal = new DualRetroTerminalForm(leftCommand, rightCommand);
            splitTerminal.Show();
        }

        private Button CreateTitleButton(string marlettChar, Point location)
        {
            Button btn = new Button();
            btn.Size = new Size(16, 14);
            btn.Location = location;
            btn.FlatStyle = FlatStyle.Standard;
            btn.UseVisualStyleBackColor = false;

            btn.Paint += (s, e) =>
            {
                e.Graphics.Clear(Win95Theme.Background); // Dynamic Background
                using (Font marlett = new Font("Marlett", 8.25f))
                {
                    TextRenderer.DrawText(e.Graphics, marlettChar, marlett, new Rectangle(0, 0, btn.Width, btn.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btn.Width, btn.Height), Border3DStyle.Raised);
            };
            return btn;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ControlPaint.DrawBorder3D(e.Graphics, this.ClientRectangle, Border3DStyle.Raised);
            if (this.WindowState != FormWindowState.Maximized)
            {
                Rectangle gripRect = new Rectangle(this.Width - 16, this.Height - 16, 16, 16);
                ControlPaint.DrawSizeGrip(e.Graphics, Win95Theme.Background, gripRect);
            }
        }

        private void CustomTitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && this.WindowState != FormWindowState.Maximized)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x84)
            {
                int x = unchecked((short)m.LParam);
                int y = unchecked((short)((uint)m.LParam >> 16));
                Point pos = this.PointToClient(new Point(x, y));
                int resizeBorder = 5;

                if (pos.X >= this.Width - 16 && pos.Y >= this.Height - 16) m.Result = (IntPtr)17;
                else if (pos.X <= resizeBorder && pos.Y <= resizeBorder) m.Result = (IntPtr)13;
                else if (pos.X >= this.Width - resizeBorder && pos.Y <= resizeBorder) m.Result = (IntPtr)14;
                else if (pos.X <= resizeBorder && pos.Y >= this.Height - resizeBorder) m.Result = (IntPtr)16;
                else if (pos.X <= resizeBorder) m.Result = (IntPtr)10;
                else if (pos.X >= this.Width - resizeBorder) m.Result = (IntPtr)11;
                else if (pos.Y <= resizeBorder) m.Result = (IntPtr)12;
                else if (pos.Y >= this.Height - resizeBorder) m.Result = (IntPtr)15;
            }
        }
    }

    // =========================================================================
    // KNOWLEDGE BASE WEB BROWSER WINDOW
    // =========================================================================
    public class KBWindowForm : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();

        private WebView2 webView;
        private Form1 mainForm;
        private bool isSyncing = false;

        // UI Controls to Update
        private Panel titleBar;
        private Label titleText;
        private Panel controlBox;
        private Button btnMin, btnMax, btnClose;
        private Panel browserContainer;

        public KBWindowForm(Form1 parent)
        {
            this.mainForm = parent;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            SetupTerminalUI();
            InitializeWebView();

            // Subscribe to Theme
            Win95Theme.OnThemeChanged += ApplyTheme;
            ApplyTheme();
        }

        private async void InitializeWebView()
        {
            await webView.EnsureCoreWebView2Async();
            string mobileUserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1";
            webView.CoreWebView2.Settings.UserAgent = mobileUserAgent;
            webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            webView.Source = new Uri("https://kb.apollotechnology.co.uk/");
        }

        public void SetWindowState(FormWindowState state)
        {
            if (this.WindowState != state)
            {
                isSyncing = true;
                this.WindowState = state;
                isSyncing = false;
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (!isSyncing && mainForm != null && !mainForm.IsDisposed)
            {
                mainForm.SetWindowState(this.WindowState);
            }
        }

        private void SetupTerminalUI()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(400, 600);
            this.Padding = new Padding(4, 4, 4, 16);
            this.StartPosition = FormStartPosition.Manual;

            titleBar = new Panel() { Height = 18, Dock = DockStyle.Top };
            titleBar.MouseDown += CustomTitleBar_MouseDown;

            titleText = new Label() { Text = "Apollo Knowledge Base", Font = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Bold), AutoSize = true, Location = new Point(2, 2) };
            titleText.MouseDown += CustomTitleBar_MouseDown;

            controlBox = new Panel() { Size = new Size(54, 18), Dock = DockStyle.Right };

            btnMin = CreateTitleButton("0", new Point(2, 2));
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            btnMax = CreateTitleButton("1", new Point(18, 2));
            btnMax.Click += (s, e) => {
                this.WindowState = this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
                btnMax.Invalidate();
            };
            btnMax.Paint += (s, e) => {
                e.Graphics.Clear(Win95Theme.Background);
                string iconChar = this.WindowState == FormWindowState.Maximized ? "2" : "1";
                using (Font marlett = new Font("Marlett", 8.25f))
                    TextRenderer.DrawText(e.Graphics, iconChar, marlett, new Rectangle(0, 0, btnMax.Width, btnMax.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btnMax.Width, btnMax.Height), Border3DStyle.Raised);
            };

            btnClose = CreateTitleButton("r", new Point(36, 2));
            btnClose.Click += (s, e) => Application.Exit();

            controlBox.Controls.Add(btnMin); controlBox.Controls.Add(btnMax); controlBox.Controls.Add(btnClose);
            titleBar.Controls.Add(titleText); titleBar.Controls.Add(controlBox);

            webView = new WebView2();
            webView.Dock = DockStyle.Fill;

            browserContainer = new Panel() { Dock = DockStyle.Fill, Padding = new Padding(2) };
            browserContainer.Controls.Add(webView);

            this.Controls.Add(browserContainer);
            this.Controls.Add(titleBar);
        }

        private void ApplyTheme()
        {
            this.BackColor = Win95Theme.Background;
            titleBar.BackColor = Win95Theme.TitleBar;
            controlBox.BackColor = Win95Theme.TitleBar;
            titleText.ForeColor = Color.White;
            titleText.BackColor = Win95Theme.TitleBar;
            browserContainer.BackColor = Win95Theme.WindowBackground;

            btnMin.Invalidate();
            btnMax.Invalidate();
            btnClose.Invalidate();
            this.Invalidate();
        }

        private Button CreateTitleButton(string marlettChar, Point location)
        {
            Button btn = new Button() { Size = new Size(16, 14), Location = location, FlatStyle = FlatStyle.Standard, UseVisualStyleBackColor = false };
            btn.Paint += (s, e) => {
                e.Graphics.Clear(Win95Theme.Background);
                using (Font marlett = new Font("Marlett", 8.25f))
                    TextRenderer.DrawText(e.Graphics, marlettChar, marlett, new Rectangle(0, 0, btn.Width, btn.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btn.Width, btn.Height), Border3DStyle.Raised);
            };
            return btn;
        }

        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); ControlPaint.DrawBorder3D(e.Graphics, this.ClientRectangle, Border3DStyle.Raised); if (this.WindowState != FormWindowState.Maximized) ControlPaint.DrawSizeGrip(e.Graphics, Win95Theme.Background, new Rectangle(this.Width - 16, this.Height - 16, 16, 16)); }
        private void CustomTitleBar_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left && this.WindowState != FormWindowState.Maximized) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x84)
            {
                int x = unchecked((short)m.LParam), y = unchecked((short)((uint)m.LParam >> 16)); Point pos = this.PointToClient(new Point(x, y)); int b = 5;
                if (pos.X >= this.Width - 16 && pos.Y >= this.Height - 16) m.Result = (IntPtr)17; else if (pos.X <= b && pos.Y <= b) m.Result = (IntPtr)13; else if (pos.X >= this.Width - b && pos.Y <= b) m.Result = (IntPtr)14; else if (pos.X <= b && pos.Y >= this.Height - b) m.Result = (IntPtr)16; else if (pos.X <= b) m.Result = (IntPtr)10; else if (pos.X >= this.Width - b) m.Result = (IntPtr)11; else if (pos.Y <= b) m.Result = (IntPtr)12; else if (pos.Y >= this.Height - b) m.Result = (IntPtr)15;
            }
        }
    }

    // =========================================================================
    // STANDARD SINGLE MS-DOS PROMPT TERMINAL 
    // =========================================================================
    public class RetroTerminalForm : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();

        private TextBox consoleBox;
        private string commandToRun;

        // UI
        private Panel titleBar;
        private Label titleText;
        private Panel controlBox;
        private Button btnMin, btnMax, btnClose;
        private Panel consoleContainer;

        public RetroTerminalForm(string command)
        {
            this.commandToRun = command;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            SetupTerminalUI();
            RunScriptInBackground();

            Win95Theme.OnThemeChanged += ApplyTheme;
            ApplyTheme();
        }

        private void SetupTerminalUI()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(640, 400);
            this.Padding = new Padding(4, 4, 4, 16);
            this.StartPosition = FormStartPosition.CenterScreen;

            titleBar = new Panel() { Height = 18, Dock = DockStyle.Top };
            titleBar.MouseDown += CustomTitleBar_MouseDown;

            titleText = new Label() { Text = "MS-DOS Prompt", Font = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Bold), AutoSize = true, Location = new Point(2, 2) };
            titleText.MouseDown += CustomTitleBar_MouseDown;

            controlBox = new Panel() { Size = new Size(54, 18), Dock = DockStyle.Right };

            btnMin = CreateTitleButton("0", new Point(2, 2));
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            btnMax = CreateTitleButton("1", new Point(18, 2));
            btnMax.Click += (s, e) => {
                this.WindowState = this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
                btnMax.Invalidate();
            };
            btnMax.Paint += (s, e) => {
                e.Graphics.Clear(Win95Theme.Background);
                string iconChar = this.WindowState == FormWindowState.Maximized ? "2" : "1";
                using (Font marlett = new Font("Marlett", 8.25f))
                    TextRenderer.DrawText(e.Graphics, iconChar, marlett, new Rectangle(0, 0, btnMax.Width, btnMax.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btnMax.Width, btnMax.Height), Border3DStyle.Raised);
            };

            btnClose = CreateTitleButton("r", new Point(36, 2));
            btnClose.Click += (s, e) => this.Close();

            controlBox.Controls.Add(btnMin); controlBox.Controls.Add(btnMax); controlBox.Controls.Add(btnClose);
            titleBar.Controls.Add(titleText); titleBar.Controls.Add(controlBox);

            consoleBox = new TextBox() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, BackColor = Color.Black, ForeColor = Color.LightGray, Font = new Font("Consolas", 9.75f, FontStyle.Bold), BorderStyle = BorderStyle.None, ScrollBars = ScrollBars.Vertical };
            consoleContainer = new Panel() { Dock = DockStyle.Fill, Padding = new Padding(2) };
            consoleContainer.Controls.Add(consoleBox);

            this.Controls.Add(consoleContainer); this.Controls.Add(titleBar);
        }

        private void ApplyTheme()
        {
            this.BackColor = Win95Theme.Background;
            titleBar.BackColor = Win95Theme.TitleBar;
            controlBox.BackColor = Win95Theme.TitleBar;
            titleText.ForeColor = Color.White;
            titleText.BackColor = Win95Theme.TitleBar;
            consoleContainer.BackColor = Win95Theme.WindowBackground;

            btnMin.Invalidate();
            btnMax.Invalidate();
            btnClose.Invalidate();
            this.Invalidate();
        }

        private Button CreateTitleButton(string marlettChar, Point location)
        {
            Button btn = new Button() { Size = new Size(16, 14), Location = location, FlatStyle = FlatStyle.Standard, UseVisualStyleBackColor = false };
            btn.Paint += (s, e) => {
                e.Graphics.Clear(Win95Theme.Background);
                using (Font marlett = new Font("Marlett", 8.25f))
                    TextRenderer.DrawText(e.Graphics, marlettChar, marlett, new Rectangle(0, 0, btn.Width, btn.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btn.Width, btn.Height), Border3DStyle.Raised);
            };
            return btn;
        }

        private void RunScriptInBackground()
        {
            consoleBox.AppendText("C:\\APOLLO_SYS> Initializing Diagnostics...\r\n");
            Process p = new Process();
            p.StartInfo.FileName = "powershell.exe";
            p.StartInfo.Arguments = "-ExecutionPolicy Bypass -WindowStyle Hidden -NonInteractive -NoProfile -Command \"" + commandToRun + "\"";
            p.StartInfo.UseShellExecute = false; p.StartInfo.RedirectStandardOutput = true; p.StartInfo.RedirectStandardError = true; p.StartInfo.CreateNoWindow = true;

            p.OutputDataReceived += (s, a) => { if (a.Data != null) AppendToConsole(a.Data); };
            p.ErrorDataReceived += (s, a) => { if (a.Data != null) AppendToConsole("ERROR: " + a.Data); };
            p.EnableRaisingEvents = true;
            p.Exited += (s, a) => { AppendToConsole("\r\nC:\\APOLLO_SYS> Task Completed."); };

            try { p.Start(); p.BeginOutputReadLine(); p.BeginErrorReadLine(); } catch (Exception ex) { AppendToConsole("SYSTEM FAILURE: " + ex.Message); }
        }

        private void AppendToConsole(string text)
        {
            if (this.InvokeRequired) { this.Invoke(new Action<string>(AppendToConsole), new object[] { text }); return; }
            consoleBox.AppendText(text + "\r\n"); consoleBox.SelectionStart = consoleBox.Text.Length; consoleBox.ScrollToCaret();
        }

        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); ControlPaint.DrawBorder3D(e.Graphics, this.ClientRectangle, Border3DStyle.Raised); if (this.WindowState != FormWindowState.Maximized) ControlPaint.DrawSizeGrip(e.Graphics, Win95Theme.Background, new Rectangle(this.Width - 16, this.Height - 16, 16, 16)); }
        private void CustomTitleBar_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left && this.WindowState != FormWindowState.Maximized) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x84)
            {
                int x = unchecked((short)m.LParam), y = unchecked((short)((uint)m.LParam >> 16)); Point pos = this.PointToClient(new Point(x, y)); int b = 5;
                if (pos.X >= this.Width - 16 && pos.Y >= this.Height - 16) m.Result = (IntPtr)17; else if (pos.X <= b && pos.Y <= b) m.Result = (IntPtr)13; else if (pos.X >= this.Width - b && pos.Y <= b) m.Result = (IntPtr)14; else if (pos.X <= b && pos.Y >= this.Height - b) m.Result = (IntPtr)16; else if (pos.X <= b) m.Result = (IntPtr)10; else if (pos.X >= this.Width - b) m.Result = (IntPtr)11; else if (pos.Y <= b) m.Result = (IntPtr)12; else if (pos.Y >= this.Height - b) m.Result = (IntPtr)15;
            }
        }
    }

    // =========================================================================
    // SPLIT-SCREEN DUAL MS-DOS PROMPT TERMINAL
    // =========================================================================
    public class DualRetroTerminalForm : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();

        private TextBox leftBox;
        private TextBox rightBox;
        private string commandLeft;
        private string commandRight;

        // UI
        private Panel titleBar;
        private Label titleText;
        private Panel controlBox;
        private Button btnMin, btnMax, btnClose;
        private TableLayoutPanel splitPanel;

        public DualRetroTerminalForm(string cmdLeft, string cmdRight)
        {
            this.commandLeft = cmdLeft;
            this.commandRight = cmdRight;

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);

            SetupTerminalUI();
            RunProcess(commandLeft, leftBox);
            RunProcess(commandRight, rightBox);

            Win95Theme.OnThemeChanged += ApplyTheme;
            ApplyTheme();
        }

        private void SetupTerminalUI()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(1000, 450);
            this.Padding = new Padding(4, 4, 4, 16);
            this.StartPosition = FormStartPosition.CenterScreen;

            titleBar = new Panel() { Height = 18, Dock = DockStyle.Top };
            titleBar.MouseDown += CustomTitleBar_MouseDown;

            titleText = new Label() { Text = "MS-DOS Prompt - Dual Network Diagnostics", Font = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Bold), AutoSize = true, Location = new Point(2, 2) };
            titleText.MouseDown += CustomTitleBar_MouseDown;

            controlBox = new Panel() { Size = new Size(54, 18), Dock = DockStyle.Right };

            btnMin = CreateTitleButton("0", new Point(2, 2));
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            btnMax = CreateTitleButton("1", new Point(18, 2));
            btnMax.Click += (s, e) => {
                this.WindowState = this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
                btnMax.Invalidate();
            };
            btnMax.Paint += (s, e) => {
                e.Graphics.Clear(Win95Theme.Background);
                string iconChar = this.WindowState == FormWindowState.Maximized ? "2" : "1";
                using (Font marlett = new Font("Marlett", 8.25f))
                    TextRenderer.DrawText(e.Graphics, iconChar, marlett, new Rectangle(0, 0, btnMax.Width, btnMax.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btnMax.Width, btnMax.Height), Border3DStyle.Raised);
            };

            btnClose = CreateTitleButton("r", new Point(36, 2));
            btnClose.Click += (s, e) => this.Close();

            controlBox.Controls.Add(btnMin); controlBox.Controls.Add(btnMax); controlBox.Controls.Add(btnClose);
            titleBar.Controls.Add(titleText); titleBar.Controls.Add(controlBox);

            splitPanel = new TableLayoutPanel();
            splitPanel.Dock = DockStyle.Fill;
            splitPanel.ColumnCount = 2;
            splitPanel.RowCount = 1;
            splitPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            splitPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            splitPanel.Padding = new Padding(2);

            leftBox = new TextBox() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, BackColor = Color.Black, ForeColor = Color.LightGray, Font = new Font("Consolas", 9.75f, FontStyle.Bold), BorderStyle = BorderStyle.None, ScrollBars = ScrollBars.Vertical, Margin = new Padding(0, 0, 1, 0) };
            rightBox = new TextBox() { Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, BackColor = Color.Black, ForeColor = Color.LightGray, Font = new Font("Consolas", 9.75f, FontStyle.Bold), BorderStyle = BorderStyle.None, ScrollBars = ScrollBars.Vertical, Margin = new Padding(1, 0, 0, 0) };

            splitPanel.Controls.Add(leftBox, 0, 0);
            splitPanel.Controls.Add(rightBox, 1, 0);

            this.Controls.Add(splitPanel);
            this.Controls.Add(titleBar);
        }

        private void ApplyTheme()
        {
            this.BackColor = Win95Theme.Background;
            titleBar.BackColor = Win95Theme.TitleBar;
            controlBox.BackColor = Win95Theme.TitleBar;
            titleText.ForeColor = Color.White;
            titleText.BackColor = Win95Theme.TitleBar;
            splitPanel.BackColor = Win95Theme.WindowBackground;

            btnMin.Invalidate();
            btnMax.Invalidate();
            btnClose.Invalidate();
            this.Invalidate();
        }

        private Button CreateTitleButton(string marlettChar, Point location)
        {
            Button btn = new Button() { Size = new Size(16, 14), Location = location, FlatStyle = FlatStyle.Standard, UseVisualStyleBackColor = false };
            btn.Paint += (s, e) => {
                e.Graphics.Clear(Win95Theme.Background);
                using (Font marlett = new Font("Marlett", 8.25f))
                    TextRenderer.DrawText(e.Graphics, marlettChar, marlett, new Rectangle(0, 0, btn.Width, btn.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btn.Width, btn.Height), Border3DStyle.Raised);
            };
            return btn;
        }

        private void RunProcess(string command, TextBox targetBox)
        {
            targetBox.AppendText($"C:\\APOLLO_SYS> Executing: {command}\r\n\r\n");

            Process p = new Process();
            p.StartInfo.FileName = "powershell.exe";
            p.StartInfo.Arguments = "-ExecutionPolicy Bypass -WindowStyle Hidden -NonInteractive -NoProfile -Command \"" + command + "\"";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;

            p.OutputDataReceived += (s, a) => { if (a.Data != null) AppendToConsole(targetBox, a.Data); };
            p.ErrorDataReceived += (s, a) => { if (a.Data != null) AppendToConsole(targetBox, "ERROR: " + a.Data); };
            p.EnableRaisingEvents = true;
            p.Exited += (s, a) => { AppendToConsole(targetBox, "\r\nC:\\APOLLO_SYS> Task Completed."); };

            try { p.Start(); p.BeginOutputReadLine(); p.BeginErrorReadLine(); } catch (Exception ex) { AppendToConsole(targetBox, "SYSTEM FAILURE: " + ex.Message); }
        }

        private void AppendToConsole(TextBox box, string text)
        {
            if (box.InvokeRequired)
            {
                box.Invoke(new Action<TextBox, string>(AppendToConsole), new object[] { box, text });
                return;
            }
            box.AppendText(text + "\r\n");
            box.SelectionStart = box.Text.Length;
            box.ScrollToCaret();
        }

        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); ControlPaint.DrawBorder3D(e.Graphics, this.ClientRectangle, Border3DStyle.Raised); if (this.WindowState != FormWindowState.Maximized) ControlPaint.DrawSizeGrip(e.Graphics, Win95Theme.Background, new Rectangle(this.Width - 16, this.Height - 16, 16, 16)); }
        private void CustomTitleBar_MouseDown(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left && this.WindowState != FormWindowState.Maximized) { ReleaseCapture(); SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0); } }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x84)
            {
                int x = unchecked((short)m.LParam), y = unchecked((short)((uint)m.LParam >> 16)); Point pos = this.PointToClient(new Point(x, y)); int b = 5;
                if (pos.X >= this.Width - 16 && pos.Y >= this.Height - 16) m.Result = (IntPtr)17; else if (pos.X <= b && pos.Y <= b) m.Result = (IntPtr)13; else if (pos.X >= this.Width - b && pos.Y <= b) m.Result = (IntPtr)14; else if (pos.X <= b && pos.Y >= this.Height - b) m.Result = (IntPtr)16; else if (pos.X <= b) m.Result = (IntPtr)10; else if (pos.X >= this.Width - b) m.Result = (IntPtr)11; else if (pos.Y <= b) m.Result = (IntPtr)12; else if (pos.Y >= this.Height - b) m.Result = (IntPtr)15;
            }
        }
    }

    // =========================================================================
    // THEME MANAGER
    // =========================================================================
    public static class Win95Theme
    {
        public static bool IsDarkMode { get; private set; } = false;

        // Event to notify all open forms that the theme has changed
        public static event Action OnThemeChanged;

        // Colors
        public static Color Background => IsDarkMode ? Color.FromArgb(50, 50, 50) : Color.FromArgb(192, 192, 192);
        public static Color Text => IsDarkMode ? Color.White : Color.Black;
        public static Color WindowBackground => IsDarkMode ? Color.FromArgb(30, 30, 30) : Color.White;
        public static Color WindowText => IsDarkMode ? Color.FromArgb(220, 220, 220) : Color.Black;
        public static Color TitleBar => Color.FromArgb(0, 0, 128); // Keep classic blue for identity
        public static Color ButtonFace => IsDarkMode ? Color.FromArgb(70, 70, 70) : Color.FromArgb(192, 192, 192);
        public static Color ButtonText => IsDarkMode ? Color.White : Color.Black;

        public static void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
            OnThemeChanged?.Invoke();
        }
    }
}