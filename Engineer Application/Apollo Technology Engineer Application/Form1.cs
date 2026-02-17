using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Apollo_Technology_Engineer_Application
{
    public partial class Form1 : Form
    {
        // Title Bar Dragging API
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        // Exact classic Windows 95 colors
        private readonly Color win95Gray = Color.FromArgb(192, 192, 192);
        private readonly Color win95TitleBlue = Color.FromArgb(0, 0, 128);

        public Form1()
        {
            InitializeComponent();
            SetupTrueWin95Theme();
        }

        private void SetupTrueWin95Theme()
        {
            // Remove modern borders
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = win95Gray;
            this.Size = new Size(800, 600);
            this.Font = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Regular);

            // Leave exactly 4 pixels of padding around the app for our resize grab-handles
            this.Padding = new Padding(4);

            // --- MAIN WHITEBOARD AREA ---
            Panel workAreaContainer = new Panel();
            workAreaContainer.Dock = DockStyle.Fill;
            workAreaContainer.Padding = new Padding(8);

            Panel whiteBoard = new Panel();
            whiteBoard.BackColor = Color.White;
            whiteBoard.Dock = DockStyle.Fill;
            whiteBoard.BorderStyle = BorderStyle.Fixed3D;

            Button btnDiagnostic = new Button() { Text = "Run System Diagnostics", Location = new Point(20, 20), Size = new Size(160, 30), FlatStyle = FlatStyle.Standard };
            Button btnCalibrate = new Button() { Text = "Calibrate Thrusters", Location = new Point(20, 60), Size = new Size(160, 30), FlatStyle = FlatStyle.Standard };
            Button btnEngage = new Button() { Text = "Engage Main Drive", Location = new Point(20, 100), Size = new Size(160, 30), FlatStyle = FlatStyle.Standard };

            whiteBoard.Controls.Add(btnDiagnostic);
            whiteBoard.Controls.Add(btnCalibrate);
            whiteBoard.Controls.Add(btnEngage);
            workAreaContainer.Controls.Add(whiteBoard);

            // --- MENU BAR ---
            MenuStrip menuStrip = new MenuStrip();
            menuStrip.RenderMode = ToolStripRenderMode.System;
            menuStrip.BackColor = win95Gray;
            menuStrip.Dock = DockStyle.Top;

            menuStrip.Items.Add("File");
            menuStrip.Items.Add("Options");

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

            // --- FAKE WINDOWS 95 TITLE BAR ---
            Panel titleBar = new Panel();
            titleBar.Height = 18;
            titleBar.Dock = DockStyle.Top;
            titleBar.BackColor = win95TitleBlue;
            titleBar.MouseDown += CustomTitleBar_MouseDown;

            Label titleText = new Label();
            titleText.Text = "Apollo Technology - Engineer Application";
            titleText.ForeColor = Color.White;
            titleText.Font = new Font("Microsoft Sans Serif", 8.25f, FontStyle.Bold);
            titleText.AutoSize = true;
            titleText.Location = new Point(2, 2);
            titleText.MouseDown += CustomTitleBar_MouseDown;

            // --- CONTROL BOX (Min, Max, Close) ---
            Panel controlBox = new Panel();
            controlBox.Size = new Size(54, 18);
            controlBox.Dock = DockStyle.Right;
            controlBox.BackColor = win95TitleBlue;

            Button btnMin = CreateTitleButton("0", new Point(2, 2));
            btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

            Button btnMax = CreateTitleButton("1", new Point(18, 2));
            btnMax.Click += (s, e) => {
                if (this.WindowState == FormWindowState.Maximized)
                {
                    this.WindowState = FormWindowState.Normal;
                    // Flip back to Maximize icon
                    btnMax.Invalidate(); // Forces a redraw
                }
                else
                {
                    this.WindowState = FormWindowState.Maximized;
                    // Flip to Restore icon
                    btnMax.Invalidate();
                }
            };

            // Custom logic for the Maximize button redraw so it swaps the icon
            btnMax.Paint += (s, e) =>
            {
                e.Graphics.Clear(win95Gray);
                string iconChar = this.WindowState == FormWindowState.Maximized ? "2" : "1";
                using (Font marlett = new Font("Marlett", 8.25f))
                {
                    TextRenderer.DrawText(e.Graphics, iconChar, marlett,
                        new Rectangle(0, 0, btnMax.Width, btnMax.Height),
                        Color.Black,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
                // Draw standard 3D border
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btnMax.Width, btnMax.Height), Border3DStyle.Raised);
            };

            Button btnClose = CreateTitleButton("r", new Point(36, 2));
            btnClose.Click += (s, e) => this.Close();

            controlBox.Controls.Add(btnMin);
            controlBox.Controls.Add(btnMax);
            controlBox.Controls.Add(btnClose);

            titleBar.Controls.Add(titleText);
            titleBar.Controls.Add(controlBox);

            // --- ASSEMBLE ---
            this.Controls.Add(workAreaContainer);
            this.Controls.Add(menuStrip);
            this.Controls.Add(titleBar);
            this.MainMenuStrip = menuStrip;
        }

        // Helper to generate perfectly fitted grey title bar buttons with pixel-perfect centering
        private Button CreateTitleButton(string marlettChar, Point location)
        {
            Button btn = new Button();

            btn.Size = new Size(16, 14);
            btn.Location = location;
            btn.FlatStyle = FlatStyle.Standard;
            btn.BackColor = win95Gray;
            btn.UseVisualStyleBackColor = false;

            // Manually draw the Marlett glyph to force absolute pixel-perfect centering
            btn.Paint += (s, e) =>
            {
                e.Graphics.Clear(win95Gray); // Clear the background

                using (Font marlett = new Font("Marlett", 8.25f))
                {
                    // TextFormatFlags.NoPadding strips away the invisible margins
                    TextRenderer.DrawText(e.Graphics, marlettChar, marlett,
                        new Rectangle(0, 0, btn.Width, btn.Height),
                        Color.Black,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
                // Draw standard 3D border
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btn.Width, btn.Height), Border3DStyle.Raised);
            };

            return btn;
        }

        // Draws the chunky 3D raised border around the entire application
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ControlPaint.DrawBorder3D(e.Graphics, this.ClientRectangle, Border3DStyle.Raised);
        }

        // Allows you to click and drag the custom title bar to move the window
        private void CustomTitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && this.WindowState != FormWindowState.Maximized)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        // --- THE MAGIC RESIZE OVERRIDE ---
        // Intercepts Windows messages to allow resizing on our borderless form
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x84) // WM_NCHITTEST
            {
                // Get the mouse position
                int x = unchecked((short)m.LParam);
                int y = unchecked((short)((uint)m.LParam >> 16));
                Point pos = this.PointToClient(new Point(x, y));

                int resizeBorder = 5; // How many pixels from the edge you can click to resize

                if (pos.X <= resizeBorder && pos.Y <= resizeBorder)
                    m.Result = (IntPtr)13; // Top-Left
                else if (pos.X >= this.Width - resizeBorder && pos.Y <= resizeBorder)
                    m.Result = (IntPtr)14; // Top-Right
                else if (pos.X <= resizeBorder && pos.Y >= this.Height - resizeBorder)
                    m.Result = (IntPtr)16; // Bottom-Left
                else if (pos.X >= this.Width - resizeBorder && pos.Y >= this.Height - resizeBorder)
                    m.Result = (IntPtr)17; // Bottom-Right
                else if (pos.X <= resizeBorder)
                    m.Result = (IntPtr)10; // Left
                else if (pos.X >= this.Width - resizeBorder)
                    m.Result = (IntPtr)11; // Right
                else if (pos.Y <= resizeBorder)
                    m.Result = (IntPtr)12; // Top
                else if (pos.Y >= this.Height - resizeBorder)
                    m.Result = (IntPtr)15; // Bottom
            }
        }
    }
}