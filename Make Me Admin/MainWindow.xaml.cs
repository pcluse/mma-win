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
using System.Diagnostics;


namespace Make_Me_Admin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Client client = new Client();
        private string user = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        EventLog myLog = new EventLog("Application");
        private string MMALogSource = "MMA";
        private bool ShortcutMode = false;

        public bool bCheckCanBecomeAdmin { get; set; }
        
        public MainWindow()
        {
            

            myLog.Source = MMALogSource;

            string[] args = Environment.GetCommandLineArgs();
            this.ShortcutMode = (args.Length == 2 && args[1].Equals("--shortcut"));
            
            DoStartup();
            InitializeComponent();
            Keyboard.Focus(tbTwofactor);
        }

        private async void DoStartup()
        {
            if (Precheck())
            {
                try
                {
                    AdminResult r = await client.CheckAdmin(user);
                    if (!r.success)
                    {
                        ShowMessage(r.message,
                        "Make Me Admin - Something went wrong", 1, false);
                        Close();
                    }
                } catch (Exception ex)
                {
                    Progress.IsIndeterminate = false;
                    if (ex is TaskCanceledException)
                    {
                        ShowMessage("No reply. Are you connected to internet?",
                            "Make Me Admin - Something went wrong", 1, false);   
                        Close();
                    }
                    else if (ex is System.Net.Http.HttpRequestException)
                    {
                        ShowMessage("Make Me Admin failed to contact the service which should run locally on your computer. Please contact your support.",
                            "Make Me Admin - Something went wrong", 1, false);
                        Close();
                    }
                    else
                    {
                        ShowMessage(ex.Message, "", 1, true);
                        Close();
                    }
                }

                if (ShortcutMode)
                {
                    string StartMenuPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu);
                    string shortcutLocation = System.IO.Path.Combine(StartMenuPath, "Programs", this.Title + ".lnk");
                    if (System.IO.File.Exists(shortcutLocation))
                    {
                        ShowMessage(string.Format("Creating shortcut at {0}", shortcutLocation),"",0,true);
                        CreateShortcut(shortcutLocation, "C:\\Program Files\\PLS\\Make Me Admin Client\\Make Me Admin.exe");
                    }
                    Close();
                }
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

        private void ToggleControls()
        {
            Progress.IsIndeterminate = true ^ Progress.IsIndeterminate;
            bCancel.IsEnabled = true ^ bCancel.IsEnabled;
            bOk.IsEnabled = true ^ bOk.IsEnabled;
            tbTwofactor.IsEnabled = true ^ tbTwofactor.IsEnabled;
            cbExpire.IsEnabled = true ^ cbExpire.IsEnabled;
        }

        private void bOk_Click(object sender, RoutedEventArgs e)
        {
            AddAdmin();
        }

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            Close();
        }

        private void TBTwoFactor_KeyDown(object sender, KeyEventArgs e)
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
                Close();
            }
            // Users should be able to press tab and switch focus to the next UI element
            else if (e.Key == Key.Tab)
            {
                e.Handled = false;
            }
            // Let users press enter but only if the textbox has the right amount of characters
            else if (e.Key == Key.Return && tbTwofactor.Text.Length == 6)
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
            else if (tbTwofactor.SelectedText.Length > 0 && !(e.Key < Key.D0 || e.Key > Key.D9) && (e.Key < Key.NumPad0 || e.Key > Key.NumPad9))
            {
                e.Handled = false;
            }
            // No more than six characters in the textbox
            else if (tbTwofactor.Text.Length >= 6)
            {
                e.Handled = true;
            }
        }

        private void ShowMessage(string Message, string Title, short Severity,bool HideMessageBox)
        {
            // If this is a message which should not be shown to the user OR if shortcutmode is active,
            if (HideMessageBox || ShortcutMode)
            {
                Console.WriteLine(Message);
            }
            else
            {
                var mbi = MessageBoxImage.Information;

                switch (Severity)
                {
                    case 1:
                        mbi = MessageBoxImage.Error;
                        break;
                    default:
                        mbi = MessageBoxImage.Information;
                        break;
                }
                MessageBox.Show(Message,
                        Title,
                        MessageBoxButton.OK,
                        mbi);
            }
            // Log to eventlog
            var LogEntryType = EventLogEntryType.Information;
            switch (Severity)
            {
                case 1:
                    LogEntryType = EventLogEntryType.Error;
                    break;
                default:
                    LogEntryType = EventLogEntryType.Information;
                    break;
            }
            myLog.WriteEntry(Message, LogEntryType);
        }

        private bool Precheck()
        {
            ShowMessage("MMA Client starting precheck.","",0, true);

            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                ShowMessage("The network is currently unavailable, please try again later.", "Make Me Admin - Network not available", 1, false);
                Close();
            }
            Process[] procs = System.Diagnostics.Process.GetProcesses();
            bool serviceRunning = false;
            foreach (Process proc in procs)
            {
                if (proc.ProcessName == "MMAService") {
                    serviceRunning = true;
                    break;
                }
            }
            if (!serviceRunning)
            {
                ShowMessage("Make Me Admin Service is not running, can not proceed.", "Make Me Admin - Error", 1, false);
                Close();
            }
            ShowMessage("MMA Client adminprecheck was successful.", "", 0, true);
            return true;
        }

        private async void AddAdmin()
        {
            ToggleControls();
            var twofactor = tbTwofactor.Text.Trim();
            // This should only happen if we click OK because the input only submits if length is 6
            if (twofactor == "")
            {
                ShowMessage("Two factor code is empty",
                        "Make Me Admin - Error", 1, false);
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
            AdminResult r = null;
            try
            {
                r = await client.AddAdmin(user, twofactor, expire);
            }
            catch (TaskCanceledException)
            {
                // Do nothing
            }

            if (r == null)
            {
                ShowMessage("No reply. Are you connected to internet?",
                    "Make Me Admin - Something went wrong",1,false);
               
            }
            else if (r.success)
            {
                ShowMessage(String.Format("You are now a member of the local administrators group\nMembership will be removed in {0} minutes.",expire),
                        "Make Me Admin - Success",0,false);
                Close();
            }
            else
            {
                ShowMessage(r.message,
                    "Make Me Admin - Something went wrong",1,false);
            }
            ToggleControls();
        }
    }
}
