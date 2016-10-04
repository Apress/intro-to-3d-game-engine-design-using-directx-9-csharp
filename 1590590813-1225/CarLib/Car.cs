using System;
using System.Threading;
using System.Diagnostics;

namespace VehicleDynamics
{
	public class CarDynamics : IDisposable
	{
		public enum IgnitionState { IgnitionOff, IgnitionOn, IgnitionStart };
		public enum GearState { Park=0, Reverse=1, Neutral=2, Drive=3, FirstGear=4, SecondGear=5, ThirdGear=6 };

	#region Attributes
		private	Euler			attitude = new Euler();
		private	Euler			attitude_rate = new Euler();
		private	Euler			ground_slope = new Euler();
		private	Vector			position = new Vector();
		private	Vector			velocity = new Vector();			// body velocity
		private	Vector			earth_velocity = new Vector();		// earth velocity
		private	Vector			acceleration = new Vector();		// body accelerations
		private	Vector			body_gravity = new Vector();
		private	Wheel[]			wheel = new Wheel[4];
		private	double			weight;
		private	double			cg_height;
		private	double			wheel_base;
		private	double			wheel_track;
		private	double			wheel_angle;
		private	double			wheel_max_angle; 
		private	double			weight_distribution;
		private	double			front_weight;
		private	double			back_weight;
		private	double			wheel_pos;
		private	double			throttle;
		private	double			brake;
		private	double			engine_rpm;
		private	double			wheel_rpm;
		private	double			wheel_force;
		private	double			net_force;
		private	double			drag;
		private	double			rolling_resistance;
		private	double			eng_torque;
		private	double			mph;
		private	double			brake_torque;
		private	double			engine_loss;
		private	double			idle_rpm;
		private	double			max_rpm;
		private	double			target_rpm;
		private	double			engine_horsepower;
		private	double			max_engine_torque;
		private	double			air_density;
		private	double			drag_coeff;
		private	double			frontal_area;
		private	double			wheel_diameter;
		private	double			mass;		// in slugs
		private	double			inverse_mass;		// in slugs
		private	float			centripedal_accel;
		private	bool			running;
		private	bool			front_wheel_drive = false;
		private	GearState		gear;
		private	GearState		auto_gear = GearState.Drive;
		private	double[]		gear_ratio = new double[7];
		private	LFI				torque_curve = new LFI();      // percent max torque - index by % max RPM * 10
		private	IgnitionState	ignition_state = IgnitionState.IgnitionOn;
		private bool            was_sliding = false;
		private Thread          m_process_thread;
		private bool            thread_active = true;
		private Mutex           mutex = new Mutex();

	#endregion

	#region Properties
		public double SteeringWheel	{ get { return wheel_pos; } set { wheel_pos = value; } }

		public bool EngineRunning { get { return running; } }

		public double EngineRPM { get { return engine_rpm; } }

		public double Throttle { get { return throttle; } set { throttle = value; } }

		public double Brake { get { return brake; } set { brake = value; } }

		public GearState Gear { get { return gear; } set { gear = value; } }

		public IgnitionState Ignition { get { return ignition_state; }
			set 
			{
				ignition_state = value;
				if ( ignition_state == IgnitionState.IgnitionStart && 
					( gear == GearState.Park || gear == GearState.Neutral ) ) 
				{
					running = true;
				} 
				else if ( ignition_state == IgnitionState.IgnitionOff ) 
				{
					running = false;
				}
			}
		}

		public double Roll { get { return attitude.Phi; } set { attitude.Phi = value; } }
		public double Pitch { get { return attitude.Theta; } set { attitude.Theta = value; } }
		public double Heading { get { return attitude.Psi; } set { attitude.Psi = value; } }

		public double North { get { return position.X; } set { position.X = value; } }
		public double East { get { return position.Y; } set { position.Y = value; } }
		public double Height { get { return position.Z; } set { position.Z = value; } }

		public double NorthVelocity { get { return velocity.X; } set { velocity.X = value; } }
		public double EastVelocity { get { return velocity.Y; } set { velocity.Y = value; } }
		public double VerticalVelocity { get { return velocity.Z; } set { velocity.Z = value; } }

		public double ForwardVelocity { get { return velocity.X; } }
		public double SidewaysVelocity { get { return velocity.Y; } }
		public double WheelRadius { get { return wheel[(int)WhichWheel.LeftFront].radius; }
			set 
			{
				wheel_diameter = value * 2.0;
				wheel[(int)WhichWheel.LeftFront].radius = value;
				wheel[(int)WhichWheel.LeftRear].radius = value; 
				wheel[(int)WhichWheel.RightFront].radius = value; 
				wheel[(int)WhichWheel.RightRear].radius = value; } 
		}

		public double HorsePower { get { return engine_horsepower; } set { engine_horsepower = value; } }

		public double LFGroundHeight { set { wheel[(int)WhichWheel.LeftFront].ground_height = value; } }
		public double RFGroundHeight { set { wheel[(int)WhichWheel.RightFront].ground_height = value; } }
		public double LRGroundHeight { set { wheel[(int)WhichWheel.LeftRear].ground_height = value; } }
		public double RRGroundHeight { set { wheel[(int)WhichWheel.RightRear].ground_height = value; } }

		public double MPH { get { return mph; } }
		public bool   Running { get { return running; } }

	#endregion

		public CarDynamics()
		{
			engine_horsepower = 470.0f;
			torque_curve.SetDataPoint(  0, 0.0f );
			torque_curve.SetDataPoint(  10, 0.00f );
			torque_curve.SetDataPoint(  20, 0.22f );
			torque_curve.SetDataPoint(  30, 0.5f );
			torque_curve.SetDataPoint(  40, 0.72f );
			torque_curve.SetDataPoint(  50, 0.9f );
			torque_curve.SetDataPoint(  60, 1.0f );
			torque_curve.SetDataPoint(  70, 0.98f );
			torque_curve.SetDataPoint(  80, 0.89f );
			torque_curve.SetDataPoint(  90, 0.5f );
			torque_curve.SetDataPoint( 100, 0.13f );
			wheel[(int)WhichWheel.LeftFront]  = new Wheel(WhichWheel.LeftFront);
			wheel[(int)WhichWheel.RightFront] = new Wheel(WhichWheel.RightFront);
			wheel[(int)WhichWheel.LeftRear]   = new Wheel(WhichWheel.LeftRear);
			wheel[(int)WhichWheel.RightRear]  = new Wheel(WhichWheel.RightRear);
			Reset();
			m_process_thread = new Thread(new ThreadStart(Process));
			m_process_thread.Start();
		}

		public void Process()
		{
			float delta_t = 0.01f;
               
			Debug.WriteLine("car physics thread started");
			while ( thread_active )
			{
				Process( delta_t );
				Thread.Sleep(10);
			}
			Debug.WriteLine("car physics thread terminated");
		}

		public void Reset()
		{
			wheel_max_angle = 0.69813170079773183076947630739545; // 40 degrees
			idle_rpm = 700.0f;
			max_rpm = 7000.0f;
			engine_rpm = 0.0f;
			attitude.Theta = 0.0f;
			attitude.Phi = 0.0f;
			attitude.Psi = 0.0f;
			attitude_rate.Theta = 0.0;
			attitude_rate.Phi = 0.0;
			attitude_rate.Psi = 0.0;
			position.X = 0.0;
			position.Y = 0.0;
			position.Z = 0.0;
			velocity.X = 0.0;			// body velocity
			velocity.Y = 0.0;			// body velocity
			velocity.Z = 0.0;			// body velocity
			acceleration.X = 0.0;		// body accelerations
			acceleration.Y = 0.0;		// body accelerations
			acceleration.Z = 0.0;		// body accelerations
			cg_height             = -1.0;
			wheel_base            = 5.0;
			wheel_track           = 8.0;
			weight                = 2000.0;
			WheelRadius = 0.25;
			wheel[(int)WhichWheel.LeftFront ].offset.Set( wheel_track / 2.0f, -wheel_base / 2.0f, +cg_height + wheel[(int)WhichWheel.LeftFront].radius );
			wheel[(int)WhichWheel.RightFront].offset.Set( wheel_track / 2.0f,  wheel_base / 2.0f, +cg_height + wheel[(int)WhichWheel.LeftFront].radius );
			wheel[(int)WhichWheel.LeftRear  ].offset.Set(-wheel_track / 2.0f, -wheel_base / 2.0f, +cg_height + wheel[(int)WhichWheel.LeftFront].radius );
			wheel[(int)WhichWheel.RightRear ].offset.Set(-wheel_track / 2.0f,  wheel_base / 2.0f, +cg_height + wheel[(int)WhichWheel.LeftFront].radius );
			for ( int i=0; i<4; i++ ) 
			{
				wheel[i].StaticWeightOverWheel = weight / 4.0;
			}
			weight_distribution   = 0.5f;
			front_weight          = weight * ( 1.0 - weight_distribution);
			back_weight           = weight * weight_distribution;
			wheel_pos             = 0.0f;
			throttle              = 0.0f;
			brake                 = 0.0f;
			engine_rpm            = 0.0f;
			wheel_rpm             = 0.0f;
			wheel_force           = 0.0f;
			net_force             = 0.0f;
			mph                   = 0.0f;
			drag                  = 0.0f;
			rolling_resistance    = 0.0f;
			eng_torque            = 0.0f;
			brake_torque          = 0.0f;
			engine_loss           = 0.0f;
			air_density           = 0.068;
			drag_coeff            = 0.004f;
			frontal_area          = 20.0f;
			mass                  = weight * 0.031080950172;		// in slugs
			inverse_mass          = 1.0 / mass;
			running = true;
			front_wheel_drive = false;
			gear = GearState.Drive;
			gear_ratio[(int)GearState.Park]			= 0.0f;		
			gear_ratio[(int)GearState.Reverse]		= -80.0f;
			gear_ratio[(int)GearState.Neutral]		= 0.0f;
			gear_ratio[(int)GearState.Drive]		= 45.0f;
			gear_ratio[(int)GearState.FirstGear]	= 70.0f;
			gear_ratio[(int)GearState.SecondGear]	= 50.0f;
			gear_ratio[(int)GearState.ThirdGear]	= 30.0f;
			ignition_state = IgnitionState.IgnitionOn;
			max_engine_torque = engine_horsepower * 255.00f;

			if ( front_wheel_drive ) 
			{
				wheel[(int)WhichWheel.LeftFront ].drive_wheel = true;
				wheel[(int)WhichWheel.RightFront].drive_wheel = true;
				wheel[(int)WhichWheel.LeftRear  ].drive_wheel = false;
				wheel[(int)WhichWheel.RightRear ].drive_wheel = false;
			} 
			else 
			{
				wheel[(int)WhichWheel.LeftFront ].drive_wheel = false;
				wheel[(int)WhichWheel.RightFront].drive_wheel = false;
				wheel[(int)WhichWheel.LeftRear  ].drive_wheel = true;
				wheel[(int)WhichWheel.RightRear ].drive_wheel = true;
			}
		}

		private void IntegratePosition( float delta_t )
		{
   
			Vector	earth_velocity;            

			velocity.IncrementX( delta_t * acceleration.X );
			velocity.IncrementY( delta_t * acceleration.Y );
			velocity.IncrementZ( delta_t * acceleration.Z );

			if ( velocity.X > 48.0 ) 
			{
				velocity.X = 48.0;
				acceleration.X = -1.0;
			}
			if ( velocity.X < 0.0 ) 
			{
				velocity.X = 0.0;
				acceleration.X = 0.5;
			}

			attitude = attitude + (attitude_rate * delta_t);
			attitude.Limits();

			earth_velocity = new Vector( velocity );
			attitude.RotateAtoE( earth_velocity );

			position.IncrementX( delta_t * earth_velocity.X );
			position.IncrementY( delta_t * earth_velocity.Y );
			position.IncrementZ( delta_t * earth_velocity.Z );
//			Debug.WriteLine("accelZ = " + acceleration.Z + " velz = " + velocity.Z + " posz = " + position.Z);

			mph = (float)velocity.X * 3600.0f / 5280.0f;
			
		}

		private void CalcWeightTransfer()
		{
			front_weight = (1.0f - weight_distribution) * weight + (float)acceleration.X * cg_height / wheel_base;
			back_weight = weight - front_weight;

			wheel[(int)WhichWheel.LeftFront].WeightOverWheel  = 0.5f * front_weight - (float)acceleration.Y * cg_height / wheel_track;
			wheel[(int)WhichWheel.RightFront].WeightOverWheel = front_weight - wheel[(int)WhichWheel.LeftFront].WeightOverWheel;
			wheel[(int)WhichWheel.LeftRear].WeightOverWheel   = 0.5f * back_weight - (float)acceleration.Y * cg_height / wheel_track;
			wheel[(int)WhichWheel.RightRear].WeightOverWheel  = back_weight - wheel[(int)WhichWheel.LeftRear].WeightOverWheel;
		}

		public void SetFriction(WhichWheel the_wheel, float friction)
		{
			wheel[(int)the_wheel].friction = friction;
		}

		public void SetGearRatio(GearState state, float ratio)
		{
			gear_ratio[(int)state] = ratio;
		}

		public float WheelNorth(WhichWheel the_wheel)
		{
			return (float)wheel[(int)the_wheel].earth_location.X;
		}

		public float WheelEast(WhichWheel the_wheel)
		{
			return (float)wheel[(int)the_wheel].earth_location.Y;
		}

		public float WheelHeight(WhichWheel the_wheel)
		{
			return (float)wheel[(int)the_wheel].earth_location.Z;
		}

		public void SetWheelAltitude(WhichWheel the_wheel, float altitude)
		{
			wheel[(int)the_wheel].ground_height = altitude;
		}

		public void SetWheelOffset(WhichWheel the_wheel, float forward, float right, float up)
		{
			wheel[(int)the_wheel].offset.X = forward;
			wheel[(int)the_wheel].offset.Y = right;
			wheel[(int)the_wheel].offset.Z = up;
		}

		public void Dispose()
		{
			Debug.WriteLine("car physics Dispose");
			thread_active = false;
			Thread.Sleep(20);
		}

		void Process(float delta_t)
		{
			double temp;  
			double delta_rpm;
			double current_gear_ratio = 0.0;
			double brake_force;
			double percent_rpm;
			double turn_rate;
			bool  shift_up;
			bool  shift_down;
			double delta_psi;
			Vector	gravity = new Vector(0.0f, 0.0f, 32.0f);        

			wheel_angle = wheel_pos * wheel_max_angle;

			wheel[(int)WhichWheel.LeftFront].RelHeading = wheel_angle;
			wheel[(int)WhichWheel.RightFront].RelHeading = wheel_angle;

			double sine_wheel = Math.Sin(wheel_angle);

			turn_rate = (sine_wheel * velocity.X / wheel_track) / 10.0f;
//			Debug.WriteLine( "sine_wheel=" + sine_wheel + " turn rate=" + turn_rate);

			if ( wheel[(int)WhichWheel.LeftFront].sliding && wheel[(int)WhichWheel.RightFront].sliding ) 
			{
//				turn_rate = 0.0f;
			}

			attitude_rate.Psi = turn_rate;

			delta_psi = turn_rate * delta_t;

			centripedal_accel = (float)(2.0 * velocity.X * Math.Sin(delta_psi) ) / delta_t;

			wheel_rpm = 60.0f * velocity.X / (Math.PI * wheel_diameter);

			rolling_resistance = 0.00696f * (float)Math.Abs(velocity.X);

			drag = 0.5f * drag_coeff * frontal_area * air_density * Math.Abs(velocity.X * velocity.X);

			brake_force = brake * 32.0;  // max braking 1G

			if ( mph < 0.0 ) // braking force opposes movement
			{
				brake_force *= -1.0;
			}

			if ( wheel[(int)WhichWheel.LeftFront].sliding && wheel[(int)WhichWheel.RightFront].sliding && 
				wheel[(int)WhichWheel.RightRear].sliding && wheel[(int)WhichWheel.RightRear].sliding ) 
			{
				brake_force = 0.0f;
			}

			percent_rpm = engine_rpm / max_rpm;

			switch ( gear ) 
			{
				case GearState.Park:
					if ( mph > 1.0  || mph < -1.0 ) 
					{
						brake_force = 32.0;
					} 
					else 
					{
						velocity.SetX(0.0f);
					}
					auto_gear = GearState.Park;
					break;
				case GearState.Reverse:
					auto_gear = GearState.Reverse;
					break;
				case GearState.Neutral:
					auto_gear = GearState.Neutral;
					break;
				case GearState.Drive:
					shift_up = false;
					shift_down = false;
					if ( ( percent_rpm > 0.8 && auto_gear < GearState.Drive ) ||
						( percent_rpm > 0.1 && auto_gear == GearState.Neutral ) ) 
					{
						shift_up = true;
					}
					if ( percent_rpm < 0.4 && auto_gear >= GearState.FirstGear ) 
					{
						shift_down = true;
					}
				switch ( auto_gear ) 
				{
					case GearState.Neutral:
						if ( shift_up ) 
						{
							auto_gear = GearState.FirstGear;
						} 
						break;
					case GearState.Drive:
						if ( shift_down ) 
						{
							auto_gear = GearState.ThirdGear;
						} 
						break;
					case GearState.FirstGear:
						if ( shift_up ) 
						{
							auto_gear = GearState.SecondGear;
						} 
						else if ( shift_down ) 
						{
							auto_gear = GearState.Neutral;
						} 
						break;
					case GearState.SecondGear:
						if ( shift_up ) 
						{
							auto_gear = GearState.ThirdGear;
						} 
						else if ( shift_down ) 
						{
							auto_gear = GearState.FirstGear;
						} 
						break;
					case GearState.ThirdGear:
						if ( shift_up ) 
						{
							auto_gear = GearState.Drive;
						} 
						else if ( shift_down ) 
						{
							auto_gear = GearState.SecondGear;
						} 
						break;
				}
					break;
				case GearState.FirstGear:
					auto_gear = GearState.FirstGear;
					break;
				case GearState.SecondGear:
					auto_gear = GearState.SecondGear;
					break;
				case GearState.ThirdGear:
					auto_gear = GearState.ThirdGear;
					break;
			}
			current_gear_ratio = gear_ratio[(int)auto_gear];

			if ( running && target_rpm < idle_rpm ) 
			{
				target_rpm = idle_rpm;
			} 
			else if ( !running ) 
			{
				target_rpm = 0.0f;
			}
			else 
			{
				target_rpm = idle_rpm + throttle * ( max_rpm - idle_rpm);
			}
			delta_rpm = target_rpm - engine_rpm;
			if ( delta_rpm > 3000.0f ) 
			{
				delta_rpm = 3000.0f;
			} 
			else if ( delta_rpm < -3000.0f ) 
			{
				delta_rpm = -3000.0f;
			}
			if ( delta_rpm < 1.0f && delta_rpm > -1.0f ) 
			{
				delta_rpm = 0.0f;
			}
			engine_rpm += (delta_rpm * delta_t );
			if ( auto_gear == GearState.Neutral || gear == GearState.Park ) 
			{
				eng_torque = 0.0;
			} 
			else 
			{
				eng_torque = torque_curve.Interpolate(percent_rpm * 100.0) * max_engine_torque;
			}

			engine_loss = Math.Max(((engine_rpm/20) * (engine_rpm/20) + 45), 0.0);

			brake_torque = brake_force * mass; 

			temp = (eng_torque - engine_loss);

			if ( temp < 0.0 && Math.Abs(mph) < 0.1 ) 
			{
				temp = 0.0;
			}

			if ( current_gear_ratio != 0.0 ) 
			{
				wheel_force = temp;
			} 
			else 
			{
				wheel_force = 0.0f;
			}

			if ( (drag + rolling_resistance) > wheel_force )
			{
				wheel_force = drag + rolling_resistance;
			}
			net_force = wheel_force - drag - rolling_resistance;
//Debug.WriteLine("wheel force=" + wheel_force.ToString() + " drag=" + drag + " rolling resist=" + rolling_resistance);

			if ( gear == GearState.Reverse ) 
			{
				net_force *= -1.0f;      // force in reverse is in opposite direction
			}

			ground_slope.RotateEtoA( gravity );
			body_gravity = gravity;

			if ( gear != GearState.Park ) 
			{
				double net_accel = net_force/mass;  // covert to an accel
				net_accel -= brake_force;
				acceleration.X = net_accel;// + body_gravity.X;
//				Debug.WriteLine("Accel X=" + acceleration.X.ToString() + " body grav X=" + body_gravity.X.ToString()+
//					" net force= " + net_force.ToString());
//				Debug.WriteLine("ground slope=" + ground_slope.ThetaAsDegrees().ToString());
			}
			acceleration.Z -= body_gravity.Z;

			if ( velocity.X < ( delta_t * brake_force ) && velocity.X > ( delta_t * -brake_force ) ) 
			{
				mph = 0.0f;
				velocity.X = 0.0;
				acceleration.X =0.0;
				brake_force = 0.0;
			}

			if ( auto_gear == GearState.Neutral || gear == GearState.Park ) 
			{
				temp = idle_rpm + (max_rpm-idle_rpm) * throttle;
			} 
			else 
			{
				temp = velocity.X * current_gear_ratio;
			}

			if ( temp >= (idle_rpm * 0.9f) ) 
			{
				if ( temp > max_rpm ) 
				{
					target_rpm = max_rpm;
				} 
				else 
				{
					target_rpm = temp;
				}
			} 
			else 
			{
				target_rpm = idle_rpm;
			}

			CalcWeightTransfer();

			ProcessWheels( delta_t );

			ProcessAttitude( delta_t );

			IntegratePosition( delta_t );
		}

		void SetAttitude(float roll, float pitch, float heading)
		{
			attitude.Phi = roll;
			attitude.Theta = pitch;
			attitude.Psi = heading;
		}

		void SetPosition(float north, float east, float height)
		{
			position.X = north;
			position.Y = east;
			position.Z = height;
		}

		void SetVelocity(float north, float east, float vertical)
		{
			velocity.X = north;
			velocity.Y = east;
			velocity.Z = vertical;
		}

		void SetGroundHeight(WhichWheel the_wheel, float height)
		{
			wheel[(int)the_wheel].ground_height = height;
		}

		void SetAllGroundHeights(float left_front, float right_front, float left_rear, float right_rear )
		{
			wheel[(int)WhichWheel.LeftFront].ground_height = left_front;
			wheel[(int)WhichWheel.RightFront].ground_height = right_front;
			wheel[(int)WhichWheel.LeftRear].ground_height = left_rear;
			wheel[(int)WhichWheel.RightRear].ground_height = right_rear;
		}

		double WheelAngle( bool in_degrees )
		{
			double result;

			if ( in_degrees ) 
			{
				result = (wheel_angle * 180.0 / Math.PI);
			} 
			else 
			{
				result = wheel_angle;
			}

			return result;
		}

		double GetPitch(bool in_degrees)
		{
			double result;

			if ( in_degrees ) 
			{
				result = attitude.ThetaAsDegrees();
			} 
			else 
			{
				result = attitude.Theta;
			}

			return result;
		}

		double GetRoll(bool in_degrees)
		{
			double result;

			if ( in_degrees ) 
			{
				result = attitude.PhiAsDegrees();
			} 
			else 
			{
				result = attitude.Phi;
			}

			return result;
		}

		bool IsTireSquealing(WhichWheel the_wheel)
		{
			return wheel[(int)the_wheel].squealing;
		}

		bool IsTireLoose(WhichWheel the_wheel)
		{
			return wheel[(int)the_wheel].sliding;
		}

		void SetTireStiction(float new_value)
		{
			wheel[(int)WhichWheel.LeftFront].Stiction = new_value;
			wheel[(int)WhichWheel.RightFront].Stiction = new_value;
			wheel[(int)WhichWheel.LeftRear].Stiction = new_value;
			wheel[(int)WhichWheel.RightRear].Stiction = new_value;

		}
		void ProcessWheels(float delta_t)
		{
			int		i;
			double	accel;
			double	total_upwards_force = 0.0;
			bool	bottomed_out = false;
			bool	on_ground = false;
			int		sliding = 0;
			double	avg_suspension = 0.0;
			double	delta_force;
			double	avg_ground_height = 0.0;

			acceleration.SetY(-centripedal_accel);

			for ( i=0; i<4; i++ ) 
			{
				wheel[i].Process( delta_t, attitude, acceleration, velocity, position );
				total_upwards_force += wheel[i].UpwardsForce;
				avg_suspension += wheel[i].suspension_offset;
				avg_ground_height += wheel[i].ground_height;
				if ( wheel[i].bottomed_out ) 
				{
					bottomed_out = true;
				}
				if ( wheel[i].touching_ground ) 
				{
					on_ground = true;
				}
				if ( wheel[i].sliding ) 
				{
					sliding++;
				}
			}
			avg_suspension /= 4.0f;
			avg_ground_height /= 4.0f;

			if ( Math.Abs(avg_suspension) < 0.1f ) 
			{
				velocity.Z = velocity.Z * 0.2;

			}

			accel = total_upwards_force / mass;
//			Debug.WriteLine("upwards force " + accel.ToString());
			acceleration.Z = accel - body_gravity.Z;
//			Debug.WriteLine("Accel Z="+acceleration.Z.ToString());

			if ( on_ground ) 
			{
				velocity.Z = velocity.Z * 0.75;
//				Debug.WriteLine("on ground");
			}

			if ( bottomed_out ) 
			{
//				position.Z = avg_ground_height + wheel[0].offset.X + wheel[0].radius;
//				Debug.WriteLine("bottomed out");
			}

			if ( bottomed_out && velocity.Z < 0.0f ) 
			{
				velocity.Z = 0.0;
//				Debug.WriteLine("bottomed out & velocity cleared");
			}

			if ( sliding > 2 && !was_sliding ) 
			{
				was_sliding = true;
//				velocity.Y = acceleration.Y;
			}
			if ( sliding > 2 && velocity.Y > 0.0 ) 
			{
				acceleration.Y = -32.0;
//				Debug.WriteLine("sliding");
			} 
			else if ( sliding > 2 && velocity.Y < 0.0 ) 
			{
				acceleration.Y = 32.0;
//				Debug.WriteLine("sliding");
			} 
			else 
			{
				velocity.Y = 0.0;
				acceleration.Y = 0.0;
				was_sliding = false;
			}
		}

		void ProcessAttitude(float delta_t)
		{
			double avg_front;
			double avg_rear;
			double pitch;
			double avg_left;
			double avg_right;
			double roll;

			// first do ground slope
			avg_front = (wheel[(int)WhichWheel.LeftFront].ground_height + wheel[(int)WhichWheel.RightFront].ground_height) / 2.0;
			avg_rear  = (wheel[(int)WhichWheel.LeftRear].ground_height + wheel[(int)WhichWheel.RightRear].ground_height) / 2.0;
			pitch = Math.Asin((avg_front - avg_rear) / wheel_base);

			ground_slope.Theta = pitch;

			avg_left = (wheel[(int)WhichWheel.LeftFront].ground_height + wheel[(int)WhichWheel.LeftRear].ground_height) / 2.0;
			avg_right  = (wheel[(int)WhichWheel.RightFront].ground_height + wheel[(int)WhichWheel.RightRear].ground_height) / 2.0;
			roll = Math.Asin((avg_left - avg_right) / wheel_track);

			ground_slope.Phi = roll;

			// now do vehicle attitude
			avg_front = (wheel[(int)WhichWheel.LeftFront].suspension_offset + wheel[(int)WhichWheel.RightFront].suspension_offset) / 2.0f;
			avg_rear  = (wheel[(int)WhichWheel.LeftRear].suspension_offset + wheel[(int)WhichWheel.RightRear].suspension_offset) / 2.0f;
			pitch = Math.Asin((avg_front - avg_rear) / wheel_base);
			pitch = 0.0;

//			attitude_rate.Theta = (ground_slope.Theta+pitch-attitude.Theta) * 0.025;
//			attitude.Theta = ground_slope.Theta;

			avg_left = (wheel[(int)WhichWheel.LeftFront].suspension_offset + wheel[(int)WhichWheel.LeftRear].suspension_offset) / 2.0f;
			avg_right  = (wheel[(int)WhichWheel.RightFront].suspension_offset + wheel[(int)WhichWheel.RightRear].suspension_offset) / 2.0f;
			roll = Math.Asin((avg_left - avg_right) / wheel_track);
			roll = 0.0;

//			attitude_rate.Phi = (ground_slope.Phi-roll-attitude.Phi) * 0.05;
//			attitude.Phi = ground_slope.Phi;

		}

		public void MinorCollision()
		{
			velocity = velocity * 0.9f;
		}

		public void MajorCollision( float delta_x_velocity, 
			float delta_y_velocity, 
			float delta_z_velocity, 
			float delta_x_position, 
			float delta_y_position, 
			float delta_z_position )
		{
			Vector RelativeVelocity = new Vector( delta_x_velocity, delta_y_velocity, delta_z_velocity );
			Vector CollisionNormal = new Vector( delta_x_position, delta_y_position, delta_z_position );
			CollisionNormal.Normalize();

			double collisionSpeed = RelativeVelocity * CollisionNormal;

			float impulse = (float)(( 2.50 * collisionSpeed ) / 
				(( CollisionNormal * CollisionNormal) * 
				( inverse_mass )));

			Debug.WriteLine("major collision");
			Debug.WriteLine("velocity prior to crash x="+velocity.X+" y="+velocity.Y+" z="+velocity.Z);
			velocity = ( CollisionNormal * impulse ) * (float)inverse_mass;
			Debug.WriteLine("velocity after crash x="+velocity.X+" y="+velocity.Y+" z="+velocity.Z);
			engine_rpm = idle_rpm;
		}
	};
}
