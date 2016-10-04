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
		private bool                m_bValid = false;
		private string              m_sName;
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

		public float m_fRate = 12.0f;  // particles to create per second
		public float m_fEmitVel = 17.5f;
		public float m_Radius = 0.1f;

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
				m_VB = new VertexBuffer( typeof(CustomVertex.PositionColored), m_Discard, 
					CGameEngine.Device3D,  Usage.Dynamic | Usage.WriteOnly | Usage.Points, 
					CustomVertex.PositionColored.Format, Pool.Default);
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
				m_Texture = D3DUtil.CreateTexture( CGameEngine.Device3D, m_sTexture, Format.Unknown);
				try
				{
					m_VB = new VertexBuffer( typeof(CustomVertex.PositionColored), m_Discard, 
						CGameEngine.Device3D,  Usage.Dynamic | Usage.WriteOnly | Usage.Points, 
						CustomVertex.PositionColored.Format, Pool.Default);
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
			m_bValid = other.m_bValid;
		}

		public override void Update( float DeltaT )
		{
			m_fTime += DeltaT;

			// Emit new particles
			int NumParticlesToEmit = (int)(DeltaT * m_fRate);
			if ( NumParticlesToEmit == 0 ) NumParticlesToEmit = 1;
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
				float fRand1 = ((float)rand.Next(int.MaxValue)/(float)int.MaxValue) * (float)Math.PI * 2.0f;
				float fRand2 = ((float)rand.Next(int.MaxValue)/(float)int.MaxValue) * (float)Math.PI * 0.25f;

				particle.m_InitialPosition = m_vPosition + new Vector3( 0.0f, m_Radius, 0.0f );

				particle.m_InitialVelocity = new Vector3( 0.0f, 0.0f, 0.0f );

				particle.m_InitialVelocity.X  = (float)Math.Cos(fRand1) * (float)Math.Sin(fRand2) * 2.5f;
				particle.m_InitialVelocity.Z  = (float)Math.Sin(fRand1) * (float)Math.Sin(fRand2) * 2.5f;
				particle.m_InitialVelocity.Y  = (float)Math.Cos(fRand2);
				particle.m_InitialVelocity.Y *= ((float)rand.Next(int.MaxValue)/(float)int.MaxValue) * m_fEmitVel;

				particle.m_Position = particle.m_InitialPosition;
				particle.m_Velocity = particle.m_InitialVelocity;

				particle.m_Color = m_Color;
				particle.m_fCreationTime     = m_fTime;
				particle.m_bActive = true;

				m_ActiveParticles.Add(particle);
				m_Particles++;
//				Console.AddLine(" added particle " + m_Particles);
			}
//			foreach ( Particle obj in m_ActiveParticles )
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
//				Console.AddLine( "particle" + i + " Y= " + p.m_Position.Y + " " + p.m_Velocity.Y);
			}
		}

		public override void Render( Camera cam )
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
				CGameEngine.Device3D.RenderState.PointSize = 0.08f;
				CGameEngine.Device3D.RenderState.PointSizeMin = 0.00f;
				CGameEngine.Device3D.RenderState.PointScaleA = 0.00f;
				CGameEngine.Device3D.RenderState.PointScaleB = 0.00f;
				CGameEngine.Device3D.RenderState.PointScaleC = 1.00f;

				CGameEngine.Device3D.VertexFormat = CustomVertex.PositionColoredTextured.Format;
				
				// Set up the vertex buffer to be rendered
				CGameEngine.Device3D.SetStreamSource( 0, m_VB, 0);
				CGameEngine.Device3D.VertexFormat = CustomVertex.PositionColored.Format;

				CustomVertex.PositionColored[] vertices = null;
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
				vertices = (CustomVertex.PositionColored[])m_VB.Lock(m_BaseParticle * DXHelp.GetTypeSize(typeof(CustomVertex.PositionColored)), typeof(CustomVertex.PositionColored), (m_BaseParticle != 0) ? LockFlags.NoOverWrite : LockFlags.Discard, m_Flush);
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

						CGameEngine.Device3D.DrawPrimitive(PrimitiveType.PointList, m_BaseParticle, numParticlesToRender);

						// Lock the next chunk of the vertex buffer.  If we are at the 
						// end of the vertex buffer, LockFlags.Discard the vertex buffer and start
						// at the beginning.  Otherwise, specify LockFlags.NoOverWrite, so we can
						// continue filling the VB while the previous chunk is drawing.
						m_BaseParticle += m_Flush;

						if(m_BaseParticle >= m_Discard)
							m_BaseParticle = 0;

						vertices = (CustomVertex.PositionColored[])m_VB.Lock(m_BaseParticle * DXHelp.GetTypeSize(typeof(CustomVertex.PositionColored)), typeof(CustomVertex.PositionColored), (m_BaseParticle != 0) ? LockFlags.NoOverWrite : LockFlags.Discard, m_Flush);
						count = 0;

						numParticlesToRender = 0;
					}

				}

				// Unlock the vertex buffer
				m_VB.Unlock();
				// Render any remaining particles
				if( numParticlesToRender > 0)
					CGameEngine.Device3D.DrawPrimitive(PrimitiveType.PointList, m_BaseParticle, numParticlesToRender );

				// Reset render states
				CGameEngine.Device3D.RenderState.PointSpriteEnable = false;
				CGameEngine.Device3D.RenderState.PointScaleEnable = false;

				CGameEngine.Device3D.RenderState.ZBufferWriteEnable = true;
				CGameEngine.Device3D.RenderState.AlphaBlendEnable = false;

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
