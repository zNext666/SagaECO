using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace TomatoProxyTool
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentUICulture;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainUI());
        }
    }
}
