using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using SixteenCoreCharacterMapper.Core.ViewModels;
using System;
using System.Collections.Generic;

namespace SixteenCoreCharacterMapper.Avalonia
{
    public partial class QuestionnaireWindow : Window
    {
        public QuestionnaireWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        public QuestionnaireWindow(bool isDarkMode) : this()
        {
            ApplyTheme(isDarkMode);
        }

        private void ApplyTheme(bool isDarkMode)
        {
            var contentBorder = this.FindControl<Border>("ContentBorder");
            var titleText = this.FindControl<TextBlock>("TitleTextBlock");
            
            if (isDarkMode)
            {
                Background = new SolidColorBrush(Color.FromRgb(25, 25, 25));
                Foreground = Brushes.White;
                
                if (contentBorder != null)
                {
                    contentBorder.Background = new SolidColorBrush(Color.FromRgb(50, 50, 50));
                    contentBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(85, 85, 85));
                }
                
                if (titleText != null)
                {
                    titleText.Foreground = Brushes.White;
                }
            }
            else
            {
                Background = Brushes.White;
                Foreground = Brushes.Black;
                
                if (contentBorder != null)
                {
                    contentBorder.Background = new SolidColorBrush(Color.Parse("#FAFAFA"));
                    contentBorder.BorderBrush = new SolidColorBrush(Color.Parse("#CCCCCC"));
                }
                
                if (titleText != null)
                {
                    titleText.Foreground = new SolidColorBrush(Color.Parse("#555555"));
                }
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is QuestionnaireViewModel vm)
            {
                vm.RequestClose += OnRequestClose;
            }
        }

        private void OnRequestClose((Dictionary<string, double>? Scores, Dictionary<string, int>? Answers, List<string>? Exclusions) result)
        {
            Close(result);
        }
    }
}
