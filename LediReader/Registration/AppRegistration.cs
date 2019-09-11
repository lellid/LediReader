using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace LediReader.Registration
{
    public class AppRegistration
    {
        public const string EpubExtension = ".epub";
        public const string AppFileAssociationName = "LediReader.AssocFile.EPUB";
        public static void RegisterApplication()
        {
            bool success = false;
            try
            {
                {
                    // Test if we have write access to the registry
                    // note: it was not helpful (Windows 8.1, 2013-01-18) to use "Registry.ClassesRoot.OpenSubKey("CLSID", RegistryKeyPermissionCheck.ReadWriteSubTree);" -> it simply threw no exception if logged on as normal user!!
                    string testkeystring = "6F06713B-FFC4-46B7-BECF-7BC228AC9C0E";
                    Registry.ClassesRoot.CreateSubKey(testkeystring, RegistryKeyPermissionCheck.ReadWriteSubTree).Close();
                    Registry.ClassesRoot.DeleteSubKeyTree(testkeystring, false);
                }
                Register(Registry.LocalMachine);
                success = true;
            }
            catch (Exception)
            {
            }

            if (!success)
            {

                // if not successful to register into HKLM, we use the user's registry
                try
                {
                    Register(Registry.CurrentUser);
                    success = true;

                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"An exception has occured while registering the application\r\nDetails:\r\n{ex}", "Exception", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
                }
            }

            if (success)
            {
                System.Windows.MessageBox.Show($"The application was successfully associated to .epub files", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }


        private static void Register(RegistryKey rootKey)
        {
            RegistryKey keySW = null;
            RegistryKey keyCR = null;
            RegistryKey key1 = null;
            RegistryKey key2 = null;
            RegistryKey key3 = null;
            RegistryKey key4 = null;

            RegistryValueKind applicationFileNameKind = RegistryValueKind.String;
            string applicationFileName = System.Reflection.Assembly.GetEntryAssembly().Location;

            string programFilesPath = System.Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            if (applicationFileName.ToUpperInvariant().StartsWith(programFilesPath.ToUpperInvariant()))
            {
                applicationFileNameKind = RegistryValueKind.ExpandString;
                applicationFileName = "%ProgramFiles%" + applicationFileName.Substring(programFilesPath.Length);
            }

            try
            {
                keySW = rootKey.CreateSubKey("Software");
                keyCR = keySW.CreateSubKey("Classes");

                {
                    // Register the project file extension
                    key1 = keyCR.CreateSubKey(EpubExtension);
                    key1.SetValue(null, AppFileAssociationName);
                    var keyPI = key1.CreateSubKey("OpenWithProgids");
                    keyPI.SetValue(AppFileAssociationName, new byte[0], RegistryValueKind.None);
                }

                {
                    // Register the Application
                    key1 = keyCR.CreateSubKey(AppFileAssociationName); // set ProgID
                    key1.SetValue("DefaultIcon", string.Format("{0},0", applicationFileName), applicationFileNameKind);
                    key2 = key1.CreateSubKey("shell");
                    key3 = key2.CreateSubKey("open");
                    key4 = key3.CreateSubKey("command");
                    key4.SetValue(null, string.Format("\"{0}\" \"%1\"", applicationFileName), applicationFileNameKind);
                }
            }
            finally
            {

                key4?.Close();
                key3?.Close();
                key2?.Close();
                key1?.Close();
                keyCR?.Close();
                keySW?.Close();
            }
        }

        public static void UnregisterApplication()
        {
            bool success = false;
            try
            {
                Unregister(Registry.LocalMachine);
            }
            catch (Exception ex)
            {
            }

            // unregister also from the user's registry
            try
            {
                Unregister(Registry.CurrentUser);
                success = true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"An exception has occured while unregistering the application\r\nDetails:\r\n{ex}", "Exception", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Exclamation);
            }

            if (success)
            {
                System.Windows.MessageBox.Show($"The application was successfully unregistered", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        private static void Unregister(RegistryKey rootKey)
        {
            var keySW = rootKey.CreateSubKey("Software");
            var keyCR = keySW.CreateSubKey("Classes");
            RegistryKey keyPI = null;
            RegistryKey key1 = null;
            Exception exception = null;

            {
                try
                {
                    key1 = keyCR.OpenSubKey(EpubExtension, true);
                    keyPI = key1.OpenSubKey("OpenWithProgids", true);
                    keyPI.DeleteValue(AppFileAssociationName);

                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    keyPI?.Close();
                    key1?.Close();
                }
            }

            try
            {
                keyCR.DeleteSubKeyTree(AppFileAssociationName, false);
            }
            catch (Exception ex2)
            {
                exception = (exception is null) ? ex2 : new AggregateException(exception, ex2);
            }

            keyCR?.Close();
            keySW?.Close();

            if (null != exception)
                throw exception;
        }
    }
}






