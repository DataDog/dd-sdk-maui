using Datadog.Maui;
using Datadog.Maui.Http;

namespace MauiApp1;

public partial class MainPage : ContentPage
{
	int count = 0;
	private readonly HttpClient _httpClient;

	public MainPage()
	{
		InitializeComponent();

		// Create HttpClient with Datadog tracking
		var handler = new DatadogHttpMessageHandler();
		_httpClient = new HttpClient(handler);
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		Console.WriteLine("[DEBUG] OnAppearing - checking SDK status");
		Console.WriteLine($"[DEBUG] DatadogSdk.IsInitialized: {DatadogSdk.Instance.IsInitialized}");

		// Track view start
		try
		{
			Rum.Instance.StartView("MainPage", "Main Page", new Dictionary<string, object>
			{
				["screen_type"] = "home"
			});
			Console.WriteLine("[RUM] Started view: MainPage - SUCCESS");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[RUM] StartView FAILED: {ex.Message}");
		}
	}

	protected override void OnDisappearing()
	{
		// Track view stop
		Rum.Instance.StopView("MainPage");
		System.Diagnostics.Debug.WriteLine("[RUM] Stopped view: MainPage");
		base.OnDisappearing();
	}

	private void OnCounterClicked(object? sender, EventArgs e)
	{
		count++;

		// Track the button tap action
		Rum.Instance.AddAction(RumActionType.Tap, "Counter Button", new Dictionary<string, object>
		{
			["click_count"] = count
		});
		System.Diagnostics.Debug.WriteLine($"[RUM] Added action: Counter Button (count={count})");

		// Simulate an error on every 5th click
		if (count % 5 == 0)
		{
			Rum.Instance.AddError(
				$"Simulated error at count {count}",
				RumErrorSource.Source,
				"MainPage.OnCounterClicked:45",
				new Dictionary<string, object>
				{
					["count"] = count
				});
			System.Diagnostics.Debug.WriteLine($"[RUM] Added error at count {count}");
		}

		if (count == 1)
			CounterBtn.Text = $"Clicked {count} time";
		else
			CounterBtn.Text = $"Clicked {count} times";

		SemanticScreenReader.Announce(CounterBtn.Text);
	}

	private async void OnTestHttpClicked(object? sender, EventArgs e)
	{
		StatusLabel.Text = "Making HTTP request...";
		Console.WriteLine("[TEST] Starting HTTP request test");

		try
		{
			// Test successful request
			var response = await _httpClient.GetAsync("https://httpbin.org/get");
			var content = await response.Content.ReadAsStringAsync();

			StatusLabel.Text = $"HTTP {(int)response.StatusCode} - {content.Length} bytes";
			Console.WriteLine($"[TEST] HTTP request succeeded: {response.StatusCode}");

			// Also test a POST request
			var postResponse = await _httpClient.PostAsync(
				"https://httpbin.org/post",
				new StringContent("{\"test\": true}", System.Text.Encoding.UTF8, "application/json"));

			Console.WriteLine($"[TEST] POST request succeeded: {postResponse.StatusCode}");
		}
		catch (Exception ex)
		{
			StatusLabel.Text = $"HTTP Error: {ex.Message}";
			Console.WriteLine($"[TEST] HTTP request failed: {ex.Message}");
		}
	}

	private void OnTestManagedExceptionClicked(object? sender, EventArgs e)
	{
		StatusLabel.Text = "Throwing managed exception...";
		Console.WriteLine("[TEST] About to throw managed exception");

		// This will be caught by AppDomain.UnhandledException handler
		// Note: In debug mode, the debugger may catch this first
		MainThread.BeginInvokeOnMainThread(() =>
		{
			throw new InvalidOperationException("Test managed exception from MainPage");
		});
	}

	private void OnTestUnobservedTaskExceptionClicked(object? sender, EventArgs e)
	{
		StatusLabel.Text = "Creating unobserved task exception...";
		Console.WriteLine("[TEST] Creating unobserved task exception");

		// Create a task that throws but is never awaited
		_ = Task.Run(() =>
		{
			throw new ApplicationException("Test unobserved task exception");
		});

		// Force GC to trigger UnobservedTaskException
		Task.Delay(100).ContinueWith(_ =>
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			Console.WriteLine("[TEST] GC forced - UnobservedTaskException should fire");
		});

		StatusLabel.Text = "Unobserved exception created (check logs)";
	}

	private void OnCrashAppClicked(object? sender, EventArgs e)
	{
		StatusLabel.Text = "Crashing app...";
		Console.WriteLine("[TEST] About to crash the app");

		// Give a moment to see the status before crashing
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			await Task.Delay(500);

			// This will cause a native crash that should be captured by crash reporting
			// Using null reference to trigger crash
			object? obj = null;
			_ = obj!.ToString();
		});
	}
}
