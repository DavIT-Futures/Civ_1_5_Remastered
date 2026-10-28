using System;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Civilization1Remaster.Containers;
using Civilization1Remaster.Video;

namespace Civilization1Remaster.Desktop;

public partial class MainWindow : Window, IGameHost
{
	private const int GameWidth = 320;
	private const int GameHeight = 200;

	private GameEngine? session;
	private Thread? sessionWorker;
	private WriteableBitmap? writeableBitmap;
	private readonly int[] rgbBuffer = new int[GameWidth * GameHeight];
	private readonly int[] colors = new int[256];
	private DispatcherTimer? renderTimer;
	private int currentScale = 4;

	private System.Drawing.Point pointerSite = System.Drawing.Point.Empty;
	private PointerButtons pointerClicks = PointerButtons.None;
	private volatile bool shutting;

	public System.Drawing.Point PointerSite => this.pointerSite;
	public PointerButtons PointerClicks => this.pointerClicks;

	public MainWindow()
	{
		InitializeComponent();

		var settings = AppSettings.Load();
		this.currentScale = settings.Scale > 0 ? settings.Scale : 4;
		if (settings.FullScreen)
		{
			this.WindowState = WindowState.FullScreen;
		}
		else if (settings.Maximized)
		{
			this.WindowState = WindowState.Maximized;
		}
		else
		{
			this.WindowState = WindowState.Normal;
			this.Width = GameWidth * this.currentScale;
			this.Height = (GameHeight * this.currentScale) + 30;
		}

		this.writeableBitmap = new WriteableBitmap(
			new PixelSize(GameWidth, GameHeight),
			new Vector(96, 96),
			PixelFormat.Bgra8888,
			AlphaFormat.Opaque);

		this.ScreenImage.Source = this.writeableBitmap;

		this.AddHandler(InputElement.KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
		this.AddHandler(InputElement.TextInputEvent, OnWindowTextInput, RoutingStrategies.Tunnel);

		this.renderTimer = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
		};
		this.renderTimer.Tick += OnRenderTick;
		this.renderTimer.Start();

		this.Opened += OnWindowOpened;
		this.Closing += OnWindowClosing;
	}

	private void OnWindowOpened(object? sender, EventArgs e)
	{
		if (this.sessionWorker == null)
		{
			this.sessionWorker = new Thread(PlayThread)
			{
				IsBackground = true,
				Name = "GameSession"
			};
			this.sessionWorker.Start();
		}
	}

	private void PlayThread()
	{
		try
		{
			this.session = new GameEngine(this);
			this.session.Launch();
		}
		catch (ApplicationExitException)
		{
		}
		catch (Exception ex)
		{
			CrashLogger.Log(ex, "PlayThread");

			if (this.session?.Journal != null)
			{
				this.session.Journal.EmitLine("");
				this.session.Journal.EmitLine($"Exception message: {ex.Message}");
				this.session.Journal.EmitLine($"Exception stack trace: {ex.StackTrace}");
			}
		}

		if (!this.shutting)
		{
			this.shutting = true;
			this.confirmedExit = true;
			Dispatcher.UIThread.Post(() => this.Close());
		}
	}

	private void OnRenderTick(object? sender, EventArgs e)
	{
		if (this.session?.VideoCard == null || this.writeableBitmap == null)
			return;

		bool needsRepaint = false;
		lock (this.session.VideoCard.VideoLatch)
		{
			// Check screen 0 or active seen screens
			VGABitmap? display = null;
			for (int idx = 0; idx < this.session.VideoCard.Displays.Count; idx++)
			{
				var entry = this.session.VideoCard.Displays[idx];
				if (entry.Datum.Seen)
				{
					display = entry.Datum;
					break;
				}
			}

			if (display == null && this.session.VideoCard.Displays.IncludesKey(0))
			{
				display = this.session.VideoCard.Displays.ObtainValueByKey(0);
			}

			if (display != null && display.Edited)
			{
				Array.Copy(display.SwatchRgba, this.colors, this.colors.Length);
				byte[] storage = display.PictureStorage;
				int step = display.Pitch;
				int width = Math.Min(GameWidth, display.Extent.Width);
				int height = Math.Min(GameHeight, display.Extent.Height);

				for (int y = 0; y < height; y++)
				{
					int srcRow = y * step;
					int dstRow = y * GameWidth;
					for (int x = 0; x < width; x++)
					{
						int color = this.colors[storage[srcRow + x]];
						// Ensure full alpha (0xFF000000) for Bgra8888 display
						this.rgbBuffer[dstRow + x] = (int)(0xff000000 | (uint)(color & 0x00ffffff));
					}
				}

				display.Edited = false;
				needsRepaint = true;
			}
		}

		if (needsRepaint)
		{
			using (var fb = this.writeableBitmap.Lock())
			{
				unsafe
				{
					fixed (int* pSrc = this.rgbBuffer)
					{
						Buffer.MemoryCopy(pSrc, (void*)fb.Address, GameWidth * GameHeight * 4, GameWidth * GameHeight * 4);
					}
				}
			}
			this.ScreenImage.InvalidateVisual();
		}
	}

	private void OnScaleMenuItemClicked(object? sender, RoutedEventArgs e)
	{
		if (sender is MenuItem item && item.Tag != null && int.TryParse(item.Tag.ToString(), out int scale))
		{
			ApplyScale(scale, save: true);
		}
	}

	private void ApplyScale(int scale, bool save = true)
	{
		if (scale == 0)
		{
			this.WindowState = WindowState.FullScreen;
		}
		else if (scale == -1)
		{
			if (this.WindowState == WindowState.FullScreen)
			{
				this.WindowState = WindowState.Normal;
			}
			this.WindowState = WindowState.Maximized;
		}
		else
		{
			if (this.WindowState == WindowState.FullScreen || this.WindowState == WindowState.Maximized)
			{
				this.WindowState = WindowState.Normal;
			}

			this.currentScale = scale;
			this.Width = GameWidth * scale;
			this.Height = (GameHeight * scale) + 30; // + menu height
		}

		if (save)
		{
			SaveCurrentSettings();
		}
	}

	private void ToggleFullScreen()
	{
		if (this.WindowState == WindowState.FullScreen)
		{
			ApplyScale(this.currentScale > 0 ? this.currentScale : 4, save: true);
		}
		else
		{
			ApplyScale(0, save: true);
		}
	}

	private void SaveCurrentSettings()
	{
		var settings = new AppSettings
		{
			Scale = this.currentScale > 0 ? this.currentScale : 4,
			FullScreen = this.WindowState == WindowState.FullScreen,
			Maximized = this.WindowState == WindowState.Maximized
		};
		settings.Save();
	}

	private void OnWindowTextInput(object? sender, TextInputEventArgs e)
	{
		if (this.session?.VideoCard != null && !string.IsNullOrEmpty(e.Text))
		{
			lock (this.session.VideoCard.VideoLatch)
			{
				foreach (char c in e.Text)
				{
					// Skip control characters that are already enqueued in KeyDown (Enter, Esc, Tab, Backspace)
					if (c == '\r' || c == '\n' || c == '\t' || c == '\b' || c == 0x1b)
						continue;

					this.session.VideoCard.Tokens.Enqueue(c);
				}
			}
		}
	}

	private void OnWindowKeyDown(object? sender, KeyEventArgs e)
	{
		if (this.session?.VideoCard == null)
			return;

		bool alt = (e.KeyModifiers & KeyModifiers.Alt) != 0;
		bool shift = (e.KeyModifiers & KeyModifiers.Shift) != 0;

		lock (this.session.VideoCard.VideoLatch)
		{
			if (alt)
			{
				e.Handled = true;
				switch (e.Key)
				{
					case Key.Enter:
						ToggleFullScreen();
						break;
					case Key.A: this.session.VideoCard.Tokens.Enqueue(0x1e00); break;
					case Key.C: this.session.VideoCard.Tokens.Enqueue(0x2e00); break;
					case Key.D: this.session.VideoCard.Tokens.Enqueue(0x2000); break;
					case Key.G: this.session.VideoCard.Tokens.Enqueue(0x2200); break;
					case Key.H: this.session.VideoCard.Tokens.Enqueue(0x2300); break;
					case Key.M: this.session.VideoCard.Tokens.Enqueue(0x3200); break;
					case Key.O: this.session.VideoCard.Tokens.Enqueue(0x1800); break;
					case Key.Q: this.session.VideoCard.Tokens.Enqueue(0x1000); break;
					case Key.R: this.session.VideoCard.Tokens.Enqueue(0x1300); break;
					case Key.V: this.session.VideoCard.Tokens.Enqueue(0x2f00); break;
					case Key.W: this.session.VideoCard.Tokens.Enqueue(0x1100); break;
				}
			}
			else if (shift)
			{
				switch (e.Key)
				{
					case Key.Down:
					case Key.NumPad2: this.session.VideoCard.Tokens.Enqueue(0x5032); e.Handled = true; break;
					case Key.Left:
					case Key.NumPad4: this.session.VideoCard.Tokens.Enqueue(0x4b34); e.Handled = true; break;
					case Key.Right:
					case Key.NumPad6: this.session.VideoCard.Tokens.Enqueue(0x4d36); e.Handled = true; break;
					case Key.Up:
					case Key.NumPad8: this.session.VideoCard.Tokens.Enqueue(0x4838); e.Handled = true; break;
					case Key.Home:
					case Key.NumPad7: this.session.VideoCard.Tokens.Enqueue(0x4737); e.Handled = true; break;
					case Key.End:
					case Key.NumPad1: this.session.VideoCard.Tokens.Enqueue(0x4f31); e.Handled = true; break;
					case Key.PageUp:
					case Key.NumPad9: this.session.VideoCard.Tokens.Enqueue(0x4939); e.Handled = true; break;
					case Key.PageDown:
					case Key.NumPad3: this.session.VideoCard.Tokens.Enqueue(0x5133); e.Handled = true; break;
					default:
						if (TryEnqueueAlphanumeric(e.Key, true))
						{
							e.Handled = true;
						}
						break;
				}
			}
			else
			{
				switch (e.Key)
				{
					case Key.Enter: this.session.VideoCard.Tokens.Enqueue(0x0d); e.Handled = true; break;
					case Key.Escape: this.session.VideoCard.Tokens.Enqueue(0x1b); e.Handled = true; break;
					case Key.Space: this.session.VideoCard.Tokens.Enqueue(0x20); e.Handled = true; break;
					case Key.Tab: this.session.VideoCard.Tokens.Enqueue(0x09); e.Handled = true; break;
					case Key.Back: this.session.VideoCard.Tokens.Enqueue(0x08); e.Handled = true; break;
					case Key.Delete: this.session.VideoCard.Tokens.Enqueue(0x5300); e.Handled = true; break;
					case Key.F1: this.session.VideoCard.Tokens.Enqueue(0x3b00); e.Handled = true; break;
					case Key.F2: this.session.VideoCard.Tokens.Enqueue(0x3c00); e.Handled = true; break;
					case Key.F3: this.session.VideoCard.Tokens.Enqueue(0x3d00); e.Handled = true; break;
					case Key.F4: this.session.VideoCard.Tokens.Enqueue(0x3e00); e.Handled = true; break;
					case Key.F5: this.session.VideoCard.Tokens.Enqueue(0x3f00); e.Handled = true; break;
					case Key.F6: this.session.VideoCard.Tokens.Enqueue(0x4000); e.Handled = true; break;
					case Key.F7: this.session.VideoCard.Tokens.Enqueue(0x4100); e.Handled = true; break;
					case Key.F8: this.session.VideoCard.Tokens.Enqueue(0x4200); e.Handled = true; break;
					case Key.F9: this.session.VideoCard.Tokens.Enqueue(0x4300); e.Handled = true; break;
					case Key.F10: this.session.VideoCard.Tokens.Enqueue(0x4400); e.Handled = true; break;
					case Key.F11: ToggleFullScreen(); e.Handled = true; break;
					case Key.Down:
					case Key.NumPad2: this.session.VideoCard.Tokens.Enqueue(0x5000); e.Handled = true; break;
					case Key.Left:
					case Key.NumPad4: this.session.VideoCard.Tokens.Enqueue(0x4b00); e.Handled = true; break;
					case Key.Right:
					case Key.NumPad6: this.session.VideoCard.Tokens.Enqueue(0x4d00); e.Handled = true; break;
					case Key.Up:
					case Key.NumPad8: this.session.VideoCard.Tokens.Enqueue(0x4800); e.Handled = true; break;
					case Key.Home:
					case Key.NumPad7: this.session.VideoCard.Tokens.Enqueue(0x4700); e.Handled = true; break;
					case Key.End:
					case Key.NumPad1: this.session.VideoCard.Tokens.Enqueue(0x4f00); e.Handled = true; break;
					case Key.PageUp:
					case Key.NumPad9: this.session.VideoCard.Tokens.Enqueue(0x4900); e.Handled = true; break;
					case Key.PageDown:
					case Key.NumPad3: this.session.VideoCard.Tokens.Enqueue(0x5100); e.Handled = true; break;
					case Key.NumPad5: this.session.VideoCard.Tokens.Enqueue(0x4c00); e.Handled = true; break;
					case Key.NumPad0: this.session.VideoCard.Tokens.Enqueue(0x475c); e.Handled = true; break;
					default:
						if (TryEnqueueAlphanumeric(e.Key, false))
						{
							e.Handled = true;
						}
						break;
				}
			}
		}
	}

	private bool TryEnqueueAlphanumeric(Key key, bool shift)
	{
		if (this.session?.VideoCard == null)
			return false;

		// Letters A-Z
		if (key >= Key.A && key <= Key.Z)
		{
			char c = (char)((shift ? 'A' : 'a') + (key - Key.A));
			this.session.VideoCard.Tokens.Enqueue(c);
			return true;
		}

		// Digits 0-9
		if (key >= Key.D0 && key <= Key.D9)
		{
			string shiftSymbols = ")!@#$%^&*(";
			char c = shift ? shiftSymbols[key - Key.D0] : (char)('0' + (key - Key.D0));
			this.session.VideoCard.Tokens.Enqueue(c);
			return true;
		}

		// Punctuation and symbols
		switch (key)
		{
			case Key.OemMinus: this.session.VideoCard.Tokens.Enqueue(shift ? '_' : '-'); return true;
			case Key.OemPlus: this.session.VideoCard.Tokens.Enqueue(shift ? '+' : '='); return true;
			case Key.OemComma: this.session.VideoCard.Tokens.Enqueue(shift ? '<' : ','); return true;
			case Key.OemPeriod: this.session.VideoCard.Tokens.Enqueue(shift ? '>' : '.'); return true;
			case Key.OemQuestion: this.session.VideoCard.Tokens.Enqueue(shift ? '?' : '/'); return true;
			case Key.OemSemicolon: this.session.VideoCard.Tokens.Enqueue(shift ? ':' : ';'); return true;
			case Key.OemQuotes: this.session.VideoCard.Tokens.Enqueue(shift ? '"' : '\''); return true;
			case Key.OemOpenBrackets: this.session.VideoCard.Tokens.Enqueue(shift ? '{' : '['); return true;
			case Key.OemCloseBrackets: this.session.VideoCard.Tokens.Enqueue(shift ? '}' : ']'); return true;
			case Key.OemPipe: this.session.VideoCard.Tokens.Enqueue(shift ? '|' : '\\'); return true;
			case Key.OemTilde: this.session.VideoCard.Tokens.Enqueue(shift ? '~' : '`'); return true;
		}

		return false;
	}

	private void OnScreenPointerMoved(object? sender, PointerEventArgs e)
	{
		var pos = e.GetPosition(this.ScreenImage);
		int gx = (int)Math.Clamp(pos.X, 0, GameWidth - 1);
		int gy = (int)Math.Clamp(pos.Y, 0, GameHeight - 1);
		this.pointerSite = new System.Drawing.Point(gx, gy);
	}

	private void OnScreenPointerPressed(object? sender, PointerPressedEventArgs e)
	{
		var point = e.GetCurrentPoint(this.ScreenImage);
		if (point.Properties.IsLeftButtonPressed)
			this.pointerClicks |= PointerButtons.Left;
		if (point.Properties.IsRightButtonPressed)
			this.pointerClicks |= PointerButtons.Right;
	}

	private void OnScreenPointerReleased(object? sender, PointerReleasedEventArgs e)
	{
		var point = e.GetCurrentPoint(this.ScreenImage);
		if (!point.Properties.IsLeftButtonPressed)
			this.pointerClicks &= ~PointerButtons.Left;
		if (!point.Properties.IsRightButtonPressed)
			this.pointerClicks &= ~PointerButtons.Right;
	}

	private bool confirmedExit;

	private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
	{
		if (this.shutting)
			return;

		if (!this.confirmedExit)
		{
			e.Cancel = true;
			this.ExitConfirmOverlay.IsVisible = true;
			return;
		}

		this.shutting = true;
		SaveCurrentSettings();
		this.renderTimer?.Stop();
		if (this.session?.Processor != null)
		{
			this.session.Processor.WhenApplicationQuits();
		}
	}

	private void OnConfirmExitClicked(object? sender, RoutedEventArgs e)
	{
		this.confirmedExit = true;
		this.ExitConfirmOverlay.IsVisible = false;
		this.Close();
	}

	private void OnCancelExitClicked(object? sender, RoutedEventArgs e)
	{
		this.ExitConfirmOverlay.IsVisible = false;
	}

	#region IGameHost implementation
	public void OnDisplayCountChanged()
	{
	}

	public void ShowMessage(string message, string caption)
	{
		// Fallback log or simple console output for non-blocking notifications
		Console.WriteLine($"[{caption}] {message}");
	}

	public HostDialogResult ShowRetryCancel(string message, string caption)
	{
		Console.WriteLine($"[{caption}] {message} - Cancelling...");
		return HostDialogResult.Cancel;
	}
	#endregion
}