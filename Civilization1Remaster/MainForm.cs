using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;
using Civilization1Remaster.Containers;
using Civilization1Remaster.Video;

namespace Civilization1Remaster
{
	public partial class MainForm : Form, IGameHost
	{
		private delegate void InitializeControlsDelegate();
		private delegate void ScreenCountChangeDelegate();
		private delegate void CloseFormDelegate();
		private readonly InitializeControlsDelegate setupWidgets;
		private readonly ScreenCountChangeDelegate displayTallyAlter;
		private readonly CloseFormDelegate shutWindow;

		private bool shutting = false;
		private bool beginCompleteDisplay = false;
		private Thread? sessionWorker = null;
		private GameEngine? session = null;
		private Size displayExtent;
		private Rectangle pointerBox = Rectangle.Empty;
		private Point pointerSite = Point.Empty;
		private MouseButtons pointerClicks = MouseButtons.None;
		private int displayCols = 1;
		private int displayLines = 1;
		private readonly Font typeface;
		private readonly Font bigTypeface;

		// Window size: the game screen (320 x 200) is enlarged iWindowScale times, or fills the whole monitor in full screen mode
		private const int PeakPaneZoom = 8;
		private int paneZoom = 2;
		private bool completeDisplay = false;
		private Rectangle framedLimits = Rectangle.Empty;
		// Where the first screen is drawn (top left corner of its frame), in client coordinates
		private Point displayAnchor = Point.Empty;
		private readonly ToolStripDropDownButton toolbarExtent;
		private static readonly string OptionsDocument = Path.Combine(AppContext.BaseDirectory, "Civilization1Remaster.settings.txt");

		public MainForm()
		{
			InitializeComponent();

			this.SetStyle(ControlStyles.Opaque, true);
			this.SetStyle(ControlStyles.UserPaint, true);
			this.setupWidgets = new InitializeControlsDelegate(PrepareControlsMethod);
			this.displayTallyAlter = new ScreenCountChangeDelegate(DisplayCountChangedHandler);
			this.shutWindow = new CloseFormDelegate(ShutFormMethod);

			this.typeface = new Font("Verdana", 10.0f, FontStyle.Regular, GraphicsUnit.Pixel);
			this.bigTypeface = new Font("Verdana", 40.0f, FontStyle.Bold, GraphicsUnit.Pixel);

			this.toolbarExtent = new ToolStripDropDownButton("Window size");
			this.toolbarExtent.DisplayStyle = ToolStripItemDisplayStyle.Text;
			this.toolbarExtent.DropDownOpening += OnSizeMenuAboutToOpen;
			this.toolbarPrimary.Items.Add(new ToolStripSeparator());
			this.toolbarPrimary.Items.Add(this.toolbarExtent);
			AssembleSizeMenu();

			this.Resize += OnWindowResized;

			ImportOptions();
			RefreshScreenLayout();
		}

		private void AssembleSizeMenu()
		{
			this.toolbarExtent.DropDownItems.Clear();

			for (int zoom = 1; zoom <= PeakPaneZoom; zoom++)
			{
				ToolStripMenuItem element = new ToolStripMenuItem($"{zoom}x  ({320 * zoom} x {200 * zoom})");
				element.Tag = zoom;
				element.Click += OnSizeEntryChosen;
				this.toolbarExtent.DropDownItems.Add(element);
			}

			this.toolbarExtent.DropDownItems.Add(new ToolStripSeparator());

			ToolStripMenuItem completeDisplay = new ToolStripMenuItem("Full screen  (F11 or Alt+Enter)");
			completeDisplay.Tag = 0;
			completeDisplay.Click += OnSizeEntryChosen;
			this.toolbarExtent.DropDownItems.Add(completeDisplay);
		}

		private void OnSizeMenuAboutToOpen(object? caller, EventArgs ev)
		{
			foreach (ToolStripItem element in this.toolbarExtent.DropDownItems)
			{
				if (element is ToolStripMenuItem choicesElement && choicesElement.Tag is int zoom)
				{
					if (zoom == 0)
					{
						choicesElement.Checked = this.completeDisplay;
					}
					else
					{
						choicesElement.Checked = !this.completeDisplay && zoom == this.paneZoom;
						// Sizes larger than the monitor are not offered
						choicesElement.Enabled = ScaleFitsWindow(zoom);
					}
				}
			}
		}

		private void OnSizeEntryChosen(object? caller, EventArgs ev)
		{
			if (caller is ToolStripMenuItem choicesElement && choicesElement.Tag is int zoom)
			{
				if (zoom == 0)
				{
					AssignFullScreen(!this.completeDisplay);
				}
				else
				{
					this.paneZoom = zoom;
					AssignFullScreen(false);
					RefreshScreenLayout();
				}

				StoreOptions();
			}
		}

		/// <summary>
		/// The window with the screen enlarged this many times fits on the monitor
		/// </summary>
		private bool ScaleFitsWindow(int zoom)
		{
			if (zoom <= 1)
				return true;

			Rectangle busyZone = Screen.FromControl(this).WorkingArea;
			Size edge = this.completeDisplay ? new Size(8, 32) : this.Size - this.ClientSize;

			return 320 * zoom + 2 + edge.Width <= busyZone.Width &&
				200 * zoom + 2 + this.toolbarPrimary.Height + edge.Height <= busyZone.Height;
		}

		/// <summary>
		/// Switches between the window and the full screen mode, the full screen hides the tool bar and fills the monitor
		/// </summary>
		private void AssignFullScreen(bool completeDisplay)
		{
			if (completeDisplay == this.completeDisplay)
				return;

			this.completeDisplay = completeDisplay;

			if (completeDisplay)
			{
				this.framedLimits = this.Bounds;
				this.toolbarPrimary.Visible = false;
				this.FormBorderStyle = FormBorderStyle.None;
				this.Bounds = Screen.FromControl(this).Bounds;
			}
			else
			{
				this.FormBorderStyle = FormBorderStyle.FixedSingle;
				this.toolbarPrimary.Visible = true;

				if (!this.framedLimits.IsEmpty)
				{
					this.Location = this.framedLimits.Location;
				}
			}

			RefreshScreenLayout();
			StoreOptions();
		}

		private void OnWindowResized(object? caller, EventArgs ev)
		{
			if (this.completeDisplay)
			{
				RefreshScreenLayout();
			}
		}

		/// <summary>
		/// Sets the size of the screens and of the window (or the position of the screen in full screen mode)
		/// </summary>
		private void RefreshScreenLayout()
		{
			int head = this.toolbarPrimary.Visible ? this.toolbarPrimary.Height : 0;
			bool loneDisplay = this.displayCols == 1 && this.displayLines == 1;

			if (this.completeDisplay)
			{
				Size zone = new Size(this.ClientSize.Width, this.ClientSize.Height - head);

				if (loneDisplay)
				{
					// As large as possible, keeping the shape of the original screen, centered with black borders
					int breadth = Math.Min(zone.Width, zone.Height * 320 / 200);
					this.displayExtent = new Size(breadth, breadth * 200 / 320);
				}
				else
				{
					this.displayExtent = new Size(320, 200);
				}

				this.displayAnchor = new Point(
					Math.Max(0, (zone.Width - (this.displayExtent.Width * this.displayCols + this.displayCols + 1)) / 2),
					head + Math.Max(0, (zone.Height - (this.displayExtent.Height * this.displayLines + this.displayLines + 1)) / 2));
			}
			else
			{
				// A remembered size can be too large for this monitor
				while (this.paneZoom > 1 && !ScaleFitsWindow(this.paneZoom))
					this.paneZoom--;

				this.displayExtent = loneDisplay ? new Size(320 * this.paneZoom, 200 * this.paneZoom) : new Size(320, 200);
				this.displayAnchor = new Point(0, head);

				this.ClientSize = new Size(this.displayExtent.Width * this.displayCols + 1 + this.displayCols,
					head + this.displayExtent.Height * this.displayLines + 1 + this.displayLines);
			}

			this.pointerBox = new Rectangle(this.displayAnchor.X + 1, this.displayAnchor.Y + 1, this.displayExtent.Width, this.displayExtent.Height);
			this.Invalidate();
		}

		private void ImportOptions()
		{
			try
			{
				if (!File.Exists(OptionsDocument))
					return;

				foreach (string stroke in File.ReadAllLines(OptionsDocument))
				{
					string[] pieces = stroke.Split('=', 2);

					if (pieces.Length != 2)
						continue;

					switch (pieces[0].Trim())
					{
						case "WindowScale":
							if (int.TryParse(pieces[1].Trim(), out int zoom))
								this.paneZoom = Math.Clamp(zoom, 1, PeakPaneZoom);
							break;

						case "FullScreen":
							if (pieces[1].Trim() == "1")
								this.beginCompleteDisplay = true;
							break;
					}
				}
			}
			catch
			{
				// Settings are not essential, the default size is used
			}
		}

		private void StoreOptions()
		{
			try
			{
				File.WriteAllLines(OptionsDocument, new[] { $"WindowScale={this.paneZoom}", $"FullScreen={(this.completeDisplay ? 1 : 0)}" });
			}
			catch
			{
				// The folder may be read only, the size is then not remembered
			}
		}

		public Point DisplayPointerSite
		{
			get { return this.pointerSite; }
		}

		public MouseButtons DisplayPointerClicks
		{
			get { return this.pointerClicks; }
		}

		Point IGameHost.PointerSite => this.pointerSite;

		PointerButtons IGameHost.PointerClicks
		{
			get
			{
				PointerButtons buttons = PointerButtons.None;
				if ((this.pointerClicks & MouseButtons.Left) == MouseButtons.Left)
					buttons |= PointerButtons.Left;
				if ((this.pointerClicks & MouseButtons.Right) == MouseButtons.Right)
					buttons |= PointerButtons.Right;
				if ((this.pointerClicks & MouseButtons.Middle) == MouseButtons.Middle)
					buttons |= PointerButtons.Middle;
				return buttons;
			}
		}

		public void ShowMessage(string message, string caption)
		{
			if (this.InvokeRequired)
			{
				this.Invoke(new Action(() => MessageBox.Show(this, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information)));
			}
			else
			{
				MessageBox.Show(this, message, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		public HostDialogResult ShowRetryCancel(string message, string caption)
		{
			if (this.InvokeRequired)
			{
				return (HostDialogResult)this.Invoke(new Func<HostDialogResult>(() =>
				{
					DialogResult res = MessageBox.Show(this, message, caption, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
					return res == DialogResult.Retry ? HostDialogResult.Retry : HostDialogResult.Cancel;
				}));
			}

			DialogResult result = MessageBox.Show(this, message, caption, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
			return result == DialogResult.Retry ? HostDialogResult.Retry : HostDialogResult.Cancel;
		}

		private void PrepareControlsMethod()
		{
			DisplayCountChangedHandler();

			this.btnHold.Enabled = true;
			this.btnOperate.Enabled = true;
			this.toolbarDisplays.Enabled = true;

			this.btnHold.Visible = true;
			this.btnOperate.Visible = false;
		}

		public void OnDisplayCountChanged()
		{
			if (this.InvokeRequired)
			{
				this.Invoke(displayTallyAlter);
			}
			else
			{
				DisplayCountChangedHandler();
			}
		}

		private void DisplayCountChangedHandler()
		{
			if (this.session != null && this.session.VideoCard != null)
			{
				lock (this.session.VideoCard.VideoLatch)
				{
					int displayTally = 0;
					// update screen list
					this.captionDisplays.Text = $"Screens ({this.session.VideoCard.Displays.Count})";

					this.toolbarDisplays.DropDownItems.Clear();
					for (int idx = 0; idx < this.session.VideoCard.Displays.Count; idx++)
					{
						BKeyValuePair<int, VGABitmap> element = this.session.VideoCard.Displays[idx];

						ToolStripMenuItem choicesElement = new ToolStripMenuItem($"Screen ({element.Token})");
						choicesElement.CheckState = element.Datum.Seen ? CheckState.Checked : CheckState.Unchecked;
						choicesElement.Tag = element.Token;
						choicesElement.Click += OnMenuEntryChosen;

						this.toolbarDisplays.DropDownItems.Add(choicesElement);

						if (element.Datum.Seen)
						{
							displayTally++;
						}
					}

					switch (displayTally)
					{
						case 1:
							this.displayCols = 1;
							this.displayLines = 1;
							break;

						case 2:
							this.displayCols = 2;
							this.displayLines = 1;
							break;

						case 3:
							this.displayCols = 2;
							this.displayLines = 2;
							break;

						case 4:
							this.displayCols = 2;
							this.displayLines = 2;
							break;

						case 5:
							this.displayCols = 3;
							this.displayLines = 2;
							break;

						case 6:
							this.displayCols = 3;
							this.displayLines = 2;
							break;

						case 7:
							this.displayCols = 3;
							this.displayLines = 3;
							break;

						case 8:
							this.displayCols = 3;
							this.displayLines = 3;
							break;
					}
					RefreshScreenLayout();
				}
			}
			else
			{
				this.displayCols = 1;
				this.displayLines = 1;

				RefreshScreenLayout();
			}
		}

		private void ShutFormMethod()
		{
			this.Close();
		}

		private void OnMenuEntryChosen(object? caller, EventArgs ev)
		{
			if (caller!=null && caller is ToolStripMenuItem)
			{
				ToolStripMenuItem choicesElement = (ToolStripMenuItem)caller;
				int view = Convert.ToInt32(choicesElement.Tag);

				if (this.session != null && this.session.VideoCard != null)
				{
					if (this.session.VideoCard.Displays.IncludesKey(view))
					{
						VGABitmap display = this.session.VideoCard.Displays.ObtainValueByKey(view);

						display.Seen = !display.Seen;

						OnDisplayCountChanged();
					}
				}
			}
		}

		private void OnRefreshTick(object caller, EventArgs ev)
		{
			if (this.session != null && this.session.VideoCard != null)
			{
				using (Graphics gfx = Graphics.FromHwnd(this.Handle))
				{
					RepaintScreens(gfx, false);
				}
			}
		}

		private void OnFormRepaint(object caller, PaintEventArgs ev)
		{
			Graphics gfx = ev.Graphics;
			gfx.FillRectangle(Brushes.Black, this.ClientRectangle);

			if (this.session != null && this.session.VideoCard != null)
			{
				RepaintScreens(gfx, true);
			}
		}

		private readonly List<ScreenFrame> images = new List<ScreenFrame>();
		private readonly int[] colors = new int[256];

		/// <summary>
		/// Copies the changed game screens. The game thread waits while the lock is held,
		/// so only the copy is done here and the slow drawing (enlarging) is done after the lock is released.
		/// </summary>
		private void DuplicateScreens(bool compelRepaint)
		{
			if (this.session == null)
				return;

			lock (this.session.VideoCard.VideoLatch)
			{
				int col = 0;
				int strip = 0;
				int borderInt = 0;

				for (int idx = 0; idx < this.session.VideoCard.Displays.Count; idx++)
				{
					BKeyValuePair<int, VGABitmap> element = this.session.VideoCard.Displays[idx];

					if (!element.Datum.Seen)
						continue;

					if (borderInt >= this.images.Count)
						this.images.Add(new ScreenFrame());

					ScreenFrame border = this.images[borderInt++];
					VGABitmap display = element.Datum;
					bool arrangementAltered = border.Token != element.Token || border.Col != col || border.Line != strip;

					border.Token = element.Token;
					border.Col = col;
					border.Line = strip;
					border.Altered = display.Edited || compelRepaint || arrangementAltered;

					if (display.Edited || arrangementAltered || border.Dots.Length != display.Extent.Width * display.Extent.Height)
					{
						Array.Copy(display.SwatchRgba, this.colors, this.colors.Length);

						border.Breadth = display.Extent.Width;
						border.Altitude = display.Extent.Height;

						if (border.Dots.Length != border.Breadth * border.Altitude)
							border.Dots = new int[border.Breadth * border.Altitude];

						byte[] storage = display.PictureStorage;
						int step = display.Pitch;
						int[] dots = border.Dots;

						for (int py = 0; py < border.Altitude; py++)
						{
							int originAlt = py * step;
							int endpoint = py * border.Breadth;

							for (int px = 0; px < border.Breadth; px++)
							{
								dots[endpoint + px] = this.colors[storage[originAlt + px]];
							}
						}

						display.Edited = false;
					}

					col++;
					if (col >= this.displayCols)
					{
						col = 0;
						strip++;
					}
				}

				if (borderInt < this.images.Count)
					this.images.RemoveRange(borderInt, this.images.Count - borderInt);
			}
		}

		private void RepaintScreens(Graphics gfx, bool compelRepaint)
		{
			if (this.session == null)
				return;

			DuplicateScreens(compelRepaint);

			bool painted = false;

			foreach (ScreenFrame border in this.images)
			{
				if (!border.Altered)
					continue;

				border.Altered = false;
				painted = true;

				Rectangle box = new Rectangle(this.displayAnchor.X + (border.Col * this.displayExtent.Width) + (border.Col + 1),
					this.displayAnchor.Y + (border.Line * this.displayExtent.Height) + (border.Line + 1),
					this.displayExtent.Width, this.displayExtent.Height);

				ScaleFrame(gfx, border, box);

				// The screen number and the frame are only for the debug view with more screens
				if (!this.completeDisplay || this.displayCols > 1 || this.displayLines > 1)
				{
					gfx.DrawString($"{border.Token}", this.typeface, Brushes.White, box.Left + 5.0f, box.Top + 5.0f);
					gfx.DrawRectangle(Pens.Purple, this.displayAnchor.X + (border.Col * this.displayExtent.Width) + border.Col,
						this.displayAnchor.Y + (border.Line * this.displayExtent.Height) + border.Line,
						this.displayExtent.Width + 1, this.displayExtent.Height + 1);
				}
			}

			if (painted && this.session.VideoCard.Processor.Hold)
			{
				string scratch = "Game paused";
				SizeF extent = gfx.MeasureString(scratch, this.bigTypeface);
				gfx.DrawString(scratch, this.bigTypeface, Brushes.White,
					this.displayAnchor.X + (float)((this.displayExtent.Width * this.displayCols) - extent.Width) / 2.0f,
					this.displayAnchor.Y + (float)((this.displayExtent.Height * this.displayLines) - extent.Height) / 2.0f);
			}
		}

		[DllImport("gdi32.dll", EntryPoint = "StretchDIBits")]
		private static extern int BlitStretched(IntPtr devctx, int horizTarget, int vertTarget, int targetBreadth, int targetAltitude,
			int horizOrigin, int vertOrigin, int originBreadth, int originAltitude, int[] digits, ref BitmapInfoHeader pictureDetails, uint use, uint rasterop);

		[DllImport("gdi32.dll", EntryPoint = "SetStretchBltMode")]
		private static extern int ChooseStretchMode(IntPtr devctx, int style);

		private const int HueOverHue = 3;
		private const uint OriginClone = 0x00cc0020;

		/// <summary>
		/// Enlarges the screen with the Windows GDI, which is many times faster than Graphics.DrawImage for large windows
		/// </summary>
		private static void ScaleFrame(Graphics gfx, ScreenFrame border, Rectangle box)
		{
			BitmapInfoHeader preamble = new BitmapInfoHeader();
			preamble.Extent = Marshal.SizeOf<BitmapInfoHeader>();
			preamble.Breadth = border.Breadth;
			// Negative height: the rows go from the top down
			preamble.Altitude = -border.Altitude;
			preamble.Aircraft = 1;
			preamble.BinaryTally = 32;

			IntPtr devctx = gfx.GetHdc();

			try
			{
				ChooseStretchMode(devctx, HueOverHue);
				BlitStretched(devctx, box.X, box.Y, box.Width, box.Height, 0, 0, border.Breadth, border.Altitude,
					border.Dots, ref preamble, 0, OriginClone);
			}
			finally
			{
				gfx.ReleaseHdc(devctx);
			}
		}

		private void OnCharTyped(object caller, KeyPressEventArgs ev)
		{
			if (this.session != null)
			{
				lock (this.session.VideoCard.VideoLatch)
				{
					this.session.VideoCard.Tokens.Enqueue(ev.KeyChar);
				}
			}
		}

		private void OnKeyPressedDown(object caller, KeyEventArgs ev)
		{
			if (this.session != null)
			{
				lock (this.session.VideoCard.VideoLatch)
				{
					if (ev.Modifiers == Keys.None)
					{
						switch (ev.KeyCode)
						{
							case Keys.NumPad0:
								// for testing
								this.session.VideoCard.Tokens.Enqueue(0x475c);
								break;

							case Keys.F1:
								this.session.VideoCard.Tokens.Enqueue(0x3b00);
								break;

							case Keys.F2:
								this.session.VideoCard.Tokens.Enqueue(0x3c00);
								break;

							case Keys.F3:
								this.session.VideoCard.Tokens.Enqueue(0x3d00);
								break;

							case Keys.F4:
								this.session.VideoCard.Tokens.Enqueue(0x3e00);
								break;

							case Keys.F5:
								this.session.VideoCard.Tokens.Enqueue(0x3f00);
								break;

							case Keys.F6:
								this.session.VideoCard.Tokens.Enqueue(0x4000);
								break;

							case Keys.F7:
								this.session.VideoCard.Tokens.Enqueue(0x4100);
								break;

							case Keys.F8:
								this.session.VideoCard.Tokens.Enqueue(0x4200);
								break;

							case Keys.F9:
								this.session.VideoCard.Tokens.Enqueue(0x4300);
								break;

							case Keys.F10:
								this.session.VideoCard.Tokens.Enqueue(0x4400);
								break;

							case Keys.F11:
								AssignFullScreen(!this.completeDisplay);
								break;

							case Keys.Down:
								this.session.VideoCard.Tokens.Enqueue(0x5000);
								break;

							case Keys.Left:
								this.session.VideoCard.Tokens.Enqueue(0x4b00);
								break;

							case Keys.Right:
								this.session.VideoCard.Tokens.Enqueue(0x4d00);
								break;

							case Keys.Up:
								this.session.VideoCard.Tokens.Enqueue(0x4800);
								break;

							case Keys.Home:
								this.session.VideoCard.Tokens.Enqueue(0x4700);
								break;

							case Keys.End:
								this.session.VideoCard.Tokens.Enqueue(0x4f00);
								break;

							case Keys.PageUp:
								this.session.VideoCard.Tokens.Enqueue(0x4900);
								break;

							case Keys.PageDown:
								this.session.VideoCard.Tokens.Enqueue(0x5100);
								break;
						}
					}
					else if ((ev.Modifiers & Keys.Shift) == Keys.Shift)
					{
						switch (ev.KeyCode)
						{
							case Keys.Down:
								this.session.VideoCard.Tokens.Enqueue(0x5032);
								break;

							case Keys.Left:
								this.session.VideoCard.Tokens.Enqueue(0x4b34);
								break;

							case Keys.Right:
								this.session.VideoCard.Tokens.Enqueue(0x4d36);
								break;

							case Keys.Up:
								this.session.VideoCard.Tokens.Enqueue(0x4838);
								break;

							case Keys.Home:
								this.session.VideoCard.Tokens.Enqueue(0x4737);
								break;

							case Keys.End:
								this.session.VideoCard.Tokens.Enqueue(0x4f31);
								break;

							case Keys.PageUp:
								this.session.VideoCard.Tokens.Enqueue(0x4939);
								break;

							case Keys.PageDown:
								this.session.VideoCard.Tokens.Enqueue(0x5133);
								break;
						}
					}
					else if ((ev.Modifiers & Keys.Alt) == Keys.Alt)
					{
						ev.Handled = true;
						ev.SuppressKeyPress = true;

						switch (ev.KeyCode)
						{
							case Keys.Enter:
								AssignFullScreen(!this.completeDisplay);
								break;

							case Keys.A:
								this.session.VideoCard.Tokens.Enqueue(0x1e00);
								break;

							case Keys.C:
								this.session.VideoCard.Tokens.Enqueue(0x2e00);
								break;

							case Keys.D:
								this.session.VideoCard.Tokens.Enqueue(0x2000);
								break;

							case Keys.G:
								this.session.VideoCard.Tokens.Enqueue(0x2200);
								break;

							case Keys.H:
								this.session.VideoCard.Tokens.Enqueue(0x2300);
								break;

							case Keys.M:
								this.session.VideoCard.Tokens.Enqueue(0x3200);
								break;

							case Keys.O:
								this.session.VideoCard.Tokens.Enqueue(0x1800);
								break;

							case Keys.Q:
								this.session.VideoCard.Tokens.Enqueue(0x1000);
								break;

							case Keys.R:
								this.session.VideoCard.Tokens.Enqueue(0x1300);
								break;

							case Keys.V:
								this.session.VideoCard.Tokens.Enqueue(0x2f00);
								break;

							case Keys.W:
								this.session.VideoCard.Tokens.Enqueue(0x1100);
								break;
						}
					}
				}
			}
		}

		private void OnMouseMoved(object caller, MouseEventArgs ev)
		{
			if (this.pointerBox.Contains(ev.Location))
			{
				// The window can have any size, so the position is scaled to the 320 x 200 game screen
				this.pointerSite = new Point((ev.Location.X - this.pointerBox.X) * 320 / this.displayExtent.Width,
					(ev.Location.Y - this.pointerBox.Y) * 200 / this.displayExtent.Height);
				this.pointerClicks = ev.Button;
			}
		}

		private void OnMouseButtonDown(object caller, MouseEventArgs ev)
		{
			if (this.pointerBox.Contains(ev.Location))
			{
				if (ev.Button == MouseButtons.Left)
				{
					this.pointerClicks |= MouseButtons.Left;
				}
				else if (ev.Button == MouseButtons.Right)
				{
					this.pointerClicks |= MouseButtons.Right;
				}
			}
		}

		private void OnMouseButtonUp(object caller, MouseEventArgs ev)
		{
			if (this.pointerBox.Contains(ev.Location))
			{
				if (ev.Button == MouseButtons.Left)
				{
					this.pointerClicks |= MouseButtons.Left;
					this.pointerClicks ^= MouseButtons.Left;
				}
				else if (ev.Button == MouseButtons.Right)
				{
					this.pointerClicks |= MouseButtons.Right;
					this.pointerClicks ^= MouseButtons.Right;
				}
			}
		}

		private void PauseButtonPressed(object caller, EventArgs ev)
		{
			if (this.session != null && this.session.Processor != null)
			{
				if (!this.session.VideoCard.Processor.Hold)
				{
					this.session.VideoCard.Processor.Hold = true;
					this.btnHold.Visible = false;
					this.btnOperate.Visible = true;

					using (Graphics gfx = Graphics.FromHwnd(this.Handle))
					{
						RepaintScreens(gfx, true);
					}
				}
			}
		}

		private void RunButtonPressed(object caller, EventArgs ev)
		{
			if (this.session != null && this.session.Processor != null)
			{
				if (this.session.VideoCard.Processor.Hold)
				{
					this.session.VideoCard.Processor.Hold = false;
					this.btnHold.Visible = true;
					this.btnOperate.Visible = false;

					using (Graphics gfx = Graphics.FromHwnd(this.Handle))
					{
						RepaintScreens(gfx, true);
					}
				}
			}
		}

		private void OnWindowShown(object caller, EventArgs ev)
		{
#if !DEBUG
            MessageBox.Show("This Alpha Release of Civilization1Remaster " +
				"most certainly has bugs, but most functions should work normally. " +
				"It is compatible with old civ.exe and can save/load original game files.\n" +
				"The Debug mode can be toggled by pressing Alt + D Key.",
				"Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
#endif
			if (this.beginCompleteDisplay)
			{
				this.beginCompleteDisplay = false;
				AssignFullScreen(true);
			}

			if (this.sessionWorker == null)
			{
				this.sessionWorker = new Thread(new ThreadStart(PlayThread));
				this.sessionWorker.Start();
			}
		}

		private void PlayThread()
		{
			try
			{
				this.session = new GameEngine(this);

				if (this.InvokeRequired)
				{
					this.Invoke(this.setupWidgets);
				}
				else
				{
					this.PrepareControlsMethod();
				}

				this.session.Launch();
			}
			catch (ApplicationExitException)
			{
			}
#if !DEBUG
			catch (Exception e)
			{
				if (this.session != null && this.session.Journal != null)
				{
					this.session.Journal.EmitLine("");
					this.session.Journal.EmitLine($"Exception message: {e.Message}");
					this.session.Journal.EmitLine($"Exception source: {e.Source}");
					this.session.Journal.EmitLine($"Exception stack trace: {e.StackTrace}");
				}

				MessageBox.Show("There was an error (Exception) in the Civilization1Remaster application, "+
					"the details about the error should be in a Log.txt file.");
			}
#endif
			if (!this.shutting)
			{
				this.shutting = true;

				try
				{
					if (this.InvokeRequired)
					{
						this.Invoke(this.shutWindow);
					}
					else
					{
						this.Close();
					}
				}
				catch { }
			}
		}

		private void OnWindowClosing(object caller, FormClosingEventArgs ev)
		{
			if (this.shutting || MessageBox.Show("Are you sure you want to quit?", "Exiting application", MessageBoxButtons.OKCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) ==
				DialogResult.OK)
			{
				this.shutting = true;

				if (this.session != null && this.session.Processor != null)
				{
					this.session.Processor.WhenApplicationQuits();
				}
			}
			else
			{
				ev.Cancel = true;
			}
		}
	}
}