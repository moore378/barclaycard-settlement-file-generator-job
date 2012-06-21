namespace Rtcc
{
    partial class RtccForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RtccForm));
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripDropDownButton1 = new System.Windows.Forms.ToolStripDropDownButton();
            this.importantLoggingOnlyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.detailedLoggingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox1.Location = new System.Drawing.Point(13, 13);
            this.textBox1.Margin = new System.Windows.Forms.Padding(4);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(860, 325);
            this.textBox1.TabIndex = 1;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripDropDownButton1});
            this.statusStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
            this.statusStrip1.Location = new System.Drawing.Point(0, 342);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(891, 26);
            this.statusStrip1.TabIndex = 6;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(70, 21);
            this.toolStripStatusLabel1.Text = "Starting...";
            // 
            // toolStripDropDownButton1
            // 
            this.toolStripDropDownButton1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripDropDownButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripDropDownButton1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.importantLoggingOnlyToolStripMenuItem,
            this.detailedLoggingToolStripMenuItem});
            this.toolStripDropDownButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripDropDownButton1.Image")));
            this.toolStripDropDownButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripDropDownButton1.Name = "toolStripDropDownButton1";
            this.toolStripDropDownButton1.Size = new System.Drawing.Size(167, 24);
            this.toolStripDropDownButton1.Text = "Detailed logging only";
            // 
            // importantLoggingOnlyToolStripMenuItem
            // 
            this.importantLoggingOnlyToolStripMenuItem.Checked = true;
            this.importantLoggingOnlyToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.importantLoggingOnlyToolStripMenuItem.Name = "importantLoggingOnlyToolStripMenuItem";
            this.importantLoggingOnlyToolStripMenuItem.Size = new System.Drawing.Size(237, 24);
            this.importantLoggingOnlyToolStripMenuItem.Text = "Important Logging Only";
            this.importantLoggingOnlyToolStripMenuItem.Click += new System.EventHandler(this.importantLoggingOnlyToolStripMenuItem_Click);
            // 
            // detailedLoggingToolStripMenuItem
            // 
            this.detailedLoggingToolStripMenuItem.Name = "detailedLoggingToolStripMenuItem";
            this.detailedLoggingToolStripMenuItem.Size = new System.Drawing.Size(237, 24);
            this.detailedLoggingToolStripMenuItem.Text = "Detailed logging";
            this.detailedLoggingToolStripMenuItem.Click += new System.EventHandler(this.detailedLoggingToolStripMenuItem_Click);
            // 
            // RtccForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(891, 368);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.textBox1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "RtccForm";
            this.Text = "RTCC";
            this.Load += new System.EventHandler(this.RtccForm_Load);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripDropDownButton toolStripDropDownButton1;
        private System.Windows.Forms.ToolStripMenuItem detailedLoggingToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importantLoggingOnlyToolStripMenuItem;
    }
}

