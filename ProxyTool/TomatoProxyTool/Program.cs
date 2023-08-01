using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace TomatoProxyTool
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentUICulture;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainUI());
        }
    }
}