﻿//
// SearchBar.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using AppKit;
using Foundation;
using CoreGraphics;
using Gtk;
using MonoDevelop.Core;

using MonoDevelop.Ide;
using Xwt.Mac;

namespace MonoDevelop.MacIntegration.MainToolbar
{
	[Register]
	class SearchBar : NSSearchField
	{
		bool debugSearchbar;
		internal Widget gtkWidget;
		internal event EventHandler<Xwt.KeyEventArgs> KeyPressed;
		internal event EventHandler LostFocus;
		internal event EventHandler SelectionActivated;
		public event EventHandler GainedFocus;

		// To only draw the border, NSSearchFieldCell needs to be subclassed. Unfortunately this stops the 
		// animation on activation working. I suspect this is implemented inside the NSSearchField rather
		// than the NSSearchFieldCell which can't do animation.
		class DarkSkinSearchFieldCell : NSSearchFieldCell
		{
			public override void DrawWithFrame (CGRect cellFrame, NSView inView)
			{
				if (IdeApp.Preferences.UserInterfaceSkin == Skin.Dark) {
					var inset = cellFrame.Inset (0.25f, 0.25f);
					if (!ShowsFirstResponder) {
						var path = NSBezierPath.FromRoundedRect (inset, 5, 5);
						path.LineWidth = 0.5f;

						NSColor.FromRgba (0.56f, 0.56f, 0.56f, 1f).SetStroke ();
						path.Stroke ();
					}

					// Can't just call base.DrawInteriorWithFrame because it draws the placeholder text
					// with a strange emboss effect when it the view is not first responder.
					// Again, probably because the NSSearchField handles the not first responder state itself
					// rather than using NSSearchFieldCell
					//base.DrawInteriorWithFrame (inset, inView);

					// So instead, draw the various extra cells and text in the correct places
					SearchButtonCell.DrawWithFrame (SearchButtonRectForBounds (inset), inView);

					if (!ShowsFirstResponder) {
						PlaceholderAttributedString.DrawInRect (SearchTextRectForBounds (inset));
					}

					if (!string.IsNullOrEmpty (StringValue)) {
						CancelButtonCell.DrawWithFrame (CancelButtonRectForBounds (inset), inView);
					}
				} else {
					base.DrawWithFrame (cellFrame, inView);
				}
			}

			// This is the rect for the placeholder text, not the text field entry
			public override CGRect SearchTextRectForBounds (CGRect rect)
			{
				if (ShowsFirstResponder) {
					rect = new CGRect (rect.X + 26, 0, rect.Width - 52, 22);
				} else {
					rect = new CGRect (rect.X + 28, 3, rect.Width - 56, 22);
				}

				return rect;
			}

			// The rect for the search icon
			public override CGRect SearchButtonRectForBounds (CGRect rect)
			{
				rect = new CGRect (0, -1, 26, rect.Height);
				return rect;
			}

			// The rect for the cancel button
			public override CGRect CancelButtonRectForBounds (CGRect rect)
			{
				rect = new CGRect (rect.X + rect.Width - 26.0, -1, 26, 22);

				return rect;
			}

			// When customising the NSCell these are the methods which determine
			// where the editing and selecting text appears
			public override void EditWithFrame (CGRect aRect, NSView inView, NSText editor, NSObject delegateObject, NSEvent theEvent)
			{
				aRect = new CGRect (aRect.X, aRect.Y + 10, aRect.Width - 66, aRect.Height);
				base.EditWithFrame (aRect, inView, editor, delegateObject, theEvent);
			}

			public override void SelectWithFrame (CGRect aRect, NSView inView, NSText editor, NSObject delegateObject, nint selStart, nint selLength)
			{
				nfloat xOffset = 0;
				if (IdeApp.Preferences.UserInterfaceSkin == Skin.Dark) {
					xOffset = -1.5f;
				}
				aRect = new CGRect (aRect.X + xOffset, aRect.Y, aRect.Width, aRect.Height);
				base.SelectWithFrame (aRect, inView, editor, delegateObject, selStart, selLength);
			}
		}

		public SearchBar ()
		{
			Cell = new DarkSkinSearchFieldCell ();

			Initialize ();
			var debugFilePath = System.IO.Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), ".xs-searchbar-debug");
			debugSearchbar = System.IO.File.Exists (debugFilePath);

			Ide.Gui.Styles.Changed +=  (o, e) => UpdateLayout ();
			UpdateLayout ();
		}

		void UpdateLayout ()
		{
			/*
			if (IdeApp.Preferences.UserInterfaceSkin == Skin.Dark) {
				Bezeled = true;
			} else {
				BezelStyle = NSTextFieldBezelStyle.Rounded;
				Bezeled = true;
			}
*/
			Bezeled = true;
			BezelStyle = NSTextFieldBezelStyle.Rounded;
			Editable = true;
			Cell.Scrollable = true;
			Selectable = true;

			PlaceholderAttributedString = new NSAttributedString ("Search", foregroundColor: NSColor.FromRgba (0.63f, 0.63f, 0.63f, 1.0f));
		}

		internal void LogMessage (string message)
		{
			if (!debugSearchbar)
				return;

			LoggingService.LogInfo (message);
		}

		void Initialize ()
		{
			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidResignKeyNotification, notification => Runtime.RunInMainThread (() => {
				var other = (NSWindow)notification.Object;

				LogMessage ($"Lost focus from resign key: {other.DebugDescription}.");
				if (notification.Object == Window) {
					if (LostFocus != null)
						LostFocus (this, null);
				}
			}));
			NSNotificationCenter.DefaultCenter.AddObserver (NSWindow.DidResizeNotification, notification => Runtime.RunInMainThread (() => {
				var other = (NSWindow)notification.Object;
				LogMessage ($"Lost focus from resize: {other.DebugDescription}.");
				if (notification.Object == Window) {
					if (LostFocus != null)
						LostFocus (this, null);
				}
			}));
		}

		bool SendKeyPressed (Xwt.KeyEventArgs kargs)
		{
			if (KeyPressed != null)
				KeyPressed (this, kargs);

			LogMessage ($"KeyPressed with Handled {kargs.Handled}");
			return kargs.Handled;
		}

		public override bool PerformKeyEquivalent (NSEvent theEvent)
		{
			var popupHandled = SendKeyPressed (theEvent.ToXwtKeyEventArgs ());
			LogMessage ($"Popup handled {popupHandled}");
			if (popupHandled)
				return true;
			var baseHandled = base.PerformKeyEquivalent (theEvent);;
			LogMessage ($"Base handled {baseHandled}");
			LogMessage ($"First Reponder {NSApplication.SharedApplication?.KeyWindow?.FirstResponder}");
			LogMessage ($"Refuses First Responder {RefusesFirstResponder}");
			LogMessage ($"Editor chain {CurrentEditor}");
			return baseHandled;
		}

		public override void DidEndEditing (NSNotification notification)
		{
			base.DidEndEditing (notification);

			LogMessage ("Did end editing");

			nint value = ((NSNumber)notification.UserInfo.ValueForKey ((NSString)"NSTextMovement")).LongValue;
			if (value == (nint)(long)NSTextMovement.Tab) {
				LogMessage ("Tab movement");
				SelectText (this);
				return;
			}

			if (value == (nint)(long)NSTextMovement.Return) {
				LogMessage ("Activated by enter");
				if (SelectionActivated != null)
					SelectionActivated (this, null);
				return;
			}

			LogMessage ($"Got NSTextMovement: {value}");

			// This means we've reached a focus loss event.
			var replacedWith = notification.UserInfo.ValueForKey ((NSString)"_NSFirstResponderReplacingFieldEditor");
			if (replacedWith != this && LostFocus != null) {
				if (replacedWith != null)
					LogMessage ($"Mouse focus loss to {replacedWith.DebugDescription}");
				LostFocus (this, null);
			}
		}

		public override void ViewDidMoveToWindow ()
		{
			base.ViewDidMoveToWindow ();

			LogMessage ("View moved to parent window");
			// Needs to be grabbed after it's parented.
			gtkWidget = Components.Mac.GtkMacInterop.NSViewToGtkWidget (this);
		}

		public override bool BecomeFirstResponder ()
		{
			LogMessage ("Becoming first responder");
			bool firstResponder = base.BecomeFirstResponder ();
			if (firstResponder)
				Focus ();

			return firstResponder;
		}

		public void Focus ()
		{
			LogMessage ("Focused");
			if (GainedFocus != null)
				GainedFocus (this, EventArgs.Empty);
		}
	}
}

