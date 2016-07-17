using System;
using System.Collections.Generic;
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
			public List<string> entries { get; set; }
		}

		public OVPSettings ovpSettings, ovp2Settings;
		public System.Timers.Timer m_timer;

		public PointF[] refPoly;
		public PointF[] previewPoly;

		TestViewport viewport;
		ProgressBar progressBar;
		Label statusLine;

		ComboBox testComboBox;
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

		private void updateSimUIMT_()
		{
			m_timer.Elapsed += new System.Timers.ElapsedEventHandler(updatePreview);
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
				ovpSettings.polyList.Clear();
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
				newPoly[pt] = new PointF((float)(refPoly[pt].X * myRandom1), (float)(refPoly[pt].Y * myRandom));
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
			DataContext = new myStuff { entries = new List<string> { "First", "Second" } };

			refPoly = new PointF[5];
			refPoly[0] = new PointF(10, 10);
			refPoly[1] = new PointF(20, 10);
			refPoly[2] = new PointF(20, 5);
			refPoly[3] = new PointF(10, 5);
			refPoly[4] = refPoly[0];

			drawingLock = new object();

			MinimumSize = new Size(200, 200);

			updateSimUIMTFunc = updateSimUIMT_;

			numberOfCases = 25000;
			timer_interval = 1000;

            ovpSettings = new OVPSettings ();
            ovp2Settings = new OVPSettings ();

            ovp2Settings.zoomFactor = 3;

            Title = "My Eto Form";

            viewport = new TestViewport (ovpSettings);
            viewport.Size = new Size (250, 250);

			var viewport2 = new TestViewport (ovp2Settings);
            viewport2.Size = new Size(200, 200);

			Panel testing = new Panel();
			testing.Content = new Splitter
			{
				Orientation = Orientation.Horizontal,
				FixedPanel = SplitterFixedPanel.None,
				Panel1 = viewport,
				Panel2 = viewport2
			};

			statusLine = new Label();
			statusLine.Text = "Hello world";

			progressBar = new ProgressBar();
			progressBar.MaxValue = numberOfCases;

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

			testComboBox = new ComboBox();
			testComboBox.ReadOnly = true;
			testComboBox.BindDataContext(c => c.DataStore, (myStuff m) => m.entries);

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

			Content = new Splitter
			{
				Orientation = Orientation.Vertical,
				FixedPanel = SplitterFixedPanel.None,
				Panel1 = testing4,
				Panel2 = testing2
            };

            // create a few commands that can be used for the menu and toolbar
            var clickMe = new Command { MenuText = "Run", ToolBarText = "Run" };
            clickMe.Executed += runCases;

			var abort = new Command { MenuText = "Abort", ToolBarText = "Abort" };
			abort.Executed += abortTheRun;

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
            ToolBar = new ToolBar { Items = { clickMe, abort } };

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