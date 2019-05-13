using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SQLTest
{
    public partial class ClassManager : Form
    {
        //Sql Connection Variable
        private readonly SqlConnection _connection;
        private int _creditHours;

        private double _gpa;



        //These variables are used to calculate GPA to be displayed in the application
        private double _totalGradePoint;

        //Form that shows classes prereqs haven't been met for
        private Form _unavailableForm;

        private readonly string ConnectionString =
            @"Data Source=localhost;Initial Catalog=master;Integrated Security=True";

        public ClassManager()
        {
            _unavailableForm = new UnavailableClasses(this);
            InitializeComponent();
            //Set Connection to new SQLConnection, set string to local SQL master database
            _connection = new SqlConnection();


            using (_connection) //Create database if it doesn't exist
            {
                _connection.ConnectionString = ConnectionString;
                _connection.Open();
                //SQL code to create table if it doesnt exist
                using (var updateCommand = new SqlCommand(@"IF  NOT EXISTS (SELECT * FROM sys.objects 
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

                                        ALTER TABLE [dbo].[ClassTable] CHECK CONSTRAINT [FK_ClassTable_ClassTable] END",
                    _connection))
                {
                    updateCommand.ExecuteNonQuery();
                }
            }
        }

        public void UpdateTable() //Updates dataGridView tables to database values
        {
            using (_connection)
            {
                _connection.ConnectionString = ConnectionString;
                _connection.Open();
                //Completed classes
                using (var command =
                    new SqlCommand("SELECT ClassName, GradePoint, CreditHours FROM ClassTable WHERE Completed = '1'",
                        _connection))
                {
                    var da = new SqlDataAdapter(command);
                    var table = new DataTable();
                    da.Fill(table);
                    dataGridView1.DataSource = new BindingSource(table, null);
                }

                //Uncompleted classes where prereqs have been completed
                using (var command =
                    new SqlCommand(
                        "SELECT ClassName, CreditHours, PreReq FROM ClassTable WHERE Completed = '0' AND (PreReq is null OR PreReq IN (SELECT ClassName FROM ClassTable WHERE Completed = '1'))",
                        _connection))
                {
                    var da = new SqlDataAdapter(command);
                    var table = new DataTable();
                    da.Fill(table);
                    dataGridView2.DataSource = new BindingSource(table, null);
                }
            }

            //Calculate GPA and credit hours, display it under table
            _totalGradePoint = 0;
            _creditHours = 0;
            for (var i = 0; i < dataGridView1.RowCount; i++)
            {
                _totalGradePoint = _totalGradePoint + double.Parse(dataGridView1.Rows[i].Cells[1].Value.ToString()) *
                                   int.Parse(dataGridView1.Rows[i].Cells[2].Value.ToString());
                _creditHours = _creditHours + int.Parse(dataGridView1.Rows[i].Cells[2].Value.ToString());
            }

            _gpa = _totalGradePoint / _creditHours;
            label1.Text = "Average GPA: " + Math.Round(_gpa, 2);
            label2.Text = "Credit Hours: " + _creditHours;
        }

        //Dialog to complete class, dialog simply asks what grade the user recieved, CompleteClass form then calls the completeClass method
        private void
            AddDialogButton_Click(object sender, EventArgs e) //Prompts user to add class from available to taken
        {
            try
            {
                if (dataGridView2.CurrentRow != null)
                {
                    var form = new CompleteClassDialog(dataGridView2.CurrentRow.Cells[0].Value.ToString(), this)
                    {
                        Owner = this, StartPosition = FormStartPosition.Manual
                    };
                    form.Location = new Point(Location.X + (Width - form.Width) / 2,
                        Location.Y + (Height - form.Height) / 2);
                    form.Show(this);
                }

                Enabled = false;
            }
            catch (NullReferenceException)
            {
                MessageBox.Show("Class list is empty", "Error", MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public void
            CompleteClass(double grade, string className) //Completes class with given name and assigns given grade
        {
            using (_connection)
            {
                _connection.ConnectionString = ConnectionString;
                _connection.Open();
                using (var updateCommand =
                    new SqlCommand(
                        "UPDATE ClassTable SET GradePoint = '" + grade + "', Completed = '1' WHERE Classname = '" +
                        className + "'", _connection))
                {
                    updateCommand.ExecuteNonQuery();
                }
            }

            Enabled = true;
        }

        private void RemoveButton_Click(object sender, EventArgs e) //Removes class from taken grid to available grid
        {
            using (_connection)
            {
                _connection.ConnectionString = ConnectionString;
                _connection.Open();
                using (var command =
                    new SqlCommand("UPDATE ClassTable SET GradePoint = NULL, Completed = '0' WHERE ClassName = @0",
                        _connection))
                {
                    if (dataGridView1.CurrentRow != null)
                    {
                        command.Parameters.Add(new SqlParameter("@0", dataGridView1.CurrentRow.Cells[0].Value));
                        command.ExecuteNonQuery();
                    }
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
                        var words = new List<string[]>();
                        using (var reader = new StreamReader(fileStream))
                        {
                            string line = null;
                            while (!string.IsNullOrEmpty(line = reader.ReadLine()))
                            {
                                var brokenLine = line.Split(' ');
                                words.Add(brokenLine);
                            }
                        }

                        // insert all classes into fresh table, using parameters to avoid injection
                        using (_connection)
                        {
                            _connection.ConnectionString = ConnectionString;
                            _connection.Open();
                            using (var deleteCommand =
                                new SqlCommand(
                                    "DELETE FROM ClassTable",
                                    _connection))
                            {
                                deleteCommand.ExecuteNonQuery();
                            }

                            foreach (var t in words)
                            {
                                var preReq = "null";
                                if (t.Length == 3)
                                    preReq = t[2];
                                if (t[0].Length >= 21 || preReq.Length >= 21 || int.Parse(t[1]) <= 0) continue;
                                var insertCommand = preReq.Equals("null")
                                    ? new SqlCommand(
                                        "INSERT INTO ClassTable (ClassName, CreditHours, Completed, PreReq) VALUES (@0, @1,'0', null)",
                                        _connection)
                                    : new SqlCommand(
                                        "INSERT INTO ClassTable (ClassName, CreditHours, Completed, PreReq) VALUES (@0, @1,'0', @2)",
                                        _connection);
                                using (insertCommand)
                                {
                                    insertCommand.Parameters.Add(new SqlParameter("@0", t[0]));
                                    insertCommand.Parameters.Add(new SqlParameter("@1", t[1]));
                                    insertCommand.Parameters.Add(new SqlParameter("@2", preReq));
                                    insertCommand.ExecuteNonQuery();
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

        //Open unavailable class form
        private void UnavailableButton_Click(object sender, EventArgs e) //List classes not available to take
        {
            if (_unavailableForm.Created) _unavailableForm.Close();
            _unavailableForm = new UnavailableClasses(this) {Owner = this, StartPosition = FormStartPosition.Manual};
            _unavailableForm.Location = new Point(Location.X + (Width - _unavailableForm.Width) / 2,
                Location.Y + (Height - _unavailableForm.Height) / 2);
            _unavailableForm.Show(this);
        }
    }
}
