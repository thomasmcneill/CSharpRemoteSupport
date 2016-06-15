namespace RemoteSupportClient
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.button_Start = new System.Windows.Forms.Button();
            this.textBox_FrameCounter = new System.Windows.Forms.TextBox();
            this.textBox_TempPath = new System.Windows.Forms.TextBox();
            this.textBox_Ticks = new System.Windows.Forms.TextBox();
            this.textBox_Log = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // button_Start
            // 
            this.button_Start.Location = new System.Drawing.Point(135, 81);
            this.button_Start.Name = "button_Start";
            this.button_Start.Size = new System.Drawing.Size(75, 23);
            this.button_Start.TabIndex = 0;
            this.button_Start.Text = "Start";
            this.button_Start.UseVisualStyleBackColor = true;
            this.button_Start.Click += new System.EventHandler(this.button_Start_Click);
            // 
            // textBox_FrameCounter
            // 
            this.textBox_FrameCounter.Location = new System.Drawing.Point(30, 156);
            this.textBox_FrameCounter.Name = "textBox_FrameCounter";
            this.textBox_FrameCounter.Size = new System.Drawing.Size(100, 20);
            this.textBox_FrameCounter.TabIndex = 1;
            // 
            // textBox_TempPath
            // 
            this.textBox_TempPath.Location = new System.Drawing.Point(30, 182);
            this.textBox_TempPath.Name = "textBox_TempPath";
            this.textBox_TempPath.Size = new System.Drawing.Size(100, 20);
            this.textBox_TempPath.TabIndex = 2;
            this.textBox_TempPath.TextChanged += new System.EventHandler(this.textBox_TempPath_TextChanged);
            // 
            // textBox_Ticks
            // 
            this.textBox_Ticks.Location = new System.Drawing.Point(30, 208);
            this.textBox_Ticks.Name = "textBox_Ticks";
            this.textBox_Ticks.Size = new System.Drawing.Size(100, 20);
            this.textBox_Ticks.TabIndex = 3;
            // 
            // textBox_Log
            // 
            this.textBox_Log.Location = new System.Drawing.Point(30, 247);
            this.textBox_Log.Multiline = true;
            this.textBox_Log.Name = "textBox_Log";
            this.textBox_Log.Size = new System.Drawing.Size(242, 234);
            this.textBox_Log.TabIndex = 4;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 493);
            this.Controls.Add(this.textBox_Log);
            this.Controls.Add(this.textBox_Ticks);
            this.Controls.Add(this.textBox_TempPath);
            this.Controls.Add(this.textBox_FrameCounter);
            this.Controls.Add(this.button_Start);
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button_Start;
        private System.Windows.Forms.TextBox textBox_FrameCounter;
        private System.Windows.Forms.TextBox textBox_TempPath;
        private System.Windows.Forms.TextBox textBox_Ticks;
        private System.Windows.Forms.TextBox textBox_Log;
    }
}

