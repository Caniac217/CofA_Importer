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

namespace CofA_Importer
{
    /// <summary>
    /// Interaction logic for LogPrompt.xaml
    /// </summary>
    public partial class LogPrompt : Window
    {
        public bool UserWantsToOpen { get; private set; } = false;
        public bool SuppressPrompt { get; private set; } = false;
        public string FileName { get; set; } = string.Empty;
        public bool DontAskAgain { get; private set; }
        public string LogFilePath { get; set; } = string.Empty;


        public LogPrompt()
        {
            InitializeComponent();
        }

        public LogPrompt(string logFilePath)
        {
            InitializeComponent();
            this.LogFilePath = logFilePath;
        }

        private void Yes_Click(object sender, RoutedEventArgs e)
        {
            UserWantsToOpen = true;
            DontAskAgain = chkDontAskAgain.IsChecked == true;
            DialogResult = true;
            Close();
        }

        private void No_Click(object sender, RoutedEventArgs e)
        {
            UserWantsToOpen = false;
            DontAskAgain = chkDontAskAgain.IsChecked == true;
            DialogResult = false;
            Close();
        }
    }

}
