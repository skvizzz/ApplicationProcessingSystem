using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.Entity;
using ApplicationProcessingSystem;
using ApplicationProcessingSystem.Views; // Обязательно для .Include()

namespace ApplicationProcessingSystem.Views // Убедитесь, что namespace совпадает с вашим
{
    public partial class RequestsPage : Page
    {
        // Контекст базы данных
        private СистемаФормированияЗаявокСервисаАрендыАвтомобилейEntities _db;

        // Полный список заявок для фильтрации
        private List<RequestViewModel> _allRequests = new List<RequestViewModel>();

        public RequestsPage()
        {
            InitializeComponent();
            LoadData();
        }

        // Загрузка данных из БД
        private void LoadData()
        {
            try
            {

                _db = new СистемаФормированияЗаявокСервисаАрендыАвтомобилейEntities();
                _db.Configuration.LazyLoadingEnabled = false;


                var requestsFromDb = _db.Заявки
                    .OrderByDescending(z => z.КодЗаявки)
                    .ToList();


                var allCars = _db.Автомобили.ToList();
                var allStatuses = _db.СтатусЗаявки.ToList();


                _allRequests = requestsFromDb.Select(z => new RequestViewModel
                {
                    CodeZayavki = z.КодЗаявки,
                    DataNachala = z.ДатаНачала,
                    DataOkonchaniya = z.ДатаОкончания,
                    GosNomer = allCars.FirstOrDefault(a => a.КодАвтомобиля == z.КодАвтомобиля)?.ГосНомер ?? "Не найден",
                    Status = allStatuses.FirstOrDefault(s => s.КодСтатуса == z.КодСтатуса)?.Название ?? "Нет статуса"
                }).ToList();

                // Применяем фильтр (если в поиске уже что-то введено)
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчик поиска
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        // Логика фильтрации списка
        private void ApplyFilter()
        {
            if (_allRequests == null) return;

            string searchText = SearchTextBox.Text?.Trim().ToLower() ?? "";

            var filteredList = _allRequests.Where(r =>
                string.IsNullOrEmpty(searchText) ||
                r.CodeZayavki.ToString().Contains(searchText) ||
                r.DataNachala.ToString("dd.MM.yyyy HH:mm").Contains(searchText) ||
                r.DataOkonchaniya.ToString("dd.MM.yyyy HH:mm").Contains(searchText) ||
                (r.GosNomer != null && r.GosNomer.ToLower().Contains(searchText)) ||
                (r.Status != null && r.Status.ToLower().Contains(searchText))
            ).ToList();

            RequestsListBox.ItemsSource = filteredList;

            if (filteredList.Count == 0)
            {
                EmptyTextBlock.Visibility = Visibility.Visible;
                ResultsTextBlock.Text = "Найдено: 0";
            }
            else
            {
                EmptyTextBlock.Visibility = Visibility.Collapsed;
                ResultsTextBlock.Text = $"Найдено: {filteredList.Count}";
            }
        }

        // Двойной клик по заявке — ОТКРЫВАЕМ ОКНО РЕДАКТИРОВАНИЯ
        private void RequestItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RequestsListBox.SelectedItem is RequestViewModel selectedRequest)
            {

                var editWindow = new EditRequestWindow(selectedRequest.CodeZayavki);


                editWindow.Owner = Window.GetWindow(this);


                if (editWindow.ShowDialog() == true)
                {
                    LoadData();
                }
            }
        }

        // Кнопка возврата на страницу создания
        private void CreateRequestButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
            else
            {
                this.NavigationService?.Navigate(new ApplicationFormPage());
            }
        }
    }

    // Класс-модель для удобной привязки к XAML
    public class RequestViewModel
    {
        public int CodeZayavki { get; set; }
        public DateTime DataNachala { get; set; }
        public DateTime DataOkonchaniya { get; set; }
        public string GosNomer { get; set; }
        public string Status { get; set; }
    }
}