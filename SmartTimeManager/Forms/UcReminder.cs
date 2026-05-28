using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using SmartTimeManager.Models;
using SmartTimeManager.Services;
using SmartTimeManager.UI;

namespace SmartTimeManager.Forms
{
    public partial class UcReminder : UserControl
    {
        private FlowLayoutPanel listPanel;
        private Label lblClock;
        private Label lblDate;
        private string currentFilter = "All";
        private Button btnAll;
        private Button btnToday;
        private Button btnTomorrow;
        private Button btnWeek;
        private Timer timer;
        private int _lastRefreshMinute = -1;

        public UcReminder()
        {
            InitializeComponent();
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            BuildUI();
            LoadTasks();
            StartClock();
        }

        private void BuildUI()
        {
            Controls.Clear();
            BackColor = Theme.Background;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 3;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.BackColor = Theme.Background;
            Controls.Add(root);

            TableLayoutPanel header = new TableLayoutPanel();
            header.Dock = DockStyle.Fill;
            header.ColumnCount = 2;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            header.BackColor = Theme.Background;

            FlowLayoutPanel titleBox = new FlowLayoutPanel();
            titleBox.Dock = DockStyle.Fill;
            titleBox.FlowDirection = FlowDirection.TopDown;
            titleBox.WrapContents = false;
            titleBox.BackColor = Theme.Background;
            titleBox.Margin = new Padding(0);
            titleBox.Controls.Add(Ui.Label("Reminder", Theme.H1, Theme.Text));
            titleBox.Controls.Add(Ui.Label("Your upcoming reminders", Theme.Body, Theme.Muted));
            header.Controls.Add(titleBox, 0, 0);

            Panel clockBox = new Panel();
            clockBox.Dock = DockStyle.Fill;
            clockBox.BackColor = Theme.Background;
            Label icon = new Label();
            icon.Text = "◷";
            icon.Font = new Font("Segoe UI Symbol", 22F, FontStyle.Regular);
            icon.ForeColor = Theme.Primary2;
            icon.AutoSize = true;
            icon.Location = new Point(8, 6);
            clockBox.Controls.Add(icon);
            lblClock = new Label();
            lblClock.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            lblClock.ForeColor = Theme.Text;
            lblClock.AutoSize = true;
            lblClock.Location = new Point(48, 2);
            clockBox.Controls.Add(lblClock);
            lblDate = new Label();
            lblDate.Font = Theme.Small;
            lblDate.ForeColor = Theme.Muted;
            lblDate.AutoSize = true;
            lblDate.Location = new Point(50, 35);
            clockBox.Controls.Add(lblDate);
            header.Controls.Add(clockBox, 1, 0);
            root.Controls.Add(header, 0, 0);

            TableLayoutPanel toolbar = new TableLayoutPanel();
            toolbar.Dock = DockStyle.Fill;
            toolbar.ColumnCount = 2;
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
            toolbar.BackColor = Theme.Background;

            Panel filterBar = new Panel();
            filterBar.Dock = DockStyle.Fill;
            filterBar.BackColor = Theme.Background;
            btnAll = CreateFilterButton("All", 0, true);
            btnToday = CreateFilterButton("Today", 58, false);
            btnTomorrow = CreateFilterButton("Tomorrow", 124, false);
            btnWeek = CreateFilterButton("This Week", 214, false);
            filterBar.Controls.Add(btnAll);
            filterBar.Controls.Add(btnToday);
            filterBar.Controls.Add(btnTomorrow);
            filterBar.Controls.Add(btnWeek);
            toolbar.Controls.Add(filterBar, 0, 0);

            PillButton add = new PillButton();
            add.Text = "+ Add Reminder";
            add.Width = 135;
            add.Height = 34;
            add.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            add.Margin = new Padding(0, 10, 0, 0);
            add.Click += (s, e) => AddTask();
            toolbar.Controls.Add(add, 1, 0);
            root.Controls.Add(toolbar, 0, 1);

            RoundedPanel card = new RoundedPanel();
            card.Dock = DockStyle.Fill;
            card.Margin = new Padding(0, 8, 0, 0);
            card.Padding = new Padding(18, 14, 18, 14);
            root.Controls.Add(card, 0, 2);

            listPanel = new FlowLayoutPanel();
            listPanel.Dock = DockStyle.Fill;
            listPanel.FlowDirection = FlowDirection.TopDown;
            listPanel.WrapContents = false;
            listPanel.AutoScroll = true;
            listPanel.BackColor = Color.Transparent;
            card.Controls.Add(listPanel);
        }

        private Button CreateFilterButton(string text, int x, bool active)
        {
            Button b = new Button();
            b.Text = text;
            b.Size = new Size(text == "This Week" ? 82 : (text == "Tomorrow" ? 80 : 54), 30);
            b.Location = new Point(x, 8);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = active ? Theme.SoftBlue : Theme.Background;
            b.ForeColor = active ? Theme.Primary : Theme.Muted;
            b.Font = new Font("Segoe UI", 8.5F, active ? FontStyle.Bold : FontStyle.Regular);
            b.Cursor = Cursors.Hand;
            b.Click += (s, e) => SetFilter(text);
            return b;
        }

        private void SetFilter(string filter)
        {
            currentFilter = filter;
            UpdateFilterVisuals();
            LoadTasks();
        }

        private void UpdateFilterVisuals()
        {
            UpdateFilterButton(btnAll, currentFilter == "All");
            UpdateFilterButton(btnToday, currentFilter == "Today");
            UpdateFilterButton(btnTomorrow, currentFilter == "Tomorrow");
            UpdateFilterButton(btnWeek, currentFilter == "This Week");
        }

        private void UpdateFilterButton(Button b, bool active)
        {
            b.BackColor = active ? Theme.SoftBlue : Theme.Background;
            b.ForeColor = active ? Theme.Primary : Theme.Muted;
            b.Font = new Font("Segoe UI", 8.5F, active ? FontStyle.Bold : FontStyle.Regular);
        }

        private void LoadTasks()
        {
            listPanel.SuspendLayout();
            listPanel.Controls.Clear();

            var allTasks = SafeTasks()
                .Where(t => !t.IsCompleted && !Same(t.Status, "Completed"))
                .OrderBy(t => ParseDate(t.DueDate) ?? DateTime.MaxValue)
                .ThenBy(t => t.ReminderTime)
                .ToList();

            var filtered = ApplyFilter(allTasks);
            if (filtered.Count == 0)
            {
                listPanel.Controls.Add(Ui.Label("No reminders yet", Theme.Body, Theme.Muted));
                listPanel.ResumeLayout();
                return;
            }

            foreach (var group in BuildGroups(filtered))
            {
                if (group.Value.Count == 0) continue;
                listPanel.Controls.Add(CreateSectionHeader(group.Key));
                foreach (var task in group.Value)
                    listPanel.Controls.Add(CreateRow(task));
            }
            listPanel.ResumeLayout();
        }

        private List<TaskModel> ApplyFilter(List<TaskModel> tasks)
        {
            if (currentFilter == "Today")
                return tasks.Where(t => IsToday(ParseDate(t.DueDate))).ToList();
            if (currentFilter == "Tomorrow")
                return tasks.Where(t => IsTomorrow(ParseDate(t.DueDate))).ToList();
            if (currentFilter == "This Week")
                return tasks.Where(t => IsThisWeek(ParseDate(t.DueDate))).ToList();
            return tasks;
        }

        private Dictionary<string, List<TaskModel>> BuildGroups(List<TaskModel> tasks)
        {
            Dictionary<string, List<TaskModel>> groups = new Dictionary<string, List<TaskModel>>();
            if (currentFilter == "Today") groups["Today"] = tasks;
            else if (currentFilter == "Tomorrow") groups["Tomorrow"] = tasks;
            else if (currentFilter == "This Week") groups["This Week"] = tasks;
            else
            {
                groups["Today"] = tasks.Where(t => IsToday(ParseDate(t.DueDate))).ToList();
                groups["Tomorrow"] = tasks.Where(t => IsTomorrow(ParseDate(t.DueDate))).ToList();
                groups["This Week"] = tasks.Where(t => IsThisWeek(ParseDate(t.DueDate)) && !IsToday(ParseDate(t.DueDate)) && !IsTomorrow(ParseDate(t.DueDate))).ToList();
                groups["Later"] = tasks.Where(t => ParseDate(t.DueDate).HasValue && ParseDate(t.DueDate).Value.Date > EndOfWeek(DateTime.Today).Date).ToList();
                groups["No Date"] = tasks.Where(t => !ParseDate(t.DueDate).HasValue).ToList();
            }
            return groups;
        }

        private Control CreateSectionHeader(string title)
        {
            Label lbl = new Label();
            lbl.AutoSize = false;
            lbl.Width = Math.Max(760, listPanel.ClientSize.Width - 28);
            lbl.Height = 28;
            lbl.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lbl.ForeColor = Theme.Muted;
            lbl.BackColor = Color.Transparent;
            lbl.Margin = new Padding(0, 6, 0, 4);
            lbl.Text = SectionText(title);
            return lbl;
        }

        private string SectionText(string key)
        {
            if (key == "Today") return "Today — " + DateTime.Today.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
            if (key == "Tomorrow") return "Tomorrow — " + DateTime.Today.AddDays(1).ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
            if (key == "This Week") return "This Week";
            return key;
        }

        private Control CreateRow(TaskModel task)
        {
            RoundedPanel row = new RoundedPanel();
            row.Width = Math.Max(760, listPanel.ClientSize.Width - 28);
            row.Height = 78;
            row.Margin = new Padding(0, 0, 0, 10);
            row.Padding = new Padding(0);
            row.Radius = 14;
            row.BorderColor = Theme.Border;
            row.BackColor = Theme.Card;

            string category = Ui.Safe(task.Category, "General");

            Label bell = new Label();
            bell.Text = "🔔";
            bell.Font = new Font("Segoe UI Emoji", 12F, FontStyle.Regular);
            bell.AutoSize = true;
            bell.Location = new Point(16, 28);
            row.Controls.Add(bell);

            Panel iconBox = new Panel();
            iconBox.Size = new Size(34, 34);
            iconBox.BackColor = Ui.CategorySoftColor(category);
            iconBox.Location = new Point(46, 22);
            row.Controls.Add(iconBox);

            var iconImage = Ui.CategoryImage(category);
            if (iconImage != null)
            {
                PictureBox iconPic = new PictureBox();
                iconPic.Dock = DockStyle.Fill;
                iconPic.SizeMode = PictureBoxSizeMode.Zoom;
                iconPic.Image = iconImage;
                iconBox.Controls.Add(iconPic);
            }
            else
            {
                Label icon = new Label();
                icon.Dock = DockStyle.Fill;
                icon.Text = Ui.CategoryIcon(category);
                icon.Font = Ui.CategoryIconFont(category, 15F);
                icon.ForeColor = Ui.CategoryColor(category);
                icon.TextAlign = ContentAlignment.MiddleCenter;
                iconBox.Controls.Add(icon);
            }

            Panel dot = new Panel();
            dot.Size = new Size(8, 8);
            dot.BackColor = PriorityColor(task.Priority);
            dot.Location = new Point(92, 38);
            row.Controls.Add(dot);

            Label name = Ui.FixedLabel(Ui.Safe(task.TaskName, "Untitled task"), Theme.H3, Theme.Text, 430, 22);
            name.Location = new Point(110, 14);
            row.Controls.Add(name);

            Label sub = Ui.FixedLabel(Ui.Safe(task.Priority, "Medium") + " Priority  •  " + category, Theme.Small, Theme.Muted, 430, 20);
            sub.Location = new Point(110, 36);
            row.Controls.Add(sub);

            DateTime? date = ParseDate(task.DueDate);
            Label time = Ui.FixedLabel(Ui.Safe(task.ReminderTime, "--:--"), Theme.H3, Theme.Text, 80, 22);
            time.Location = new Point(row.Width - 120, 14);
            time.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            row.Controls.Add(time);

            Label due = Ui.FixedLabel(DueText(date), Theme.Small, Theme.Muted, 80, 20);
            due.Location = new Point(row.Width - 120, 38);
            due.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            row.Controls.Add(due);

            Button more = new Button();
            more.Text = "⋮";
            more.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            more.FlatStyle = FlatStyle.Flat;
            more.FlatAppearance.BorderSize = 0;
            more.ForeColor = Theme.Muted;
            more.BackColor = Color.Transparent;
            more.Size = new Size(34, 34);
            more.Location = new Point(row.Width - 40, 22);
            more.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            row.Controls.Add(more);

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Start task", null, (s, e) => { DatabaseService.UpdateTaskWorkflowStatus(task.Id, "In Progress"); LoadTasks(); });
            menu.Items.Add("Move to pending", null, (s, e) => { DatabaseService.UpdateTaskWorkflowStatus(task.Id, "Pending"); LoadTasks(); });
            menu.Items.Add("Mark as completed", null, (s, e) => { DatabaseService.UpdateTaskStatus(task.Id, true); LoadTasks(); });
            menu.Items.Add("Delete", null, (s, e) =>
            {
                if (MessageBox.Show("Delete this reminder?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    DatabaseService.DeleteTask(task.Id);
                    LoadTasks();
                }
            });
            more.Click += (s, e) => menu.Show(more, new Point(0, more.Height));

            listPanel.Resize -= ListPanel_Resize;
            listPanel.Resize += ListPanel_Resize;
            return row;
        }

        private void ListPanel_Resize(object sender, EventArgs e)
        {
            foreach (Control c in listPanel.Controls)
            {
                c.Width = Math.Max(760, listPanel.ClientSize.Width - 28);
            }
        }

        private void AddTask()
        {
            using (FormAddTask f = new FormAddTask())
            {
                if (f.ShowDialog() == DialogResult.OK)
                {
                    DatabaseService.InsertTask(new TaskModel
                    {
                        TaskName = f.TaskName,
                        Category = f.Category,
                        Priority = f.Priority,
                        DueDate = f.DueDate,
                        ReminderTime = f.ReminderTime,
                        Status = f.Status,
                        IsCompleted = false
                    });
                    LoadTasks();
                }
            }
        }

        private void StartClock()
        {
            lblClock.Text = DateTime.Now.ToString("HH:mm:ss");
            lblDate.Text = DateTime.Now.ToString("dddd, MMM dd, yyyy", CultureInfo.InvariantCulture);
            _lastRefreshMinute = DateTime.Now.Minute;
            timer = new Timer();
            timer.Interval = 1000;
            timer.Tick += (s, e) =>
            {
                lblClock.Text = DateTime.Now.ToString("HH:mm:ss");
                lblDate.Text = DateTime.Now.ToString("dddd, MMM dd, yyyy", CultureInfo.InvariantCulture);
                if (DateTime.Now.Minute != _lastRefreshMinute)
                {
                    _lastRefreshMinute = DateTime.Now.Minute;
                    LoadTasks();
                }
            };
            timer.Start();
        }

        private List<TaskModel> SafeTasks()
        {
            try { return DatabaseService.GetAllTasks(); }
            catch { return new List<TaskModel>(); }
        }

        private DateTime? ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string[] formats = { "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy", "M/d/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "d-M-yyyy" };
            DateTime dt;
            if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt)) return dt;
            if (DateTime.TryParse(value, out dt)) return dt;
            return null;
        }

        private bool IsToday(DateTime? d) { return d.HasValue && d.Value.Date == DateTime.Today; }
        private bool IsTomorrow(DateTime? d) { return d.HasValue && d.Value.Date == DateTime.Today.AddDays(1); }
        private bool IsThisWeek(DateTime? d) { return d.HasValue && d.Value.Date >= DateTime.Today && d.Value.Date <= EndOfWeek(DateTime.Today).Date; }
        private DateTime EndOfWeek(DateTime d)
        {
            int diff = DayOfWeek.Saturday - d.DayOfWeek;
            if (diff < 0) diff += 7;
            return d.AddDays(diff);
        }

        private string DueText(DateTime? date)
        {
            if (!date.HasValue) return "No date";
            if (date.Value.Date == DateTime.Today) return "Today";
            if (date.Value.Date == DateTime.Today.AddDays(1)) return "Tomorrow";
            return date.Value.ToString("MMM dd", CultureInfo.InvariantCulture);
        }

        private Color PriorityColor(string p)
        {
            p = (p ?? "").ToLowerInvariant();
            if (p.Contains("high")) return Theme.Red;
            if (p.Contains("low")) return Theme.Green;
            return Theme.Yellow;
        }

        private bool Same(string a, string b) { return string.Equals(a ?? "", b, StringComparison.OrdinalIgnoreCase); }
    }
}
