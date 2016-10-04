using System;
using System.Collections;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace GameEngine
{
	/// <summary>
	/// Summary description for BillBoard.
	/// </summary>
	public class BillBoard : Object3D, IDisposable
	{
		#region Attributes
		private CustomVertex.PositionNormalTextured[]  m_Corners;   
		private Texture             m_Texture;    // image for face
		private bool                m_bValid = false;
		private string              m_sTexture;

		private static string       m_sLastTexture;
		private static VertexBuffer m_VB;
		private static Matrix m_TheMatrix;

		private static ArrayList m_List = new ArrayList();

		public static ArrayList  Objects { get { return m_List; } }


		public bool Valid { get { return m_bValid; } }
	#endregion

		private static void SetupMatrix( Camera cam )
		{
			// Set up a rotation matrix to orient the billboard towards the camera.
			Vector3 vDir = Vector3.Subtract(cam.LookAtVector, cam.EyeVector);
			if( vDir.X > 0.001f )
				m_TheMatrix = Matrix.RotationY( (float)(-Math.Atan(vDir.Z/vDir.X)+Math.PI/2) );
			else if( vDir.X < -0.001f )
				m_TheMatrix = Matrix.RotationY( (float)(-Math.Atan(vDir.Z/vDir.X)-Math.PI/2) );
			else 
			{
				if ( cam.Heading < 179.0f || cam.Heading > 181.0f )
					m_TheMatrix = Matrix.Identity;
				else 
					m_TheMatrix = Matrix.RotationY( (float)Math.PI );
			}
		}

		public static void Add( float north, float east, float altitude, string sName, 
			string sTextureName, float fWidth, float fHeight)
		{
			BillBoard obj;

			if ( sTextureName.CompareTo(m_sLastTexture) == 0 )
			{
				BillBoard lastObject = (BillBoard)m_List[m_List.Count-1];
				obj = new BillBoard(sName,lastObject);
			}
			else 
			{
				obj = new BillBoard(sName, sTextureName, fWidth, fHeight);
			}
			m_sLastTexture = sTextureName;

			float height = CGameEngine.Ground.TerrainHeight(east,north);

			if ( altitude < height )
			{
				altitude = height;
			}

			obj.m_vPosition.X = east;
			obj.m_vPosition.Y = altitude;
			obj.m_vPosition.Z = north;

			try 
			{

				m_List.Add( obj );

				CGameEngine.QuadTree.AddObject((Object3D)obj);
			}
			catch (DirectXException d3de)
			{
				Console.AddLine("Unable to add billboard " + sName);
				Console.AddLine(d3de.ErrorString);
			}
			catch ( Exception e )
			{
				Console.AddLine("Unable to add billboard " + sName);
				Console.AddLine(e.Message);
			}
		}

		public BillBoard(string sName, BillBoard other) : base( sName )
		{
			m_sName = sName;
			Copy(other);
		}
		public BillBoard(string sName, string sTextureName, float fWidth, float fHeight) : base( sName )
		{
			m_sName = sName;
			m_sTexture = sTextureName;

			m_fRadius = fWidth / 2.0f;

			// create the vertices for the box
			m_Corners = new CustomVertex.PositionNormalTextured[4]; 

			m_Corners[0].X = m_fRadius;  // upper left
			m_Corners[0].Y = fHeight;  // upper left
			m_Corners[0].Z = 0.0f;  // upper left
			m_Corners[0].Tu = 1.0f;
			m_Corners[0].Tv = 0.0f;
			m_Corners[0].Nx = 0.0f;
			m_Corners[0].Ny = 0.0f;
			m_Corners[0].Nz = -1.0f;
			m_Corners[1].X = -m_fRadius;  // upper right
			m_Corners[1].Y = fHeight;  // upper right
			m_Corners[1].Z = 0.0f;  // upper right
			m_Corners[1].Tu = 0.0f;
			m_Corners[1].Tv = 0.0f;
			m_Corners[1].Nx = 0.0f;
			m_Corners[1].Ny = 0.0f;
			m_Corners[1].Nz = -1.0f;
			m_Corners[2].X = m_fRadius;  // lower left
			m_Corners[2].Y = 0.0f;  // lower left
			m_Corners[2].Z = 0.0f;  // lower left
			m_Corners[2].Tu = 1.0f;
			m_Corners[2].Tv = 1.0f;
			m_Corners[2].Nx = 0.0f;
			m_Corners[2].Ny = 0.0f;
			m_Corners[2].Nz = -1.0f;
			m_Corners[3].X = -m_fRadius;  // lower right
			m_Corners[3].Y = 0.0f;  // lower right
			m_Corners[3].Z = 0.0f;  // lower right
			m_Corners[3].Tu = 0.0f;
			m_Corners[3].Tv = 1.0f;
			m_Corners[3].Nx = 0.0f;
			m_Corners[3].Ny = 0.0f;
			m_Corners[3].Nz = -1.0f;

			// load the texture for the face
			try
			{
				m_Texture = GraphicsUtility.CreateTexture(CGameEngine.Device3D, sTextureName); 
				m_bValid = true;
			}
			catch
			{
				Console.AddLine("Unable to create billboard texture for " + sName);
			}
			
		}

		private void Copy(BillBoard other)
		{
			m_sTexture = other.m_sTexture;

			m_fRadius = other.m_fRadius;

			// create the vertices for the box
			m_Corners = other.m_Corners; 

			m_bValid = true;
		}

		public static void RenderAll( Camera cam )
		{
			string currentTexture = "";

			if ( m_List.Count > 0 )
			{
				CGameEngine.Device3D.RenderState.CullMode = Microsoft.DirectX.Direct3D.Cull.Clockwise;

				// Set diffuse blending for alpha set in vertices.
				CGameEngine.Device3D.RenderState.AlphaBlendEnable = true;
				CGameEngine.Device3D.RenderState.SourceBlend = Blend.SourceAlpha;
				CGameEngine.Device3D.RenderState.DestinationBlend = Blend.InvSourceAlpha;

				// Enable alpha testing (skips pixels with less than a certain alpha.)
				if( CGameEngine.Device3D.DeviceCaps.AlphaCompareCaps.SupportsGreaterEqual )
				{
					CGameEngine.Device3D.RenderState.AlphaTestEnable = true;
					CGameEngine.Device3D.RenderState.ReferenceAlpha = 0x08;
					CGameEngine.Device3D.RenderState.AlphaFunction = Compare.GreaterEqual;
				}

				SetupMatrix( cam );

				CGameEngine.Device3D.VertexFormat = CustomVertex.PositionNormalTextured.Format;
				
				if ( m_VB == null )
				{
					m_VB = new VertexBuffer(typeof(CustomVertex.PositionNormalTextured), 
						4, CGameEngine.Device3D, Usage.WriteOnly, CustomVertex.PositionNormalTextured.Format, Pool.Default );
				}

				CGameEngine.Device3D.SetStreamSource( 0, m_VB, 0 );

				foreach ( BillBoard obj in m_List ) 
				{

					if ( currentTexture.CompareTo(obj.m_sTexture) != 0 )
					{
						m_VB.SetData(obj.m_Corners, 0, 0);

						// Set the texture
						CGameEngine.Device3D.SetTexture(0, obj.m_Texture );

						currentTexture = obj.m_sTexture;
					}

					obj.Render( cam );
				}

				foreach ( BillBoard obj in m_List ) 
				{
					obj.RenderChildren( cam );
				}


			}
		}

		public override void Render( Camera cam )
		{
			if ( Visible && m_bValid && !IsCulled ) 
			{

				// Translate the billboard into place
				m_TheMatrix.M41 = m_vPosition.X;
				m_TheMatrix.M42 = m_vPosition.Y;
				m_TheMatrix.M43 = m_vPosition.Z;
				CGameEngine.Device3D.Transform.World = m_TheMatrix;

				// Render the face
				CGameEngine.Device3D.DrawPrimitives( PrimitiveType.TriangleStrip, 0, 2 );
			}
		}

		public void RenderChildren( Camera cam )
		{
			if ( Visible && m_bValid && !IsCulled ) 
			{

				// Translate the billboard into place
				CGameEngine.Device3D.Transform.World = m_TheMatrix;

				Culled = true;
				if ( m_Children.Count > 0 )
				{
					Object3D obj;
					for ( int i=0; i<m_Children.Count; i++ )
					{
						obj = (Object3D)m_Children.GetByIndex(i);
						obj.Render( cam );
					}
				}
			}
		}

		public override bool InRect( Rectangle rect )
		{
			return rect.Contains( (int)m_vPosition.X, (int)m_vPosition.Z);
		}

		public override void Dispose()
		{
			m_Texture.Dispose();

			if ( m_VB != null )
			{
				m_VB.Dispose();
				m_VB = null;

			}
			if ( m_Children.Count > 0 )
			{
				Object3D obj;
				for ( int i=0; i<m_Children.Count; i++ )
				{
					obj = (Object3D)m_Children.GetByIndex(i);
					obj.Dispose();
				}
			}
		}
	}
}
