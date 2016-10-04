using System;
using System.Collections;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace GameEngine
{
	/// <summary>
	/// delegate used for specifying the update method for each 
	/// particle from a given generator
	/// </summary>
	public delegate void ParticleUpdate( ref Particle Obj, float DeltaT );

	/// <summary>
	/// data structure for a given particle
	/// </summary>
	public struct Particle
	{
		public Vector3 m_Position;
		public Vector3 m_Velocity;
		public float   m_fTimeRemaining;
		public Vector3 m_InitialPosition;
		public Vector3 m_InitialVelocity;
		public float   m_fCreationTime;
		public System.Drawing.Color m_Color;
		public bool m_bActive;
	}
	
	
	/// <summary>
	/// Summary description for ParticleGenerator.
	/// </summary>
	public class ParticleGenerator : Object3D, IDisposable, IDynamic
	{
		#region Attributes
		private bool m_bValid = false;
		private string m_sTexture;
		private Texture m_Texture;
		private int m_BaseParticle = 0;
		private int m_Flush = 0;
		private int m_Discard = 0;
		private int m_ParticlesLimit = 2000;
		private int m_Particles = 0;
		private Color m_Color;
		private float m_fTime = 0.0f;

		private VertexBuffer m_VB;
		private bool m_bActive = false;

		private ArrayList m_ActiveParticles = new ArrayList();
		private ArrayList m_FreeParticles = new ArrayList();
		private ParticleUpdate m_Method = null;
		private System.Random rand = new System.Random();

		public float m_fRate = 22.0f;  // particles to create per second
		public float m_fPartialParticles = 0.0f;
		public float m_fEmitVel = 7.5f;
		public Attitude m_Attitude;
		 // window around the pitch axis for distribution (radians)
		public float m_PitchWidth = 1.0f; 
		 // window around the heading axis for distribution (radians)
		public float m_HeadingWidth = 1.0f;
		public float m_PointSize = 0.02f;
		public float m_PointSizeMin = 0.00f;
		public float m_PointScaleA = 0.00f;
		public float m_PointScaleB = 0.00f;
		public float m_PointScaleC = 1.00f;

		public bool Valid { get { return m_bValid; } }
		public bool Active { set { m_bActive = value; } }
	#endregion

		/// </Summary>copy constructor<//Summary>
		public ParticleGenerator(string sName, ParticleGenerator other) : base( sName )
		{
			m_sName = sName;
			Copy(other);
			try
			{
				m_VB = new VertexBuffer( typeof(CustomVertex.PositionColoredTextured), m_Discard, 
					CGameEngine.Device3D,  Usage.Dynamic | Usage.WriteOnly | Usage.Points, 
					CustomVertex.PositionColoredTextured.Format, Pool.Default);
				m_bValid = true;

			}
			catch (DirectXException d3de)
			{
				Console.AddLine("Unable to create vertex buffer for " + m_sTexture);
				Console.AddLine(d3de.ErrorString);
			}
			catch ( Exception e )
			{
				Console.AddLine("Unable to create vertex buffer for " + m_sTexture);
				Console.AddLine(e.Message);
			}
		}
		
		/// </Summary>normal constructor<//Summary>
		public ParticleGenerator(string sName, int numFlush, int numDiscard, Color color, string sTextureName, ParticleUpdate method ) : base( sName )
		{
			m_sName = sName;
			m_Color = color;
			m_Flush = numFlush;
			m_Discard = numDiscard;
			m_sTexture = sTextureName;
			m_Method = method;
			try
			{
				m_Texture = GraphicsUtility.CreateTexture( CGameEngine.Device3D, m_sTexture, Format.Unknown);
				try
				{
					m_VB = new VertexBuffer( typeof(CustomVertex.PositionColoredTextured), m_Discard, 
						CGameEngine.Device3D,  Usage.Dynamic | Usage.WriteOnly | Usage.Points, 
						CustomVertex.PositionColoredTextured.Format, Pool.Default);
					m_bValid = true;

				}
				catch (DirectXException d3de)
				{
					Console.AddLine("Unable to create vertex buffer for " + m_sTexture);
					Console.AddLine(d3de.ErrorString);
				}
				catch ( Exception e )
				{
					Console.AddLine("Unable to create vertex buffer for " + m_sTexture);
					Console.AddLine(e.Message);
				}
			}
			catch (DirectXException d3de)
			{
				Console.AddLine("Unable to create texture " + m_sTexture);
				Console.AddLine(d3de.ErrorString);
			}
			catch ( Exception e )
			{
				Console.AddLine("Unable to create texture " + m_sTexture);
				Console.AddLine(e.Message);
			}

		}

		private void Copy(ParticleGenerator other)
		{
			m_sName = other.m_sName;
			m_Flush = other.m_Flush;
			m_Discard = other.m_Discard;
			m_sTexture = other.m_sTexture;
			m_Texture = other.m_Texture;
			m_Method = other.m_Method;
			m_fRate = other.m_fRate;  
			m_fEmitVel = other.m_fEmitVel;
			m_Attitude = other.m_Attitude;
			m_PitchWidth = other.m_PitchWidth; 
			m_HeadingWidth = other.m_HeadingWidth;
			m_PointSize = other.m_PointSize;
			m_PointSizeMin = other.m_PointSizeMin;
			m_PointScaleA = other.m_PointScaleA;
			m_PointScaleB = other.m_PointScaleB;
			m_PointScaleC = other.m_PointScaleC;
			m_bValid = other.m_bValid;
		}

		public override void Update( float DeltaT )
		{
			m_fTime += DeltaT;

			// Emit new particles
			float TotalNewParticles = (DeltaT * m_fRate) + m_fPartialParticles ;
			int NumParticlesToEmit = (int)TotalNewParticles;
			m_fPartialParticles = TotalNewParticles - NumParticlesToEmit;
			int particlesEmit = m_Particles + NumParticlesToEmit;
			while( m_Particles < m_ParticlesLimit && m_Particles < particlesEmit )
			{
				Particle particle;

				if( m_FreeParticles.Count > 0 )
				{
					particle = (Particle)m_FreeParticles[0];
					m_FreeParticles.RemoveAt(0);
				}
				else
				{
					particle = new Particle();
				}

				// Emit new particle
				float fRand1 = (float)(rand.NextDouble()-0.5) * m_PitchWidth;
				float fRand2 = (float)(rand.NextDouble()-0.5) * m_HeadingWidth;

				m_Matrix = Matrix.RotationYawPitchRoll( m_Attitude.Heading+fRand2, m_Attitude.Pitch+fRand1, 0.0f);
				
				Matrix TotalMatrix;
				
				if ( m_Parent != null )
				{
					TotalMatrix = Matrix.Multiply( m_Matrix, m_Parent.WorldMatrix );
				}
				else
				{
					TotalMatrix = m_Matrix;
				}

				particle.m_InitialVelocity = Vector3.TransformCoordinate( new Vector3( 0.0f, 0.0f, m_fEmitVel ),TotalMatrix);
				particle.m_InitialPosition = Vector3.TransformCoordinate(m_vPosition, TotalMatrix );

				particle.m_Position = particle.m_InitialPosition;
				particle.m_Velocity = particle.m_InitialVelocity;

				particle.m_Color = m_Color;
				particle.m_fCreationTime     = m_fTime;
				particle.m_bActive = true;

				m_ActiveParticles.Add(particle);
				m_Particles++;
			}
			for ( int i=0; i < m_ActiveParticles.Count; i++ )
			{
				Particle p = (Particle)m_ActiveParticles[i];
				m_Method( ref p, DeltaT );
				if ( p.m_bActive )
				{
					m_ActiveParticles[i] = p;
				}
				else
				{
					m_ActiveParticles.RemoveAt(i);
					m_FreeParticles.Add(p);
					m_Particles--;
				}
			}
		}

		public override void Render( Camera cam )
		{
			try
			{
				if ( m_ActiveParticles.Count > 0 )
				{
					// Set the render states for using point sprites
					CGameEngine.Device3D.RenderState.ZBufferWriteEnable = false;
					CGameEngine.Device3D.RenderState.AlphaBlendEnable = true;
					CGameEngine.Device3D.RenderState.SourceBlend = Blend.One;
					CGameEngine.Device3D.RenderState.DestinationBlend = Blend.One;

					CGameEngine.Device3D.SetTexture(0, m_Texture );

					CGameEngine.Device3D.RenderState.PointSpriteEnable = true;
					CGameEngine.Device3D.RenderState.PointScaleEnable = true ;
					CGameEngine.Device3D.RenderState.PointSize = m_PointSize;
					CGameEngine.Device3D.RenderState.PointSizeMin = m_PointSizeMin;
					CGameEngine.Device3D.RenderState.PointScaleA = m_PointScaleA;
					CGameEngine.Device3D.RenderState.PointScaleB = m_PointScaleB;
					CGameEngine.Device3D.RenderState.PointScaleC = m_PointScaleC;

					CGameEngine.Device3D.VertexFormat = CustomVertex.PositionColoredTextured.Format;
				
					// Set up the vertex buffer to be rendered
					CGameEngine.Device3D.SetStreamSource( 0, m_VB, 0);

					CustomVertex.PositionColoredTextured[] vertices = null;
					int numParticlesToRender = 0;

					// Lock the vertex buffer.  We fill the vertex buffer in small
					// chunks, using LockFlags.NoOverWrite.  When we are done filling
					// each chunk, we call DrawPrim, and lock the next chunk.  When
					// we run out of space in the vertex buffer, we start over at
					// the beginning, using LockFlags.Discard.

					m_BaseParticle += m_Flush;

					if(m_BaseParticle >= m_Discard)
						m_BaseParticle = 0;

					int count = 0;
					vertices = (CustomVertex.PositionColoredTextured[])m_VB.Lock(m_BaseParticle * DXHelp.GetTypeSize(typeof(CustomVertex.PositionColoredTextured)), typeof(CustomVertex.PositionColoredTextured), (m_BaseParticle != 0) ? LockFlags.NoOverwrite : LockFlags.Discard, m_Flush);
					foreach(Particle p in m_ActiveParticles)
					{
						vertices[count].X     = p.m_Position.X;
						vertices[count].Y     = p.m_Position.Y;
						vertices[count].Z     = p.m_Position.Z;
						vertices[count].Color = p.m_Color.ToArgb();
						count++;

						if( ++numParticlesToRender == m_Flush )
						{
							// Done filling this chunk of the vertex buffer.  Lets unlock and
							// draw this portion so we can begin filling the next chunk.

							m_VB.Unlock();

							CGameEngine.Device3D.DrawPrimitives(PrimitiveType.PointList, m_BaseParticle, numParticlesToRender);

							// Lock the next chunk of the vertex buffer.  If we are at the 
							// end of the vertex buffer, LockFlags.Discard the vertex buffer and start
							// at the beginning.  Otherwise, specify LockFlags.NoOverWrite, so we can
							// continue filling the VB while the previous chunk is drawing.
							m_BaseParticle += m_Flush;

							if(m_BaseParticle >= m_Discard)
								m_BaseParticle = 0;

							vertices = (CustomVertex.PositionColoredTextured[])m_VB.Lock(m_BaseParticle * DXHelp.GetTypeSize(typeof(CustomVertex.PositionColoredTextured)), typeof(CustomVertex.PositionColoredTextured), (m_BaseParticle != 0) ? LockFlags.NoOverwrite : LockFlags.Discard, m_Flush);
							count = 0;

							numParticlesToRender = 0;
						}

					}

					// Unlock the vertex buffer
					m_VB.Unlock();
					// Render any remaining particles
					if( numParticlesToRender > 0)
						CGameEngine.Device3D.DrawPrimitives(PrimitiveType.PointList, m_BaseParticle, numParticlesToRender );

					// Reset render states
					CGameEngine.Device3D.RenderState.PointSpriteEnable = false;
					CGameEngine.Device3D.RenderState.PointScaleEnable = false;

					CGameEngine.Device3D.RenderState.ZBufferWriteEnable = true;
					CGameEngine.Device3D.RenderState.AlphaBlendEnable = false;

				}
			}
			catch (DirectXException d3de)
			{
				Console.AddLine("Unable to Render Particles for " + Name);
				Console.AddLine(d3de.ErrorString);
			}
			catch ( Exception e )
			{
				Console.AddLine("Unable to Render Particles for " + Name);
				Console.AddLine(e.Message);
			}
		}

		public override bool InRect( Rectangle rect )
		{
			return rect.Contains( (int)m_vPosition.X, (int)m_vPosition.Z);
		}

		public override void Dispose()
		{
			/// <returns>nothing</returns>
			m_Texture.Dispose();

			if ( m_VB != null )
			{
				m_VB.Dispose();

			}
		}
	}
}
