﻿#region

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Hearthstone_Deck_Tracker.Annotations;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.HearthStats.API;
using Hearthstone_Deck_Tracker.Stats;
using Hearthstone_Deck_Tracker.Utility;

#endregion

namespace Hearthstone_Deck_Tracker.Windows
{
	public partial class GameResultNotificationWindow
	{
		private const int ExpandedHeight = 100;
		private const int ExpandedWidth = 350;
		private const double FadeInDuration = 0.4;
		private const int FadeOutSpeedup = 2;
		private readonly GameStats _game;
		private bool _edited;
		private bool _expanded;
		private DateTime _startUpTime;

		public GameResultNotificationWindow(string deckName, [NotNull] GameStats game)
		{
			InitializeComponent();
			DeckName = deckName;
			_game = game;
			ComboBoxOpponentClass.ItemsSource = Enum.GetValues(typeof(HeroClass)).Cast<HeroClass>().Select(x => new HeroClassWrapper(x));
			ComboBoxResult.ItemsSource = new[] {GameResult.Win, GameResult.Loss};
			ComboBoxGameMode.ItemsSource = new[]
			{
				GameMode.Arena,
				GameMode.Brawl,
				GameMode.Casual,
				GameMode.Friendly,
				GameMode.Practice,
				GameMode.Ranked,
				GameMode.Spectator
			};
			UpdatePosition();
			_startUpTime = DateTime.UtcNow;
			CloseAsync();
			Logger.WriteLine("Now showing", "GameResultNotification");
			Activate();
		}

		public string DeckName { get; set; }

		public GameResult Result
		{
			get { return _game.Result; }
			set
			{
				_game.Result = value;
				_edited = true;
			}
		}

		public HeroClassWrapper Opponent
		{
			get
			{
				HeroClass heroClass;
				if(Enum.TryParse(_game.OpponentHero, out heroClass))
					return new HeroClassWrapper(heroClass);
				return null;
			}
			set
			{
				if(value != null)
				{
					HeroClass heroClass;
					if(Enum.TryParse(value.Class, out heroClass))
					{
						_game.OpponentHero = heroClass.ToString();
						_edited = true;
					}
				}
			}
		}

		public BitmapImage PlayerClassImage
		{
			get { return ImageCache.GetClassIcon(_game.PlayerHero); }
		}

		public GameMode Mode
		{
			get { return _game.GameMode; }
			set
			{
				_game.GameMode = value;
				_edited = true;
			}
		}

		private void UpdatePosition()
		{
			Top = SystemParameters.WorkArea.Bottom - Height - 5;
			Left = SystemParameters.WorkArea.Right - Width - 5;
		}

		private async void CloseAsync()
		{
			while(DateTime.UtcNow - _startUpTime < TimeSpan.FromSeconds(Config.Instance.NotificationFadeOutDelay + FadeInDuration))
			{
				await Task.Delay(100);
				if(IsMouseOver)
				{
					Expand();
					_startUpTime = DateTime.UtcNow - TimeSpan.FromSeconds(FadeOutSpeedup);
					CloseAsync();
					return;
				}
			}
			((Storyboard)FindResource("StoryboardFadeOut")).Begin(this);
		}

		private void StoryboardFadeOut_OnCompleted(object sender, EventArgs e)
		{
			if(_edited)
			{
				if(Config.Instance.HearthStatsAutoUploadNewGames && HearthStatsAPI.IsLoggedIn)
				{
					var deck = DeckList.Instance.Decks.FirstOrDefault(d => d.DeckId == _game.DeckId);
					if(deck != null)
					{
						if(_game.HasHearthStatsId)
						{
							if(_game.GameMode == GameMode.Arena)
								HearthStatsManager.UpdateArenaMatchAsync(_game, deck, true, true);
							else
								HearthStatsManager.UpdateMatchAsync(_game, deck.GetVersion(_game.PlayerDeckVersion), true, true);
						}
						else
						{
							if(_game.GameMode == GameMode.Arena)
								HearthStatsManager.UploadArenaMatchAsync(_game, deck, true, true);
							else
								HearthStatsManager.UploadMatchAsync(_game, deck.GetVersion(_game.PlayerDeckVersion), true, true);
						}
					}
				}
			}
			Close();
		}

		private void RectangleSettings_OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			Core.MainWindow.FlyoutOptions.IsOpen = true;
			Core.MainWindow.Options.TreeViewItemTrackerNotifications.IsSelected = true;
			Core.MainWindow.ActivateWindow();
		}

		private void Expand()
		{
			if(_expanded)
				return;
			_expanded = true;
			PanelSummary.Visibility = Visibility.Collapsed;
			PanelDetailHeader.Visibility = Visibility.Visible;
			PanelDetailBody.Visibility = Visibility.Visible;
			Height = ExpandedHeight;
			Width = ExpandedWidth;
			UpdatePosition();
		}

		public class HeroClassWrapper
		{
			private readonly string _heroClass;

			public HeroClassWrapper(HeroClass heroClass)
			{
				_heroClass = heroClass.ToString();
			}

			public BitmapImage ClassImage
			{
				get { return ImageCache.GetClassIcon(_heroClass); }
			}

			public string Class
			{
				get { return _heroClass; }
			}

			public override bool Equals(object obj)
			{
				var hcw = obj as HeroClassWrapper;
				return hcw != null && Equals(hcw);
			}

			protected bool Equals(HeroClassWrapper other)
			{
				return string.Equals(_heroClass, other._heroClass);
			}

			public override int GetHashCode()
			{
				return (_heroClass != null ? _heroClass.GetHashCode() : 0);
			}
		}
	}
}