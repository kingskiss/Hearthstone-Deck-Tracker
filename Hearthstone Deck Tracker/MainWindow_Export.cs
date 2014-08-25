﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Hearthstone_Deck_Tracker.Hearthstone;
using MahApps.Metro.Controls.Dialogs;

namespace Hearthstone_Deck_Tracker
{
	public partial class MainWindow
	{

		private async void BtnExport_Click(object sender, RoutedEventArgs e)
		{
			var deck = DeckPickerList.SelectedDeck;
			if(deck == null) return;

			var message = "Please create a new, empty " + deck.Class +
						  "-Deck in Hearthstone before continuing (leave the deck creation screen open).\nDo not move your mouse after clicking OK!";

			if(deck.Cards.Any(c => c.Name == "Stalagg" || c.Name == "Feugen"))
				message += "\n\nIMPORTANT: If you own golden versions of Feugen or Stalagg please make sure to configure\nOptions > Other > Exporting";

			var result = await this.ShowMessageAsync("Export " + deck.Name + " to Hearthstone",
																  message,
																  MessageDialogStyle.AffirmativeAndNegative);

			if(result == MessageDialogResult.Affirmative)
			{
				var controller =
					await this.ShowProgressAsync("Creating Deck", "Please do not move your mouse or type.");
				Topmost = false;
				await Task.Delay(500);
				await DeckExporter.Export(DeckPickerList.SelectedDeck);
				await controller.CloseAsync();
			}


		}

		private async void BtnScreenhot_Click(object sender, RoutedEventArgs e)
		{
			if(DeckPickerList.SelectedDeck == null) return;
			var screenShotWindow = new PlayerWindow(Config.Instance, DeckPickerList.SelectedDeck.Cards, true);
			screenShotWindow.Show();
			screenShotWindow.Top = 0;
			screenShotWindow.Left = 0;
			await Task.Delay(100);
			var source = PresentationSource.FromVisual(screenShotWindow);
			if(source == null) return;

			var dpiX = 96.0 * source.CompositionTarget.TransformToDevice.M11;
			var dpiY = 96.0 * source.CompositionTarget.TransformToDevice.M22;

			var fileName = Helper.ScreenshotDeck(screenShotWindow.ListViewPlayer, dpiX, dpiY,
												 DeckPickerList.SelectedDeck.Name);

			screenShotWindow.Shutdown();
			if(fileName == null)
				await this.ShowMessageAsync("", "Error saving screenshot");
			else
				await ShowSavedFileMessage(fileName, "Screenshots");


		}

		private async void BtnSaveToFile_OnClick(object sender, RoutedEventArgs e)
		{
			var deck = DeckPickerList.SelectedDeck;
			if(deck == null) return;
			var path = Helper.GetValidFilePath("SavedDecks", deck.Name, ".xml");
			XmlManager<Deck>.Save(path, deck);
			await ShowSavedFileMessage(path, "SavedDecks");



		}

		private void BtnClipboard_OnClick(object sender, RoutedEventArgs e)
		{
			var deck = DeckPickerList.SelectedDeck;
			if(deck == null) return;
			Clipboard.SetText(Helper.DeckToIdString(deck));
			ShowMessage("", "copied to clipboard");


		}
	}
}
