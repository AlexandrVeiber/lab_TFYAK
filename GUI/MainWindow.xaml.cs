using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.IO;
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
            OutputTextBox.Clear();
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

                OutputTextBox.Text = "Открыт файл: " + dlg.FileName;
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
            OutputTextBox.Text = "Сохранено: " + _currentFilePath;
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
                OutputTextBox.Text = "Сохранено: " + _currentFilePath;
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

            if (res == MessageBoxResult.Yes)
                return TrySave();

            return true; // No
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
                OutputTextBox.Text = "Сохранено: " + _currentFilePath;
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

        // ---------- Пока заглушки (доделаем следующим коммитом) ----------
        private void TextTask_Click(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Text = "Раздел 'Текст' пока не заполнен.";
        }

        private void TextGrammar_Click(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Text = "Раздел 'Текст' пока не заполнен.";
        }

        private void TextGrammarClass_Click(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Text = "Раздел 'Текст' пока не заполнен.";
        }

        private void TextAnalyzeMethod_Click(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Text = "Раздел 'Текст' пока не заполнен.";
        }

        private void TextTest_Click(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Text = "Раздел 'Текст' пока не заполнен.";
        }

        private void TextRefs_Click(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Text = "Раздел 'Текст' пока не заполнен.";
        }

        private void TextSource_Click(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Text = "Исходники лежат в репозитории.";
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            string text = EditorTextBox.Text;

            if (string.IsNullOrWhiteSpace(text))
            {
                OutputTextBox.Text = "Пуск: текст пустой.";
                return;
            }

            int lines = text.Split('\n').Length;
            int chars = text.Length;

            OutputTextBox.Text =
                "Пуск: заглушка анализа.\n" +
                $"Строк: {lines}\n" +
                $"Символов: {chars}\n" +
                $"Время: {DateTime.Now}";
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Файл: создать/открыть/сохранить/сохранить как/выход.\n" +
                "Правка: undo/redo/cut/copy/paste/delete/select all.\n" +
                "Пуск: пока заглушка (статистика по тексту).",
                "Справка",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "GUI (часть проекта по ТФЯК)\nWPF / C#\nАвтор: Александр",
                "О программе",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}