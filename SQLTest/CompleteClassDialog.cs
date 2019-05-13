using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SQLTest
{
    public partial class CompleteClassDialog : Form
    {
        private readonly ClassManager parent;
        private readonly string classname;
        public CompleteClassDialog(string className, ClassManager parent)
        {
            InitializeComponent();
            label1.Text = "Enter grade for " + className;
            this.parent = parent;
            this.classname = className;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            double gradePoint = 0;
            switch (comboBox1.Text)
            {
                case "A":
                    gradePoint = 4.0;
                    break;
                case "B":
                    gradePoint = 3.0;
                    break;
                case "C":
                    gradePoint = 2.0;
                    break;
                case "D":
                    gradePoint = 1.0;
                    break;
                case "F":
                    gradePoint = 0;
                    break;
                default:
                    return;
            }
            parent.CompleteClass(gradePoint, classname);
            parent.UpdateTable();
            this.Close();



        }

        private void CompleteClassDialog_Load(object sender, EventArgs e)
        {

        }

        private void CompleteClassDialog_FormClosed(object sender, FormClosedEventArgs e)
        {
            parent.Enabled = true;
        }
    }
}
