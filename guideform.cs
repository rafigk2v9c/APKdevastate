using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace APKdevastate
{
    public partial class guideform : Form
    {
        private bool isMouseDown = false;
        private Point lastLocation;

        public guideform()
        {
            InitializeComponent();

            this.panelsurusdur.MouseDown += new MouseEventHandler(panelsurusdur_MouseDown);
            this.panelsurusdur.MouseMove += new MouseEventHandler(panelsurusdur_MouseMove);
            this.panelsurusdur.MouseUp += new MouseEventHandler(panelsurusdur_MouseUp);

            this.Shown += guideform_Shown;
        }

        private void guideform_Shown(object sender, EventArgs e)
        {
            this.BeginInvoke(new Action(() =>
            {
                ScrollToTop();
            }));
        }

        private void ScrollToTop()
        {
            panelsurusdur.AutoScrollPosition = new Point(0, 0);
            panelsurusdur.VerticalScroll.Value = 0;
            panelsurusdur.PerformLayout();
        }

        private void panelsurusdur_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                isMouseDown = true;
                lastLocation = e.Location;
            }
        }

        private void panelsurusdur_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown)
            {
                this.Location = new Point(
                    (this.Location.X - lastLocation.X) + e.X,
                    (this.Location.Y - lastLocation.Y) + e.Y
                );
            }
        }

        private void panelsurusdur_MouseUp(object sender, MouseEventArgs e)
        {
            isMouseDown = false;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "https://www.instagram.com/rafok2v9c/",
                UseShellExecute = true
            };
            Process.Start(psi);
        }
    }
}
