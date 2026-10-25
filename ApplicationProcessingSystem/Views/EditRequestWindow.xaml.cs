using System;
using Xceed.Words.NET;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ApplicationProcessingSystem.Views
{
    /// <summary>
    /// Логика взаимодействия для EditRequestWindow.xaml
    /// </summary>
    public partial class EditRequestWindow : Window
    {
        private СистемаФормированияЗаявокСервисаАрендыАвтомобилейEntities _db = new СистемаФормированияЗаявокСервисаАрендыАвтомобилейEntities();

        private int _requestId;
        private Заявки _currentRequest;
        private List<СтатусЗаявки> _allStatuses;
        private int _currentStatusIndex;

        public EditRequestWindow(int requestId)
        {
            InitializeComponent();
            _requestId = requestId;
            LoadData();
        }

        public EditRequestWindow() : this(-1) { }

        private void LoadData()
        {
            try
            {
                _currentRequest = _db.Заявки
                    .Include(r => r.Клиенты)
                    .FirstOrDefault(r => r.КодЗаявки == _requestId);

                if (_currentRequest == null)
                {
                    MessageBox.Show("Заявка не найдена!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                RequestTitle.Text = $"Заявка № {_currentRequest.КодЗаявки:D6}";


                if (_currentRequest.Клиенты != null)
                {
                    ClientNameBox.Text = $"{_currentRequest.Клиенты.Фамилия} {_currentRequest.Клиенты.Имя} {_currentRequest.Клиенты.Отчество}".Trim();
                    PhoneBox.Text = _currentRequest.Клиенты.Телефон;
                }

                CarBox.ItemsSource = _db.Автомобили.ToList();
                CarBox.SelectedValue = _currentRequest.КодАвтомобиля;

                StartDatePicker.SelectedDate = _currentRequest.ДатаНачала;
                EndDatePicker.SelectedDate = _currentRequest.ДатаОкончания;

                _allStatuses = _db.СтатусЗаявки.ToList();
                _currentStatusIndex = _allStatuses.FindIndex(s => s.КодСтатуса == _currentRequest.КодСтатуса);
                if (_currentStatusIndex == -1) _currentStatusIndex = 0; 

                UpdateStatusButtonUI();
                UpdateCalculation();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        // Переключение статуса по клику
        private void StatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (_allStatuses == null || _allStatuses.Count == 0) return;

            // Переключаем на следующий по кругу
            _currentStatusIndex = (_currentStatusIndex + 1) % _allStatuses.Count;
            UpdateStatusButtonUI();
        }

        // Обновление цвета и текста кнопки статуса
        private void UpdateStatusButtonUI()
        {
            var status = _allStatuses[_currentStatusIndex];
            StatusButton.Content = status.Название;

            // Те же цвета, что и на странице RequestsPage
            switch (status.Название)
            {
                case "Проверка":
                    StatusButton.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#4E70FF"); // Синий
                    StatusButton.Foreground = Brushes.White; // Белый текст
                    break;
                case "Отклонена":
                    StatusButton.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF7787"); // Красный
                    StatusButton.Foreground = Brushes.White;
                    break;
                case "Принята":
                    StatusButton.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#89F86B"); // Зеленый
                    StatusButton.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#17212F"); // Темный текст
                    break;
                case "Уточнение":
                    StatusButton.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFD477"); // Желтый
                    StatusButton.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#17212F");
                    break;
                default:
                    StatusButton.Background = new SolidColorBrush(Colors.LightGray);
                    StatusButton.Foreground = Brushes.Black;
                    break;
            }
        }


        private void RentalParameters_Changed(object sender, EventArgs e)
        {
            UpdateCalculation();
        }

        // Пересчет стоимости
        private void UpdateCalculation()
        {
            if (CarBox.SelectedItem is Автомобили selectedCar &&
                StartDatePicker.SelectedDate.HasValue &&
                EndDatePicker.SelectedDate.HasValue)
            {
                DateTime start = StartDatePicker.SelectedDate.Value;
                DateTime end = EndDatePicker.SelectedDate.Value;

                if (start > end)
                {
                    CalculationHint.Text = "Дата окончания не может быть раньше даты начала!";
                    TotalText.Text = "0";
                    DaysText.Text = "";
                    DailyPriceText.Text = "";
                    return;
                }

                int days = (end - start).Days;
                if (days == 0) days = 1; 

                decimal totalCost = selectedCar.СтоимостьВДень * days;

                DailyPriceText.Text = $"Цена за день: {selectedCar.СтоимостьВДень:C0}";
                DaysText.Text = $"Количество дней: {days}";
                TotalText.Text = $"Итого: {totalCost:C0}";
                CalculationHint.Text = "";
            }
        }

        // Печать договора
        private void ContractButton_Click(object sender, RoutedEventArgs e)
        {
            var currentStatusName = _allStatuses[_currentStatusIndex].Название;

            if (currentStatusName != "Принята")
            {
                MessageBox.Show("Для формирования договора необходим статус \"Принята\".",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 1. Путь к шаблону
                string templatePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ContractTemplate.docx");

                if (!File.Exists(templatePath))
                {
                    MessageBox.Show($"Файл шаблона не найден по пути:\n{templatePath}\n\nУбедитесь, что он добавлен в проект.",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 2. Настраиваем папку для сохранения

                string folderPath = System.IO.Path.Combine("C:\\Users\\Step\\Desktop\\Дизаин ИС", "Договоры");


                Directory.CreateDirectory(folderPath);

                // Формируем имя файла
                string fileName = $"Договор_Заявка_{_currentRequest.КодЗаявки:D6}.docx";
                string savePath = System.IO.Path.Combine(folderPath, fileName);

                // Получаем данные для вставки
                var client = _currentRequest.Клиенты;
                var car = (Автомобили)CarBox.SelectedItem;

                string initials = "";
                if (client != null && !string.IsNullOrEmpty(client.Имя) && !string.IsNullOrEmpty(client.Фамилия))
                {
                    initials = $"{client.Фамилия} {client.Имя[0]}.{client.Отчество[0]}.";
                }

                // 3. Открываем шаблон и заменяем теги
                using (var document = DocX.Load(templatePath))
                {
                    document.ReplaceText("<Текущая дата>", DateTime.Now.ToString("dd.MM.yyyy"));

                    if (client != null)
                    {
                        document.ReplaceText("<Фамилия>", client.Фамилия ?? "");
                        document.ReplaceText("<Имя>", client.Имя ?? "");
                        document.ReplaceText("<Отчество>", client.Отчество ?? "");
                    }

                    document.ReplaceText("<Инициалы>", initials);

                    document.ReplaceText("<ГосНомер>", car.ГосНомер ?? "");
                    document.ReplaceText("<VIN>", car.VIN ?? "");
                    document.ReplaceText("<Марка>", car.Марка ?? "");
                    document.ReplaceText("<Модель>", car.Модель ?? "");
                    document.ReplaceText("<ГодВыпуска>", car.ГодВыпуска.ToString());
                    document.ReplaceText("<Цвет>", car.Цвет ?? "");

                    string otherFeatures = string.IsNullOrWhiteSpace(car.ИныеПризнаки) ? "Отсутствуют" : car.ИныеПризнаки;
                    document.ReplaceText("<ИныеПризнаки>", otherFeatures);

                    document.ReplaceText("<ДатаНачала>", StartDatePicker.SelectedDate.Value.ToString("dd.MM.yyyy"));
                    document.ReplaceText("<ДатаОкончания>", EndDatePicker.SelectedDate.Value.ToString("dd.MM.yyyy"));
                    document.ReplaceText("<ОбщаяСтоимость>", _currentRequest.ОбщаяСтоимость.ToString("F2"));

                    // 4. Сохраняем готовый документ
                    document.SaveAs(savePath);
                }

                // 5. Спрашиваем у пользователя, открыть ли документ
                var result = MessageBox.Show($"Договор успешно сформирован и сохранен в папку:\n{savePath}\n\nОткрыть документ?",
                    "Успех", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(savePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при формировании договора: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация полей
            if (string.IsNullOrWhiteSpace(ClientNameBox.Text) ||
                string.IsNullOrWhiteSpace(PhoneBox.Text) ||
                CarBox.SelectedItem == null ||
                !StartDatePicker.SelectedDate.HasValue ||
                !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Пожалуйста, заполните все поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (StartDatePicker.SelectedDate > EndDatePicker.SelectedDate)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {

                if (_currentRequest.Клиенты != null)
                {
                    string[] fio = ClientNameBox.Text.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    _currentRequest.Клиенты.Фамилия = fio.Length > 0 ? fio[0] : "";
                    _currentRequest.Клиенты.Имя = fio.Length > 1 ? fio[1] : "";
                    _currentRequest.Клиенты.Отчество = fio.Length > 2 ? fio[2] : "";
                    _currentRequest.Клиенты.Телефон = PhoneBox.Text.Trim();
                }


                var selectedCar = (Автомобили)CarBox.SelectedItem;
                _currentRequest.КодАвтомобиля = selectedCar.КодАвтомобиля;
                _currentRequest.ДатаНачала = StartDatePicker.SelectedDate.Value;
                _currentRequest.ДатаОкончания = EndDatePicker.SelectedDate.Value;
                _currentRequest.КодСтатуса = _allStatuses[_currentStatusIndex].КодСтатуса;

                int days = (EndDatePicker.SelectedDate.Value - StartDatePicker.SelectedDate.Value).Days;
                if (days == 0) days = 1;
                _currentRequest.ОбщаяСтоимость = selectedCar.СтоимостьВДень * days;


                _db.SaveChanges();

                DialogResult = true; 
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}