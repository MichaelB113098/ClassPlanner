using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace SQLTest
{
    public partial class UnavailableClasses : Form
    {
        private ClassManager _parent;

        public UnavailableClasses(ClassManager parent)
        {
            _parent = parent;
            InitializeComponent();
        }

        private void UnavailableClasses_Load(object sender, EventArgs e)
        {
            var connection = new SqlConnection();
            using (connection)
            {
                connection.ConnectionString =
                    @"Data Source=localhost;Initial Catalog=master;Integrated Security=True";
                connection.Open();
                using (var command =
                    new SqlCommand(
                        "SELECT ClassName, CreditHours, PreReq FROM ClassTable WHERE Completed = '0' AND PreReq IN (SELECT ClassName FROM ClassTable WHERE Completed = '0')",
                        connection))
                {
                    var da = new SqlDataAdapter(command);
                    var table = new DataTable();
                    da.Fill(table);
                    dataGridView2.DataSource = new BindingSource(table, null);
                }
            }
        }

        private void UnavailableClasses_FormClosed(object sender, FormClosedEventArgs e)
        {
        }
    }
}