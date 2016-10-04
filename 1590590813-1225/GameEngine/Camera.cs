using System;
using System.Collections;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace GameEngine
{
	/// <summary>
	/// Summary description for Camera.
	/// </summary>
	public class Camera
	{
		#region	// Cull State enumeration
		/// <summary>
		/// Each member of this enumeration is one possible culling result
		/// </summary>
		
		public enum CullState
		{
			/// <summary>
			/// The rectangle is completely within the viewing Frustum
			/// </summary>
			AllInside,	
			/// <summary>
			/// The rectangle is completely outside the viewing Frustum
			/// </summary>
			AllOutside,		
			/// <summary>
			/// The rectangle is partially within the viewing Frustum
			/// </summary>
			PartiallyIn,		
		}
	#endregion

		#region Attributes
		private Object3D  m_AttachedObject = null;
		private Object3D  m_LookAtObject = null;
		private Vector3   m_Offset;
		private Attitude  m_Attitude;
		private Matrix    m_matProj;
		private Matrix    m_matView;
		private float     m_X = 1.0f;
		private float     m_Y = 1.0f;
		private float     m_Z = 1.0f;
		private Vector3[] vecFrustum;    // corners of the view frustum
		private Plane[]   planeFrustum;    // planes of the view frustum
		private Vector3   m_Eye;
		private Vector3   m_LookAt;
		private string    m_name = "default";
		private float     m_fFOV = (float)Math.PI/4.0f;
		private float     m_fAspect = 1.33f;
		private float     m_fNearPlane = 1.0f;
		private float     m_fFarPlane = 800.0f;
		private ArrayList m_VisibleObjects = null;
		#endregion

		#region Properties
		public ArrayList VisibleObjects { get { return m_VisibleObjects; } }
		public float Heading { get { return (float)(m_Attitude.Heading*180.0/Math.PI); } }
		public float Pitch { get { return (float)(m_Attitude.Pitch*180.0/Math.PI); } }
		public string Name { get { return m_name; } }
		public float X { get { return m_X; } }
		public float Y { get { return m_Y; } }
		public float Z { get { return m_Z; } }
		public Matrix View { get { return m_matView; } }
		public Vector3 EyeVector { get { return m_Eye; } }
		public Vector3 LookAtVector { get { return m_LookAt; } }
		public float FieldOfView 
		{
			set 
			{ 
				m_fFOV = value;
				m_matProj = Matrix.PerspectiveFovLH( m_fFOV, m_fAspect, 
					m_fNearPlane, m_fFarPlane );
			} 
		}
		public float AspectRatio 
		{
			set 
			{ 
				m_fAspect = value;
				m_matProj = Matrix.PerspectiveFovLH( m_fFOV, m_fAspect, 
					m_fNearPlane, m_fFarPlane );
			} 
		}
		public float NearClipPlane 
		{
			set 
			{ 
				m_fNearPlane = value;
				m_matProj = Matrix.PerspectiveFovLH( m_fFOV, m_fAspect, 
					m_fNearPlane, m_fFarPlane );
			} 
		}
		public float FarClipPlane 
		{
			set 
			{ 
				m_fFarPlane = value;
				m_matProj = Matrix.PerspectiveFovLH( m_fFOV, m_fAspect, 
					m_fNearPlane, m_fFarPlane );
			} 
		}

		#endregion

		public Camera()
		{
			m_VisibleObjects = new ArrayList();
			m_matView = Matrix.Identity;
			m_Offset = new Vector3(0.0f, 0.0f, 0.0f);
			vecFrustum = new Vector3[8];    // corners of the view frustum
			planeFrustum = new Plane[6];    // planes of the view frustum
			m_matProj = Matrix.PerspectiveFovLH( m_fFOV, m_fAspect, 
				m_fNearPlane, m_fFarPlane );
		}

		public Camera(string name)
		{
			m_VisibleObjects = new ArrayList();
			m_matView = Matrix.Identity;
			m_Offset = new Vector3(0.0f, 0.0f, 0.0f);;
			vecFrustum = new Vector3[8];    // corners of the view frustum
			planeFrustum = new Plane[6];    // planes of the view frustum
			m_fFOV = (float)Math.PI/4.0f;
			m_fAspect = 1.0f;
			m_fNearPlane = .10f;
			m_fFarPlane = 800.0f;
			m_matProj = Matrix.PerspectiveFovLH( m_fFOV, m_fAspect, 
				m_fNearPlane, m_fFarPlane );
			m_name = name;
		}

		public void AdjustHeading( float deltaHeading )
		{
			m_Attitude.Heading += (deltaHeading * (float)Math.PI / 180.0f);

			if ( m_Attitude.Heading > (2.0f * Math.PI) )
			{
				m_Attitude.Heading -= (float)(2.0f * Math.PI);
			}

			if ( m_Attitude.Heading < 0.0f )
			{
				m_Attitude.Heading += (float)(2.0f * Math.PI);
			}

		}

		public void AdjustPitch( float deltaPitch )
		{
			m_Attitude.Pitch += (deltaPitch * (float)Math.PI / 180.0f);

			if ( m_Attitude.Pitch > (0.5f * Math.PI) )
			{
				m_Attitude.Pitch = (float)(0.5f * Math.PI);
			}

			if ( m_Attitude.Pitch < (-0.5f * Math.PI) )
			{
				m_Attitude.Pitch = (float)(-0.5f * Math.PI);
			}

		}

		public void MoveCamera( float x, float y, float z )
		{
			float ty;
			m_X += x * (float)Math.Cos(m_Attitude.Heading) + z * (float)Math.Sin(m_Attitude.Heading);
			m_Y += y;
			m_Z += z * (float)Math.Cos(m_Attitude.Heading) + x * (float)Math.Sin(m_Attitude.Heading);
			try
			{
				ty = CGameEngine.Ground.HeightOfTerrain(new Vector3(m_X, m_Y, m_Z));
			}
			catch {ty=0;}
			m_Y = ty + 1.0f;
		}

		public CullState CheckFrustum( Vector3 pos, float radius )
		{

			float distance;
			int count = 0;

			for( int iPlane = 0; iPlane < 4; iPlane++ )  // don't check against top and bottom
			{
				distance = planeFrustum[iPlane].Dot( pos );
				if( distance <= -radius )
				{
					return CullState.AllOutside;
				}
				if ( distance > radius ) count++;
			}

			if ( count == 4 ) return CullState.AllInside;

			return CullState.PartiallyIn;
		}

		public CullState CheckFrustum( Object3D obj )
		{

			float distance = 0.0f;
			int count = 0;

			for( int iPlane = 0; iPlane < 4; iPlane++ )  // don't check against top and bottom
			{
				distance = planeFrustum[iPlane].Dot( obj.Position );
				if( distance <= -obj.Radius )
				{
					return CullState.AllOutside;
				}
				if ( distance > obj.Radius ) count++;
			}

			if ( count == 4 ) return CullState.AllInside;

			return CullState.PartiallyIn;
		}

		public float GetDistance( Object3D obj )
		{
			return planeFrustum[3].Dot( obj.Position ) + m_fNearPlane;
		}

		public void Attach( Object3D parent, Vector3 offset )
		{
			m_AttachedObject = parent;
			m_Offset = offset;
		}

		public void LookAt( Object3D obj )
		{
			m_LookAtObject = obj;
		}

		public void Render()
		{
			Vector3 Up = new Vector3(0.0f, 1.0f, 0.0f);
			if ( m_AttachedObject != null ) 
			{
				if ( m_LookAtObject != null )
				{
					m_LookAt = m_LookAtObject.Position;
				}
				else
				{
					m_LookAt = m_AttachedObject.Position;
					m_LookAt.X += (float)Math.Sin(m_Attitude.Heading)*10.0f;
					m_LookAt.Y += (float)Math.Sin(m_Attitude.Pitch)*10.0f;
					m_LookAt.Z += (float)Math.Cos(m_Attitude.Heading)*10.0f;
				}
				Matrix transpose = Matrix.Identity;

				m_Attitude.Heading = Attitude.Aepc(m_AttachedObject.Heading);
				m_Attitude.Pitch = m_AttachedObject.Pitch;
				m_Attitude.Roll = 0.0f;
				transpose.RotateYawPitchRoll(m_Attitude.Heading,
					m_Attitude.Pitch,m_Attitude.Roll);

				m_Eye = m_AttachedObject.Position + 
					Vector3.TransformCoordinate(m_Offset, transpose);
			}
			else 
			{
				m_Eye = new Vector3( m_X, m_Y, m_Z);
				if ( m_LookAtObject != null )
				{
					m_LookAt = m_LookAtObject.Position;
				}
				else
				{
					m_LookAt = m_Eye;
					m_LookAt.X += (float)Math.Sin(m_Attitude.Heading)*10.0f;
					m_LookAt.Y += (float)Math.Sin(m_Attitude.Pitch)*10.0f;
					m_LookAt.Z += (float)Math.Cos(m_Attitude.Heading)*10.0f;
				}
			}
			// Set the app view matrix for normal viewing
			m_matView = Matrix.LookAtLH(m_Eye, m_LookAt, Up);

			CGameEngine.Device3D.Transform.View = m_matView;

			CGameEngine.Device3D.Transform.Projection = m_matProj;

			Matrix mat = Matrix.Multiply(m_matView, m_matProj);
			mat.Invert();

			vecFrustum[0] = new Vector3(-1.0f, -1.0f,  0.0f); // xyz
			vecFrustum[1] = new Vector3( 1.0f, -1.0f,  0.0f); // Xyz
			vecFrustum[2] = new Vector3(-1.0f,  1.0f,  0.0f); // xYz
			vecFrustum[3] = new Vector3( 1.0f,  1.0f,  0.0f); // XYz
			vecFrustum[4] = new Vector3(-1.0f, -1.0f,  1.0f); // xyZ
			vecFrustum[5] = new Vector3( 1.0f, -1.0f,  1.0f); // XyZ
			vecFrustum[6] = new Vector3(-1.0f,  1.0f,  1.0f); // xYZ
			vecFrustum[7] = new Vector3( 1.0f,  1.0f,  1.0f); // XYZ

			for( int i = 0; i < 8; i++ )
				vecFrustum[i] = Vector3.TransformCoordinate(vecFrustum[i],mat);

			planeFrustum[0] = Plane.FromPoints(vecFrustum[7],
				vecFrustum[3],vecFrustum[5]); // Right
			planeFrustum[1] = Plane.FromPoints(vecFrustum[2], 
				vecFrustum[6],vecFrustum[4]); // Left
			planeFrustum[2] = Plane.FromPoints(vecFrustum[6], 
				vecFrustum[7],vecFrustum[5]); // Far
			planeFrustum[3] = Plane.FromPoints(vecFrustum[0], 
				vecFrustum[1],vecFrustum[2]); // Near
			planeFrustum[4] = Plane.FromPoints(vecFrustum[2], 
				vecFrustum[3],vecFrustum[6]); // Top
			planeFrustum[5] = Plane.FromPoints(vecFrustum[1], 
				vecFrustum[0],vecFrustum[4]); // Bottom
		}

		public void Reset()
		{
			m_VisibleObjects.Clear();
		}

		public void AddVisibleObject( Object3D obj )
		{
			if ( ! (obj is TerrainQuad) )
			{
				m_VisibleObjects.Add( obj );
			}
		}

	}
}
