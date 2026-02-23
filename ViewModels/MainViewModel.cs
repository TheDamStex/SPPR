using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using SPPR.Helpers;
using SPPR.Models;
using SPPR.Services;

namespace SPPR.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly DecisionService _decisionService;
    private readonly TrainingService _trainingService;
    private readonly KnowledgeStorageService _storageService;

    private KnowledgeBase? _knowledgeBase;
    private bool _hasRecognition;
    private int _predictedIndex = -1;
    private string _predictedObjectName = "Ще не розпізнано";
    private string _explanationText = "";
    private string _trainingChangesText = "";
    private string _statusMessage = "Готово";
    private string? _selectedTrueObject;

    public MainViewModel(
        DecisionService decisionService,
        TrainingService trainingService,
        KnowledgeStorageService storageService)
    {
        _decisionService = decisionService;
        _trainingService = trainingService;
        _storageService = storageService;

        Features = new ObservableCollection<FeatureViewModel>();
        Results = new ObservableCollection<ResultRowViewModel>();
        Objects = new ObservableCollection<string>();

        RecognizeCommand = new RelayCommand(_ => Recognize(), _ => _knowledgeBase != null);
        TrainCommand = new RelayCommand(_ => Train(), _ => CanTrain());
        SaveCommand = new RelayCommand(_ => Save(), _ => _knowledgeBase != null);
        LoadCommand = new RelayCommand(_ => LoadFromFile());
        ResetCommand = new RelayCommand(_ => ResetToDefault());

        InitializeKnowledge();
    }

    public ObservableCollection<FeatureViewModel> Features { get; }
    public ObservableCollection<ResultRowViewModel> Results { get; }
    public ObservableCollection<string> Objects { get; }

    public ICommand RecognizeCommand { get; }
    public ICommand TrainCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand LoadCommand { get; }
    public ICommand ResetCommand { get; }

    public string? SelectedTrueObject
    {
        get => _selectedTrueObject;
        set
        {
            if (SetProperty(ref _selectedTrueObject, value))
            {
                RaiseCommandStates();
            }
        }
    }

    public string PredictedObjectName
    {
        get => _predictedObjectName;
        set => SetProperty(ref _predictedObjectName, value);
    }

    public string ExplanationText
    {
        get => _explanationText;
        set => SetProperty(ref _explanationText, value);
    }

    public string TrainingChangesText
    {
        get => _trainingChangesText;
        set => SetProperty(ref _trainingChangesText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    private void InitializeKnowledge()
    {
        try
        {
            if (_storageService.Exists())
            {
                var loaded = _storageService.Load();
                if (loaded == null)
                {
                    MessageBox.Show("Не вдалося прочитати knowledge.json. Буде створена стандартна база.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    _knowledgeBase = BuildDefaultKnowledgeBase();
                    StatusMessage = "Стандартну базу створено (файл був некоректний).";
                }
                else
                {
                    _knowledgeBase = loaded;
                    StatusMessage = "Базу знань завантажено з knowledge.json.";
                }
            }
            else
            {
                _knowledgeBase = BuildDefaultKnowledgeBase();
                StatusMessage = "knowledge.json не знайдено. Створено стандартну базу. Рекомендується натиснути 'Зберегти'.";
            }

            RefreshCollections();
        }
        catch (Exception ex)
        {
            _knowledgeBase = BuildDefaultKnowledgeBase();
            RefreshCollections();
            MessageBox.Show($"Помилка завантаження: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Завантаження завершилось з помилкою. Використовується стандартна база.";
        }
    }

    private void RefreshCollections()
    {
        Features.Clear();
        Results.Clear();
        Objects.Clear();

        if (_knowledgeBase == null)
        {
            return;
        }

        foreach (var feature in _knowledgeBase.Features)
        {
            Features.Add(new FeatureViewModel(feature.Name));
        }

        foreach (var foodObject in _knowledgeBase.Objects)
        {
            Objects.Add(foodObject.Name);
        }

        SelectedTrueObject = Objects.FirstOrDefault();
        _hasRecognition = false;
        _predictedIndex = -1;
        PredictedObjectName = "Ще не розпізнано";
        ExplanationText = string.Empty;
        TrainingChangesText = string.Empty;
        RaiseCommandStates();
    }

    private void Recognize()
    {
        if (_knowledgeBase == null)
        {
            return;
        }

        var answers = Features.Select(f => f.IsSelected).ToList();
        var recognitionResult = _decisionService.Recognize(_knowledgeBase, answers);

        if (recognitionResult.WinnerIndex < 0 || recognitionResult.WinnerIndex >= _knowledgeBase.Objects.Count)
        {
            Results.Clear();
            _predictedIndex = -1;
            _hasRecognition = false;
            PredictedObjectName = "Неможливо розпізнати";
            ExplanationText = recognitionResult.ExplanationText;
            TrainingChangesText = "";
            StatusMessage = "Розпізнавання не виконано: перевірте структуру бази знань.";
            RaiseCommandStates();
            return;
        }

        Results.Clear();
        for (var i = 0; i < _knowledgeBase.Objects.Count; i++)
        {
            Results.Add(new ResultRowViewModel
            {
                ObjectName = _knowledgeBase.Objects[i].Name,
                Score = i < recognitionResult.Scores.Count ? recognitionResult.Scores[i] : 0,
                IsWinner = i == recognitionResult.WinnerIndex
            });
        }

        _predictedIndex = recognitionResult.WinnerIndex;
        _hasRecognition = true;
        PredictedObjectName = _knowledgeBase.Objects[_predictedIndex].Name;
        ExplanationText = recognitionResult.ExplanationText;
        TrainingChangesText = "";
        StatusMessage = "Розпізнавання виконано.";
        RaiseCommandStates();
    }

    private void Train()
    {
        if (_knowledgeBase == null)
        {
            return;
        }

        if (!_hasRecognition)
        {
            MessageBox.Show("Спочатку виконайте розпізнавання.", "Увага", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedTrueObject))
        {
            MessageBox.Show("Оберіть правильний об'єкт.", "Увага", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var trueIndex = _knowledgeBase.Objects.FindIndex(o => o.Name == SelectedTrueObject);
        if (trueIndex < 0)
        {
            MessageBox.Show("Не вдалося знайти обраний об'єкт.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var answers = Features.Select(f => f.IsSelected).ToList();
        var trainResult = _trainingService.Train(_knowledgeBase, answers, _predictedIndex, trueIndex);

        TrainingChangesText = trainResult.ChangesText;
        StatusMessage = trainResult.WeightsChanged ? "Навчання виконано, ваги змінено." : "Навчання не змінило ваги.";

        if (_hasRecognition)
        {
            Recognize();
        }
    }

    private void Save()
    {
        if (_knowledgeBase == null)
        {
            MessageBox.Show("Немає бази знань для збереження.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _storageService.Save(_knowledgeBase);
            StatusMessage = $"Базу знань збережено: {_storageService.GetKnowledgePath()}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка збереження: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Помилка збереження.";
        }
    }

    private void LoadFromFile()
    {
        try
        {
            var loaded = _storageService.Load();
            if (loaded == null)
            {
                MessageBox.Show("Файл knowledge.json не знайдено.", "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                StatusMessage = "Файл knowledge.json відсутній.";
                return;
            }

            _knowledgeBase = loaded;
            RefreshCollections();
            StatusMessage = "Базу знань завантажено вручну.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Помилка завантаження: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Помилка завантаження.";
        }
    }

    private void ResetToDefault()
    {
        _knowledgeBase = BuildDefaultKnowledgeBase();
        RefreshCollections();
        StatusMessage = "Базу знань скинуто до стандартних значень.";
    }

    private bool CanTrain()
    {
        return _knowledgeBase != null && _hasRecognition && !string.IsNullOrWhiteSpace(SelectedTrueObject);
    }

    private void RaiseCommandStates()
    {
        if (RecognizeCommand is RelayCommand recognize)
        {
            recognize.RaiseCanExecuteChanged();
        }

        if (TrainCommand is RelayCommand train)
        {
            train.RaiseCanExecuteChanged();
        }

        if (SaveCommand is RelayCommand save)
        {
            save.RaiseCanExecuteChanged();
        }
    }

    private static KnowledgeBase BuildDefaultKnowledgeBase()
    {
        return new KnowledgeBase
        {
            Objects = new List<FoodObject>
            {
                new() { Name = "Борщ" },
                new() { Name = "Піца" },
                new() { Name = "Суші" },
                new() { Name = "Овочевий салат" },
                new() { Name = "Млинці" }
            },
            Features = new List<Feature>
            {
                new() { Name = "гаряча страва" },
                new() { Name = "містить м’ясо" },
                new() { Name = "містить тісто/борошно" },
                new() { Name = "містить рис" },
                new() { Name = "солона" },
                new() { Name = "солодка" },
                new() { Name = "є овочі" },
                new() { Name = "подається з соусом/сметаною" }
            },
            Weights = new List<List<int>>
            {
                new() { 9, 6, 1, 0, 8, 0, 9, 7 }, // Борщ
                new() { 8, 5, 9, 0, 8, 1, 4, 8 }, // Піца
                new() { 2, 4, 1, 10, 7, 0, 2, 8 }, // Суші
                new() { 1, 0, 1, 0, 5, 1, 10, 4 }, // Овочевий салат
                new() { 7, 1, 9, 0, 3, 9, 1, 7 }  // Млинці
            }
        };
    }
}
