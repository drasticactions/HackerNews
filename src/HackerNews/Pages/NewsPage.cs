using System;
using System.Collections;
using System.Linq;
using CommunityToolkit.Maui.Markup;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;

namespace HackerNews;

class NewsPage : BaseContentPage<NewsViewModel>
{
	readonly IBrowser _browser;
	readonly IDispatcher _dispatcher;

	public NewsPage(IBrowser browser,
						IDispatcher dispatcher,
						NewsViewModel newsViewModel) : base(newsViewModel, "Top Stories")
	{
		_browser = browser;
		_dispatcher = dispatcher;

		BindingContext.PullToRefreshFailed += HandlePullToRefreshFailed;

		Content = new RefreshView
		{
			RefreshColor = Colors.Black,

			Content = new ListView
			{
				BackgroundColor = Color.FromArgb("F6F6EF"),
				SelectionMode = ListViewSelectionMode.Single,
				ItemTemplate = new StoryDataTemplate(),

			}.Bind(ListView.ItemsSourceProperty, nameof(NewsViewModel.TopStoryCollection))
			 .Invoke(listView => listView.ItemSelected += HandleSelectionChanged)

		}.Bind(RefreshView.IsRefreshingProperty, nameof(NewsViewModel.IsListRefreshing))
		 .Bind(RefreshView.CommandProperty, nameof(NewsViewModel.RefreshCommand));
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		if (Content is RefreshView refreshView
			&& refreshView.Content is ListView listView
			&& IsNullOrEmpty(listView.ItemsSource))
		{
			refreshView.IsRefreshing = true;
		}

		static bool IsNullOrEmpty(in IEnumerable? enumerable) => !enumerable?.GetEnumerator().MoveNext() ?? true;
	}

	async void HandleSelectionChanged(object? sender, SelectedItemChangedEventArgs e)
	{
		ArgumentNullException.ThrowIfNull(sender);

		var listView = (ListView)sender;
		listView.SelectedItem = null;

		if (e.SelectedItem is StoryModel storyModel)
		{
			if (!string.IsNullOrEmpty(storyModel.Url))
			{
				var browserOptions = new BrowserLaunchOptions
				{
					PreferredControlColor = ColorConstants.BrowserNavigationBarTextColor,
					PreferredToolbarColor = ColorConstants.BrowserNavigationBarBackgroundColor
				};

				await _browser.OpenAsync(storyModel.Url, browserOptions);
			}
			else
			{
				await DisplayAlert("Invalid Article", "ASK HN articles have no url", "OK");
			}
		}
	}

	void HandlePullToRefreshFailed(object? sender, string message) =>
		_dispatcher.DispatchAsync(() => DisplayAlert("Refresh Failed", message, "OK"));
}