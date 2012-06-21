namespace Cctm
{
    partial class EventLogForm
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
            this.txtEventLog = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // txtEventLog
            // 
            this.txtEventLog.CausesValidation = false;
            this.txtEventLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtEventLog.Location = new System.Drawing.Point(0, 0);
            this.txtEventLog.Multiline = true;
            this.txtEventLog.Name = "txtEventLog";
            this.txtEventLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtEventLog.Size = new System.Drawing.Size(629, 322);
            this.txtEventLog.TabIndex = 19;
            this.txtEventLog.WordWrap = false;
            // 
            // EventLogForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(629, 322);
            this.Controls.Add(this.txtEventLog);
            this.Name = "EventLogForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Event Log";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.EventLogForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtEventLog;
    }
}