using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace GUI
{
    public partial class MainWindow : Window
    {
        private string? _currentFilePath = null;
        private bool _isDirty = false;

        public MainWindow()
        {
            InitializeComponent();
            UpdateTitle();
            StatusTextBlock.Text = "Ожидание...";
        }

        private void UpdateTitle()
        {
            string fileName = _currentFilePath == null ? "Безымянный" : Path.GetFileName(_currentFilePath);
            Title = "GUI — " + fileName + (_isDirty ? "*" : "");
        }

        private void EditorTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _isDirty = true;
            UpdateTitle();
        }

        // ---------- Файл ----------
        private void FileNew_Click(object sender, RoutedEventArgs e)
        {
            if (!AskSaveIfNeeded()) return;

            EditorTextBox.Clear();
            LexemesGrid.ItemsSource = null;
            StatusTextBlock.Text = "Создан новый документ.";
            _currentFilePath = null;
            _isDirty = false;
            UpdateTitle();
        }

        private void FileOpen_Click(object sender, RoutedEventArgs e)
        {
            if (!AskSaveIfNeeded()) return;

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";

            if (dlg.ShowDialog() == true)
            {
                EditorTextBox.Text = File.ReadAllText(dlg.FileName);
                _currentFilePath = dlg.FileName;
                _isDirty = false;
                UpdateTitle();

                LexemesGrid.ItemsSource = null;
                StatusTextBlock.Text = "Открыт файл: " + dlg.FileName;
            }
        }

        private void FileSave_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFilePath == null)
            {
                FileSaveAs_Click(sender, e);
                return;
            }

            File.WriteAllText(_currentFilePath, EditorTextBox.Text);
            _isDirty = false;
            UpdateTitle();
            StatusTextBlock.Text = "Сохранено: " + _currentFilePath;
        }

        private void FileSaveAs_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
            dlg.FileName = _currentFilePath == null ? "document.txt" : Path.GetFileName(_currentFilePath);

            if (dlg.ShowDialog() == true)
            {
                _currentFilePath = dlg.FileName;
                File.WriteAllText(_currentFilePath, EditorTextBox.Text);
                _isDirty = false;
                UpdateTitle();
                StatusTextBlock.Text = "Сохранено: " + _currentFilePath;
            }
        }

        private void FileExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (!AskSaveIfNeeded())
                e.Cancel = true;
        }

        private bool AskSaveIfNeeded()
        {
            if (!_isDirty) return true;

            var res = MessageBox.Show(
                "Есть несохранённые изменения. Сохранить?",
                "GUI",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Cancel) return false;
            if (res == MessageBoxResult.Yes) return TrySave();

            return true;
        }

        private bool TrySave()
        {
            try
            {
                if (_currentFilePath == null)
                {
                    SaveFileDialog dlg = new SaveFileDialog();
                    dlg.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                    dlg.FileName = "document.txt";

                    if (dlg.ShowDialog() != true) return false;
                    _currentFilePath = dlg.FileName;
                }

                File.WriteAllText(_currentFilePath, EditorTextBox.Text);
                _isDirty = false;
                UpdateTitle();
                StatusTextBlock.Text = "Сохранено: " + _currentFilePath;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message);
                return false;
            }
        }

        // ---------- Правка ----------
        private void EditUndo_Click(object sender, RoutedEventArgs e)
        {
            if (EditorTextBox.CanUndo) EditorTextBox.Undo();
        }

        private void EditRedo_Click(object sender, RoutedEventArgs e)
        {
            if (EditorTextBox.CanRedo) EditorTextBox.Redo();
        }

        private void EditCut_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.Cut();
        }

        private void EditCopy_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.Copy();
        }

        private void EditPaste_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.Paste();
        }

        private void EditDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(EditorTextBox.SelectedText))
                EditorTextBox.SelectedText = "";
        }

        private void EditSelectAll_Click(object sender, RoutedEventArgs e)
        {
            EditorTextBox.SelectAll();
            EditorTextBox.Focus();
        }

        // ---------- Текст ----------
        private void TextTask_Click(object sender, RoutedEventArgs e)
        {
            ShowTextFromFile("Постановка задачи", "task.txt");
        }

        private void TextGrammar_Click(object sender, RoutedEventArgs e)
        {
            ShowTextFromFile("Грамматика", "grammar.txt");
        }

        private void TextGrammarClass_Click(object sender, RoutedEventArgs e)
        {
            ShowTextFromFile("Классификация грамматики", "class.txt");
        }

        private void TextAnalyzeMethod_Click(object sender, RoutedEventArgs e)
        {
            ShowTextFromFile("Метод анализа", "method.txt");
        }

        private void TextTest_Click(object sender, RoutedEventArgs e)
        {
            ShowTextFromFile("Тестовый пример", "test.txt");
        }

        private void TextRefs_Click(object sender, RoutedEventArgs e)
        {
            ShowTextFromFile("Список литературы", "refs.txt");
        }

        private void TextSource_Click(object sender, RoutedEventArgs e)
        {
            ShowTextFromFile("Исходный код программы", "source.txt");
        }

        private void ShowTextFromFile(string title, string fileName)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Texts", fileName);

                if (!File.Exists(path))
                {
                    MessageBox.Show(
                        "Не найден файл:\n" + path + "\n\nПроверь, что для файла выставлено:\n" +
                        "- Build Action: Content\n- Copy to Output Directory: Copy if newer",
                        "GUI",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                string text = File.ReadAllText(path);
                ShowTextWindow(title, text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка чтения файла: " + ex.Message);
            }
        }

        // ---------- Пуск ----------
        private void Run_Click(object sender, RoutedEventArgs e)
        {
            string text = EditorTextBox.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                LexemesGrid.ItemsSource = null;
                StatusTextBlock.Text = "Текст пустой.";
                MessageBox.Show("Текст пустой.");
                return;
            }

            var scanner = new GUI.Scanner.LexicalAnalyzer();
            var lexemes = scanner.Analyze(text);

            LexemesGrid.ItemsSource = lexemes;

            int errorCount = lexemes.Count(l => l.IsError);

            if (errorCount == 0)
            {
                StatusTextBlock.Text = $"Лексический анализ завершён. Найдено лексем: {lexemes.Count}. Ошибок нет.";
            }
            else
            {
                StatusTextBlock.Text = $"Лексический анализ завершён. Найдено лексем: {lexemes.Count}. Ошибок: {errorCount}.";
            }
        }

        // ---------- Окна ----------
        private string ReadTextFileOrError(string fileName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Texts", fileName);

            if (!File.Exists(path))
            {
                return "Не найден файл:\n" + path + "\n\n" +
                       "Проверь свойства файла:\n" +
                       "- Build Action: Content\n" +
                       "- Copy to Output Directory: Copy if newer";
            }

            return File.ReadAllText(path);
        }

        private void ShowTextWindow(string title, string text)
        {
            var w = new HelpWindow(title, text);
            w.Owner = this;
            w.ShowDialog();
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            string text = ReadTextFileOrError("help.txt");

            var w = new HelpWindow("Справка", text);
            w.Owner = this;
            w.ShowDialog();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            string text = ReadTextFileOrError("about.txt");

            var w = new AboutWindow(text);
            w.Owner = this;
            w.ShowDialog();
        }
    }
}