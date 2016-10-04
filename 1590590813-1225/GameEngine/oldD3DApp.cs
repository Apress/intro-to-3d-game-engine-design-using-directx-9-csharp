//-----------------------------------------------------------------------------
// File: D3DApp.cs
//
// Desc: Application class for the Direct3D samples framework library.
//
// Copyright (c) 2001-2002 Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
using System;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Direct3D=Microsoft.DirectX.Direct3D;

#region Enums for D3D Applications
public enum AppMsgType 
{ 
	None, 
	ErrorAppMustExit, 
	WarnSwitchToRef
};

#endregion

public class GraphicsException : System.Exception
{
	private ErrorCode resultCode = ErrorCode.Generic;
	public enum ErrorCode
	{
		Generic,
		NoDirect3D,
		NoWindow,
		NoCompatibleDevices,
		NoWindowableDevices,
		NoHardwareDevice,
		HalNotCompatible,
		NoWindowedHal,
		NoDesktopHal,
		NoHalThisMode,
		MediaNotFound,
		ResizeFailed,
		NullRefDevice,
	}
	public GraphicsException(){}
	public GraphicsException(ErrorCode code)
	{
		resultCode = code;
	}
	public override string Message { 
		get 
		{ 
			string strMsg = null;

			switch( resultCode )
			{
				case ErrorCode.NoDirect3D:
					strMsg = "Could not initialize Direct3D. You may\n";
					strMsg += "want to check that the latest version of\n";
					strMsg += "DirectX is correctly installed on your\n";
					strMsg += "system.  Also make sure that this program\n";
					strMsg += "was compiled with header files that match\n";
					strMsg += "the installed DirectX DLLs.";
					break;

				case ErrorCode.NoCompatibleDevices:
					strMsg = "Could not find any compatible Direct3D\ndevices.";
					break;

				case ErrorCode.NoWindowableDevices:
					strMsg = "This sample cannot run in a desktop\n";
					strMsg += "window with the current display settings.\n";
					strMsg += "Please change your desktop settings to a\n";
					strMsg += "16- or 32-bit display mode and re-run this\n";
					strMsg += "sample.";
					break;

				case ErrorCode.NoHardwareDevice:
					strMsg = "No hariare-accelerated Direct3D devices\n";
					strMsg += "were found.";
					break;

				case ErrorCode.HalNotCompatible:
					strMsg = "This sample requires functionality that is\n";
					strMsg += "not available on your Direct3D hariare\n";
					strMsg += "accelerator.";
					break;

				case ErrorCode.NoWindowedHal:
					strMsg = "Your Direct3D hariare accelerator cannot\n";
					strMsg += "render into a window.\n";
					strMsg += "Press F2 while the app is running to see a\n";
					strMsg += "list of available devices and modes.";
					break;

				case ErrorCode.NoDesktopHal:
					strMsg = "Your Direct3D hariare accelerator cannot\n";
					strMsg += "render into a window with the current\n";
					strMsg += "desktop display settings.\n";
					strMsg += "Press F2 while the app is running to see a\n";
					strMsg += "list of available devices and modes.";
					break;

				case ErrorCode.NoHalThisMode:
					strMsg = "This sample requires functionality that is\n";
					strMsg += "not available on your Direct3D hariare\n";
					strMsg += "accelerator with the current desktop display\n";
					strMsg += "settings.\n";
					strMsg += "Press F2 while the app is running to see a\n";
					strMsg += "list of available devices and modes.";
					break;

				case ErrorCode.MediaNotFound:
					strMsg = "Could not load required media.";
					break;

				case ErrorCode.ResizeFailed:
					strMsg = "Could not reset the Direct3D device.";
					break;

				case ErrorCode.NullRefDevice:
					strMsg = "Warning: Nothing will be rendered.\n";
					strMsg += "The reference rendering device was selected, but your\n";
					strMsg += "computer only has a reduced-functionality reference device\n";
					strMsg += "installed.  Install the DirectX SDK to get the full\n";
					strMsg += "reference device.\n";
					break;

				case (ErrorCode)Direct3D.ErrorCode.OutOfVidMemory:
					strMsg = "Not enough video memory.";
					break;

				default:
					strMsg = "Generic application error. Enable\n";
					strMsg += "debug output for detailed information.";
					break;

			}
			return strMsg;
		} 
	}
}
public class GraphicsSample : System.Windows.Forms.Form, IDisposable
{
	// Menu information
	protected System.Windows.Forms.MainMenu mnuMain;
	protected System.Windows.Forms.MenuItem mnuFile;
	private System.Windows.Forms.MenuItem mnuGo;
	private System.Windows.Forms.MenuItem mnuSingle;
	private System.Windows.Forms.MenuItem mnuBreak1;
	private System.Windows.Forms.MenuItem mnuChange;
	private System.Windows.Forms.MenuItem mnuBreak2;
	private System.Windows.Forms.MenuItem mnuExit;

	//-----------------------------------------------------------------------------
	// Global access to the app (needed for the global WndProc())
	//-----------------------------------------------------------------------------
	public static GraphicsSample ourSample = null;
	public bool m_bTerminate = false;

	protected D3DEnumeration enumerationSettings = new D3DEnumeration();
	protected D3DSettings graphicsSettings = new D3DSettings();
	private bool isDisposed = false;

	private float lastTime = 0.0f;
	private uint frames  = 0;
	private uint appPausedCount = 0;

	// Internal variables for the state of the app
	protected bool windowed;
	protected bool active;
	protected bool ready;
	protected bool hasFocus;

	// Internal variables used for timing
	protected bool frameMoving;
	protected bool singleStep;
	// Main objects used for creating and rendering the 3D scene
	protected PresentParameters presentParams = new PresentParameters();         // Parameters for CreateDevice/Reset
	protected D3D graphicsObject;              // The main D3D object
	protected Device device;        // The D3D rendering device
	protected RenderStates renderState;
	protected SamplerStates sampleState;
	protected TextureStates textureStates;
	private Caps graphicsCaps;           // Caps for the device
	protected Caps Caps { get { return graphicsCaps; } }
	private SurfaceDescription backBufferDesc;   // Surface desc of the backbuffer
	public SurfaceDescription backBuffer { get { return backBufferDesc; } }
	private CreateFlags behavior;     // Indicate sw or hw vertex processing
	protected BehaviorFlags BehaviorFlags { get { return new BehaviorFlags(behavior); } }
	protected System.Drawing.Rectangle windowBoundsRect;    // Saved window bounds for mode switches
	protected System.Drawing.Rectangle clientRect;    // Saved client area size for mode switches

	// Variables for timing
	protected float appTime;             // Current time in seconds
	protected float elapsedTime;      // Time elapsed since last frame
	protected float framePerSecond;              // Instanteous frame rate
	protected string deviceStats;// String to hold D3D device stats
	protected string frameStats; // String to hold frame stats

	private bool deviceLost = false;

	// Overridable variables for the app
	protected string windowTitle;    // Title for the app's window
	private int minDepthBits;    // Minimum number of bits needed in depth buffer
	protected int MinDepthBits { get { return minDepthBits; } set { minDepthBits = value;  enumerationSettings.AppMinDepthBits = value;} }
	private int minStencilBits;  // Minimum number of bits needed in stencil buffer
	protected int MinStencilBits { get { return minStencilBits; } set { minStencilBits = value;  enumerationSettings.AppMinStencilBits = value;} }
	protected bool showCursorWhenFullscreen; // Whether to show cursor when fullscreen
	protected bool clipCursorWhenFullscreen; // Whether to limit cursor pos when fullscreen
	protected bool startFullscreen; // Whether to start up the app in fullscreen mode

	// Overridable functions for the 3D scene created by the app
	protected virtual bool ConfirmDevice(Caps caps, VertexProcessingType vertexProcessingType, Format fmt) { return true; }
	protected virtual void OneTimeSceneInit()                         { /* Do Nothing */ }
	protected virtual void InitDeviceObjects()                        { /* Do Nothing */ }
	protected virtual void RestoreDeviceObjects(System.Object sender, System.EventArgs e)                     { /* Do Nothing */ }
	protected virtual void FrameMove()                                { /* Do Nothing */ }
	protected virtual void Render()                                   { /* Do Nothing */ }
	protected virtual void InvalidateDeviceObjects(System.Object sender, System.EventArgs e)                  { /* Do Nothing */ }
	protected virtual void DeleteDeviceObjects(System.Object sender, System.EventArgs e)                      { /* Do Nothing */ }
	protected virtual void FinalCleanup()                       { /* Do Nothing */ }





	//-----------------------------------------------------------------------------
	// Name: GraphicsSample()
	// Desc: Constructor
	//-----------------------------------------------------------------------------
	public GraphicsSample()
	{
		ourSample = this;

		graphicsObject              = null;
		device        = null;
		active           = false;
		ready            = false;
		hasFocus		= false;
		behavior     = 0;

		frameMoving      = true;
		singleStep       = false;
		framePerSecond              = 0.0f;
		deviceStats = null;
		frameStats  = null;

		this.Text    = "D3D9 Sample";
		this.ClientSize = new System.Drawing.Size(800,600);
		this.KeyPreview = true;

		minDepthBits    = 16;
		minStencilBits  = 0;
		showCursorWhenFullscreen = false;
		startFullscreen = false;

		// When clipCursorWhenFullscreen is TRUE, the cursor is limited to
		// the device window when the app goes fullscreen.  This prevents users
		// from accidentally clicking outside the app window on a multimon system.
		// This flag is turned off by default for debug builds, since it makes 
		// multimon debugging difficult.
		#if (DEBUG)
			clipCursorWhenFullscreen = false;
		#else
			clipCursorWhenFullscreen = true;
		#endif
		InitializeComponent();
	}




	//-----------------------------------------------------------------------------
	// Name: CreateD3DApp()
	// Desc:
	//-----------------------------------------------------------------------------
	public bool CreateD3DApp()
	{
		// Create the Direct3D object
		graphicsObject = new D3D();
		if( graphicsObject == null )
		{
			DisplayErrorMsg( new GraphicsException(GraphicsException.ErrorCode.NoDirect3D), AppMsgType.ErrorAppMustExit );
			return false;
		}

		enumerationSettings.D3D = graphicsObject;
		enumerationSettings.ConfirmDeviceCallback = new D3DEnumeration.ConfirmDeviceCallbackType(this.ConfirmDevice);
		enumerationSettings.Enumerate();

		if (this.Cursor == null)
		{
			// Set up a default cursor
			this.Cursor = System.Windows.Forms.Cursors.Default;
		}
		// Unless a substitute hWnd has been specified, create a window to
		// render into
		this.Menu = mnuMain;

		// Save window properties
		windowBoundsRect = new System.Drawing.Rectangle(this.Location, this.Size);
		clientRect = this.ClientRectangle;

		ChooseInitialD3DSettings();

		// Initialize the application timer
		DXUtil.Timer( TIMER.START );

		try
		{
			// Initialize the 3D environment for the app
			Initialize3DEnvironment();
			// Initialize the app's custom scene stuff
			OneTimeSceneInit();
		}
		catch (GraphicsException d3de)
		{
			DisplayErrorMsg( d3de, AppMsgType.ErrorAppMustExit );
			return false;
		}
		catch
		{
			return false;
		}

		// The app is ready to go
		ready = true;

		return true;
	}



	// Sets up graphicsSettings with best available windowed mode, subject to 
	// the bRequireHAL and bRequireREF constraints.  Returns false if no such
	// mode can be found.
	public bool FindBestWindowedMode(bool bRequireHAL, bool bRequireREF)
	{
		// Get display mode of primary adapter (which is assumed to be where the window 
		// will appear)
		DisplayMode primaryDesktopDisplayMode = graphicsObject.Adapters[0].DisplayMode;

		D3DAdapterInfo bestAdapterInfo = null;
		D3DDeviceInfo bestDeviceInfo = null;
		D3DDeviceCombo bestDeviceCombo = null;

		foreach (D3DAdapterInfo adapterInfo in enumerationSettings.AdapterInfoList)
		{
			foreach (D3DDeviceInfo deviceInfo in adapterInfo.DeviceInfoList)
			{
				if (bRequireHAL && deviceInfo.DevType != DeviceType.Hardware)
					continue;
				if (bRequireREF && deviceInfo.DevType != DeviceType.Reference)
					continue;
				foreach (D3DDeviceCombo deviceCombo in deviceInfo.DeviceComboList)
				{
					bool bAdapterMatchesBB = (deviceCombo.BackBufferFormat == deviceCombo.AdapterFormat);
					if (!deviceCombo.IsWindowed)
						continue;
					if (deviceCombo.AdapterFormat != primaryDesktopDisplayMode.Format)
						continue;
					// If we haven't found a compatible DeviceCombo yet, or if this set
					// is better (because it's a HAL, and/or because formats match better),
					// save it
					if (bestDeviceCombo == null || 
						bestDeviceCombo.DevType != DeviceType.Hardware && deviceInfo.DevType == DeviceType.Hardware ||
						deviceCombo.DevType == DeviceType.Hardware && bAdapterMatchesBB )
					{
						bestAdapterInfo = adapterInfo;
						bestDeviceInfo = deviceInfo;
						bestDeviceCombo = deviceCombo;
						if (deviceInfo.DevType == DeviceType.Hardware && bAdapterMatchesBB)
						{
							// This windowed device combo looks great -- take it
							goto EndWindowedDeviceComboSearch;
						}
						// Otherwise keep looking for a better windowed device combo
					}
				}
			}
		}
	EndWindowedDeviceComboSearch:
		if (bestDeviceCombo == null )
			return false;

		graphicsSettings.Windowed_AdapterInfo = bestAdapterInfo;
		graphicsSettings.Windowed_DeviceInfo = bestDeviceInfo;
		graphicsSettings.Windowed_DeviceCombo = bestDeviceCombo;
		graphicsSettings.IsWindowed = true;
		graphicsSettings.Windowed_DisplayMode = primaryDesktopDisplayMode;
		graphicsSettings.Windowed_Width = clientRect.Right - clientRect.Left;
		graphicsSettings.Windowed_Height = clientRect.Bottom - clientRect.Top;
		if (enumerationSettings.AppUsesDepthBuffer)
			graphicsSettings.Windowed_DepthStencilBufferFormat = (Format)bestDeviceCombo.DepthStencilFormatList[0];
		graphicsSettings.Windowed_MultisampleType = (MultiSampleType)bestDeviceCombo.MultiSampleTypeList[0];
		graphicsSettings.Windowed_MultisampleQuality = 0;
		graphicsSettings.Windowed_VertexProcessingType = (VertexProcessingType)bestDeviceCombo.VertexProcessingTypeList[0];
		graphicsSettings.Windowed_PresentInterval = (PresentInterval)bestDeviceCombo.PresentIntervalList[0];
		return true;
	}




	// Sets up graphicsSettings with best available fullscreen mode, subject to 
	// the bRequireHAL and bRequireREF constraints.  Returns false if no such
	// mode can be found.
	public bool FindBestFullscreenMode(bool bRequireHAL, bool bRequireREF)
	{
		// For fullscreen, default to first HAL DeviceCombo that supports the current desktop 
		// display mode, or any display mode if HAL is not compatible with the desktop mode, or 
		// non-HAL if no HAL is available
		DisplayMode adapterDesktopDisplayMode = new DisplayMode();
		DisplayMode bestAdapterDesktopDisplayMode = new DisplayMode();
		DisplayMode bestDisplayMode = new DisplayMode();
		bestAdapterDesktopDisplayMode.Width = 0;
		bestAdapterDesktopDisplayMode.Height = 0;
		bestAdapterDesktopDisplayMode.Format = 0;
		bestAdapterDesktopDisplayMode.RefreshRate = 0;

		D3DAdapterInfo bestAdapterInfo = null;
		D3DDeviceInfo bestDeviceInfo = null;
		D3DDeviceCombo bestDeviceCombo = null;

		foreach (D3DAdapterInfo adapterInfo in enumerationSettings.AdapterInfoList)
		{
			adapterDesktopDisplayMode = graphicsObject.Adapters[adapterInfo.AdapterOrdinal].DisplayMode;
			foreach (D3DDeviceInfo deviceInfo in adapterInfo.DeviceInfoList)
			{
				if (bRequireHAL && deviceInfo.DevType != DeviceType.Hardware)
					continue;
				if (bRequireREF && deviceInfo.DevType != DeviceType.Reference)
					continue;
				foreach (D3DDeviceCombo deviceCombo in deviceInfo.DeviceComboList)
				{
					bool bAdapterMatchesBB = (deviceCombo.BackBufferFormat == deviceCombo.AdapterFormat);
					bool bAdapterMatchesDesktop = (deviceCombo.AdapterFormat == adapterDesktopDisplayMode.Format);
					if (deviceCombo.IsWindowed)
						continue;
					// If we haven't found a compatible set yet, or if this set
					// is better (because it's a HAL, and/or because formats match better),
					// save it
					if (bestDeviceCombo == null ||
						bestDeviceCombo.DevType != DeviceType.Hardware && deviceInfo.DevType == DeviceType.Hardware ||
						bestDeviceCombo.DevType == DeviceType.Hardware && bestDeviceCombo.AdapterFormat != adapterDesktopDisplayMode.Format && bAdapterMatchesDesktop ||
						bestDeviceCombo.DevType == DeviceType.Hardware && bAdapterMatchesDesktop && bAdapterMatchesBB )
					{
						bestAdapterDesktopDisplayMode = adapterDesktopDisplayMode;
						bestAdapterInfo = adapterInfo;
						bestDeviceInfo = deviceInfo;
						bestDeviceCombo = deviceCombo;
						if (deviceInfo.DevType == DeviceType.Hardware && bAdapterMatchesDesktop && bAdapterMatchesBB)

						{
							// This fullscreen device combo looks great -- take it
							goto EndFullscreenDeviceComboSearch;
						}
						// Otherwise keep looking for a better fullscreen device combo
					}
				}
			}
		}
	EndFullscreenDeviceComboSearch:
		if (bestDeviceCombo == null)
			return false;

		// Need to find a display mode on the best adapter that uses pBestDeviceCombo->AdapterFormat
		// and is as close to bestAdapterDesktopDisplayMode's res as possible
		bestDisplayMode.Width = 0;
		bestDisplayMode.Height = 0;
		bestDisplayMode.Format = 0;
		bestDisplayMode.RefreshRate = 0;
		foreach( DisplayMode displayMode in bestAdapterInfo.DisplayModeList )
		{
			if( displayMode.Format != bestDeviceCombo.AdapterFormat )
				continue;
			if( displayMode.Width == bestAdapterDesktopDisplayMode.Width &&
				displayMode.Height == bestAdapterDesktopDisplayMode.Height && 
				displayMode.RefreshRate == bestAdapterDesktopDisplayMode.RefreshRate )
			{
				// found a perfect match, so stop
				bestDisplayMode = displayMode;
				break;
			}
			else if( displayMode.Width == bestAdapterDesktopDisplayMode.Width &&
				displayMode.Height == bestAdapterDesktopDisplayMode.Height && 
				displayMode.RefreshRate > bestDisplayMode.RefreshRate )
			{
				// refresh rate doesn't match, but width/height match, so keep this
				// and keep looking
				bestDisplayMode = displayMode;
			}
			else if( bestDisplayMode.Width == bestAdapterDesktopDisplayMode.Width )
			{
				// width matches, so keep this and keep looking
				bestDisplayMode = displayMode;
			}
			else if( bestDisplayMode.Width == 0 )
			{
				// we don't have anything better yet, so keep this and keep looking
				bestDisplayMode = displayMode;
			}
		}
		graphicsSettings.Fullscreen_AdapterInfo = bestAdapterInfo;
		graphicsSettings.Fullscreen_DeviceInfo = bestDeviceInfo;
		graphicsSettings.Fullscreen_DeviceCombo = bestDeviceCombo;
		graphicsSettings.IsWindowed = false;
		graphicsSettings.Fullscreen_DisplayMode = bestDisplayMode;
		if (enumerationSettings.AppUsesDepthBuffer)
			graphicsSettings.Fullscreen_DepthStencilBufferFormat = (Format)bestDeviceCombo.DepthStencilFormatList[0];
		graphicsSettings.Fullscreen_MultisampleType = (MultiSampleType)bestDeviceCombo.MultiSampleTypeList[0];
		graphicsSettings.Fullscreen_MultisampleQuality = 0;
		graphicsSettings.Fullscreen_VertexProcessingType = (VertexProcessingType)bestDeviceCombo.VertexProcessingTypeList[0];
		graphicsSettings.Fullscreen_PresentInterval = (PresentInterval)bestDeviceCombo.PresentIntervalList[0];
		return true;
	}

 


	public bool ChooseInitialD3DSettings()
	{
		bool bFoundFullscreen = FindBestFullscreenMode(false, false);
		bool bFoundWindowed = FindBestWindowedMode(false, false);
		if (startFullscreen && bFoundFullscreen)
			graphicsSettings.IsWindowed = false;
		return (bFoundFullscreen || bFoundWindowed);
	}
	/*
//				case WindowMessage.SETCURSOR:
//					// Turn off Windows cursor in fullscreen mode
//					if( active && ready && !windowed )
//					{
//						SetCursor( null );
//						if( showCursorWhenFullscreen )
//							device.ShowCursor( true );
//						return true; // prevent Windows from setting cursor to window class cursor
//					}
//					break;
//
//				case WindowMessage.MOUSEMOVE:
//					if( active && ready && device != null )
//					{
//						Point ptCursor;
//						GetCursorPos( &ptCursor );
//						if( !windowed )
//							ScreenToClient( m_hWnd, &ptCursor );
//						device.SetCursorPosition( ptCursor.x, ptCursor.y, 0L );
//					}
//					break;
	*/
	public void BuildPresentParamsFromSettings()
	{
		presentParams.Windowed               = graphicsSettings.IsWindowed;
		presentParams.BackBufferCount        = 1;
		presentParams.MultiSample        = graphicsSettings.MultisampleType;
		presentParams.MultiSampleQuality     = graphicsSettings.MultisampleQuality;
		presentParams.SwapEffect             = SwapEffect.Discard;
		presentParams.EnableAutoDepthStencil = enumerationSettings.AppUsesDepthBuffer;
		presentParams.AutoDepthStencilFormat = graphicsSettings.DepthStencilBufferFormat;
		presentParams.DeviceWindowHandle          = this.Handle;
		presentParams.Flags                  = 0;
		if( windowed )
		{
			presentParams.BackBufferWidth  = clientRect.Right - clientRect.Left;
			presentParams.BackBufferHeight = clientRect.Bottom - clientRect.Top;
			presentParams.BackBufferFormat = graphicsSettings.DeviceCombo.BackBufferFormat;
			presentParams.FullScreenRefreshRateInHz = 0;
			presentParams.PresentationInterval = PresentInterval.Immediate;
		}
		else
		{
			presentParams.BackBufferWidth  = graphicsSettings.DisplayMode.Width;
			presentParams.BackBufferHeight = graphicsSettings.DisplayMode.Height;
			presentParams.BackBufferFormat = graphicsSettings.DeviceCombo.BackBufferFormat;
			presentParams.FullScreenRefreshRateInHz = graphicsSettings.DisplayMode.RefreshRate;
			presentParams.PresentationInterval = graphicsSettings.PresentInterval;
		}
	}


	//-----------------------------------------------------------------------------
	// Name: Initialize3DEnvironment()
	// Desc:
	//-----------------------------------------------------------------------------
	public void Initialize3DEnvironment()
	{
		D3DAdapterInfo adapterInfo = graphicsSettings.AdapterInfo;
		D3DDeviceInfo deviceInfo = graphicsSettings.DeviceInfo;

		windowed = graphicsSettings.IsWindowed;

		// Prepare window for possible windowed/fullscreen change
		AdjustWindowForChange();

		// Set up the presentation parameters
		BuildPresentParamsFromSettings();

		if (deviceInfo.Caps.PrimitiveMiscCaps.NullReference )
		{
			// Warn user about null ref device that can't render anything
			DisplayErrorMsg( new GraphicsException(GraphicsException.ErrorCode.NullRefDevice), AppMsgType.None );
		}

		CreateFlags createFlags = new CreateFlags();
		if (graphicsSettings.VertexProcessingType == VertexProcessingType.Software)
			createFlags = CreateFlags.SoftwareVertexProcessing;
		else if (graphicsSettings.VertexProcessingType == VertexProcessingType.Mixed)
			createFlags = CreateFlags.MixedVertexProcessing;
		else if (graphicsSettings.VertexProcessingType == VertexProcessingType.Hardware)
			createFlags = CreateFlags.HardwareVertexProcessing;
		else if (graphicsSettings.VertexProcessingType == VertexProcessingType.PureHardware)
		{
			createFlags = CreateFlags.HardwareVertexProcessing | CreateFlags.PureDevice;
		}
		/*
		else
			// TODO: throw exception
		*/
		createFlags |= CreateFlags.MultiThreaded;

		// Create the device
		device = new Device(graphicsObject, graphicsSettings.AdapterOrdinal, graphicsSettings.DevType, 
			this, createFlags, presentParams);

		if( device != null )
		{
			// Cache our local objects
			renderState = device.RenderState;
			sampleState = device.SamplerState;
			textureStates = device.TextureState;
			// When moving from fullscreen to windowed mode, it is important to
			// adjust the window size after recreating the device rather than
			// beforehand to ensure that you get the window size you want.  For
			// example, when switching from 640x480 fullscreen to windowed with
			// a 1000x600 window on a 1024x768 desktop, it is impossible to set
			// the window size to 1000x600 until after the display mode has
			// changed to 1024x768, because windows cannot be larger than the
			// desktop.
			if( windowed )
			{
				// Make sure main window isn't topmost, so error message is visible
				this.Location = new System.Drawing.Point(windowBoundsRect.Left, windowBoundsRect.Top);
				this.Size = new System.Drawing.Size(( windowBoundsRect.Right - windowBoundsRect.Left ), ( windowBoundsRect.Bottom - windowBoundsRect.Top));
			}

			// Store device Caps
			graphicsCaps = device.DeviceCaps;
			behavior = createFlags;

			// Store device description
			if( deviceInfo.DevType == DeviceType.Reference )
				deviceStats = "REF";
			else if( deviceInfo.DevType == DeviceType.Hardware )
				deviceStats = "HAL";
			else if( deviceInfo.DevType == DeviceType.Software )
				deviceStats = "SW";

			BehaviorFlags behaviorFlags = new BehaviorFlags(createFlags);
			if( (behaviorFlags.HardwareVertexProcessing) && 
				(behaviorFlags.PureDevice) )
			{
				if( deviceInfo.DevType == DeviceType.Hardware )
					deviceStats += " (pure hw vp)";
				else
					deviceStats += " (simulated pure hw vp)";
			}
			else if( (behaviorFlags.HardwareVertexProcessing) )
			{
				if( deviceInfo.DevType == DeviceType.Hardware )
					deviceStats  += " (hw vp)";
				else
					deviceStats += " (simulated hw vp)";
			}
			else if( behaviorFlags.MixedVertexProcessing)
			{
				if( deviceInfo.DevType == DeviceType.Hardware )
					deviceStats += " (mixed vp)";
				else
					deviceStats += " (simulated mixed vp)";
			}
			else if( behaviorFlags.SoftwareVertexProcessing )
			{
				deviceStats += " (sw vp)";
			}

			if( deviceInfo.DevType == DeviceType.Hardware )
			{
				deviceStats += ": ";
				deviceStats += adapterInfo.AdapterIdentifier.Description;
			}

			// Store render target surface desc
			Surface BackBuffer = device.GetBackBuffer(0,0, BackBufferType.Mono);
			backBufferDesc = BackBuffer.Description;
			BackBuffer.Dispose();
			BackBuffer = null;

			// Set up the fullscreen cursor
			if( showCursorWhenFullscreen && !windowed )
			{
				System.Windows.Forms.Cursor ourCursor = this.Cursor;
				device.SetCursor(ourCursor.Handle, true);
				device.ShowCursor(true);
			}

			// Confine cursor to fullscreen window
			if( clipCursorWhenFullscreen )
			{
				if (!windowed )
				{
					System.Drawing.Rectangle rcWindow = this.ClientRectangle;
				}
			}

			// Setup the event handlers for our device
			device.DeviceLost += new System.EventHandler(this.InvalidateDeviceObjects);
			device.DeviceReset += new System.EventHandler(this.RestoreDeviceObjects);
			device.Disposing += new System.EventHandler(this.DeleteDeviceObjects);


			// Initialize the app's device-dependent objects
			try
			{
				InitDeviceObjects();
				RestoreDeviceObjects(null, null);
				active = true;
				return;
			}
			catch
			{
				// Cleanup before we try again
				InvalidateDeviceObjects(null, null);
				DeleteDeviceObjects(null, null);
				device.Dispose();
				device = null;
				if (this.Disposing)
					return;
			}
		}

		// If that failed, fall back to the reference rasterizer
		if( deviceInfo.DevType == DeviceType.Hardware )
		{
			if (FindBestWindowedMode(false, true))
			{
				windowed = true;
				// Make sure main window isn't topmost, so error message is visible
				this.Location = new System.Drawing.Point(windowBoundsRect.Left, windowBoundsRect.Top);
				this.Size = new System.Drawing.Size(( windowBoundsRect.Right - windowBoundsRect.Left ), ( windowBoundsRect.Bottom - windowBoundsRect.Top));
				AdjustWindowForChange();

				// Let the user know we are switching from HAL to the reference rasterizer
				DisplayErrorMsg( null, AppMsgType.WarnSwitchToRef);

				Initialize3DEnvironment();
			}
		}
	}




	//-----------------------------------------------------------------------------
	// Name: DisplayErrorMsg()
	// Desc: Displays error messages in a message box
	//-----------------------------------------------------------------------------
	public void DisplayErrorMsg( GraphicsException e, AppMsgType Type )
	{
		string strMsg = null;
		if (e != null)
			strMsg = e.Message;

		if (windowTitle == null)
			windowTitle = "";

		if( AppMsgType.ErrorAppMustExit == Type )
		{
			strMsg  += "\n\nThis sample will now exit.";
			System.Windows.Forms.MessageBox.Show(strMsg, windowTitle, 
				System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);

			// Close the window, which shuts down the app
			if( this.IsHandleCreated )
				this.Dispose( );
		}
		else
		{
			if( AppMsgType.WarnSwitchToRef == Type )
				strMsg = "\n\nSwitching to the reference rasterizer,\n";
				strMsg += "a software device that implements the entire\n";
				strMsg += "Direct3D feature set, but runs very slowly.";

			System.Windows.Forms.MessageBox.Show(strMsg, windowTitle, 
				System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Information);
		}
	}




	//-----------------------------------------------------------------------------
	// Name: Resize3DEnvironment
	// Desc: Resizes the environment
	//-----------------------------------------------------------------------------
	public void Resize3DEnvironment()
	{
		if (presentParams.Windowed)
		{
			// Store the new width/height
			backBufferDesc.Width = this.ClientSize.Width;
			backBufferDesc.Height = this.ClientSize.Height;
		}
		else
		{
			// Store the new width/height
			backBufferDesc.Width = presentParams.BackBufferWidth;
			backBufferDesc.Height = presentParams.BackBufferHeight;
		}

		// Reset the device
		device.Reset(presentParams);

		this.ClientSize = new System.Drawing.Size(presentParams.BackBufferWidth, presentParams.BackBufferHeight);
		// Store render target surface desc
		Surface BackBuffer = device.GetBackBuffer(0,0, BackBufferType.Mono);
		backBufferDesc = BackBuffer.Description;
		BackBuffer.Dispose();
		BackBuffer = null;



		// Set up the fullscreen cursor
		if( showCursorWhenFullscreen && !windowed )
		{
			System.Windows.Forms.Cursor ourCursor = this.Cursor;
			device.SetCursor(ourCursor.Handle, true);
			device.ShowCursor(true);
		}

		// Confine cursor to fullscreen window
		if( clipCursorWhenFullscreen )
		{
			if (!windowed )
			{
				System.Drawing.Rectangle rcWindow = this.ClientRectangle;
			}
		}

		// If the app is paused, trigger the rendering of the current frame
		if( false == frameMoving )
		{
			singleStep = true;
			DXUtil.Timer( TIMER.START );
			DXUtil.Timer( TIMER.STOP );
		}
	}




	//-----------------------------------------------------------------------------
	// Name: ToggleFullScreen()
	// Desc: Called when user toggles between fullscreen mode and windowed mode
	//-----------------------------------------------------------------------------
	public void ToggleFullscreen()
	{
		int AdapterOrdinalOld = graphicsSettings.AdapterOrdinal;
		DeviceType DevTypeOld = graphicsSettings.DevType;

		ready = false;

		// Toggle the windowed state
		windowed = !windowed;
		graphicsSettings.IsWindowed = windowed;

		// Prepare window for windowed/fullscreen change
		AdjustWindowForChange();

		// If AdapterOrdinal and DevType are the same, we can just do a Reset().
		// If they've changed, we need to do a complete device teardown/rebuild.
		if (graphicsSettings.AdapterOrdinal == AdapterOrdinalOld &&
			graphicsSettings.DevType == DevTypeOld)
		{
			// Resize the 3D device
			try
			{
				BuildPresentParamsFromSettings();
				Resize3DEnvironment();
			}
			catch 
			{
				if( windowed )
					ForceWindowed();
				else
					 throw new Exception();
			}
		}
		else
		{
			device.Dispose();
			device = null;
			Initialize3DEnvironment();
		}

		// When moving from fullscreen to windowed mode, it is important to
		// adjust the window size after resetting the device rather than
		// beforehand to ensure that you get the window size you want.  For
		// example, when switching from 640x480 fullscreen to windowed with
		// a 1000x600 window on a 1024x768 desktop, it is impossible to set
		// the window size to 1000x600 until after the display mode has
		// changed to 1024x768, because windows cannot be larger than the
		// desktop.
		if( windowed )
		{
			this.Location = new System.Drawing.Point(windowBoundsRect.Left, windowBoundsRect.Top);
			this.Size = new System.Drawing.Size(( windowBoundsRect.Right - windowBoundsRect.Left ), ( windowBoundsRect.Bottom - windowBoundsRect.Top));
		}

		ready = true;
	}




	//-----------------------------------------------------------------------------
	// Name: ForceWindowed()
	// Desc: Switch to a windowed mode, even if that means picking a new device
	//       and/or adapter
	//-----------------------------------------------------------------------------
	public void ForceWindowed()
	{
		if( windowed )
			return;

		if( !FindBestWindowedMode(false, false) )
		{
			return;
		}
		windowed = true;

		// Now destroy the current 3D device objects, then reinitialize

		ready = false;

		// Release display objects, so a new device can be created
		device.Dispose();
		device = null;

		// Create the new device
		try
		{
			Initialize3DEnvironment();
		}
		catch (GraphicsException e)
		{
			DisplayErrorMsg( e,AppMsgType.ErrorAppMustExit );
		}
		catch
		{
			DisplayErrorMsg( new GraphicsException(),AppMsgType.ErrorAppMustExit );
		}
		ready = true;
	}




	//-----------------------------------------------------------------------------
	// Name: AdjustWindowForChange()
	// Desc: Prepare the window for a possible change between windowed mode and
	//       fullscreen mode.  
	//-----------------------------------------------------------------------------
	public void AdjustWindowForChange()
	{
		if( windowed )
		{
			// Set windowed-mode style
		}
		else
		{
			// Set fullscreen-mode style
		}
	}




	//-----------------------------------------------------------------------------
	// Name: IdleTime()
	// Desc: This function is called anytime our thread has nothing else to do
	// (ie, no messages to process, etc)
	//-----------------------------------------------------------------------------
	public void IdleTime(object sender, EventArgs e)
	{
		// Render a frame during idle time (no messages are waiting)
		if( active && ready )
		{
			try 
			{
				if (deviceLost)
				{
					// Yield some CPU time to other processes
					System.Threading.Thread.Sleep(100); // 100 milliseconds
				}
				if ( m_bTerminate ) 					
					this.Exit(null, null);

				// Render a frame during idle time
				if (active)
				{
					Render3DEnvironment();
				}
			}
			catch (DirectXException d3de)
			{
				System.Diagnostics.Debug.WriteLine(d3de.ErrorString);
			}
			catch (GraphicsException e2)
			{
				DisplayErrorMsg( e2,AppMsgType.ErrorAppMustExit );
			}
			catch ( Exception e3 )
			{
				System.Diagnostics.Debug.WriteLine(e3.Message);
			}
			catch
			{
				DisplayErrorMsg( new GraphicsException(),AppMsgType.ErrorAppMustExit );
			}
/*			catch 
			{
				this.Dispose();
			}
			*/
		}
	}

	//-----------------------------------------------------------------------------
	// Name: Run()
	// Desc: Run the D3D Application
	//-----------------------------------------------------------------------------
	public void Run()
	{
		// Now we're ready to recieve and process Windows messages.
		System.Windows.Forms.Application.Run(this);
		if (!isDisposed)
			this.Dispose();
	}




	//-----------------------------------------------------------------------------
	// Name: Render3DEnvironment()
	// Desc: Draws the scene.
	//-----------------------------------------------------------------------------
	public void Render3DEnvironment()
	{
		if (deviceLost)
		{
			// Test the cooperative level to see if it's okay to render
			int result;
			device.TestCooperativeLevel(out result);
			if (result != 0)
			{
				// If the device was lost, do not render until we get it back
				if (result == (int)ErrorCode.DeviceLost)
					return;

				// Check if the device needs to be resized.
				if( (int)ErrorCode.DeviceNotReset == result )
				{
					// If we are windowed, read the desktop mode and use the same format for
					// the back buffer
					if( windowed )
					{
						D3DAdapterInfo adapterInfo = graphicsSettings.AdapterInfo;
						graphicsSettings.Windowed_DisplayMode = graphicsObject.Adapters[adapterInfo.AdapterOrdinal].DisplayMode;
						presentParams.BackBufferFormat = graphicsSettings.Windowed_DisplayMode.Format;
					}

					Resize3DEnvironment();
				}
			}
			deviceLost = false;
		}

		// Get the app's time, in seconds. Skip rendering if no time elapsed
		float fAppTime        = DXUtil.Timer( TIMER.GETAPPTIME );
		float fElapsedAppTime = DXUtil.Timer( TIMER.GETELAPSEDTIME );
		if( ( 0.0f == fElapsedAppTime ) && frameMoving )
			return;

		// FrameMove (animate) the scene
		if( frameMoving || singleStep )
		{
			// Store the time for the app
			appTime        = fAppTime;
			elapsedTime = fElapsedAppTime;

			// Frame move the scene
			FrameMove();

			singleStep = false;
		}

		// Render the scene as normal
		Render();

		UpdateStats();

		try
		{
			// Show the frame on the primary surface.
			device.Present();
		}
		catch (DirectXException de)
		{
			if (de.ErrorCode == (int) ErrorCode.DeviceLost)
				deviceLost = true;
		}
	}



	//-----------------------------------------------------------------------------
	// Name: UpdateStats()
	// Desc: 
	//-----------------------------------------------------------------------------
	public void UpdateStats()
	{
		// Keep track of the frame count
		float time = DXUtil.Timer( TIMER.GETABSOLUTETIME );
		++frames;

		// Update the scene stats once per second
		if( time - lastTime > 1.0f )
		{
			framePerSecond    = frames / (time - lastTime);
			lastTime = time;
			frames  = 0;

			string strFmt;
			DisplayMode mode = graphicsObject.Adapters[graphicsSettings.AdapterOrdinal].DisplayMode;
			if (mode.Format == backBufferDesc.Format)
			{
				strFmt = mode.Format.ToString();
			}
			else
			{
				strFmt = String.Format("backbuf {0}, adapter {1}", 
					backBufferDesc.Format.ToString(), mode.Format.ToString());
			}

			string strDepthFmt;
			if( enumerationSettings.AppUsesDepthBuffer )
			{
				strDepthFmt = String.Format( " ({0})", 
					graphicsSettings.DepthStencilBufferFormat.ToString());
			}
			else
			{
				// No depth buffer
				strDepthFmt = "";
			}

			string strMultiSample;
			switch( graphicsSettings.MultisampleType )
			{
				case Direct3D.MultiSampleType.OneSample: strMultiSample = " (1x Multisample)"; break;
				case Direct3D.MultiSampleType.TwoSamples: strMultiSample = " (2x Multisample)"; break;
				case Direct3D.MultiSampleType.ThreeSamples: strMultiSample = " (3x Multisample)"; break;
				case Direct3D.MultiSampleType.FourSamples: strMultiSample = " (4x Multisample)"; break;
				case Direct3D.MultiSampleType.FiveSamples: strMultiSample = " (5x Multisample)"; break;
				case Direct3D.MultiSampleType.SixSamples: strMultiSample = " (6x Multisample)"; break;
				case Direct3D.MultiSampleType.SevenSamples: strMultiSample = " (7x Multisample)"; break;
				case Direct3D.MultiSampleType.EightSamples: strMultiSample = " (8x Multisample)"; break;
				case Direct3D.MultiSampleType.NineSamples: strMultiSample = " (9x Multisample)"; break;
				case Direct3D.MultiSampleType.TenSamples: strMultiSample = " (10x Multisample)"; break;
				case Direct3D.MultiSampleType.ElevenSamples: strMultiSample = " (11x Multisample)"; break;
				case Direct3D.MultiSampleType.TwelveSamples: strMultiSample = " (12x Multisample)"; break;
				case Direct3D.MultiSampleType.ThirteenSamples: strMultiSample = " (13x Multisample)"; break;
				case Direct3D.MultiSampleType.FourteenSamples: strMultiSample = " (14x Multisample)"; break;
				case Direct3D.MultiSampleType.FifteenSamples: strMultiSample = " (15x Multisample)"; break;
				case Direct3D.MultiSampleType.SixteenSamples: strMultiSample = " (16x Multisample)"; break;
				default: strMultiSample = ""; break;
			}
			frameStats = String.Format("{0} fps ({1}x{2}), {3}{4}{5}", framePerSecond.ToString("f2"),
				backBufferDesc.Width, backBufferDesc.Height, strFmt, strDepthFmt, strMultiSample);
		}
	}



	//-----------------------------------------------------------------------------
	// Name: Pause()
	// Desc: Called in to toggle the pause state of the app.
	//-----------------------------------------------------------------------------
	public void Pause( bool bPause )
	{

		appPausedCount  += (uint)( bPause ? +1 : -1 );
		ready = ( (appPausedCount > 0) ? false : true );

		// Handle the first pause request (of many, nestable pause requests)
		if( bPause && ( 1 == appPausedCount ) )
		{
			// Stop the scene from animating
			if( frameMoving )
				DXUtil.Timer( TIMER.STOP );
		}

		if( 0 == appPausedCount )
		{
			// Restart the timers
			if( frameMoving )
				DXUtil.Timer( TIMER.START );
		}
	}




	//-----------------------------------------------------------------------------
	// Name: Cleanup3DEnvironment()
	// Desc: Cleanup scene objects
	//-----------------------------------------------------------------------------
	public void Cleanup3DEnvironment()
	{
		active = false;
		ready  = false;

		if( device != null )
		{
			device.Dispose();
			device = null;
			graphicsObject       = null;
		}

		FinalCleanup();
	}
	#region Menu EventHandlers



	//-----------------------------------------------------------------------------
	// Name: UserSelectNewDevice()
	// Desc: Displays a dialog so the user can select a new adapter, device, or
	//       display mode, and then recreates the 3D environment if needed
	//-----------------------------------------------------------------------------
	public void UserSelectNewDevice(object sender, EventArgs e)
	{
		// Prompt the user to select a new device or mode
		if( active && ready )
		{
			Pause(true);

			DoSelectNewDevice();

			Pause(false);
		}
	}
	private void DoSelectNewDevice()
	{
		// Can't display dialogs in fullscreen mode
		if( windowed == false )
		{
			try
			{
				ToggleFullscreen();
			}
			catch
			{
				DisplayErrorMsg( new GraphicsException(GraphicsException.ErrorCode.ResizeFailed), AppMsgType.ErrorAppMustExit);
				return;
			}
		}

		// Make sure the main form is in the background
		this.SendToBack();
		D3DSettingsForm settingsForm = new D3DSettingsForm(enumerationSettings, graphicsSettings);
		System.Windows.Forms.DialogResult result = settingsForm.ShowDialog(null);
		if( result != System.Windows.Forms.DialogResult.OK)
		{
			return;
		}
		graphicsSettings = settingsForm.settings;

		windowed = graphicsSettings.IsWindowed;

		// Release display objects, so a new device can be created
		device.Dispose();
		device = null;

		// Inform the display class of the change. It will internally
		// re-create valid surfaces, a d3ddevice, etc.
		try
		{
			Initialize3DEnvironment();
		}
		catch(GraphicsException d3de)
		{
			DisplayErrorMsg( d3de, AppMsgType.ErrorAppMustExit );
		}
		catch (DirectXException de)
		{
			DisplayErrorMsg( new GraphicsException((GraphicsException.ErrorCode)de.ErrorCode), AppMsgType.ErrorAppMustExit );
		}

		// If the app is paused, trigger the rendering of the current frame
		if( false == frameMoving )
		{
			singleStep = true;
			DXUtil.Timer( TIMER.START );
			DXUtil.Timer( TIMER.STOP );
		}
	}



	//-----------------------------------------------------------------------------
	// Name: ToggleStart()
	// Desc: Will start (or stop) simulation
	//-----------------------------------------------------------------------------
	private void ToggleStart(object sender, EventArgs e)
	{
		//Toggle frame movement
		frameMoving = !frameMoving;
		DXUtil.Timer( frameMoving ? TIMER.START : TIMER.STOP);
	}



	//-----------------------------------------------------------------------------
	// Name: SingleStep()
	// Desc: Will single step the simulation
	//-----------------------------------------------------------------------------
	private void SingleStep(object sender, EventArgs e)
	{
		// Single-step frame movement
		if( false == frameMoving )
			DXUtil.Timer( TIMER.ADVANCE );
		else
			DXUtil.Timer( TIMER.STOP );
		frameMoving = false;
		singleStep  = true;
	}



	//-----------------------------------------------------------------------------
	// Name: Exit()
	// Desc: Will end the simulation
	//-----------------------------------------------------------------------------
	private void Exit(object sender, EventArgs e)
	{
		this.Dispose();
	}
	#endregion
	#region WinForms Overrides
	protected override void OnLoad(System.EventArgs e)
	{
		// Show the window and set focus to it
		this.Show();
		this.Select();
		while(Created)
		{
			IdleTime(null, null);
			System.Windows.Forms.Application.DoEvents();
		}
	}

	protected override void Dispose(bool disposing)
	{
		isDisposed = true;
		Cleanup3DEnvironment();
		base.Dispose(disposing);
		mnuMain.Dispose();
	}

	protected override void OnKeyPress(System.Windows.Forms.KeyPressEventArgs e)
	{
/*
		// Check for our shortcut keys (Space)
		if (e.KeyChar == ' ')
		{
			mnuSingle.PerformClick();
			e.Handled = true;
		}

		// Check for our shortcut keys (Return to start or stop)
		if (e.KeyChar == '\r')
		{
			mnuGo.PerformClick();
			e.Handled = true;

		}
*/
		// Check for our shortcut keys (Escape to quit)
		if ((byte)e.KeyChar == (byte)(int)System.Windows.Forms.Keys.Escape)
		{
			mnuExit.PerformClick();
			e.Handled = true;
		}

	}

	protected override void OnLostFocus(System.EventArgs e)
	{
		hasFocus = false;
		base.OnLostFocus(e);
	}

	protected override void OnGotFocus(System.EventArgs e)
	{
		hasFocus = true;
		base.OnGotFocus(e);
	}
	protected override void OnKeyDown(System.Windows.Forms.KeyEventArgs e)
	{
		char tstr = (char)(e.KeyValue);
		if ( GameEngine.Console.IsVisible && e.KeyData == System.Windows.Forms.Keys.Return )
		{
			GameEngine.Console.ProcessEntry();
		}
		if ( e.KeyData == System.Windows.Forms.Keys.F12 )
		{
			GameEngine.Console.ToggleState();
		}
		else if ( GameEngine.Console.IsVisible && 
			(e.KeyData == System.Windows.Forms.Keys.Space ||
			( e.KeyData >= System.Windows.Forms.Keys.A &&
			e.KeyData <= System.Windows.Forms.Keys.Z ) ||
			( e.KeyData >= System.Windows.Forms.Keys.D0 &&
			e.KeyData <= System.Windows.Forms.Keys.D9 ) 
			) )
		{
			GameEngine.Console.AddCharacterToEntryLine( tstr );
		}
		else if ( GameEngine.Console.IsVisible && e.KeyData == System.Windows.Forms.Keys.OemPeriod )
		{
			GameEngine.Console.AddCharacterToEntryLine( '.' );
		}
		else if ( GameEngine.Console.IsVisible && e.KeyData == System.Windows.Forms.Keys.OemMinus )
		{
			GameEngine.Console.AddCharacterToEntryLine( '-' );
		}
		else if ( GameEngine.Console.IsVisible && e.KeyData == System.Windows.Forms.Keys.Back )
		{
			GameEngine.Console.Backspace();
		}
		if ( (e.Alt) && (e.KeyCode == System.Windows.Forms.Keys.Return))
		{
			// Toggle the fullscreen/window mode
			if( active && ready )
			{
				Pause( true );

				try
				{
					ToggleFullscreen();
					Pause( false );                        
					return;
				}
				catch
				{
					DisplayErrorMsg( new GraphicsException(GraphicsException.ErrorCode.ResizeFailed), AppMsgType.ErrorAppMustExit);
				}
				finally
				{
					e.Handled = true;
				}
			}
		}
		// Allow the control to handle the keystroke now
//		base.OnKeyDown(e);
	}

	private void InitializeComponent()
	{
		// 
		// GraphicsSample
		// 
		this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
		this.MinimumSize = new System.Drawing.Size(100, 100);
		#region MenuCreation/etc
		this.mnuMain = new System.Windows.Forms.MainMenu();
		this.mnuFile = new System.Windows.Forms.MenuItem();
		this.mnuGo = new System.Windows.Forms.MenuItem();
		this.mnuSingle = new System.Windows.Forms.MenuItem();
		this.mnuBreak1 = new System.Windows.Forms.MenuItem();
		this.mnuChange = new System.Windows.Forms.MenuItem();
		this.mnuBreak2 = new System.Windows.Forms.MenuItem();
		this.mnuExit = new System.Windows.Forms.MenuItem();
		// 
		// mainMenu1
		// 
		this.mnuMain.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																				this.mnuFile});
		// 
		// mnuFile
		// 
		this.mnuFile.Index = 0;
		this.mnuFile.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																				this.mnuGo,
																				this.mnuSingle,
																				this.mnuBreak1,
																				this.mnuChange,
																				this.mnuBreak2,
																				this.mnuExit});
		this.mnuFile.Text = "&File";
		// 
		// mnuGo
		// 
		this.mnuGo.Index = 0;
		this.mnuGo.Text = "&Go/stop\tEnter";
		this.mnuGo.Click += new System.EventHandler(this.ToggleStart);
		// 
		// mnuSingle
		// 
		this.mnuSingle.Index = 1;
		this.mnuSingle.Text = "&Single step\tSpace";
		this.mnuSingle.Click += new System.EventHandler(this.SingleStep);
		// 
		// mnuBreak1
		// 
		this.mnuBreak1.Index = 2;
		this.mnuBreak1.Text = "-";
		// 
		// mnuChange
		// 
		this.mnuChange.Index = 3;
		this.mnuChange.Shortcut = System.Windows.Forms.Shortcut.F2;
		this.mnuChange.ShowShortcut = true;
		this.mnuChange.Text = "&Change Device...";
		this.mnuChange.Click += new System.EventHandler(this.UserSelectNewDevice);
		// 
		// mnuBreak2
		// 
		this.mnuBreak2.Index = 4;
		this.mnuBreak2.Text = "-";
		// 
		// mnuExit
		// 
		this.mnuExit.Index = 5;
		this.mnuExit.Text = "E&xit\tESC";
		this.mnuExit.Click += new System.EventHandler(this.Exit);
		#endregion
	}

	protected override void OnMenuStart(System.EventArgs e)
	{
		Pause(true); // Pause the simulation while the menu starts
	}

	protected override void OnMenuComplete(System.EventArgs e)
	{
		Pause(false); // Unpause the simulation 
	}

	protected override void OnSizeChanged(System.EventArgs e)
	{
		this.OnResize(null);
	}
	protected override void OnResize(System.EventArgs e)
	{
		if (!ready)
			return;

		active = !( this.WindowState == System.Windows.Forms.FormWindowState.Minimized || this.Visible == false );

		if( active && windowed )
		{
			System.Drawing.Rectangle rcClientOld;
			rcClientOld = clientRect;

			// Update window properties
			windowBoundsRect = new System.Drawing.Rectangle(this.Location, this.Size);
			clientRect = this.ClientRectangle;

			if( rcClientOld.Right - rcClientOld.Right !=
				clientRect.Right - clientRect.Left ||
				rcClientOld.Bottom - rcClientOld.Top !=
				clientRect.Bottom - clientRect.Top)
			{
				// A new window size will require a new backbuffer
				// size, so the 3D structures must be changed accordingly.
				ready = false;

				presentParams.BackBufferWidth  = clientRect.Right - clientRect.Left;
				presentParams.BackBufferHeight = clientRect.Bottom - clientRect.Top;

				// Resize the 3D environment
				try
				{
					Resize3DEnvironment();
				}
				catch 
				{
					DisplayErrorMsg( new GraphicsException(GraphicsException.ErrorCode.ResizeFailed), AppMsgType.ErrorAppMustExit);
				}

				ready = true;
			}
		}
	}
	#endregion

}

