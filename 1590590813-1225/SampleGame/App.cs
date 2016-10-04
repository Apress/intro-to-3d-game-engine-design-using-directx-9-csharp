//-----------------------------------------------------------------------------
// File: App.cs
//
// Desc: Sample code for Introduction to 3D Game Engine Design.
//
//       This sample shows the basic application software that sets up the
//       base application and the process flow.  The application uses a version of the
//       CD3DApplication base class provided with the Microsoft DirectX 9 SDK to
//       perform the standard initialization of DirectX.
//
//       Note: This code uses the D3D Framework helper library.
//
// Copyright (c) 2002 Lynn T. Harrison All rights reserved.
//-----------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX.DirectInput;
using GameEngine;
using GameAI;

namespace SampleGame
{
	/// <summary>
	/// Summary description for GameEngine.
	/// </summary>
	class CGameApplication : GraphicsSample
	{
		#region	// Game State enumeration
		/// <summary>
		/// Each member of this enumeration is one possible state for the application
		/// </summary>
		/// 
		/// <remarks>
		/// DevSplash         - Display the Developer splash screen
		/// </remarks>
		/// <remarks>
		/// GameSplash        - Display the game splash screen
		/// </remarks>
		/// <remarks>
		/// OptionsMain       - Displays and process the primary options screen
		/// </remarks>
		/// <remarks>
		/// GamePlay          - state to actually play the game
		/// </remarks>
		/// <remarks>
		/// AfterActionReview - Display the results of the game
		/// </remarks>
		public enum GameState
		{
			/// <summary>
			/// Display the Developer splash screen
			/// </summary>
			DevSplash,	
			/// <summary>
			/// Display the game splash screen
			/// </summary>
			GameSplash,		
			/// <summary>
			/// Displays and process the primary options screen
			/// </summary>
			OptionsMain,		
			/// <summary>
			/// state to actually play the game
			/// </summary>
			GamePlay,			 
			/// <summary>
			/// Display the results of the game
			/// </summary>
			AfterActionReview,	 
		}
	#endregion

		#region // Application member variables
		/// <summary>
		/// Current state of the application
		/// </summary>
		private GameState          m_State; 
		private static CGameEngine m_Engine = new CGameEngine();  // connection to the game engine
		private GraphicsFont       m_pFont = null;  // font for screen text rendering
		private GameEngine.Console m_Console;
		private ArrayList          m_opponents = null;
		private OptionScreen       m_OptionScreen = null;
		private bool			   m_bShowStatistics = false;
		private bool               m_bScreenCapture = false;
		private bool               m_bUsingJoystick = true;
		private bool               m_bUsingKeyboard = false;
		private bool               m_bUsingMouse = false;
		private Ownship            m_ownship = null;
		private Cloth			   m_flag = null;
		private Jukebox            music = null;
		#endregion

		public static CGameEngine Engine { get { return m_Engine; } }
		
		/// <summary>
		/// Application constructor. Sets attributes for the app.
		/// </summary>
		public CGameApplication()
		{
			// Initialize the Game state for the Developer Splash Screen
			m_State = GameState.DevSplash;

			// create a copy of the game engine
			m_pFont = new GraphicsFont( "Aerial", System.Drawing.FontStyle.Bold );
			windowed = false;

			m_opponents = new ArrayList();

		}

		/// <summary>
		/// Called during initial app startup, this function performs all the
		/// permanent initialization.
		/// </summary>
		protected override void OneTimeSceneInitialization()
		{
			// Initialize the font's internal textures
			m_pFont.InitializeDeviceObjects( device );

			m_Engine.Initialize( this, device );

			CGameEngine.Inputs.MapKeyboardAction(Key.Escape,new ButtonAction(Terminate), true);  
			CGameEngine.Inputs.MapKeyboardAction(Key.A,new ButtonAction(MoveCameraXM), false);  
			CGameEngine.Inputs.MapKeyboardAction(Key.W,new ButtonAction(MoveCameraZP), false);  
			CGameEngine.Inputs.MapKeyboardAction(Key.S,new ButtonAction(MoveCameraXP), false);  
			CGameEngine.Inputs.MapKeyboardAction(Key.Z,new ButtonAction(MoveCameraZM), false);  
			CGameEngine.Inputs.MapKeyboardAction(Key.P,new ButtonAction(ScreenCapture), true);  
			CGameEngine.Inputs.MapMouseAxisAction(0,new AxisAction(PointCamera));  
			CGameEngine.Inputs.MapMouseAxisAction(1,new AxisAction(PitchCamera));  

			m_Console = new GameEngine.Console( m_pFont, @"..\..\Resources\console.jpg" );

			GameEngine.Console.AddCommand("QUIT", "Terminate the game", new CommandFunction(TerminateCommand));
			GameEngine.Console.AddCommand("STATISTICS", "Toggle statistics display", new CommandFunction(ToggleStatistics));

			m_OptionScreen = new OptionScreen( @"..\..\Resources\Options2.jpg" );
			m_OptionScreen.AddButton( 328, 150, @"..\..\Resources\PlayOff.bmp", @"..\..\Resources\PlayOn.bmp", @"..\..\Resources\PlayHover.bmp", new ButtonFunction(Play) );
			m_OptionScreen.AddButton( 328, 300, @"..\..\Resources\QuitOff.bmp", @"..\..\Resources\QuitOn.bmp", @"..\..\Resources\QuitHover.bmp", new ButtonFunction(Terminate) );
			m_Engine.SetOptionScreen( m_OptionScreen );

			music = new Jukebox();
			music.AddSong("nadine.mp3");
			music.AddSong("ComeOn.mp3");
			music.AddSong("Rock.mp3");
			music.Volume = 0.75f;
			music.Play();

		}


		/// <summary>
		/// Called once per frame, the call is the entry point for all game processing. 
		/// This function calls the appropriate part of the game engine based on the
		/// current state.
		/// </summary>
		protected override void FrameMove()
		{
			try
			{
				SelectControls select_form = null;
				// get any player inputs
				m_Engine.GetPlayerInputs();

				// Clear the viewport
				device.Clear( ClearFlags.Target | ClearFlags.ZBuffer, 0x00000000, 1.0f, 0 );

				device.BeginScene();

				// determine what needs to be rendered based on the current game state
				switch ( m_State ) 
				{
					case GameState.DevSplash:
						if ( m_Engine.ShowSplash(@"..\..\Resources\devsplash.jpg", 8, new BackgroundTask(LoadOptions)) ) 
						{
							m_State = GameState.GameSplash;
						}
						break;
					case GameState.GameSplash:
						if ( m_Engine.ShowSplash(@"..\..\Resources\gamesplash.jpg", 8, null) ) 
						{
							m_State = GameState.OptionsMain;
							select_form = new SelectControls();
							select_form.ShowDialog(this);
							m_bUsingJoystick = select_form.UseJoystick.Checked;
							m_bUsingKeyboard = select_form.UseKeyboard.Checked;
							m_bUsingMouse = select_form.UseMouse.Checked;
							if ( m_bUsingJoystick ) GameEngine.Console.AddLine("Using Joystick");
							if ( m_bUsingKeyboard ) GameEngine.Console.AddLine("Using Keyboard");
							if ( m_bUsingMouse ) GameEngine.Console.AddLine("Using Mouse");
							m_ownship = (Ownship)Engine.GetObject("car1");
							m_ownship.UseJoystick = m_bUsingJoystick;
							m_ownship.UseKeyboard = m_bUsingKeyboard;
							m_ownship.UseMouse = m_bUsingMouse;
						}
						break;
					case GameState.OptionsMain:
						m_Engine.DoOptions();
						break;
					case GameState.GamePlay:
						m_Engine.GetPlayerInputs();
						m_Engine.DoAI( elapsedTime );
						m_Engine.DoDynamics( elapsedTime );
						m_Engine.DoNetworking( elapsedTime );
						m_Engine.Render();
						break;
					case GameState.AfterActionReview:
						m_Engine.DoAfterActionReview();
						break;
				}

				GameEngine.Console.Render();

				if ( false && m_ownship != null && m_State == GameState.GamePlay )
				{
					m_pFont.DrawText( 200, 560, Color.FromArgb(255,0,0,0), m_ownship.MPH.ToString() );
					m_pFont.DrawText( 280, 560, Color.FromArgb(255,0,0,0), m_ownship.RPM.ToString() );
					m_pFont.DrawText( 200, 580, Color.FromArgb(255,0,0,0), m_ownship.ForwardVelocity.ToString() );
					m_pFont.DrawText( 100, 560, Color.FromArgb(255,0,0,0), m_ownship.SidewaysVelocity.ToString() );
					m_pFont.DrawText( 100, 580, Color.FromArgb(255,0,0,0), m_ownship.Steering.ToString() );
				}

				// Output statistics
				if ( m_bShowStatistics )
				{
					m_pFont.DrawText( 2, 560, Color.FromArgb(255,255,255,0), frameStats );
					m_pFont.DrawText( 2, 580, Color.FromArgb(255,255,255,0), deviceStats );
					m_pFont.DrawText( 500, 580, Color.FromArgb(255,255,255,0), 
						m_Engine.Cam.Heading.ToString() + " " + m_Engine.Cam.Pitch.ToString()  + " " +
						m_Engine.Cam.X  + " " +m_Engine.Cam.Y  + " " +m_Engine.Cam.Z);
					m_pFont.DrawText( 2, 600, Color.FromArgb(255,255,255,0), 
						"Steering " + (CGameEngine.Inputs.GetJoystickNormalX()-1.0f).ToString()  + " " +
						"throttle/Brake " + (1.0f-CGameEngine.Inputs.GetJoystickNormalY()).ToString());
				}

				if ( m_bScreenCapture )
				{
					SurfaceLoader.Save("capture.bmp",ImageFileFormat.Bmp,device.GetBackBuffer(0,0,BackBufferType.Mono));
					m_bScreenCapture = false;
					GameEngine.Console.AddLine("snapshot taken");
				}
			}
			catch (DirectXException d3de)
			{
				System.Diagnostics.Debug.WriteLine("Error in Sample Game Application FrameMove" );
				System.Diagnostics.Debug.WriteLine(d3de.ErrorString);
			}
			catch ( Exception e )
			{
				System.Diagnostics.Debug.WriteLine("Error in Sample Game Application FrameMove" );
				System.Diagnostics.Debug.WriteLine(e.Message);
			}
			finally
			{
				device.EndScene();
			}


		}


		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			try
			{
				CGameApplication d3dApp = new CGameApplication();
				if (d3dApp.CreateGraphicsSample())
					d3dApp.Run();
			}
			catch (DirectXException d3de)
			{
				System.Diagnostics.Debug.WriteLine("Error in Sample Game Application" );
				System.Diagnostics.Debug.WriteLine(d3de.ErrorString);
			}
			catch ( Exception e )
			{
				System.Diagnostics.Debug.WriteLine("Error in Sample Game Application" );
				System.Diagnostics.Debug.WriteLine(e.Message);
			}
		}

		// action functions

		/// <summary>
		/// Action to start playing
		/// </summary>
		public void Play()
		{
			m_State = GameState.GamePlay;
			GameEngine.Console.Reset();
		}

		/// <summary>
		/// Action to terminate the application
		/// </summary>
		public void Terminate()
		{
			m_bTerminate = true;
		}

		/// <summary>
		/// screen capture
		/// </summary>
		public void ScreenCapture()
		{
			m_bScreenCapture = true;
		}

		/// <summary>
		/// version of terminate for use by the console
		/// </summary>
		/// <param name="sData"></param>
		public void TerminateCommand( string sData )
		{
			Terminate();
		}

		/// <summary>
		/// Toggle the display of statistics information
		/// </summary>
		/// <param name="sData"></param>
		public void ToggleStatistics( string sData )
		{
			m_bShowStatistics = !m_bShowStatistics;
		}

		/// <summary>
		/// Action to transition to the next game state based on a mapper action
		/// </summary>
		public void NextState()
		{
			if ( m_State < GameState.AfterActionReview ) 
			{
				m_State++;
				if ( m_State == GameState.GamePlay )
				{
					m_ownship = (Ownship)Engine.GetObject("car1");
					m_ownship.Driving = true;
				}
			} 
			else 
			{
				m_State = GameState.OptionsMain;
			}
		}

		public void PointCamera( int count )
		{
			m_Engine.MoveCamera(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, count);
		}

		public void PitchCamera( int count )
		{
			m_Engine.MoveCamera(0.0f, 0.0f, 0.0f, count * 0.1f, 0.0f, 0.0f);
		}

		public void MoveCameraXP()
		{
			m_Engine.MoveCamera(0.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
		}

		public void MoveCameraXM()
		{
			m_Engine.MoveCamera(-0.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f);
		}

		public void MoveCameraY()
		{
			m_Engine.MoveCamera(0.0f, 0.5f, 0.0f, 0.0f, 0.0f, 0.0f);
		}

		public void MoveCameraZP()
		{
			m_Engine.MoveCamera(0.0f, 0.0f, 0.5f, 0.0f, 0.0f, 0.0f);
		}

		public void MoveCameraZM()
		{
			m_Engine.MoveCamera(0.0f, 0.0f, -0.5f, 0.0f, 0.0f, 0.0f);
		}

		/// <summary>
		/// 
		/// </summary>
		protected override void RestoreDeviceObjects(System.Object sender, System.EventArgs e)
		{
			// Restore the device objects for the meshes and fonts

			// Set the transform matrices (view and world are updated per frame)
			Matrix matProj;
			float fAspect = device.PresentationParameters.BackBufferWidth / (float)device.PresentationParameters.BackBufferHeight;
			matProj = Matrix.PerspectiveFovLH( (float)Math.PI/4, fAspect, 1.0f, 100.0f );
			device.Transform.Projection = matProj;

			// Set up the default texture states
			device.TextureState[0].ColorOperation = TextureOperation.Modulate;
			device.TextureState[0].ColorArgument1 = TextureArgument.TextureColor;
			device.TextureState[0].ColorArgument2 = TextureArgument.Diffuse;
			device.TextureState[0].AlphaOperation = TextureOperation.SelectArg1;
			device.TextureState[0].AlphaArgument1 = TextureArgument.TextureColor;
			device.SamplerState[0].MinFilter = TextureFilter.Linear;
			device.SamplerState[0].MagFilter = TextureFilter.Linear;
			device.SamplerState[0].MipFilter = TextureFilter.Linear;
			device.SamplerState[0].AddressU = TextureAddress.Clamp;
			device.SamplerState[0].AddressV = TextureAddress.Clamp;

			device.RenderState.DitherEnable = true;
		}




		/// <summary>
		/// Called when the app is exiting, or the device is being changed, this 
		/// function deletes any device-dependent objects.
		/// </summary>
		protected override void DeleteDeviceObjects(System.Object sender, System.EventArgs e)
		{
			m_Engine.Dispose();
			m_Console.Dispose();
		}

		public void LoadOptions()
		{
			try
			{
				System.Random rand = new System.Random();
				// loading of options will happen here
				m_Engine.SetTerrain(200,200,@"..\..\Resources\heightmap.jpg",@"..\..\Resources\sand1.jpg", 10.0f, 0.45f);

				for ( int i=0; i<150; i++ )
				{
					float north = (float)(rand.NextDouble() * 1900.0);
					float east  = (float)(rand.NextDouble() * 1900.0);
					BillBoard.Add( east, north, 0.0f, "cactus"+i, @"..\..\Resources\cactus.dds",1.0f, 1.0f);
				}
				for ( int i=0; i<150; i++ )
				{
					float north = (float)(rand.NextDouble() * 1900.0);
					float east  = (float)(rand.NextDouble() * 1900.0);
					BillBoard.Add( east, north, 0.0f, "tree"+i, @"..\..\Resources\palmtree.dds",6.5f, 10.0f);
				}
				GameEngine.Console.AddLine("all trees loaded");

				//			m_Engine.AddObject( new ParticleGenerator("Spray1", 2000, 2000, Color.Yellow, "Particle.bmp", new ParticleUpdate(Gravity)));

				double j = 0.0;
				double center_x = 1000.0;
				double center_z = 1000.0;
				double radius = 700.0;
				double width = 20.0;

				m_flag = new Cloth("flag", @"..\..\Resources\flag.jpg", 2, 2, 0.1, 1.0f);
				m_flag.Height = 0.6f;
				m_flag.North = 2.0f;
				m_flag.East = 0.1f;
				Cloth.EastWind = -3.0f;

				for ( double i=0.0; i<360.0; i += 1.5 )
				{
					float north = (float)(center_z + Math.Cos(i/180.0*Math.PI) * radius 
						);
					float east  = (float)(center_x + Math.Sin(i/180.0*Math.PI) * radius 
						);
					BillBoard.Add( east, north, 0.0f, "redpost"+(int)(i*2), @"..\..\Resources\redpost.dds",0.25f, 1.0f);
					j += 5.0;
					if ( j > 360.0 ) j -= 360.0;
				}
			
				j = 0.0;
				for ( double i=0.5; i<360.0; i += 1.5 )
				{
					float north = (float)(center_z + Math.Cos(i/180.0*Math.PI) * (radius+width) 
						);
					float east  = (float)(center_x + Math.Sin(i/180.0*Math.PI) * (radius+width) 
						);
					BillBoard.Add( east, north, 0.0f, "bluepost"+(int)(i*2), @"..\..\Resources\bluepost.dds",0.25f, 1.0f);
					j += 5.0;
					if ( j >= 360.0 ) j -= 360.0;
				}

				m_ownship = new Ownship(this, "car1", @"..\..\Resources\SprintRacer.x", new Vector3(0.0f, 0.8f, 0.0f), new Attitude(0.0f, (float)Math.PI, 0.0f));
				m_ownship.AddChild(m_flag);

				SoundEffect.Volume = 0.25f;

				m_Engine.AddObject( m_ownship );
/*				Opponent opp1 = new Opponent("car2", @"..\..\Resources\SprintRacer.x", new Vector3(0.0f, 0.8f, 0.0f), 
					new Attitude(0.0f, (float)Math.PI, 0.0f), "knowledge.xml");
				opp1.SetLOD( 10, 3000.0f );
				m_opponents.Add( opp1 );
				m_Engine.AddObject( opp1 );
*/
				m_ownship.North = 298.0f;
				m_ownship.East = 1000.0f;
				m_Engine.Cam.Attach(m_ownship, new Vector3(0.0f, 0.85f,-4.5f));
				m_Engine.Cam.LookAt(m_ownship);
				m_ownship.Heading = (float)Math.PI * 1.5f;
				m_ownship.SetLOD( 10, 3000.0f );

				//			Car car2 = (Car)m_Engine.GetObject("car2");
				//			car2.North = 295.0f;
				//			car2.East  = 1000.0f;
				//			car2.Height = 0.0f;
				//			car2.Heading = (float)Math.PI * 1.5f;
				//			car2.SetLOD( 10, 300.0f );
				//			car2.SetUpdateMethod( new ObjectUpdate(OpponentUpdate));

				GameEngine.GameLights.Ambient = Color.White;

				//			GameEngine.GameLights light1 = GameEngine.GameLights.AddPointLight( new Vector3(130.0f,35.0f,130.0f),Color.White,"light1");
				//					light1.Attenuation1 = 0.11f;
				//			GameEngine.GameLights light2 = GameEngine.GameLights.AddPointLight( new Vector3(40.0f,25.0f,40.0f),Color.White,"light2");
				//					light2.Attenuation1 = 0.11f;
				//			GameEngine.GameLights light3 = GameEngine.GameLights.AddPointLight( new Vector3(130.0f,35.0f,230.0f),Color.White,"light3");
				//					light3.Attenuation1 = 0.11f;
				//			GameEngine.GameLights light4 = GameEngine.GameLights.AddPointLight( new Vector3(230.0f,40.0f,230.0f),Color.White,"light4");
				//					light4.Attenuation1 = 0.11f;
				//			GameEngine.GameLights light5 = GameEngine.GameLights.AddPointLight( new Vector3(70.0f,25.0f,70.0f),Color.White,"light5");
				//					light5.Attenuation1 = 0.11f;
				GameEngine.GameLights headlights = GameEngine.GameLights.AddSpotLight(new Vector3(0.0f,0.0f,0.0f), 
					new Vector3(1.0f,0.0f,1.0f), Color.White, "headlight");
				headlights.EffectiveRange = 200.0f;
				headlights.Attenuation0 = 1.0f;
				headlights.Attenuation1 = 0.0f;
				headlights.InnerConeAngle = 1.0f;
				headlights.OuterConeAngle = 1.5f;
				headlights.PositionOffset = new Vector3(0.0f, 2.0f, 1.0f);
				headlights.DirectionOffset = new Vector3(0.0f, 0.00f, 1.0f);
				m_ownship.AddChild(headlights);
				headlights.Enabled = false;

				CGameEngine.FogColor = Color.Beige;
				CGameEngine.FogDensity = 0.5f;
				CGameEngine.FogEnable = true;
				CGameEngine.FogStart = 100.0f;
				CGameEngine.FogEnd = 900.0f;
				CGameEngine.FogTableMode = FogMode.Linear;
			}
			catch ( Exception e )
			{
				GameEngine.Console.AddLine("Exception");
				GameEngine.Console.AddLine(e.Message);
			}
		}

		public void Gravity( ref Particle Obj, float DeltaT )
		{
			Obj.m_Position   += Obj.m_Velocity * DeltaT;
			Obj.m_Velocity.Y  += -32.0f * DeltaT;
			if ( Obj.m_Position.Y < 0.0f ) Obj.m_bActive = false;
		}


		public void OwnshipUpdate( Object3D Obj, float DeltaT )
		{
		}

		public void OpponentUpdate( Object3D Obj, float DeltaT )
		{

			Obj.Height = CGameEngine.Ground.HeightOfTerrain(Obj.Position) + ((Model)Obj).Offset.Y;
		}

	}
}
