using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Gl;
using System.Diagnostics;

namespace TestEtoGl
{
    /// <summary>
    /// Your application's main form
    /// </summary>
    public class MainForm : Form
    {
        public OVPSettings ovpSettings, ovp2Settings;

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

            var viewport = new TestViewport (ovpSettings);
            viewport.Size = new Size (250, 250);

			var viewport2 = new TestViewport (ovp2Settings);
            viewport2.Size = new Size(200, 200);

			Content = new Splitter {
                Orientation = Orientation.Horizontal,
                FixedPanel = SplitterFixedPanel.None,
                Panel1 = viewport,
                Panel2 = viewport2
            };

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
        }
    }
}