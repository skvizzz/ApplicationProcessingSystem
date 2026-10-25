using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
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
namespace ApplicationProcessingSystem.Views
{
    /// <summary>
    /// Логика взаимодействия для AuthorizationPage.xaml
    /// </summary>
    public partial class AuthorizationPage : Page
    {


        public AuthorizationPage()
        {
            InitializeComponent();
        }
        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordPlaceholder.Visibility =
                PasswordInput.Password.Length == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            СистемаФормированияЗаявокСервисаАрендыАвтомобилейEntities DbArenda = new СистемаФормированияЗаявокСервисаАрендыАвтомобилейEntities();

            

            string userLogin = LoginTextBox.Text;
            string userPassword = GetHash(PasswordInput.Password);

            int userId = DbArenda.Сотрудники.Where(s => s.Логин == userLogin && s.ХешПароля == userPassword).Select(s => s.КодСотрудника).FirstOrDefault();

            if (userId > 0)
            {
                App.CurrentUserId = userId;
                this.NavigationService?.Navigate(new ApplicationFormPage());
                
            }
            else
            {
                MessageBox.Show("Введён неверный логин или пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static string GetHash(string password)
        {
            SHA384 sHA384 = SHA384.Create();
            var hashedPassword = sHA384.ComputeHash(Encoding.UTF8.GetBytes(password));

            return Convert.ToBase64String(hashedPassword);
        }
    }
}
