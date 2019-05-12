using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;


namespace SQLTest
{
    public partial class ClassManager : Form
    {
    	//These variables are used to calculate GPA to be displayed in the application
        private double totalGradePoint;
        private int creditHours;
        private double gpa;
	private SqlConnection connection;
	//Form that shows classes prereqs haven't been met for
        private Form unavailableForm;

        public ClassManager()
        {
            unavailableForm = new UnavailableClasses(this);
            InitializeComponent();
            connection = new SqlConnection();
	    connection.ConnectionString =
                    @"Data Source=localhost;Initial Catalog=master;Integrated Security=True";
            using (connection) //Create database if it doesn't exist
            {
                connection.Open();
                using (SqlCommand updateCommand = new SqlCommand(@"IF  NOT EXISTS (SELECT * FROM sys.objects 
                                        WHERE object_id = OBJECT_ID(N'[dbo].[ClassTable]') AND type in (N'U'))
                                        BEGIN USE [master]
                                        SET ANSI_NULLS ON

                                        SET QUOTED_IDENTIFIER ON

                                        CREATE TABLE [dbo].[ClassTable](
	                                    [ClassName] [nchar](20) NOT NULL,
	                                    [GradePoint] [decimal](18, 0) NULL,
	                                    [CreditHours] [int] NOT NULL,
	                                    [Completed] [bit] NOT NULL,
	                                    [PreReq] [nchar](20) NULL,
                                        CONSTRAINT [PK_ClassTable] PRIMARY KEY CLUSTERED 
                                        (
	                                    [ClassName] ASC
                                        )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                                        ) ON [PRIMARY]

                                        ALTER TABLE [dbo].[ClassTable]  WITH CHECK ADD  CONSTRAINT [FK_ClassTable_ClassTable] FOREIGN KEY([PreReq])
                                        REFERENCES [dbo].[ClassTable] ([ClassName])

                                        ALTER TABLE [dbo].[ClassTable] CHECK CONSTRAINT [FK_ClassTable_ClassTable] END", connection))
                {

                    updateCommand.ExecuteNonQuery();
                }


            }
        }

        public void UpdateTable() //Updates dataGridView tables to database values
        {
            using (connection)
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("SELECT ClassName, GradePoint, CreditHours FROM ClassTable WHERE Completed = '1'", connection))
                {
                    SqlDataAdapter da = new SqlDataAdapter(command);
                    DataTable table = new DataTable();
                    da.Fill(table);
                    dataGridView1.DataSource = new BindingSource(table, null);

                }
                using (SqlCommand command = new SqlCommand("SELECT ClassName, CreditHours, PreReq FROM ClassTable WHERE Completed = '0' AND (PreReq is null OR PreReq IN (SELECT ClassName FROM ClassTable WHERE Completed = '1'))", connection))
                {
                    SqlDataAdapter da = new SqlDataAdapter(command);
                    DataTable table = new DataTable();
                        da.Fill(table);
                    dataGridView2.DataSource = new BindingSource(table, null);

                }

            }

            totalGradePoint = 0;
            creditHours = 0;
            for (int i = 0; i < dataGridView1.RowCount; i++)
            {
                totalGradePoint = totalGradePoint + (double.Parse(dataGridView1.Rows[i].Cells[1].Value.ToString()) * int.Parse(dataGridView1.Rows[i].Cells[2].Value.ToString()));
                creditHours = creditHours + int.Parse(dataGridView1.Rows[i].Cells[2].Value.ToString());
            }

            gpa = totalGradePoint / creditHours;
            label1.Text = "Average GPA: " + Math.Round(gpa,2);
            label2.Text = "Credit Hours: " + creditHours;

        }
        private void AddDialogButton_Click(object sender, EventArgs e) //Prompts user to add class from available to taken
        {
            try
            {
                var form = new CompleteClassDialog(dataGridView2.CurrentRow.Cells[0].Value.ToString(), this);
                form.Owner = this;
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(this.Location.X + (this.Width - form.Width) / 2, this.Location.Y + (this.Height - form.Height) / 2);
                form.Show(this);
                this.Enabled = false;
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("Class list is empty", "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public void completeClass(double grade, string className) //Completes class with given name and assigns given grade
        {
            using (connection)
            {
                connection.Open();
                using (SqlCommand updateCommand = new SqlCommand("UPDATE ClassTable SET GradePoint = '" + grade + "', Completed = '1' WHERE Classname = '"+ className +"'", connection))
                {
                    updateCommand.ExecuteNonQuery();
                }


            }

            this.Enabled = true;
        }

        private void RemoveButton_Click(object sender, EventArgs e) //Removes class from taken grid to available grid
        {
            using (connection)
            {
                connection.Open();
                try
                {
                    using (SqlCommand command =
                        new SqlCommand("UPDATE ClassTable SET GradePoint = NULL, Completed = '0' WHERE ClassName = @0", connection))
                    {
                        command.Parameters.Add(new SqlParameter("@0", dataGridView1.CurrentRow.Cells[0].Value));
                        command.ExecuteNonQuery();

                    }
                }
                catch (NullReferenceException)
                {
                    MessageBox.Show("Class list is empty", "Error", MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            UpdateTable();
        }

        private void ClassManager_Load(object sender, EventArgs e)
        {
            UpdateTable();
        }

        private void ImportButton_Click(object sender, EventArgs e) //Imports new class list from a text file
        {
            try
            {
                using (openFileDialog1)
                {
                    if (openFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        var filePath = openFileDialog1.FileName;
                        var fileStream = openFileDialog1.OpenFile();
                        List<string[]> words = new List<string[]>();
                        String line = null;
                        string[] brokenline;
                        using (StreamReader reader = new StreamReader(fileStream))
                        {
                            while (!String.IsNullOrEmpty(line = reader.ReadLine()))
                            {
                                brokenline = line.Split(' ');
                                words.Add(brokenline);
                            }

                            
                        }
                            using (connection)
                            {
                                connection.Open();
                                using (var deleteCommand =
                                    new SqlCommand(
                                        "DELETE FROM ClassTable",
                                        connection))
                                {
                                    deleteCommand.ExecuteNonQuery();
                                }
                                for (int i = 0; i < words.Count; i ++)
                                {
                                    string preReq = "null";
                                    if (words[i].Length == 3)
                                        preReq = words[i][2];
                                    if (words[i][0].Length < 21 && preReq.Length < 21 && int.Parse(words[i][1]) > 0)
                                    {
                                        var insertCommand = new SqlCommand();
                                        if (preReq.Equals("null")) insertCommand = new SqlCommand("INSERT INTO ClassTable (ClassName, CreditHours, Completed, PreReq) VALUES (@0, @1,'0', null)", connection);
                                        else insertCommand = new SqlCommand("INSERT INTO ClassTable (ClassName, CreditHours, Completed, PreReq) VALUES (@0, @1,'0', @2)", connection);
                                    using (insertCommand)
                                        {
                                            insertCommand.Parameters.Add(new SqlParameter("@0", words[i][0]));
                                            insertCommand.Parameters.Add(new SqlParameter("@1", words[i][1]));
                                            insertCommand.Parameters.Add(new SqlParameter("@2", preReq));
                                            insertCommand.ExecuteNonQuery();
                                        }

                                    }
                                }
                            
                        }

                    }
                }
                UpdateTable();
            }
            catch (Exception f)
            {
                MessageBox.Show(f.ToString(), "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void UnavailableButton_Click(object sender, EventArgs e) //List classes not available to take
        {
            if (unavailableForm.Created) unavailableForm.Close();
            unavailableForm = new UnavailableClasses(this);
            unavailableForm.Owner = this;
            unavailableForm.StartPosition = FormStartPosition.Manual;
            unavailableForm.Location = new Point(this.Location.X + (this.Width - unavailableForm.Width) / 2, this.Location.Y + (this.Height - unavailableForm.Height) / 2);
            unavailableForm.Show(this);
        }
    }
}
