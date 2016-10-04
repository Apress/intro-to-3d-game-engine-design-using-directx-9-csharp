using System;
using System.Collections;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace GameEngine
{
	/// <summary>
	/// The basic structure for building a Quadtree 
	/// </summary>
	public class Quad : IDisposable
	{
		private Rectangle  m_Bounds;
		private Quad       m_NorthEast = null;
		private Quad       m_NorthWest = null;
		private Quad       m_SouthWest = null;
		private Quad       m_SouthEast = null;
		private Quad       m_Parent    = null;
		private int        m_nLevel;
		private SortedList m_Objects;
		private float      m_fRadius;
		private Vector3    m_vPosition;
		private string     m_sName;
		private static Quad m_BaseQuad = null;

		public String Name { get { return m_sName; } }

		public Rectangle Bounds { get { return m_Bounds; } }

		public Quad(Rectangle bounds, int level, int maxlevel, Quad parent )
		{
			if ( m_BaseQuad == null )
			{
				m_BaseQuad = this;
			}

			m_Bounds = bounds;
			m_nLevel = level;
			m_Parent = parent;
			m_Objects = new SortedList();

			m_sName = "L" + level + ":X" + bounds.Left + "Y" + bounds.Top;

			m_vPosition.X = (bounds.Left + bounds.Right) / 2.0f;
			m_vPosition.Y = 0.0f;
			m_vPosition.Z = (bounds.Top + bounds.Bottom) / 2.0f;

			double dx = bounds.Width;
			double dz = bounds.Height;
			m_fRadius = (float)Math.Sqrt( dx * dx + dz * dz ) / 2.0f;

			if ( level < maxlevel )
			{
				int nHalfHeight = (int)dz / 2;
				int nHalfWidth = (int)dx / 2;
				m_NorthEast = new Quad ( 
					new Rectangle(bounds.Left + nHalfWidth, bounds.Top, nHalfWidth, nHalfHeight), 
					level+1, maxlevel, this );
				m_NorthWest = new Quad ( 
					new Rectangle(bounds.Left, bounds.Top, nHalfWidth, nHalfHeight), 
					level+1, maxlevel, this);
				m_SouthWest = new Quad ( 
					new Rectangle(bounds.Left, bounds.Top + nHalfHeight, nHalfWidth, nHalfHeight), 
					level+1, maxlevel, this);
				m_SouthEast = new Quad ( 
					new Rectangle(bounds.Left + nHalfWidth, bounds.Top + nHalfHeight, nHalfWidth, nHalfHeight), 
					level+1, maxlevel, this);
			}
		}

		public void AddObject( Object3D obj )
		{
			try
			{
				if ( obj != null )
				{
					if ( obj.InRect( m_Bounds ) )
					{
						int nIndex = m_Objects.IndexOfKey( obj.Name );
						try
						{
							if ( nIndex < 0 )  // add object if we don't have it yet
							{
								m_Objects.Add(obj.Name, obj );
								obj.m_Quads.Add(this);
//								if ( !obj.Name.StartsWith("Quad") && !obj.Name.StartsWith("tree") && !obj.Name.StartsWith("red")&& !obj.Name.StartsWith("blue"))
//								{
//									Console.AddLine(obj.Name + " added to " + Name );
//								}
								if ( m_NorthEast != null && obj.InRect( m_NorthEast.Bounds ) )
								{
									m_NorthEast.AddObject( obj );
								}
								if ( m_NorthWest != null && obj.InRect( m_NorthWest.Bounds ) )
								{
									m_NorthWest.AddObject( obj );
								}
								if ( m_SouthWest != null && obj.InRect( m_SouthWest.Bounds ) )
								{
									m_SouthWest.AddObject( obj );
								}
								if ( m_SouthEast != null && obj.InRect( m_SouthEast.Bounds ) )
								{
									m_SouthEast.AddObject( obj );
								}
							}
							else
							{
								//							Console.AddLine("Attempt to add another " + obj.Name );
							}
						}
						catch (DirectXException d3de)
						{
							Console.AddLine("Unable to add object" );
							Console.AddLine(d3de.ErrorString);
						}
						catch ( Exception e )
						{
							Console.AddLine("Unable to add object" );
							Console.AddLine(e.Message);
						}
					}
					else
					{
						int nIndex = m_Objects.IndexOfKey( obj.Name );
						if ( nIndex >= 0 )  // remove the  object if we have it 
						{
							RemoveObject( obj );
							if ( m_Parent != null )
							{
								m_Parent.AddObject( obj );
							}
						}
					}
				}
			}
			catch
			{
				Console.AddLine("fails in Quad AddObject");
			}
		}

		public void RemoveObject( Object3D obj )
		{
			try
			{
				if ( obj != null ) 
				{
					int nIndex = m_Objects.IndexOfKey( obj.Name );
					if ( nIndex >= 0 )
					{
						try
						{
							m_Objects.Remove( obj.Name );
						}
						catch
						{
							Console.AddLine("failing while removing object from quad object list" );
						}
						try
						{
							if ( obj.m_Quads.Count > 0 )
							{
								obj.m_Quads.Clear();
							}
						}
						catch
						{
							Console.AddLine("failing while clearing objects quad list");
						}
						if ( m_NorthEast != null )
						{
							m_NorthEast.RemoveObject( obj );
						}
						if ( m_NorthWest != null )
						{
							m_NorthWest.RemoveObject( obj );
						}
						if ( m_SouthWest != null )
						{
							m_SouthWest.RemoveObject( obj );
						}
						if ( m_SouthEast != null )
						{
							m_SouthEast.RemoveObject( obj );
						}
					}
				}
			}
			catch (DirectXException d3de)
			{
				Console.AddLine("Unable to remove object" );
				Console.AddLine(d3de.ErrorString);
			}
			catch ( Exception e )
			{
				Console.AddLine("Unable to remove object" );
				Console.AddLine(e.Message);
			}
		}

		public void Cull( Camera cam ) 
		{ 
			Object3D obj;
			int i;

			cam.Reset();

			if ( m_Objects.Count > 0 )
			{

				try
				{
					switch ( cam.CheckFrustum( m_vPosition, m_fRadius ) )
					{
						case Camera.CullState.AllInside:
							for ( i = 0; i < m_Objects.Count; i++ )  
							{
								obj = (Object3D)m_Objects.GetByIndex(i);
								obj.Range = cam.GetDistance( obj );
								obj.Culled = false;
								m_Objects.SetByIndex(i, obj);
								cam.AddVisibleObject( obj );
							}
							break;
						case Camera.CullState.AllOutside:
							if ( m_Parent == null ) // i.e. if this is the root quad
							{
								goto case Camera.CullState.PartiallyIn;
							}
							// do nothing since the default state is true (reset after each render)
							break;
						case Camera.CullState.PartiallyIn:
							if ( m_NorthEast != null ) 
							{
								m_NorthEast.Cull( cam );
								m_NorthWest.Cull( cam );
								m_SouthWest.Cull( cam );
								m_SouthEast.Cull( cam );
							}
							else  // if partially in at the bottom level treat as in
							{
								for ( i = 0; i < m_Objects.Count; i++ )  
								{
									obj = (Object3D)m_Objects.GetByIndex(i);
									obj.Culled = false;
									m_Objects.SetByIndex(i, obj);
									cam.AddVisibleObject( obj );
								}
							}
							break;
					}
				}
				catch (DirectXException d3de)
				{
					Console.AddLine("Unable to cull object" );
					Console.AddLine(d3de.ErrorString);
				}
				catch ( Exception e )
				{
					Console.AddLine("Unable to cull object" );
					Console.AddLine(e.Message);
				}
			}
		}


		public void Update ( Object3D obj )
		{
			bool bResetNeeded = false;

			try
			{

				// only need to reset the quad for the object if it is no longer in one of the quads
				try
				{
					foreach ( Quad q in obj.m_Quads )
					{
						try
						{
							if ( !obj.InRect( q.m_Bounds ) )
							{
								bResetNeeded = true;
							}
						}
						catch
						{
							Console.AddLine("invalid quad in object quad list");
						}
					}
				}
				catch
				{
					Console.AddLine("fails in foreach");
				}
				try
				{
					if ( bResetNeeded )
					{
						m_BaseQuad.RemoveObject( obj );
						m_BaseQuad.AddObject( obj );
					}
				}
				catch
				{
					Console.AddLine("fails in reset needed");
				}
			}
			catch (DirectXException d3de)
			{
				Console.AddLine("Unable to update a Quad " + Name);
				Console.AddLine(d3de.ErrorString);
			}
			catch ( Exception e )
			{
				Console.AddLine("Unable to update a Quad " + Name);
				Console.AddLine(e.Message);
			}

		}

		public void Dispose()
		{
			if ( m_NorthEast != null ) m_NorthEast.Dispose();
			if ( m_NorthWest != null ) m_NorthWest.Dispose();
			if ( m_SouthWest != null ) m_SouthWest.Dispose();
			if ( m_SouthEast != null ) m_SouthEast.Dispose();
		}
	}
}
