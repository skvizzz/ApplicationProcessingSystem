
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ApplicationProcessingSystem.Views
{
    /// <summary>
    /// Логика взаимодействия для ApplicationFormPage.xaml
    /// </summary>
    public partial class ApplicationFormPage : Page
    {
        public СистемаФормированияЗаявокСервисаАрендыАвтомобилейEntities db = new СистемаФормированияЗаявокСервисаАрендыАвтомобилейEntities();
        public ApplicationFormPage()
        {
            InitializeComponent();
            LoadCarsAsync();
        }


        private void AddApplicationButton_Click(object sender, RoutedEventArgs e)
        {
            string[] clientFIO = null;
            string clientNumber = null;
            int carId = -1;
            DateTime? dateStart = null;
            DateTime? dateEnd = null;

            bool validApplication = false;

            // 1. Проверка ФИО
            if (ClientFIOTextBox.Text.Trim().Split(' ').Length >= 3) 
            {
                clientFIO = ClientFIOTextBox.Text.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректные фамилию, имя, отчество Клиента.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // 2. Проверка телефона (исправлена логика)
            string phoneText = PhoneInputTextBox.Text.Trim();
            if (phoneText.StartsWith("+7") && phoneText.Length == 12)
            {
                clientNumber = phoneText;
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректный номер телефона (+7**********).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // 3. Проверка автомобиля
            if (CarInputComboBox.SelectedValue is int selectedId)
            {
                carId = selectedId;
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите автомобиль из списка.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // 4. Проверка дат 
            if (DateStartInput.SelectedDate != null && DateEndInput.SelectedDate != null && DateStartInput.SelectedDate < DateEndInput.SelectedDate)
            {
                dateStart = DateStartInput.SelectedDate;
                dateEnd = DateEndInput.SelectedDate;
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите даты начала и окончания аренды. Дата окончания не может быть меньше или равна Дате начала.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            
            if (clientFIO != null && clientNumber != null && carId != -1 && dateStart != null && dateEnd != null)
            {
                validApplication = true;
            }

            if (validApplication)
            {
                try
                {
                    
                    var client = db.Клиенты.FirstOrDefault(c => c.Телефон == clientNumber);
                    if (client == null)
                    {
                        client = new Клиенты
                        {
                            Фамилия = clientFIO[0],
                            Имя = clientFIO[1],
                            Отчество = clientFIO.Length > 2 ? clientFIO[2] : "",
                            Телефон = clientNumber
                        };
                        db.Клиенты.Add(client);
                        db.SaveChanges(); 
                    }


                    var car = db.Автомобили.FirstOrDefault(c => c.КодАвтомобиля == carId);
                    int days = (dateEnd.Value - dateStart.Value).Days;
                    if (days == 0) days = 1; 

                    decimal totalCost = (car.СтоимостьВДень) * days;

                    
                    var statusId = db.СтатусЗаявки.First(x => x.Название == "Проверка").КодСтатуса;

                   
                    var newAplication = new Заявки
                    {
                        ДатаНачала = dateStart.Value,
                        ДатаОкончания = dateEnd.Value,
                        КодАвтомобиля = carId,
                        КодКлиента = client.КодКлиента,
                        КодСотрудника = App.CurrentUserId,
                        КодСтатуса = statusId,
                        ОбщаяСтоимость = totalCost
                    };

                    
                    db.Заявки.Add(newAplication);
                    db.SaveChanges();

                    MessageBox.Show("Заявка успешно сформирована!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    
                    ClientFIOTextBox.Text = "";
                    PhoneInputTextBox.Text = "";
                    CarInputComboBox.Text = "";
                    DateStartInput.Text = "";
                    DateEndInput.Text = "";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении: {ex.Message}\n{ex.InnerException?.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void LoadCarsAsync()
        {
            try
            {
                var cars = await db.Автомобили
                    .AsNoTracking() 
                    .ToListAsync();

                // Настраиваем ComboBox
                CarInputComboBox.ItemsSource = cars;
                CarInputComboBox.DisplayMemberPath = "DisplayText"; 
                CarInputComboBox.SelectedValuePath = "КодАвтомобиля";
            }
            catch (Exception ex)
            {
                
                MessageBox.Show($"Ошибка загрузки автомобилей: {ex.Message}");
            }
        }

        private void RequestsPageButton_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new RequestsPage());
        }
    }
}
