using System;
using System.Windows.Forms;

namespace APKdevastate
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string apkPath = null;

            using (selectapkform selectForm = new selectapkform())
            {
                DialogResult result = selectForm.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrEmpty(selectForm.SelectedApkPath))
                {
                    apkPath = selectForm.SelectedApkPath;
                }
            }

            if (!string.IsNullOrEmpty(apkPath))
            {
                Application.Run(new MainForm(apkPath));
            }
        }
    }
}
