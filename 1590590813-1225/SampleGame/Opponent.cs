using System;
using Microsoft.DirectX;
using GameEngine;
using GameAI;
using VehicleDynamics;
using System.Collections;
using System.Diagnostics;

namespace SampleGame
{
	/// <summary>
	/// Summary description for Opponent.
	/// </summary>
	public class Opponent : Car
	{
		#region Attributes
		private Thinker m_thinker;
		private Camera  m_camera;
		#endregion

		#region Properties
		public Camera Eyes { get { return m_camera; } }
		#endregion

		public Opponent(string name, string meshFile, Vector3 offset, Attitude adjust,
			string knowledge ) : base (name, meshFile, offset, adjust)
		{
			m_camera = new Camera(name + " cam");
			m_camera.Attach( this, new Vector3(0.0f, 0.0f, 0.0f ));

			Thinker.AddAction( "SteerLeft", new Thinker.ActionMethod( SteerLeft ) );
			Thinker.AddAction( "SteerStraight", new Thinker.ActionMethod( SteerStraight ) );
			Thinker.AddAction( "SteerRight", new Thinker.ActionMethod( SteerRight ) );
			Thinker.AddAction( "HitTheBrakes", new Thinker.ActionMethod( HitTheBrakes ) );
			Thinker.AddAction( "Accelerate", new Thinker.ActionMethod( Accelerate ) );

			m_thinker = new Thinker( this );

			m_thinker.AddSensorMethod( new Thinker.SensorMethod( DriverView) );

			m_thinker.Read( knowledge );
		}

		public void DriverView( Thinker thinker )
		{
			ArrayList objects = new ArrayList();

			Opponent self = (Opponent)thinker.Self;
			Camera eyes = self.Eyes;

			// get a local copy of the objects that the camera can see
			objects.Clear();
			foreach ( Object3D obj in eyes.VisibleObjects )
			{
				objects.Add( obj );
			}
			
			float range_to_nearest = 10000.0f;
			float bearing_to_nearest = 0.0f;
			Object3D nearest_object = null;

			// nearest red post
			foreach ( Object3D obj in objects )
			{
				if ( obj.Name.Substring(0,3) == "red" )
				{
					float range = eyes.GetDistance(obj);
					if ( range < range_to_nearest )
					{
						range_to_nearest = range;
						nearest_object = obj;
					}
				}
			}
			if ( nearest_object != null )
			{
				bearing_to_nearest = GetBearing( self, nearest_object );
				thinker.SetFact("red_post_in_sight", 1.0f );
				thinker.SetFact("red_post_range", range_to_nearest );
				thinker.SetFact("red_post_bearing", bearing_to_nearest );
			}
			else
			{
				thinker.SetFact("red_post_in_sight", 0.0f );
			}

			// nearest blue post
			range_to_nearest = 10000.0f;
			foreach ( Object3D obj in objects )
			{
				if ( obj.Name.Substring(0,4) == "blue" )
				{
					float range = eyes.GetDistance(obj);
					if ( range < range_to_nearest )
					{
						range_to_nearest = range;
						nearest_object = obj;
					}
				}
			}
			if ( nearest_object != null )
			{
				bearing_to_nearest = GetBearing( self, nearest_object );
				thinker.SetFact("blue_post_in_sight", 1.0f );
				thinker.SetFact("blue_post_range", range_to_nearest );
				thinker.SetFact("blue_post_bearing", bearing_to_nearest );
			}
			else
			{
				thinker.SetFact("blue_post_in_sight", 0.0f );
			}

			// nearest obstacle (vehicles and trees)
			range_to_nearest = 10000.0f;
			foreach ( Object3D obj in objects )
			{
				if ( obj.Name.Substring(0,4) == "tree" ||
					 obj.Name.Substring(0,3) == "car" )
				{
					float bearing = GetBearing( self, nearest_object );
					float range = eyes.GetDistance(obj);

					// only accept nearest object within +/- 5 degrees
					if ( Math.Abs(bearing) < 0.087266462599716478846184538424431 && range < range_to_nearest )
					{
						range_to_nearest = range;
						nearest_object = obj;
						bearing_to_nearest = bearing;
					}
				}
			}
			if ( nearest_object != null )
			{
				thinker.SetFact("obstacle_in_sight", 1.0f );
				thinker.SetFact("obstacle_range", range_to_nearest );
				thinker.SetFact("obstacle_bearing", bearing_to_nearest );
			}
			else
			{
				thinker.SetFact("obstacle_in_sight", 0.0f );
			}
		}

		void SteerLeft( Thinker thinker )
		{
			Opponent self = (Opponent)thinker.Self;

			if ( self.Steering > -1.0 ) self.Steering = self.Steering - 0.01f;
		}

		void SteerStraight( Thinker thinker )
		{
			Opponent self = (Opponent)thinker.Self;

			if ( self.Steering > 0.0 ) self.Steering = self.Steering - 0.01f;
			else if ( self.Steering < 0.0 ) self.Steering = self.Steering + 0.01f;
		}

		void SteerRight( Thinker thinker )
		{
			Opponent self = (Opponent)thinker.Self;

			if ( self.Steering < 1.0 ) self.Steering = self.Steering + 0.01f;
		}

		void HitTheBrakes( Thinker thinker )
		{
			Opponent self = (Opponent)thinker.Self;

			self.Gas = 0.0f;

			if ( self.Brake < 1.0 ) self.Brake = self.Brake + 0.1f;
		}

		void Accelerate( Thinker thinker )
		{
			Opponent self = (Opponent)thinker.Self;

			self.Brake = 0.0f;

			if ( self.Gas < 1.0 ) self.Gas = self.Gas + 0.1f;
		}

		float GetBearing( Object3D self, Object3D other )
		{
			float bearing = 0.0f;

			Vector3 direction = other.Position - self.Position;
			if ( direction.X != 0.0 )
			{
				bearing = (float)Math.Atan(direction.Z / direction.X);
			}
			else
			{
				if ( direction.Z > 0.0 )
				{
					bearing = 0.0f;
				}
				else
				{
					bearing = (float)Math.PI;
				}
			}
			return bearing;
		}

		public override void Dispose()
		{
			Debug.WriteLine("disposing of " + Name + " in opponent");
			m_thinker.Dispose();
			base.Dispose();
		}
	}

}
