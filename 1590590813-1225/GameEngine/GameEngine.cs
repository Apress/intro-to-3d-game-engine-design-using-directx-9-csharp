//-----------------------------------------------------------------------------
// File: GameEngine.cs
//
// Desc: GameEngine code for Chapter 1 of Introduction to 3D Game Engine Design.
//
//       This File contains the GameEngine class definition for Chapter 1.  You
//       will notice that at this point it consists of only a few stub methods
//       That provide no functionality yet.
//
// Copyright (c) 2002 Lynn T. Harrison All rights reserved.
//-----------------------------------------------------------------------------
using System;
using System.Threading;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;
using Microsoft.DirectX.Direct3D;


namespace GameEngine 
{
	/// <summary>
	/// Summary description for GameEngine.
	/// This is the definition of the 3D game engine developed as part of the bok
	/// Introduction to 3D Game Engine Design.
	/// </summary>
	#region delegates
		public delegate void BackgroundTask();
	#endregion

	public class CGameEngine : IDisposable
	{
		#region Attributes
		// A local reference to the DirectX device
		private static Microsoft.DirectX.Direct3D.Device m_pd3dDevice;
		private System.Windows.Forms.Form m_WinForm;
		private SplashScreen      m_SplashScreen  = null;
		private OptionScreen      m_OptionScreen  = null;
		private SkyBox            m_Skybox        = null;
		private Camera            m_Camera        = null;
		public static GameInput   m_GameInput     = null;
		private static Terrain    m_Terrain       = null;
		private static Quad       m_QuadTree      = null;
		private ArrayList         m_Objects = null;
		private ArrayList         m_Cameras = null;
		public float              fTimeLeft       = 0.0f;
		Thread			          m_threadTask	  = null;
		#endregion

		#region Properties

		public Camera Cam { get { return m_Camera; } }
		public ArrayList Objects { get { return m_Objects; } }
			public static Terrain Ground { get { return m_Terrain; } }
		public static Quad QuadTree { get { return m_QuadTree; } }
		public static Microsoft.DirectX.Direct3D.Device Device3D { get { return m_pd3dDevice; } }
		public static GameInput Inputs { get { return m_GameInput; } }
		public static Color FogColor { set { m_pd3dDevice.RenderState.FogColor = value; } }
		public static FogMode FogTableMode { set { m_pd3dDevice.RenderState.FogTableMode = value; } }
		public static FogMode FogVertexMode { set { m_pd3dDevice.RenderState.FogVertexMode = value; } }
		public static float FogDensity { set { m_pd3dDevice.RenderState.FogDensity = value; } }
		public static float FogStart { set { m_pd3dDevice.RenderState.FogStart = value; } }
		public static float FogEnd { set { m_pd3dDevice.RenderState.FogEnd = value; } }
		public static bool FogEnable { set { m_pd3dDevice.RenderState.FogEnable = value; } }
		#endregion

		int frame = 0;

		public void Dispose()
		{
			Debug.WriteLine("disposing of game engine objects");
			m_GameInput.Dispose();
			Debug.WriteLine("disposing of terrain");
			if ( m_Terrain != null ) m_Terrain.Dispose();
			Debug.WriteLine("disposing of skybox");
			m_Skybox.Dispose();
			Debug.WriteLine("disposing of quadtree");
			m_QuadTree.Dispose();
			Debug.WriteLine("disposing of splashscreen");
			if ( m_SplashScreen != null )
			{
				m_SplashScreen.Dispose();
			}
			Debug.WriteLine("disposing of optionscreen");
			if ( m_OptionScreen != null )
			{
				m_OptionScreen.Dispose();
			}
			Debug.WriteLine("number of objects="+m_Objects.Count);
			for ( int i=0; i < m_Objects.Count; i++ )
			{
				try
				{
					Object3D obj = (Object3D)m_Objects[i];
					Debug.WriteLine("calling dispose for " + obj.Name);
					obj.Dispose( );
				}
				catch
				{
				}
			}
			for ( int i=0; i < BillBoard.Objects.Count; i++ )
			{
				Object3D obj = (Object3D)BillBoard.Objects[i];
				obj.Dispose();
			}
		}

		public void RestoreSurfaces()
		{
			if ( m_SplashScreen != null )
			{
				m_SplashScreen.Restore();
			}
			if ( m_OptionScreen != null )
			{
				m_OptionScreen.Restore();
			}
		}

		public void SetOptionScreen( OptionScreen Screen )
		{
			m_OptionScreen = Screen;
		}

		/// <summary>
		///  Initial setup of the Game Engine
		/// </summary>
		/// <param name="pd3dDevice"></param>
		public void Initialize ( System.Windows.Forms.Form form, Microsoft.DirectX.Direct3D.Device pd3dDevice ) 
		{
			// capture a reference to the window handle
			m_WinForm = form;
			// For now just capture a reference to the DirectX device for future use
			m_pd3dDevice = pd3dDevice;

			m_GameInput = new GameInput( m_WinForm );

			m_Skybox = new SkyBox(@"..\..\Resources\Dunes_Front.tga", 
								  @"..\..\Resources\Dunes_Right.tga", 
								  @"..\..\Resources\Dunes_Back.tga", 
								  @"..\..\Resources\Dunes_Left.tga", 
								  @"..\..\Resources\Dunes_Top.tga",  
								  @"..\..\Resources\Dunes_Bottom.tga" );
			m_Camera = new Camera();
			m_Cameras = new ArrayList();
			m_Cameras.Add(m_Camera);

			m_Objects = new ArrayList();

			m_pd3dDevice.RenderState.Ambient = System.Drawing.Color.Gray;
			// Set light #0 to be a simple, faint grey directional light so 
			// the walls and floor are slightly different shades of grey
			m_pd3dDevice.RenderState.Lighting = true;  // was true

			GameLights.InitializeLights();

			//			SetDefaultStates(  );
		}

		/// <summary>
		///  Display a Splash Screen based on a suppplied bitmap filename
		/// </summary>
		/// <param name="sFileName"></param>
		public bool ShowSplash ( string sFileName, int nSeconds, BackgroundTask task ) 
		{
			bool bDone = false;

			if ( m_SplashScreen == null ) 
			{
				m_SplashScreen = new SplashScreen( sFileName, nSeconds);

				if ( task != null ) 
				{
					m_threadTask = new Thread(new ThreadStart(task) ); 
					m_threadTask.Name = "Game_backgroundTask";
					m_threadTask.Start();
				}
			}

			bDone = m_SplashScreen.Render();

			fTimeLeft = m_SplashScreen.fTimeLeft;

			if ( bDone ) 
			{
				m_SplashScreen.Dispose();
				m_SplashScreen = null;
			}

			return bDone;
		}

		/// <summary>
		///  Display the Options screen
		/// </summary>
		public void DoOptions ( ) 
		{
			if ( m_OptionScreen != null ) 
			{
				m_OptionScreen.SetMousePosition(m_GameInput.GetMousePoint().X, m_GameInput.GetMousePoint().Y, m_GameInput.IsMouseButtonDown(0) );
				m_OptionScreen.Render();

			}

		}

		/// <summary>
		///  Display the latest game frame
		/// </summary>
		public void Render (  ) 
		{
			m_Camera.Render();

			m_QuadTree.Cull( m_Camera );

			GameLights.CheckCulling( m_Camera );

			// test code
			Model ownship = (Model)GetObject("car1");
			if ( ownship != null && ownship.IsCulled )
			{
				Console.AddLine("ownship culled at " + ownship.North + " " + ownship.East + " H " + ownship.Heading );
			}

			GameLights.DeactivateLights();

			if ( m_Skybox != null )
			{
				m_Skybox.Render( m_Camera );
			}

			GameLights.SetupLights();

			if ( m_Terrain != null )
			{
				m_Terrain.Render( m_Camera );
			}

			BillBoard.RenderAll( m_Camera );

			foreach ( Object3D obj in m_Objects )
			{
				if ( !obj.IsCulled )
				{
					obj.Render( m_Camera );
				}
			}

		}

		void SetDefaultStates(  )
		{
			//
			// Set the recomended defaults by nVidia
			//

			m_pd3dDevice.RenderState.MultiSampleAntiAlias = true;  
			m_pd3dDevice.RenderState.MultiSampleMask = -1;

			m_pd3dDevice.RenderState.ColorWriteEnable = ColorWriteEnable.Alpha | ColorWriteEnable.Blue | 
				ColorWriteEnable.Green | ColorWriteEnable.Red;

			m_pd3dDevice.RenderState.AlphaBlendEnable = true;  
			m_pd3dDevice.RenderState.SourceBlend = Blend.One;
			m_pd3dDevice.RenderState.DestinationBlend = Blend.Zero;
			m_pd3dDevice.RenderState.BlendOperation = BlendOperation.Add;


			m_pd3dDevice.RenderState.AlphaTestEnable = true; // was false
			m_pd3dDevice.RenderState.AlphaFunction = Compare.Always; 
			//			m_pd3dDevice.RenderState.AlphaReference = 0; 

			m_pd3dDevice.RenderState.Lighting = false;  // was true
			m_pd3dDevice.RenderState.Ambient = Color.FromArgb(128,255,255,255);
			m_pd3dDevice.RenderState.SpecularEnable = false;
			m_pd3dDevice.RenderState.LocalViewer = true;
			m_pd3dDevice.RenderState.NormalizeNormals = false;
			m_pd3dDevice.RenderState.ColorVertex = true;

			m_pd3dDevice.RenderState.AmbientMaterialSource = ColorSource.Material;
			m_pd3dDevice.RenderState.DiffuseMaterialSource = ColorSource.Color1;
			m_pd3dDevice.RenderState.EmissiveMaterialSource = ColorSource.Material;
			m_pd3dDevice.RenderState.SpecularMaterialSource = ColorSource.Color2;

			m_pd3dDevice.RenderState.Clipping = true;
//			m_pd3dDevice.RenderState.ClipPlaneEnable = 0;
			m_pd3dDevice.RenderState.CullMode = Cull.CounterClockwise;

			m_pd3dDevice.RenderState.FillMode = FillMode.Solid;
			m_pd3dDevice.RenderState.ShadeMode = ShadeMode.Gouraud;
			m_pd3dDevice.RenderState.TextureFactor = 0;
//			m_pd3dDevice.RenderState.VertexBlend = VertexBlendFlags.Disable;
			m_pd3dDevice.RenderState.DitherEnable = false;
			m_pd3dDevice.RenderState.LastPixel = true;

			m_pd3dDevice.RenderState.FogEnable = false;
			m_pd3dDevice.RenderState.FogColor =	Color.FromArgb(0x00ff0000);
			m_pd3dDevice.RenderState.FogTableMode = Microsoft.DirectX.Direct3D.FogMode.None;
			m_pd3dDevice.RenderState.FogVertexMode = Microsoft.DirectX.Direct3D.FogMode.None;
			m_pd3dDevice.RenderState.RangeFogEnable = false;
			m_pd3dDevice.RenderState.FogDensity = 0;
			m_pd3dDevice.RenderState.FogStart = 0;

			m_pd3dDevice.RenderState.StencilEnable = false;
			m_pd3dDevice.RenderState.StencilFail = StencilOperation.Keep;
			m_pd3dDevice.RenderState.StencilFunction = Compare.Always;
			m_pd3dDevice.RenderState.StencilMask = -1;
			m_pd3dDevice.RenderState.StencilPass = StencilOperation.Keep;
			//			m_pd3dDevice.RenderState.StencilReference = 0;
			m_pd3dDevice.RenderState.StencilWriteMask = -1;
			//			m_pd3dDevice.RenderState.StencilZFail = StencilOperation.Keep;

			m_pd3dDevice.RenderState.Wrap0 = 0;
			m_pd3dDevice.RenderState.Wrap1 = 0;
			m_pd3dDevice.RenderState.Wrap2 = 0;
			m_pd3dDevice.RenderState.Wrap3 = 0;
			m_pd3dDevice.RenderState.Wrap4 = 0;
			m_pd3dDevice.RenderState.Wrap5 = 0;
			m_pd3dDevice.RenderState.Wrap6 = 0;
			m_pd3dDevice.RenderState.Wrap7 = 0;

			//			m_pd3dDevice.RenderState.ZEnable = ZB.True;
			//			m_pd3dDevice.RenderState.ZWriteEnable = true;
			//			m_pd3dDevice.RenderState.ZBias = 0;
			//			m_pd3dDevice.RenderState.ZFunction = Compare.LessEqual;

			for( int i=0; i<4; i++ )
			{
				//				m_pd3dDevice.SetTexture(i, null);
				//				m_pd3dDevice.SetTextureStageState(i, TextureStageState.TexCoordIndex, i);
				//				m_pd3dDevice.SetTextureStageState(i, TextureStageState.ColorArg0,		(int)TextureArgument.TextureColor);
				//				m_pd3dDevice.SetTextureStageState(i, TextureStageState.ColorArg1,		(int)TextureArgument.Diffuse);
				//				m_pd3dDevice.SetTextureStageState(i, TextureStageState.AlphaArg0,		(int)TextureArgument.TextureColor);
				//				m_pd3dDevice.SetTextureStageState(i, TextureStageState.AlphaArg1,		(int)TextureArgument.Diffuse);
				//				m_pd3dDevice.SetTextureStageState(i, TextureStageState.ColorOp, 		(int)TextureOp.Modulate);
				//				m_pd3dDevice.SetTextureStageState(i, TextureStageState.AlphaOp, 		(int)TextureOp.Modulate);

				//				m_pd3dDevice.SetTextureStageState(i, TextureStageState.TextureTransformFlags D3DTSS_MINFILTER,		D3DTEXF_LINEAR );
				//				m_pd3dDevice.SetTextureStageState(i, TextureStageStateD3DTSS_MAGFILTER,		D3DTEXF_LINEAR );

				/*				m_pd3dDevice.SetTextureStageState(i, TextureStageStateD3DTSS_MIPFILTER,		D3DTEXF_NONE );
								m_pd3dDevice.SetTextureStageState(i, TextureStageStateD3DTSS_BUMPENVMAT00,	0);
								m_pd3dDevice.SetTextureStageState(i, TextureStageStateD3DTSS_BUMPENVMAT01,	0);
								m_pd3dDevice.SetTextureStageState(i, TextureStageStateD3DTSS_BUMPENVMAT10,	0);
								m_pd3dDevice.SetTextureStageState(i, TextureStageStateD3DTSS_BUMPENVMAT11,	0);
								m_pd3dDevice.SetTextureStageState(i, TextureStageStateD3DTSS_ADDRESSU,		D3DTADDRESS_WRAP);
								m_pd3dDevice.SetTextureStageState(i, TextureStageStateD3DTSS_ADDRESSV,		D3DTADDRESS_WRAP);
								m_pd3dDevice.SetTextureStageState(i, TextureStageStateD3DTSS_ADDRESSW,		D3DTADDRESS_WRAP);
								m_pd3dDevice.SetTextureStageState(i, TextureStageStateD3DTSS_BORDERCOLOR,	0);
								m_pd3dDevice.SetTextureStageState(i, TextureStageStateD3DTSS_MIPMAPLODBIAS, 0);
								m_pd3dDevice.SetTextureStageState(i, TextureStageStateD3DTSS_MAXMIPLEVEL,	0);
								m_pd3dDevice.SetTextureStageState(i, TextureStageStateD3DTSS_MAXANISOTROPY, 1);
								m_pd3dDevice.SetTextureStageState(i, TextureStageStateD3DTSS_BUMPENVLSCALE, 0);
								m_pd3dDevice.SetTextureStageState(i, TextureStageStateD3DTSS_BUMPENVLOFFSET, 0);
								m_pd3dDevice.SetTextureStageState(i, TextureStageStateD3DTSS_TEXTURETRANSFORMFLAGS, D3DTTFF_DISABLE);
				*/
			}

//			m_pd3dDevice.SetTextureStageState(0, TextureStageState.ColorOperation,	(int)TextureOperation.Modulate);

			// Bad bad nVidia bad! hehe...
			//			m_pd3dDevice.RenderState D3DRS_CULLMODE, D3DCULL_CW );

			// Don't do alpha as a default
			m_pd3dDevice.RenderState.AlphaBlendEnable = true; 
			m_pd3dDevice.RenderState.SourceBlend = Blend.One; 
			m_pd3dDevice.RenderState.DestinationBlend = Blend.Zero; 

			// Set the ambient light
			//			ENG_SetAmbientLight( zcolor(128,128,128,255) );
		}
		/// <summary>
		///  Display the After Action Review Screen
		/// </summary>
		public void DoAfterActionReview (  ) 
		{
			// The implementation for this will be done in a later Chapter
		}

		/// <summary>
		///  Process mouse, keyboard and if appropriate, joystick inputs
		/// </summary>
		public void GetPlayerInputs (  ) 
		{
			m_GameInput.Poll();
		}

		/// <summary>
		///  Process any automated player artificial intelligence
		/// </summary>
		/// <param name="DeltaT"></param>
		public void DoAI ( float DeltaT ) 
		{
			// The implementation for this will be done in a later Chapter
		}

		/// <summary>
		///  Process any moving object dynamics
		/// </summary>
		/// <param name="DeltaT"></param>
		public void DoDynamics ( float DeltaT ) 
		{

			try
			{
				frame++;
				if ( frame > 30 )
				{
					bool los = m_Terrain.InLineOfSight(new Vector3(0.0f, 1.0f, 0.0f ), m_Camera.EyeVector);
					frame=0;
					//			Console.AddLine("los = " + los);
				}
				if ( m_Objects.Count > 0 )
				{
					foreach ( Object3D obj in m_Objects )
					{
						obj.Update( DeltaT );
					}
				}
			}
			catch (DirectXException d3de)
			{
				Console.AddLine("Unable to update an object " + m_Objects);
				Console.AddLine(d3de.ErrorString);
			}
			catch ( Exception e )
			{
				Console.AddLine("Unable to update an object " + m_Objects);
				Console.AddLine(e.Message);
			}
		}

		/// <summary>
		///  Process any multiplayer state sharing if applicable
		/// </summary>
		/// <param name="DeltaT"></param>
		public void DoNetworking ( float DeltaT ) 
		{
			// The implementation for this will be done in a later Chapter
		}

		public void MoveCamera( float x, float y, float z, float pitch, float roll, float heading )
		{
			m_Camera.AdjustHeading( heading );
			m_Camera.AdjustPitch( pitch );
//			m_Camera.AdjustRoll( roll );
			m_Camera.MoveCamera(x, y, z );

		}

		public void SetTerrain(int xSize, int ySize, string sName, string sTexture, float fSpacing, float fElevFactor)
		{
			Rectangle bounds = new Rectangle(0,0,(int)(xSize*fSpacing+0.9),(int)(ySize*fSpacing+0.9));
			m_QuadTree = new Quad(bounds, 0, 7, null);
			m_Terrain = new Terrain(xSize, ySize, sName, sTexture, fSpacing, fElevFactor);

		}

		public void AddObject( Object3D obj )
		{
			Debug.WriteLine("adding " + obj.Name + " to engine object list");
			m_QuadTree.AddObject(obj);
			m_Objects.Add(obj);
		}

		public Object3D GetObject ( string name )
		{
			Object3D obj = null;
			foreach ( Object3D o in m_Objects )
			{
				if ( o.Name == name )
				{
					obj = o;
				}
			}
			if ( obj == null )
			{
				foreach ( Object3D o in BillBoard.Objects )
				{
					if ( o.Name == name )
					{
						obj = o;
					}
				}
			}
			return obj;
		}

		public bool SetCamera ( string name )
		{
			bool success = false;
			foreach ( Camera c in m_Cameras )
			{
				if ( c.Name == name )
				{
					m_Camera = c;
					success = true;
				}
			}
			return success;
		}

		public void AddCamera( Camera cam )
		{
			m_Cameras.Add(cam);
		}

		public void RemoveCamera ( string name )
		{
			Camera cam = null;
			foreach ( Camera c in m_Cameras )
			{
				if ( c.Name == name )
				{
					cam = c;
					break;
				}
			}
			if ( cam != null ) m_Cameras.Remove(cam);
		}
	}
}
