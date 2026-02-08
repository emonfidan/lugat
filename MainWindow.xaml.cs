using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace LugatimApp
{
    public partial class MainWindow : Window
    {
        private const string DATA_FILE = "lugatim_data.json";
        private ObservableCollection<WordEntry> historyItems;
        private ObservableCollection<WordEntry> favoriteItems;
        private AppData appData;
        private bool isNavigatingFromHistory = false;

        public MainWindow()
        {
            InitializeComponent();
            historyItems = new ObservableCollection<WordEntry>();
            favoriteItems = new ObservableCollection<WordEntry>();
            lstHistory.ItemsSource = historyItems;
            lstFavorites.ItemsSource = favoriteItems;

            LoadData();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // WebView2 başlatma
            await webView.EnsureCoreWebView2Async(null);

            // URL değiştiğinde çağrılacak olay
            webView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
        }

        private void CoreWebView2_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            // Geçmişten geziniyorsa kaydetme
            if (isNavigatingFromHistory)
            {
                isNavigatingFromHistory = false;
                return;
            }

            string currentUrl = webView.CoreWebView2.Source;

            // Sadece lugatim.com/s/ ile başlayan URL'leri kaydet
            if (currentUrl.Contains("lugatim.com/s/") && !string.IsNullOrEmpty(currentUrl))
            {
                AddToHistory(currentUrl);
            }
        }

        private void BtnRandom_Click(object sender, RoutedEventArgs e)
        {
            webView.CoreWebView2.Navigate("https://lugatim.com");
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchWord();
        }

        private void TxtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchWord();
            }
        }

        private void SearchWord()
        {
            string word = txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(word))
            {
                // Türkçe karakterleri URL encode et
                string encodedWord = Uri.EscapeDataString(word.ToUpper());
                string url = $"https://lugatim.com/s/{encodedWord}";
                webView.CoreWebView2.Navigate(url);
                txtSearch.Clear();
            }
        }

        private void AddToHistory(string url)
        {
            try
            {
                // URL'den kelimeyi çıkar
                Uri uri = new Uri(url);
                string word = Uri.UnescapeDataString(uri.Segments.Last().TrimEnd('/'));

                // "s" veya boş kelime ise kaydetme
                if (string.IsNullOrWhiteSpace(word) || word.Equals("s", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // Aynı kelime zaten varsa ekleme
                var existing = appData.History.FirstOrDefault(w => w.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
                if (existing != null)
                {
                    return;
                }

                // Yeni kelimeyi ekle
                var newEntry = new WordEntry
                {
                    Word = word,
                    Url = url,
                    Timestamp = DateTime.Now,
                    IsFavorite = false
                };

                appData.History.Insert(0, newEntry);
                historyItems.Insert(0, newEntry);

                SaveData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LstHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstHistory.SelectedItem is WordEntry entry)
            {
                // Notu göster
                ShowNotePanel(entry);

                isNavigatingFromHistory = true;
                webView.CoreWebView2.Navigate(entry.Url);
                lstHistory.SelectedItem = null;
            }
        }

        private void LstFavorites_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstFavorites.SelectedItem is WordEntry entry)
            {
                // Notu göster
                ShowNotePanel(entry);

                isNavigatingFromHistory = true;
                webView.CoreWebView2.Navigate(entry.Url);
                lstFavorites.SelectedItem = null;
            }
        }

        private void ShowNotePanel(WordEntry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.Notes))
            {
                // Not panelini göster
                txtNotePanelTitle.Text = $"📝 {entry.Word} - Not";
                txtNoteDisplay.Text = entry.Notes;
                notePanel.Visibility = Visibility.Visible;
            }
            else
            {
                // Not yoksa paneli gizle
                notePanel.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnToggleFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is WordEntry entry)
            {
                entry.IsFavorite = !entry.IsFavorite;

                if (entry.IsFavorite)
                {
                    // Favorilere ekle
                    if (!favoriteItems.Contains(entry))
                    {
                        favoriteItems.Add(entry);
                    }
                }
                else
                {
                    // Favorilerden çıkar
                    favoriteItems.Remove(entry);
                }

                // UI'yi güncelle
                RefreshLists();
                SaveData();
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is WordEntry entry)
            {
                // Onay iste
                var result = MessageBox.Show(
                    $"'{entry.Word}' kelimesini geçmişten silmek istediğinize emin misiniz?",
                    "Silme Onayı",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // Hem geçmişten hem favorilerden sil
                    appData.History.Remove(entry);
                    historyItems.Remove(entry);
                    favoriteItems.Remove(entry);

                    // Not panelini gizle
                    notePanel.Visibility = Visibility.Collapsed;

                    SaveData();
                }
            }
        }

        private void BtnAddNote_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is WordEntry entry)
            {
                // Not dialog penceresi oluştur
                var noteDialog = new NoteDialog(entry.Word, entry.Notes);
                noteDialog.Owner = this;

                if (noteDialog.ShowDialog() == true)
                {
                    entry.Notes = noteDialog.NoteText;

                    // Eğer şu anda görüntülenen not bu kelimeye aitse, paneli güncelle
                    if (notePanel.Visibility == Visibility.Visible &&
                        txtNotePanelTitle.Text.Contains(entry.Word))
                    {
                        ShowNotePanel(entry);
                    }

                    // UI'yi güncelle
                    RefreshLists();
                    SaveData();
                }
            }
        }

        private void RefreshLists()
        {
            // Geçmişi güncelle
            historyItems.Clear();
            foreach (var item in appData.History)
            {
                historyItems.Add(item);
            }

            // Favorileri güncelle
            favoriteItems.Clear();
            foreach (var item in appData.History.Where(w => w.IsFavorite))
            {
                favoriteItems.Add(item);
            }
        }

        private void LoadData()
        {
            try
            {
                if (File.Exists(DATA_FILE))
                {
                    string json = File.ReadAllText(DATA_FILE, System.Text.Encoding.UTF8);
                    appData = JsonSerializer.Deserialize<AppData>(json) ?? new AppData();
                }
                else
                {
                    appData = new AppData();
                }

                RefreshLists();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri yükleme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                appData = new AppData();
            }
        }

        private void SaveData()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string json = JsonSerializer.Serialize(appData, options);
                File.WriteAllText(DATA_FILE, json, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri kaydetme hatası: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class WordEntry
    {
        public string Word { get; set; }
        public string Url { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsFavorite { get; set; }
        public string Notes { get; set; } = "";

        public string FavoriteIcon => IsFavorite ? "★" : "☆";
        public string NotesVisibility => string.IsNullOrWhiteSpace(Notes) ? "Collapsed" : "Visible";
    }

    public class AppData
    {
        public ObservableCollection<WordEntry> History { get; set; } = new ObservableCollection<WordEntry>();
    }
}
