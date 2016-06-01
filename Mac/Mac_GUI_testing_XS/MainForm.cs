using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Gl;

namespace Mac_GUI_testing_XS
{
	/// <summary>
	/// Your application's main form
	/// </summary>
		public class MainForm : Form
		{
			private GLSurface glControl1;
			private GLSurface glControl2;
			etoViewport oVP2, oVP;

			public OVPSettings ovpSettings, ovp2Settings;

			bool loaded = false;
			bool loaded2 = false;

			private void setupViewports ()
			{
				glControl1 = new GLSurface ();
				glControl2 = new GLSurface ();
				SuspendLayout ();
				// 
				// glControl1
				// 
				//glControl1.BackgroundColor = Color.Black;
				//glControl1.Location = new Point(328, 13);
				glControl1.Size = new Size (259, 236);
				// 
				// glControl2
				// 
				//glControl2.BackgroundColor = Color.Black;
				//glControl2.Location = new Point(30, 29);
				glControl2.Size = new Size (150, 150);

				//Controls.Add(glControl2);
				//Controls.Add(glControl1);
				ResumeLayout ();
			}

			public MainForm ()
			{
				ovpSettings = new OVPSettings ();
				ovp2Settings = new OVPSettings ();
				List<PointF []> polyList = new List<PointF []> ();
				PointF [] testPoly = new PointF [5];
				testPoly [0] = new PointF (100, 100);
				testPoly [1] = new PointF (200, 100);
				testPoly [2] = new PointF (200, 50);
				testPoly [3] = new PointF (100, 50);
				testPoly [4] = testPoly [0];

				polyList.Add (testPoly);
				ovpSettings.addPolygon (testPoly, new Color (0, 0, 0));
				ovp2Settings.addPolygon (testPoly, new Color (0, 0, 0));

				testPoly = new PointF [5];
				testPoly [0] = new PointF (-80, -100);
				testPoly [1] = new PointF (-180, -100);
				testPoly [2] = new PointF (-200, -50);
				testPoly [3] = new PointF (-100, -50);
				testPoly [4] = testPoly [0];
				polyList.Add (testPoly);
				ovpSettings.addPolygon (testPoly, new Color (1, 0, 0));
				ovp2Settings.addPolygon (testPoly, new Color (1, 0, 0));
				ovp2Settings.zoomFactor = 3;


				Title = "My Eto Form";
				ClientSize = new Size (400, 350);

				setupViewports ();

				etoViewport viewport = new etoViewport (glControl1, ovpSettings);

				// scrollable region as the main content
				PixelLayout content_ = new PixelLayout ();
				Content = content_;

				content_.Add (viewport, new Point (0, 0));

				// create a few commands that can be used for the menu and toolbar
				var clickMe = new Command { MenuText = "Click Me!", ToolBarText = "Click Me!" };
				clickMe.Executed += (sender, e) => MessageBox.Show (this, "I was clicked!");

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
				ToolBar = new ToolBar { Items = { clickMe } };

                viewport.Invalidate ();

                viewport.updateViewport ();
			}
		}
		/*
		public MainForm2()
		{
			Title = "My Eto Form";
			ClientSize = new Size(400, 350);

			// scrollable region as the main content
			Content = new Scrollable
			{
				// table with three rows
				Content = new TableLayout(
					null,
					// row with three columns
					new TableRow(null, new Label { Text = "Hello World!" }, null),
					null
				)
			};

			// create a few commands that can be used for the menu and toolbar
			var clickMe = new Command { MenuText = "Click Me!", ToolBarText = "Click Me!" };
			clickMe.Executed += (sender, e) => MessageBox.Show(this, "I was clicked!");

			var quitCommand = new Command { MenuText = "Quit", Shortcut = Application.Instance.CommonModifier | Keys.Q };
			quitCommand.Executed += (sender, e) => Application.Instance.Quit();
			
			var aboutCommand = new Command { MenuText = "About..." };
			aboutCommand.Executed += (sender, e) => MessageBox.Show(this, "About my app...");
					
			// create menu
			Menu = new MenuBar
			{
				Items = {
					// File submenu
					new ButtonMenuItem { Text = "&File", Items = { clickMe } },
					// new ButtonMenuItem { Text = "&Edit", Items = { /* commands/items * / } },
					// new ButtonMenuItem { Text = "&View", Items = { /* commands/items * / } },
				},
				ApplicationItems = {
					// application (OS X) or file menu (others)
					new ButtonMenuItem { Text = "&Preferences..." },
				},
				QuitItem = quitCommand,
				AboutItem = aboutCommand
			};
			
			// create toolbar			
			ToolBar = new ToolBar { Items = { clickMe } };
		}
		
	}*/
}