namespace PrivilegeDesk.Admin;

public class LoginForm : Form
{
    private readonly TextBox _username = new();
    private readonly TextBox _password = new();
    private readonly Button _loginButton = new();
    private readonly Label _status = new();

    public ApiClient? Client { get; private set; }

    public LoginForm()
    {
        Text = "PrivilegeDesk — Sign in";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(380, 210);

        var userLabel = new Label { Text = "Username", Location = new Point(24, 24), AutoSize = true };
        _username.Location = new Point(24, 48);
        _username.Width = 330;
        _username.Text = "approver";

        var passLabel = new Label { Text = "Password", Location = new Point(24, 84), AutoSize = true };
        _password.Location = new Point(24, 108);
        _password.Width = 330;
        _password.UseSystemPasswordChar = true;
        _password.Text = "Password123!";

        _loginButton.Text = "Login";
        _loginButton.Location = new Point(24, 148);
        _loginButton.Width = 100;
        _loginButton.Click += async (_, _) => await LoginAsync();
        AcceptButton = _loginButton;

        _status.Location = new Point(140, 154);
        _status.AutoSize = true;
        _status.ForeColor = Color.Firebrick;

        Controls.AddRange(new Control[] { userLabel, _username, passLabel, _password, _loginButton, _status });
    }

    private async Task LoginAsync()
    {
        _status.Text = string.Empty;
        _loginButton.Enabled = false;

        try
        {
            var client = new ApiClient("http://localhost:5080/");
            await client.LoginAsync(_username.Text.Trim(), _password.Text);

            if (client.Role is not "Approver" and not "Admin")
            {
                throw new InvalidOperationException("This app is for Approvers and Admins only.");
            }

            Client = client;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _status.Text = ex.InnerException?.Message ?? ex.Message;
        }
        finally
        {
            _loginButton.Enabled = true;
        }
    }
}
