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
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace Make_Me_Admin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Client client = new Client();
        // private string user = Environment.UserName;
        private string user = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

        public bool bCheckCanBecomeAdmin { get; set; }
        
        // For testing
        // private string user = @"DESKTOP-IB70NP4\Johan";

        public MainWindow()
        {
            InitializeComponent();
            string[] args = Environment.GetCommandLineArgs();
            
            if (args.Length == 2 && args[1].Equals("--shortcut"))
            {
                string StartMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
                string shortcutLocation = System.IO.Path.Combine(StartMenuPath, "Programs", this.Title + ".lnk");
                SilentCheckCanBecomeAdmin();
                
                if (this.bCheckCanBecomeAdmin)
                {
                    CreateShortcut(shortcutLocation, "C:\\Program Files\\PLS\\Make Me Admin Client\\Make Me Admin.exe");
                }
                else
                {
                    System.IO.File.Delete(shortcutLocation);
                }

                this.Close();
                Environment.Exit(0);
            }
            else
            {
                
                CheckCanBecomeAdmin();
            }
        }

        private static void CreateShortcut(string shortcutLocation, string targetFileLocation)
        {
            
            if (!System.IO.File.Exists(shortcutLocation))
            {
                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(shortcutLocation);
                shortcut.TargetPath = targetFileLocation;
                shortcut.Save();
            }
        }

        private void ControlsEnabled(bool enabled)
        {
            bCancel.IsEnabled = enabled;
            bOk.IsEnabled = enabled;
            tbTwofactor.IsEnabled = enabled;
            cbExpire.IsEnabled = enabled;
        }

        private void bOk_Click(object sender, RoutedEventArgs e)
        {
            AddAdmin();
        }

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Environment.Exit(0);
        }

        private void TBTwoFactor_KeyDown(object sender, KeyEventArgs e)
        {
            /*
             * If input is not a number we say it was handled, this way it wont end up in the textbox
             */
            if ((e.Key < Key.D0 || e.Key > Key.D9) && (e.Key < Key.NumPad0 || e.Key > Key.NumPad9))
            {
                e.Handled = true;
            }
            /*
             * Our code can be six characters long
             */
            if (tbTwofactor.Text.Length >= 6)
            {
                e.Handled = true;
            }
            // Validate length = 6?
            if (e.Key == Key.Return && tbTwofactor.Text.Length == 6)
            {
                AddAdmin();
            }
        }

        private async void SilentCheckCanBecomeAdmin()
        {
            this.bCheckCanBecomeAdmin = false;
            // AdminResult r = new AdminResult();
            try
            {
                var r = Task.Run(() => client.CheckAdmin(user)).Result;
                
                if (r.success)
                {
                    this.bCheckCanBecomeAdmin = true;
                }
            }
            catch (Exception ex)
            {
            }
        }
        
        private async void CheckCanBecomeAdmin()
        {
            ControlsEnabled(false);
            Progress.IsIndeterminate = true;
            AdminResult r = new AdminResult();
            try
            {
                r = await client.CheckAdmin(user);
                Progress.IsIndeterminate = false;
                ControlsEnabled(true);
            }
            catch (Exception ex)
            {
                Progress.IsIndeterminate = false;
                if (ex is TaskCanceledException)
                {
                    MessageBox.Show("No reply. Are you connected to internet?",
                        "Make Me Admin - Something went wrong",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Environment.Exit(0);
                }
                else if (ex is System.Net.Http.HttpRequestException)
                {
                    MessageBox.Show("Make Me Admin failed to contact the service which should run locally on your computer. Please contact your support.",
                        "Make Me Admin - Something went wrong",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    Environment.Exit(0);
                }
                //throw;
                //else if (ex is )
            }

            if (! r.success)
            {

                MessageBox.Show(r.message,
                    "Make Me Admin - Something went wrong",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Environment.Exit(0);

            }
            ControlsEnabled(true);
            Keyboard.Focus(tbTwofactor);
        }

        private async void AddAdmin()
        {
            var twofactor = tbTwofactor.Text.Trim();
            // This should only happen if we click OK because the input only submits if length is 6
            if (twofactor == "")
            {
                MessageBox.Show("Two factor code is empty",
                        "Make Me Admin - Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                return;
            }
            int expire = 15;
            switch (cbExpire.Text)
            {
                case "30 minutes":
                    expire = 30;
                    break;
                case "1 hour":
                    expire = 60;
                    break;
            }
            ControlsEnabled(false);
            Progress.IsIndeterminate = true;
            AdminResult r = null;
            try
            {
                r = await client.AddAdmin(user, twofactor, expire);
            }
            catch (TaskCanceledException)
            {
                // Do nothing
            }

            Progress.IsIndeterminate = false;
            if (r == null)
            {
                MessageBox.Show("No reply. Are you connected to internet?",
                    "Make Me Admin - Something went wrong",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
               
            }
            else if (r.success)
            {
                MessageBox.Show(String.Format("You are now a member of the local administrators group\nMembership will be removed in {0} minutes.",expire),
                        "Make Me Admin - Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                Environment.Exit(0);
            }
            else
            {
                MessageBox.Show(r.message,
                    "Make Me Admin - Something went wrong",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            ControlsEnabled(true);
        }
    }
}
