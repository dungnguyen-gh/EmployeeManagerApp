using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace WpfApp2_Data
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly DataManager _dataManager = new DataManager();
        private ObservableCollection<Employee> _employees;
        private ObservableCollection<Employee> _filteredEmployees;

        private Employee? _selectedEmployee;

        private string? _selectedDepartment;

        private Employee? _editBackup;

        private bool _isEditingNew;

        private bool _suppressUnsavedCheck = false;

        private Employee? _newEmployeeDraft;

        private string? _searchText;
        private bool _isSearchActive;

        public ObservableCollection<Employee> Employees
        {
            get => _filteredEmployees;
            set { _filteredEmployees = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Departments { get; set; } = new ObservableCollection<string>();

        public Employee? SelectedEmployee
        {
            get => _selectedEmployee;
            set
            {
                if (value == _selectedEmployee) return;

                // Block if unsaved exist
                if (!_suppressUnsavedCheck && _selectedEmployee != null && HasUnsavedChanges())
                {
                    var dialog = new UnsavedChangesDialog { Owner = this };
                    dialog.ShowDialog();
                    // Do NOT change the employee until user handles unsaved data
                    OnPropertyChanged(nameof(SelectedEmployee));
                    return;
                }

                // Handle cancelling a new employee
                if (_isEditingNew && _newEmployeeDraft != null && value != _newEmployeeDraft)
                {
                    // User abandoned the new employee
                    _newEmployeeDraft = null;
                    _isEditingNew = false;
                }

                _selectedEmployee = value;

                // Clear editing flag if don't have the new draft
                if (value != _newEmployeeDraft)
                {
                    _isEditingNew = false;
                }

                if (_selectedEmployee != null)
                {
                    BackupSelected();
                }

                OnPropertyChanged();
            }
        }

        public string? SelectedDepartment
        {
            get => _selectedDepartment;
            set
            {
                if (value == _selectedDepartment) return;

                if (!_suppressUnsavedCheck && HasUnsavedChanges())
                {
                    var dialog = new UnsavedChangesDialog { Owner = this };
                    dialog.ShowDialog();
                    OnPropertyChanged(nameof(SelectedDepartment)); // Revert binding
                    return;
                }

                _selectedDepartment = value;
                OnPropertyChanged();

                SearchText = string.Empty;

                FilterEmployees();
            }
        }
        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;

                    // update clear button enabled state
                    IsSearchActive = !string.IsNullOrWhiteSpace(_searchText); 

                    OnPropertyChanged();
                    FilterEmployees();
                }
            }
        }
        public bool IsSearchActive
        {
            get => _isSearchActive;
            set
            {
                if (_isSearchActive != value)
                {
                    _isSearchActive = value;
                    OnPropertyChanged();
                }
            }
        }
        public MainWindow()
        {
            InitializeComponent();

            _employees = new ObservableCollection<Employee>(_dataManager.LoadEmployees()); // load data to employees
            _filteredEmployees = new ObservableCollection<Employee>(_employees);

            UpdateDepartments(); //initialize department

            DataContext = this;
        }
        private bool HasUnsavedChanges()
        {
            if (_isEditingNew) return true; // New employee have not saved yet
            if (_editBackup == null || SelectedEmployee == null) return false;

            return _editBackup.Department != SelectedEmployee.Department ||
                   _editBackup.Name != SelectedEmployee.Name ||
                   _editBackup.Email != SelectedEmployee.Email ||
                   _editBackup.Position != SelectedEmployee.Position ||
                   _editBackup.Salary != SelectedEmployee.Salary ||
                   _editBackup.StartDate != SelectedEmployee.StartDate;
        }
        private bool PromptUnsavedChanges()
        {
            var dialog = new UnsavedChangesDialog { Owner= this };
            var result = dialog.ShowDialog();

            return result == true;
        }
        private void BackupSelected()
        {
            if (_selectedEmployee != null)
            {
                _editBackup = new Employee
                {
                    Department = _selectedEmployee.Department,
                    Name = _selectedEmployee.Name,
                    Email = _selectedEmployee.Email,
                    Position = _selectedEmployee.Position,
                    Salary = _selectedEmployee.Salary,
                    StartDate = _selectedEmployee.StartDate
                };
            }
        }
        private void UpdateDepartments()
        {
            var uniqueDepartments = _employees
                .Select(e => e.Department)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            Departments.Clear();
            Departments.Add("All Departments");
            foreach (var dept in uniqueDepartments)
                Departments.Add(dept);

            if (string.IsNullOrEmpty(SelectedDepartment))
                SelectedDepartment = "All Departments";
        }
        private void FilterEmployees()
        {
            IEnumerable<Employee> filtered = _employees;

            // Filter by selected department
            if (!string.IsNullOrWhiteSpace(SelectedDepartment) && SelectedDepartment != "All Departments")
            {
                filtered = filtered.Where(e => e.Department == SelectedDepartment);
            }

            // Filter by search text (name)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string searchLower = SearchText.ToLower();
                filtered = filtered.Where(e => e.Name != null && e.Name.ToLower().Contains(searchLower));
            }

            Employees = new ObservableCollection<Employee>(filtered);

            if (_selectedEmployee != null && Employees.Contains(_selectedEmployee))
            {
                // Do nothing – keep selection
            }
            else
            {
                // Clear selection silently without triggering logic
                _suppressUnsavedCheck = true;
                _selectedEmployee = null;
                OnPropertyChanged(nameof(SelectedEmployee));
                _suppressUnsavedCheck = false;
            }
        }

        private void AddNew_Click(object sender, RoutedEventArgs e)
        {
            // If the user is editing an existing or new employee, show dialog
            if (_isEditingNew || (SelectedEmployee != null && HasUnsavedChanges()))
            {
                var dialog = new UnsavedChangesDialog
                {
                    Owner = this
                };

                dialog.ShowDialog();

                return;
            }
            // Mark editing
            _isEditingNew = true;

            // Create new employee draft
            _newEmployeeDraft = new Employee
            {
                Department = "Department",
                Name = "Name",
                Email = "email@email.com",
                Position = "Position",
                Salary = 1000,
                StartDate = DateTime.Today
            };

            // Add draft to the list
            _employees.Add(_newEmployeeDraft);

            // Ensure the department is set to show all employees
            _suppressUnsavedCheck = true;
            SelectedDepartment = "All Departments";
            _suppressUnsavedCheck = false;

            // Refresh filter to make sure draft is shown
            FilterEmployees();

            // Select new employee
            SelectedEmployee = _newEmployeeDraft;

        }
        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedEmployee == null) return;

            if (HasUnsavedChanges())
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Do you want to discard them and remove this employee?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNo
                );

                if (result != MessageBoxResult.Yes)
                    return;
            }
            else
            {
                var result = MessageBox.Show(
                    "Are you sure you want to remove this employee?",
                    "Confirm Remove",
                    MessageBoxButton.YesNo
                );

                if (result != MessageBoxResult.Yes)
                    return;
            }

            _suppressUnsavedCheck = true;
            _employees.Remove(SelectedEmployee);
            SelectedEmployee = null;
            _suppressUnsavedCheck = false;

            _dataManager.SaveEmployees(_employees); // Save to CSV

            UpdateDepartments();
            FilterEmployees();
        }
        private void SaveChanges_Click(object sender, RoutedEventArgs e)
        {
            if (_isEditingNew && _newEmployeeDraft != null)
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(_newEmployeeDraft.Department) ||
                    string.IsNullOrWhiteSpace(_newEmployeeDraft.Name) ||
                    string.IsNullOrWhiteSpace(_newEmployeeDraft.Email) ||
                    string.IsNullOrWhiteSpace(_newEmployeeDraft.Position))
                {
                    MessageBox.Show("Please fill in all required fields (Department, Name, Email, Position).",
                        "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // If it's not already in the main employee list, add it
                if (!_employees.Contains(_newEmployeeDraft))
                {
                    _employees.Add(_newEmployeeDraft);
                }

                // Clear editing state for new employee
                _isEditingNew = false;
                _selectedEmployee = _newEmployeeDraft;  // Set as current selected
                _newEmployeeDraft = null;

                BackupSelected();  // Save backup of new employee
            }
            else if (SelectedEmployee != null)
            {
                BackupSelected();  // Save backup of existing employee
            }

            // Save changes to data source
            _dataManager.SaveEmployees(_employees);

            // Notify user
            MessageBox.Show("Employees saved!");

            // Refresh UI
            UpdateDepartments();
            FilterEmployees();

            // Reselect saved employee if still present
            if (_selectedEmployee != null && _employees.Contains(_selectedEmployee))
            {
                _suppressUnsavedCheck = true;
                SelectedEmployee = _selectedEmployee;
                _suppressUnsavedCheck = false;
            }
            else
            {
                SelectedEmployee = null;
            }
            Keyboard.ClearFocus();
        }
        private void EmployeeListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var item = ItemsControl.ContainerFromElement
                (EmployeeListBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (item == null) return;

            var clickedEmployee = item.DataContext as Employee;
            if (clickedEmployee == null || clickedEmployee == SelectedEmployee) return;

            if (HasUnsavedChanges())
            {
                if (!PromptUnsavedChanges())
                {
                    e.Handled = true;
                }
            }
        }
        private void DepartmentComboBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (HasUnsavedChanges())
            {
                // Prevent ComboBox from opening
                e.Handled = true;
                
                var dialog = new UnsavedChangesDialog { Owner = this };
                dialog.ShowDialog();
            }
        }
        private void SearchTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (HasUnsavedChanges())
            {
                e.Handled = true;
                var dialog = new UnsavedChangesDialog { Owner = this };
                dialog.ShowDialog();
            }
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchText = string.Empty;
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? prop = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
