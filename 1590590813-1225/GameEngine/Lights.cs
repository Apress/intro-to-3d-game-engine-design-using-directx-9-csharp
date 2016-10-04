using System;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;
using System.Collections;

namespace GameEngine
{
	/// <summary>
	/// Summary description for GameLights.
	/// </summary>
	public class GameLights : Object3D, IDisposable, IDynamic, IComparable
	{
		#region Attributes
		private LightType m_Type = LightType.Point;
		private Vector3   m_Direction = new Vector3(0.0f,0.0f,0.0f);
		private Vector3   m_DirectionOffset = new Vector3(0.0f,0.0f,0.0f);
		private Vector3   m_PositionOffset = new Vector3(0.0f,0.0f,0.0f);
		private Color     m_Diffuse = Color.White;
		private Color     m_Specular = Color.White;
		private float	  m_EffectiveRange = 1000.0f;
		private float     m_Attenuation0 = 0.0f;
		private float     m_Attenuation1 = 1.0f;
		private float     m_Attenuation2 = 0.0f;
		private float     m_FallOff = 1.0f;
		private float     m_InnerConeAngle = 0.5f;
		private float     m_OuterConeAngle = 1.0f;
		private bool      m_Deferred = true;
		private bool      m_Enabled = true;

		// a static array that will hold all lights
		private static Color     m_Ambient = Color.White;
		private static ArrayList m_ActiveLights = new ArrayList();
		private static ArrayList m_InactiveLights = new ArrayList();
		private static int m_max_lights = 1;
		private static int m_num_activated = 0;
		#endregion

		#region Properties
		public LightType Type { get { return m_Type; } }
		public Vector3   Direction { get { return m_Direction; } set { m_Direction = value; }}
		public Vector3   DirectionOffset { get { return m_DirectionOffset; } set { m_DirectionOffset = value; }}
		public Vector3   PositionOffset { get { return m_PositionOffset; } set { m_PositionOffset = value; }}
		public Color     Diffuse { get { return m_Diffuse; } set { m_Diffuse = value; }}
		public Color     Specular { get { return m_Specular; } set { m_Specular = value; }}
		public float     EffectiveRange { get { return m_EffectiveRange; } set { m_EffectiveRange = value; }}
		public float     Attenuation0 { get { return m_Attenuation0; } set { m_Attenuation0 = value; }}
		public float     Attenuation1 { get { return m_Attenuation1; } set { m_Attenuation1 = value; }}
		public float     Attenuation2 { get { return m_Attenuation2; } set { m_Attenuation2 = value; }}
		public float     FallOff { get { return m_FallOff; } set { m_FallOff = value; }}
		public float     InnerConeAngle { get { return m_InnerConeAngle; } set { m_InnerConeAngle = value; }}
		public float     OuterConeAngle { get { return m_OuterConeAngle; } set { m_OuterConeAngle = value; }}
		public bool      Deferred { get { return m_Deferred; } set { m_Deferred = value; }}
		public bool Enabled 
		{
			get { return m_Enabled; }
			set 
			{
				m_Enabled = value;
				// remove from both list to ensure it does not get onto a list twice
				m_ActiveLights.Remove( this ); 
				m_InactiveLights.Remove( this );
				if ( m_Enabled ) // move from inactive list to active list
				{
					m_ActiveLights.Add( this );
				}
				else // move from active list to inactive list
				{
					m_InactiveLights.Add( this );
				}
			} 
		}

		public static Color Ambient { get { return m_Ambient; } set { m_Ambient = value; } }
		#endregion

		public GameLights(string name) :base(name)
		{
			m_EffectiveRange = 1000.0f;
			m_fRadius = m_EffectiveRange;
		}

		public int CompareTo( object other )
		{
			GameLights other_light = (GameLights)other;
			return (int)(Range - other_light.Range);
		}

		public static GameLights GetLight( string name )
		{
			GameLights light_found = null;
			foreach ( GameLights light in m_ActiveLights )
			{
				if ( light.Name == name )
				{
					light_found = light;
				}
			}
			foreach ( GameLights light in m_InactiveLights )
			{
				if ( light.Name == name )
				{
					light_found = light;
				}
			}
			return light_found;
		}

		public static GameLights AddDirectionalLight(Vector3 direction, Color color, string name)
		{
			GameLights light = new GameLights(name);
			light.m_Diffuse = color;
			light.m_Direction = direction;
			light.m_Type = LightType.Directional;
			m_ActiveLights.Add( light );
			return light;
		}

		public static GameLights AddPointLight(Vector3 position, Color color, string name)
		{
			GameLights light = new GameLights(name);
			light.m_Diffuse = color;
			light.Position = position;
			light.m_Type = LightType.Point;
			m_ActiveLights.Add( light );
			return light;
		}

		public static GameLights AddSpotLight(Vector3 position, Vector3 direction, Color color, string name)
		{
			GameLights light = new GameLights(name);
			light.m_Diffuse = color;
			light.m_Direction = direction;
			light.Position = position;
			light.m_Type = LightType.Spot;
			light.Attenuation0 = 0.0f;
			light.Attenuation1 = 1.0f;
			m_ActiveLights.Add( light );
			return light;
		}

		public static void InitializeLights()
		{
			m_max_lights = CGameEngine.Device3D.DeviceCaps.MaxActiveLights;
		}

		public static void DeactivateLights()
		{
			try
			{
				for ( int i=0; i< m_num_activated; i++ )
				{
					CGameEngine.Device3D.Lights[i].Enabled = false;
					CGameEngine.Device3D.Lights[i].Commit();
				}
			}
			catch (DirectXException d3de)
			{
				Console.AddLine("Unable to Deactivate lights ");
				Console.AddLine(d3de.ErrorString);
			}
			catch ( Exception e )
			{
				Console.AddLine("Unable to Deactivate lights ");
				Console.AddLine(e.Message);
			}
		}

		public static void SetupLights()
		{
			int num_active_lights = 0;

			CGameEngine.Device3D.RenderState.Lighting = true;
			CGameEngine.Device3D.RenderState.Ambient = m_Ambient;
			CGameEngine.Device3D.RenderState.SpecularEnable = true;

			// sort lights to be in range order from closest to farthest

			m_ActiveLights.Sort();

			try
			{

				foreach ( GameLights light in m_ActiveLights )
				{
					if ( !light.IsCulled && num_active_lights < m_max_lights )
					{
						Light this_light = CGameEngine.Device3D.Lights[num_active_lights];
						this_light.Deferred = light.m_Deferred;
						this_light.Type = light.m_Type;
						this_light.Position = light.m_vPosition;
						this_light.Direction = light.m_Direction;
						this_light.Diffuse = light.m_Diffuse;
						this_light.Specular = light.m_Specular;
						this_light.Attenuation0 = light.m_Attenuation0;
						this_light.Attenuation1 = light.m_Attenuation1;
						this_light.Attenuation2 = light.m_Attenuation2;
						this_light.InnerConeAngle = light.m_InnerConeAngle;
						this_light.OuterConeAngle = light.m_OuterConeAngle;
						this_light.Range = light.m_EffectiveRange;
						this_light.Falloff = light.FallOff;
						this_light.Enabled = true;
						this_light.Commit();
						num_active_lights++;
					}
				}

				if ( m_num_activated > num_active_lights )
				{
					for ( int i=0; i< (m_num_activated - num_active_lights); i++ )
					{
						Light this_light = CGameEngine.Device3D.Lights[num_active_lights+i];
						this_light.Enabled = false;
						this_light.Commit();
					}
				}
				m_num_activated = num_active_lights;
			}
			catch (DirectXException d3de)
			{
				Console.AddLine("dx Unable to setup lights ");
				Console.AddLine(d3de.ErrorString);
			}
			catch ( Exception e )
			{
				Console.AddLine("Unable to setup lights " );
				Console.AddLine(e.Message);
			}
		}

		public static void CheckCulling ( Camera cam )
		{
			foreach ( GameLights light in m_ActiveLights )
			{
				if ( light.m_Type == LightType.Directional )
				{
					light.Culled = false;  // can't cull a directional light
					light.Range = 0.0f;
				}
				else
				{
					if ( cam.CheckFrustum( light ) != Camera.CullState.AllOutside )
					{
						light.Culled = false;

						// we want the absolute value of the range
						light.m_fRange = Math.Abs(light.m_fRange);
					}
					else
					{
						light.Culled = true;  
						light.Range = 1000000000.0f;  // big range to sort to end of list
					}
				}
			}
		}

		public override void Update( float DeltaT )
		{
			m_fRadius = m_EffectiveRange;

			if ( m_Parent != null )
			{
				Matrix matrix = Matrix.Identity;

				matrix.RotateYawPitchRoll(m_Parent.Heading,
					m_Parent.Pitch,m_Parent.Roll);
				Vector3 pos_offset = Vector3.TransformCoordinate(m_PositionOffset,matrix);
//				Console.AddLine("light offset " + pos_offset);
				m_vPosition = m_Parent.Position + pos_offset;
				m_Direction.X = (float)Math.Sin(m_Parent.Attitude.Heading);
				m_Direction.Y = (float)Math.Sin(m_Parent.Attitude.Pitch);
				m_Direction.Z = (float)Math.Cos(m_Parent.Attitude.Heading);
				m_Direction += Vector3.TransformCoordinate(m_DirectionOffset,matrix);
			}
		}
	}
}
