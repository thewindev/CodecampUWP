﻿using System;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Codecamp.UWP.Views
{
    using ViewModels;

    public sealed partial class HomeView
    {
        public HomeView()
        {
            InitializeComponent();
            Loaded += ViewLoaded;
            SizeChanged += ViewSizeChanged;
        }

        private void ViewSizeChanged(object sender, SizeChangedEventArgs e)
        {
            //SessionsGridView.Height = Window.Current.Bounds.Height - (VoiceGrid.ActualHeight + TitleBox.ActualHeight + 70);

            //UpdateItems();
        }

        private void UpdateItems()
        {
            foreach (var item in SessionsGridView.Items)
            {
                var template = SessionsGridView.ContainerFromItem(item);
                if (template == null) continue;
                var border = GetChildOfType<Border>(template);
                var width = Window.Current.Bounds.Width;
                border.Width = width/7;
                border.Height = (template as GridViewItem).ActualHeight;
            }
        }

        public T GetChildOfType<T>(DependencyObject depObj)
    where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        private async void ViewLoaded(object sender, RoutedEventArgs e)
        {
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(VoiceGrid);
        }

        public HomeViewModel ViewModel => DataContext as HomeViewModel;

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await ViewModel.Activate();
            SpeechRecognitionResult result = e.Parameter as SpeechRecognitionResult;
            if (result != null)
            {
                TextSpoken.Text = result.Text;
                ProcessCommands(result);
            }
        }

        public async void ProcessCommands(SpeechRecognitionResult result)
        {
            string voiceCommandName = result.RulePath.First();
            SpeechSynthesizer synthesizer = new SpeechSynthesizer();
            switch (voiceCommandName)
            {
                case "showSessions":
                    var stream = await synthesizer.SynthesizeTextToStreamAsync("There are " + ViewModel.CodecampSessions.Count + " sessions!");
                    AudioPlayer.SetSource(stream, string.Empty);
                    break;
                case "findSessions":
                    string tag = result.SemanticInterpretation.Properties["tag"][0];
                    var sessionCount = ViewModel.CodecampSessions.Count(s => s.Tags.Contains(tag));
                    var findStream = await synthesizer.SynthesizeTextToStreamAsync("There are " + sessionCount + " sessions related to " + tag + "!");
                    AudioPlayer.SetSource(findStream, string.Empty);
                    break;
                case "findSessionsByKeyword":
                    var keyword = result.SemanticInterpretation.Properties["keyword"][0];
                    // Debug.WriteLine(key);
                    break;
            }
        }

        private async void FindByVoice(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await ViewModel.FindByVoiceAsync());
        }

        private void BorderLoaded(object sender, RoutedEventArgs e)
        {
            var border = sender as Border;
            //border.Width = Window.Current.Bounds.Width / 7;
        }
    }
}