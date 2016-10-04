// created on 10/22/2002 at 12:02 PM
using System;

namespace VehicleDynamics
{
	public enum WhichWheel { LeftFront=0, RightFront, LeftRear, RightRear };

	public struct Wheel  
	{
		#region Attributes

		public	Vector	    offset;
		public  Vector      earth_location;		
		private double		rel_heading;
		public	double		radius;
		public	double		ground_height;
		public	double		altitude;
		public	double		height_above_ground;
		private	double		weight_over_wheel;
		private	double		static_weight_over_wheel;
		public	double		suspension_offset;
		public	double		max_suspension_offset;
		private	double		upwards_force;
		public	double		friction;
		private	double		stiction;
		private	double		sliding_friction;
		public	bool		bottomed_out;
		public	bool		touching_ground;
		public	bool		squealing;
		public	bool		sliding;
		public	bool		drive_wheel;
		public WhichWheel   position;

		private static double spring_constant = 61000.0;
		private static double damping = -700.0;
		#endregion

		#region Properties

		public double RelHeading { set { rel_heading = value; } }
		public double UpwardsForce { get { return upwards_force; } }
		public double WeightOverWheel { set { weight_over_wheel = value; } get { return weight_over_wheel; } }
		public double StaticWeightOverWheel { set { static_weight_over_wheel = value; } }
		public double Stiction 
		{ 
			set 
			{ 
				stiction = value; 
				sliding_friction = 0.6f * stiction;
			}
		}
		#endregion
	
		public Wheel(WhichWheel where)
		{
			position = where;
			offset = new Vector(0.0, 0.0, 0.0);
			earth_location = new Vector(0.0, 0.0, 0.0);
			rel_heading = 0.0;
			radius = 0.5;
			height_above_ground = 0.0;
			ground_height = 0.0;
			friction = 1.0;
			weight_over_wheel = 0.0;
			static_weight_over_wheel = 0.0;
			suspension_offset = 0.0;
			max_suspension_offset = 0.25;
			altitude = 0.0;
			bottomed_out = false;
			drive_wheel = false;
			sliding = false;
			sliding_friction = 0.0;
			squealing = false;
			stiction = 1.0;
			upwards_force = 0.0;
			touching_ground = true;
		}

		public void Process(float delta_t, Euler attitude, Vector acceleration, Vector velocity, Vector position)
		{
			double	temp;
			double	susp_delta;
			double	squeel_force;
			double	slide_force;
			double	grab_force;
			double	tire_side_force;

			earth_location.X = offset.X;
			earth_location.Y = offset.Y;
			earth_location.Z = offset.Z + suspension_offset;
			attitude.RotateAtoE( earth_location );

			altitude = position.Z + earth_location.Z - radius;

			height_above_ground = altitude - ground_height;

			touching_ground = height_above_ground <= 0.0f;

			if ( touching_ground ) 
			{
				suspension_offset = -height_above_ground;
			} 
			else 
			{
				suspension_offset = -max_suspension_offset;
			}

			susp_delta = (suspension_offset + height_above_ground) * delta_t;
			if ( Math.Abs(upwards_force - weight_over_wheel) < 2.0 ) 
			{
				suspension_offset -= susp_delta;
			}
			if ( suspension_offset > max_suspension_offset ) 
			{
				suspension_offset = max_suspension_offset;
			} 
			else if ( suspension_offset < -max_suspension_offset ) 
			{
				suspension_offset = -max_suspension_offset;
			}
			bottomed_out = suspension_offset == max_suspension_offset;

			temp = ( 0.3f * ( suspension_offset * suspension_offset ) / (max_suspension_offset * max_suspension_offset ) );
			if ( suspension_offset < 0.0f ) 
			{
				temp *= -1.0f;
			}
			if ( Math.Abs(suspension_offset) < 0.3f ) 
			{
				temp = 0.0f;  // suspension neutral
			}
			temp += 1.0f;
			if ( !touching_ground ) 
			{
				temp = 0.0f;
			}

			double spring_force = spring_constant * suspension_offset;
			double damping_force = damping * suspension_offset / max_suspension_offset;

			if ( touching_ground ) 
			{
				upwards_force = (spring_force + damping_force);
			} 
			else 
			{
				upwards_force = 0.0;
			}
//			upwards_force = static_weight_over_wheel * temp;  

			if ( (upwards_force - weight_over_wheel) > 2.0f ) 
			{
				suspension_offset -= 0.5f * delta_t;
			} 
			else if ( (upwards_force - weight_over_wheel) < -2.0f ) 
			{
				suspension_offset += 0.5f * delta_t;
			}

			slide_force = 32.0f * stiction * friction;
			squeel_force = 0.9f * slide_force;
			grab_force = 32.0f * sliding_friction;

			if ( (acceleration.Y > 0.0f && rel_heading > 0.0f ) || (acceleration.Y < 0.0f && rel_heading < 0.0f ) ) 
			{
				tire_side_force = (float)Math.Abs(acceleration.Y * (1.0f - Math.Cos(rel_heading)));
			} 
			else 
			{
				tire_side_force = (float)Math.Abs(acceleration.Y * Math.Cos(rel_heading));
			}

			squealing = false;
			sliding = false;
			if ( drive_wheel && acceleration.X >= slide_force ) 
			{  
				sliding = true;
			}
			if ( (acceleration.X < -squeel_force && acceleration.X > -slide_force) || 
				(tire_side_force > squeel_force && tire_side_force < slide_force) ) 
			{
				squealing = true;
			}
			if ( acceleration.X <= -slide_force || tire_side_force >= slide_force ) 
			{
				sliding = true;
			}
			if ( Math.Abs(acceleration.X) < grab_force && Math.Abs(velocity.Y)< grab_force && tire_side_force < grab_force ) 
			{
				sliding = false;
			}
		}

	};
}
