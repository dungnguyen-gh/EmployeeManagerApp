using System;
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
using System.Windows.Shapes;

namespace WpfApp2_Data
{
    /// <summary>
    /// Interaction logic for UnsavedChangesDialog.xaml
    /// </summary>
    public partial class UnsavedChangesDialog : Window
    {
        public UnsavedChangesDialog()
        {
            InitializeComponent();
        }
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        //prevent closing the window
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (this.DialogResult != true)
            {
                e.Cancel = true;
            }
            base.OnClosing(e);
        }
    }
}
