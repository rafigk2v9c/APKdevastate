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

            using (selectapkform selectForm = new selectapkform())
            {
                DialogResult result = selectForm.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrEmpty(selectForm.SelectedApkPath))
                {
                    Application.Run(new MainForm(selectForm.SelectedApkPath));
                }
                else
                {
                    MessageBox.Show("apk ERROR", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }
}
