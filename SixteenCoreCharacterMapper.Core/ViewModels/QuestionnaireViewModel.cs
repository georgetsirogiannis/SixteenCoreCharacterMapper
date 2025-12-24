using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SixteenCoreCharacterMapper.Core.Models;
using SixteenCoreCharacterMapper.Core.Properties;
using SixteenCoreCharacterMapper.Core.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace SixteenCoreCharacterMapper.Core.ViewModels
{
    public partial class QuestionViewModel : ObservableObject
    {
        public PersonalityQuestion Question { get; }

        [ObservableProperty]
        private int _answer; // 0 = not answered, 1-5 = answer

        public QuestionViewModel(PersonalityQuestion question)
        {
            Question = question;
        }
    }

    public partial class QuestionGroupViewModel : ObservableObject
    {
        public string Title { get; }
        public List<QuestionViewModel> Questions { get; }

        [ObservableProperty]
        private bool _includeInCalculation = true;

        public QuestionGroupViewModel(string title, List<QuestionViewModel> questions)
        {
            Title = title;
            Questions = questions;
        }
    }

    public partial class QuestionnaireViewModel : ObservableObject
    {
        private readonly IQuestionnaireService _questionnaireService;
        private List<QuestionGroupViewModel> _groups = new();
        private Dictionary<string, int> _existingAnswers = new();
        private List<string> _existingExclusions = new();

        [ObservableProperty]
        private QuestionGroupViewModel? _currentGroup;

        [ObservableProperty]
        private int _currentGroupIndex;

        [ObservableProperty]
        private int _totalGroups;

        [ObservableProperty]
        private double _progress;

        [ObservableProperty]
        private bool _isLoading = true;

        [ObservableProperty]
        private bool _isLastPage;

        [ObservableProperty]
        private bool _isFirstPage = true;

        public event Action<(Dictionary<string, double>? Scores, Dictionary<string, int>? Answers, List<string>? Exclusions)>? RequestClose;

        public QuestionnaireViewModel(IQuestionnaireService questionnaireService, Dictionary<string, int>? existingAnswers = null, List<string>? existingExclusions = null)
        {
            _questionnaireService = questionnaireService;
            if (existingAnswers != null)
            {
                _existingAnswers = existingAnswers;
            }
            if (existingExclusions != null)
            {
                _existingExclusions = existingExclusions;
            }
            LoadQuestionsCommand.Execute(null);
        }

        [RelayCommand]
        private async Task LoadQuestionsAsync()
        {
            IsLoading = true;
            var questions = await _questionnaireService.LoadQuestionsAsync();
            
            // Group by Trait
            var grouped = questions.GroupBy(q => q.TraitId);
            
            _groups = grouped.Select(g => 
            {
                var traitName = Strings.ResourceManager.GetString($"Trait_{g.Key}_Name") ?? g.Key;

                var groupVm = new QuestionGroupViewModel(
                    traitName, 
                    g.Select(q => 
                    {
                        var qvm = new QuestionViewModel(q);
                        if (_existingAnswers.TryGetValue(q.Text, out int val))
                        {
                            qvm.Answer = val;
                        }
                        return qvm;
                    }).ToList()
                );
                
                if (_existingExclusions.Contains(g.Key))
                {
                    groupVm.IncludeInCalculation = false;
                }
                
                return groupVm;
            }).ToList();

            TotalGroups = _groups.Count;
            
            if (_groups.Any())
            {
                CurrentGroupIndex = 0;
                CurrentGroup = _groups[0];
                UpdateProgress();
            }

            IsLoading = false;
        }

        [RelayCommand]
        private void Next()
        {
            if (CurrentGroupIndex < TotalGroups - 1)
            {
                CurrentGroupIndex++;
                CurrentGroup = _groups[CurrentGroupIndex];
                UpdateProgress();
            }
            else
            {
                Submit();
            }
        }

        [RelayCommand]
        private void Previous()
        {
            if (CurrentGroupIndex > 0)
            {
                CurrentGroupIndex--;
                CurrentGroup = _groups[CurrentGroupIndex];
                UpdateProgress();
            }
        }

        [RelayCommand]
        private void SkipGroup()
        {
            Next();
        }

        [RelayCommand]
        private void SkipRest()
        {
            Submit();
        }

        [RelayCommand]
        private void Cancel()
        {
            RequestClose?.Invoke((null, null, null));
        }

        private void Submit()
        {
            // Collect answers
            var answersToScore = new Dictionary<PersonalityQuestion, int>();
            var rawAnswers = new Dictionary<string, int>();
            var exclusions = new List<string>();

            foreach (var group in _groups)
            {
                if (!group.IncludeInCalculation)
                {
                    exclusions.Add(group.Title);
                }

                // Check if this group has ANY answers
                bool hasAnyAnswer = group.Questions.Any(q => q.Answer > 0);

                foreach (var q in group.Questions)
                {
                    // Save raw answer for persistence
                    if (q.Answer > 0)
                    {
                        rawAnswers[q.Question.Text] = q.Answer;
                    }

                    if (hasAnyAnswer && group.IncludeInCalculation)
                    {
                        // Treat unanswered (0) as Neutral (3) ONLY if the group is being scored
                        int effectiveAnswer = q.Answer > 0 ? q.Answer : 3;
                        answersToScore[q.Question] = effectiveAnswer;
                    }
                }
            }

            var scores = _questionnaireService.CalculateScores(answersToScore);
            RequestClose?.Invoke((scores, rawAnswers, exclusions));
        }

        private void UpdateProgress()
        {
            Progress = (double)(CurrentGroupIndex + 1) / TotalGroups * 100;
            IsFirstPage = CurrentGroupIndex == 0;
            IsLastPage = CurrentGroupIndex == TotalGroups - 1;
        }
    }
}
