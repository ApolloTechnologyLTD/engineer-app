using System;
using System.Windows.Forms;

namespace Apollo_Technology_Engineer_Application
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Forces Windows Forms to fall back to classic, un-themed rendering (Windows 95 style).
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new Form1());
        }
    }
}