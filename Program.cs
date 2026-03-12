using System;
using System.Windows.Forms;

namespace ZeroFN;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (!SetupManager.EnsureRequirements())
            return;

        Application.Run(new MainForm());
    }
}
