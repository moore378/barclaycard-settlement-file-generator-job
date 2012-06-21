using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Cctm
{
    public partial class WaitForFinishForm : Form
    {
        public WaitForFinishForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Call this to specify how many transactions the form is still waiting for
        /// </summary>
        public void WaitCountChanged(int waitCount)
        {
            int position = progressBar1.Maximum - waitCount;
            if (position < progressBar1.Minimum)
                position = progressBar1.Minimum;
            if (position > progressBar1.Maximum)
                position = progressBar1.Maximum;
            this.Invoke(new Action(() =>
                {
                    progressBar1.Value = position;
                }));

            if (waitCount == 0)
            {
                this.Invoke(new Action(() =>
                    {
                        this.DialogResult = System.Windows.Forms.DialogResult.OK;
                        this.Close();
                    }
                ));
            }
        }

        public int MaxWaitCount 
        { 
            get { return maxWaitCount; } 
            set 
            {
                maxWaitCount = value;
                progressBar1.Maximum = value;
            } 
        }
        private int maxWaitCount;
    }
}
