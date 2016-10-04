// created on 10/22/2002 at 11:39 AM
using System;

namespace VehicleDynamics
{
	public class  Vector
	{

		public    Vector() {x=0.0; y=0.0; z=0.0;}
		public    Vector ( Vector other ) {x=other.x; y=other.y; z=other.z;}
		public    Vector ( double new_x, double new_y, double new_z )
		{x=new_x; y=new_y; z=new_z;}
		~Vector () {}

		// Operator functions.
		public static bool operator <( Vector first, Vector second )  {return (first.x < second.x && first.y < second.y && first.z < second.z);}
		public static bool operator >( Vector first, Vector second )  {return (first.x > second.x && first.y > second.y && first.z > second.z);}
		public  static  bool operator ==(Vector first, Vector second )  {return first.x==second.x && first.y==second.y && first.z==second.z;}
		public  bool Equals( Vector second )  {return x==second.x && y==second.y && z==second.z;}
		public  static  bool operator !=(Vector first, Vector second )  {return first.x!=second.x || first.y!=second.y || first.z!=second.z;}

		public  static  Vector operator -(Vector first)  {return new Vector(-first.x, -first.y, -first.z);}
		public  static  Vector operator - (Vector first, Vector second)  {return new Vector(first.x - second.x, first.y - second.y, first.z - second.z);}
		public  static  Vector operator + (Vector first, Vector second)  {return new Vector(first.x + second.x, first.y + second.y, first.z + second.z);}
		public  static  Vector operator / (Vector first, float value)  {return new Vector(first.x/value, first.y/value, first.z/value);}
		public  static  Vector operator * (Vector first, float value)  {return new Vector(first.x*value, first.y*value, first.z*value);}
		public  static  double operator * (Vector first, Vector second)  {return (first.x*second.x + first.y*second.y + first.z*second.z);}
   
		// Accessor functions.
		public double X  { get {return x;} set { x = value; } }
		public double Y  { get {return y;} set { y = value; } }
		public double Z  { get {return z;} set { z = value; } }

		// Set functions.
		public    Vector Set (
			double new_x,
			double new_y,
			double new_z
			) {x= new_x; y= new_y; z= new_z; return this;}
		public    Vector SetX (
			double new_x
			) {x= new_x; return this;}
		public    Vector SetY (
			double new_y
			) {y= new_y; return this;}
		public    Vector SetZ (
			double new_z
			) {z= new_z; return this;}
		public    Vector SetToZero () {x= 0; y= 0; z= 0; return this;}

		// Limiting functions.
		public    Vector Limit (
			Vector value,
			Vector limit
			) 
		{
			return new Vector( Math.Min( limit.X, Math.Max( limit.X, value.X) ),
				   Math.Min( limit.Y, Math.Max( limit.Y, value.Y) ),
				   Math.Min( limit.Z, Math.Max( limit.Z, value.Z) ) );}

		public    Vector Limit (
			Vector limit
			) 
		{
			LimitX( limit.X );
			LimitY( limit.Y );
			LimitZ( limit.Z ); return this;}

		public    double LimitX (
			double min_value,
			double max_value
			)  {return Math.Min( max_value, Math.Max( min_value, x) );}
		public    double LimitX (
			double value
			)  {return Math.Min( value, Math.Max( -value, x) );}
		public    double LimitY (
			double min_value,
			double max_value
			)  {return Math.Min( max_value, Math.Max( min_value, y) );}
		public    double LimitY (
			double value
			)  {return Math.Min( value, Math.Max( -value, y) );}
		public    double LimitZ (
			double min_value,
			double max_value
			)  {return Math.Min( max_value, Math.Max( min_value, z) );}
		public    double LimitZ (
			double value
			)  {return Math.Min( value, Math.Max( -value, z) );}

		public    Vector Delta (
			Vector from
			)  {return new Vector(x - from.x, y - from.y, z - from.z);}
		public    double DeltaX (
			Vector from
			)  {return x - from.x;}
		public    double DeltaY (
			Vector from
			)  {return y - from.y;}
		public    double DeltaZ (
			Vector from
			)  {return z - from.z;}
		public    Vector ABSDelta (
			Vector from
			)  {return new Vector(Math.Abs(x - from.x), Math.Abs(y - from.y), Math.Abs(z - from.z));}
		public    double ABSDeltaX (
			Vector from
			)  {return Math.Abs(x - from.x);}
		public    double ABSDeltaY (
			Vector from
			)  {return Math.Abs(y - from.y);}
		public    double ABSDeltaZ (
			Vector from
			)  {return Math.Abs(z - from.z);}
		/*
		public    float XYDotProduct (
				 Vector other
				)  { return new Vector( *this ).SetZ( 0. ).DotProduct( Vector( other ).SetZ( 0. ) ); }

		public    float XZDotProduct (
				 Vector other
				)  { return new Vector( *this ).SetY( 0. ).DotProduct( Vector( other ).SetY( 0. ) ); }
		public    float YZDotProduct (
				 Vector other
				)  { return new Vector( *this ).SetX( 0. ).DotProduct( Vector( other ).SetX( 0. ) ); }
		*/

		protected double x;
		protected double y;
		protected double z;


		private double DistanceFrom ( Vector from ) 
		{
			return Math.Sqrt((x-from.x)*(x-from.x) + (y-from.y)*(y-from.y) + (z-from.z)*(z-from.z));

		} // end otherDistanceFrom


		///////////////////////////////////////////////////////////////////////////////
		public double otherXYDistanceFrom (
			Vector from
			) 

		{
			return Math.Sqrt((x-from.x)*(x-from.x) + (y-from.y)*(y-from.y));

		} // end otherXYDistanceFrom



		///////////////////////////////////////////////////////////////////////////////
		public double otherDistanceFrom () 
		{
			return Math.Sqrt( (x*x) + (y*y) + (z*z) );

		} // end otherDistanceFrom

		///////////////////////////////////////////////////////////////////////////////
		public double DistanceFrom ( )
		{
			return Math.Sqrt( (x*x) + (y*y) + (z*z) );

		} // end DistanceFrom


		///////////////////////////////////////////////////////////////////////////////
		public double XYDistanceFrom ( Vector from )
		{
			return Math.Sqrt((x-from.x)*(x-from.x) + (y-from.y)*(y-from.y));

		} // end XYDistanceFrom

		///////////////////////////////////////////////////////////////////////////////
		public double XYDistanceFrom ()
		{
			return Math.Sqrt( (x*x) + (y*y) );

		} // end XYDistanceFrom

		///////////////////////////////////////////////////////////////////////////////
		public double otherXYDistanceFrom () 
		{
			return Math.Sqrt( (x*x) + (y*y) );

		} // end otherXYDistanceFrom

		///////////////////////////////////////////////////////////////////////////////
		public Vector Normalize () 
		{
			float temp = (float)Math.Sqrt(x*x + y*y + z*z);
			if ( temp != 0.0f )
				return new Vector( x/temp, y/temp, z/temp );

			return new Vector (0.0f, 0.0f, 0.0f );

		} // end otherUnitNormal

		///////////////////////////////////////////////////////////////////////////////
		public double otherDotProduct ( Vector other ) 
		{
			double cosine_theta = 0.0;
			if ( !( other.x == 0.0 && other.y == 0.0 && other.z == 0.0 ) &&
				!( x == 0.0 && y == 0.0 && z == 0.0 ) ) 
			{
				double dist1= other.DistanceFrom();
				double dist2= DistanceFrom();
				cosine_theta= x*other.x + y*other.y + z*other.z / ( dist1 * dist2 );
			}

			return cosine_theta;

		} // end otherDotProduct


		///////////////////////////////////////////////////////////////////////////////
		Vector CrossProduct ( Vector that)
		{
			return new Vector(y*that.z - z*that.y, z*that.x - x*that.z, x*that.y - y*that.x);

		} // end CrossProduct

		///////////////////////////////////////////////////////////////////////////////
		public Vector otherCrossProduct (  Vector other ) 
		{
			return new Vector(y*other.z - z*other.y, z*other.x - x*other.z, x*other.y - y*other.x);

		} // end otherCrossProduct





		///////////////////////////////////////////////////////////////////////////////
		public double otherSinThetaCrossProduct (
			Vector other,
			Vector product
			) 
		{
			double sine_theta = 0.0;
			if ( !( other.x == 0.0 && other.y == 0.0 && other.z == 0.0 ) &&
				!( x == 0.0 && y == 0.0 && z == 0.0 ) ) 
			{
				double dist1= other.DistanceFrom();
				double dist2= DistanceFrom();
				product= other.CrossProduct(this);
				sine_theta= ( product.X - product.Y + product.Z ) / ( dist1 * dist2 ) ;
			}

			return sine_theta;

		} // end otherSinThetaCrossProduct





		///////////////////////////////////////////////////////////////////////////////
		public double otherElevBrg (
			Vector other
			) 

		{
			double xy_length= XYDistanceFrom( other );
			double elev_brg= Math.Atan2( DeltaZ( other ), xy_length );

			while ( elev_brg > Math.PI ) elev_brg -= Math.PI;
			while ( elev_brg < -Math.PI ) elev_brg += Math.PI;

			return elev_brg;

		} // end otherElevBrg





		///////////////////////////////////////////////////////////////////////////////
		public double otherRelBrg (
			Vector other
			) 

		{
			return Math.Atan2( Y-other.Y, X-other.X );

		} // end otherRelBrg


		///////////////////////////////////////////////////////////////////////////////
		bool otherIsParallelWith (
			Vector other
			) 

		{
			return CrossProduct(other) == new Vector(0.0f, 0.0f, 0.0f);

		} // end otherIsParallelWith





		///////////////////////////////////////////////////////////////////////////////
		/*
		 float otherDistanceFromLine (
			 Vector point0,
			 Vector point1
			) 

		{
			float distance= point0.DistanceFrom(point1);
			return (distance > 0.001)? ((point0 - point1).CrossProduct(this - point1).DistanceFrom() / distance) : DistanceFrom(point0);

		} // end otherDistanceFromLine
		*/

		public void IncrementX(double value)
		{
			x += value;
		}

		public void IncrementY(double value)
		{
			y += value;
		}

		public void IncrementZ(double value)
		{
			z += value;
		}

	};
}
