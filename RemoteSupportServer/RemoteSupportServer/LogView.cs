using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoteSupportServer
{
    public partial class LogView : Form
    {
        public LogView()
        {
            InitializeComponent();
        }

        private void button_Clear_Click(object sender, EventArgs e)
        {
            textBox1.Clear();

        }

        public void Append(String text)
        {
            if (textBox1.InvokeRequired)
            {
                textBox1.Invoke(new MethodInvoker(
                    delegate ()
                    {
                        textBox1.AppendText(text + "\r\n");
                    }));

            }
            else
            {
                try {
                    textBox1.AppendText(text + "\r\n");
                }
                catch
                {

                }
            }

        }
        public void HideMe()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(
                    delegate ()
                    {
                        this.Hide();
                    }));

            } else {
                this.Hide();

            }
        }

        public void ShowMe()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new MethodInvoker(
                    delegate ()
                    {
                        this.Show();
                    }));

            }
            else {
                this.Show();

            }
        }

        private void LogView_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
    }
}
