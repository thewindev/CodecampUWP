﻿using Windows.UI.Core;
using Codecamp.Common.Agenda;
using Codecamp.Common.Models;

namespace Codecamp.UWP.ViewModels
{
    using System.Diagnostics;
    using System.Linq;
    using Windows.Media.SpeechRecognition;
    using System.Threading.Tasks;
    using System;
    using System.ComponentModel;
    using System.Collections.ObjectModel;
    using System.Runtime.CompilerServices;
    using Annotations;

    public class HomeViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Session> _codecampSessions;
        private AgendaService _agendaService;
        private string _spokenText;
        private bool _spokenTextIsVisible;
        private bool _isListening;
        private SpeechRecognizer _speechRecognizer;

        public async Task Activate()
        {
            _agendaService = new AgendaService();
            var sessions = await _agendaService.GetSessionsAsync();
            CodecampSessions = new ObservableCollection<Session>(sessions);
        }

        public ObservableCollection<Session> CodecampSessions
        {
            get { return _codecampSessions; }
            set
            {
                _codecampSessions = value;
                OnPropertyChanged();
            }
        }

        public string SpokenText
        {
            get { return _spokenText; }
            set
            {
                if (value == _spokenText) return;
                _spokenText = value;
                OnPropertyChanged();
            }
        }

        public bool SpokenTextIsVisible
        {
            get { return _spokenTextIsVisible; }
            set
            {
                if (value == _spokenTextIsVisible) return;
                _spokenTextIsVisible = value;
                OnPropertyChanged();
            }
        }

        public bool IsListening
        {
            get { return _isListening; }
            set
            {
                if (value == _isListening) return;
                _isListening = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task FindByVoiceAsync()
        {
            IsListening = true;
            _speechRecognizer = new SpeechRecognizer();
            var wordsFromJson = _agendaService.GetWordsFromJson().Where(w => w.Length >= 3).Distinct().ToList();
            _speechRecognizer.Constraints.Add(new SpeechRecognitionListConstraint(wordsFromJson, "keyword"));
            await _speechRecognizer.CompileConstraintsAsync();
            _speechRecognizer.ContinuousRecognitionSession.ResultGenerated +=
                ContinuousRecognitionSession_ResultGenerated;
            _speechRecognizer.HypothesisGenerated += SpeechRecognizerHypothesisGenerated;
            await _speechRecognizer.ContinuousRecognitionSession.StartAsync();
        }

        private async void SpeechRecognizerHypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            await
                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                    {
                        SpokenTextIsVisible = true;
                        SpokenText = args.Hypothesis.Text;
                    });
        }

        private async void ContinuousRecognitionSession_ResultGenerated(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            await
                  Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                      CoreDispatcherPriority.Normal, () =>
                      {
                          ProcessCommands(args.Result);
                          FindResults(args);
                      });
        }

        private void ProcessCommands(SpeechRecognitionResult result)
        {
            switch (result.Text)
            {
                case "stop":
                    StopVoiceRecognition();
                    break;
            }
        }

        private void StopVoiceRecognition()
        {
            try
            {
                CodecampSessions = new ObservableCollection<Session>(_agendaService.AllSessions);
                _speechRecognizer.Dispose();
                _speechRecognizer = null;
                IsListening = false;
                SpokenTextIsVisible = false;
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.Message);
            }
        }

        private void FindResults(SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            var results = _agendaService.FindSessionsByKeyword(args.Result.Text);
            var list = results.Where(r => r.Value > 0).OrderByDescending(r => r.Value).Take(10);

            CodecampSessions = new ObservableCollection<Session>();
            foreach (var keyValuePair in list)
            {
                CodecampSessions.Add(keyValuePair.Key);
            }
        }

        public async Task FindSessionsByRoom(string roomNumber)
        {
            var results = await _agendaService.FindSessionsByRoom("Room " + roomNumber);
            CodecampSessions = new ObservableCollection<Session>(results);
        }
    }
}