using System;
using System.Collections;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace GameEngine
{
	/// <summary>
	/// Summary description for Model.
	/// </summary>
	public class Model : Object3D, IDisposable, IDynamic
	{
		#region Attributes
		private Mesh m_mesh = null; // Our mesh object in sysmem
		private Material[] m_meshMaterials; // Materials for our m_mesh
		private Texture[] m_meshTextures; // Textures for our mesh
		private Vector3  m_vOffset = new Vector3(0.0f, 0.0f, 0.0f);
		private Attitude m_AttitudeOffset = new Attitude();
		private ProgressiveMesh[] m_pMeshes = null;          
		private int m_currentPmesh = 0;
		private int m_nNumLOD = 1;
		private float[] m_LODRanges = null;
		private float m_fMaxLODRange = 1.0f;
		private GraphicsStream m_adj = null;
		private Vector3 m_PositiveExtents = new Vector3(-1.0f,-1.0f,-1.0f);
		private Vector3 m_NegativeExtents = new Vector3(1.0f,1.0f,1.0f);
		private Vector3[] m_Corners = new Vector3[8];
		#endregion
		public Vector3 Offset { get { return m_vOffset; } }

		public Model(string name, string meshFile, Vector3 offset, Attitude adjust) : base(name)
		{
			Mesh pTempMesh = null;
			WeldEpsilons Epsilons = new WeldEpsilons();

			Vector3 objectCenter;        // Center of bounding sphere of object
			m_vOffset = offset;
			m_AttitudeOffset = adjust;
			m_vPosition.X = 100.0f;
			m_vPosition.Z = 100.0f;
			ExtendedMaterial[] materials = null;

			try 
			{
				// Load the m_mesh from the specified file
				m_mesh = Mesh.FromFile(meshFile, MeshFlags.SystemMemory, CGameEngine.Device3D,  out m_adj, out materials);
				// Lock the vertex buffer to generate a simple bounding sphere
				VertexBuffer vb = m_mesh.VertexBuffer;
				GraphicsStream vertexData = vb.Lock(0, 0, LockFlags.NoSystemLock);
				m_fRadius = Geometry.ComputeBoundingSphere(vertexData, m_mesh.NumberVertices, m_mesh.VertexFormat, out objectCenter);
				Geometry.ComputeBoundingBox(vertexData,m_mesh.NumberVertices, m_mesh.VertexFormat, out m_NegativeExtents, out m_PositiveExtents );
				vb.Unlock();
				vb.Dispose();

				m_vOffset.Y = -m_NegativeExtents.Y;

				m_Corners[0].X = m_NegativeExtents.X;
				m_Corners[0].Y = m_NegativeExtents.Y + m_vOffset.Y;
				m_Corners[0].Z = m_NegativeExtents.Z;
				
				m_Corners[1].X = m_PositiveExtents.X;
				m_Corners[1].Y = m_NegativeExtents.Y + m_vOffset.Y;
				m_Corners[1].Z = m_NegativeExtents.Z;
				
				m_Corners[2].X = m_NegativeExtents.X;
				m_Corners[2].Y = m_PositiveExtents.Y + m_vOffset.Y;
				m_Corners[2].Z = m_NegativeExtents.Z;
				
				m_Corners[3].X = m_PositiveExtents.X;
				m_Corners[3].Y = m_PositiveExtents.Y + m_vOffset.Y;
				m_Corners[3].Z = m_NegativeExtents.Z;
				
				m_Corners[4].X = m_NegativeExtents.X;
				m_Corners[4].Y = m_NegativeExtents.Y + m_vOffset.Y;
				m_Corners[4].Z = m_PositiveExtents.Z;
				
				m_Corners[5].X = m_PositiveExtents.X;
				m_Corners[5].Y = m_NegativeExtents.Y + m_vOffset.Y;
				m_Corners[5].Z = m_PositiveExtents.Z;
				
				m_Corners[6].X = m_PositiveExtents.X;
				m_Corners[6].Y = m_PositiveExtents.Y + m_vOffset.Y;
				m_Corners[6].Z = m_PositiveExtents.Z;
				
				m_Corners[7].X = m_PositiveExtents.X;
				m_Corners[7].Y = m_PositiveExtents.Y + m_vOffset.Y;
				m_Corners[7].Z = m_PositiveExtents.Z;
				
//					Console.AddLine("Max extents " + m_PositiveExtents);
//					Console.AddLine("Min extents " + m_NegativeExtents);


				// perform simple cleansing operations on m_mesh
				pTempMesh = Mesh.Clean(m_mesh, m_adj, m_adj);
				m_mesh.Dispose();

				m_mesh = pTempMesh;
				//  Perform a weld to try and remove excess vertices like the model bigship1.x in the DX9.0 SDK (current model is fixed)
				//    Weld the m_mesh using all epsilons of 0.0f.  A small epsilon like 1e-6 works well too
				m_mesh.WeldVertices( 0, Epsilons, m_adj, m_adj);
				// verify validity of m_mesh for simplification
				m_mesh.Validate(m_adj);

				CreateLod();

			}
			catch (DirectXException d3de)
			{
				Console.AddLine("Unable to load mesh " + meshFile);
				Console.AddLine(d3de.ErrorString);
			}
			catch ( Exception e )
			{
				Console.AddLine("Unable to load mesh " + meshFile);
				Console.AddLine(e.Message);
			}
			
			if (m_meshTextures == null && materials != null)
			{
				// We need to extract the material properties and texture names 
				m_meshTextures  = new Texture[materials.Length];
				m_meshMaterials = new Material[materials.Length];
				for( int i=0; i<materials.Length; i++ )
				{
					m_meshMaterials[i] = materials[i].Material3D;
					// Set the ambient color for the material (D3DX does not do this)
					m_meshMaterials[i].Ambient = m_meshMaterials[i].Diffuse;
     
					// Create the texture
					try
					{
						if ( materials[i].TextureFilename != null )
						{
							m_meshTextures[i] = TextureLoader.FromFile(CGameEngine.Device3D, @"..\..\Resources\" +materials[i].TextureFilename);
						}
					}
					catch (DirectXException d3de)
					{
						Console.AddLine("Unable to load texture " + materials[i].TextureFilename);
						Console.AddLine(d3de.ErrorString);
					}
					catch ( Exception e )
					{
						Console.AddLine("Unable to load texture " + materials[i].TextureFilename);
						Console.AddLine(e.Message);
					}
				}
			}
		}

		public void SetLOD( int numLOD, float MaxRange )
		{
			if ( numLOD < 1 ) numLOD = 1;

			m_nNumLOD = numLOD;
			m_fMaxLODRange = MaxRange;

			m_LODRanges = new float[numLOD];

			float rangeDelta = MaxRange / numLOD;

			for ( int i=0; i < numLOD; i++ )
			{
				m_LODRanges[i] = rangeDelta * ( i+1);
			}
			CreateLod();
		}

		private void CreateLod()
		{
			ProgressiveMesh pPMesh = null;
			int cVerticesMin = 0;
			int cVerticesMax = 0;
			int cVerticesPerMesh = 0;

			pPMesh = new ProgressiveMesh(m_mesh, m_adj, null, 1, MeshFlags.SimplifyVertex);

			cVerticesMin = pPMesh.MinVertices;
			cVerticesMax = pPMesh.MaxVertices;

			if ( m_pMeshes != null )
			{
				for (int iPMesh = 0; iPMesh < m_pMeshes.Length; iPMesh++)
				{
					m_pMeshes[iPMesh].Dispose();
				}
			}

			cVerticesPerMesh = (cVerticesMax - cVerticesMin) / m_nNumLOD;
			m_pMeshes = new ProgressiveMesh[m_nNumLOD];

			// clone all the separate m_pMeshes
			for (int iPMesh = 0; iPMesh < m_pMeshes.Length; iPMesh++)
			{
				m_pMeshes[m_pMeshes.Length - 1 - iPMesh] = pPMesh.Clone( MeshFlags.Managed | MeshFlags.VbShare, pPMesh.VertexFormat, CGameEngine.Device3D);
				// trim to appropriate space
				if ( m_nNumLOD > 1 )
				{
					m_pMeshes[m_pMeshes.Length - 1 - iPMesh].TrimByVertices(cVerticesMin + cVerticesPerMesh * iPMesh, cVerticesMin + cVerticesPerMesh * (iPMesh+1));
				}

				m_pMeshes[m_pMeshes.Length - 1 - iPMesh].OptimizeBaseLevelOfDetail(MeshFlags.OptimizeVertexCache);
			}
			m_currentPmesh = 0;
			m_pMeshes[m_currentPmesh].NumberVertices = cVerticesMax;
			pPMesh.Dispose();
		}
		public override bool InRect( Rectangle rect )
		{
			// check to see if the bounding circle around the model 
			// intersects this rectangle
			int center_x = (rect.Left + rect.Right)/2;
			int center_z = (rect.Top + rect.Bottom)/2;

			int delta_x = center_x - (int)m_vPosition.X;
			int delta_z = center_z - (int)m_vPosition.Z;
			int distance_squared = delta_x * delta_x + delta_z * delta_z;
			int combined_radius = (int)(m_fRadius * m_fRadius)+(rect.Width*rect.Width);
			bool bInside = distance_squared < combined_radius;
			return bInside;
		}

		Vector3 GetCorner( int index )
		{
			Vector3 WorldCorner = Vector3.TransformCoordinate(m_Corners[index],m_Matrix);
			return WorldCorner;
		}

		public override bool Collide( Object3D Other ) 
		{ 
			bool bCollide = false;

			if ( Visible )
			{
				Plane[] planeCollide;    // planes of the collide box
				Vector3[] WorldCorners = new Vector3[8];

				// perform bounding sphere collision test
				float delta_north = Other.North - North;
				float delta_east = Other.East - East;
				float distance_squared = delta_north * delta_north + delta_east * delta_east;
				float combined_radius = (Radius * Radius)+(Other.Radius * Other.Radius);
				bCollide = distance_squared < combined_radius;

				// if the bounding spheres are in contact perform a more precise collision test
				if ( bCollide )
				{
					planeCollide = new Plane[6];

					for( int i = 0; i < 8; i++ )
						WorldCorners[i] = Vector3.TransformCoordinate(m_Corners[i],m_Matrix);

					planeCollide[0] = Plane.FromPoints(WorldCorners[7],WorldCorners[3],WorldCorners[5]); // Right
					planeCollide[1] = Plane.FromPoints(WorldCorners[2],WorldCorners[6],WorldCorners[4]); // Left
					planeCollide[2] = Plane.FromPoints(WorldCorners[6],WorldCorners[7],WorldCorners[5]); // Far
					planeCollide[3] = Plane.FromPoints(WorldCorners[0],WorldCorners[1],WorldCorners[2]); // Near
					planeCollide[4] = Plane.FromPoints(WorldCorners[2],WorldCorners[3],WorldCorners[6]); // Top
					planeCollide[5] = Plane.FromPoints(WorldCorners[1],WorldCorners[0],WorldCorners[4]); // Bottom

					if ( Other.GetType() == typeof(Model) )
					{
						for( int i = 0; i < 8; i++ )
						{
							float distance;
							Vector3 testPoint = ((Model)Other).GetCorner(i);

							for( int iPlane = 0; iPlane < 6; iPlane++ )
							{
								distance = planeCollide[iPlane].Dot( testPoint );
								if ( distance > 0.0f ) bCollide = true;
							}
						}
					}
					else
					{
						float distance;
						Vector3 testPoint = Other.Position;
						testPoint.Y += 0.1f;

						for( int iPlane = 0; iPlane < 6; iPlane++ )  
						{
							distance = planeCollide[iPlane].Dot( testPoint );
							if ( distance > 0.0f ) 
							{
								bCollide = true;
							}
						}
						for( int i = 0; i < 8; i++ )
						{
							testPoint = Other.Position;

							float angle = ((float)Math.PI / 4) * i;

							testPoint.X += (float)Math.Cos(angle) * Other.Radius;
							testPoint.Y += 0.2f;
							testPoint.Z += (float)Math.Sin(angle) * Other.Radius;

							for( int iPlane = 0; iPlane < 6; iPlane++ ) 
							{
								distance = planeCollide[iPlane].Dot( testPoint );
								if ( distance > 0.0f ) 
								{
									bCollide = true;
								}
							}
						}
					}
				}
			}

			return bCollide; 
		}

		public override void Render( Camera cam )
		{
			if ( Visible )
			{
				Matrix world_matrix;

				// Meshes are divided into subsets, one for each material. 
				// Render them in a loop
				CGameEngine.Device3D.RenderState.CullMode = Microsoft.DirectX.Direct3D.Cull.CounterClockwise;

				if ( m_Parent != null )
				{
					world_matrix = Matrix.Multiply(m_Matrix, m_Parent.WorldMatrix);
				}
				else
				{
					world_matrix = m_Matrix;
				}

				CGameEngine.Device3D.Transform.World = world_matrix;

				for( int i=0; i<m_meshMaterials.Length; i++ )
				{
					// Set the material and texture for this subset
					CGameEngine.Device3D.Material = m_meshMaterials[i];
					CGameEngine.Device3D.SetTexture(0, m_meshTextures[i]);
        
					// Draw the m_mesh subset
					m_pMeshes[m_currentPmesh].DrawSubset(i);
				}

				if ( m_Children.Count > 0 )
				{
					Object3D obj;
					for ( int i=0; i<m_Children.Count; i++ )
					{
						obj = (Object3D)m_Children.GetByIndex(i);
						obj.Render( cam );
					}
				}
				Culled = true;
			}
		}

		public override void Update( float DeltaT )
		{
			if ( Visible )
			{
				try
				{
					if ( m_UpdateMethod != null ) 
					{
						m_UpdateMethod( (Object3D)this, DeltaT );
					}
					m_Matrix = Matrix.Identity;
					m_Matrix = Matrix.RotationYawPitchRoll(Heading+m_AttitudeOffset.Heading,
						Pitch+m_AttitudeOffset.Pitch,Roll+m_AttitudeOffset.Roll);
					Matrix temp = Matrix.Translation(m_vPosition);
					m_Matrix.Multiply(temp);

					// determine the proper LOD index based on range from the camera
					int index = m_nNumLOD;
					for ( int i = 0; i < m_nNumLOD; i++ )
					{
						if ( Range < m_LODRanges[i] )
						{
							index = i;
							break;
						}
					}
					if ( index >= m_nNumLOD ) index = m_nNumLOD-1;
					m_currentPmesh = index;

					if ( m_bHasMoved && m_Quads.Count > 0 )
					{
						Quad q = (Quad)m_Quads[0];
						q.Update( this );
					}
				}
				catch (DirectXException d3de)
				{
					Console.AddLine("Unable to update a Model " + Name);
					Console.AddLine(d3de.ErrorString);
				}
				catch ( Exception e )
				{
					Console.AddLine("Unable to update a Model " + Name);
					Console.AddLine(e.Message);
				}

				if ( m_Children.Count > 0 )
				{
					Object3D obj;
					for ( int i=0; i<m_Children.Count; i++ )
					{
						obj = (Object3D)m_Children.GetByIndex(i);
						obj.Update( DeltaT );
					}
				}
			}
		}

		public override void Dispose()
		{
			m_mesh.Dispose();

			if ( m_Children.Count > 0 )
			{
				Object3D obj;
				for ( int i=0; i<m_Children.Count; i++ )
				{
					obj = (Object3D)m_Children.GetByIndex(i);
					obj.Dispose();
				}
			}
			if ( m_pMeshes != null )
			{
				for (int iPMesh = 0; iPMesh < m_pMeshes.Length; iPMesh++)
				{
					m_pMeshes[iPMesh].Dispose();
				}
			}

			if (m_meshTextures != null)
			{
				for( int i=0; i<m_meshMaterials.Length; i++ )
				{
					// Create the texture
					m_meshTextures[i].Dispose();
				}
			}
			base.Dispose();
		}
	}
}
