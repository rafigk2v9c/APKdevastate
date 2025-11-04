using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

///CODED BY RAFIG ZARBALIYEV\\\
///   INSTAGRAM @rafok2v9c    \\\

namespace APKdevastate
{
    public partial class selectapkform : Form
    {
        private bool isMouseDown = false;
        private Point lastLocation;
        public string SelectedApkPath { get; private set; }

        public selectapkform()
        {
            InitializeComponent();

            this.surusdurmekpaneli.MouseDown += new MouseEventHandler(surusdurmekpaneli_MouseDown);
            this.surusdurmekpaneli.MouseMove += new MouseEventHandler(surusdurmekpaneli_MouseMove);
            this.surusdurmekpaneli.MouseUp += new MouseEventHandler(surusdurmekpaneli_MouseUp);

            this.AllowDrop = true;
            this.DragEnter += selectapkform_DragEnter;
            this.DragDrop += selectapkform_DragDrop;
            this.Load += selectapkform_Load;
        }

        private void selectapkform_Load(object sender, EventArgs e)
        {
            if (!IsJavaInstalled())
            {
                MessageBox.Show(
                    "Java is not installed on your system!\n\n" +
                    "APKdevastate requires Java to analyze APK files.\n" +
                    "Please install Java (JDK or JRE) and try again.\n\n",
                 
                    "Java Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                Application.Exit();
            }
        }

        private bool IsJavaInstalled()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = "-version",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private void surusdurmekpaneli_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = true;
                lastLocation = e.Location;
            }
        }

        private void surusdurmekpaneli_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown)
            {
                this.Location = new Point(
                    (this.Location.X - lastLocation.X) + e.X,
                    (this.Location.Y - lastLocation.Y) + e.Y
                );
            }
        }

        private void surusdurmekpaneli_MouseUp(object sender, MouseEventArgs e)
        {
            isMouseDown = false;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void yeniproyektbutton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select an APK File";
                openFileDialog.Filter = "APK File (*.apk)|*.apk";
                openFileDialog.Multiselect = false;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    SelectedApkPath = openFileDialog.FileName;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
        }

        private void selectapkform_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1 && Path.GetExtension(files[0]).ToLower() == ".apk")
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void selectapkform_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                string apkPath = files[0];
                if (Path.GetExtension(apkPath).ToLower() == ".apk")
                {
                    SelectedApkPath = apkPath;
                   
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    //MessageBox.Show("erro", "erro", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
    }
}
