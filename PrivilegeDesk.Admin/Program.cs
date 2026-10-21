namespace PrivilegeDesk.Admin;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var login = new LoginForm();
        if (login.ShowDialog() != DialogResult.OK || login.Client is null)
        {
            return;
        }

        Application.Run(new MainForm(login.Client));
    }
}
