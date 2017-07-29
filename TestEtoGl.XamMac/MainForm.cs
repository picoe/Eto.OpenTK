using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Eto.Forms;
using Eto.Drawing;
using Eto.Gl;
using System.Diagnostics;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Cryptography;

namespace TestEtoGl
{
	/// <summary>
	/// Your application's main form
	/// </summary>
	public class MainForm : Form
	{
		public class myStuff
		{
			public List<ObservableCollection<string>> entries { get; set; }
		}

		public List<ObservableCollection<string>> myList;

		public OVPSettings ovpSettings, ovp2Settings, vSettings;
		public System.Timers.Timer m_timer;

		public PointF[] refPoly;
		public PointF[] previewPoly;

		TestViewport viewport, viewport2;
		ProgressBar progressBar;
		Label statusLine;

		DropDown testComboBox;
		Button testComboBox_SelEntry;

		public Int32 numberOfCases;
		public Int32 timer_interval;
		public object drawingLock;
		public Int64 timeOfLastPreviewUpdate;
		public Stopwatch sw;
		public double swTime;
		public Stopwatch sw_Preview;
		public Int32 currentProgress;
		public bool runAbort;
		public bool drawing;

		public delegate void updateSimUIMT();
		public updateSimUIMT updateSimUIMTFunc { get; set; }

		public delegate void abortRun();
		public abortRun abortRunFunc { get; set; }

		public delegate void configureProgressBar(Int32 maxValue);
		public configureProgressBar configureProgressBarFunc { get; set; }

		private void updateSimUIMT_()
		{
			m_timer.Elapsed += new System.Timers.ElapsedEventHandler(updatePreview);
		}

		private void configureProgressBar_(Int32 maxValue)
		{
            Application.Instance.Invoke(() =>
            {
                progressBar.MaxValue = maxValue;
                progressBar.Value = 0;
            });
		}

		private void updatePreview(object sender, EventArgs e)
		{
			if (Monitor.TryEnter(drawingLock))
			{
				try
				{
					drawing = true;
					//if ((sw_Preview.Elapsed.TotalMilliseconds - timeOfLastPreviewUpdate) > m_timer.Interval)
					{
						//						try
						{
							Application.Instance.Invoke(new Action(() =>
							{
								// also sets commonVars.drawing to 'false'
								previewUpdate();
							}));
						}
					}
				}
				catch (Exception)
				{
				}
				finally
				{
					Monitor.Exit(drawingLock);
					drawing = false;
				}
			}
		}

		public void previewUpdate()
		{
			{
				//ovpSettings.polyList.Clear();
				lock (previewPoly)
				{
					ovpSettings.addPolygon(previewPoly.ToArray(), new Color(0, 0.5f, 0));
				}
				viewport.updateViewport();
				double progress = (double)currentProgress / (double)numberOfCases;
				statusLine.Text = (progress * 100).ToString("#.##") + "% complete";
				progressBar.Value = currentProgress; // * 100.0f;
			}
		}

		public void runCases(object sender, EventArgs e)
		{
			Task t2 = Task.Factory.StartNew(() =>
			{
				run2();
			}
			);

			if (t2.IsCompleted || t2.IsCanceled || t2.IsFaulted)
			{
				t2.Dispose();
			}
		}

		public void run2()
		{
			currentProgress = 0;
			ovpSettings.polyList.Clear();
			configureProgressBarFunc?.Invoke(numberOfCases);
			previewPoly = refPoly.ToArray();
			m_timer = new System.Timers.Timer();
			// Set up timers for the UI refresh
			m_timer.AutoReset = true;
			m_timer.Interval = timer_interval;
			updateSimUIMTFunc?.Invoke();
			m_timer.Start();

			swTime = 0.0; // reset time for the batch
			timeOfLastPreviewUpdate = 0;
			sw = new Stopwatch();
			sw.Stop();
			sw.Reset();
			sw_Preview = new Stopwatch();
			sw_Preview.Stop();
			sw_Preview.Reset();

			// Set our parallel task options based on user settings.
			ParallelOptions po = new ParallelOptions();
			// Attempt at parallelism.
			CancellationTokenSource cancelSource = new CancellationTokenSource();
			CancellationToken cancellationToken = cancelSource.Token;
			po.MaxDegreeOfParallelism = 4;

			// Run a task to enable cancelling from another thread.
			Task t = Task.Factory.StartNew(() =>
			{
				if (abortRunFunc != null)
				{
					abortRunFunc();
					if (runAbort)
					{
						cancelSource.Cancel();
					}
				}
			}
			);

			sw.Start();
			sw_Preview.Start();

			try
			{
				Parallel.For(0, numberOfCases, po, (i, loopState) =>
				{
					try
					{
						PointF[] newPoly = randomScale_MT();
						Interlocked.Increment(ref currentProgress);
						if (!drawing)
						{
							lock (previewPoly)
							{
								previewPoly = newPoly.ToArray();
							}
						}
						if (runAbort)
						{
							cancelSource.Cancel();
							cancellationToken.ThrowIfCancellationRequested();
						}
					}
					catch (OperationCanceledException)
					{
						m_timer.Stop();
						runAbort = false; // reset state to allow user to abort save of results.
						sw.Stop();
						loopState.Stop();
					}
				});
			}
			catch (Exception ex)
			{
				var err = ex.ToString();
			}

			t.Dispose();
			sw.Stop();
			sw.Reset();
			sw_Preview.Stop();
			sw_Preview.Reset();
			m_timer.Stop();
			m_timer.Dispose();
		}

		private PointF[] randomScale_MT()
		{
			double myRandom = RNG.random_gauss3()[0];
			double myRandom1 = RNG.random_gauss3()[1];

			PointF[] newPoly = new PointF[5];
			for (int pt = 0; pt < newPoly.Length; pt++)
			{
				newPoly[pt] = new PointF((float)(refPoly[pt].X + (100.0f * myRandom1)), (float)(refPoly[pt].Y + (100.0f * myRandom)));
			}

			Thread.Sleep(10);

			return newPoly;
		}

		public void abortTheRun(object sender, EventArgs e)
		{
			runAbort = true;
		}

		private void changeSelEntry(object sender, EventArgs e)
		{
			testComboBox.SelectedIndex = Math.Abs(testComboBox.SelectedIndex - 1);
		}

        public MainForm ()
        {
			myList = new List<ObservableCollection<string>>();
			myList.Add(new ObservableCollection<string> { "First", "Second" });

			DataContext = new myStuff
			{
				entries = myList
			};

			refPoly = new PointF[5];
			refPoly[0] = new PointF(-50, 50);
			refPoly[1] = new PointF(50, 50);
			refPoly[2] = new PointF(50, -50);
			refPoly[3] = new PointF(-50, -50);
			refPoly[4] = refPoly[0];

			drawingLock = new object();

			MinimumSize = new Size(200, 200);

			updateSimUIMTFunc = updateSimUIMT_;

			configureProgressBarFunc = configureProgressBar_;

			numberOfCases = 25000;
			timer_interval = 10;

            ovpSettings = new OVPSettings ();
			ovp2Settings = new OVPSettings ();

            ovp2Settings.zoomFactor = 3;

            Title = "My Eto Form";

			/*
			  Test flags.
			  0 : stamdard viewports in splitter test.
			  1 : viewports in tabs (WPF has issues here due to the deferred evaluation; still need a better fix)
			  2 : single viewport in panel, dropdown switches out the view settings.
			*/

			int mode = 3;

			if (mode == 0)
			{
				viewport = new TestViewport(ref ovpSettings);
				viewport.Size = new Size(250, 250);

				viewport2 = new TestViewport(ref ovp2Settings);
				viewport2.Size = new Size(200, 200);

				Panel testing = new Panel();
				testing.Size = new Size(viewport.Width + viewport2.Width, viewport.Height);
				testing.Content = new Splitter
				{
					Orientation = Orientation.Horizontal,
					FixedPanel = SplitterFixedPanel.None,
					Panel1 = viewport,
					Panel2 = viewport2
				};

				Panel testing2 = new Panel();
				testing2.Content = new Splitter
				{
					Orientation = Orientation.Horizontal,
					FixedPanel = SplitterFixedPanel.None,
					Panel1 = statusLine,
					Panel2 = progressBar
				};

				testComboBox_SelEntry = new Button();
				testComboBox_SelEntry.Text = "Change";
				testComboBox_SelEntry.Click += changeSelEntry;

				testComboBox = new DropDown();
				testComboBox.DataContext = DataContext;
				testComboBox.BindDataContext(c => c.DataStore, (myStuff m) => m.entries[0]);
				testComboBox.SelectedIndex = 0;
				//testComboBox.SelectedIndexBinding.BindDataContext((myStuff m) => m.index);

				Panel testing3 = new Panel();
				testing3.Content = new Splitter
				{
					Orientation = Orientation.Horizontal,
					FixedPanel = SplitterFixedPanel.None,
					Panel1 = testComboBox_SelEntry,
					Panel2 = testComboBox
				};

				Panel testing4 = new Panel();
				testing4.Content = new Splitter
				{
					Orientation = Orientation.Vertical,
					FixedPanel = SplitterFixedPanel.None,
					Panel1 = testing3,
					Panel2 = testing
				};

				Splitter mySplitter = new Splitter
				{
					Orientation = Orientation.Vertical,
					FixedPanel = SplitterFixedPanel.None,
					Panel1 = testing4,
					Panel2 = testing2
				};

				Content = mySplitter;
			}
			if (mode == 1)
			{
				PixelLayout mainContent = new PixelLayout();
				mainContent.Size = new Size(300, 300);
				Content = mainContent;
				TabControl tabControl_main = new TabControl();
				tabControl_main.Size = new Size(300, 300);
				mainContent.Add(tabControl_main, 1, 20);

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
				PixelLayout tabPage_2_content = new PixelLayout();
				tabPage_2_content.Size = new Size(280, 280);
				tab_2.Content = tabPage_2_content;

				viewport = new TestViewport(ref ovpSettings);
				viewport.Size = new Size(200, 200);
				tabPage_1_content.Add(viewport, 5, 5);

				viewport2 = new TestViewport(ref ovp2Settings);
				viewport2.Size = new Size(200, 200);
				tabPage_2_content.Add(viewport2, 5, 5);
			}
			if (mode == 3)
			{
				ovpSettings.addPolygon(refPoly, Color.FromArgb(0, 255, 0));
				ovp2Settings.addPolygon(refPoly, Color.FromArgb(255, 0, 0));

				vSettings = new OVPSettings();
				viewport = new TestViewport(ref vSettings);
				viewport.Size = new Size(250, 250);

				Panel testing = new Panel();
				testing.Size = new Size(viewport.Width, viewport.Height);
				PixelLayout p = new PixelLayout();
				p.Add(viewport, 0, 0);
				testing.Content = p;
	
				testComboBox_SelEntry = new Button();
				testComboBox_SelEntry.Text = "Change";
				testComboBox_SelEntry.Click += changeSelEntry;

				testComboBox = new DropDown();
				testComboBox.DataContext = DataContext;
				testComboBox.BindDataContext(c => c.DataStore, (myStuff m) => m.entries[0]);
				testComboBox.SelectedIndex = 0;
				testComboBox.SelectedIndexChanged += adjustView_;
				//testComboBox.SelectedIndexBinding.BindDataContext((myStuff m) => m.index);

				Panel testing3 = new Panel();
				testing3.Content = new Splitter
				{
					Orientation = Orientation.Horizontal,
					FixedPanel = SplitterFixedPanel.None,
					Panel1 = testComboBox_SelEntry,
					Panel2 = testComboBox
				};

				Panel testing4 = new Panel();
				testing4.Content = new Splitter
				{
					Orientation = Orientation.Vertical,
					FixedPanel = SplitterFixedPanel.None,
					Panel1 = testing3,
					Panel2 = testing
				};

				Splitter mySplitter = new Splitter
				{
					Orientation = Orientation.Vertical,
					FixedPanel = SplitterFixedPanel.None,
					Panel1 = testing4,
					Panel2 = testing
				};

				Content = mySplitter;
			}

			statusLine = new Label();
			statusLine.Size = new Size(150, 11);
			statusLine.Text = "Hello world";

			progressBar = new ProgressBar();
			progressBar.Height = 15;
			progressBar.MaxValue = numberOfCases;

			// create a few commands that can be used for the menu and toolbar
			var clickMe = new Command { MenuText = "Run", ToolBarText = "Run" };
            clickMe.Executed += runCases;

			var abort = new Command { MenuText = "Abort", ToolBarText = "Abort" };
			abort.Executed += abortTheRun;

			var adjustList = new Command { MenuText = "Add to list", ToolBarText = "Add" };
			if (mode != 3)
			{
				adjustList.Executed += adjustList_;
			}

			var quitCommand = new Command { MenuText = "Quit", Shortcut = Application.Instance.CommonModifier | Keys.Q };
            quitCommand.Executed += (sender, e) => Application.Instance.Quit ();

            var aboutCommand = new Command { MenuText = "About..." };
            aboutCommand.Executed += (sender, e) => MessageBox.Show (this, "About my app...");

            // create menu
            Menu = new MenuBar {
                Items = {
					// File submenu
					new ButtonMenuItem { Text = "&File", Items = { clickMe } },
					// new ButtonMenuItem { Text = "&Edit", Items = { /* commands/items */ } },
					// new ButtonMenuItem { Text = "&View", Items = { /* commands/items */ } },
				},
                ApplicationItems = {
					// application (OS X) or file menu (others)
					new ButtonMenuItem { Text = "&Preferences..." },
                },
                QuitItem = quitCommand,
                AboutItem = aboutCommand
            };

            // create toolbar			
            ToolBar = new ToolBar { Items = { clickMe, abort, adjustList } };

			//mySplitter.Panel1.SizeChanged += splitterSize;
			//mySplitter.Panel2.SizeChanged += splitterSize;
		}

		void adjustView_(object sender, EventArgs e)
		{
			if (testComboBox.SelectedIndex == 0)
			{
				viewport.changeSettings(ref ovpSettings);
			}
			else
			{
				viewport.changeSettings(ref ovp2Settings);
			}
		}


		void adjustList_(object sender, EventArgs e)
		{
			myList[0].Add("Entry " + myList[0].Count.ToString());
			if (testComboBox.SelectedIndex == -1)
			{
				testComboBox.SelectedIndex = 0;
			}
			if (testComboBox.SelectedIndex >= myList[0].Count)
			{
				testComboBox.SelectedIndex = myList[0].Count - 1;
			}
//			testComboBox.SelectedIndex = 1;
		}

		protected override void OnWindowStateChanged(EventArgs e)
		{
			base.OnWindowStateChanged(e);
			viewport.updateViewport();
			viewport.updateViewport();
			if (viewport2 != null)
			{
				viewport2.updateViewport();
			}
		}

		void splitterSize(object sender, EventArgs e)
		{
			viewport.updateViewport();
			viewport.updateViewport();
			if (viewport2 != null)
			{
				viewport2.updateViewport();
			}
		}

		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			viewport.updateViewport();
			if (viewport2 != null)
			{
				viewport2.updateViewport();
			}
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			viewport.updateViewport();
			viewport.updateViewport();
			if (viewport2 != null)
			{
				viewport2.updateViewport();
			}
		}

	}


	public static class RNG
	{
		/*
         * This class is interesting. Originally, the intent was to have per-thread RNGs, but it became apparent that threads that instantiated an RNG
         * would get the same random number distribution when the RNGs were initialized at the same system time.
         * To avoid this, earlier systems made a common RNG and the threads would query from that RNG.
         * However, this was not thread-safe, such that the calls to the RNG would start returning 0 and the application enters a spiral of death as the RNG was 
         * continuously, and unsuccessfully, polled for a non-zero value.
         * 
         * Locking the RNG was one option, so that only one thread could query at a time, but this caused severe performance issues.
         * 
         * So, to address this, I've gone back to a per-thread RNG (referenced in jobSettings()) and then use the RNGCryptoServiceProvider to provide a 
         * 'seed' value for a null RNG entity. This avoids some severe performance issues if the RNGCryptoServiceProvider is used for all random numbers.
         * 
         * Ref : http://blogs.msdn.com/b/pfxteam/archive/2009/02/19/9434171.aspx
         */
		private static RNGCryptoServiceProvider _global = new RNGCryptoServiceProvider();

		[ThreadStatic]
		private static Random _local;

		public static double[] random_gauss3()
		{
			Random random = _local;

			if (random == null)
			{
				byte[] buffer = new byte[4];
				_global.GetBytes(buffer);
				_local = random = new Random(BitConverter.ToInt32(buffer, 0));
			}

			// Box-Muller transform
			// We aren't allowed 0, so we reject any values approaching zero.
			double U1, U2;
			U1 = random.NextDouble();
			while (U1 < 1E-15)
			{
				U1 = random.NextDouble();
			}
			U2 = random.NextDouble();
			while (U2 < 1E-15)
			{
				U2 = random.NextDouble();
			}
			// PAs are 3-sigma, so this needs to be divided by 3 to give single sigma value when used
			double A1 = Math.Sqrt(-2 * Math.Log(U2, Math.E)) * Math.Cos(2 * Math.PI * U1) / 3;
			double A2 = Math.Sqrt(-2 * Math.Log(U1, Math.E)) * Math.Sin(2 * Math.PI * U2) / 3;
			double[] myReturn = { A1, A2 };
			return myReturn;

		}
		// This is our Gaussian RNG
		public static double random_gauss()
		{
			Random random = _local;

			if (random == null)
			{
				byte[] buffer = new byte[4];
				_global.GetBytes(buffer);
				_local = random = new Random(BitConverter.ToInt32(buffer, 0));
			}

			// We aren't allowed 0, so we reject any values approaching zero.
			double U1 = random.NextDouble();
			while (U1 < 1E-15)
			{
				U1 = random.NextDouble();
			}
			double U2 = random.NextDouble();
			while (U2 < 1E-15)
			{
				U2 = random.NextDouble();
			}
			// PAs are 3-sigma, so this needs to be divided by 3 to give single sigma value when used
			double A1 = Math.Sqrt(-2 * Math.Log(U2, Math.E)) * Math.Cos(2 * Math.PI * U1) / 3;
			double A2 = Math.Sqrt(-2 * Math.Log(U1, Math.E)) * Math.Sin(2 * Math.PI * U2) / 3;
			return A1;
		}

		// This is a slightly different version of our Gaussian RNG
		public static double random_gauss2()
		{
			Random random = _local;

			if (random == null)
			{
				byte[] buffer = new byte[4];
				_global.GetBytes(buffer);
				_local = random = new Random(BitConverter.ToInt32(buffer, 0));
			}
			// We aren't allowed 0, so we reject any values approaching zero.
			double U1 = random.NextDouble();
			while (U1 < 1E-15)
			{
				U1 = random.NextDouble();
			}
			double U2 = random.NextDouble();
			while (U2 < 1E-15)
			{
				U2 = random.NextDouble();
			}
			// PAs are 3-sigma, so this needs to be divided by 3 to give single sigma value when used
			double A2 = Math.Sqrt(-2 * Math.Log(U1, Math.E)) * Math.Sin(2 * Math.PI * U2) / 3;
			return A2;
		}
	}

}