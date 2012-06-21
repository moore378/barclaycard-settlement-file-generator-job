using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;

namespace Cctm
{
    public partial class AboutForm : Form
    {
        private string launchedAt;
        public AboutForm(string launchedAt)
        {
            InitializeComponent();
            this.launchedAt = launchedAt;
        }

       /// <summary>
       /// Gets the assembly copyright.
       /// </summary>
       /// <value>The assembly copyright.</value>
       public string AssemblyCopyright
       {
           get
           {
               // Get all Copyright attributes on this assembly
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                // If there aren't any Copyright attributes, return an empty string
                if (attributes.Length == 0)
                    return "";
                // If there is a Copyright attribute, return its value
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

       private string versionNumber()
       {
           Assembly assembly = Assembly.GetExecutingAssembly();
           AssemblyName assemblyName = assembly.GetName();
           Version version = assemblyName.Version;
           return version.ToString();
       }
        
        private void AboutForm_Load(object sender, EventArgs e)
        {
            listView2.Items.Add("Product").SubItems.Add(ProductName);
            listView2.Items.Add("Version").SubItems.Add(versionNumber());
            listView2.Items.Add("Copyright").SubItems.Add(AssemblyCopyright);
            listView2.Items.Add("Company").SubItems.Add(CompanyName);
            listView2.Items.Add("Version").SubItems.Add(ProductVersion);
            listView2.Items.Add("Launched at").SubItems.Add(launchedAt);
        }

        private void AboutForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}
