using System;
using System.Diagnostics;
using Microsoft.DirectX;
using GameEngine;
using GameAI;
using VehicleDynamics;

namespace SampleGame
{
	/// <summary>
	/// Summary description for Car.
	/// </summary>
	public class Car : Model
	{
		#region Attributes
		private CarDynamics m_dynamics;
		private float steering_wheel = 0.0f;
		private float brake_pedal = 0.0f;
		private float gas_pedal = 0.0f;
		private float terrain_limit = 2000.0f;
		#endregion

		#region Properties
		public CarDynamics Dynamics { get { return m_dynamics; } }
		public float Steering { set { steering_wheel = value; } get { return steering_wheel; } }
		public float Brake { set { brake_pedal = value; } get { return brake_pedal; } }
		public float Gas { set { gas_pedal = value; } get { return gas_pedal; } }
		public override float   North    { get { return m_vPosition.Z; } set { m_vPosition.Z = value; m_dynamics.North = value; m_bHasMoved = true;} }
		public override float   East     { get { return m_vPosition.X; } set { m_vPosition.X = value; m_dynamics.East = value; m_bHasMoved = true;} }
		public override float   Height   { get { return m_vPosition.Y; } set { m_vPosition.Y = value; m_dynamics.Height = value; m_bHasMoved = true;} }
		public override float   Roll     { get { return m_vOrientation.Roll; } set { m_vOrientation.Roll = value; m_dynamics.Roll = value; } }
		public override float   Pitch    { get { return m_vOrientation.Pitch; } set { m_vOrientation.Pitch = value; m_dynamics.Pitch = value; } }
		public override float   Heading  { get { return m_vOrientation.Heading; } set { m_vOrientation.Heading = value; m_dynamics.Heading = value; } }
		public int MPH { get { return (int)(m_dynamics.MPH); } }
		public int RPM { get { return (int)(m_dynamics.EngineRPM); } }
		public double ForwardVelocity { get { return m_dynamics.ForwardVelocity; } }
		public double SidewaysVelocity { get { return m_dynamics.SidewaysVelocity; } }
		public bool Driving { 
			set { 
				if ( value ) m_dynamics.Gear = CarDynamics.GearState.Drive;
				else m_dynamics.Gear = CarDynamics.GearState.Park;
			} 
		}
		#endregion

		public Car(string name, string meshFile, Vector3 offset, Attitude adjust ) 
			: base (name, meshFile, offset, adjust)
		{
			m_dynamics = new CarDynamics();

			// start the engine
//			m_dynamics.Gear = CarDynamics.GearState.Park;
			m_dynamics.Ignition = CarDynamics.IgnitionState.IgnitionStart;
			m_dynamics.Gear = CarDynamics.GearState.Drive;
		}
		public override void Update( float DeltaT )
		{
//			System.Diagnostics.Debug.WriteLine("car update");

			m_dynamics.Brake = brake_pedal;
			m_dynamics.SteeringWheel = steering_wheel;
			m_dynamics.Throttle = gas_pedal;

			float tire_north = m_dynamics.WheelNorth(WhichWheel.LeftFront) + North;
			float tire_east  = m_dynamics.WheelEast(WhichWheel.LeftFront) + East;
			float tire_altitude = CGameEngine.Ground.TerrainHeight( tire_east, tire_north );
			m_dynamics.SetWheelAltitude( WhichWheel.LeftFront, tire_altitude );
//			GameEngine.Console.AddLine( Name + " LeftFront height " + tire_altitude + " at n="+tire_north+" e="+tire_east);

			tire_north = m_dynamics.WheelNorth(WhichWheel.LeftRear) + North;
			tire_east  = m_dynamics.WheelEast(WhichWheel.LeftRear) + East;
			tire_altitude = CGameEngine.Ground.TerrainHeight( tire_east, tire_north );
			m_dynamics.SetWheelAltitude( WhichWheel.LeftRear, tire_altitude );
//			GameEngine.Console.AddLine( Name + " LeftRear height " + tire_altitude + " at n="+tire_north+" e="+tire_east);

			tire_north = m_dynamics.WheelNorth(WhichWheel.RightRear) + North;
			tire_east  = m_dynamics.WheelEast(WhichWheel.RightRear) + East;
			tire_altitude = CGameEngine.Ground.TerrainHeight( tire_east, tire_north );
			m_dynamics.SetWheelAltitude( WhichWheel.RightRear, tire_altitude );
//			GameEngine.Console.AddLine( Name + " RightRear height " + tire_altitude + " at n="+tire_north+" e="+tire_east);

			tire_north = m_dynamics.WheelNorth(WhichWheel.RightFront) + North;
			tire_east  = m_dynamics.WheelEast(WhichWheel.RightFront) + East;
			tire_altitude = CGameEngine.Ground.TerrainHeight( tire_east, tire_north );
			m_dynamics.SetWheelAltitude( WhichWheel.RightFront, tire_altitude );
//			GameEngine.Console.AddLine( Name + " RightFront height " + tire_altitude + " at n="+tire_north+" e="+tire_east);

			if ( (float)m_dynamics.North > 10.0 && (float)m_dynamics.North < (terrain_limit-10.0f) )
			{
				m_vPosition.Z = (float)m_dynamics.North;
			}
			else
			{
				m_dynamics.North = m_vPosition.Z;
			}
			if ( (float)m_dynamics.East > 10.0 && (float)m_dynamics.East < (terrain_limit-10.0f) )
			{
				m_vPosition.X  = (float)m_dynamics.East;
			}
			else
			{
				m_dynamics.East = m_vPosition.X;
			}

			Heading = (float)m_dynamics.Heading;
			Pitch   = (float)m_dynamics.Pitch;
			Roll    = (float)m_dynamics.Roll;

			base.VelocityX = (float)m_dynamics.EastVelocity;
			base.VelocityZ = (float)m_dynamics.NorthVelocity;
			base.VelocityY = (float)m_dynamics.VerticalVelocity;

			m_bHasMoved = true;

			base.Update( DeltaT );
		}

		public override void Dispose()
		{
			Debug.WriteLine("Disposing of " + Name + " in Car");
			m_dynamics.Dispose();
			base.Dispose();
		}
	}
}
