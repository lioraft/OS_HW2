using System.Windows.Forms;
using System;
using System.Windows.Forms;

namespace SpreadsheetApp
{
    public partial class Form1 : Form
    {
        private SharableSpreadSheet spreadsheet; //the spreadsheet object

        public Form1()
        {
            InitializeComponent();
            //create a new spreadsheet
            spreadsheet = new SharableSpreadSheet(10, 10); //10 rows, 10 columns

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Initialize the DataGridView control
            dataGridView1.ColumnCount = 11; // Set the number of columns (10 subjects + 1 for student names)
            dataGridView1.RowCount = 11; // Set the number of rows (10 students + 1 for column headers)

            // Set column headers
            dataGridView1.Columns[0].HeaderText = "Student Names/ subjects:"; // Set the header for the first column


            string[] subjects = new string[] { "Math", "History", "Science", "English", "Geography", "Art", "Music", "Physical Education", "Computer Science", "Foreign Language" };

            for (int subjectIndex = 1; subjectIndex <= 10; subjectIndex++)
            {
                dataGridView1.Columns[subjectIndex].HeaderText = subjects[subjectIndex - 1]; // Set headers for subject columns
            }


            // Define sample student names
            string[] studentNames = new string[] { "John", "Emma", "Michael", "Sophia", "William", "Olivia", "James", "Ava", "Benjamin", "Isabella" };

            // Assign student names and sample grades to the cells
            for (int studentRow = 0; studentRow < 10; studentRow++)
            {
                dataGridView1.Rows[studentRow].Cells[0].Value = studentNames[studentRow]; // Set student names in the first column

                for (int subjectCol = 1; subjectCol <= 10; subjectCol++)
                {
                    // Generate random grade between 0 and 100
                    int grade = new Random().Next(101);
                    dataGridView1.Rows[studentRow].Cells[subjectCol].Value = grade.ToString(); // Set the grade in the corresponding cell
                }
            }
            // dataGridView1.TopLeftHeaderCell.Value = "rows_subject\\columns_subject";
            dataGridView1.RowHeadersVisible = false; // Hide the row headers
        }





        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {


        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void average_Click(object sender, EventArgs e)
        {

        }
    }
}