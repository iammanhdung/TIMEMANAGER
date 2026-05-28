using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SmartTimeManager.Forms;
using SmartTimeManager.UI;
using SmartTimeManager.Services;

namespace SmartTimeManager
{
    public partial class Form1 : Form
    {
        private TableLayoutPanel root;
        private Panel sidebarPanel;
        private Panel mainPanel;

        private SidebarButton btnDashboard;
        private SidebarButton btnStatistics;
        private SidebarButton btnReminder;
        private SidebarButton btnGoals;
        private Button btnResetData;
        private Label lblUserName;
        private Label lblUserEmail;
        private string profileFolder;
        private string namePath;
        private string emailPath;

        public Form1()
        {
            InitializeComponent();
            AutoScaleMode = AutoScaleMode.None;
            Text = "Smart Time Manager";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1180, 720);
            Size = new Size(1450, 850);
            BackColor = Theme.Background;
            profileFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SmartTimeManager");
            Directory.CreateDirectory(profileFolder);
            namePath = Path.Combine(profileFolder, "user_name.txt");
            emailPath = Path.Combine(profileFolder, "user_email.txt");
            BuildLayout();
        }

        private void BuildLayout()
        {
            Controls.Clear();
            root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 1;
            root.ColumnCount = 2;
            root.Margin = new Padding(0);
            root.Padding = new Padding(0);
            root.BackColor = Theme.Background;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(root);

            sidebarPanel = new Panel();
            sidebarPanel.Dock = DockStyle.Fill;
            sidebarPanel.BackColor = Theme.Sidebar;
            sidebarPanel.Margin = new Padding(0);

            mainPanel = new Panel();
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.BackColor = Theme.Background;
            mainPanel.Padding = new Padding(28, 24, 28, 24);
            mainPanel.Margin = new Padding(0);

            root.Controls.Add(sidebarPanel, 0, 0);
            root.Controls.Add(mainPanel, 1, 0);
            BuildSidebar();
            ShowDashboard();
        }

        private void BuildSidebar()
        {
            sidebarPanel.Controls.Clear();

            RoundedPanel logoBox = new RoundedPanel();
            logoBox.Size = new Size(58, 58);
            logoBox.Location = new Point(28, 58);
            logoBox.BackColor = Theme.Primary;
            logoBox.BorderSize = 0;
            logoBox.Radius = 18;
            logoBox.Padding = new Padding(0);
            sidebarPanel.Controls.Add(logoBox);

            PictureBox logoPic = new PictureBox();
            logoPic.Size = new Size(34, 34);
            logoPic.Location = new Point(12, 12);
            logoPic.SizeMode = PictureBoxSizeMode.Zoom;
            logoPic.BackColor = Color.Transparent;
            logoPic.Image = Ui.AppLogoImage();
            logoBox.Controls.Add(logoPic);

            Label logoTitle = new Label();
            logoTitle.Text = "SMART TIME";
            logoTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            logoTitle.ForeColor = Color.White;
            logoTitle.AutoSize = true;
            logoTitle.Location = new Point(98, 64);
            sidebarPanel.Controls.Add(logoTitle);

            Label logoSub = new Label();
            logoSub.Text = "MANAGER";
            logoSub.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            logoSub.ForeColor = Theme.Primary2;
            logoSub.AutoSize = true;
            logoSub.Location = new Point(99, 94);
            sidebarPanel.Controls.Add(logoSub);

            btnDashboard = CreateSidebarButton("⌂   Dashboard", 185);
            btnStatistics = CreateSidebarButton("▥   Statistics", 240);
            btnReminder = CreateSidebarButton("◷   Reminder", 295);
            btnGoals = CreateSidebarButton("◎   Goals", 350);

            btnDashboard.Click += (s, e) => ShowDashboard();
            btnStatistics.Click += (s, e) => ShowStatistics();
            btnReminder.Click += (s, e) => ShowReminder();
            btnGoals.Click += (s, e) => ShowGoals();

            sidebarPanel.Controls.Add(btnDashboard);
            sidebarPanel.Controls.Add(btnStatistics);
            sidebarPanel.Controls.Add(btnReminder);
            sidebarPanel.Controls.Add(btnGoals);

            BuildResetButton();
            BuildUserBox();
        }

        private SidebarButton CreateSidebarButton(string text, int y)
        {
            SidebarButton button = new SidebarButton();
            button.Text = text;
            button.Location = new Point(24, y);
            button.Size = new Size(212, 50);
            return button;
        }

        private void BuildResetButton()
        {
            btnResetData = new Button();
            btnResetData.Text = "↻   Reset Data";
            btnResetData.Size = new Size(212, 38);
            btnResetData.Location = new Point(24, 420);
            btnResetData.FlatStyle = FlatStyle.Flat;
            btnResetData.FlatAppearance.BorderSize = 1;
            btnResetData.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnResetData.BackColor = Color.FromArgb(10, 27, 50);
            btnResetData.ForeColor = Color.FromArgb(248, 113, 113);
            btnResetData.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnResetData.TextAlign = ContentAlignment.MiddleLeft;
            btnResetData.Padding = new Padding(18, 0, 0, 0);
            btnResetData.Cursor = Cursors.Hand;
            btnResetData.Click += (s, e) => ResetApplicationData();
            sidebarPanel.Controls.Add(btnResetData);
        }

        private void ResetApplicationData()
        {
            DialogResult result = MessageBox.Show(
                "Reset all tasks and goals?\n\nThis will clear completed tasks, pending tasks, reminders, and goals. Your profile name/email will be kept.",
                "Reset Smart Time Manager",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result != DialogResult.Yes) return;

            try
            {
                DatabaseService.ResetAllData();
                MessageBox.Show("Data reset successfully.", "Smart Time Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowDashboard();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Reset failed: " + ex.Message, "Smart Time Manager", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BuildUserBox()
        {
            Panel line = new Panel();
            line.BackColor = Theme.SidebarLine;
            line.Size = new Size(212, 1);
            line.Location = new Point(24, 0);
            line.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            sidebarPanel.Controls.Add(line);

            Label avatar = new Label();
            avatar.Text = "●";
            avatar.Font = new Font("Segoe UI", 28F, FontStyle.Bold);
            avatar.ForeColor = Theme.Primary2;
            avatar.Size = new Size(48, 48);
            avatar.Location = new Point(32, 0);
            avatar.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            avatar.Cursor = Cursors.Hand;
            sidebarPanel.Controls.Add(avatar);

            lblUserName = new Label();
            lblUserName.Text = LoadProfileText(namePath, "Dũng");
            lblUserName.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblUserName.ForeColor = Color.White;
            lblUserName.AutoSize = true;
            lblUserName.Location = new Point(88, 0);
            lblUserName.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            lblUserName.Cursor = Cursors.Hand;
            sidebarPanel.Controls.Add(lblUserName);

            lblUserEmail = new Label();
            lblUserEmail.Text = LoadProfileText(emailPath, "dung@example.com");
            lblUserEmail.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lblUserEmail.ForeColor = Color.FromArgb(148, 163, 184);
            lblUserEmail.AutoSize = true;
            lblUserEmail.Location = new Point(88, 0);
            lblUserEmail.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            lblUserEmail.Cursor = Cursors.Hand;
            sidebarPanel.Controls.Add(lblUserEmail);

            ToolTip tip = new ToolTip();
            tip.SetToolTip(avatar, "Click to edit profile");
            tip.SetToolTip(lblUserName, "Click to edit profile");
            tip.SetToolTip(lblUserEmail, "Click to edit profile");

            avatar.Click += (s, e) => ShowProfileDialog();
            lblUserName.Click += (s, e) => ShowProfileDialog();
            lblUserEmail.Click += (s, e) => ShowProfileDialog();

            sidebarPanel.Resize += (s, e) =>
            {
                line.Top = sidebarPanel.Height - 152;
                avatar.Top = sidebarPanel.Height - 118;
                lblUserName.Top = sidebarPanel.Height - 104;
                lblUserEmail.Top = sidebarPanel.Height - 82;
            };
        }

        private string LoadProfileText(string path, string fallback)
        {
            try
            {
                if (File.Exists(path))
                {
                    string value = File.ReadAllText(path).Trim();
                    if (!string.IsNullOrWhiteSpace(value)) return value;
                }
            }
            catch { }
            return fallback;
        }

        private void SaveProfile(string name, string email)
        {
            try
            {
                File.WriteAllText(namePath, name);
                File.WriteAllText(emailPath, email);
            }
            catch { }
        }

        private void ShowProfileDialog()
        {
            Form dialog = new Form();
            dialog.Text = "Edit Profile";
            dialog.StartPosition = FormStartPosition.CenterParent;
            dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            dialog.MaximizeBox = false;
            dialog.MinimizeBox = false;
            dialog.ClientSize = new Size(380, 230);
            dialog.BackColor = Theme.Background;
            dialog.Font = Theme.Body;

            Label title = new Label();
            title.Text = "Edit profile";
            title.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            title.ForeColor = Theme.Text;
            title.AutoSize = true;
            title.Location = new Point(24, 18);
            dialog.Controls.Add(title);

            Label nameLabel = new Label();
            nameLabel.Text = "Name";
            nameLabel.Font = Theme.H3;
            nameLabel.ForeColor = Theme.Muted;
            nameLabel.AutoSize = true;
            nameLabel.Location = new Point(24, 64);
            dialog.Controls.Add(nameLabel);

            TextBox txtName = new TextBox();
            txtName.Text = lblUserName.Text;
            txtName.Font = Theme.Body;
            txtName.Location = new Point(24, 88);
            txtName.Size = new Size(330, 28);
            dialog.Controls.Add(txtName);

            Label emailLabel = new Label();
            emailLabel.Text = "Email";
            emailLabel.Font = Theme.H3;
            emailLabel.ForeColor = Theme.Muted;
            emailLabel.AutoSize = true;
            emailLabel.Location = new Point(24, 124);
            dialog.Controls.Add(emailLabel);

            TextBox txtEmail = new TextBox();
            txtEmail.Text = lblUserEmail.Text;
            txtEmail.Font = Theme.Body;
            txtEmail.Location = new Point(24, 148);
            txtEmail.Size = new Size(330, 28);
            dialog.Controls.Add(txtEmail);

            Button save = new Button();
            save.Text = "Save";
            save.BackColor = Theme.Primary;
            save.ForeColor = Color.White;
            save.FlatStyle = FlatStyle.Flat;
            save.FlatAppearance.BorderSize = 0;
            save.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            save.Size = new Size(92, 34);
            save.Location = new Point(162, 188);
            save.Click += (s, e) =>
            {
                string name = string.IsNullOrWhiteSpace(txtName.Text) ? "Dũng" : txtName.Text.Trim();
                string email = string.IsNullOrWhiteSpace(txtEmail.Text) ? "dung@example.com" : txtEmail.Text.Trim();
                lblUserName.Text = name;
                lblUserEmail.Text = email;
                SaveProfile(name, email);
                dialog.DialogResult = DialogResult.OK;
                dialog.Close();
            };
            dialog.Controls.Add(save);

            Button cancel = new Button();
            cancel.Text = "Cancel";
            cancel.BackColor = Color.FromArgb(226, 232, 240);
            cancel.ForeColor = Theme.Text;
            cancel.FlatStyle = FlatStyle.Flat;
            cancel.FlatAppearance.BorderSize = 0;
            cancel.Font = Theme.Body;
            cancel.Size = new Size(92, 34);
            cancel.Location = new Point(262, 188);
            cancel.Click += (s, e) => dialog.Close();
            dialog.Controls.Add(cancel);

            dialog.ShowDialog(this);
        }

        private void ShowDashboard() { SetActiveButton(btnDashboard); LoadPage(new UcDashboard()); }
        private void ShowStatistics() { SetActiveButton(btnStatistics); LoadPage(new UcStatistics()); }
        private void ShowReminder() { SetActiveButton(btnReminder); LoadPage(new UcReminder()); }
        private void ShowGoals() { SetActiveButton(btnGoals); LoadPage(new UcGoals()); }

        private void LoadPage(UserControl page)
        {
            mainPanel.Controls.Clear();
            page.Dock = DockStyle.Fill;
            page.Margin = new Padding(0);
            page.Padding = new Padding(0);
            page.BackColor = Theme.Background;
            mainPanel.Controls.Add(page);
        }

        private void SetActiveButton(SidebarButton activeButton)
        {
            SidebarButton[] buttons = { btnDashboard, btnStatistics, btnReminder, btnGoals };
            foreach (SidebarButton btn in buttons)
            {
                if (btn == null) continue;
                btn.Active = false;
                btn.Invalidate();
            }
            if (activeButton != null)
            {
                activeButton.Active = true;
                activeButton.Invalidate();
            }
        }
    }
}
