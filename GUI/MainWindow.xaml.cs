using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GUI.Scanner;
using GUI.Syntax;

namespace GUI
{
    public partial class MainWindow : Window
    {
        private string? _currentFilePath = null;
        private bool _isDirty = false;

        private enum AnalysisMode
        {
            LexerOnly,
            ParserOnly,
            Both
        }

        private sealed class CombinedAnalysisRow
        {
            public string Stage { get; set; } = "";
            public string Code { get; set; } = "";
            public string Type { get; set; } = "";
            public string Text { get; set; } = "";
            public string Location { get; set; } = "";
            public string Description { get; set; } = "";

            public int StartIndex { get; set; } = -1;
            public int Length { get; set; }
        }

        private AnalysisMode CurrentAnalysisMode =>
            AnalysisModeComboBox.SelectedIndex switch
            {
                0 => AnalysisMode.LexerOnly,
                1 => AnalysisMode.ParserOnly,
                _ => AnalysisMode.Both
            };

        public MainWindow()
        {
            InitializeComponent();
            UpdateTitle();

            AnalysisModeComboBox.SelectedIndex = 2;
            ConfigureGridForCurrentMode();

            StatusTextBlock.Text = "Ожидание...";
            LexemesGrid.ItemsSource = null;
            LexemesGrid.Items.Clear();
        }

        private void UpdateTitle()
        {
            string fileName = _currentFilePath == null ? "Безымянный" : Path.GetFileName(_currentFilePath);
            Title = "GUI — " + fileName + (_isDirty ? "*" : "");
        }

        private void EditorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _isDirty = true;
            UpdateTitle();
        }

        // ---------- Файл ----------
        private void FileNew_Click(object sender, RoutedEventArgs e)
        {
            if (!AskSaveIfNeeded())
                return;

            EditorTextBox.Clear();
            ClearDiagnosticsGrid();
            ConfigureGridForCurrentMode();
            StatusTextBlock.Text = "Создан новый документ.";

            _currentFilePath = null;
            _isDirty = false;
            UpdateTitle();
        }

        private void FileOpen_Click(object sender, RoutedEventArgs e)
        {
            if (!AskSaveIfNeeded())
                return;

            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                EditorTextBox.Text = File.ReadAllText(dlg.FileName);
                _currentFilePath = dlg.FileName;
                _isDirty = false;
                UpdateTitle();

                ClearDiagnosticsGrid();
                ConfigureGridForCurrentMode();
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
            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                FileName = _currentFilePath == null ? "document.txt" : Path.GetFileName(_currentFilePath)
            };

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
            if (!_isDirty)
                return true;

            var res = MessageBox.Show(
                "Есть несохранённые изменения. Сохранить?",
                "GUI",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Cancel)
                return false;

            if (res == MessageBoxResult.Yes)
                return TrySave();

            return true;
        }

        private bool TrySave()
        {
            try
            {
                if (_currentFilePath == null)
                {
                    SaveFileDialog dlg = new SaveFileDialog
                    {
                        Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                        FileName = "document.txt"
                    };

                    if (dlg.ShowDialog() != true)
                        return false;

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
            if (EditorTextBox.CanUndo)
                EditorTextBox.Undo();
        }

        private void EditRedo_Click(object sender, RoutedEventArgs e)
        {
            if (EditorTextBox.CanRedo)
                EditorTextBox.Redo();
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

        // ---------- Режимы анализа ----------
        private void AnalysisModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            ClearDiagnosticsGrid();
            ConfigureGridForCurrentMode();

            StatusTextBlock.Text = CurrentAnalysisMode switch
            {
                AnalysisMode.LexerOnly => "Выбран режим: только лексический анализ.",
                AnalysisMode.ParserOnly => "Выбран режим: только синтаксический анализ.",
                _ => "Выбран режим: оба анализа."
            };
        }

        private void ConfigureGridForCurrentMode()
        {
            switch (CurrentAnalysisMode)
            {
                case AnalysisMode.LexerOnly:
                    ConfigureLexerColumns();
                    break;

                case AnalysisMode.ParserOnly:
                    ConfigureParserColumns();
                    break;

                case AnalysisMode.Both:
                    ConfigureCombinedColumns();
                    break;
            }
        }

        private void ConfigureLexerColumns()
        {
            LexemesGrid.Columns.Clear();

            LexemesGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Код",
                Binding = new System.Windows.Data.Binding("Code"),
                Width = DataGridLength.Auto,
                MinWidth = 60
            });

            LexemesGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Тип",
                Binding = new System.Windows.Data.Binding("Type"),
                Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                MinWidth = 170
            });

            LexemesGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Лексема",
                Binding = new System.Windows.Data.Binding("Text"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                MinWidth = 120
            });

            LexemesGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Позиция",
                Binding = new System.Windows.Data.Binding("Location"),
                Width = DataGridLength.Auto,
                MinWidth = 150
            });
        }

        private void ConfigureParserColumns()
        {
            LexemesGrid.Columns.Clear();

            LexemesGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Неверный фрагмент",
                Binding = new System.Windows.Data.Binding("InvalidFragment"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                MinWidth = 150
            });

            LexemesGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Местоположение",
                Binding = new System.Windows.Data.Binding("Location"),
                Width = DataGridLength.Auto,
                MinWidth = 180
            });

            LexemesGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Описание",
                Binding = new System.Windows.Data.Binding("Description"),
                Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                MinWidth = 260
            });
        }

        private void ConfigureCombinedColumns()
        {
            LexemesGrid.Columns.Clear();

            LexemesGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Этап",
                Binding = new System.Windows.Data.Binding("Stage"),
                Width = DataGridLength.Auto,
                MinWidth = 120
            });

            LexemesGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Код",
                Binding = new System.Windows.Data.Binding("Code"),
                Width = DataGridLength.Auto,
                MinWidth = 60
            });

            LexemesGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Тип / ошибка",
                Binding = new System.Windows.Data.Binding("Type"),
                Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                MinWidth = 180
            });

            LexemesGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Фрагмент",
                Binding = new System.Windows.Data.Binding("Text"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                MinWidth = 120
            });

            LexemesGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Позиция",
                Binding = new System.Windows.Data.Binding("Location"),
                Width = DataGridLength.Auto,
                MinWidth = 150
            });

            LexemesGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Описание",
                Binding = new System.Windows.Data.Binding("Description"),
                Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                MinWidth = 200
            });
        }

        // ---------- Пуск ----------
        private void Run_Click(object sender, RoutedEventArgs e)
        {
            string text = EditorTextBox.Text;

            ClearDiagnosticsGrid();
            ConfigureGridForCurrentMode();

            if (string.IsNullOrWhiteSpace(text))
            {
                StatusTextBlock.Text = "Текст пустой.";
                return;
            }

            var scanner = new LexicalAnalyzer();
            var lexemes = scanner.Analyze(text);

            var lexicalErrors = lexemes
                .Where(l => l.IsError)
                .ToList();

            switch (CurrentAnalysisMode)
            {
                case AnalysisMode.LexerOnly:
                    ShowLexerResult(lexemes);
                    break;

                case AnalysisMode.ParserOnly:
                    ShowParserResult(lexemes, lexicalErrors);
                    break;

                case AnalysisMode.Both:
                    ShowCombinedResult(lexemes, lexicalErrors);
                    break;
            }
        }

        private void ShowLexerResult(List<Lexeme> lexemes)
        {
            ConfigureLexerColumns();
            LexemesGrid.ItemsSource = lexemes;

            int errorCount = lexemes.Count(l => l.IsError);

            if (errorCount == 0)
            {
                StatusTextBlock.Text =
                    $"Лексический анализ завершён. Найдено лексем: {lexemes.Count}. Ошибок нет.";
            }
            else
            {
                StatusTextBlock.Text =
                    $"Лексический анализ завершён. Найдено лексем: {lexemes.Count}. Ошибок: {errorCount}.";
            }
        }

        private void ShowParserResult(List<Lexeme> lexemes, List<Lexeme> lexicalErrors)
        {
            ConfigureParserColumns();

            if (lexicalErrors.Count > 0)
            {
                var parserViewLexErrors = lexicalErrors
                    .Select(ConvertLexicalErrorToSyntaxError)
                    .ToList();

                LexemesGrid.ItemsSource = parserViewLexErrors;
                StatusTextBlock.Text =
                    $"Лексический анализ завершён. Лексических ошибок: {parserViewLexErrors.Count}. " +
                    "Синтаксический анализ не выполнялся.";
                return;
            }

            var syntaxAnalyzer = new SyntaxAnalyzer();
            var syntaxResult = syntaxAnalyzer.Analyze(lexemes);

            LexemesGrid.ItemsSource = syntaxResult.Errors;
            StatusTextBlock.Text = syntaxResult.Message;
        }

        private void ShowCombinedResult(List<Lexeme> lexemes, List<Lexeme> lexicalErrors)
        {
            ConfigureCombinedColumns();

            var rows = new List<CombinedAnalysisRow>();

            foreach (var lex in lexemes)
            {
                rows.Add(new CombinedAnalysisRow
                {
                    Stage = "Лексер",
                    Code = lex.Code.ToString(),
                    Type = lex.Type,
                    Text = lex.Text,
                    Location = lex.Location,
                    Description = lex.IsError ? "Лексическая ошибка" : "",
                    StartIndex = lex.StartIndex,
                    Length = lex.Length
                });
            }

            if (lexicalErrors.Count > 0)
            {
                foreach (var err in lexicalErrors)
                {
                    rows.Add(new CombinedAnalysisRow
                    {
                        Stage = "Парсер",
                        Code = "",
                        Type = "Лексическая ошибка",
                        Text = err.Text,
                        Location = $"строка {err.Line}, позиция {err.ColumnFrom}",
                        Description = "Синтаксический анализ не выполнялся",
                        StartIndex = err.StartIndex,
                        Length = err.Length
                    });
                }

                LexemesGrid.ItemsSource = rows;
                StatusTextBlock.Text =
                    $"Лексический анализ: ошибок {lexicalErrors.Count}. " +
                    "Синтаксический анализ остановлен из-за лексических ошибок.";
                return;
            }

            var syntaxAnalyzer = new SyntaxAnalyzer();
            var syntaxResult = syntaxAnalyzer.Analyze(lexemes);

            if (syntaxResult.Errors.Count == 0)
            {
                rows.Add(new CombinedAnalysisRow
                {
                    Stage = "Парсер",
                    Code = "",
                    Type = "Ошибок нет",
                    Text = "",
                    Location = "",
                    Description = "Синтаксический анализ завершён успешно",
                    StartIndex = -1,
                    Length = 0
                });
            }
            else
            {
                foreach (var err in syntaxResult.Errors)
                {
                    rows.Add(new CombinedAnalysisRow
                    {
                        Stage = "Парсер",
                        Code = "",
                        Type = "Синтаксическая ошибка",
                        Text = err.InvalidFragment,
                        Location = err.Location,
                        Description = err.Description,
                        StartIndex = err.StartIndex,
                        Length = err.Length
                    });
                }
            }

            LexemesGrid.ItemsSource = rows;
            StatusTextBlock.Text =
                $"Лексем: {lexemes.Count}. Синтаксических ошибок: {syntaxResult.Errors.Count}.";
        }

        private void ClearDiagnosticsGrid()
        {
            LexemesGrid.ItemsSource = null;
            LexemesGrid.Items.Clear();
        }

        private SyntaxErrorInfo ConvertLexicalErrorToSyntaxError(Lexeme lexeme)
        {
            return new SyntaxErrorInfo
            {
                InvalidFragment = lexeme.Text,
                Location = $"строка {lexeme.Line}, позиция {lexeme.ColumnFrom}",
                Description = lexeme.Type,
                StartIndex = lexeme.StartIndex,
                Length = lexeme.Length,
                Line = lexeme.Line,
                ColumnFrom = lexeme.ColumnFrom,
                ColumnTo = lexeme.ColumnTo
            };
        }

        private void LexemesGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            HighlightSelectedLexeme();
        }

        private void LexemesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HighlightSelectedLexeme();
        }

        private void HighlightSelectedLexeme()
        {
            switch (LexemesGrid.SelectedItem)
            {
                case Lexeme lex:
                    HighlightFragment(lex.StartIndex, lex.Length);
                    if (lex.IsError)
                        StatusTextBlock.Text = $"Переход к ошибке: {lex.Location}";
                    else
                        StatusTextBlock.Text = $"Выделена лексема: {lex.Text} ({lex.Location})";
                    break;

                case SyntaxErrorInfo error:
                    HighlightFragment(error.StartIndex, error.Length);
                    StatusTextBlock.Text = $"Переход к ошибке: {error.Location}";
                    break;

                case CombinedAnalysisRow row when row.StartIndex >= 0:
                    HighlightFragment(row.StartIndex, row.Length);
                    StatusTextBlock.Text = $"Переход к фрагменту: {row.Location}";
                    break;
            }
        }

        private void HighlightFragment(int startIndex, int fragmentLength)
        {
            int index = startIndex;

            if (index < 0)
                index = 0;

            if (index > EditorTextBox.Text.Length)
                index = EditorTextBox.Text.Length;

            int length = Math.Max(fragmentLength, 1);

            if (index + length > EditorTextBox.Text.Length)
                length = EditorTextBox.Text.Length - index;

            if (length < 0)
                length = 0;

            EditorTextBox.Focus();
            EditorTextBox.Select(index, length);

            int lineIndex = EditorTextBox.GetLineIndexFromCharacterIndex(index);
            EditorTextBox.ScrollToLine(lineIndex);
        }

        private void EditorTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                EditorTextBox.SelectedText = "    ";
                EditorTextBox.CaretIndex = EditorTextBox.SelectionStart;
                e.Handled = true;
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