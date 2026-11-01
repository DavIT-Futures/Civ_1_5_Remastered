using System;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Civilization1Remaster.Core;
using Civilization1Remaster.Foundation.Support;
using Civilization1Remaster.Imaging;
using Civilization1Remaster.Machine;

namespace Civilization1Remaster.Desktop;

public partial class MainWindow : Window, IGameHost
{
	private const int GameWidth = 320;
	private const int GameHeight = 200;

	private readonly int[] colorBuffer = new int[GameWidth * GameHeight];
	private readonly int[] colors = new int[256];
	private readonly WriteableBitmap? writeableBitmap;
	private readonly DispatcherTimer? renderTimer;

	private GameEngine? session;
	private Thread? sessionWorker;
	private int currentScale = 4;
	private System.Drawing.Point pointerSite = System.Drawing.Point.Empty;
	private PointerButtons pointerClicks = PointerButtons.None;
	private volatile bool shutting;
	private bool confirmedExit;

	public MainWindow()
	{
		InitializeComponent();

		var settings = ApplicationSettings.Load();
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
			Interval = TimeSpan.FromMilliseconds(16)
		};
		this.renderTimer.Tick += OnRenderTick;
		this.renderTimer.Start();

		this.Opened += OnWindowOpened;
		this.Closing += OnWindowClosing;
	}

	public System.Drawing.Point PointerSite => this.pointerSite;
	public PointerButtons PointerClicks => this.pointerClicks;

	public void OnDisplayCountChanged()
	{
	}

	public void ShowMessage(string message, string caption)
	{
		Console.WriteLine($"[{caption}] {message}");
	}

	public HostDialogResult ShowRetryCancel(string message, string caption)
	{
		Console.WriteLine($"[{caption}] {message} - Cancelling...");
		return HostDialogResult.Cancel;
	}

	private void OnWindowOpened(object? sender, EventArgs arguments)
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
		catch (Exception exception)
		{
			CrashLogger.Log(exception, "PlayThread");

			if (this.session?.Journal != null)
			{
				this.session.Journal.EmitLine(string.Empty);
				this.session.Journal.EmitLine($"Exception message: {exception.Message}");
				this.session.Journal.EmitLine($"Exception stack trace: {exception.StackTrace}");
			}
		}

		if (!this.shutting)
		{
			this.shutting = true;
			this.confirmedExit = true;
			Dispatcher.UIThread.Post(() => this.Close());
		}
	}

	private void OnRenderTick(object? sender, EventArgs arguments)
	{
		if (this.session?.VideoCard == null || this.writeableBitmap == null)
		{
			return;
		}

		bool needsRepaint = false;
		lock (this.session.VideoCard.VideoLatch)
		{
			VideoGraphicsBitmap? display = null;
			foreach (VideoGraphicsBitmap candidate in this.session.VideoCard.Displays.Values)
			{
				if (candidate.Seen)
				{
					display = candidate;
					break;
				}
			}

			if (display == null && this.session.VideoCard.Displays.TryGetValue(0, out VideoGraphicsBitmap? primary))
			{
				display = primary;
			}

			if (display != null && display.Edited)
			{
				Array.Copy(display.SwatchColor, this.colors, this.colors.Length);
				byte[] storage = display.PictureStorage;
				int step = display.Pitch;
				int width = Math.Min(GameWidth, display.Extent.Width);
				int height = Math.Min(GameHeight, display.Extent.Height);

				for (int vertical = 0; vertical < height; vertical++)
				{
					int sourceRow = vertical * step;
					int destinationRow = vertical * GameWidth;
					for (int horizontal = 0; horizontal < width; horizontal++)
					{
						int color = this.colors[storage[sourceRow + horizontal]];
						this.colorBuffer[destinationRow + horizontal] = (int)(0xff000000 | (uint)(color & 0x00ffffff));
					}
				}

				display.Edited = false;
				needsRepaint = true;
			}
		}

		if (needsRepaint)
		{
			using (var fallback = this.writeableBitmap.Lock())
			{
				unsafe
				{
					fixed (int* sourcePixels = this.colorBuffer)
					{
						Buffer.MemoryCopy(sourcePixels, (void*)fallback.Address, GameWidth * GameHeight * 4, GameWidth * GameHeight * 4);
					}
				}
			}

			this.ScreenImage.InvalidateVisual();
		}
	}

	private void OnScaleMenuItemClicked(object? sender, RoutedEventArgs arguments)
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
			this.Height = (GameHeight * scale) + 30;
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
		var settings = new ApplicationSettings
		{
			Scale = this.currentScale > 0 ? this.currentScale : 4,
			FullScreen = this.WindowState == WindowState.FullScreen,
			Maximized = this.WindowState == WindowState.Maximized
		};
		settings.Save();
	}

	private void OnWindowTextInput(object? sender, TextInputEventArgs arguments)
	{
		if (this.session?.VideoCard != null && !string.IsNullOrEmpty(arguments.Text))
		{
			lock (this.session.VideoCard.VideoLatch)
			{
				foreach (char character in arguments.Text)
				{
					if (character == '\r' || character == '\n' || character == '\t' || character == '\b' || character == 0x1b)
					{
						continue;
					}

					this.session.VideoCard.Tokens.Enqueue(character);
				}
			}
		}
	}

	private void OnWindowKeyDown(object? sender, KeyEventArgs arguments)
	{
		if (this.session?.VideoCard == null)
		{
			return;
		}

		bool alternate = (arguments.KeyModifiers & KeyModifiers.Alt) != 0;
		bool shift = (arguments.KeyModifiers & KeyModifiers.Shift) != 0;

		lock (this.session.VideoCard.VideoLatch)
		{
			if (alternate)
			{
				arguments.Handled = true;
				switch (arguments.Key)
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
				switch (arguments.Key)
				{
					case Key.Down:
					case Key.NumPad2: this.session.VideoCard.Tokens.Enqueue(0x5032); arguments.Handled = true; break;
					case Key.Left:
					case Key.NumPad4: this.session.VideoCard.Tokens.Enqueue(0x4b34); arguments.Handled = true; break;
					case Key.Right:
					case Key.NumPad6: this.session.VideoCard.Tokens.Enqueue(0x4d36); arguments.Handled = true; break;
					case Key.Up:
					case Key.NumPad8: this.session.VideoCard.Tokens.Enqueue(0x4838); arguments.Handled = true; break;
					case Key.Home:
					case Key.NumPad7: this.session.VideoCard.Tokens.Enqueue(0x4737); arguments.Handled = true; break;
					case Key.End:
					case Key.NumPad1: this.session.VideoCard.Tokens.Enqueue(0x4f31); arguments.Handled = true; break;
					case Key.PageUp:
					case Key.NumPad9: this.session.VideoCard.Tokens.Enqueue(0x4939); arguments.Handled = true; break;
					case Key.PageDown:
					case Key.NumPad3: this.session.VideoCard.Tokens.Enqueue(0x5133); arguments.Handled = true; break;
					default:
						if (TryEnqueueAlphanumeric(arguments.Key, true))
						{
							arguments.Handled = true;
						}

						break;
				}
			}
			else
			{
				switch (arguments.Key)
				{
					case Key.Enter: this.session.VideoCard.Tokens.Enqueue(0x0d); arguments.Handled = true; break;
					case Key.Escape: this.session.VideoCard.Tokens.Enqueue(0x1b); arguments.Handled = true; break;
					case Key.Space: this.session.VideoCard.Tokens.Enqueue(0x20); arguments.Handled = true; break;
					case Key.Tab: this.session.VideoCard.Tokens.Enqueue(0x09); arguments.Handled = true; break;
					case Key.Back: this.session.VideoCard.Tokens.Enqueue(0x08); arguments.Handled = true; break;
					case Key.Delete: this.session.VideoCard.Tokens.Enqueue(0x5300); arguments.Handled = true; break;
					case Key.F1: this.session.VideoCard.Tokens.Enqueue(0x3b00); arguments.Handled = true; break;
					case Key.F2: this.session.VideoCard.Tokens.Enqueue(0x3c00); arguments.Handled = true; break;
					case Key.F3: this.session.VideoCard.Tokens.Enqueue(0x3d00); arguments.Handled = true; break;
					case Key.F4: this.session.VideoCard.Tokens.Enqueue(0x3e00); arguments.Handled = true; break;
					case Key.F5: this.session.VideoCard.Tokens.Enqueue(0x3f00); arguments.Handled = true; break;
					case Key.F6: this.session.VideoCard.Tokens.Enqueue(0x4000); arguments.Handled = true; break;
					case Key.F7: this.session.VideoCard.Tokens.Enqueue(0x4100); arguments.Handled = true; break;
					case Key.F8: this.session.VideoCard.Tokens.Enqueue(0x4200); arguments.Handled = true; break;
					case Key.F9: this.session.VideoCard.Tokens.Enqueue(0x4300); arguments.Handled = true; break;
					case Key.F10: this.session.VideoCard.Tokens.Enqueue(0x4400); arguments.Handled = true; break;
					case Key.F11: ToggleFullScreen(); arguments.Handled = true; break;
					case Key.Down:
					case Key.NumPad2: this.session.VideoCard.Tokens.Enqueue(0x5000); arguments.Handled = true; break;
					case Key.Left:
					case Key.NumPad4: this.session.VideoCard.Tokens.Enqueue(0x4b00); arguments.Handled = true; break;
					case Key.Right:
					case Key.NumPad6: this.session.VideoCard.Tokens.Enqueue(0x4d00); arguments.Handled = true; break;
					case Key.Up:
					case Key.NumPad8: this.session.VideoCard.Tokens.Enqueue(0x4800); arguments.Handled = true; break;
					case Key.Home:
					case Key.NumPad7: this.session.VideoCard.Tokens.Enqueue(0x4700); arguments.Handled = true; break;
					case Key.End:
					case Key.NumPad1: this.session.VideoCard.Tokens.Enqueue(0x4f00); arguments.Handled = true; break;
					case Key.PageUp:
					case Key.NumPad9: this.session.VideoCard.Tokens.Enqueue(0x4900); arguments.Handled = true; break;
					case Key.PageDown:
					case Key.NumPad3: this.session.VideoCard.Tokens.Enqueue(0x5100); arguments.Handled = true; break;
					case Key.NumPad5: this.session.VideoCard.Tokens.Enqueue(0x4c00); arguments.Handled = true; break;
					case Key.NumPad0: this.session.VideoCard.Tokens.Enqueue(0x475c); arguments.Handled = true; break;
					default:
						if (TryEnqueueAlphanumeric(arguments.Key, false))
						{
							arguments.Handled = true;
						}

						break;
				}
			}
		}
	}

	private bool TryEnqueueAlphanumeric(Key key, bool shift)
	{
		if (this.session?.VideoCard == null)
		{
			return false;
		}

		if (key >= Key.A && key <= Key.Z)
		{
			char character = (char)((shift ? 'A' : 'a') + (key - Key.A));
			this.session.VideoCard.Tokens.Enqueue(character);
			return true;
		}

		if (key >= Key.D0 && key <= Key.D9)
		{
			string shiftSymbols = ")!@#$%^&*(";
			char character = shift ? shiftSymbols[key - Key.D0] : (char)('0' + (key - Key.D0));
			this.session.VideoCard.Tokens.Enqueue(character);
			return true;
		}

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

	private void OnScreenPointerMoved(object? sender, PointerEventArgs arguments)
	{
		var position = arguments.GetPosition(this.ScreenImage);
		int gridHorizontal = (int)Math.Clamp(position.X, 0, GameWidth - 1);
		int gridVertical = (int)Math.Clamp(position.Y, 0, GameHeight - 1);
		this.pointerSite = new System.Drawing.Point(gridHorizontal, gridVertical);
	}

	private void OnScreenPointerPressed(object? sender, PointerPressedEventArgs arguments)
	{
		var point = arguments.GetCurrentPoint(this.ScreenImage);
		if (point.Properties.IsLeftButtonPressed)
		{
			this.pointerClicks |= PointerButtons.Left;
		}

		if (point.Properties.IsRightButtonPressed)
		{
			this.pointerClicks |= PointerButtons.Right;
		}
	}

	private void OnScreenPointerReleased(object? sender, PointerReleasedEventArgs arguments)
	{
		var point = arguments.GetCurrentPoint(this.ScreenImage);
		if (!point.Properties.IsLeftButtonPressed)
		{
			this.pointerClicks &= ~PointerButtons.Left;
		}

		if (!point.Properties.IsRightButtonPressed)
		{
			this.pointerClicks &= ~PointerButtons.Right;
		}
	}

	private void OnWindowClosing(object? sender, WindowClosingEventArgs arguments)
	{
		if (this.shutting)
		{
			return;
		}

		if (!this.confirmedExit)
		{
			arguments.Cancel = true;
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

	private void OnConfirmExitClicked(object? sender, RoutedEventArgs arguments)
	{
		this.confirmedExit = true;
		this.ExitConfirmOverlay.IsVisible = false;
		this.Close();
	}

	private void OnCancelExitClicked(object? sender, RoutedEventArgs arguments)
	{
		this.ExitConfirmOverlay.IsVisible = false;
	}
}
