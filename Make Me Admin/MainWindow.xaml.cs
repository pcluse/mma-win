using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public RestClient client = new RestClient();
        private string loggedInUid = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        private string uid = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        private string preferredService = null; // totp or freja or null
        EventLog myLog = new EventLog("Application");
        private string MMALogSource = "MMA";

        // For testing
        // private string user = @"DESKTOP-IB70NP4\Johan";

        public MainWindow()
        {
            myLog.Source = MMALogSource;

            string[] args = Environment.GetCommandLineArgs();
            var shortcutMode = args.Length == 2 && args[1].Equals("--shortcut");

            var success = Precheck(shortcutMode /* silent */);
            if (!success)
            {
                Environment.Exit(1);
            }
            
            if (shortcutMode)
            {
                SilentCheckHandleShortcut();
                Environment.Exit(0);
            }
            
            // Show window
            InitializeComponent();
            CheckCanBecomeAdmin();
        }

        private void LogInformation(string message)
        {
            myLog.WriteEntry(message, EventLogEntryType.Information);
        }
        private void LogError(string message)
        {
            myLog.WriteEntry(message, EventLogEntryType.Error);
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

        private static void DeleteShortcut(string shortcutLocation)
        {

            if (System.IO.File.Exists(shortcutLocation))
            {
                System.IO.File.Delete(shortcutLocation);
            }
        }

        private void DefaultButton_Click(object sender, RoutedEventArgs e)
        {
            if (preferredService == null)
            {
                CheckCanBecomeAdmin();
            } else
            {
                AddAdmin();
            }
            
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Environment.Exit(0);
        }

        private void CodeField_KeyDown(object sender, KeyEventArgs e)
        {
            // Modifier keys are not represented in e.Key, need to detect these to avoid garbage in the textbox
            if (Keyboard.IsKeyDown(Key.LeftShift)
                || Keyboard.IsKeyDown(Key.RightShift)
                || Keyboard.IsKeyDown(Key.RightAlt)
                || Keyboard.IsKeyDown(Key.RightAlt)
                || Keyboard.IsKeyDown(Key.LeftCtrl)
                || Keyboard.IsKeyDown(Key.RightCtrl)
               )
            {
                e.Handled = true;
            }
            // If escape is pressed, close the window
            else if (e.Key == Key.Escape)
            {
                Close();
            }
            // Users should be able to press tab and switch focus to the next UI element
            else if (e.Key == Key.Tab)
            {
                e.Handled = false;
            }
            // Let users press enter but only if the textbox has the right amount of characters
            else if (e.Key == Key.Return && CodeField.Text.Length == 6)
            {
                AddAdmin();
                // No need to pass this key
                e.Handled = true;
            }
            // Input has to be between 0 and 9
            else if ((e.Key < Key.D0 || e.Key > Key.D9) && (e.Key < Key.NumPad0 || e.Key > Key.NumPad9))
            {
                e.Handled = true;
            }
            // If there is selected text and the input is between 0-9 we skip handling to be able to replace text
            else if (CodeField.SelectedText.Length > 0 && !(e.Key < Key.D0 || e.Key > Key.D9) && (e.Key < Key.NumPad0 || e.Key > Key.NumPad9))
            {
                e.Handled = false;
            }
            // No more than six characters in the textbox
            else if (CodeField.Text.Length >= 6)
            {
                e.Handled = true;
            }
        }

        private void SilentCheckHandleShortcut()
        {
            
            var r = Task.Run(() => client.GetPrerequisites(uid)).Result;
            preferredService = r.preferredService;


            string StartMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
            string shortcutLocation = System.IO.Path.Combine(StartMenuPath, "Programs", "Make me admin.lnk");

            if (!r.success || preferredService == null)
            {
                DeleteShortcut(shortcutLocation);
                LogInformation(String.Format("MMA Client: deleted shortcut {0}", shortcutLocation));
            }
            else
            {
                CreateShortcut(shortcutLocation, System.Reflection.Assembly.GetExecutingAssembly().Location);
                LogInformation(String.Format("MMA Client: created shortcut {0}", shortcutLocation));
            }
        }
       
        private void StartChecking()
        {
            DefaultButton.IsEnabled = false;
            TechnicianButton.IsEnabled = false;
            Spinner.IsIndeterminate = true;
            ErrorText.Visibility = Visibility.Hidden;
            if (preferredService == "totp")
            {
                CodeField.IsEnabled = false;
                DefaultButton.Content = "Ok";
            } else
            {
                DefaultButton.Content = "Retry";
            }
        }

        private void StopCheckingSuccess()
        {
            Spinner.IsIndeterminate = false;
            ErrorText.Visibility = Visibility.Hidden;
            DefaultButton.Content = "OK";
            DefaultButton.IsEnabled = false;
            TechnicianButton.IsEnabled = false;
        }

        private void StopCheckingFail(String message, String title)
        {
            Spinner.IsIndeterminate = false;
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
            DefaultButton.IsEnabled = true;
            TechnicianButton.IsEnabled = true;
            if (preferredService == "totp")
            {
                CodeField.IsEnabled = false;
                DefaultButton.Content = "Ok";
            }
            else
            {
                DefaultButton.Content = "Retry";
            }
        }
        
        private async void CheckCanBecomeAdmin()
        {
            InfoText.Text = "Checking...";
            StartChecking();
            PrerequisitesReply reply = await client.GetPrerequisites(uid);
            if (!reply.success)
            {
                StopCheckingFail(reply.message, "Retry");
                return;
            }
            
            preferredService = reply.preferredService;
            if (preferredService == null || (preferredService != "totp" && preferredService != "freja"))
            {
                StopCheckingFail(reply.message, "Retry");
                return;
            }
            StopCheckingSuccess();
            if (preferredService == "totp")
            {
                InfoText.Text = "∙ Open mobile authentication app\n∙ Enter Lund University (Security Code 2)";
                CodeField.Visibility = Visibility.Visible;
                DefaultButton.IsEnabled = true;
                DefaultButton.Content = "OK";
                Keyboard.Focus(CodeField);
            }
            else if (preferredService == "freja")
            {
                InfoText.Text = "∙ Start Freja eID on your phone\n∙ Send request\n∙ Accept request in Freja eID";
                DefaultButton.IsEnabled = true;
                DefaultButton.Content = "Request";
            }
            
        }

        private bool Precheck(bool silent)
        {
            LogInformation("MMA Client: starting precheck.");

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                LogError("MMA Client: The network is currently unavailable, please try again later.");
                if (!silent) {
                    MessageBox.Show("The network is currently unavailable, please try again later",
                       "Make Me Admin - Network not available",
                       MessageBoxButton.OK,
                       MessageBoxImage.Error);  
                }
                return false;
            }
            Process[] procs = System.Diagnostics.Process.GetProcesses();
            bool serviceRunning = false;
            foreach (Process proc in procs)
            {
                if (proc.ProcessName == "MMAService")
                {
                    serviceRunning = true;
                    break;
                }
            }
            if (!serviceRunning)
            {
                LogError("MMA Client: Make Me Admin Service is not running, can not proceed.");
                if (!silent) {
                    MessageBox.Show("Make Me Admin Service is not running, can not proceed.",
                       "Make Me Admin - Error",
                       MessageBoxButton.OK,
                       MessageBoxImage.Error);
                }
                return false;
            }
            LogInformation("MMA Client: precheck was successful.");
            return true;
        }

        private async void AddAdmin()
        {
            var code = CodeField.Text.Trim();
            // This should only happen if we click OK because the input only submits if length is 6
            if (preferredService == "totp" && code == "")
            {
                ErrorText.Text = "Two factor code is empty";
            }
            StartChecking();
            CodeField.IsEnabled = false;
            ValidateReply reply = null;
            if (preferredService == "totp")
            {
                reply = await client.ValidateTotp(uid, code);
            } else if (preferredService == "freja")
            {
                reply = await client.ValidateFreja(uid);
            }
                
            if (!reply.success)
            {
                StopCheckingFail("No reply. Are you connected to internet?", "Retry");
                CodeField.IsEnabled = true;
                return;
            }
            if (!reply.validated)
            {
                if (preferredService == "totp")
                {
                    StopCheckingFail("Could not verify your code", "OK");
                    CodeField.IsEnabled = true;
                }
                else
                {
                    StopCheckingFail("Could not validate, try again", "Retry");
                }
                return;
            }
            StopCheckingSuccess();
            MessageBox.Show(String.Format("You are now a member of the local administrators group\nMembership will be removed in 15 minutes."),
                    "Make Me Admin - Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            Environment.Exit(0);
        }

        private void Technician_Click(object sender, RoutedEventArgs e)
        {
            TechnicianWindow w = new TechnicianWindow();
            var ok = (bool)w.ShowDialog();
            if (! ok)
            {
                if (uid != loggedInUid)
                {
                    uid = loggedInUid;
                    CheckCanBecomeAdmin();
                }
                return;
            }
            
            var technicianUid = w.TechnicianUid.Text.Trim();
            var technicianUidDomain = "UW\\" + technicianUid;
            if (technicianUid.Length > 0 && technicianUidDomain != loggedInUid)
            {
                uid = technicianUidDomain;
                CheckCanBecomeAdmin();
            }
        }
    }
}
