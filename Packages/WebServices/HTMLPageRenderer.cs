﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

using CefSharp;
using CefSharp.OffScreen;

namespace WebServices {

// TODO:
//	• Use schemes to handle local files: https://github.com/cefsharp/CefSharp/wiki/General-Usage#scheme-handler
//	• Example: Capture Full Page Using Scrolling https://github.com/WildGenie/OSIRTv2/blob/3e60d3ce908a1d25a7b4633dc9afdd53256cbb4f/OSIRT/Browser/MainBrowser.cs#L300
//	• Download element rectangles! => Use JS

	/// <summary>
	/// Class wrapping CEF Sharp (Chromium Embedded Framework, .Net wrapper version) to render web pages in an offscreen bitmap
	/// https://github.com/cefsharp/CefSharp/wiki/General-Usage
	/// </summary>
	public class HTMLPageRenderer : IDisposable {

		#region NESTED TYPES

		class LifeSpanHandler : ILifeSpanHandler {
			public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser) {
				return false;
			}

			public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser) {
			}

			public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser) {
			}

			public bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser) {
				// See https://github.com/cefsharp/CefSharp/wiki/General-Usage#popups
				newBrowser = null;
				return true;
			}
		}

// 		class HTMLSourceReader : CefSharp.IStringVisitor {
// 			public string	m_HTMLContent = null;
// 			public HTMLSourceReader() {
// 			}
// 			public void Visit( string _str ) {
// 				m_HTMLContent = _str;
// 			}
// 
// 			public void Dispose() {
// 			}
// 		}

		/// <summary>
		/// Used to notify the HTML source is available
		/// </summary>
		/// <param name="_pageTitle">HTML Document's title</param>
		/// <param name="_HTMLContent">HTML source</param>
		/// <param name="_DOMElements">The XML document describing the DOM elements present in the returned web page</param>
		public delegate void	WebPageSourceAvailable( string _pageTitle, string _HTMLContent, XmlDocument _DOMElements );

		/// <summary>
		/// Used to notify a new piece of the web page is available in the form of a rendering
		/// </summary>
		/// <param name="_imageIndex">Index of the piece of image that is available</param>
		/// <param name="_imageWebPage">The piece of rendering of the web page</param>
		public delegate void	WebPageRendered( uint _imageIndex, ImageUtility.ImageFile _imageWebPage );

		/// <summary>
		/// Used to notify the web page was successfully loaded
		/// </summary>
		public delegate void	WebPageSuccess();

		/// <summary>
		/// Used to notify of an error in rendering
		/// </summary>
		/// <param name="_errorCode"></param>
		/// <param name="_errorText"></param>
		public delegate void	WebPageErrorOccurred( int _errorCode, string _errorText );

		public enum LOG_TYPE {
			INFO,
			WARNING,
			ERROR,
			DEBUG,
		}

		public delegate void	LogDelegate( LOG_TYPE _type, string _text );

		#endregion

		#region FIELDS

		private int					m_Time_ms_StablePage = 5000;			// Page is deemed stable if no event have been received for 5s

		private int					m_TimeOut_ms_JavascriptNoRender = 1000;	// Default timeout after 1s of a JS command that doesn't trigger a new rendering
		private int					m_TimeOut_ms_PageRender = 30000;		// Default timeout after 30s for a page rendering
		private int					m_TimeOut_ms_Screenshot = 1000;			// Default timeout after 1s for a screenshot

		private ChromiumWebBrowser	m_browser = null;
//		public Timer				m_timer = new Timer() { Enabled = false, Interval = 1000 };

// 		public HostHandler host;
// 		private DownloadHandler dHandler;
// 		private ContextMenuHandler mHandler;
// 		private LifeSpanHandler lHandler;
// 		private KeyboardHandler kHandler;
// 		private RequestHandler rHandler;

		private string					m_URL;
		private int						m_maxScreenshotsCount;

		private WebPageSourceAvailable	m_pageSourceAvailable;
		private WebPageRendered			m_pageRendered;
		private WebPageSuccess			m_pageSuccess;
		private WebPageErrorOccurred	m_pageError;

		private LogDelegate				m_logDelegate;

		#endregion

		#region METHODS

		public HTMLPageRenderer( string _URL, int _browserViewportWidth, int _browserViewportHeight, int _maxScreenshotsCount, WebPageSourceAvailable _pageSourceAvailable, WebPageRendered _pageRendered, WebPageSuccess _pageSuccess, WebPageErrorOccurred _pageError, LogDelegate _logDelegate ) {

//Main( null );

			m_URL = _URL;
			m_maxScreenshotsCount = _maxScreenshotsCount;
			m_pageSourceAvailable = _pageSourceAvailable;
			m_pageRendered = _pageRendered;
			m_pageSuccess = _pageSuccess;
			m_pageError = _pageError;
			m_logDelegate = _logDelegate != null ? _logDelegate : DefaultLogger;

			if ( !Cef.IsInitialized ) {
				InitChromium();
			}

			// https://github.com/cefsharp/CefSharp/wiki/General-Usage#cefsettings-and-browsersettings
			BrowserSettings	browserSettings = new BrowserSettings();

// 			dHandler = new DownloadHandler(this);
// 			lHandler = new LifeSpanHandler(this);
// 			mHandler = new ContextMenuHandler(this);
// 			kHandler = new KeyboardHandler(this);
// 			rHandler = new RequestHandler(this);
// 
// 			InitDownloads();
// 
// 			host = new HostHandler(this);

//			m_timer.Tick += timer_Tick;

			m_browser = new ChromiumWebBrowser( "", browserSettings );
			m_browser.LifeSpanHandler = new LifeSpanHandler();

			// https://github.com/cefsharp/CefSharp/wiki/General-Usage#handlers
			m_browser.LoadError += browser_LoadError;
			m_browser.LoadingStateChanged += browser_LoadingStateChanged;
			m_browser.FrameLoadStart += browser_FrameLoadStart;
			m_browser.FrameLoadEnd += browser_FrameLoadEnd;

 			if ( _browserViewportHeight == 0 )
 				_browserViewportHeight = (int) (_browserViewportWidth * 1.6180339887498948482045868343656);

			m_browser.Size = new System.Drawing.Size( _browserViewportWidth, _browserViewportHeight );

			m_browser.BrowserInitialized += browser_BrowserInitialized;
		}

// 		private void timer_Tick(object sender, EventArgs e) {
// Log( LOG_TYPE.DEBUG, "timer_Tick()" );
// 
// 			m_timer.Stop();	// Prevent any further tick
// 
// // 			m_browser.LoadError -= browser_LoadError;
// // 			m_browser.LoadingStateChanged -= browser_LoadingStateChanged;
// // 			m_browser.FrameLoadEnd -= browser_FrameLoadEnd;
// 
// 			// Raise a "stable" flag once dust seems to have settled for a moment...
// 			m_pageStable = true;
// 		}

		private void browser_BrowserInitialized(object sender, EventArgs e) {
Log( LOG_TYPE.DEBUG, "browser_BrowserInitialized" );

			// No event have been registered yet
			m_hasPageEvents = false;

			// Start actual page loading
			m_browser.Load( m_URL );

			// Execute waiting task
//			var	T = ExecuteTaskOrTimeOut( WaitForPageRendered(), m_TimeOut_ms_PageRender );
			Task	T = new Task( WaitForPageRendered );
					T.Start();
		}

		private void browser_LoadError(object sender, LoadErrorEventArgs e) {
Log( LOG_TYPE.DEBUG, "browser_LoadError: " + e.ErrorText );

			switch ( (uint) e.ErrorCode ) {
				case 0xffffffe5U:
					return;	// Ignore...
			}

			m_pageError( (int) e.ErrorCode, e.ErrorText );
		}

		private void browser_FrameLoadStart(object sender, FrameLoadStartEventArgs e) {
			RegisterPageEvent( "browser_FrameLoadStart" );
		}
		
		private void browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e) {
			RegisterPageEvent( "browser_FrameLoadEnd" );
		}

		private void browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e) {
// 			if ( e.IsLoading )
// 				return;	// Still loading...

			RegisterPageEvent( "browser_LoadingStateChanged (" + (e.IsLoading ? "loading" : "finished") + ")" );
		}

		bool		m_hasPageEvents = false;
 		DateTime	m_lastPageEvent;
		void	RegisterPageEvent( string _eventType ) {
			m_lastPageEvent = DateTime.Now;
			m_hasPageEvents = true;
Log( LOG_TYPE.DEBUG, _eventType );
		}

		/// <summary>
		/// Asynchronous task that will wait for the page to be stable (i.e. all elements have been loaded for some time) before accessing the DOM and taking screenshots
		/// </summary>
		async void	WaitForPageRendered() {
			// Wait until the page is stable a first time...
			await WaitForStablePage( "WaitForPageRendered() => Wait before qurying content" );

			// First query the HTML source code and DOM content
			await QueryContent();

			// Ask for the page's height (not always reliable, especially on infinite scrolling feeds like facebook or twitter!)
			JavascriptResponse	JSResult = await ExecuteJS( "(function() { var body = document.body, html = document.documentElement; return Math.max( body.scrollHeight, body.offsetHeight, html.clientHeight, html.scrollHeight, html.offsetHeight ); } )();" );
			if ( JSResult.Result == null ) {
Log( LOG_TYPE.DEBUG, "JS scrollHeight returned null => 2nd call" );
				JSResult = await ExecuteJS( "(function() { var body = document.body; return Math.max( body.scrollHeight, body.offsetHeight ); } )();" );
				if ( JSResult.Result == null ) {
Log( LOG_TYPE.DEBUG, "JS scrollHeight returned null => 3rd call" );
					JSResult = await ExecuteJS( "(function() { var html = document.documentElement; return Math.max( html.clientHeight, html.scrollHeight, html.offsetHeight ); } )();" );
					if ( JSResult.Result == null ) {
Log( LOG_TYPE.ERROR, "JS scrollHeight returned null => Exception!" );
						throw new Exception( "None of the 3 attempts at querying page height was successful!" );
					}
				}
			}
			int	scrollHeight = (int) JSResult.Result;

			// Perform as many screenshots as necessary to capture the entire page
			int	viewportHeight = m_browser.Size.Height;
			int	screenshotsCount = (int) Math.Ceiling( (double) scrollHeight / viewportHeight );
Log( LOG_TYPE.DEBUG, "Page scroll height = " + scrollHeight + " - Screenshots Count = " + screenshotsCount );

			await DoScreenshots( screenshotsCount );
		}

		/// <summary>
		/// Reads back HTML content and do a screenshot
		/// </summary>
		/// <returns></returns>
		async Task	QueryContent() {
			try {
Log( LOG_TYPE.DEBUG, "QueryContent for " + m_URL );

				// From Line 162 https://github.com/WildGenie/OSIRTv2/blob/3e60d3ce908a1d25a7b4633dc9afdd53256cbb4f/OSIRT/Browser/MainBrowser.cs#L300
				string	source = await m_browser.GetBrowser().MainFrame.GetSourceAsync();
				if ( source == null )
					throw new Exception( "Failed to retrieve HTML source!" );

Log( LOG_TYPE.DEBUG, "QueryContent() => Retrieved HTML code " + (source.Length < 100 ? source : source.Remove( 100 )) );

				JavascriptResponse	JSResult = await ExecuteJS( "(function() { return document.title; } )();" );
				string	pageTitle = JSResult.Result as string;

// @TODO: Parse DOM!
Log( LOG_TYPE.WARNING, "QueryContent() => @TODO: Parse DOM!" );

				// Notify source is ready
				m_pageSourceAvailable( pageTitle, source, null );

			} catch ( Exception _e ) {
				m_pageError( -1, "An error occurred while attempting to retrieve HTML source for URL \"" + m_URL + "\": \r\n" + _e.Message );
			}
		}

		/// <summary>
		/// Do multiple screenshots to capture the entire page
		/// </summary>
		/// <returns></returns>
		async Task	DoScreenshots( int _scrollsCount ) {
			_scrollsCount = Math.Min( m_maxScreenshotsCount, _scrollsCount );

			try {
				// Code from https://github.com/WildGenie/OSIRTv2/blob/3e60d3ce908a1d25a7b4633dc9afdd53256cbb4f/OSIRT/Browser/MainBrowser.cs#L300
//				await m_browser.GetBrowser().MainFrame.EvaluateScriptAsync("(function() { document.documentElement.style.overflow = 'hidden'; })();");
//				await ExecuteTaskOrTimeOut( m_browser.GetBrowser().MainFrame.EvaluateScriptAsync( "(function() { document.documentElement.style.overflow = 'hidden'; })();" ), m_TimeOut_ms_JavascriptNoRender );
				await ExecuteJS( "(function() { document.documentElement.style.overflow = 'hidden'; })();" );

				uint	viewportHeight = (uint) m_browser.Size.Height;
				for ( uint scrollIndex=0; scrollIndex < _scrollsCount; scrollIndex++ ) {

					try {
						//////////////////////////////////////////////////////////////////////////
						/// Request a screenshot
Log( LOG_TYPE.DEBUG, "DoScreenshots() => Requesting screenshot {0}", scrollIndex );

// 						Task<System.Drawing.Bitmap>	task = m_browser.ScreenshotAsync();
// 						if ( (await Task.WhenAny( task, Task.Delay( m_TimeOut_ms_PageRender ) )) == task ) {

 						Task<System.Drawing.Bitmap>	task = (await ExecuteTaskOrTimeOut( m_browser.ScreenshotAsync(), m_TimeOut_ms_Screenshot, "m_browser.ScreenshotAsync()" )) as Task<System.Drawing.Bitmap>;

Log( LOG_TYPE.DEBUG, "DoScreenshots() => Retrieved web page image screenshot {0} / {1}", 1+scrollIndex, _scrollsCount );

						try {
							ImageUtility.ImageFile image = new ImageUtility.ImageFile( task.Result, new ImageUtility.ColorProfile( ImageUtility.ColorProfile.STANDARD_PROFILE.sRGB ) );
							m_pageRendered( scrollIndex, image );
						} catch ( Exception _e ) {
							throw new Exception( "Failed to create image from web page bitmap: \r\n" + _e.Message, _e );
						} finally {
							task.Result.Dispose();	// Always dispose of the bitmap anyway!
						}

						//////////////////////////////////////////////////////////////////////////
						/// Scroll down the page
						if ( scrollIndex < _scrollsCount-1 ) {
Log( LOG_TYPE.DEBUG, "DoScreenshots() => Requesting scrolling... (should retrigger rendering)" );

// 							// Mark the page as "unstable" and scroll down until we reach the bottom (if it exists, or until we reach the specified maximum amount of authorized screenshots)
// 							m_hasPageEvents = false;

//							await m_browser.GetBrowser().MainFrame.EvaluateScriptAsync("(function() { window.scroll(0," + ((scrollIndex+1) * viewportHeight) + "); })();");
							await ExecuteJS( "(function() { window.scroll(0," + ((scrollIndex+1) * viewportHeight) + "); })();" );

// 							// Wait for the page to stabilize (i.e. the timer hasn't been reset for some time, indicating most elements should be ready)
// 							await WaitForStablePage( "DoScreenshots() => Wait for scrolling..." );

Log( LOG_TYPE.DEBUG, "DoScreenshots() => Scrolling done!" );
						}

					} catch ( TimeoutException _e ) {
Log( LOG_TYPE.ERROR, "DoScreenshots() => TIMEOUT EXCEPTION! " + _e.Message );
//						throw new Exception( "Page rendering timed out" );
//m_pageError()
					} catch ( Exception _e ) {
Log( LOG_TYPE.ERROR, "DoScreenshots() => EXCEPTION! " + _e.Message );
					}
				}

				// Notify the page was successfully loaded
				m_pageSuccess();

			} catch ( Exception _e ) {
				m_pageError( -1, "An error occurred while attempting to render a page screenshot for URL \"" + m_URL + "\": \r\n" + _e.Message );
			}
		}

		/// <summary>
		/// Executes a task for a given amount of time before it times out
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="_task"></param>
		/// <param name="_timeOut_ms"></param>
		/// <returns></returns>
		async Task<Task>	ExecuteTaskOrTimeOut< T >( T _task, int _timeOut_ms, string _timeOutMessage ) where T : Task {
			if ( (await Task.WhenAny( _task, Task.Delay( _timeOut_ms ) )) != _task ) {
//				_task.Dispose();
				throw new TimeoutException( _timeOutMessage );
			}

			return _task;
		}

		async Task<JavascriptResponse>	ExecuteJS( string _JS ) {
			Task<JavascriptResponse>	task = (await ExecuteTaskOrTimeOut( m_browser.GetBrowser().MainFrame.EvaluateScriptAsync( _JS, null ), m_TimeOut_ms_JavascriptNoRender, "EvaluateScriptAsync " + _JS )) as Task<JavascriptResponse>;
			return task.Result;
		}

		async Task	AsyncWaitForStablePage( string _waiter ) {
			const int	MAX_COUNTER = 100;

			int	counter = 0;
			while ( counter < MAX_COUNTER ) {
Log( LOG_TYPE.DEBUG, "AsyncWaitForStablePage( {0} ) => Waiting {1}", _waiter, counter++ );

				double	elapsedTimeSinceLastPageEvent = m_hasPageEvents ? (DateTime.Now - m_lastPageEvent).TotalMilliseconds : 0;
				if ( elapsedTimeSinceLastPageEvent > m_Time_ms_StablePage )
					return;	// Page seems stable enough...

//				Application.DoEvents();
				await Task.Delay( 250 );  // We do need these delays. Some pages, like facebook, may need to load viewport content.
			}

Log( LOG_TYPE.DEBUG, "AsyncWaitForStablePage( {0} ) => Exiting after {1} loops!", _waiter, counter );
		}

		async Task	WaitForStablePage( string _waiter ) {
			await ExecuteTaskOrTimeOut( AsyncWaitForStablePage( _waiter ), m_TimeOut_ms_PageRender, "AsyncWaitForStablePage()" );
		}

		public void Dispose() {
			m_browser.Dispose();
		}

		#region Static CEF Init/Exit

		// https://github.com/cefsharp/CefSharp/wiki/General-Usage#initialize-and-shutdown

		public static void	InitChromium() {
			// We're going to manually call Cef.Shutdown
            CefSharpSettings.ShutdownOnExit = false;

			CefSettings	settings = new CefSettings();
 			Cef.Initialize( settings, performDependencyCheck: true, browserProcessHandler: null );
		}

		public static void	ExitChromium() {
			Cef.Shutdown();
		}

		#endregion

		public void	Log( LOG_TYPE _type, string _text, params object[] _arguments ) {
			_text = string.Format( _text, _arguments );
			m_logDelegate( _type, _text );
		}

		public void	DefaultLogger( LOG_TYPE _type, string _text ) {
			switch ( _type ) {
				case LOG_TYPE.WARNING:	_text = "<WARNING> " + _text; break;
				case LOG_TYPE.ERROR:	_text = "<ERROR> " + _text; break;
				case LOG_TYPE.DEBUG:	_text = "<DEBUG> " + _text; break;
			}

System.Diagnostics.Debug.WriteLine( _text );
		}

		#endregion
	}
}
