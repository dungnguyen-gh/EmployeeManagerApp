using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using System.Windows;

namespace WpfApp2_Data
{
    class DataManager
    {
        //Handle Data Logic
        private const string FilePath = "employees.csv";
        //Read from CSV file
        public List<Employee> LoadEmployees()
        {
            if (!File.Exists(FilePath))
            {
                MessageBox.Show("CSV file not found!");
                return new List<Employee>();
            }

            return File.ReadAllLines(FilePath)
                .Skip(1) //Skip header
                .Select(line =>
                {
                    var parts = line.Split(',');
                    return new Employee
                    {
                        Department = parts[0],
                        Name = parts[1],
                        Email = parts[2],
                        Position = parts[3],
                        Salary = double.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out var salary) ? salary : 0,
                        StartDate = DateTime.TryParse(parts[5], out var date) ? date : DateTime.MinValue
                    };
                }).ToList();
        }
        //Write to CSV file
        public void SaveEmployees(IEnumerable<Employee> employees)
        {
            var lines = new List<string>
            {
                "Department,Name,Email,Position,Salary,StartDate"
            };

            lines.AddRange(employees.Select(e =>
                $"{e.Department},{e.Name},{e.Email},{e.Position},{e.Salary},{e.StartDate:yyyy-MM-dd}"));

            File.WriteAllLines(FilePath, lines);
        }
    }
}
