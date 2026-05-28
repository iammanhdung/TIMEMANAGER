using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using SmartTimeManager.Services;
using SmartTimeManager.UI;

namespace SmartTimeManager.Forms
{
    public partial class UcGoals : UserControl
    {
        private FlowLayoutPanel goalsPanel;
        private TextBox txtGoal;
        private DateTimePicker dtpTarget;
        private string currentFilter = "All";
        private Button btnAll;
        private Button btnInProgress;
        private Button btnCompleted;
        private Button btnOnHold;

        public UcGoals()
        {
            InitializeComponent();
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            BuildUI();
            LoadGoals();
        }

        private void BuildUI()
        {
            Controls.Clear();
            BackColor = Theme.Background;

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 4;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.BackColor = Theme.Background;
            Controls.Add(root);

            FlowLayoutPanel header = new FlowLayoutPanel();
            header.Dock = DockStyle.Fill;
            header.FlowDirection = FlowDirection.TopDown;
            header.WrapContents = false;
            header.BackColor = Theme.Background;
            header.Controls.Add(Ui.Label("Goals", Theme.H1, Theme.Text));
            header.Controls.Add(Ui.Label("Track and achieve your goals", Theme.Body, Theme.Muted));
            root.Controls.Add(header, 0, 0);

            Panel filterBar = new Panel();
            filterBar.Dock = DockStyle.Fill;
            filterBar.BackColor = Theme.Background;
            btnAll = CreateFilterButton("All Goals", 0, true);
            btnInProgress = CreateFilterButton("In Progress", 104, false);
            btnCompleted = CreateFilterButton("Completed", 218, false);
            btnOnHold = CreateFilterButton("On Hold", 322, false);
            filterBar.Controls.Add(btnAll);
            filterBar.Controls.Add(btnInProgress);
            filterBar.Controls.Add(btnCompleted);
            filterBar.Controls.Add(btnOnHold);
            root.Controls.Add(filterBar, 0, 1);

            RoundedPanel addCard = new RoundedPanel();
            addCard.Dock = DockStyle.Fill;
            addCard.Margin = new Padding(0, 6, 0, 12);
            addCard.Padding = new Padding(0);
            root.Controls.Add(addCard, 0, 2);

            Panel addPanel = new Panel();
            addPanel.Dock = DockStyle.Fill;
            addPanel.BackColor = Color.Transparent;
            addCard.Controls.Add(addPanel);

            Label lbl = Ui.FixedLabel("New Goal", Theme.H3, Theme.Text, 110, 26);
            lbl.Location = new Point(28, 33);
            addPanel.Controls.Add(lbl);

            txtGoal = new TextBox();
            txtGoal.Font = Theme.Body;
            txtGoal.Location = new Point(150, 30);
            txtGoal.Size = new Size(620, 28);
            txtGoal.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            addPanel.Controls.Add(txtGoal);

            dtpTarget = new DateTimePicker();
            dtpTarget.Format = DateTimePickerFormat.Short;
            dtpTarget.Font = Theme.Body;
            dtpTarget.Location = new Point(790, 30);
            dtpTarget.Size = new Size(130, 28);
            dtpTarget.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            addPanel.Controls.Add(dtpTarget);

            Button addBtn = new Button();
            addBtn.Text = "+ Add Goal";
            addBtn.FlatStyle = FlatStyle.Flat;
            addBtn.FlatAppearance.BorderSize = 0;
            addBtn.BackColor = Theme.Primary;
            addBtn.ForeColor = Color.White;
            addBtn.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            addBtn.Size = new Size(126, 36);
            addBtn.Location = new Point(940, 26);
            addBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            addBtn.Cursor = Cursors.Hand;
            addBtn.Click += (s, e) => AddGoal();
            addPanel.Controls.Add(addBtn);

            addPanel.Resize += (s, e) =>
            {
                int right = addPanel.ClientSize.Width;
                addBtn.Left = right - addBtn.Width - 26;
                dtpTarget.Left = addBtn.Left - dtpTarget.Width - 18;
                txtGoal.Width = Math.Max(200, dtpTarget.Left - txtGoal.Left - 18);
            };

            goalsPanel = new FlowLayoutPanel();
            goalsPanel.Dock = DockStyle.Fill;
            goalsPanel.FlowDirection = FlowDirection.TopDown;
            goalsPanel.WrapContents = false;
            goalsPanel.AutoScroll = true;
            goalsPanel.BackColor = Theme.Background;
            root.Controls.Add(goalsPanel, 0, 3);
        }

        private Button CreateFilterButton(string text, int x, bool active)
        {
            Button b = new Button();
            b.Text = text;
            b.Size = new Size(text == "All Goals" ? 92 : 98, 30);
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

        private void SetFilter(string value)
        {
            currentFilter = value == "All Goals" ? "All" : value;
            UpdateFilterVisuals();
            LoadGoals();
        }

        private void UpdateFilterVisuals()
        {
            UpdateFilterButton(btnAll, currentFilter == "All");
            UpdateFilterButton(btnInProgress, currentFilter == "In Progress");
            UpdateFilterButton(btnCompleted, currentFilter == "Completed");
            UpdateFilterButton(btnOnHold, currentFilter == "On Hold");
        }

        private void UpdateFilterButton(Button b, bool active)
        {
            b.BackColor = active ? Theme.SoftBlue : Theme.Background;
            b.ForeColor = active ? Theme.Primary : Theme.Muted;
            b.Font = new Font("Segoe UI", 8.5F, active ? FontStyle.Bold : FontStyle.Regular);
        }

        private void LoadGoals()
        {
            goalsPanel.Controls.Clear();
            DataTable dt;
            try { dt = DatabaseService.GetAllGoals(); }
            catch { dt = new DataTable(); }

            var rows = dt.AsEnumerable().Select(r => new GoalVm
            {
                Id = Convert.ToInt32(r["Id"]),
                Name = Convert.ToString(r["GoalName"]),
                Progress = Convert.ToInt32(r["Progress"]),
                TargetDate = Convert.ToString(r["TargetDate"])
            }).ToList();

            rows = ApplyFilter(rows);

            if (rows.Count == 0)
            {
                goalsPanel.Controls.Add(Ui.Label("No goals yet", Theme.Body, Theme.Muted));
                return;
            }

            foreach (var row in rows)
                goalsPanel.Controls.Add(CreateGoalCard(row));
        }

        private List<GoalVm> ApplyFilter(List<GoalVm> rows)
        {
            if (currentFilter == "Completed")
                return rows.Where(r => r.Progress >= 100).ToList();
            if (currentFilter == "In Progress")
                return rows.Where(r => r.Progress > 0 && r.Progress < 100).ToList();
            if (currentFilter == "On Hold")
                return rows.Where(r => r.Progress == 0).ToList();
            return rows;
        }

        private Control CreateGoalCard(GoalVm goal)
        {
            RoundedPanel card = new RoundedPanel();
            card.Width = Math.Max(820, goalsPanel.ClientSize.Width - 28);
            card.Height = 118;
            card.Margin = new Padding(0, 0, 0, 14);
            card.Padding = new Padding(18);

            Panel iconWrap = new Panel();
            iconWrap.Size = new Size(56, 56);
            iconWrap.Location = new Point(20, 26);
            iconWrap.BackColor = GoalSoftColor(goal.Progress);
            card.Controls.Add(iconWrap);

            Label icon = new Label();
            icon.Dock = DockStyle.Fill;
            icon.TextAlign = ContentAlignment.MiddleCenter;
            icon.Font = new Font("Segoe UI Symbol", 24F, FontStyle.Regular);
            icon.ForeColor = GoalColor(goal.Progress);
            icon.Text = GoalIcon(goal.Progress);
            iconWrap.Controls.Add(icon);

            Label title = Ui.FixedLabel(Ui.Safe(goal.Name, "Untitled goal"), Theme.H3, Theme.Text, 320, 22);
            title.Location = new Point(92, 18);
            card.Controls.Add(title);

            Label sub = Ui.FixedLabel(GoalSubtitle(goal.Name), Theme.Small, Theme.Muted, 320, 18);
            sub.Location = new Point(92, 42);
            card.Controls.Add(sub);

            Panel progressTrack = new Panel();
            progressTrack.Size = new Size(430, 6);
            progressTrack.Location = new Point(92, 74);
            progressTrack.BackColor = Color.FromArgb(226, 232, 240);
            card.Controls.Add(progressTrack);

            Panel progressFill = new Panel();
            progressFill.Size = new Size(Math.Max(10, (int)Math.Round(progressTrack.Width * Math.Max(0, Math.Min(100, goal.Progress)) / 100.0)), 6);
            progressFill.BackColor = GoalColor(goal.Progress);
            progressTrack.Controls.Add(progressFill);

            Label pct = Ui.FixedLabel(goal.Progress + "%", Theme.H3, Theme.Text, 44, 20);
            pct.Location = new Point(534, 66);
            card.Controls.Add(pct);

            Label target = Ui.FixedLabel("Target: " + Ui.Safe(FormatTarget(goal.TargetDate), "No date"), Theme.Small, Theme.Muted, 180, 18);
            target.Location = new Point(card.Width - 210, 20);
            target.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            card.Controls.Add(target);

            Label badge = CreateBadge(StatusText(goal.Progress), StatusBackColor(goal.Progress), StatusForeColor(goal.Progress));
            badge.Location = new Point(card.Width - 136, 44);
            badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            card.Controls.Add(badge);

            Button del = ActionButton("Delete", Theme.Red, 68);
            del.Location = new Point(card.Width - 234, 74);
            del.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            del.Click += (s, e) =>
            {
                if (MessageBox.Show("Delete this goal?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    DatabaseService.DeleteGoal(goal.Id);
                    LoadGoals();
                }
            };
            card.Controls.Add(del);

            Button minus = ActionButton("-10%", Theme.Muted, 60);
            minus.Location = new Point(card.Width - 160, 74);
            minus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            minus.Click += (s, e) => UpdateProgress(goal.Id, Math.Max(0, goal.Progress - 10));
            card.Controls.Add(minus);

            Button plus = ActionButton("+10%", Theme.Primary, 60);
            plus.Location = new Point(card.Width - 92, 74);
            plus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            plus.Click += (s, e) => UpdateProgress(goal.Id, Math.Min(100, goal.Progress + 10));
            card.Controls.Add(plus);

            goalsPanel.Resize -= GoalsPanel_Resize;
            goalsPanel.Resize += GoalsPanel_Resize;
            return card;
        }

        private void GoalsPanel_Resize(object sender, EventArgs e)
        {
            foreach (Control c in goalsPanel.Controls)
                c.Width = Math.Max(820, goalsPanel.ClientSize.Width - 28);
        }

        private Label CreateBadge(string text, Color back, Color fore)
        {
            Label b = new Label();
            b.Text = text;
            b.AutoSize = false;
            b.Size = new Size(98, 26);
            b.BackColor = back;
            b.ForeColor = fore;
            b.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            b.TextAlign = ContentAlignment.MiddleCenter;
            return b;
        }

        private Button ActionButton(string text, Color fore, int width)
        {
            Button b = new Button();
            b.Text = text;
            b.Size = new Size(width, 28);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.BackColor = Color.FromArgb(241, 245, 249);
            b.ForeColor = fore;
            b.Font = Theme.Small;
            b.Cursor = Cursors.Hand;
            return b;
        }

        private void AddGoal()
        {
            if (string.IsNullOrWhiteSpace(txtGoal.Text))
            {
                MessageBox.Show("Please enter goal name.", "Smart Time Manager");
                return;
            }
            DatabaseService.InsertGoal(txtGoal.Text.Trim(), dtpTarget.Value.ToString("dd/MM/yyyy"));
            txtGoal.Clear();
            LoadGoals();
        }

        private void UpdateProgress(int id, int value)
        {
            DatabaseService.UpdateGoalProgress(id, value);
            LoadGoals();
        }

        private string GoalIcon(int progress)
        {
            if (progress >= 100) return "◎";
            if (progress >= 70) return "✓";
            if (progress == 0) return "◌";
            return "◈";
        }

        private string GoalSubtitle(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Track your goal progress";
            return name.Length > 38 ? name.Substring(0, 38) + "..." : "Track progress toward this goal";
        }

        private string StatusText(int progress)
        {
            if (progress >= 100) return "Completed";
            if (progress == 0) return "On Hold";
            return "In Progress";
        }

        private Color GoalColor(int progress)
        {
            if (progress >= 100) return Theme.Green;
            if (progress >= 70) return Theme.Yellow;
            if (progress == 0) return Theme.Red;
            return Theme.Purple;
        }

        private Color GoalSoftColor(int progress)
        {
            if (progress >= 100) return Theme.SoftGreen;
            if (progress >= 70) return Theme.SoftYellow;
            if (progress == 0) return Theme.SoftRed;
            return Theme.SoftPurple;
        }

        private Color StatusBackColor(int progress)
        {
            if (progress >= 100) return Theme.SoftGreen;
            if (progress == 0) return Theme.SoftYellow;
            return Theme.SoftGreen;
        }

        private Color StatusForeColor(int progress)
        {
            if (progress >= 100) return Theme.Green;
            if (progress == 0) return Theme.Yellow;
            return Theme.Green;
        }

        private string FormatTarget(string value)
        {
            DateTime dt;
            if (DateTime.TryParseExact(value, new[] { "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy", "M/d/yyyy", "yyyy-MM-dd" },
                CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
            if (DateTime.TryParse(value, out dt))
                return dt.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);
            return value;
        }

        private class GoalVm
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Progress { get; set; }
            public string TargetDate { get; set; }
        }
    }
}
