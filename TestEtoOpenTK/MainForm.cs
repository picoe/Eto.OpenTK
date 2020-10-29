using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Eto.Forms;
using Eto.Drawing;
using Eto.OpenTK;
using System.Diagnostics;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace TestEtoOpenTK
{
	/// <summary>
	/// Your application's main form
	/// </summary>
	public class MainForm : Form
	{
		static Random random = new Random();
		OVPSettings ovpSettings, ovp2Settings;
		PointF[] refPoly;

		TestViewport viewport, viewport2;
		ProgressBar progressBar;
		Label statusLine;

		Int32 numberOfCases = 1000;
		double timerInterval = 1.0 / 30.0; // 60 hz
		Int32 currentProgress;
		CancellationTokenSource cancelSource;

		public void PreviewUpdate()
		{
			Application.Instance.Invoke(() => {
			lock (ovpSettings)
			{
				// invalidate as there's no point updating the view directly
				// viewport.Invalidate();
				viewport.UpdateViewport();
			}
				double progress = (double)currentProgress / (double)numberOfCases;
				statusLine.Text = $"{(progress * 100):#.##} % complete";
				progressBar.Value = currentProgress;
			});
		}

		public void StartRun()
		{
			cancelSource?.Cancel();
			progressBar.MaxValue = numberOfCases;
			progressBar.Value = 0;
			Task.Factory.StartNew(Run);
		}

		public void Run()
		{
			currentProgress = 0;
			ovpSettings.polyList.Clear();

			// Set up timers for the UI refresh
			var timer = new System.Timers.Timer();
			timer.Interval = (int)(1000 * timerInterval);
			timer.Elapsed += (sender, e) => PreviewUpdate();
			timer.Start();

			// Set our parallel task options based on user settings.
			var po = new ParallelOptions();

			cancelSource = new CancellationTokenSource();
			var cancellationToken = cancelSource.Token;
			po.MaxDegreeOfParallelism = 4;

			Parallel.For(0, numberOfCases, po, (i, loopState) =>
			{
				PointF[] newPoly = CreatePoly();
				Interlocked.Increment(ref currentProgress);
				lock (ovpSettings)
				{
					ovpSettings.addPolygon(newPoly, new Color(0, 0.5f, 0), 0.7f, false);
				}

				if (cancellationToken.IsCancellationRequested)
				{
					timer.Stop();
					loopState.Stop();
				}
			});

			timer.Stop();
			timer.Dispose();
			timer = null;
		}

		private PointF[] CreatePoly()
		{
			double myRandom = random.NextDouble();
			double myRandom1 = random.NextDouble();

			PointF[] newPoly = new PointF[5];
			for (int pt = 0; pt < newPoly.Length; pt++)
			{
				newPoly[pt] = new PointF((float)(refPoly[pt].X + (400.0f * myRandom1) - 200f), (float)(refPoly[pt].Y + (400.0f * myRandom) - 200f));
			}

			Thread.Sleep(50);

			return newPoly;
		}

		public void AbortTheRun(object sender, EventArgs e)
		{
			cancelSource?.Cancel();
		}

		public MainForm()
		{
			refPoly = new PointF[5];
			refPoly[0] = new PointF(-10, 10);
			refPoly[1] = new PointF(10, 10);
			refPoly[2] = new PointF(10, -10);
			refPoly[3] = new PointF(-10, -10);
			refPoly[4] = refPoly[0];

			MinimumSize = new Size(300, 300);

			ovpSettings = new OVPSettings();
			ovp2Settings = new OVPSettings();

			ovp2Settings.zoomFactor = 3;

			Title = "Eto.OpenTK Test";


			statusLine = new Label();

			progressBar = new ProgressBar();
			progressBar.MaxValue = numberOfCases;

			/*
			  Test flags.
			  0 : stamdard viewports in splitter test.
			  1 : viewports in tabs (WPF has issues here due to the deferred evaluation; still need a better fix)
			  2 : single viewport in panel
			*/

			int mode = 2;

			if (mode == 0)
			{
				viewport = new TestViewport(ovpSettings);
				viewport.Size = new Size(350, 350);

				viewport2 = new TestViewport(ovp2Settings);
				viewport2.Size = new Size(300, 300);

				var testing = new Splitter
				{
					Orientation = Orientation.Horizontal,
					FixedPanel = SplitterFixedPanel.None,
					Panel1 = viewport,
					Panel2 = viewport2
				};

				var layout = new DynamicLayout();
				layout.Add(testing, xscale: true, yscale: true);
				layout.Add(TableLayout.HorizontalScaled(statusLine, progressBar));

				Content = layout;
			}
			if (mode == 1)
			{
				TabControl tabControl_main = new TabControl();
				tabControl_main.Size = new Size(300, 300);
				Content = tabControl_main;

				TabPage tab_0 = new TabPage();
				tab_0.Text = "0";
				tabControl_main.Pages.Add(tab_0);
				PixelLayout tabPage_0_content = new PixelLayout();
				tabPage_0_content.Size = new Size(280, 280);

				TabPage tab_1 = new TabPage();
				tab_1.Text = "1";
				tabControl_main.Pages.Add(tab_1);
				PixelLayout tabPage_1_content = new PixelLayout();
				tabPage_1_content.Size = new Size(280, 280);
				tab_1.Content = tabPage_1_content;

				TabPage tab_2 = new TabPage();
				tab_2.Text = "2";
				tabControl_main.Pages.Add(tab_2);

				viewport = new TestViewport(ovpSettings);
				viewport.Size = new Size(200, 200);
				tabPage_1_content.Add(viewport, 5, 5);

				viewport2 = new TestViewport(ovp2Settings);
				viewport2.Size = new Size(200, 200);
				tab_2.Content = viewport2;
			}
			if (mode == 2)
			{
				Content = viewport = new TestViewport(ovpSettings) { Size = new Size(600, 400) };
			}

			// create a few commands that can be used for the menu and toolbar
			var runCommand = new Command { MenuText = "Run", ToolBarText = "Run" };
			runCommand.Executed += (sender, e) => StartRun();

			var abortCommand = new Command { MenuText = "Abort", ToolBarText = "Abort" };
			abortCommand.Executed += AbortTheRun;

			var quitCommand = new Command { MenuText = "Quit", Shortcut = Application.Instance.CommonModifier | Keys.Q };
			quitCommand.Executed += (sender, e) => Application.Instance.Quit();

			var aboutCommand = new Command { MenuText = "About..." };
			aboutCommand.Executed += (sender, e) => new AboutDialog().ShowDialog(this);

			// create menu
			Menu = new MenuBar
			{
				Items = {
					// File submenu
					new ButtonMenuItem { Text = "&File", Items = { runCommand, abortCommand } },
				},
				QuitItem = quitCommand,
				AboutItem = aboutCommand
			};

			// create toolbar			
			ToolBar = new ToolBar { Items = { runCommand, abortCommand } };
		}
	}

}