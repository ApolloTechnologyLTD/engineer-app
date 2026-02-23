using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Microsoft.Win32;
using System.Net;
using System.Net.Sockets;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace Apollo_Technology_Engineer_Application
{
    // =========================================================================
    // 1. MAIN APPLICATION WINDOW (MUST BE FIRST CLASS TO PREVENT RESOURCE CRASH)
    // =========================================================================
    public partial class Form1 : Form
    {
        private const string RepoOwner = "ApolloTechnologyLTD";
        private const string RepoName = "engineer-app";
        private const string CurrentVersion = "v1.1.2";

        public const string GoogleClientId = "551734036390-k39ivhe5mhfnj6ada7lv7p6edj1ct753.apps.googleusercontent.com";
        public const string GoogleClientSecret = "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";

        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        // Unified Layout Components
        private TableLayoutPanel mainLayout;
        private Panel leftPanel;
        private Panel workAreaContainer;
        private Panel topInfoPanel;
        private Label lblGreeting;
        private Panel whiteBoard;
        private Label lblSysInfo;
        private Button btnHelp;

        // Browser Components
        private Panel rightPanel;
        private Panel browserContainer;
        private Panel browserInner;
        private WebView2 webView;

        // Menu & Title
        private Panel titleBar;
        private Panel controlBox;
        private Label titleText;
        private Button btnMin;
        private Button btnMax;
        private Button btnClose;
        private MenuStrip menuStrip;
        private List<Button> actionButtons = new List<Button>();

        public Form1()
        {
            // FORCE LOGIN SCREEN BEFORE APP INITIALIZES
            using (var loginForm = new LoginForm())
            {
                if (loginForm.ShowDialog() != DialogResult.OK)
                {
                    Environment.Exit(0);
                }
            }

            SetBrowserFeatureControl();
            InitializeComponent();

            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.StartPosition = FormStartPosition.Manual;

            SetupTrueWin95Theme();
            InitializeWebView();
            LoadSystemInformationAsync();

            Win95Theme.OnThemeChanged += ApplyTheme;
            ApplyTheme();
        }

        private void SetBrowserFeatureControl()
        {
            var fileName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
            {
                key.SetValue(fileName, 11001, RegistryValueKind.DWord);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Rectangle screen = Screen.FromControl(this).WorkingArea;
            int startX = screen.Left + (screen.Width - this.Width) / 2;
            int startY = screen.Top + (screen.Height - this.Height) / 2;
            this.Location = new Point(startX, startY);
        }

        private async void LoadSystemInformationAsync()
        {
            lblSysInfo.Text = "Loading system diagnostics...";

            string hostName = Environment.MachineName;
            string cpu = "Unknown";
            try
            {
                cpu = Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", "Unknown CPU")?.ToString();
            }
            catch { }

            string gpu = SystemInfoHelper.GetGPUName();
            string ram = SystemInfoHelper.GetTotalPhysicalMemory();
            string localIp = SystemInfoHelper.GetLocalIPv4();
            string pubIp = "Detecting...";

            Action updateLabel = () =>
            {
                lblSysInfo.Text = $"Hostname: {hostName}\nCPU: {cpu}\nGPU: {gpu}\nRAM: {ram}\nIPv4 Address: {localIp}\nPublic IP: {pubIp}";
            };

            if (this.InvokeRequired) this.Invoke(updateLabel); else updateLabel();

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    pubIp = await client.GetStringAsync("https://api.ipify.org");
                }
            }
            catch
            {
                pubIp = "Unavailable";
            }

            if (this.InvokeRequired) this.Invoke(updateLabel); else updateLabel();
        }

        private async void InitializeWebView()
        {
            var env = await CoreWebView2Environment.CreateAsync(null, AppPaths.AppDataFolder);
            await webView.EnsureCoreWebView2Async(env);

            string mobileUserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1";
            webView.CoreWebView2.Settings.UserAgent = mobileUserAgent;
            webView.CoreWebView2.Settings.IsZoomControlEnabled = false;

            webView.Source = new Uri("https://kb.apollotechnology.co.uk/");
        }

        private void SetupTrueWin95Theme()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(1600, 650);
            this.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Regular);
            this.Padding = new Padding(4, 4, 4, 16);

            // =========================================================
            // 1. TOP MENU BARS
            // =========================================================
            titleBar = new Panel();
            titleBar.Height = 18;
            titleBar.Dock = DockStyle.Top;
            titleBar.MouseDown += CustomTitleBar_MouseDown;

            titleText = new Label();
            titleText.Text = "Apollo Technology - Technical Engineer Application";
            titleText.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Bold);
            titleText.AutoSize = false;
            titleText.Size = new Size(400, 15);
            titleText.Location = new Point(2, 2);
            titleText.TextAlign = ContentAlignment.MiddleLeft;
            titleText.MouseDown += CustomTitleBar_MouseDown;

            controlBox = new Panel();
            controlBox.Size = new Size(54, 18);
            controlBox.Dock = DockStyle.Right;

            btnMin = CreateTitleButton("0", new Point(2, 2));
            btnMin.Click += (s, e) => { this.WindowState = FormWindowState.Minimized; };

            btnMax = CreateTitleButton("1", new Point(18, 2));
            btnMax.Click += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Maximized)
                    this.WindowState = FormWindowState.Normal;
                else
                    this.WindowState = FormWindowState.Maximized;
                btnMax.Invalidate();
            };
            btnMax.Paint += (s, e) =>
            {
                e.Graphics.Clear(Win95Theme.Background);
                string iconChar = this.WindowState == FormWindowState.Maximized ? "2" : "1";
                using (Font marlett = new Font("Marlett", 8.25f))
                {
                    TextRenderer.DrawText(e.Graphics, iconChar, marlett, new Rectangle(0, 0, btnMax.Width, btnMax.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btnMax.Width, btnMax.Height), Border3DStyle.Raised);
            };

            btnClose = CreateTitleButton("r", new Point(36, 2));
            btnClose.Click += (s, e) => { Application.Exit(); };

            controlBox.Controls.Add(btnMin);
            controlBox.Controls.Add(btnMax);
            controlBox.Controls.Add(btnClose);
            titleBar.Controls.Add(titleText);
            titleBar.Controls.Add(controlBox);

            menuStrip = new MenuStrip();
            menuStrip.RenderMode = ToolStripRenderMode.System;
            menuStrip.Dock = DockStyle.Top;
            menuStrip.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Regular);

            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            ToolStripMenuItem logoutItem = new ToolStripMenuItem("Logout");
            logoutItem.Click += (s, e) =>
            {
                if (MessageBox.Show("Are you sure you want to log out?", "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    try
                    {
                        if (File.Exists(AppPaths.SmtpConfig)) File.Delete(AppPaths.SmtpConfig);
                        if (Directory.Exists(AppPaths.TokenFolder)) Directory.Delete(AppPaths.TokenFolder, true);
                    }
                    catch { }
                    Application.Restart();
                    Environment.Exit(0);
                }
            };
            fileMenu.DropDownItems.Add(logoutItem);
            menuStrip.Items.Add(fileMenu);

            ToolStripMenuItem emailReportItem = new ToolStripMenuItem("Email Reports");
            emailReportItem.Click += (s, e) => { new EmailReportForm().ShowDialog(); };
            menuStrip.Items.Add(emailReportItem);

            ToolStripMenuItem optionsMenu = new ToolStripMenuItem("Options");
            ToolStripMenuItem themeItem = new ToolStripMenuItem("Toggle Dark Mode");
            themeItem.Click += (s, e) => { Win95Theme.ToggleTheme(); };
            ToolStripMenuItem emailItem = new ToolStripMenuItem("Email Settings");
            emailItem.Click += (s, e) => { new SmtpSettingsForm().ShowDialog(); };
            optionsMenu.DropDownItems.Add(themeItem);
            optionsMenu.DropDownItems.Add(emailItem);
            menuStrip.Items.Add(optionsMenu);

            ToolStripMenuItem helpMenu = new ToolStripMenuItem("Help");
            ToolStripMenuItem bugItem = new ToolStripMenuItem("Bug report");
            bugItem.Click += (s, e) => { Process.Start(new ProcessStartInfo { FileName = "https://forms.gle/rDxseGaWDFda5Cft7", UseShellExecute = true }); };
            ToolStripMenuItem updateItem = new ToolStripMenuItem("Check for updates");
            updateItem.Click += async (s, e) => { await CheckForUpdatesAsync(); };
            ToolStripMenuItem aboutItem = new ToolStripMenuItem("About");
            aboutItem.Click += (s, e) => { MessageBox.Show($"Apollo Technology Engineer Application\nVersion {CurrentVersion}\n\nCopyright © {DateTime.Now.Year} Apollo Technology.", "About", MessageBoxButtons.OK, MessageBoxIcon.Information); };
            helpMenu.DropDownItems.Add(bugItem);
            helpMenu.DropDownItems.Add(updateItem);
            helpMenu.DropDownItems.Add(new ToolStripSeparator());
            helpMenu.DropDownItems.Add(aboutItem);
            menuStrip.Items.Add(helpMenu);

            // =========================================================
            // 2. UNIFIED TABLE LAYOUT 
            // =========================================================
            mainLayout = new TableLayoutPanel();
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.ColumnCount = 3;
            mainLayout.RowCount = 1;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 5f));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 450f));

            // =========================================================
            // 3. LEFT APP PANEL & GREETING
            // =========================================================
            leftPanel = new Panel();
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.Margin = new Padding(0);

            workAreaContainer = new Panel();
            workAreaContainer.Dock = DockStyle.Fill;
            workAreaContainer.Padding = new Padding(8, 0, 8, 8);

            topInfoPanel = new Panel();
            topInfoPanel.Dock = DockStyle.Top;
            topInfoPanel.Height = 25;

            lblGreeting = new Label();
            int hour = DateTime.Now.Hour;
            string greeting = "Good Evening";
            if (hour >= 0 && hour < 12) greeting = "Good Morning";
            else if (hour >= 12 && hour < 17) greeting = "Good Afternoon";

            lblGreeting.Text = $"{greeting} {SessionInfo.UserName}";
            lblGreeting.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Bold);
            lblGreeting.AutoSize = true;
            lblGreeting.Dock = DockStyle.Right;
            lblGreeting.TextAlign = ContentAlignment.MiddleRight;
            lblGreeting.Padding = new Padding(0, 5, 0, 0);

            topInfoPanel.Controls.Add(lblGreeting);

            whiteBoard = new Panel();
            whiteBoard.Dock = DockStyle.Fill;
            whiteBoard.BorderStyle = BorderStyle.Fixed3D;

            Button btnDiagnostic = CreateMenuButton("Run System Health Check", new Point(20, 20));
            btnDiagnostic.Click += (s, e) =>
            {
                if (webView != null && webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.Navigate("https://kb.apollotechnology.co.uk/en/windows-health-check");
                }

                DialogResult dialogResult = MessageBox.Show(
                    "Have you read the relevant article on the Knowledge base for this?",
                    "Knowledge Base Check",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (dialogResult == DialogResult.Yes)
                {
                    if (webView != null && webView.CoreWebView2 != null)
                    {
                        webView.CoreWebView2.Navigate("https://kb.apollotechnology.co.uk/");
                    }

                    string psCmd = $"$scriptPath = '{AppPaths.HealthCheckScript}'; iwr https://apollotech.short.gy/health_check -OutFile $scriptPath; & $scriptPath";
                    new RetroTerminalForm(psCmd).Show();
                }
                else
                {
                    MessageBox.Show("Please review the Knowledge Base article in the side panel before running this diagnostic.", "Required Reading", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            Button btnInternetCheck = CreateMenuButton("Check internet access", new Point(20, 60));
            btnInternetCheck.Click += (s, e) =>
            {
                new DualRetroTerminalForm("ping google.com -n 6", "ping 1.1.1.1 -n 6").Show();
            };

            Button btnEngage = CreateMenuButton("Engage Main Drive", new Point(20, 100));

            // Setup System Specs Label (Position calculated dynamically below)
            lblSysInfo = new Label();
            lblSysInfo.AutoSize = true;
            lblSysInfo.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Regular);
            lblSysInfo.ForeColor = Color.Gray;
            lblSysInfo.TextAlign = ContentAlignment.BottomRight; // Right-aligns the multi-line text block

            // Setup Help Chat Button (Made perfectly round)
            btnHelp = new Button();
            btnHelp.Size = new Size(48, 48);
            btnHelp.Image = RetroIcons.GetChatIcon(); // Draws centered offset icon
            btnHelp.ImageAlign = ContentAlignment.MiddleCenter;
            btnHelp.FlatStyle = FlatStyle.Flat;
            btnHelp.FlatAppearance.BorderSize = 0;

            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, 48, 48);
            btnHelp.Region = new Region(path);

            btnHelp.Click += (s, e) =>
            {
                new HelpEmailForm().ShowDialog();
            };
            actionButtons.Add(btnHelp);

            // Dynamic layout logic: Locks the info 5px left of the button, sharing the bottom baseline
            Action alignSysInfo = () =>
            {
                if (whiteBoard == null || btnHelp == null || lblSysInfo == null) return;
                btnHelp.Location = new Point(whiteBoard.Width - 65, whiteBoard.Height - 65);
                lblSysInfo.Location = new Point(btnHelp.Left - lblSysInfo.Width - 5, btnHelp.Bottom - lblSysInfo.Height);
            };

            whiteBoard.Resize += (s, e) => alignSysInfo();
            lblSysInfo.TextChanged += (s, e) => alignSysInfo();

            whiteBoard.Controls.Add(btnDiagnostic);
            whiteBoard.Controls.Add(btnInternetCheck);
            whiteBoard.Controls.Add(btnEngage);
            whiteBoard.Controls.Add(lblSysInfo);
            whiteBoard.Controls.Add(btnHelp);

            workAreaContainer.Controls.Add(whiteBoard);
            workAreaContainer.Controls.Add(topInfoPanel);

            leftPanel.Controls.Add(workAreaContainer);

            // =========================================================
            // 4. RIGHT KB PANEL (No Browser Controls)
            // =========================================================
            rightPanel = new Panel();
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.Margin = new Padding(0);

            browserContainer = new Panel();
            browserContainer.Dock = DockStyle.Fill;
            browserContainer.Padding = new Padding(2, 25, 2, 5);

            browserInner = new Panel();
            browserInner.Dock = DockStyle.Fill;
            browserInner.BorderStyle = BorderStyle.Fixed3D;

            webView = new WebView2();
            webView.Dock = DockStyle.Fill;

            browserInner.Controls.Add(webView);
            browserContainer.Controls.Add(browserInner);
            rightPanel.Controls.Add(browserContainer);

            mainLayout.Controls.Add(leftPanel, 0, 0);
            mainLayout.Controls.Add(rightPanel, 2, 0);

            this.Controls.Add(mainLayout);
            this.Controls.Add(menuStrip);
            this.Controls.Add(titleBar);
            this.MainMenuStrip = menuStrip;
        }
        private async Task CheckForUpdatesAsync()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Apollo-Engineer-App");
                    var response = await client.GetStringAsync($"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest");

                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        string latestVersion = doc.RootElement.GetProperty("tag_name").GetString();
                        string htmlUrl = doc.RootElement.GetProperty("html_url").GetString();

                        if (latestVersion != CurrentVersion)
                        {
                            DialogResult result = MessageBox.Show($"New version {latestVersion} available. Download?", "Update", MessageBoxButtons.YesNo);
                            if (result == DialogResult.Yes)
                            {
                                Process.Start(new ProcessStartInfo { FileName = htmlUrl, UseShellExecute = true });
                            }
                        }
                        else
                        {
                            MessageBox.Show("Up to date.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Update check failed: {ex.Message}");
            }
        }

        private Button CreateMenuButton(string text, Point loc)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = loc;
            btn.Size = new Size(160, 30);
            btn.FlatStyle = FlatStyle.Standard;
            btn.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Regular);
            actionButtons.Add(btn);
            return btn;
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
                e.Graphics.Clear(Win95Theme.Background);
                using (Font marlett = new Font("Marlett", 8.25f))
                {
                    TextRenderer.DrawText(e.Graphics, marlettChar, marlett, new Rectangle(0, 0, btn.Width, btn.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btn.Width, btn.Height), Border3DStyle.Raised);
            };
            return btn;
        }

        private void ApplyTheme()
        {
            this.BackColor = Win95Theme.Background;
            mainLayout.BackColor = Win95Theme.Background;
            workAreaContainer.BackColor = Win95Theme.Background;
            topInfoPanel.BackColor = Win95Theme.Background;
            browserContainer.BackColor = Win95Theme.Background;
            whiteBoard.BackColor = Win95Theme.WindowBackground;

            lblGreeting.ForeColor = Win95Theme.Text;

            foreach (var btn in actionButtons)
            {
                btn.BackColor = Win95Theme.ButtonFace;
                btn.ForeColor = Win95Theme.ButtonText;
            }

            menuStrip.BackColor = Win95Theme.Background;
            menuStrip.ForeColor = Win95Theme.Text;

            titleBar.BackColor = Win95Theme.TitleBar;
            titleText.ForeColor = Color.White;
            titleText.BackColor = Win95Theme.TitleBar;
            controlBox.BackColor = Win95Theme.TitleBar;

            btnMin.Invalidate();
            btnMax.Invalidate();
            btnClose.Invalidate();
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ControlPaint.DrawBorder3D(e.Graphics, this.ClientRectangle, Border3DStyle.Raised);
            if (this.WindowState != FormWindowState.Maximized)
            {
                ControlPaint.DrawSizeGrip(e.Graphics, Win95Theme.Background, new Rectangle(this.Width - 16, this.Height - 16, 16, 16));
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
                int b = 5;

                if (pos.X >= this.Width - 16 && pos.Y >= this.Height - 16) m.Result = (IntPtr)17;
                else if (pos.X <= b && pos.Y <= b) m.Result = (IntPtr)13;
                else if (pos.X >= this.Width - b && pos.Y <= b) m.Result = (IntPtr)14;
                else if (pos.X <= b && pos.Y >= this.Height - b) m.Result = (IntPtr)16;
                else if (pos.X <= b) m.Result = (IntPtr)10;
                else if (pos.X >= this.Width - b) m.Result = (IntPtr)11;
                else if (pos.Y <= b) m.Result = (IntPtr)12;
                else if (pos.Y >= this.Height - b) m.Result = (IntPtr)15;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                if (Directory.Exists(AppPaths.TokenFolder))
                {
                    Directory.Delete(AppPaths.TokenFolder, true);
                }
                if (File.Exists(AppPaths.SmtpConfig))
                {
                    File.Delete(AppPaths.SmtpConfig);
                }
            }
            catch { }
            base.OnFormClosing(e);
        }
    }

    // =========================================================================
    // 2. LOGIN FORM 
    // =========================================================================
    public class LoginForm : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private TextBox txtEmail;
        private TextBox txtPassword;
        private Button btnLogin;
        private Button btnGoogle;
        private Button btnCancel;
        private Button btnClose;
        private Panel titleBar;
        private Panel contentPanel;
        private Label titleText;
        private PictureBox picLogo;
        private Label lblWelcome;
        private Label lblFooter;
        private Label lblOr;

        public LoginForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(420, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Padding = new Padding(2);
            this.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Regular);

            SetupUI();

            Win95Theme.OnThemeChanged += ApplyTheme;
            ApplyTheme();
        }

        private void SetupUI()
        {
            titleBar = new Panel();
            titleBar.Height = 18;
            titleBar.Dock = DockStyle.Top;
            titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };

            titleText = new Label();
            titleText.Text = "Apollo Technology - Login";
            titleText.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Bold);
            titleText.AutoSize = false;
            titleText.Size = new Size(300, 15);
            titleText.Location = new Point(2, 2);
            titleText.TextAlign = ContentAlignment.MiddleLeft;

            btnClose = new Button();
            btnClose.Size = new Size(16, 14);
            btnClose.Location = new Point(this.Width - 20, 2);
            btnClose.FlatStyle = FlatStyle.Standard;
            btnClose.UseVisualStyleBackColor = false;
            btnClose.Paint += (s, e) =>
            {
                e.Graphics.Clear(Win95Theme.Background);
                using (Font marlett = new Font("Marlett", 8.25f))
                {
                    TextRenderer.DrawText(e.Graphics, "r", marlett, new Rectangle(0, 0, btnClose.Width, btnClose.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btnClose.Width, btnClose.Height), Border3DStyle.Raised);
            };
            btnClose.Click += (s, e) =>
            {
                this.Close();
            };

            titleBar.Controls.Add(titleText);
            titleBar.Controls.Add(btnClose);
            this.Controls.Add(titleBar);

            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.BorderStyle = BorderStyle.Fixed3D;
            contentPanel.BackColor = Color.White;
            contentPanel.Padding = new Padding(10, 25, 10, 10);

            picLogo = new PictureBox();
            picLogo.Size = new Size(200, 80);
            picLogo.Location = new Point((this.Width - 200) / 2, 20);
            picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picLogo.LoadAsync("https://raw.githubusercontent.com/ApolloTechnologyLTD/computer-health-check/main/Apollo%20Cropped.png");
            contentPanel.Controls.Add(picLogo);

            lblWelcome = new Label();
            lblWelcome.Text = "Welcome to the Apollo Technology Technical Engineer App";
            lblWelcome.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Bold);
            lblWelcome.AutoSize = false;
            lblWelcome.Size = new Size(380, 30);
            lblWelcome.Location = new Point((this.Width - 380) / 2, 110);
            lblWelcome.TextAlign = ContentAlignment.MiddleCenter;
            contentPanel.Controls.Add(lblWelcome);

            int currentY = 160;

            Label lblEmail = new Label();
            lblEmail.Text = "Email:";
            lblEmail.Location = new Point(40, currentY + 3);
            lblEmail.AutoSize = true;
            contentPanel.Controls.Add(lblEmail);

            txtEmail = new TextBox();
            txtEmail.Location = new Point(110, currentY);
            txtEmail.Width = 230;
            txtEmail.BorderStyle = BorderStyle.Fixed3D;
            contentPanel.Controls.Add(txtEmail);

            currentY += 35;

            Label lblPass = new Label();
            lblPass.Text = "Password:";
            lblPass.Location = new Point(40, currentY + 3);
            lblPass.AutoSize = true;
            contentPanel.Controls.Add(lblPass);

            txtPassword = new TextBox();
            txtPassword.Location = new Point(110, currentY);
            txtPassword.Width = 230;
            txtPassword.BorderStyle = BorderStyle.Fixed3D;
            txtPassword.PasswordChar = '*';
            contentPanel.Controls.Add(txtPassword);

            currentY += 45;

            btnLogin = new Button();
            btnLogin.Text = "Login";
            btnLogin.Location = new Point(110, currentY);
            btnLogin.Size = new Size(110, 30);
            btnLogin.FlatStyle = FlatStyle.Standard;
            btnLogin.Click += BtnLogin_Click;
            contentPanel.Controls.Add(btnLogin);

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(230, currentY);
            btnCancel.Size = new Size(110, 30);
            btnCancel.FlatStyle = FlatStyle.Standard;
            btnCancel.Click += (s, e) =>
            {
                this.Close();
            };
            contentPanel.Controls.Add(btnCancel);

            currentY += 45;

            lblOr = new Label();
            lblOr.Text = "---------------- OR ----------------";
            lblOr.AutoSize = false;
            lblOr.Size = new Size(this.Width, 15);
            lblOr.Location = new Point(0, currentY);
            lblOr.TextAlign = ContentAlignment.MiddleCenter;
            lblOr.ForeColor = Color.Gray;
            contentPanel.Controls.Add(lblOr);

            currentY += 25;

            btnGoogle = new Button();
            btnGoogle.Text = "Sign in with Google";
            btnGoogle.Location = new Point(110, currentY);
            btnGoogle.Size = new Size(230, 30);
            btnGoogle.FlatStyle = FlatStyle.Standard;
            btnGoogle.BackColor = Color.White;
            btnGoogle.Click += BtnGoogle_Click;
            contentPanel.Controls.Add(btnGoogle);

            lblFooter = new Label();
            lblFooter.Text = $"© {DateTime.Now.Year} Apollo Technology. All rights reserved | Created by Lewis Wiltshire";
            lblFooter.AutoSize = false;
            lblFooter.Height = 25;
            lblFooter.Dock = DockStyle.Bottom;
            lblFooter.TextAlign = ContentAlignment.MiddleCenter;
            lblFooter.ForeColor = Color.Gray;
            contentPanel.Controls.Add(lblFooter);

            this.Controls.Add(contentPanel);
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            btnLogin.Enabled = false;
            btnGoogle.Enabled = false;
            btnLogin.Text = "Authenticating...";

            bool success = await FirebaseAuthHelper.LoginWithEmailPasswordAsync(txtEmail.Text, txtPassword.Text);

            if (success)
            {
                SessionInfo.UserName = txtEmail.Text.Split('@')[0];

                string base64Password = "RG1lbGpXeklqQ0lLQktwcQ==";
                string decodedPassword = Encoding.UTF8.GetString(Convert.FromBase64String(base64Password));

                var config = new SmtpConfig();
                config.UseGoogleAuth = false;
                config.Server = "mail-eu.smtp2go.com";
                config.Port = 2525;
                config.Username = "smtp@apollotechnology.co.uk";
                config.EncryptedPassword = CryptoHelper.Encrypt(decodedPassword);

                config.FromAddress = "smtp@apollotechnology.co.uk";
                config.ToAddress = "support@apollotechnology.co.uk";

                string jsonConfig = JsonSerializer.Serialize(config);
                File.WriteAllText(AppPaths.SmtpConfig, jsonConfig);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid email or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnLogin.Enabled = true;
                btnGoogle.Enabled = true;
                btnLogin.Text = "Login";
            }
        }

        private async void BtnGoogle_Click(object sender, EventArgs e)
        {
            btnGoogle.Enabled = false;
            btnLogin.Enabled = false;
            btnGoogle.Text = "Waiting for browser...";

            try
            {
                var credential = await GoogleAuthHelper.AuthorizeAsync(Form1.GoogleClientId, Form1.GoogleClientSecret);
                var userInfo = await GoogleAuthHelper.GetUserInfoAsync(credential.Token.AccessToken);

                SessionInfo.UserName = userInfo.Name;

                var config = new SmtpConfig();
                config.UseGoogleAuth = true;
                config.Username = userInfo.Email;
                config.FromAddress = userInfo.Email;
                config.ToAddress = "support@apollotechnology.co.uk";

                string jsonConfig = JsonSerializer.Serialize(config);
                File.WriteAllText(AppPaths.SmtpConfig, jsonConfig);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Google Sign-In Failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnGoogle.Enabled = true;
                btnLogin.Enabled = true;
                btnGoogle.Text = "Sign in with Google";
            }
        }

        private void ApplyTheme()
        {
            this.BackColor = Win95Theme.Background;
            titleBar.BackColor = Win95Theme.TitleBar;
            titleText.ForeColor = Color.White;
            titleText.BackColor = Win95Theme.TitleBar;

            foreach (Control c in contentPanel.Controls)
            {
                if (c is Label lbl)
                {
                    if (lbl != lblOr && lbl != lblFooter)
                    {
                        lbl.ForeColor = Win95Theme.Text;
                    }
                }
                if (c is Button btn)
                {
                    btn.BackColor = Win95Theme.ButtonFace;
                    c.ForeColor = Win95Theme.ButtonText;
                }
            }

            contentPanel.BackColor = Win95Theme.WindowBackground;
            btnClose.Invalidate();
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ControlPaint.DrawBorder3D(e.Graphics, this.ClientRectangle, Border3DStyle.Raised);
        }
    }
    // =========================================================================
    // 3. HELP EMAIL FORM (ZOHO DESK SPECIFIC)
    // =========================================================================
    public class HelpEmailForm : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private TextBox txtTo;
        private TextBox txtSubject;
        private TextBox txtBody;
        private Button btnSend;
        private Button btnCancel;
        private Button btnClose;
        private Panel titleBar;
        private Panel contentPanel;
        private Label titleText;

        public HelpEmailForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Padding = new Padding(2);
            this.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Regular);

            SetupUI();
            Win95Theme.OnThemeChanged += ApplyTheme;
            ApplyTheme();
        }

        private void SetupUI()
        {
            titleBar = new Panel();
            titleBar.Height = 18;
            titleBar.Dock = DockStyle.Top;
            titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };

            titleText = new Label();
            titleText.Text = "Request Technical Support";
            titleText.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Bold);
            titleText.AutoSize = false;
            titleText.Size = new Size(300, 15);
            titleText.Location = new Point(2, 2);
            titleText.TextAlign = ContentAlignment.MiddleLeft;

            btnClose = new Button();
            btnClose.Size = new Size(16, 14);
            btnClose.Location = new Point(this.Width - 20, 2);
            btnClose.FlatStyle = FlatStyle.Standard;
            btnClose.UseVisualStyleBackColor = false;
            btnClose.Paint += (s, e) =>
            {
                e.Graphics.Clear(Win95Theme.Background);
                using (Font marlett = new Font("Marlett", 8.25f))
                {
                    TextRenderer.DrawText(e.Graphics, "r", marlett, new Rectangle(0, 0, btnClose.Width, btnClose.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btnClose.Width, btnClose.Height), Border3DStyle.Raised);
            };
            btnClose.Click += (s, e) => { this.Close(); };

            titleBar.Controls.Add(titleText);
            titleBar.Controls.Add(btnClose);
            this.Controls.Add(titleBar);

            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.BorderStyle = BorderStyle.Fixed3D;
            contentPanel.BackColor = Color.White;
            contentPanel.Padding = new Padding(10, 25, 10, 10);

            int currentY = 25;

            Label lblTo = new Label();
            lblTo.Text = "To:";
            lblTo.Location = new Point(10, currentY + 3);
            lblTo.AutoSize = true;
            contentPanel.Controls.Add(lblTo);

            txtTo = new TextBox();
            txtTo.Text = "lewiswiltshire@apollotechnology.co.uk.test-google-a.com";
            txtTo.ReadOnly = true;
            txtTo.Location = new Point(80, currentY);
            txtTo.Width = 380;
            txtTo.BorderStyle = BorderStyle.Fixed3D;
            contentPanel.Controls.Add(txtTo);

            currentY += 30;

            Label lblSubject = new Label();
            lblSubject.Text = "Subject:";
            lblSubject.Location = new Point(10, currentY + 3);
            lblSubject.AutoSize = true;
            contentPanel.Controls.Add(lblSubject);

            txtSubject = new TextBox();
            txtSubject.Text = "Engineer Support Request";
            txtSubject.Location = new Point(80, currentY);
            txtSubject.Width = 380;
            txtSubject.BorderStyle = BorderStyle.Fixed3D;
            contentPanel.Controls.Add(txtSubject);

            currentY += 35;

            Label lblMessage = new Label();
            lblMessage.Text = "Information:";
            lblMessage.Location = new Point(10, currentY);
            lblMessage.AutoSize = true;
            contentPanel.Controls.Add(lblMessage);

            currentY += 20;

            txtBody = new TextBox();
            txtBody.Multiline = true;
            txtBody.ScrollBars = ScrollBars.Vertical;
            txtBody.Location = new Point(10, currentY);
            txtBody.Width = 450;
            txtBody.Height = 190;
            txtBody.BorderStyle = BorderStyle.Fixed3D;
            txtBody.Text = "Please provide details about the issue you are experiencing:\r\n\r\n";
            contentPanel.Controls.Add(txtBody);

            currentY += 200;

            btnSend = new Button();
            btnSend.Text = "Send";
            btnSend.Location = new Point(136, currentY);
            btnSend.Size = new Size(100, 30);
            btnSend.FlatStyle = FlatStyle.Standard;
            btnSend.Click += BtnSend_Click;

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(256, currentY);
            btnCancel.Size = new Size(100, 30);
            btnCancel.FlatStyle = FlatStyle.Standard;
            btnCancel.Click += (s, e) => { this.Close(); };

            contentPanel.Controls.Add(btnSend);
            contentPanel.Controls.Add(btnCancel);

            this.Controls.Add(contentPanel);
        }

        private async void BtnSend_Click(object sender, EventArgs e)
        {
            if (!File.Exists(AppPaths.SmtpConfig))
            {
                MessageBox.Show("Please configure Email Settings in Options first.", "Config Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSend.Enabled = false;
            btnSend.Text = "Sending...";

            try
            {
                string jsonConfig = File.ReadAllText(AppPaths.SmtpConfig);
                var config = JsonSerializer.Deserialize<SmtpConfig>(jsonConfig);

                var msg = new MimeMessage();
                msg.From.Add(new MailboxAddress("Apollo Engineer", config.FromAddress));
                msg.To.Add(new MailboxAddress("Helpdesk", txtTo.Text));
                msg.Subject = txtSubject.Text;

                var builder = new BodyBuilder();
                builder.TextBody = txtBody.Text;
                msg.Body = builder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(config.UseGoogleAuth ? "smtp.gmail.com" : config.Server, config.UseGoogleAuth ? 587 : config.Port, SecureSocketOptions.StartTls);

                    if (config.UseGoogleAuth)
                    {
                        var credential = await GoogleAuthHelper.AuthorizeAsync(Form1.GoogleClientId, Form1.GoogleClientSecret);
                        var userInfo = await GoogleAuthHelper.GetUserInfoAsync(credential.Token.AccessToken);
                        var oauth2 = new SaslMechanismOAuth2(userInfo.Email, credential.Token.AccessToken);
                        await client.AuthenticateAsync(oauth2);
                    }
                    else
                    {
                        await client.AuthenticateAsync(config.Username, CryptoHelper.Decrypt(config.EncryptedPassword));
                    }

                    await client.SendAsync(msg);
                    await client.DisconnectAsync(true);

                    MessageBox.Show("Support request sent successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send email:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSend.Enabled = true;
                btnSend.Text = "Send";
            }
        }

        private void ApplyTheme()
        {
            this.BackColor = Win95Theme.Background;
            titleBar.BackColor = Win95Theme.TitleBar;
            titleText.ForeColor = Color.White;
            titleText.BackColor = Win95Theme.TitleBar;
            contentPanel.BackColor = Win95Theme.WindowBackground;

            foreach (Control c in contentPanel.Controls)
            {
                if (c is Label)
                {
                    c.ForeColor = Win95Theme.Text;
                }
                if (c is Button)
                {
                    c.BackColor = Win95Theme.ButtonFace;
                    c.ForeColor = Win95Theme.ButtonText;
                }
            }

            btnClose.Invalidate();
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ControlPaint.DrawBorder3D(e.Graphics, this.ClientRectangle, Border3DStyle.Raised);
        }
    }

    // =========================================================================
    // 3.5 EMAIL REPORT FORM
    // =========================================================================
    public class EmailReportForm : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private TextBox txtTo;
        private TextBox txtSubject;
        private TextBox txtBody;
        private TextBox txtAttachment;
        private Button btnBrowse;
        private Button btnSend;
        private Button btnCancel;
        private Button btnClose;
        private Panel titleBar;
        private Panel contentPanel;
        private Label titleText;

        public EmailReportForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Padding = new Padding(2);
            this.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Regular);

            SetupUI();

            Win95Theme.OnThemeChanged += ApplyTheme;
            ApplyTheme();
        }

        private void SetupUI()
        {
            titleBar = new Panel();
            titleBar.Height = 18;
            titleBar.Dock = DockStyle.Top;
            titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };

            titleText = new Label();
            titleText.Text = "Send Report to Helpdesk";
            titleText.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Bold);
            titleText.AutoSize = false;
            titleText.Size = new Size(300, 15);
            titleText.Location = new Point(2, 2);
            titleText.TextAlign = ContentAlignment.MiddleLeft;

            btnClose = new Button();
            btnClose.Size = new Size(16, 14);
            btnClose.Location = new Point(this.Width - 20, 2);
            btnClose.FlatStyle = FlatStyle.Standard;
            btnClose.UseVisualStyleBackColor = false;
            btnClose.Paint += (s, e) =>
            {
                e.Graphics.Clear(Win95Theme.Background);
                using (Font marlett = new Font("Marlett", 8.25f))
                {
                    TextRenderer.DrawText(e.Graphics, "r", marlett, new Rectangle(0, 0, btnClose.Width, btnClose.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btnClose.Width, btnClose.Height), Border3DStyle.Raised);
            };
            btnClose.Click += (s, e) =>
            {
                this.Close();
            };

            titleBar.Controls.Add(titleText);
            titleBar.Controls.Add(btnClose);
            this.Controls.Add(titleBar);

            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.BorderStyle = BorderStyle.Fixed3D;
            contentPanel.BackColor = Color.White;
            contentPanel.Padding = new Padding(10, 25, 10, 10);

            int currentY = 25;

            Label lblTo = new Label();
            lblTo.Text = "To:";
            lblTo.Location = new Point(10, currentY + 3);
            lblTo.AutoSize = true;
            contentPanel.Controls.Add(lblTo);

            txtTo = new TextBox();
            txtTo.Text = "support@apollotechnology.co.uk";
            txtTo.Location = new Point(80, currentY);
            txtTo.Width = 380;
            txtTo.BorderStyle = BorderStyle.Fixed3D;
            contentPanel.Controls.Add(txtTo);

            currentY += 30;

            Label lblSubject = new Label();
            lblSubject.Text = "Subject:";
            lblSubject.Location = new Point(10, currentY + 3);
            lblSubject.AutoSize = true;
            contentPanel.Controls.Add(lblSubject);

            txtSubject = new TextBox();
            txtSubject.Text = "Engineer Report Submission";
            txtSubject.Location = new Point(80, currentY);
            txtSubject.Width = 380;
            txtSubject.BorderStyle = BorderStyle.Fixed3D;
            contentPanel.Controls.Add(txtSubject);

            currentY += 30;

            Label lblAttach = new Label();
            lblAttach.Text = "Attach:";
            lblAttach.Location = new Point(10, currentY + 3);
            lblAttach.AutoSize = true;
            contentPanel.Controls.Add(lblAttach);

            txtAttachment = new TextBox();
            txtAttachment.ReadOnly = true;
            txtAttachment.Location = new Point(80, currentY);
            txtAttachment.Width = 290;
            txtAttachment.BorderStyle = BorderStyle.Fixed3D;
            contentPanel.Controls.Add(txtAttachment);

            btnBrowse = new Button();
            btnBrowse.Text = "Browse...";
            btnBrowse.Location = new Point(380, currentY - 1);
            btnBrowse.Size = new Size(80, 22);
            btnBrowse.FlatStyle = FlatStyle.Standard;
            btnBrowse.Click += BtnBrowse_Click;
            contentPanel.Controls.Add(btnBrowse);

            currentY += 35;

            Label lblMessage = new Label();
            lblMessage.Text = "Message:";
            lblMessage.Location = new Point(10, currentY);
            lblMessage.AutoSize = true;
            contentPanel.Controls.Add(lblMessage);

            currentY += 20;

            txtBody = new TextBox();
            txtBody.Multiline = true;
            txtBody.ScrollBars = ScrollBars.Vertical;
            txtBody.Location = new Point(10, currentY);
            txtBody.Width = 450;
            txtBody.Height = 220;
            txtBody.BorderStyle = BorderStyle.Fixed3D;
            txtBody.Text = "Hi Helpdesk,\r\n\r\nAttached to this email you will find a report made by a engineer at Apollo Technology, This email needs to be merged with the ticket request for the customer.\r\n\r\nKind Regards\r\nApollo Technical Team";
            contentPanel.Controls.Add(txtBody);

            currentY += 230;

            btnSend = new Button();
            btnSend.Text = "Send Report";
            btnSend.Location = new Point(136, currentY);
            btnSend.Size = new Size(100, 30);
            btnSend.FlatStyle = FlatStyle.Standard;
            btnSend.Click += BtnSend_Click;

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(256, currentY);
            btnCancel.Size = new Size(100, 30);
            btnCancel.FlatStyle = FlatStyle.Standard;
            btnCancel.Click += (s, e) =>
            {
                this.Close();
            };

            contentPanel.Controls.Add(btnSend);
            contentPanel.Controls.Add(btnCancel);

            this.Controls.Add(contentPanel);
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var rfd = new RetroFileDialog("C:\\"))
            {
                if (rfd.ShowDialog() == DialogResult.OK)
                {
                    txtAttachment.Text = rfd.SelectedFileName;
                }
            }
        }

        private async void BtnSend_Click(object sender, EventArgs e)
        {
            if (!File.Exists(AppPaths.SmtpConfig))
            {
                MessageBox.Show("Please configure Email Settings in Options first.", "Config Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtAttachment.Text) || !File.Exists(txtAttachment.Text))
            {
                MessageBox.Show("Please select a valid PDF file.", "Attachment Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSend.Enabled = false;
            btnSend.Text = "Sending...";

            try
            {
                string jsonConfig = File.ReadAllText(AppPaths.SmtpConfig);
                var config = JsonSerializer.Deserialize<SmtpConfig>(jsonConfig);

                var msg = new MimeMessage();
                msg.From.Add(new MailboxAddress("Apollo Engineer", config.FromAddress));
                msg.To.Add(new MailboxAddress("Helpdesk", txtTo.Text));
                msg.Subject = txtSubject.Text;

                var builder = new BodyBuilder();
                builder.TextBody = txtBody.Text;
                builder.Attachments.Add(txtAttachment.Text);
                msg.Body = builder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(config.UseGoogleAuth ? "smtp.gmail.com" : config.Server, config.UseGoogleAuth ? 587 : config.Port, SecureSocketOptions.StartTls);

                    if (config.UseGoogleAuth)
                    {
                        var credential = await GoogleAuthHelper.AuthorizeAsync(Form1.GoogleClientId, Form1.GoogleClientSecret);
                        var userInfo = await GoogleAuthHelper.GetUserInfoAsync(credential.Token.AccessToken);
                        var oauth2 = new SaslMechanismOAuth2(userInfo.Email, credential.Token.AccessToken);
                        await client.AuthenticateAsync(oauth2);
                    }
                    else
                    {
                        await client.AuthenticateAsync(config.Username, CryptoHelper.Decrypt(config.EncryptedPassword));
                    }

                    await client.SendAsync(msg);
                    await client.DisconnectAsync(true);

                    MessageBox.Show("Report sent successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send email:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSend.Enabled = true;
                btnSend.Text = "Send Report";
            }
        }

        private void ApplyTheme()
        {
            this.BackColor = Win95Theme.Background;
            titleBar.BackColor = Win95Theme.TitleBar;
            titleText.ForeColor = Color.White;
            titleText.BackColor = Win95Theme.TitleBar;
            contentPanel.BackColor = Win95Theme.WindowBackground;

            foreach (Control c in contentPanel.Controls)
            {
                if (c is Label)
                {
                    c.ForeColor = Win95Theme.Text;
                }
                if (c is Button)
                {
                    c.BackColor = Win95Theme.ButtonFace;
                    c.ForeColor = Win95Theme.ButtonText;
                }
            }

            btnClose.Invalidate();
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ControlPaint.DrawBorder3D(e.Graphics, this.ClientRectangle, Border3DStyle.Raised);
        }
    }

    // =========================================================================
    // 4. SMTP SETTINGS FORM
    // =========================================================================
    public class SmtpSettingsForm : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private TextBox txtServer;
        private TextBox txtPort;
        private TextBox txtUser;
        private TextBox txtPass;
        private TextBox txtFrom;
        private TextBox txtTo;

        private Label lblServer;
        private Label lblPort;
        private Label lblUser;
        private Label lblPass;
        private Label lblFrom;
        private Label lblTo;
        private Label lblOr;

        private CheckBox chkUseGoogle;
        private Button btnSave;
        private Button btnTest;
        private Button btnCancel;
        private Button btnGoogleLogin;
        private Button btnClose;
        private Panel titleBar;
        private Panel contentPanel;
        private Label titleText;

        public SmtpSettingsForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(450, 490);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Padding = new Padding(2);
            this.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Regular);

            SetupUI();
            LoadSettings();

            Win95Theme.OnThemeChanged += ApplyTheme;
            ApplyTheme();
        }

        private void SetupUI()
        {
            titleBar = new Panel();
            titleBar.Height = 18;
            titleBar.Dock = DockStyle.Top;
            titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };

            titleText = new Label();
            titleText.Text = "Email Configuration";
            titleText.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Bold);
            titleText.AutoSize = false;
            titleText.Size = new Size(300, 15);
            titleText.Location = new Point(2, 2);
            titleText.TextAlign = ContentAlignment.MiddleLeft;

            btnClose = new Button();
            btnClose.Size = new Size(16, 14);
            btnClose.Location = new Point(this.Width - 20, 2);
            btnClose.FlatStyle = FlatStyle.Standard;
            btnClose.UseVisualStyleBackColor = false;
            btnClose.Paint += (s, e) =>
            {
                e.Graphics.Clear(Win95Theme.Background);
                using (Font marlett = new Font("Marlett", 8.25f))
                {
                    TextRenderer.DrawText(e.Graphics, "r", marlett, new Rectangle(0, 0, btnClose.Width, btnClose.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btnClose.Width, btnClose.Height), Border3DStyle.Raised);
            };
            btnClose.Click += (s, e) =>
            {
                this.Close();
            };

            titleBar.Controls.Add(titleText);
            titleBar.Controls.Add(btnClose);
            this.Controls.Add(titleBar);

            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.BorderStyle = BorderStyle.Fixed3D;
            contentPanel.BackColor = Color.White;
            contentPanel.Padding = new Padding(10, 25, 10, 10);

            int currentY = 30;

            chkUseGoogle = new CheckBox();
            chkUseGoogle.Text = "Sign in with Google (Recommended)";
            chkUseGoogle.Location = new Point(10, currentY);
            chkUseGoogle.AutoSize = true;
            chkUseGoogle.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Bold);
            chkUseGoogle.CheckedChanged += (s, e) =>
            {
                ToggleGoogleMode(chkUseGoogle.Checked);
            };
            contentPanel.Controls.Add(chkUseGoogle);

            currentY += 35;

            btnGoogleLogin = new Button();
            btnGoogleLogin.Text = "Sign in with Google";
            btnGoogleLogin.Location = new Point(110, currentY);
            btnGoogleLogin.Size = new Size(200, 30);
            btnGoogleLogin.FlatStyle = FlatStyle.Standard;
            btnGoogleLogin.BackColor = Color.White;
            btnGoogleLogin.Click += BtnGoogleLogin_Click;
            contentPanel.Controls.Add(btnGoogleLogin);

            currentY += 45;

            lblOr = new Label();
            lblOr.Text = "------------ OR ------------";
            lblOr.AutoSize = false;
            lblOr.Size = new Size(this.Width, 15);
            lblOr.Location = new Point(0, currentY);
            lblOr.TextAlign = ContentAlignment.MiddleCenter;
            lblOr.ForeColor = Color.Gray;
            contentPanel.Controls.Add(lblOr);

            currentY += 25;

            lblServer = new Label();
            lblServer.Text = "SMTP Server:";
            lblServer.Location = new Point(10, currentY + 3);
            lblServer.AutoSize = true;
            contentPanel.Controls.Add(lblServer);

            txtServer = new TextBox();
            txtServer.Location = new Point(110, currentY);
            txtServer.Width = 300;
            txtServer.BorderStyle = BorderStyle.Fixed3D;
            txtServer.Text = "smtp.office365.com";
            contentPanel.Controls.Add(txtServer);

            currentY += 30;

            lblPort = new Label();
            lblPort.Text = "SMTP Port:";
            lblPort.Location = new Point(10, currentY + 3);
            lblPort.AutoSize = true;
            contentPanel.Controls.Add(lblPort);

            txtPort = new TextBox();
            txtPort.Location = new Point(110, currentY);
            txtPort.Width = 300;
            txtPort.BorderStyle = BorderStyle.Fixed3D;
            txtPort.Text = "587";
            contentPanel.Controls.Add(txtPort);

            currentY += 30;

            lblUser = new Label();
            lblUser.Text = "Username:";
            lblUser.Location = new Point(10, currentY + 3);
            lblUser.AutoSize = true;
            contentPanel.Controls.Add(lblUser);

            txtUser = new TextBox();
            txtUser.Location = new Point(110, currentY);
            txtUser.Width = 300;
            txtUser.BorderStyle = BorderStyle.Fixed3D;
            contentPanel.Controls.Add(txtUser);

            currentY += 30;

            lblPass = new Label();
            lblPass.Text = "Password:";
            lblPass.Location = new Point(10, currentY + 3);
            lblPass.AutoSize = true;
            contentPanel.Controls.Add(lblPass);

            txtPass = new TextBox();
            txtPass.Location = new Point(110, currentY);
            txtPass.Width = 300;
            txtPass.BorderStyle = BorderStyle.Fixed3D;
            txtPass.PasswordChar = '*';
            contentPanel.Controls.Add(txtPass);

            currentY += 30;

            lblFrom = new Label();
            lblFrom.Text = "From Email:";
            lblFrom.Location = new Point(10, currentY + 3);
            lblFrom.AutoSize = true;
            contentPanel.Controls.Add(lblFrom);

            txtFrom = new TextBox();
            txtFrom.Location = new Point(110, currentY);
            txtFrom.Width = 300;
            txtFrom.BorderStyle = BorderStyle.Fixed3D;
            contentPanel.Controls.Add(txtFrom);

            currentY += 30;

            lblTo = new Label();
            lblTo.Text = "To Email:";
            lblTo.Location = new Point(10, currentY + 3);
            lblTo.AutoSize = true;
            contentPanel.Controls.Add(lblTo);

            txtTo = new TextBox();
            txtTo.Location = new Point(110, currentY);
            txtTo.Width = 300;
            txtTo.BorderStyle = BorderStyle.Fixed3D;
            contentPanel.Controls.Add(txtTo);

            currentY += 30;

            btnSave = new Button();
            btnSave.Text = "Save";
            btnSave.Location = new Point(60, currentY);
            btnSave.Size = new Size(75, 23);
            btnSave.FlatStyle = FlatStyle.Standard;
            btnSave.Click += BtnSave_Click;

            btnTest = new Button();
            btnTest.Text = "Test";
            btnTest.Location = new Point(145, currentY);
            btnTest.Size = new Size(75, 23);
            btnTest.FlatStyle = FlatStyle.Standard;
            btnTest.Click += BtnTest_Click;

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(230, currentY);
            btnCancel.Size = new Size(75, 23);
            btnCancel.FlatStyle = FlatStyle.Standard;
            btnCancel.Click += (s, e) =>
            {
                this.Close();
            };

            contentPanel.Controls.Add(btnSave);
            contentPanel.Controls.Add(btnTest);
            contentPanel.Controls.Add(btnCancel);

            this.Controls.Add(contentPanel);
        }

        private void ToggleGoogleMode(bool isGoogle)
        {
            btnGoogleLogin.Enabled = isGoogle;
            txtPass.Enabled = !isGoogle;
            txtUser.Enabled = !isGoogle;

            lblServer.Visible = !isGoogle;
            txtServer.Visible = !isGoogle;
            lblPort.Visible = !isGoogle;
            txtPort.Visible = !isGoogle;

            if (isGoogle)
            {
                if (!txtUser.Text.Contains("@"))
                {
                    txtUser.Text = "(Sign in to fetch)";
                }
                txtPass.Text = "";
            }
            else
            {
                if (txtUser.Text.Contains("("))
                {
                    txtUser.Text = "";
                }
            }
        }

        private async void BtnGoogleLogin_Click(object sender, EventArgs e)
        {
            try
            {
                btnGoogleLogin.Text = "Waiting...";
                btnGoogleLogin.Enabled = false;

                var credential = await GoogleAuthHelper.AuthorizeAsync(Form1.GoogleClientId, Form1.GoogleClientSecret);
                var userInfo = await GoogleAuthHelper.GetUserInfoAsync(credential.Token.AccessToken);

                txtUser.Text = userInfo.Email;
                txtFrom.Text = userInfo.Email;
                MessageBox.Show($"Login Successful as {userInfo.Email}!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Login Failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGoogleLogin.Text = "Sign in with Google";
                btnGoogleLogin.Enabled = true;
            }
        }

        private async void BtnTest_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFrom.Text) || string.IsNullOrWhiteSpace(txtTo.Text))
            {
                MessageBox.Show("Please enter valid 'From' and 'To' addresses.", "Missing Info", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnTest.Enabled = false;
            btnTest.Text = "Testing...";

            try
            {
                var msg = new MimeMessage();
                msg.From.Add(new MailboxAddress("Apollo Tech", txtFrom.Text));
                msg.To.Add(new MailboxAddress("Recipient", txtTo.Text));
                msg.Subject = "Apollo Tech - SMTP Test";

                var textPart = new TextPart("plain");
                textPart.Text = "Test email from Apollo Engineer Application.";
                msg.Body = textPart;

                using (var client = new SmtpClient())
                {
                    string targetServer = chkUseGoogle.Checked ? "smtp.gmail.com" : txtServer.Text;
                    int targetPort = chkUseGoogle.Checked ? 587 : (int.TryParse(txtPort.Text, out int parsedPort) ? parsedPort : 587);

                    await client.ConnectAsync(targetServer, targetPort, SecureSocketOptions.StartTls);

                    if (chkUseGoogle.Checked)
                    {
                        var credential = await GoogleAuthHelper.AuthorizeAsync(Form1.GoogleClientId, Form1.GoogleClientSecret);
                        var userInfo = await GoogleAuthHelper.GetUserInfoAsync(credential.Token.AccessToken);
                        var oauth2 = new SaslMechanismOAuth2(userInfo.Email, credential.Token.AccessToken);
                        await client.AuthenticateAsync(oauth2);
                    }
                    else
                    {
                        await client.AuthenticateAsync(txtUser.Text, txtPass.Text);
                    }

                    await client.SendAsync(msg);
                    await client.DisconnectAsync(true);

                    MessageBox.Show("Test email sent successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed:\n{ex.Message}", "Test Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTest.Enabled = true;
                btnTest.Text = "Test";
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                var config = new SmtpConfig();
                config.UseGoogleAuth = chkUseGoogle.Checked;
                config.Server = string.IsNullOrWhiteSpace(txtServer.Text) ? "smtp.office365.com" : txtServer.Text;
                if (int.TryParse(txtPort.Text, out int parsedPort)) config.Port = parsedPort; else config.Port = 587;
                config.Username = txtUser.Text;
                config.EncryptedPassword = CryptoHelper.Encrypt(txtPass.Text); // Encrypts before writing to disk
                config.FromAddress = txtFrom.Text;
                config.ToAddress = txtTo.Text;

                string jsonConfig = JsonSerializer.Serialize(config);
                File.WriteAllText(AppPaths.SmtpConfig, jsonConfig);

                MessageBox.Show("Configuration saved.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving: " + ex.Message);
            }
        }

        private void LoadSettings()
        {
            if (File.Exists(AppPaths.SmtpConfig))
            {
                try
                {
                    string jsonConfig = File.ReadAllText(AppPaths.SmtpConfig);
                    var config = JsonSerializer.Deserialize<SmtpConfig>(jsonConfig);

                    if (config != null)
                    {
                        chkUseGoogle.Checked = config.UseGoogleAuth;
                        if (!string.IsNullOrEmpty(config.Server)) txtServer.Text = config.Server;
                        txtPort.Text = config.Port.ToString();

                        txtUser.Text = config.Username;
                        txtPass.Text = CryptoHelper.Decrypt(config.EncryptedPassword); // Decrypts from disk to display in memory
                        txtFrom.Text = config.FromAddress;
                        txtTo.Text = config.ToAddress;

                        ToggleGoogleMode(config.UseGoogleAuth);
                    }
                }
                catch { }
            }
        }

        private void ApplyTheme()
        {
            this.BackColor = Win95Theme.Background;
            titleBar.BackColor = Win95Theme.TitleBar;
            titleText.ForeColor = Color.White;
            titleText.BackColor = Win95Theme.TitleBar;

            foreach (Control c in contentPanel.Controls)
            {
                if (c is Label lbl && lbl != lblOr)
                {
                    lbl.ForeColor = Win95Theme.Text;
                }
                if (c is CheckBox)
                {
                    c.ForeColor = Win95Theme.Text;
                }
                if (c is Button)
                {
                    c.BackColor = Win95Theme.ButtonFace;
                    c.ForeColor = Win95Theme.ButtonText;
                }
            }

            contentPanel.BackColor = Win95Theme.WindowBackground;
            btnClose.Invalidate();
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ControlPaint.DrawBorder3D(e.Graphics, this.ClientRectangle, Border3DStyle.Raised);
        }
    }
    // =========================================================================
    // 5. RETRO TERMINAL - SINGLE VIEW
    // =========================================================================
    public class RetroTerminalForm : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private TextBox consoleBox;
        private string commandToRun;
        private Panel titleBar;
        private Panel controlBox;
        private Panel consoleContainer;
        private Label titleText;
        private Button btnMin;
        private Button btnMax;
        private Button btnClose;
        private Process _process;

        public RetroTerminalForm(string command)
        {
            this.commandToRun = command;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Regular);

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

            titleBar = new Panel();
            titleBar.Height = 18;
            titleBar.Dock = DockStyle.Top;
            titleBar.MouseDown += CustomTitleBar_MouseDown;

            titleText = new Label();
            titleText.Text = "MS-DOS Prompt";
            titleText.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Bold);
            titleText.AutoSize = false;
            titleText.Size = new Size(300, 15);
            titleText.Location = new Point(2, 2);
            titleText.TextAlign = ContentAlignment.MiddleLeft;
            titleText.MouseDown += CustomTitleBar_MouseDown;

            controlBox = new Panel();
            controlBox.Size = new Size(54, 18);
            controlBox.Dock = DockStyle.Right;

            btnMin = CreateTitleButton("0", new Point(2, 2));
            btnMin.Click += (s, e) => { this.WindowState = FormWindowState.Minimized; };

            btnMax = CreateTitleButton("1", new Point(18, 2));
            btnMax.Click += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Maximized)
                    this.WindowState = FormWindowState.Normal;
                else
                    this.WindowState = FormWindowState.Maximized;
                btnMax.Invalidate();
            };
            btnMax.Paint += (s, e) =>
            {
                e.Graphics.Clear(Win95Theme.Background);
                string iconChar = this.WindowState == FormWindowState.Maximized ? "2" : "1";
                using (Font f = new Font("Marlett", 8.25f))
                {
                    TextRenderer.DrawText(e.Graphics, iconChar, f, new Rectangle(0, 0, btnMax.Width, btnMax.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btnMax.Width, btnMax.Height), Border3DStyle.Raised);
            };

            btnClose = CreateTitleButton("r", new Point(36, 2));
            btnClose.Click += (s, e) => { this.Close(); };

            controlBox.Controls.Add(btnMin);
            controlBox.Controls.Add(btnMax);
            controlBox.Controls.Add(btnClose);
            titleBar.Controls.Add(titleText);
            titleBar.Controls.Add(controlBox);

            consoleBox = new TextBox();
            consoleBox.Multiline = true;
            consoleBox.ReadOnly = true;
            consoleBox.Dock = DockStyle.Fill;
            consoleBox.BackColor = Color.Black;
            consoleBox.ForeColor = Color.LightGray;
            consoleBox.Font = new Font("Consolas", 9.75f, FontStyle.Bold); // Mono-spaced font for retro terminal look
            consoleBox.BorderStyle = BorderStyle.None;
            consoleBox.ScrollBars = ScrollBars.Vertical;

            consoleContainer = new Panel();
            consoleContainer.Dock = DockStyle.Fill;
            consoleContainer.Padding = new Padding(2);
            consoleContainer.Controls.Add(consoleBox);

            this.Controls.Add(consoleContainer);
            this.Controls.Add(titleBar);
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
            Button btn = new Button();
            btn.Size = new Size(16, 14);
            btn.Location = location;
            btn.FlatStyle = FlatStyle.Standard;
            btn.UseVisualStyleBackColor = false;

            btn.Paint += (s, e) =>
            {
                e.Graphics.Clear(Win95Theme.Background);
                using (Font f = new Font("Marlett", 8.25f))
                {
                    TextRenderer.DrawText(e.Graphics, marlettChar, f, new Rectangle(0, 0, btn.Width, btn.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btn.Width, btn.Height), Border3DStyle.Raised);
            };
            return btn;
        }

        private void RunScriptInBackground()
        {
            consoleBox.AppendText("C:\\APOLLO_SYS> Initializing Diagnostics...\r\n");

            // Initializes a hidden PowerShell process that streams output back to our C# TextBox
            _process = new Process();
            _process.StartInfo.FileName = "powershell.exe";
            _process.StartInfo.Arguments = "-ExecutionPolicy Bypass -WindowStyle Hidden -NonInteractive -NoProfile -Command \"" + commandToRun + "\"";
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.CreateNoWindow = true;

            // Wire up event listeners to capture the output text live
            _process.OutputDataReceived += (s, a) => { if (a.Data != null) AppendToConsole(a.Data); };
            _process.ErrorDataReceived += (s, a) => { if (a.Data != null) AppendToConsole("ERROR: " + a.Data); };

            _process.EnableRaisingEvents = true;
            _process.Exited += (s, a) => { AppendToConsole("\r\nC:\\APOLLO_SYS> Task Completed."); };

            try
            {
                _process.Start();
                _process.BeginOutputReadLine();
                _process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                AppendToConsole("SYSTEM FAILURE: " + ex.Message);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Kill the hidden PS process if the user hits the X button early
            if (_process != null && !_process.HasExited)
            {
                try { _process.Kill(); } catch { }
            }
            base.OnFormClosing(e);
        }

        private void AppendToConsole(string text)
        {
            if (this.IsDisposed || !this.IsHandleCreated || consoleBox.IsDisposed) return;

            // Ensures UI updates happen on the main thread
            if (consoleBox.InvokeRequired)
            {
                try { consoleBox.Invoke(new Action<string>(AppendToConsole), new object[] { text }); } catch { }
                return;
            }

            try
            {
                consoleBox.AppendText(text + "\r\n");
                consoleBox.SelectionStart = consoleBox.Text.Length;
                consoleBox.ScrollToCaret(); // Auto-scroll to bottom
            }
            catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ControlPaint.DrawBorder3D(e.Graphics, this.ClientRectangle, Border3DStyle.Raised);
            if (this.WindowState != FormWindowState.Maximized)
            {
                ControlPaint.DrawSizeGrip(e.Graphics, Win95Theme.Background, new Rectangle(this.Width - 16, this.Height - 16, 16, 16));
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
                int b = 5;

                if (pos.X >= this.Width - 16 && pos.Y >= this.Height - 16) m.Result = (IntPtr)17;
                else if (pos.X <= b && pos.Y <= b) m.Result = (IntPtr)13;
                else if (pos.X >= this.Width - b && pos.Y <= b) m.Result = (IntPtr)14;
                else if (pos.X <= b && pos.Y >= this.Height - b) m.Result = (IntPtr)16;
                else if (pos.X <= b) m.Result = (IntPtr)10;
                else if (pos.X >= this.Width - b) m.Result = (IntPtr)11;
                else if (pos.Y <= b) m.Result = (IntPtr)12;
                else if (pos.Y >= this.Height - b) m.Result = (IntPtr)15;
            }
        }
    }

    // =========================================================================
    // 6. RETRO TERMINAL - DUAL SPLIT VIEW (For Double Ping)
    // =========================================================================
    public class DualRetroTerminalForm : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private TextBox leftBox;
        private TextBox rightBox;
        private string commandLeft;
        private string commandRight;
        private Panel titleBar;
        private Panel controlBox;
        private Label titleText;
        private Button btnMin;
        private Button btnMax;
        private Button btnClose;
        private TableLayoutPanel splitPanel;
        private Process _pLeft;
        private Process _pRight;

        public DualRetroTerminalForm(string cmdLeft, string cmdRight)
        {
            this.commandLeft = cmdLeft;
            this.commandRight = cmdRight;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.ResizeRedraw, true);
            this.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Regular);

            SetupTerminalUI();
            RunProcess(commandLeft, leftBox, true);
            RunProcess(commandRight, rightBox, false);

            Win95Theme.OnThemeChanged += ApplyTheme;
            ApplyTheme();
        }

        private void SetupTerminalUI()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(1000, 450);
            this.Padding = new Padding(4, 4, 4, 16);
            this.StartPosition = FormStartPosition.CenterScreen;

            titleBar = new Panel();
            titleBar.Height = 18;
            titleBar.Dock = DockStyle.Top;
            titleBar.MouseDown += CustomTitleBar_MouseDown;

            titleText = new Label();
            titleText.Text = "MS-DOS Prompt - Dual Diagnostics";
            titleText.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Bold);
            titleText.AutoSize = false;
            titleText.Size = new Size(300, 15);
            titleText.Location = new Point(2, 2);
            titleText.TextAlign = ContentAlignment.MiddleLeft;
            titleText.MouseDown += CustomTitleBar_MouseDown;

            controlBox = new Panel();
            controlBox.Size = new Size(54, 18);
            controlBox.Dock = DockStyle.Right;

            btnMin = CreateTitleButton("0", new Point(2, 2));
            btnMin.Click += (s, e) => { this.WindowState = FormWindowState.Minimized; };

            btnMax = CreateTitleButton("1", new Point(18, 2));
            btnMax.Click += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Maximized)
                    this.WindowState = FormWindowState.Normal;
                else
                    this.WindowState = FormWindowState.Maximized;
                btnMax.Invalidate();
            };
            btnMax.Paint += (s, e) =>
            {
                e.Graphics.Clear(Win95Theme.Background);
                string iconChar = this.WindowState == FormWindowState.Maximized ? "2" : "1";
                using (Font f = new Font("Marlett", 8.25f))
                {
                    TextRenderer.DrawText(e.Graphics, iconChar, f, new Rectangle(0, 0, btnMax.Width, btnMax.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btnMax.Width, btnMax.Height), Border3DStyle.Raised);
            };

            btnClose = CreateTitleButton("r", new Point(36, 2));
            btnClose.Click += (s, e) => { this.Close(); };

            controlBox.Controls.Add(btnMin);
            controlBox.Controls.Add(btnMax);
            controlBox.Controls.Add(btnClose);
            titleBar.Controls.Add(titleText);
            titleBar.Controls.Add(controlBox);

            splitPanel = new TableLayoutPanel();
            splitPanel.Dock = DockStyle.Fill;
            splitPanel.ColumnCount = 2;
            splitPanel.RowCount = 1;
            splitPanel.Padding = new Padding(2);
            splitPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            splitPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            leftBox = new TextBox();
            leftBox.Multiline = true;
            leftBox.ReadOnly = true;
            leftBox.Dock = DockStyle.Fill;
            leftBox.BackColor = Color.Black;
            leftBox.ForeColor = Color.LightGray;
            leftBox.Font = new Font("Consolas", 9.75f, FontStyle.Bold);
            leftBox.BorderStyle = BorderStyle.None;
            leftBox.ScrollBars = ScrollBars.Vertical;
            leftBox.Margin = new Padding(0, 0, 1, 0);

            rightBox = new TextBox();
            rightBox.Multiline = true;
            rightBox.ReadOnly = true;
            rightBox.Dock = DockStyle.Fill;
            rightBox.BackColor = Color.Black;
            rightBox.ForeColor = Color.LightGray;
            rightBox.Font = new Font("Consolas", 9.75f, FontStyle.Bold);
            rightBox.BorderStyle = BorderStyle.None;
            rightBox.ScrollBars = ScrollBars.Vertical;
            rightBox.Margin = new Padding(1, 0, 0, 0);

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
            Button btn = new Button();
            btn.Size = new Size(16, 14);
            btn.Location = location;
            btn.FlatStyle = FlatStyle.Standard;
            btn.UseVisualStyleBackColor = false;

            btn.Paint += (s, e) =>
            {
                e.Graphics.Clear(Win95Theme.Background);
                using (Font f = new Font("Marlett", 8.25f))
                {
                    TextRenderer.DrawText(e.Graphics, marlettChar, f, new Rectangle(0, 0, btn.Width, btn.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btn.Width, btn.Height), Border3DStyle.Raised);
            };
            return btn;
        }

        private void RunProcess(string command, TextBox targetBox, bool isLeft)
        {
            targetBox.AppendText($"C:\\APOLLO_SYS> Executing: {command}\r\n\r\n");

            Process p = new Process();
            if (isLeft) _pLeft = p; else _pRight = p;

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

            try
            {
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                AppendToConsole(targetBox, "SYSTEM FAILURE: " + ex.Message);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_pLeft != null && !_pLeft.HasExited) { try { _pLeft.Kill(); } catch { } }
            if (_pRight != null && !_pRight.HasExited) { try { _pRight.Kill(); } catch { } }
            base.OnFormClosing(e);
        }

        private void AppendToConsole(TextBox box, string text)
        {
            if (this.IsDisposed || !this.IsHandleCreated || box.IsDisposed) return;
            if (box.InvokeRequired)
            {
                try { box.Invoke(new Action<TextBox, string>(AppendToConsole), new object[] { box, text }); } catch { }
                return;
            }
            try
            {
                box.AppendText(text + "\r\n");
                box.SelectionStart = box.Text.Length;
                box.ScrollToCaret();
            }
            catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ControlPaint.DrawBorder3D(e.Graphics, this.ClientRectangle, Border3DStyle.Raised);
            if (this.WindowState != FormWindowState.Maximized)
            {
                ControlPaint.DrawSizeGrip(e.Graphics, Win95Theme.Background, new Rectangle(this.Width - 16, this.Height - 16, 16, 16));
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
                int b = 5;

                if (pos.X >= this.Width - 16 && pos.Y >= this.Height - 16) m.Result = (IntPtr)17;
                else if (pos.X <= b && pos.Y <= b) m.Result = (IntPtr)13;
                else if (pos.X >= this.Width - b && pos.Y <= b) m.Result = (IntPtr)14;
                else if (pos.X <= b && pos.Y >= this.Height - b) m.Result = (IntPtr)16;
                else if (pos.X <= b) m.Result = (IntPtr)10;
                else if (pos.X >= this.Width - b) m.Result = (IntPtr)11;
                else if (pos.Y <= b) m.Result = (IntPtr)12;
                else if (pos.Y >= this.Height - b) m.Result = (IntPtr)15;
            }
        }
    }

    // =========================================================================
    // 7. RETRO FILE DIALOG 
    // =========================================================================
    public class RetroFileDialog : Form
    {
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessagePtr(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public string SelectedFileName { get; private set; }
        private string currentDirectory;

        private Panel titleBar;
        private Panel contentPanel;
        private Panel topBar;
        private Panel bottomPanel;
        private Panel listWrapper;
        private Label titleText;
        private Label statusLabel;
        private Button btnClose;
        private Button btnUp;
        private Button btnOpen;
        private Button btnCancel;
        private ComboBox cmbDrives;
        private ComboBox cmbType;
        private ListView lstFiles;
        private TextBox txtFileName;

        public RetroFileDialog(string initialPath = "C:\\")
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(540, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Padding = new Padding(4, 4, 4, 16);
            this.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Regular);

            if (Directory.Exists(initialPath)) currentDirectory = initialPath;
            else currentDirectory = "C:\\";

            SetupUI();
            Win95Theme.OnThemeChanged += ApplyTheme;
            ApplyTheme();

            LoadDrives();
            LoadDirectory(currentDirectory);
        }

        private void SetupUI()
        {
            titleBar = new Panel();
            titleBar.Height = 18;
            titleBar.Dock = DockStyle.Top;
            titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
                }
            };

            titleText = new Label();
            titleText.Text = "Open File";
            titleText.Font = new Font("MS Sans Serif", 8.25f, FontStyle.Bold);
            titleText.AutoSize = false;
            titleText.Size = new Size(300, 15);
            titleText.Location = new Point(2, 2);
            titleText.TextAlign = ContentAlignment.MiddleLeft;

            btnClose = new Button();
            btnClose.Size = new Size(16, 14);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.Location = new Point(514, 2);
            btnClose.FlatStyle = FlatStyle.Standard;
            btnClose.UseVisualStyleBackColor = false;
            btnClose.Paint += (s, e) =>
            {
                e.Graphics.Clear(Win95Theme.Background);
                using (Font f = new Font("Marlett", 8.25f))
                {
                    TextRenderer.DrawText(e.Graphics, "r", f, new Rectangle(0, 0, btnClose.Width, btnClose.Height), Win95Theme.Text, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
                ControlPaint.DrawBorder3D(e.Graphics, new Rectangle(0, 0, btnClose.Width, btnClose.Height), Border3DStyle.Raised);
            };
            btnClose.Click += (s, e) => { this.Close(); };

            titleBar.Controls.Add(titleText);
            titleBar.Controls.Add(btnClose);
            this.Controls.Add(titleBar);

            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.BorderStyle = BorderStyle.Fixed3D;
            contentPanel.Padding = new Padding(8);

            topBar = new Panel();
            topBar.Dock = DockStyle.Top;
            topBar.Height = 35;

            Label lblLookIn = new Label();
            lblLookIn.Text = "Look in:";
            lblLookIn.Location = new Point(0, 8);
            lblLookIn.AutoSize = true;

            cmbDrives = new ComboBox();
            // Populates local C: D: drives to browse
            cmbDrives.Location = new Point(55, 5);
            cmbDrives.Width = 370;
            cmbDrives.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmbDrives.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDrives.SelectedIndexChanged += CmbDrives_SelectedIndexChanged;

            btnUp = new Button();
            btnUp.Text = "Up";
            btnUp.Location = new Point(440, 4);
            btnUp.Size = new Size(50, 23);
            btnUp.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnUp.FlatStyle = FlatStyle.Standard;
            btnUp.Click += (s, e) =>
            {
                DirectoryInfo di = new DirectoryInfo(currentDirectory);
                if (di.Parent != null) LoadDirectory(di.Parent.FullName);
            };

            topBar.Controls.Add(lblLookIn);
            topBar.Controls.Add(cmbDrives);
            topBar.Controls.Add(btnUp);

            ImageList icons = new ImageList();
            icons.ImageSize = new Size(32, 42);
            icons.ColorDepth = ColorDepth.Depth32Bit;
            icons.Images.Add("folder", RetroIcons.GetFolderIcon());
            icons.Images.Add("file", RetroIcons.GetFileIcon());
            icons.Images.Add("drive", RetroIcons.GetDriveIcon());

            listWrapper = new Panel();
            listWrapper.Dock = DockStyle.Fill;
            listWrapper.BorderStyle = BorderStyle.Fixed3D;
            listWrapper.BackColor = Color.White;
            listWrapper.Padding = new Padding(10);

            lstFiles = new ListView();
            lstFiles.Dock = DockStyle.Fill;
            lstFiles.View = View.LargeIcon;
            lstFiles.LargeImageList = icons;
            lstFiles.BorderStyle = BorderStyle.None;
            lstFiles.HideSelection = false;
            lstFiles.MultiSelect = false;
            lstFiles.Alignment = ListViewAlignment.Default;
            lstFiles.AutoArrange = true;
            lstFiles.UseCompatibleStateImageBehavior = false;

            lstFiles.DoubleClick += LstFiles_DoubleClick;
            lstFiles.SelectedIndexChanged += LstFiles_SelectedIndexChanged;

            listWrapper.Controls.Add(lstFiles);

            // Magic Windows message to override default icon spacing inside a ListView
            var handle = lstFiles.Handle;
            int xSpacing = 95;
            int ySpacing = 100;
            IntPtr lParam = (IntPtr)((ySpacing << 16) | (xSpacing & 0xFFFF));
            SendMessagePtr(lstFiles.Handle, 0x1035, IntPtr.Zero, lParam);

            bottomPanel = new Panel();
            bottomPanel.Dock = DockStyle.Bottom;
            bottomPanel.Height = 90;

            Label lblFile = new Label();
            lblFile.Text = "File name:";
            lblFile.Location = new Point(0, 15);
            lblFile.AutoSize = true;

            txtFileName = new TextBox();
            txtFileName.Location = new Point(75, 12);
            txtFileName.Width = 350;
            txtFileName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtFileName.BorderStyle = BorderStyle.Fixed3D;

            Label lblType = new Label();
            lblType.Text = "Files of type:";
            lblType.Location = new Point(0, 45);
            lblType.AutoSize = true;

            cmbType = new ComboBox();
            cmbType.Location = new Point(75, 42);
            cmbType.Width = 350;
            cmbType.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.Items.Add("PDF Files (*.pdf)");
            cmbType.SelectedIndex = 0;

            btnOpen = new Button();
            btnOpen.Text = "Open";
            btnOpen.Location = new Point(440, 10);
            btnOpen.Size = new Size(70, 25);
            btnOpen.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOpen.FlatStyle = FlatStyle.Standard;
            btnOpen.Click += (s, e) => { PerformOpen(); };

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(440, 40);
            btnCancel.Size = new Size(70, 25);
            btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCancel.FlatStyle = FlatStyle.Standard;
            btnCancel.Click += (s, e) => { this.Close(); };

            statusLabel = new Label();
            statusLabel.Text = "0 object(s)";
            statusLabel.Dock = DockStyle.Bottom;
            statusLabel.BorderStyle = BorderStyle.Fixed3D;
            statusLabel.Height = 20;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;

            bottomPanel.Controls.Add(lblFile);
            bottomPanel.Controls.Add(txtFileName);
            bottomPanel.Controls.Add(lblType);
            bottomPanel.Controls.Add(cmbType);
            bottomPanel.Controls.Add(btnOpen);
            bottomPanel.Controls.Add(btnCancel);
            bottomPanel.Controls.Add(statusLabel);

            contentPanel.Controls.Add(topBar);
            contentPanel.Controls.Add(bottomPanel);
            contentPanel.Controls.Add(listWrapper);
            listWrapper.BringToFront();

            this.Controls.Add(contentPanel);
        }

        private void LoadDrives()
        {
            cmbDrives.Items.Clear();
            foreach (var d in DriveInfo.GetDrives())
            {
                if (d.IsReady) cmbDrives.Items.Add(d.Name);
            }
        }

        private void CmbDrives_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbDrives.SelectedItem != null)
            {
                LoadDirectory(cmbDrives.SelectedItem.ToString());
            }
        }

        private void LoadDirectory(string path)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                currentDirectory = di.FullName;
                string root = di.Root.Name;

                foreach (var item in cmbDrives.Items)
                {
                    if (item.ToString().StartsWith(root))
                    {
                        cmbDrives.SelectedIndexChanged -= CmbDrives_SelectedIndexChanged;
                        cmbDrives.SelectedItem = item;
                        cmbDrives.SelectedIndexChanged += CmbDrives_SelectedIndexChanged;
                        break;
                    }
                }

                lstFiles.BeginUpdate();
                lstFiles.Items.Clear();
                int itemCount = 0;

                if (di.Parent != null)
                {
                    lstFiles.Items.Add(new ListViewItem("[Up]", "folder"));
                }

                foreach (var dir in di.GetDirectories())
                {
                    if (!dir.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        lstFiles.Items.Add(new ListViewItem(dir.Name, "folder"));
                        itemCount++;
                    }
                }

                foreach (var file in di.GetFiles("*.pdf"))
                {
                    if (!file.Attributes.HasFlag(FileAttributes.Hidden))
                    {
                        lstFiles.Items.Add(new ListViewItem(file.Name, "file"));
                        itemCount++;
                    }
                }

                lstFiles.EndUpdate();
                statusLabel.Text = $"{itemCount} object(s)";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot access directory: " + ex.Message, "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DirectoryInfo di = new DirectoryInfo(currentDirectory);
                if (di.Parent != null) LoadDirectory(di.Parent.FullName);
            }
        }

        private void LstFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstFiles.SelectedItems.Count == 0) return;
            string selected = lstFiles.SelectedItems[0].Text;
            if (selected != "[Up]") txtFileName.Text = selected;
        }

        private void LstFiles_DoubleClick(object sender, EventArgs e)
        {
            if (lstFiles.SelectedItems.Count == 0) return;
            string selected = lstFiles.SelectedItems[0].Text;

            if (selected == "[Up]")
            {
                DirectoryInfo di = new DirectoryInfo(currentDirectory);
                if (di.Parent != null) LoadDirectory(di.Parent.FullName);
            }
            else if (lstFiles.SelectedItems[0].ImageKey == "folder")
            {
                LoadDirectory(Path.Combine(currentDirectory, selected));
            }
            else
            {
                txtFileName.Text = selected;
                PerformOpen();
            }
        }

        private void PerformOpen()
        {
            if (string.IsNullOrWhiteSpace(txtFileName.Text)) return;
            string fullPath = Path.Combine(currentDirectory, txtFileName.Text);

            if (File.Exists(fullPath))
            {
                SelectedFileName = fullPath;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("File not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyTheme()
        {
            this.BackColor = Win95Theme.Background;
            titleBar.BackColor = Win95Theme.TitleBar;
            titleText.ForeColor = Color.White;
            titleText.BackColor = Win95Theme.TitleBar;

            contentPanel.BackColor = Win95Theme.Background;
            bottomPanel.BackColor = Win95Theme.Background;
            topBar.BackColor = Win95Theme.Background;

            listWrapper.BackColor = Win95Theme.WindowBackground;
            lstFiles.BackColor = Win95Theme.WindowBackground;
            lstFiles.ForeColor = Win95Theme.WindowText;

            txtFileName.BackColor = Win95Theme.WindowBackground;
            txtFileName.ForeColor = Win95Theme.WindowText;

            statusLabel.BackColor = Win95Theme.Background;
            statusLabel.ForeColor = Win95Theme.Text;

            foreach (Control c in topBar.Controls) { if (c is Label) c.ForeColor = Win95Theme.Text; }
            foreach (Control c in bottomPanel.Controls)
            {
                if (c is Label) c.ForeColor = Win95Theme.Text;
                if (c is Button)
                {
                    c.BackColor = Win95Theme.ButtonFace;
                    c.ForeColor = Win95Theme.ButtonText;
                }
            }

            btnUp.BackColor = Win95Theme.ButtonFace;
            btnUp.ForeColor = Win95Theme.ButtonText;

            btnClose.Invalidate();
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            ControlPaint.DrawBorder3D(e.Graphics, this.ClientRectangle, Border3DStyle.Raised);
            ControlPaint.DrawSizeGrip(e.Graphics, Win95Theme.Background, new Rectangle(this.Width - 16, this.Height - 16, 16, 16));
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x84)
            {
                int x = unchecked((short)m.LParam);
                int y = unchecked((short)((uint)m.LParam >> 16));
                Point pos = this.PointToClient(new Point(x, y));
                int b = 5;

                if (pos.X >= this.Width - 16 && pos.Y >= this.Height - 16) m.Result = (IntPtr)17;
                else if (pos.X <= b && pos.Y <= b) m.Result = (IntPtr)13;
                else if (pos.X >= this.Width - b && pos.Y <= b) m.Result = (IntPtr)14;
                else if (pos.X <= b && pos.Y >= this.Height - b) m.Result = (IntPtr)16;
                else if (pos.X <= b) m.Result = (IntPtr)10;
                else if (pos.X >= this.Width - b) m.Result = (IntPtr)11;
                else if (pos.Y <= b) m.Result = (IntPtr)12;
                else if (pos.Y >= this.Height - b) m.Result = (IntPtr)15;
            }
        }
    }

    // =========================================================================
    // 8. SYSTEM HELPERS (HARDWARE SCANNER)
    // =========================================================================
    public static class SystemInfoHelper
    {
        // P/Invoke Struct to capture raw RAM values from the motherboard
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX() { this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)); }
        }

        // P/Invoke call to the core kernel32.dll for memory stats
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        public static string GetTotalPhysicalMemory()
        {
            try
            {
                MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    double gb = memStatus.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
                    return $"{Math.Round(gb, 2)} GB";
                }
            }
            catch { }
            return "Unknown";
        }

        // Looks through the network adapters for an active IPv4 address
        public static string GetLocalIPv4()
        {
            try
            {
                foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }
            return "Unknown";
        }

        // Checks Windows Assessment Tool registry keys to find the primary GPU
        public static string GetGPUName()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\WinSAT"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("PrimaryAdapterString");
                        if (val != null) return val.ToString();
                    }
                }

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("DriverDesc");
                        if (val != null) return val.ToString();
                    }
                }
            }
            catch { }
            return "Unknown";
        }
    }

    public class GoogleUserInfo
    {
        public string Email { get; set; }
        public string Name { get; set; }
    }

    public static class FirebaseAuthHelper
    {
        // Google Identity Toolkit API key
        public static string FirebaseApiKey = "AIzaSyAgX8g99f43ehbtPW9Bt7OT4C9nTRu-U0U";

        // Makes a REST POST request to Firebase to authenticate email/pass logins
        public static async Task<bool> LoginWithEmailPasswordAsync(string email, string password)
        {
            using (var client = new HttpClient())
            {
                var payload = new
                {
                    email = email,
                    password = password,
                    returnSecureToken = true
                };

                string json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={FirebaseApiKey}";

                var response = await client.PostAsync(url, content);
                return response.IsSuccessStatusCode;
            }
        }
    }

    public static class GoogleAuthHelper
    {
        // Pops open the local default browser to request Google App Auth permissions
        public static async Task<UserCredential> AuthorizeAsync(string clientId, string clientSecret)
        {
            string[] scopes = { "https://mail.google.com/", "https://www.googleapis.com/auth/userinfo.email", "https://www.googleapis.com/auth/userinfo.profile" };
            var secrets = new ClientSecrets();
            secrets.ClientId = clientId;
            secrets.ClientSecret = clientSecret;

            // Automatically saves the access token into the AppData token folder
            return await GoogleWebAuthorizationBroker.AuthorizeAsync(secrets, scopes, "user", CancellationToken.None, new FileDataStore(AppPaths.TokenFolder, true));
        }

        // Queries the Google API to get the user's display name and email address
        public static async Task<GoogleUserInfo> GetUserInfoAsync(string accessToken)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var json = await client.GetStringAsync("https://www.googleapis.com/oauth2/v2/userinfo");

                using (var doc = JsonDocument.Parse(json))
                {
                    string email = doc.RootElement.GetProperty("email").GetString();
                    string name = doc.RootElement.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : email.Split('@')[0];
                    return new GoogleUserInfo { Email = email, Name = name };
                }
            }
        }

        // Compatibility fallback for SmtpSettings fetching just the email
        public static async Task<string> GetUserEmailAsync(string accessToken)
        {
            var info = await GetUserInfoAsync(accessToken);
            return info.Email;
        }
    }

    public class SmtpConfig
    {
        public bool UseGoogleAuth { get; set; }
        public string Server { get; set; } = "smtp.office365.com";
        public int Port { get; set; } = 587;
        public string Username { get; set; }
        public string EncryptedPassword { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public bool EnableSsl { get; set; } = true;
    }

    // Windows DPAPI Encrypter for storing passwords safely on disk without hardcoding decryption keys
    public static class CryptoHelper
    {
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return "";
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(plainText);
                byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encrypted);
            }
            catch { return ""; }
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return "";
            try
            {
                byte[] data = Convert.FromBase64String(cipherText);
                byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch { return ""; }
        }
    }

    // Color definitions for Light/Dark mode
    public static class Win95Theme
    {
        public static bool IsDarkMode { get; private set; } = false;
        public static event Action OnThemeChanged;

        public static Color Background => IsDarkMode ? Color.FromArgb(50, 50, 50) : Color.FromArgb(192, 192, 192);
        public static Color Text => IsDarkMode ? Color.White : Color.Black;
        public static Color WindowBackground => IsDarkMode ? Color.FromArgb(30, 30, 30) : Color.White;
        public static Color WindowText => IsDarkMode ? Color.FromArgb(220, 220, 220) : Color.Black;
        public static Color TitleBar => Color.FromArgb(0, 0, 128);
        public static Color ButtonFace => IsDarkMode ? Color.FromArgb(70, 70, 70) : Color.FromArgb(192, 192, 192);
        public static Color ButtonText => IsDarkMode ? Color.White : Color.Black;

        public static void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
            OnThemeChanged?.Invoke();
        }
    }

    // =========================================================================
    // 9. RETRO ICON GENERATOR
    // =========================================================================
    // Generates completely custom pixel art natively using GDI+, so no .ico files are needed
    public static class RetroIcons
    {
        public static Bitmap GetFolderIcon()
        {
            string[] grid = new string[]
            {
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "....kkkkkkk.....................",
                "...kYwYYYYYk....................",
                "..kYwYYYYYYYk...................",
                "..kYwYYYYYYYkkkkkkkkkkkkkkkkkk..",
                "..kwwwwwwwwwwwwwwwwwwwwwwwwwwk..",
                "..kwyyyyyyyyyyyyyyyyyyyyyyyydk..",
                "..kwyyyyyyyyyyyyyyyyyyyyyyyydk..",
                "..kwyyyyyyyyyyyyyyyyyyyyyyyydk..",
                "..kwyyyyyyyyyyyyyyyyyyyyyyyydk..",
                "..kwyyyyyyyyyyyyyyyyyyyyyyyydk..",
                "..kwyyyyyyyyyyyyyyyyyyyyyyyydk..",
                "..kwyyyyyyyyyyyyyyyyyyyyyyyydk..",
                "..kwyyyyyyyyyyyyyyyyyyyyyyyydk..",
                "..kwyyyyyyyyyyyyyyyyyyyyyyyydk..",
                "..kwyyyyyyyyyyyyyyyyyyyyyyyydk..",
                "..kwyyyyyyyyyyyyyyyyyyyyyyyydk..",
                "..kwyyyyyyyyyyyyyyyyyyyyyyyydk..",
                "..kwyyyyyyyyyyyyyyyyyyyyyyyydk..",
                "..kwyyyyyyyyyyyyyyyyyyyyyyyydk..",
                "..kddddddddddddddddddddddddddk..",
                "..kkkkkkkkkkkkkkkkkkkkkkkkkkkk..",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................"
            };
            return GetIconFromGrid(grid, 10, 42);
        }

        public static Bitmap GetFileIcon()
        {
            string[] grid = new string[]
            {
                "................................",
                "................................",
                "......kkkkkkkkkkkkkkkkk.........",
                "......kwwwwwwwwwwwwwwwkk........",
                "......kwwwwwwwwwwwwwwwkgk.......",
                "......kwwwwwwwwwwwwwwwkggk......",
                "......kwwwwwwwwwwwwwwwkgggk.....",
                "......kwwwwwwwwwwwwwwwkkkkkk....",
                "......kwwwwwwwwwwwwwwwwwwwwk....",
                "......kwwwwwwwwwwwwwwwwwwwwk....",
                "......kwwkkkkkkkkkkkkkkkwwwk....",
                "......kwwkbbbbbbbbbbbbbkwwwk....",
                "......kwwkbbbbbbbbbbbbbkwwwk....",
                "......kwwkbbbbbbbbbbbbbkwwwk....",
                "......kwwkbbbbbbbbbbbbbkwwwk....",
                "......kwwkbbbbbbbbbbbbbkwwwk....",
                "......kwwkkkkkkkkkkkkkkkwwwk....",
                "......kwwwwwwwwwwwwwwwwwwwwk....",
                "......kwwwrwwwGwwwbwwwYwwwwk....",
                "......kwwwwwwwwwwwwwwwwwwwwk....",
                "......kwwrrrrGGGGbbbbYYYYwwk....",
                "......kwwrrrrGGGGbbbbYYYYwwk....",
                "......kwwrrrrGGGGbbbbYYYYwwk....",
                "......kwwwwwwwwwwwwwwwwwwwwk....",
                "......kwwwwwwwwwwwwwwwwwwwwk....",
                "......kwwwwwwwwwwwwwwwwwwwwk....",
                "......kwwwwwwwwwwwwwwwwwwwwk....",
                "......kkkkkkkkkkkkkkkkkkkkkk....",
                "................................",
                "................................",
                "................................",
                "................................"
            };
            return GetIconFromGrid(grid, 10, 42);
        }

        public static Bitmap GetDriveIcon()
        {
            string[] grid = new string[]
            {
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                ".......kkkkkkkkkkkkkkkkkk.......",
                "......kwwwwwwwwwwwwwwwwwwk......",
                ".....kwwggggggggggggggggggk.....",
                "....kwgggggggggggggggggggggk....",
                "...kwgggggggggggggggggggggggk...",
                "..kkkkkkkkkkkkkkkkkkkkkkkkkkkk..",
                "..kggggggggggggggggggggggggggk..",
                "..kggggggggggggggggggggggggggk..",
                "..kggggggggggggggggggggggggggk..",
                "..kggggggggggggggggggggggggggk..",
                "..kggggggggggggggggggggggggggk..",
                "..kggggggggggggggggggggggggggk..",
                "..kddddddddddddddddddddddddddk..",
                "..kddddddddddddddddddddddddddk..",
                "..kddkkkdddddddddddddddddddddk..",
                "..kddkekdddddddddddddddddddddk..",
                "..kddkkkdddddddddddddddddddddk..",
                "..kkkkkkkkkkkkkkkkkkkkkkkkkkkk..",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................"
            };
            return GetIconFromGrid(grid, 10, 42);
        }

        public static Bitmap GetChatIcon()
        {
            string[] grid = new string[]
            {
                "................................",
                "................................",
                "....kkkkkkkkkkkkkkkkkkkkkkkk....",
                "...kwwwwwwwwwwwwwwwwwwwwwwwwk...",
                "..kwwYwYwYwYwYwYwYwYwYwYwYwYwk..",
                "..kwYwYwYwYwYwYwYwYwYwYwYwYwwk..",
                "..kwwYwddddddddddddddddYwYwYwk..",
                "..kwYwYddddddddddddddddwYwYwwk..",
                "..kwwYwYwYwYwYwYwYwYwYwYwYwYwk..",
                "..kwYwYwYwYwYwYwYwYwYwYwYwYwwk..",
                "..kwwYwddddddddddddddddYwYwYwk..",
                "..kwYwYddddddddddddddddwYwYwwk..",
                "..kwwYwYwYwYwYwYwYwYwYwYwYwYwk..",
                "..kwYwYwYwYwYwYwYwYwYwYwYwYwwk..",
                "..kwwYwddddddddddddddddYwYwYwk..",
                "..kwYwYddddddddddddddddwYwYwwk..",
                "..kwwYwYwYwYwYwYwYwYwYwYwYwYwk..",
                "..kwYwYwYwYwYwYwYwYwYwYwYwYwwk..",
                "..kwwwwwwwwwwwwwwwwwwwwwwwwwwk..",
                "...kkkkkkkkkkkkkkkkkkkkwYwYwwk..",
                "......................kwwYwYwk..",
                "......................kwYwYwwk..",
                ".......................kwwYwk...",
                ".......................kwYwk....",
                "........................kkk.....",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................",
                "................................"
            };
            // Offset shifted to 3 down to ensure it sits perfectly in the center of the rounded button
            return GetIconFromGrid(grid, 3, 32);
        }

        private static Bitmap GetIconFromGrid(string[] grid, int offsetY = 0, int targetHeight = 32)
        {
            Bitmap bmp = new Bitmap(32, targetHeight);
            Dictionary<char, Color> pal = new Dictionary<char, Color>();
            pal.Add('.', Color.Transparent);
            pal.Add('k', Color.Black);
            pal.Add('w', Color.White);
            pal.Add('g', Color.LightGray);
            pal.Add('G', Color.FromArgb(0, 128, 0));
            pal.Add('d', Color.Gray);
            pal.Add('y', Color.FromArgb(255, 255, 128));
            pal.Add('Y', Color.FromArgb(255, 200, 0));
            pal.Add('b', Color.FromArgb(0, 0, 128));
            pal.Add('r', Color.Red);
            pal.Add('e', Color.Lime);

            // Scans the 32x32 string array and draws pixels safely within bounds
            for (int y = 0; y < 32; y++)
            {
                if (y >= grid.Length) break;
                for (int x = 0; x < 32; x++)
                {
                    if (x >= grid[y].Length) break;
                    char c = grid[y][x];
                    if (pal.ContainsKey(c))
                    {
                        int targetY = y + offsetY;

                        // FIX: Ensure the pixel stays within the bounds of the image canvas!
                        if (targetY >= 0 && targetY < targetHeight)
                        {
                            bmp.SetPixel(x, targetY, pal[c]);
                        }
                    }
                }
            }
            return bmp;
        }
    } // <-- Closes RetroIcons class

    // =========================================================================
    // 10. APP PATHS & SESSION MANAGER (RESTORED)
    // =========================================================================
    public static class AppPaths
    {
        // Centralized file paths to route all cache, tokens, and config data into the hidden AppData folder
        public static string AppDataFolder { get; private set; }
        public static string SmtpConfig { get; private set; }
        public static string TokenFolder { get; private set; }
        public static string HealthCheckScript { get; private set; }

        static AppPaths()
        {
            AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Apollo Technology");

            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }

            SmtpConfig = Path.Combine(AppDataFolder, "smtp_config.txt");
            TokenFolder = Path.Combine(AppDataFolder, "token_v3.json");
            HealthCheckScript = Path.Combine(AppDataFolder, "heathcheck.ps1");
        }
    }

    public static class SessionInfo
    {
        // Stores the authenticated user's name globally to display in the Greeting text
        public static string UserName { get; set; } = "Engineer";
    }
}