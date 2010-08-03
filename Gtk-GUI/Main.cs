/* Copyright (c) 2010 Sean McNamara
 * This program is free software. It comes without any warranty, to
 * the extent permitted by applicable law. You can redistribute it
 * and/or modify it under the terms of the Do What The Fuck You Want
 * To Public License, Version 2, as published by Sam Hocevar. See
 * http://sam.zoy.org/wtfpl/COPYING for more details. */

using System;
using System.IO;
using System.Reflection;
using System.Threading;

using GLib;
using Gtk;

using LibTehthu;

namespace GtkGUI
{
	
	/*FIXME: Tons of static data, duplicated code, etc. This is shameful. LibTehthu is orders of magnitude tidier, and it's slightly more complicated than this.*/
	class MainClass
	{
		private static System.Threading.Thread monitor = new System.Threading.Thread(MainClass.ConsoleEater);
		private static Tehthu teh = null;
		static Glade.XML xml = null;
		static Gtk.Window mainwindow = null;
		static Gtk.TextView inputView = null, outputView = null, debugView = null;
		static bool leftToRight = true;
		
		public static void Main (string[] args)
		{
			Assembly ass = Assembly.GetExecutingAssembly();
			
			//Useful for seeing the names of embedded resources....
			/*
			string[] resources = ass.GetManifestResourceNames();
			foreach(string ss in resources)
			{
				Console.WriteLine("Resource " + ss);	
			}
			*/
			
			Stream s = ass.GetManifestResourceStream("GtkGUI.teh.glade");
			StreamReader sr = new StreamReader(s);
			string gladefile = sr.ReadToEnd();
			
			Application.Init ();
			xml = new Glade.XML(gladefile, gladefile.Length, "MW", null);
			if(xml == null)
			{
				Console.WriteLine("Failed to open Glade XML file!");	
				return;
			}
			
			mainwindow = (Gtk.Window) xml.GetWidget("MW");
			if(mainwindow == null)
			{
				Console.WriteLine("Failed to create main window!");	
				return;
			}
			else
			{
				object[] _button_options = {Gtk.Stock.Open, Gtk.ResponseType.Accept, Gtk.Stock.Cancel, Gtk.ResponseType.Cancel, null};
				FileChooserDialog fcd = new FileChooserDialog("Open A Dictionary", mainwindow, FileChooserAction.Open, _button_options);
				
				inputView = (Gtk.TextView) xml.GetWidget("InputView");
				outputView = (Gtk.TextView) xml.GetWidget("OutputView");
				debugView = (Gtk.TextView) xml.GetWidget("DebugView");
				
				mainwindow.DestroyEvent += delegate(object o, DestroyEventArgs dea) {
					Application.Quit();
				};
				
				mainwindow.Destroyed += delegate(object sender, EventArgs e) {
					Application.Quit();
				};
				
				Gtk.MenuItem quit = (Gtk.MenuItem) xml.GetWidget("Quit");
				quit.Activated += delegate(object sender, EventArgs e) {
					Application.Quit();
				};
				
				Gtk.Notebook nb = (Gtk.Notebook) xml.GetWidget("Notebook");
				
				Gtk.RadioButton leftButton = (Gtk.RadioButton) xml.GetWidget("LeftButton");
				leftButton.Toggled += delegate(object sender, EventArgs e) {
					leftToRight = true;
					
					Gtk.Label inputTabLabel = (Gtk.Label) nb.GetTabLabel(nb.GetNthPage(0));
					inputTabLabel.Text = "Input (" 
						+ (leftToRight ? teh.getLeftLanguageName() : teh.getRightLanguageName())
						+ ")";
					
					Gtk.Label outputTabLabel = (Gtk.Label) nb.GetTabLabel(nb.GetNthPage(1));
					outputTabLabel.Text = "Output (" 
						+ (leftToRight ? teh.getRightLanguageName() : teh.getLeftLanguageName())
						+ ")";
				};
				
				Gtk.RadioButton rightButton = (Gtk.RadioButton) xml.GetWidget("RightButton");
				rightButton.Toggled += delegate(object sender, EventArgs e) {
					leftToRight = false;
					
					Gtk.Label inputTabLabel = (Gtk.Label) nb.GetTabLabel(nb.GetNthPage(0));
					inputTabLabel.Text = "Input (" 
						+ (leftToRight ? teh.getLeftLanguageName() : teh.getRightLanguageName())
						+ ")";
					
					Gtk.Label outputTabLabel = (Gtk.Label) nb.GetTabLabel(nb.GetNthPage(1));
					outputTabLabel.Text = "Output (" 
						+ (leftToRight ? teh.getRightLanguageName() : teh.getLeftLanguageName())
						+ ")";
				};
				
				Gtk.Entry input = (Gtk.Entry) xml.GetWidget("Input");
				input.KeyReleaseEvent += delegate(object _o, KeyReleaseEventArgs _args) {
					if(_args.Event.Key == Gdk.Key.Return && input.Text.Length > 0)
					{
						string translated = teh.translate(input.Text, leftToRight ? TranslateDirection.LeftToRight : TranslateDirection.RightToLeft);
						inputView.Buffer.Text += input.Text + Environment.NewLine;
						outputView.Buffer.Text += translated + Environment.NewLine;
						input.Text = "";
					}
				};
				
				Gtk.Button send = (Gtk.Button) xml.GetWidget("Send");
				send.Pressed += delegate(object sender, EventArgs e) {
					string translated = teh.translate(input.Text, leftToRight ? TranslateDirection.LeftToRight : TranslateDirection.RightToLeft);
					inputView.Buffer.Text += input.Text + Environment.NewLine;
					outputView.Buffer.Text += translated + Environment.NewLine;
					input.Text = "";
				};
				
				input.Sensitive = false;
				send.Sensitive = false;
				input.IsEditable = false;
				
				Gtk.MenuItem open = (Gtk.MenuItem) xml.GetWidget("Open");
				open.Activated += delegate(object sender, EventArgs e) {
					int retval = fcd.Run();
					
					fcd.Hide();
					
					if((int) Gtk.ResponseType.Accept != retval)
					{
						return;
					}

					if(teh != null)
					{
						teh.disconnectFromLog();
						teh = null;
					}
					teh = loadFile(fcd.Filename);
					teh.connectToLog();
					
					if(!monitor.IsAlive)
					{
						monitor.Start();
					}
					else
					{
						monitor.Abort();
						monitor = new System.Threading.Thread(MainClass.ConsoleEater);
						monitor.Start();
					}
				
					teh.reparse();
					
					input.IsEditable = true;
					input.Sensitive = true;
					send.Sensitive = true;
					
					Gtk.Label inputTabLabel = (Gtk.Label) nb.GetTabLabel(nb.GetNthPage(0));
					inputTabLabel.Text = "Input (" 
						+ (leftToRight ? teh.getLeftLanguageName() : teh.getRightLanguageName())
						+ ")";
					
					Gtk.Label outputTabLabel = (Gtk.Label) nb.GetTabLabel(nb.GetNthPage(1));
					outputTabLabel.Text = "Output (" 
						+ (leftToRight ? teh.getRightLanguageName() : teh.getLeftLanguageName())
						+ ")";
					
					leftButton.Label = teh.getLeftLanguageName() + "-to-" + teh.getRightLanguageName();
					rightButton.Label = teh.getRightLanguageName() + "-to-" + teh.getLeftLanguageName();
				};
				
				mainwindow.Show();
			}
			
			Application.Run ();
			if(monitor != null && monitor.IsAlive)
			{
				monitor.Join(500);
				monitor.Abort();
			}
		}
		
		static void ConsoleEater()
		{   
			while(true)
			{
				try
				{
					if(teh == null)
					{
						System.Threading.Thread.Sleep(1000);
					}
					else
					{
						string line = teh.takeLogLine();
						Application.Invoke(delegate {
	        				debugView.Buffer.Text += line + Environment.NewLine;
	    				});
					}
				}
				catch(ThreadAbortException tae)
				{
					return;	
				}
			}
		} 
		
		private static Tehthu loadFile(string ff)
		{
			Tehthu t = new Tehthu(ff);
			return t;
		}
	}
}

