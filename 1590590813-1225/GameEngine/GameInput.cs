using System;
using System.Collections;
using System.Drawing;
using Microsoft.DirectX.DirectInput;
using Microsoft.DirectX;

namespace GameEngine
{
		#region delegates
		public delegate void ButtonAction();
		public delegate void AxisAction(int nCount);
		#endregion

	/// <summary>
	/// Summary description for GameInput.
	/// This class encapsulates the interface to the keyboard and mouse.  It provides
	/// a unified interface for the game engine where the status of any of these input devices
	/// may be polled.  More importantly it provides the capability to invoke a supplied method
	/// whenever a mapped keyboard or mouse button is pressed.
	/// </summary>
	public class GameInput : IDisposable
	{
		#region Attributes
		public  bool           m_bInitialized  = false;

		private KeyboardState  m_keydata       = null;
		private MouseState     m_mousedata     = new MouseState();
		private JoystickState  m_joystick      = new JoystickState();
		private int            m_NumPov        = 0;
		private Device         KeyboardDev     = null;
		private Device         MouseDev        = null;
		private Device         JoystickDev     = null;
		private bool           m_bJoystickSet  = false;
		private ArrayList      m_ActionMap     = new ArrayList();
		private ArrayList      m_AxisActionMap = new ArrayList();
		private int            m_MouseX        = 0;
		private int            m_MouseY        = 0;
		private int            m_MouseZ        = 0;
		private static Point   m_MousePoint;
		private System.Windows.Forms.Form m_form = null;		
			#endregion

		struct Mapping 
		{
			public int key;
			public ButtonAction action;
			public bool bOnTransition;
		}

		struct AxisMapping 
		{
			public int key;
			public AxisAction action;
		}

		/// <summary>
		/// return a reference to the DirectInput device
		/// </summary>
		/// <returns></returns>
		public Point GetMousePoint()
		{
			return m_MousePoint;
		}

		public GameInput(System.Windows.Forms.Form form)
		{
			m_form = form;

			try
			{

				m_MousePoint = new Point();
				m_MousePoint.X = 400;
				m_MousePoint.Y = 300;

				// create the keyboard device
				KeyboardDev = new Device( SystemGuid.Keyboard );
				KeyboardDev.SetCooperativeLevel( m_form, CooperativeLevelFlags.NoWindowsKey | CooperativeLevelFlags.NonExclusive | CooperativeLevelFlags.Foreground);
				KeyboardDev.SetDataFormat(DeviceDataFormat.Keyboard);

				// create the mouse device
				MouseDev = new Device( SystemGuid.Mouse );
				MouseDev.SetCooperativeLevel( m_form, CooperativeLevelFlags.Exclusive | CooperativeLevelFlags.Foreground);
				MouseDev.SetDataFormat(DeviceDataFormat.Mouse);

				// Enumerate joysticks in the system.
				foreach(DeviceInstance instance in Manager.GetDevices(DeviceClass.GameControl, EnumDevicesFlags.AttachedOnly))
				{
					// Create the device.  Just pick the first one
					JoystickDev = new Device( instance.InstanceGuid );
					break;
				}
				if ( JoystickDev != null )
				{
					// Create the device.
					// Set the cooperative level for the device.
					JoystickDev.SetCooperativeLevel( m_form, CooperativeLevelFlags.Exclusive | CooperativeLevelFlags.Foreground);
					// Set the data format to the Joystick pre-defined format.
					JoystickDev.SetDataFormat(DeviceDataFormat.Joystick);

					// Now find out how many POV's the device has
					// for displaying info in the UI thread.
					m_NumPov = JoystickDev.Caps.NumberPointOfViews;
					m_bJoystickSet = true;
					try
					{
						JoystickDev.Acquire();
					}
					catch{}
				}
			}
			catch {}

		}

		// poll method
		public void Poll()
		{
		KeyboardState oldkeydata = null;

		// Bool flag that is set when it's ok
		// to get device state information.
		bool bKeyboardOk = false;
		bool bMouseOk = false;
		bool bJoystickOk = false;
			

			MouseState oldmousedata = new MouseState();
			JoystickState oldjoystickdata = new JoystickState();

			oldkeydata = m_keydata;

			// get keyboard data
			try
			{
				KeyboardDev.Poll();
				bKeyboardOk = true;
			}
			catch(InputException ex)
			{
				// Check to see if either the app
				// needs to acquire the device, or
				// if the app lost the device to another
				// process.
				if ( (ex is NotAcquiredException)  )
				{
					try
					{
						// Reacquire the device.
						KeyboardDev.Acquire();
						// Set the flag for now.
						bKeyboardOk = true;
					}
					catch(InputException ex2)
					{								
						if ( ex2 is OtherApplicationHasPriorityException)
						{	// Something very odd happened.
							Console.AddLine("An unknown error has occcurred. This app won't be able to process device info.");
						}
						// Failed to aquire the device.
						// This could be because the app
						// doesn't have focus.
						bKeyboardOk = false;
					}
				}
				else
				{
					KeyboardDev = new Device( SystemGuid.Keyboard );
					KeyboardDev.SetCooperativeLevel( m_form, CooperativeLevelFlags.NoWindowsKey | CooperativeLevelFlags.NonExclusive | CooperativeLevelFlags.Foreground);
					KeyboardDev.SetDataFormat(DeviceDataFormat.Keyboard);
				}
			}

			// get mouse data
			try
			{
				MouseDev.Poll();
				bMouseOk = true;
			}
			catch(InputException ex)
			{
				// Check to see if either the app
				// needs to acquire the device, or
				// if the app lost the device to another
				// process.
				if ( (ex is NotAcquiredException) )
				{
					try
					{
						// Reacquire the device.
						MouseDev.Acquire();
						// Set the flag for now.
						bMouseOk = true;
//						Console.AddLine("had to reacquire the mouse");
					}
					catch(InputException ex2)
					{								
						if ( ex2 is OtherApplicationHasPriorityException )
						{	// Something very odd happened.
							System.Diagnostics.Debug.WriteLine("An unknown error has occcurred. This app won't be able to process device info. " + ex2.ErrorString);
						}
						// Failed to aquire the device.
						// This could be because the app
						// doesn't have focus.
						bMouseOk = false;
					}
				}
				else
				{
					System.Diagnostics.Debug.WriteLine(ex.ErrorString);
					MouseDev.Dispose();
					MouseDev = new Device( SystemGuid.Mouse );
					MouseDev.SetCooperativeLevel( m_form, CooperativeLevelFlags.Exclusive | CooperativeLevelFlags.Foreground);
					MouseDev.SetDataFormat(DeviceDataFormat.Mouse);
				}
			}

			// get joystick data
			try
			{
				if ( m_bJoystickSet ) 
				{
					JoystickDev.Poll();
					bJoystickOk = true;
				}
			}
			catch(InputException ex)
			{
				// Check to see if either the app
				// needs to acquire the device, or
				// if the app lost the device to another
				// process.
				if ( (ex is NotAcquiredException) )
				{
					try
					{
						if ( JoystickDev != null )
						{
							// Create the device.
							// Set the cooperative level for the device.
							JoystickDev.SetCooperativeLevel( m_form, CooperativeLevelFlags.Exclusive | CooperativeLevelFlags.Foreground);
							// Set the data format to the Joystick pre-defined format.
							JoystickDev.SetDataFormat(DeviceDataFormat.Joystick);

							// Now find out how many POV's the device has
							// for displaying info in the UI thread.
							m_NumPov = JoystickDev.Caps.NumberPointOfViews;
							m_bJoystickSet = true;
						}
						// Reacquire the device.
						JoystickDev.Acquire();
						// Set the flag for now.
						bJoystickOk = true;
					}
					catch(InputException ex2)
					{								
						if (  ex2 is OtherApplicationHasPriorityException )
						{	// Something very odd happened.
							Console.AddLine("An unknown error has occcurred. This app won't be able to process device info.");
						}
						// Failed to aquire the device.
						// This could be because the app
						// doesn't have focus.
						bJoystickOk = false;
					}
				}
				else 
				{
					if ( JoystickDev != null )
					{
						// Create the device.
						// Set the cooperative level for the device.
						JoystickDev.SetCooperativeLevel( m_form, CooperativeLevelFlags.Exclusive | CooperativeLevelFlags.Foreground);
						// Set the data format to the Joystick pre-defined format.
						JoystickDev.SetDataFormat(DeviceDataFormat.Joystick);

						// Now find out how many POV's the device has
						// for displaying info in the UI thread.
						m_NumPov = JoystickDev.Caps.NumberPointOfViews;
						m_bJoystickSet = true;
						// Reacquire the device.
						try
						{
							JoystickDev.Acquire();
						}
						catch{}

						// Set the flag for now.
						bJoystickOk = true;
					}
				}
			}
			if (bJoystickOk == true)
			{
				// Get the state of the device
				try	{ m_joystick = JoystickDev.CurrentJoystickState; }
					// Catch any exceptions. None will be handled here, 
					// any device re-aquisition will be handled above.	
				catch(DirectXException){}
			}

			try 
			{
			    if ( bKeyboardOk )
				{
				   m_keydata = KeyboardDev.GetCurrentKeyboardState();
				}
			    if ( bMouseOk )
				{
				    m_mousedata = MouseDev.CurrentMouseState;
					m_MouseX += m_mousedata.X;
					m_MouseY += m_mousedata.Y;
					m_MouseZ += m_mousedata.Z;

					m_MousePoint.X += m_mousedata.X;
					m_MousePoint.Y += m_mousedata.Y;

				}

				// call any axis actions
				foreach ( AxisMapping map in m_AxisActionMap )
				{
					switch ( map.key ) 
					{
						case 0:
							map.action(m_mousedata.X);
							break;
						case 1:
							map.action(m_mousedata.Y);
							break;
						case 2:
							map.action(m_mousedata.Z);
							break;
						case 3:
							map.action(m_joystick.X);
							break;
						case 4:
							map.action(m_joystick.X);
							break;
						case 5:
							map.action(m_joystick.X);
							break;
					}
				}

				// only process the action map if the console is not visible
				if ( !GameEngine.Console.IsVisible )
				{
					foreach ( Mapping map in m_ActionMap ) 
					{
						// if this is against the keyboard
						if ( map.key < 256 ) 
						{
							if ( m_keydata[(Key)map.key]  ) 
							{
								if ( !map.bOnTransition || oldkeydata[(Key)map.key]  ) 
								{
									map.action();
								}
							}
						} 
						else if ( map.key < 264 )  // space for 8 mouse buttons
						{
							if ( (m_mousedata.GetMouseButtons()[map.key-256] & 0x80) != 0 ) 
							{
								if ( !map.bOnTransition || (oldmousedata.GetMouseButtons()[map.key-256] & 0x80) == 0 ) 
								{
									map.action();
								}
							}
						}
						else  // joystick buttons
						{
							if ( (m_joystick.GetButtons()[map.key-264] & 0x80) != 0 ) 
							{
								if ( !map.bOnTransition || (oldjoystickdata.GetButtons()[map.key-264] & 0x80) == 0 ) 
								{
									map.action();
								}
							}
						}
					}
				}
			}
			catch {}
		}

		public bool IsMouseButtonDown( int nButton )
		{
			return (m_mousedata.GetMouseButtons()[nButton] & 0x80) != 0;
		}

		// Check if any buttons were pressed
		public bool IsKeyPressed()
		{
			bool bPressed = false;

///TODO			foreach ( byte b in m_keydata ) 
//			{
//				if ( (b & 0x80) > 0 ) bPressed = true;
//			}
			return bPressed;
		}

		// Check if any buttons were pressed
		public bool IsKeyPressed(Key key)
		{
			bool bPressed = false;
			try 
			{
				bPressed = m_keydata[key] ;
			}
			catch
			{
			}

			return bPressed;
		}

		// method to map a key to an action
		public void MapKeyboardAction( Key key, ButtonAction proc, bool bTransition )
		{
			Mapping map = new Mapping();
			map.key = (int)key;
			map.action = proc;
			map.bOnTransition = bTransition;
			m_ActionMap.Add(map);
		}

		// method to map a mouse button to an action
		public void MapMouseButtonAction( int nButton, ButtonAction proc, bool bTransition )
		{
			Mapping map = new Mapping();
			map.key = nButton + 256;
			map.action = proc;
			map.bOnTransition = bTransition;
			m_ActionMap.Add(map);
		}

		// method to map a joystick button to an action
		public void MapJoystickButtonAction( int nButton, ButtonAction proc, bool bTransition )
		{
			Mapping map = new Mapping();
			map.key = nButton + 264;
			map.action = proc;
			map.bOnTransition = bTransition;
			m_ActionMap.Add(map);
		}

		// method to map a mouse axis to an action
		public void MapMouseAxisAction( int nAxis, AxisAction proc )
		{
			AxisMapping map = new AxisMapping();
			map.key = nAxis;
			map.action = proc;
			m_AxisActionMap.Add(map);
		}

		// method to map a joystick axis to an action
		public void MapJoystickAxisAction( int nAxis, AxisAction proc )
		{
			AxisMapping map = new AxisMapping();
			map.key = nAxis + 3;
			map.action = proc;
			m_AxisActionMap.Add(map);
		}

		public void UnMapKeyboardAction( Key key )
		{
			foreach ( Mapping map in m_ActionMap ) 
			{
				if ( map.key == (int)key ) 
				{
					m_ActionMap.Remove(map);
				}
			}
		}

		public void UnMapMouseButtonAction( int nButton )
		{
			foreach ( Mapping map in m_ActionMap ) 
			{
				if ( map.key == (nButton + 256) ) 
				{
					m_ActionMap.Remove(map);
				}
			}
		}

		public void UnMapMouseAxisAction( int nAxis )
		{
			foreach ( AxisMapping map in m_AxisActionMap ) 
			{
				if ( map.key == nAxis ) 
				{
					m_AxisActionMap.Remove(map);
				}
			}
		}

		public void UnMapJoystickButtonAction( int nButton )
		{
			foreach ( Mapping map in m_ActionMap ) 
			{
				if ( map.key == (nButton + 264) ) 
				{
					m_ActionMap.Remove(map);
				}
			}
		}

		public void UnMapJoystickAxisAction( int nAxis )
		{
			foreach ( AxisMapping map in m_AxisActionMap ) 
			{
				if ( map.key == (nAxis+3) ) 
				{
					m_AxisActionMap.Remove(map);
				}
			}
		}

		public void ClearActionMaps()
		{
			m_ActionMap.Clear();
			m_AxisActionMap.Clear();
		}


		/// <summary>
		/// Get the current mouse X value
		/// </summary>
		public int GetMouseX()
		{
			int nResult = m_MouseX;

			m_MouseX = 0;
			return nResult;
		}

		/// <summary>
		/// Get the current mouse Y value
		/// </summary>
		public int GetMouseY()
		{
			int nResult = m_MouseY;

			m_MouseY = 0;
			return nResult;
		}

		/// <summary>
		/// Get the current mouse Z value (mouse wheel)
		/// </summary>
		public int GetMouseZ()
		{
			int nResult = m_MouseZ;

			m_MouseZ = 0;
			return nResult;
		}

		/// <summary>
		/// Get joystick axis data
		/// </summary>
		public int GetJoystickX( )
		{
			if ( m_bJoystickSet ) 
			{
				return m_joystick.X;
			} 
			else 
			{
				return 0;
			}
		}

		/// <summary>
		/// Get normalized joystick axis data
		/// </summary>
		public float GetJoystickNormalX( )
		{
			if ( m_bJoystickSet ) 
			{
				return (float)m_joystick.X/(float)short.MaxValue;
			} 
			else 
			{
				return 0;
			}
		}

		/// <summary>
		/// Get joystick axis data
		/// </summary>
		public int GetJoystickY( )
		{
			if ( m_bJoystickSet ) 
			{
				return m_joystick.Y;
			} 
			else 
			{
				return 0;
			}
		}

		/// <summary>
		/// Get normalized joystick axis data
		/// </summary>
		public float GetJoystickNormalY( )
		{
			if ( m_bJoystickSet ) 
			{
				return (float)m_joystick.Y/(float)short.MaxValue;
			} 
			else 
			{
				return 0;
			}
		}

		/// <summary>
		/// Get joystick axis data
		/// </summary>
		public int GetJoystickZ( )
		{
			if ( m_bJoystickSet ) 
			{
				return m_joystick.Z;
			} 
			else 
			{
				return 0;
			}
		}

		/// <summary>
		/// Get joystick button data
		/// </summary>
		public bool GetJoystickButton( int nIndex )
		{
			if ( m_bJoystickSet && nIndex < 32 ) 
			{
				return m_joystick.GetButtons()[nIndex] != 0;
			} 
			else 
			{
				return false;
			}
		}

		/// <summary>
		/// Get joystick slider data
		/// </summary>
		public int GetJoystickSlider( int nIndex )
		{
			if ( m_bJoystickSet && nIndex < 2 ) 
			{
				return m_joystick.GetSlider()[nIndex];
			} 
			else 
			{
				return 0;
			}
		}

		//Dispose method
		public void Dispose()
		{
			KeyboardDev.Unacquire();
			KeyboardDev.Dispose();
			MouseDev.Unacquire();
			MouseDev.Dispose();
			if ( m_bJoystickSet ) 
			{
				JoystickDev.Unacquire();
				JoystickDev.Dispose();
			}
		}

	}
}
