using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using CellReport.Editor.Avalonia.ViewModels;
using System.Collections;
using System.Collections.Specialized;

namespace CellReport.Editor.Avalonia.Controls;

public class StyleGallery : ContentControl
{
	public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
		AvaloniaProperty.Register<StyleGallery, IEnumerable?>(nameof(ItemsSource));

	public static readonly StyledProperty<object?> SelectedItemProperty =
		AvaloniaProperty.Register<StyleGallery, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

	private readonly StylePreview _current = new() { Width = 156, Height = 40 };
	private readonly Button _button = new();
	private readonly Flyout _flyout = new() { Placement = PlacementMode.BottomEdgeAlignedLeft };
	private IEnumerable? _watched;

	public StyleGallery()
	{
		_button.Background = Brushes.Transparent;
		_button.Padding = new Thickness(2);
		_button.HorizontalAlignment = HorizontalAlignment.Left;
		_button.VerticalAlignment = VerticalAlignment.Center;
		_button.Content = _current;
		_button.Flyout = _flyout;
		Content = _button;
	}

	public IEnumerable? ItemsSource
	{
		get => GetValue(ItemsSourceProperty);
		set => SetValue(ItemsSourceProperty, value);
	}

	public object? SelectedItem
	{
		get => GetValue(SelectedItemProperty);
		set => SetValue(SelectedItemProperty, value);
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);
		if (change.Property == SelectedItemProperty)
			_current.Choice = SelectedItem as StyleChoice;
		else if (change.Property == ItemsSourceProperty)
			WatchItems();
	}

	private void WatchItems()
	{
		if (_watched is INotifyCollectionChanged previous)
			previous.CollectionChanged -= OnItemsChanged;

		_watched = ItemsSource;
		if (_watched is INotifyCollectionChanged next)
			next.CollectionChanged += OnItemsChanged;

		Rebuild();
	}

	private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		Rebuild();
	}

	private void Rebuild()
	{
		var panel = new WrapPanel { Orientation = Orientation.Horizontal, MaxWidth = 360 };
		if (ItemsSource != null)
		{
			foreach (var item in ItemsSource)
			{
				if (item is not StyleChoice choice)
					continue;

				var preview = new StylePreview { Choice = choice, Width = 168, Height = 40 };
				var itemButton = new Button
				{
					Background = Brushes.Transparent,
					Padding = new Thickness(2),
					Margin = new Thickness(2),
					Content = preview
				};
				itemButton.Click += (_, _) =>
				{
					SelectedItem = choice;
					_flyout.Hide();
				};
				panel.Children.Add(itemButton);
			}
		}

		_flyout.Content = new ScrollViewer
		{
			MaxHeight = 280,
			Content = panel
		};
	}
}
