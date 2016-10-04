using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace GameEngine
{
	/// <summary>
	/// delegate used for specifying the update method for the object 
	/// </summary>
	public delegate void ObjectUpdate( Object3D Obj, float DeltaT );

	/// <summary>
	/// Summary description for Object3D.
	/// </summary>
	abstract public class Object3D : IDisposable, IRenderable, ICullable, ICollidable, IDynamic
	{

	#region Attributes
		protected string     m_sName;
		protected Vector3    m_vPosition;
		protected Vector3    m_vVelocity;
		protected Attitude   m_vOrientation;
		protected bool       m_bVisible = true; // visible by default
		protected bool       m_bCulled;
		protected bool		 m_bHasMoved = false;
		protected Object3D   m_Parent;
		protected SortedList m_Children = new SortedList();
		protected float      m_fRadius;  // bounding circle
		protected float      m_fRange;  // distance from view point
		protected Matrix     m_Matrix;
		protected ObjectUpdate m_UpdateMethod = null;

		public ArrayList   m_Quads = new ArrayList();

		public string  Name     { get { return m_sName; } }
		public Vector3 Position { get { return m_vPosition; }   set { m_vPosition = value; m_bHasMoved = true;} }
		public Vector3 Velocity { get { return m_vVelocity; }   set { m_vVelocity = value; } }
		public float VelocityX { get { return m_vVelocity.X; }   set { m_vVelocity.X = value; m_bHasMoved = true;} }
		public float VelocityY { get { return m_vVelocity.Y; }   set { m_vVelocity.Y = value; m_bHasMoved = true;} }
		public float VelocityZ { get { return m_vVelocity.Z; }   set { m_vVelocity.Z = value; m_bHasMoved = true;} }
		public Attitude Attitude { get { return m_vOrientation; }   set { m_vOrientation = value; } }
		public virtual float   North    { get { return m_vPosition.Z; } set { m_vPosition.Z = value; m_bHasMoved = true;} }
		public virtual float   East     { get { return m_vPosition.X; } set { m_vPosition.X = value; m_bHasMoved = true;} }
		public virtual float   Height   { get { return m_vPosition.Y; } set { m_vPosition.Y = value; m_bHasMoved = true;} }
		public virtual float   Roll     { get { return m_vOrientation.Roll; } set { m_vOrientation.Roll = value; } }
		public virtual float   Pitch    { get { return m_vOrientation.Pitch; } set { m_vOrientation.Pitch = value; } }
		public virtual float   Heading  { get { return m_vOrientation.Heading; } set { m_vOrientation.Heading = value; } }
		public float   Range    { get { return m_fRange; }      set { m_fRange = value; } }
		public float   Radius   { get { return m_fRadius; }     set { m_fRadius = value; } }
		public Matrix  WorldMatrix   { get { return m_Matrix; } }
		public bool Visible { get { return m_bVisible; } set { m_bVisible = value; } }
	#endregion

		public Object3D( string sName )
		{
			m_sName = sName;
			m_bCulled = false;
			m_bVisible = true;
			m_Matrix = Matrix.Identity;
		}

		public void SetUpdateMethod( ObjectUpdate method )
		{
			m_UpdateMethod = method;
		}

		public virtual bool InRect( Rectangle rect )
		{
			// check to see if the object is within this rectangle
			return false;
		}

		public virtual bool Collide( Object3D Other ) { return false; }

		public virtual void Render() { }

		public virtual void Dispose()
		{
			Debug.WriteLine("Disposing of " + Name + " in Object3D");
		}
		public virtual void Render( Camera cam ){}
		public virtual void Update( float DeltaT ){}
		public virtual bool Culled { set { m_bCulled = value; } }
		public virtual bool IsCulled { get { return m_bCulled; } }
		public Vector3 CenterOfMass { get { return m_vPosition; }}
		public float   BoundingRadius { get { return m_fRadius; } }
		public virtual bool CollideSphere ( Object3D other ){ return false; }
		public virtual bool CollidePolygon ( Vector3 Point1, Vector3 Point2, Vector3 Point3 ){ return false; }

		public void AddChild( Object3D child )
		{
			m_Children.Add(child.Name, child);
			child.m_Parent = this;
		}

		public void RemoveChild( string name )
		{
			Object3D obj = (Object3D)m_Children.GetByIndex(m_Children.IndexOfKey(name));
			obj.m_Parent = null;
			m_Children.Remove(name);
		}

		public Object3D GetChild( string name )
		{
			try 
			{
				return (Object3D)m_Children.GetByIndex(m_Children.IndexOfKey(name));
			}
			catch
			{
				return null;
			}
		}
	}
}
