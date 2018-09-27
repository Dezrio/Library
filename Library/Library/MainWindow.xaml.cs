using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.SqlClient;
using System.Configuration;

namespace Library
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        interface ILogger
        {
            void Log (string L);
        }
        class TxtLogger : ILogger
        {
            private static readonly string Path = @"D:\Logs\";
            public void Log (string L)
            {
                string line = L + Environment.NewLine;
                try
                {
                    if (!Directory.Exists(Path))
                    {
                        Directory.CreateDirectory(Path);
                    }
                    if (!File.Exists(Path + "applog.txt"))
                    {
                        File.Create(Path + "applog.txt").Dispose();
                    } 
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                    return;
                }   
                using (StreamWriter writer = new StreamWriter(Path + "applog.txt", true))
                {
                    writer.Write(line);
                    writer.Flush();
                }
            }
        }
        class ConsoleLogger : ILogger
        {
            public void Log(string L)
            {
                AllocConsole();
                System.Console.WriteLine(L);
            }
        }
        class NullLogger : ILogger
        {
            public void Log(string L)
            {

            }
        }

        SqlConnection ConnectSQL = new SqlConnection(ConfigurationManager.ConnectionStrings["BookConnect"].ConnectionString);

        ILogger I = new NullLogger();

        public MainWindow()
        {
            InitializeComponent();
        }
        
        public class Books
        {
            public string Title { get; set; }
            public string Author { get; set; }
            public int Year { get; set; }
        }

        private void ConsoleLog_Checked(object sender, RoutedEventArgs e)
        {
            I = new ConsoleLogger();
        }

        private void TextLog_Checked(object sender, RoutedEventArgs e)
        {
            I = new TxtLogger(); 
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            //FreeConsole();
            int i = 0;
            if (string.IsNullOrEmpty(Title.Text)) { BrTitle.BorderBrush = new SolidColorBrush(Colors.Red); i++; }
            else { BrTitle.BorderBrush = new SolidColorBrush(Colors.Black); }
            if (string.IsNullOrEmpty(Author.Text)) { BrAuthor.BorderBrush = new SolidColorBrush(Colors.Red); i++; }
            else { BrAuthor.BorderBrush = new SolidColorBrush(Colors.Black); }
            if (string.IsNullOrEmpty(Year.Text)) { BrYear.BorderBrush = new SolidColorBrush(Colors.Red); i++; }
            else { BrYear.BorderBrush = new SolidColorBrush(Colors.Black); }
            if (i > 0)
            {
                //MessageBox.Show("Обязательное поле не заполнено");
                I.Log(DateTime.Now +": "+ "Обязательное поле не заполнено");
            }
            else
            {
                string sqlExpression = "INSERT INTO Books (Title, Author, Year) VALUES (@Title, @Author, @Year)";
                try
                {
                    ConnectSQL.Open();
                    SqlCommand command = new SqlCommand(sqlExpression, ConnectSQL);
                    SqlParameter TitleParam = new SqlParameter("@Title", Title.Text);
                    command.Parameters.Add(TitleParam);
                    SqlParameter AuthorParam = new SqlParameter("@Author", Author.Text);
                    command.Parameters.Add(AuthorParam);
                    SqlParameter YearParam = new SqlParameter("@Year", Year.Text);
                    command.Parameters.Add(YearParam);
                    //MessageBox.Show(command.CommandText);
                    I.Log(DateTime.Now + " - " + command.CommandText);
                    command.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    //MessageBox.Show(ex.Message);
                    I.Log(DateTime.Now + " - " + ex.Message);
                }
                finally
                {
                    List<Books> newBooks = new List<Books> {};
                    sqlExpression = "SELECT * FROM Books";
                    SqlCommand read = new SqlCommand(sqlExpression, ConnectSQL);
                    SqlDataReader reader = read.ExecuteReader();
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            newBooks.Add(new Books { Title = reader.GetString(1), Author = reader.GetString(2), Year = (Int32)reader.GetValue(3) });
                        }
                    }
                    SQLGrid.ItemsSource = newBooks;
                    ConnectSQL.Close();
                }
            }
        }

        private void SQLGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            e.Cancel = true;
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            string sqlExpression;
            switch (FilterBox.Text)
            {
                case "Название" :
                    sqlExpression = "SELECT * FROM Books WHERE Title LIKE @Filter";
                    break;
                case "Автор":
                    sqlExpression = "SELECT * FROM Books WHERE Author LIKE @Filter";
                    break;
                case "Год":
                    sqlExpression = "SELECT * FROM Books WHERE Year LIKE @Filter";
                    break;
                default: 
                    sqlExpression = "SELECT * FROM Books";
                    //MessageBox.Show("Не выбран параметр фильтрации!");
                    I.Log(DateTime.Now + " - " + "Не выбран параметр фильтрации!");
                    break;
            }
            using (SqlConnection ConnectSQL = new SqlConnection(ConfigurationManager.ConnectionStrings["BookConnect"].ConnectionString))
            {
                List<Books> newFilter = new List<Books> {};
                ConnectSQL.Open();
                SqlCommand Fltr = new SqlCommand(sqlExpression, ConnectSQL);
                SqlParameter FilterParameter = new SqlParameter("@Filter", "%" +FilterText.Text + "%");
                Fltr.Parameters.Add(FilterParameter);
                SqlDataReader readFltr = Fltr.ExecuteReader();
                //MessageBox.Show(Fltr.CommandText);
                I.Log(DateTime.Now + " - " + Fltr.CommandText);
                while (readFltr.Read())
                {
                    newFilter.Add(new Books { Title = readFltr.GetString(1), Author = readFltr.GetString(2), Year = (Int32)readFltr.GetValue(3) });
                }
                SQLGrid.ItemsSource = newFilter;
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            string sqlExpression = "DELETE Books WHERE (Title = '' OR Author = '' OR Year = 0)";   
            using (SqlConnection ConnectSQL = new SqlConnection(ConfigurationManager.ConnectionStrings["BookConnect"].ConnectionString))
            {
                ConnectSQL.Open();
                SqlCommand Clear = new SqlCommand(sqlExpression, ConnectSQL);
                Clear.ExecuteNonQuery();
            }
        }
        [DllImport("kernel32")]
        //[return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
        [DllImport("kernel32")]
        private static extern bool FreeConsole();
        [DllImport("kernel32")]
        private static extern bool AttachConsole(int processid);
    }
}
