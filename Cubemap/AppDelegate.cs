﻿using AppKit;
using Foundation;

namespace Cubemap
{
    [Register("AppDelegate")]
    public class AppDelegate : NSApplicationDelegate
    {
        public AppDelegate()
        {
        }

		public override bool ApplicationShouldTerminateAfterLastWindowClosed(NSApplication sender)
		{
            return true;
		}

		public override void DidFinishLaunching(NSNotification notification)
        {
            // Insert code here to initialize your application
        }

        public override void WillTerminate(NSNotification notification)
        {
            // Insert code here to tear down your application
        }
    }
}
