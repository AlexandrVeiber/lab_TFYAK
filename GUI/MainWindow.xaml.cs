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
using GUI.RegexSearch;
using GUI.AutomatonSearch;
using GUI.Semantic;

namespace GUI
{
    public partial class MainWindow : Window
    {
        private string? _currentFilePath = null;
        private bool _isDirty = false;

        private readonly RegexSearchService _regexSearchService = new();
        private readonly MacAddressAutomatonSearcher _macAutomatonSearcher = new();

        private enum AnalysisMode
        {
            LexerOnly,
            ParserOnly,
            Both,
            SemanticAst,
            RegularExpressions,
            MacAutomaton
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
                2 => AnalysisMode.Both,
                3 => AnalysisMode.SemanticAst,
                4 => AnalysisMode.RegularExpressions,
                5 => AnalysisMode.MacAutomaton,
                _ => AnalysisMode.SemanticAst
            };

        private RegexTaskType CurrentRegexTask =>
            RegexTaskComboBox.SelectedIndex switch
            {
                0 => RegexTaskType.Numbers,
                1 => RegexTaskType.FileNames,
                2 => RegexTaskType.MacAddresses,
                _ => RegexTaskType.Numbers
            };

        public MainWindow()
        {
            InitializeComponent();
            UpdateTitle();

            AnalysisModeComboBox.SelectedIndex = 3;
            RegexTaskComboBox.SelectedIndex = 0;

            UpdateRegexControlsVisibility();
            ConfigureGridForCurrentMode();

            StatusTextBlock.Text = "Ожидание...";
            LexemesGrid.ItemsSource = null;
            LexemesGrid.Items.Clear();
            ResultTabControl.SelectedIndex = 0;
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

        private void UpdateRegexControlsVisibility()
        {
            bool isRegexMode = CurrentAnalysisMode == AnalysisMode.RegularExpressions;

            RegexTaskLabel.Visibility = isRegexMode ? Visibility.Visible : Visibility.Collapsed;
            RegexTaskComboBox.Visibility = isRegexMode ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateAstPlaceholder()
        {
            if (!IsLoaded && AstTextBox == null)
                return;

            AstTextBox.Text = CurrentAnalysisMode == AnalysisMode.SemanticAst
                ? "AST появится здесь после запуска семантического анализа."
                : "AST выводится только в режиме «Семантический + AST».";
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
            UpdateRegexControlsVisibility();
            ConfigureGridForCurrentMode();

            StatusTextBlock.Text = CurrentAnalysisMode switch
            {
                AnalysisMode.LexerOnly => "Выбран режим: только лексический анализ.",
                AnalysisMode.ParserOnly => "Выбран режим: только синтаксический анализ.",
                AnalysisMode.Both => "Выбран режим: оба анализа.",
                AnalysisMode.SemanticAst => "Выбран режим: семантический анализ и построение AST.",
                AnalysisMode.RegularExpressions => "Выбран режим: регулярные выражения.",
                AnalysisMode.MacAutomaton => "Выбран режим: автоматный поиск MAC-адресов.",
                _ => "Ожидание..."
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

                case AnalysisMode.SemanticAst:
                    ConfigureSemanticColumns();
                    break;

                case AnalysisMode.RegularExpressions:
                    ConfigureRegexColumns();
                    break;

                case AnalysisMode.MacAutomaton:
                    ConfigureRegexColumns();
                    break;
            }

            UpdateAstPlaceholder();
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

        private void ConfigureSemanticColumns()
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

        private void ConfigureRegexColumns()
        {
            LexemesGrid.Columns.Clear();

            LexemesGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Найденная подстрока",
                Binding = new System.Windows.Data.Binding("MatchedText"),
                Width = new DataGridLength(2, DataGridLengthUnitType.Star),
                MinWidth = 220
            });

            LexemesGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Начальная позиция",
                Binding = new System.Windows.Data.Binding("StartPosition"),
                Width = DataGridLength.Auto,
                MinWidth = 180
            });

            LexemesGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Длина",
                Binding = new System.Windows.Data.Binding("Length"),
                Width = DataGridLength.Auto,
                MinWidth = 90
            });
        }

        // ---------- Пуск ----------
        private void Run_Click(object sender, RoutedEventArgs e)
        {
            string text = EditorTextBox.Text;

            ClearDiagnosticsGrid();
            ConfigureGridForCurrentMode();

            if (CurrentAnalysisMode == AnalysisMode.RegularExpressions)
            {
                ShowRegexResult(text);
                return;
            }

            if (CurrentAnalysisMode == AnalysisMode.MacAutomaton)
            {
                ShowMacAutomatonResult(text);
                return;
            }

            if (string.IsNullOrWhiteSpace(text) &&
                (CurrentAnalysisMode == AnalysisMode.ParserOnly ||
                 CurrentAnalysisMode == AnalysisMode.Both ||
                 CurrentAnalysisMode == AnalysisMode.SemanticAst))
            {
                LexemesGrid.ItemsSource = null;
                StatusTextBlock.Text = "Ожидается строка для анализа.";
                return;
            }

            var scanner = new LexicalAnalyzer();
            var lexemes = scanner.Analyze(text);
            lexemes = MergeAdjacentLexicalErrors(lexemes);

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

                case AnalysisMode.SemanticAst:
                    ShowSemanticResult(lexemes, lexicalErrors);
                    break;
            }
        }

        private void ShowSemanticResult(List<Lexeme> lexemes, List<Lexeme> lexicalErrors)
        {
            ConfigureSemanticColumns();

            if (lexicalErrors.Count > 0)
            {
                var lexicalRows = lexicalErrors
                    .Select(ConvertLexicalErrorToSyntaxError)
                    .ToList();

                LexemesGrid.ItemsSource = lexicalRows;
                AstTextBox.Text = "AST не построено, так как обнаружены лексические ошибки.";
                ResultTabControl.SelectedIndex = 0;

                StatusTextBlock.Text =
                    $"Семантический анализ не выполнен. Сначала исправьте лексические ошибки. Общее количество найденных ошибок: {lexicalRows.Count}.";

                return;
            }

            var syntaxAnalyzer = new SyntaxAnalyzer();
            var syntaxResult = syntaxAnalyzer.Analyze(lexemes);

            if (!syntaxResult.Success)
            {
                LexemesGrid.ItemsSource = syntaxResult.Errors;
                AstTextBox.Text = "AST не построено, так как обнаружены синтаксические ошибки.";
                ResultTabControl.SelectedIndex = 0;

                StatusTextBlock.Text =
                    $"Семантический анализ не выполнен. Сначала исправьте синтаксические ошибки. Общее количество найденных ошибок: {syntaxResult.Errors.Count}.";

                return;
            }

            var semanticAnalyzer = new SemanticAnalyzer();
            var semanticResult = semanticAnalyzer.Analyze(lexemes);

            LexemesGrid.ItemsSource = semanticResult.Errors;
            AstTextBox.Text = semanticResult.AstText;

            ResultTabControl.SelectedIndex = semanticResult.Errors.Count == 0 ? 1 : 0;
            StatusTextBlock.Text = semanticResult.Message;
        }

        private void ShowMacAutomatonResult(string text)
        {
            ConfigureRegexColumns();

            if (string.IsNullOrWhiteSpace(text))
            {
                LexemesGrid.ItemsSource = null;
                StatusTextBlock.Text = "Нет данных для поиска.";
                return;
            }

            var results = _macAutomatonSearcher.Search(text);

            LexemesGrid.ItemsSource = results;

            if (results.Count == 0)
            {
                StatusTextBlock.Text = "Автоматный поиск завершён. Тип: MAC-адреса. Совпадений не найдено.";
            }
            else
            {
                StatusTextBlock.Text = $"Автоматный поиск завершён. Тип: MAC-адреса. Найдено совпадений: {results.Count}.";
            }
        }

        private List<Lexeme> MergeAdjacentLexicalErrors(List<Lexeme> lexemes)
        {
            var merged = new List<Lexeme>();
            int i = 0;

            while (i < lexemes.Count)
            {
                if (!lexemes[i].IsError)
                {
                    merged.Add(lexemes[i]);
                    i++;
                    continue;
                }

                var start = lexemes[i];
                string text = start.Text;
                int startIndex = start.StartIndex;
                int length = start.Length;
                int line = start.Line;
                int colFrom = start.ColumnFrom;
                int colTo = start.ColumnTo;

                int j = i + 1;
                while (j < lexemes.Count &&
                       lexemes[j].IsError &&
                       lexemes[j].StartIndex == startIndex + length)
                {
                    text += lexemes[j].Text;
                    length += lexemes[j].Length;
                    colTo = lexemes[j].ColumnTo;
                    j++;
                }

                merged.Add(new Lexeme
                {
                    Code = start.Code,
                    Type = start.Type,
                    Text = text,
                    Location = $"строка {line}, {colFrom}-{colTo}",
                    StartIndex = startIndex,
                    Length = length,
                    IsError = true,
                    Line = line,
                    ColumnFrom = colFrom,
                    ColumnTo = colTo
                });

                i = j;
            }

            return merged;
        }

        private void ShowLexerResult(List<Lexeme> lexemes)
        {
            ConfigureLexerColumns();
            LexemesGrid.ItemsSource = lexemes;

            int errorCount = lexemes.Count(l => l.IsError);

            StatusTextBlock.Text =
                $"Лексический анализ завершён. Найдено лексем: {lexemes.Count}. " +
                $"Общее количество найденных ошибок: {errorCount}.";
        }

        private void ShowParserResult(List<Lexeme> lexemes, List<Lexeme> lexicalErrors)
        {
            ConfigureParserColumns();

            var syntaxAnalyzer = new SyntaxAnalyzer();
            var syntaxResult = syntaxAnalyzer.Analyze(lexemes);

            var allErrors = lexicalErrors
                .Select(ConvertLexicalErrorToSyntaxError)
                .ToList();

            allErrors.AddRange(syntaxResult.Errors);

            LexemesGrid.ItemsSource = allErrors;

            if (allErrors.Count == 0)
            {
                StatusTextBlock.Text =
                    "Синтаксический анализ завершён. Общее количество найденных ошибок: 0.";
            }
            else
            {
                StatusTextBlock.Text =
                    $"Синтаксический анализ завершён. Общее количество найденных ошибок: {allErrors.Count}.";
            }
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

            var syntaxAnalyzer = new SyntaxAnalyzer();
            var syntaxResult = syntaxAnalyzer.Analyze(lexemes);

            foreach (var err in lexicalErrors)
            {
                rows.Add(new CombinedAnalysisRow
                {
                    Stage = "Парсер",
                    Code = "",
                    Type = "Лексическая ошибка",
                    Text = err.Text,
                    Location = $"строка {err.Line}, позиция {err.ColumnFrom}",
                    Description = err.Type,
                    StartIndex = err.StartIndex,
                    Length = err.Length
                });
            }

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

            if (lexicalErrors.Count == 0 && syntaxResult.Errors.Count == 0)
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

            LexemesGrid.ItemsSource = rows;

            int totalErrors = lexicalErrors.Count + syntaxResult.Errors.Count;
            StatusTextBlock.Text =
                $"Лексический и синтаксический анализ завершены. Общее количество найденных ошибок: {totalErrors}.";
        }

        private void ShowRegexResult(string text)
        {
            ConfigureRegexColumns();

            if (string.IsNullOrWhiteSpace(text))
            {
                LexemesGrid.ItemsSource = null;
                StatusTextBlock.Text = "Нет данных для поиска.";
                return;
            }

            var taskType = CurrentRegexTask;
            var results = _regexSearchService.Search(text, taskType);

            LexemesGrid.ItemsSource = results;

            string taskTitle = _regexSearchService.GetTaskTitle(taskType);

            if (results.Count == 0)
            {
                StatusTextBlock.Text = $"Поиск завершён. Тип: {taskTitle}. Совпадений не найдено.";
            }
            else
            {
                StatusTextBlock.Text = $"Поиск завершён. Тип: {taskTitle}. Найдено совпадений: {results.Count}.";
            }
        }

        private void ClearDiagnosticsGrid()
        {
            LexemesGrid.ItemsSource = null;
            LexemesGrid.Items.Clear();
            AstTextBox.Clear();
            ResultTabControl.SelectedIndex = 0;
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

                case SemanticErrorInfo semanticError:
                    HighlightFragment(semanticError.StartIndex, semanticError.Length);
                    StatusTextBlock.Text = $"Переход к ошибке: {semanticError.Location}";
                    break;

                case CombinedAnalysisRow row when row.StartIndex >= 0:
                    HighlightFragment(row.StartIndex, row.Length);
                    StatusTextBlock.Text = $"Переход к фрагменту: {row.Location}";
                    break;

                case RegexSearchResult regexResult:
                    HighlightFragment(regexResult.StartIndex, regexResult.Length);
                    StatusTextBlock.Text = $"Переход к найденному фрагменту: {regexResult.StartPosition}";
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