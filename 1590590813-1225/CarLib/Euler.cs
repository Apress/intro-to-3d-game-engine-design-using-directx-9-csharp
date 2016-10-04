// created on 10/22/2002 at 2:12 PM
using System;

namespace VehicleDynamics
{
	public class Euler
	{

		private  double	psi;
		private  double	theta;
		private  double	phi;
		private  double	cpsi;
		private  double	ctheta;
		private  double	cphi;
		private  double	spsi;
		private  double	stheta;
		private  double	sphi;
		private	 double[,]	mat = new double[3,3];
		private	 bool	matrix_current = false;

		// The default class constructor
		public    Euler () {psi = 0.0; theta=0.0; phi=0.0;} 

		// A constructor which accepts three floats defining the angles
		public    Euler (									
			double x_psi,
			double x_theta,
			double x_phi
			) {Psi = x_psi; Theta=x_theta; Phi=x_phi;}

		// A copy constructor
		public    Euler (									
			Euler x_angle
			) {psi = x_angle.Psi; theta=x_angle.Theta; phi=x_angle.Phi;}

		// The class destructor
		~Euler () {}

		// Calculates the difference between two copies of the class.
		public    Euler Delta (								
			Euler from
			)  { return new Euler( psi - from.psi, theta - from.theta, phi - from.phi ); }

		public    double DeltaPsi (
			Euler from
			)  { return psi - from.psi; }

		public    double DeltaTheta (
			Euler from
			)  { return theta - from.theta; }

		public    double DeltaPhi (
			Euler from
			)  { return phi - from.phi; }

		public    Euler ABSDelta (
			Euler from
			)  { return new Euler( Math.Abs(psi - from.psi), Math.Abs(theta - from.theta), Math.Abs(phi - from.phi) ); }

		public    double ABSDeltaPsi (
			Euler from
			)  { return Math.Abs(psi - from.psi); }

		public    double ABSDeltaTheta (
			Euler from
			)  { return Math.Abs(theta - from.theta); }

		public    double ABSDeltaPhi (
			Euler from
			)  { return Math.Abs(phi - from.phi); }

		/*    // Operator functions.
		public     bool operator< (
				 Euler other
				)  {return psi < other.psi && theta < other.theta && phi < other.phi;}
		public     bool operator== (
				Euler that
			   )  {return psi==that.psi && theta==that.theta && phi==that.phi;}

		public     Euler operator - ()  {return Euler(-psi, -theta, -phi);}
		public     Euler operator - (
		public        Euler that
			   )  {return Euler(psi - that.psi, theta - that.theta, phi - that.phi);}
		public    Euler operator -= (
				Euler that
			   ) {psi-= that.psi; theta-= that.theta; phi-= that.phi; return *this;}
		public     Euler operator + (
				Euler that
			   )  {return Euler(psi + that.psi, theta + that.theta, phi + that.phi);}
		public    Euler operator += (
				Euler that
			   ) {psi+= that.psi; theta+= that.theta; phi+= that.phi; return *this;}
		public     Euler operator / (
				float value
			   )  {return Euler(psi/value, theta/value, phi/value);}
		public    Euler operator /= (
				float value
			   ) {psi/= value; theta/= value; phi/= value; return *this;}
		public     Euler operator * (
				float that
			   )  {return Euler(psi*that, theta*that, phi*that);}
		public    Euler operator *= (
				float that
			   ) {psi*= that; theta*= that; phi*= that; return *this;}

		public     Euler operator + (
				Vector that
			   )  {return Euler(psi + that.Z(), theta + that.Y(), phi + that.X());}
		*/
		public  static  Euler operator * (Euler first, float value)  {return new Euler(first.psi*value, first.theta*value, first.phi*value);}
		public  static  Euler operator + (Euler first, Euler second)  {return new Euler(first.psi+second.psi, first.theta+second.theta, first.phi+second.phi);}
		// Accessor functions.
		public     double Psi { get {return psi;} set { matrix_current = psi == value && matrix_current; psi=value;/*psi = AEPCPI(value); */cpsi=(float)Math.Cos(psi); spsi=(float)Math.Sin(psi); } }
		public     double Theta { get {return theta;} set {matrix_current = theta == value && matrix_current; theta = AEPCPI(value); ctheta=(float)Math.Cos(theta); stheta=(float)Math.Sin(theta); } }
		public     double Phi { get {return phi;} set {matrix_current = phi == value && matrix_current; phi = AEPCPI(value); cphi=(float)Math.Cos(phi); sphi=(float)Math.Sin(phi); } }
		public     double cosPsi ()  {return cpsi;}
		public     double cosTheta ()  {return ctheta;}
		public     double cosPhi ()  {return cphi;}
		public     double sinPsi ()  {return spsi;}
		public     double sinTheta ()  {return stheta;}
		public     double sinPhi ()  {return sphi;}
		public     double PsiAsDegrees ()  {return (psi*180.0/Math.PI);}
		public     double ThetaAsDegrees ()  {return (theta*180.0/Math.PI);}
		public     double PhiAsDegrees ()  {return (phi*180.0/Math.PI);}

		// Set functions.
		public    Euler SetToZero () {psi= 0; theta= 0; phi= 0; return this;}

		public    Euler SetPsiAsDegrees (
			double x_psi
			) {Psi = (x_psi*Math.PI/180.0); return this;}
		public    Euler SetThetaAsDegrees (
			double x_theta
			) {Theta = (x_theta*Math.PI/180.0); return this;}
		public    Euler SetPhiAsDegrees (
			double x_phi
			) {Phi = (x_phi*Math.PI/180.0); return this;}

		public	float AEPCPI( float angle) 
		{ 
			while ( angle > (Math.PI+Math.PI) ) angle -= (float)(Math.PI+Math.PI); 
			while ( angle < 0.0f ) angle += (float)(Math.PI+Math.PI); 
			return angle;
		}
		public	double AEPCPI( double angle) 
		{ 
			while ( angle > (Math.PI+Math.PI) ) angle -= (Math.PI+Math.PI); 
			while ( angle < 0.0 ) angle += (Math.PI+Math.PI); 
			return angle;
		}



		public void Limits (
			)

		{
			// Flip heading and roll when we go over the top or through the bottom.
			if ( theta > (Math.PI/2.0) ) 
			{
				theta = Math.PI - Theta;
				psi = AEPCPI( Psi + Math.PI );
				phi = AEPCPI( Phi + Math.PI );
			}
			else if ( theta < -(Math.PI/2.0) ) 
			{
				theta = -Math.PI - Theta; 
				psi = AEPCPI( Psi + Math.PI );
				phi = AEPCPI( Phi + Math.PI );
			}
			else 
			{
				psi = AEPCPI( Psi );
				phi = AEPCPI( Phi );
			}

		} // end Limits





		///////////////////////////////////////////////////////////////////////////////
		public void Limits (
			Euler results
			)

		{
			// Flip heading and roll when we go over the top or through the bottom.
			if ( results.Theta > (Math.PI/2.0) ) 
			{
				theta = (float)Math.PI - results.Theta;
				psi = (float)AEPCPI( results.Psi + (float)Math.PI );
				phi = (float)AEPCPI( results.Phi + (float)Math.PI );
			}
			else if ( results.Theta < -(Math.PI/2.0) ) 
			{
				theta = -(float)Math.PI - results.Theta; 
				psi = (float)AEPCPI( results.Psi + (float)Math.PI );
				phi = (float)AEPCPI( results.Phi + (float)Math.PI );
			}
			else 
			{
				theta = results.Theta;
				psi = (float)AEPCPI( results.Psi );
				phi = (float)AEPCPI( results.Phi );
			}

		} // end Limits





		///////////////////////////////////////////////////////////////////////////////
		public void Limits (
			float x_psi,
			float x_theta,
			float x_phi
			)

		{
			// Flip heading and roll when we go over the top or through the bottom
			if ( x_theta > (Math.PI/2.0) ) 
			{
				theta = (float)Math.PI - x_theta;
				psi = (float)AEPCPI( x_psi + Math.PI );
				phi = (float)AEPCPI( x_phi + Math.PI );
			}
			else if ( x_theta < -(Math.PI/2.0) ) 
			{
				theta = -(float)Math.PI - x_theta; 
				psi = (float)AEPCPI( x_psi + Math.PI );
				phi = (float)AEPCPI( x_phi + Math.PI );
			}
			else 
			{
				theta = x_theta;
				psi = (float)AEPCPI( x_psi );
				phi = (float)AEPCPI( x_phi );
			}

		} // end Limits





		///////////////////////////////////////////////////////////////////////////////
		public float 	AngularDifference( float ang1, float ang2 )
		{
			float result;

			result = ang1 - ang2;

			if ( result < 0.0 ) 
			{
				result *= -1.0f;
			}

			return result;
		}
		//=======================================================================
		public void RotateAtoE( Vector num )
		{
			double[] temp = new double[3];

			if ( !matrix_current ) CalcMatrix();
   
			temp[0] = mat[0,0] * num.X + mat[0,1] * num.Y + mat[0,2] * num.Z;
			temp[1] = mat[1,0] * num.X + mat[1,1] * num.Y + mat[1,2] * num.Z;
			temp[2] = mat[2,0] * num.X + mat[2,1] * num.Y + mat[2,2] * num.Z;

			num.SetX(temp[0]);
			num.SetY(temp[1]);
			num.SetZ(temp[2]);
		}
		//=======================================================================
		public void RotateEtoA( Vector num )
		{
			double[] temp = new double[3];

			if ( !matrix_current ) CalcMatrix();
   
			temp[0] = mat[0,0] * num.X + mat[1,0] * num.Y + mat[2,0] * num.Z;
			temp[1] = mat[0,1] * num.X + mat[1,1] * num.Y + mat[2,1] * num.Z;
			temp[2] = mat[0,2] * num.X + mat[1,2] * num.Y + mat[2,2] * num.Z;

			num.SetX(temp[0]);
			num.SetY(temp[1]);
			num.SetZ(temp[2]);
		}
		//====================================================================================================
		public void CalcMatrix()
		{
			mat[0,0] = ctheta * cpsi;
			mat[0,1] = sphi * stheta * cpsi - cphi * spsi;
			mat[0,2] = cphi * stheta * cpsi + sphi * spsi;

			mat[1,0] = ctheta * spsi;
			mat[1,1] = sphi * stheta * spsi + cphi * cpsi;
			mat[1,2] = cphi * stheta * spsi - sphi * cpsi;

			mat[2,0] = -stheta;
			mat[2,1] = sphi * ctheta;
			mat[2,2] = cphi * ctheta; 
   
			matrix_current = true;
		}

	};
}
