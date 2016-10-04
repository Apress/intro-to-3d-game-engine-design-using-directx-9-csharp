using System;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace GameEngine
{
	public class TerrainQuad : Object3D, IDisposable
	{
		#region Attributes
		private CustomVertex.PositionNormalTextured[]  m_Corners;   
		private bool         m_bValid = false;
		public Vector3       m_Face1Normal;
		public Vector3       m_Face2Normal;

		public bool Valid { get { return m_bValid; } }
		public Vector3 FaceNormals { get { 
					Vector3 sum = Vector3.Add(m_Face1Normal, m_Face2Normal); 
					sum.Normalize(); 
					return sum; } }
	#endregion

		public TerrainQuad( string sName, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4 ) : base( sName )
		{
			m_sName = sName;

			// create the vertices for the box
			m_Corners = new CustomVertex.PositionNormalTextured[6]; 

			m_Corners[0].X = p3.X;  // nw
			m_Corners[0].Y = p3.Y;
			m_Corners[0].Z = p3.Z;
			m_Corners[0].Tu = 0.0f;
			m_Corners[0].Tv = 1.0f;
			m_Corners[1].X = p1.X;  // sw
			m_Corners[1].Y = p1.Y;
			m_Corners[1].Z = p1.Z;
			m_Corners[1].Tu = 0.0f;
			m_Corners[1].Tv = 0.0f;
			m_Corners[2].X = p4.X;  // ne
			m_Corners[2].Y = p4.Y;
			m_Corners[2].Z = p4.Z;
			m_Corners[2].Tu = 1.0f;
			m_Corners[2].Tv = 1.0f;
			m_Corners[3].X = p2.X;  // ne
			m_Corners[3].Y = p2.Y;
			m_Corners[3].Z = p2.Z;
			m_Corners[3].Tu = 1.0f;
			m_Corners[3].Tv = 0.0f;

			m_vPosition.X = (p4.X + p3.X) / 2.0f;
			m_vPosition.Y = (p1.Y + p2.Y + p3.Y + p4.Y) / 4.0f;
			m_vPosition.Z = (p1.Z + p3.Z) / 2.0f;
			double dx = p4.X - p3.X;
			double dz = p3.Z - p1.Z;
			m_fRadius = (float)Math.Sqrt( dx * dx + dz * dz ) / 2.0f;

			m_Face1Normal = GameMath.ComputeFaceNormal( 
				new Vector3(m_Corners[0].X,m_Corners[0].Y,m_Corners[0].Z),
				new Vector3(m_Corners[1].X,m_Corners[1].Y,m_Corners[1].Z),
				new Vector3(m_Corners[2].X,m_Corners[2].Y,m_Corners[2].Z) );
			m_Face2Normal = GameMath.ComputeFaceNormal( 
				new Vector3(m_Corners[1].X,m_Corners[1].Y,m_Corners[1].Z),
				new Vector3(m_Corners[3].X,m_Corners[3].Y,m_Corners[3].Z),
				new Vector3(m_Corners[2].X,m_Corners[2].Y,m_Corners[2].Z) );

			// default the vertex normals to the face normal value just in case
			m_Corners[0].SetNormal( m_Face1Normal );
			m_Corners[1].SetNormal( FaceNormals );
			m_Corners[2].SetNormal( FaceNormals );
			m_Corners[3].SetNormal( m_Face2Normal );
			m_Corners[4].SetNormal( FaceNormals );
			m_Corners[5].SetNormal( FaceNormals );

			m_Corners[4].X = m_Corners[2].X;
			m_Corners[4].Y = m_Corners[2].Y;
			m_Corners[4].Z = m_Corners[2].Z;
			m_Corners[4].Tu = m_Corners[2].Tu;
			m_Corners[4].Tv = m_Corners[2].Tv;
			m_Corners[5].X = m_Corners[1].X;
			m_Corners[5].Y = m_Corners[1].Y;
			m_Corners[5].Z = m_Corners[1].Z;
			m_Corners[5].Tu = m_Corners[1].Tu;
			m_Corners[5].Tv = m_Corners[1].Tv;

			m_bValid = true;

		}

		public void SetCornerNormal( int Corner, Vector3 Normal )
		{
			Normal.Normalize();
			m_Corners[Corner].SetNormal( Normal );
		}

		public override void Dispose()
		{
		}

		public int RenderQuad( int Offset, CustomVertex.PositionNormalTextured[] vertices )
		{
			int newOffset = Offset;

			if ( Valid && !IsCulled )
			{
				for ( int i=0; i<6; i++ )
				{
					vertices[Offset+i] = m_Corners[i];
				}
				newOffset += 6;
				Culled = true;
			}
			return newOffset;
		}

		public override bool InRect( Rectangle rect )
		{
			bool inside = false;

			// check to see if the object is within this rectangle by checking each corner
			for ( int i=0; i<4; i++ )
			{
				if ( rect.Contains((int)m_Corners[i].X,(int)m_Corners[i].Z) )
				{
					inside = true;
					break;
				}
			}
			return inside;
		}

	}
	/// <summary>
	/// Summary description for Terrain.
	/// </summary>
	public class Terrain : IDisposable, ITerrainInfo
	{

		private Vector3[,]       m_Elevations;
		private VertexBuffer m_VB = null;  // Vertex buffer
		private TerrainQuad[,] m_Quads = null;
		private int m_xSize;
		private int m_ySize;
		private Texture      m_Texture; // image for face
		private bool m_bValid = false;
		private CustomVertex.PositionNormalTextured[] m_Vertices;
		private float m_Spacing;

		public Terrain(int xSize, int ySize, string sName, string sTexture, float fSpacing, float fElevFactor)
		{
			int nTemp;

			m_Elevations = new Vector3[xSize,ySize];
			m_xSize = xSize-1;
			m_ySize = ySize-1;
			m_Quads = new TerrainQuad[m_xSize,m_ySize];
			m_Vertices = new CustomVertex.PositionNormalTextured[3000]; 
			m_Spacing = fSpacing;

			try
			{
				System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(sName);
				for ( int i=0; i<xSize; i++ )
				{
					for ( int j=0; j<ySize; j++ ) 
					{
						nTemp = bmp.GetPixel(i,j).ToArgb() & 0x000000ff;
						m_Elevations[i,j].X = i * fSpacing;
						m_Elevations[i,j].Z = j * fSpacing;
						m_Elevations[i,j].Y = nTemp * fElevFactor;
					}
				}
				bmp.Dispose();

			}
			catch (DirectXException d3de)
			{
				Console.AddLine("Unable to load terrain heightmap " + sName);
				Console.AddLine(d3de.ErrorString);
			}
			catch ( Exception e )
			{
				Console.AddLine("Unable to load terrain heightmap " + sName);
				Console.AddLine(e.Message);
			}
			try
			{
				for ( int i=0; i<m_xSize; i++ )
				{
					for ( int j=0; j<m_ySize; j++ ) 
					{
						string sQuadName = "Quad" + i + "-" + j;
						m_Quads[i,j] = new TerrainQuad(sQuadName, m_Elevations[i,j], m_Elevations[i+1,j], 
							                                      m_Elevations[i,j+1], m_Elevations[i+1,j+1]);
						CGameEngine.QuadTree.AddObject((Object3D)m_Quads[i,j]);
					}
				}
				Console.AddLine("Done creating quads");

			}
			catch (DirectXException d3de)
			{
				Console.AddLine("Unable to create quads " );
				Console.AddLine(d3de.ErrorString);
			}
			catch ( Exception e )
			{
				Console.AddLine("Unable to create quads " );
				Console.AddLine(e.Message);
			}
			for ( int i=1; i<m_xSize-1; i++ )
			{
				for ( int j=1; j<m_ySize-1; j++ ) 
				{
					// assign normals to each vertex
					Vector3 Normalsw = m_Quads[i,j].FaceNormals + m_Quads[i-1,j-1].FaceNormals + 
						m_Quads[i-1,j].FaceNormals + m_Quads[i,j-1].FaceNormals; 
					m_Quads[i,j].SetCornerNormal( 0, Normalsw );

					Vector3 Normalse = m_Quads[i,j].FaceNormals + m_Quads[i,j-1].FaceNormals + 
						m_Quads[i+1,j].FaceNormals + m_Quads[i+1,j-1].FaceNormals; 
					m_Quads[i,j].SetCornerNormal( 1, Normalse );

					Vector3 Normalnw = m_Quads[i,j].FaceNormals + m_Quads[i-1,j].FaceNormals + 
						m_Quads[i-1,j+1].FaceNormals + m_Quads[i,j+1].FaceNormals; 
					m_Quads[i,j].SetCornerNormal( 2, Normalnw );

					Vector3 Normalne = m_Quads[i,j].FaceNormals + m_Quads[i,j+1].FaceNormals + 
						m_Quads[i+1,j+1].FaceNormals + m_Quads[i+1,j].FaceNormals; 
					m_Quads[i,j].SetCornerNormal( 3, Normalne );

					}
				}

				try
				{
					m_Texture = GraphicsUtility.CreateTexture(CGameEngine.Device3D, sTexture); 
				}
				catch
				{
					Console.AddLine("Unable to create terrain texture using " + sTexture);
				}
			
				try
				{
					// Create a vertex buffer for rendering the terrain
					m_VB = new VertexBuffer( typeof(CustomVertex.PositionNormalTextured), 3000, CGameEngine.Device3D,
						Usage.WriteOnly, CustomVertex.PositionNormalTextured.Format, Pool.Default );
					m_bValid = true;
				}
				catch
				{
					Console.AddLine("Unable to create terrain vertex buffer");
				}
			

				Console.AddLine("terrain loaded");
		}

		public float HeightOfTerrain( Vector3 pos )
		{
			return TerrainHeight( pos.X, pos.Z );
		}

		public float TerrainHeight( float east, float north )
		{
			int x1 = (int)(east/m_Spacing);
			int x2 = (int)(east/m_Spacing) + 1;
			int z1 = (int)(north/m_Spacing);
			int z2 = (int)(north/m_Spacing) + 1;

			// interpolation between the corner elevations
			float height;

			float dx = (east - x1 * m_Spacing) / m_Spacing;
			float dy = (north - z1 * m_Spacing) / m_Spacing;
			height = m_Elevations[x1,z1].Y + dx * (m_Elevations[x2,z1].Y - m_Elevations[x1,z1].Y) +
				     dy * (m_Elevations[x1,z2].Y - m_Elevations[x1,z1].Y) + 
				     dx * dy * (m_Elevations[x1,z1].Y - m_Elevations[x2,z1].Y - m_Elevations[x1,z2].Y + m_Elevations[x2,z2].Y);
			return height;
		}

		public float HeightAboveTerrain( Vector3 Position )
		{
			return HeightOfTerrain( Position ) - Position.Y;
		}
		public bool InLineOfSight( Vector3 Position1, Vector3 Position2 )
		{
			bool los = true;
			float north;

			float dx = Position2.X - Position1.X;
			float dy = Position2.Y - Position1.Y;
			float dz = Position2.Z - Position1.Z;

			float dp = dz / dx;

			float dist = (float)Math.Sqrt(dx*dx + dz*dz);
			float de = dy / dist;

			float IncX = m_Spacing * 0.75f;
			float y = Position1.Y;
			float east = Position1.X;

			while ( east < Position2.X && los )
			{
				north = Position1.Z + ( east - Position1.X ) * dp;
				los = TerrainHeight(east, north ) <= y;
				east += IncX;
				y += (IncX*dp) * de;
			}

			return los;
		}

		public Attitude GetSlope( Vector3 Position, float Heading )
		{
			Attitude attitude = new Attitude();
			Matrix matrix = Matrix.Identity;

			matrix.RotateY(Heading);

			int x1 = (int)(Position.X/m_Spacing);
			int z1 = (int)(Position.Z/m_Spacing);

			Vector3 normal = m_Quads[x1,z1].FaceNormals;

			normal.TransformCoordinate(matrix);

			if ( normal.Z == 0.0f )
			{
				attitude.Pitch = 0.0f;
			}
			else 
			{
				attitude.Pitch = -(float)Math.Atan(normal.Y/normal.Z);
				if ( attitude.Pitch > 0.0 )
				{
					attitude.Pitch = (float)(Math.PI/2.0) - attitude.Pitch;
				}
				else
				{
					attitude.Pitch = -((float)(Math.PI/2.0) + attitude.Pitch);
				}
			}
			if ( attitude.Pitch > (Math.PI/4.0) || attitude.Pitch < -(Math.PI/4.0) )
			{
				Console.AddLine("Pitch " + attitude.Pitch*180.0/Math.PI + " " + normal.ToString());
			}

			if ( normal.X == 0.0f )
			{
				attitude.Roll = 0.0f;
			}
			else 
			{
				attitude.Roll = -(float)Math.Atan(normal.Y/normal.X);
				if ( attitude.Roll > 0.0 )
				{
					attitude.Roll = (float)(Math.PI/2.0) - attitude.Roll;
				}
				else
				{
					attitude.Roll = -((float)(Math.PI/2.0) + attitude.Roll);
				}
			}
			if ( attitude.Roll > (Math.PI/4.0) || attitude.Roll < -(Math.PI/4.0) )
			{
				Console.AddLine("Roll " + attitude.Roll*180.0/Math.PI + " " + normal.ToString());
			}


			attitude.Heading = Heading;

			return attitude;
		}

		public void Render( Camera cam )
		{
			int nQuadsDrawn = 0;
			if ( m_bValid ) 
			{
				CGameEngine.Device3D.RenderState.CullMode = Cull.Clockwise;
				CGameEngine.Device3D.VertexFormat = CustomVertex.PositionNormalTextured.Format;
				Material mtrl = new Material();
				mtrl.Ambient = Color.White;
				mtrl.Diffuse = Color.White;
				CGameEngine.Device3D.Material = mtrl;

				// Set the texture
				CGameEngine.Device3D.SetTexture(0, m_Texture );

				// Set the matrix for normal viewing
				Matrix matWorld = new Matrix();
				matWorld = Matrix.Identity;

				CGameEngine.Device3D.Transform.World = matWorld;
				CGameEngine.Device3D.Transform.View = cam.View;

				int Offset = 0;

				for ( int i=0; i<m_xSize; i++ )
				{
					for ( int j=0; j<m_ySize; j++ ) 
					{
						try
						{
							Offset = m_Quads[i,j].RenderQuad( Offset, m_Vertices );
							if ( Offset >= 2990 )
							{
								CGameEngine.Device3D.VertexFormat = CustomVertex.PositionNormalTextured.Format;
								m_VB.SetData(m_Vertices, 0, 0);
								CGameEngine.Device3D.SetStreamSource( 0, m_VB, 0 );
								CGameEngine.Device3D.DrawPrimitives( PrimitiveType.TriangleList, 0, Offset/3 );
								nQuadsDrawn += Offset / 6;
								Offset = 0;
							}
						}
						catch
						{
							Console.AddLine("Error rendering terrain quad " + i + "," + j);
						}
					}
				}
				if ( Offset > 0 )
				{
					try
					{
						CGameEngine.Device3D.VertexFormat = CustomVertex.PositionNormalTextured.Format;
						m_VB.SetData(m_Vertices, 0, 0);
						CGameEngine.Device3D.SetStreamSource( 0, m_VB, 0 );
						CGameEngine.Device3D.DrawPrimitives( PrimitiveType.TriangleList, 0, Offset/3 );
						nQuadsDrawn += Offset / 6;
						Offset = 0;
					}
					catch (DirectXException d3de)
					{
						Console.AddLine("Unable to render terrain " );
						Console.AddLine(d3de.ErrorString);
					}
					catch ( Exception e )
					{
						Console.AddLine("Unable to render terrain" );
						Console.AddLine(e.Message);
					}

				}
			}
		}

		public void Dispose()
		{
			for ( int i=0; i<m_xSize; i++ )
			{
				for ( int j=0; j<m_ySize; j++ ) 
				{
					if ( m_Quads[i,j] != null ) m_Quads[i,j].Dispose();
				}
			}
			if ( m_Texture != null ) m_Texture.Dispose();
			if ( m_VB != null ) m_VB.Dispose();

		}


	}
}
