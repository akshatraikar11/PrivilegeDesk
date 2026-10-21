namespace PrivilegeDesk.Admin;

public class MainForm : Form
{
    private readonly ApiClient _client;
    private readonly DataGridView _grid = new();
    private readonly Button _refresh = new();
    private readonly Button _approve = new();
    private readonly Button _reject = new();
    private readonly Label _status = new();

    public MainForm(ApiClient client)
    {
        _client = client;

        Text = $"PrivilegeDesk — pending requests ({client.Username})";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1000;
        Height = 560;

        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            Padding = new Padding(8)
        };

        _refresh.Text = "Refresh";
        _approve.Text = "Approve";
        _reject.Text = "Reject";
        _status.AutoSize = true;
        _status.Padding = new Padding(12, 8, 0, 0);

        _refresh.Click += async (_, _) => await LoadPendingAsync();
        _approve.Click += async (_, _) => await DecideAsync(approve: true);
        _reject.Click += async (_, _) => await DecideAsync(approve: false);

        bar.Controls.AddRange(new Control[] { _refresh, _approve, _reject, _status });
        Controls.Add(_grid);
        Controls.Add(bar);

        Load += async (_, _) => await LoadPendingAsync();
    }

    private async Task LoadPendingAsync()
    {
        try
        {
            _status.Text = "Loading...";
            var rows = await _client.GetPendingAsync();
            _grid.DataSource = rows;
            _status.Text = rows.Count == 0 ? "No pending requests." : $"{rows.Count} pending request(s).";
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
            MessageBox.Show(ex.Message, "Refresh failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task DecideAsync(bool approve)
    {
        if (_grid.CurrentRow?.DataBoundItem is not AccessRequestDto row)
        {
            MessageBox.Show("Select a request first.", "PrivilegeDesk", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var verb = approve ? "approve" : "reject";
        var confirm = MessageBox.Show(
            $"Are you sure you want to {verb} request #{row.Id} for {row.Username}?",
            "Confirm",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        try
        {
            await _client.DecideAsync(row.Id, approve);
            await LoadPendingAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Update failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
