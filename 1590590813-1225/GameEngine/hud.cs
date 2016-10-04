using System;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace GameEngine
{
	/// <summary>
	/// Summary description for hud.
	/// </summary>
	public class Hud : IDisposable
	{
		/// <summary>
		/// Custom vertex type for the HudPoint
		/// </summary>
		public struct HUDPOINTVERTEX
		{
			public Vector3 p;
			public float rhw;
			public int color;

			public const VertexFormats Fvf = VertexFormats.PositionW | VertexFormats.Diffuse;
		};

		private CustomVertex.TransformedColored[]  m_Points;   
		private VertexBuffer m_VB = null;  // Vertex buffer
		private int m_xSize;
		private int m_ySize;
		private int m_numPoints;

		public Hud(int xSize, int ySize)
		{
			m_xSize = xSize;
			m_ySize = ySize;
			m_numPoints = xSize * ySize;

			m_Points = new CustomVertex.TransformedColored[m_numPoints];

			for ( int i=0; i<m_xSize; i++ )
			{
				for ( int j=0; j<m_ySize; j++ )
				{
					m_Points[i+j*m_ySize].X = 200+i;
					m_Points[i+j*m_ySize].Y = 200+j;
					m_Points[i+j*m_ySize].Z = 0.0f;
					m_Points[i+j*m_ySize].Rhw = 1.0f;
					m_Points[i+j*m_ySize].Color = Color.FromArgb(128,255,255,255).ToArgb();
				}
			}

			m_VB = new VertexBuffer( typeof(CustomVertex.TransformedColored), m_numPoints, CGameEngine.Device3D,
				Usage.Dynamic, CustomVertex.TransformedColored.Format, Pool.Default );
		}

		public void SetData( int x, int y, int color)
		{
			if ( x < m_xSize && y < m_ySize )
			{
				m_Points[x+y*m_ySize].Color = color;
			}
		}

		public void Render()
		{
			m_VB.SetData(m_Points, 0, 0);
//			HUDPOINTVERTEX[] vertices = (HUDPOINTVERTEX[])m_VB.Lock(0, typeof(HUDPOINTVERTEX), LockFlags.Discard, m_numPoints);
//			for ( int i=0; i<m_numPoints; i++ )
//			{
//				vertices[i] = m_Points[i];
//			}
//			m_VB.Unlock();

			CGameEngine.Device3D.SetStreamSource( 0, m_VB, 0 );
			CGameEngine.Device3D.VertexFormat = HUDPOINTVERTEX.Fvf;

			// Set the texture
//			CGameEngine.Device3D.SetTexture(0, null );

			// Render the face
//			CGameEngine.Device3D.RenderState.ZWriteEnable = false;
			CGameEngine.Device3D.RenderState.AlphaBlendEnable = true;
			CGameEngine.Device3D.RenderState.SourceBlend = Blend.One;
			CGameEngine.Device3D.RenderState.DestinationBlend = Blend.One;
			CGameEngine.Device3D.RenderState.ShadeMode = ShadeMode.Flat;

			CGameEngine.Device3D.DrawPrimitives( PrimitiveType.PointList, 0, m_numPoints );

//			CGameEngine.Device3D.RenderState.ZWriteEnable = true;
			CGameEngine.Device3D.RenderState.AlphaBlendEnable = false;
		}

		public void Dispose()
		{
			m_VB.Dispose();
		}
	}
}
